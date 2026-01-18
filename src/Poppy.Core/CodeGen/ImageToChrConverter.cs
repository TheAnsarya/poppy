namespace Poppy.Core.CodeGen;

using System.Text;

/// <summary>
/// Converts PNG/BMP images to CHR tile data for NES/SNES/etc
/// </summary>
public static class ImageToChrConverter {
	/// <summary>
	/// Options for image to CHR conversion
	/// </summary>
	public record ConversionOptions {
		/// <summary>Tile width in pixels (default: 8)</summary>
		public int TileWidth { get; init; } = 8;

		/// <summary>Tile height in pixels (default: 8)</summary>
		public int TileHeight { get; init; } = 8;

		/// <summary>Bits per pixel (1, 2, 4, or 8)</summary>
		public int BitsPerPixel { get; init; } = 2;

		/// <summary>Tile format (planar NES/SNES, linear GBA, etc)</summary>
		public TileFormat Format { get; init; } = TileFormat.NesPlanar;

		/// <summary>Number of tiles per row in source image (0 = auto-detect)</summary>
		public int TilesPerRow { get; init; } = 0;

		/// <summary>Generate assembly source instead of binary</summary>
		public bool GenerateAsm { get; init; } = false;

		/// <summary>Label prefix for assembly output</summary>
		public string LabelPrefix { get; init; } = "chr_";

		/// <summary>Use lowercase hex</summary>
		public bool LowercaseHex { get; init; } = true;
	}

	/// <summary>
	/// Supported tile formats
	/// </summary>
	public enum TileFormat {
		/// <summary>NES 2bpp planar (8 bytes low, 8 bytes high)</summary>
		NesPlanar,

		/// <summary>SNES 4bpp planar (bitplane interleaved)</summary>
		Snes4bpp,

		/// <summary>SNES 2bpp planar (same as NES)</summary>
		Snes2bpp,

		/// <summary>GBA 4bpp linear (4 bits per pixel, sequential)</summary>
		Gba4bpp,

		/// <summary>GBA 8bpp linear (8 bits per pixel, sequential)</summary>
		Gba8bpp,

		/// <summary>Game Boy 2bpp planar (same as NES)</summary>
		GameBoy2bpp
	}

	/// <summary>
	/// Convert a BMP image to CHR tile data
	/// </summary>
	public static byte[] ConvertBmpToChr(byte[] bmpData, ConversionOptions? options = null) {
		options ??= new ConversionOptions();

		// Parse BMP header
		if (bmpData.Length < 54 || bmpData[0] != 'B' || bmpData[1] != 'M') {
			throw new ArgumentException("Invalid BMP file");
		}

		int dataOffset = BitConverter.ToInt32(bmpData, 10);
		int width = BitConverter.ToInt32(bmpData, 18);
		int height = Math.Abs(BitConverter.ToInt32(bmpData, 22));
		int bitsPerPixel = BitConverter.ToInt16(bmpData, 28);
		bool topDown = BitConverter.ToInt32(bmpData, 22) < 0;

		// Calculate tiles
		int tilesX = width / options.TileWidth;
		int tilesY = height / options.TileHeight;
		int totalTiles = tilesX * tilesY;

		// Read pixel data
		byte[,] pixels = ReadBmpPixels(bmpData, dataOffset, width, height, bitsPerPixel, topDown);

		// Convert to tiles
		var tiles = new List<byte[]>();
		for (int ty = 0; ty < tilesY; ty++) {
			for (int tx = 0; tx < tilesX; tx++) {
				var tile = ExtractTile(pixels, tx, ty, options.TileWidth, options.TileHeight);
				var encoded = EncodeTile(tile, options.Format, options.BitsPerPixel);
				tiles.Add(encoded);
			}
		}

		// Combine all tiles
		return CombineTiles(tiles);
	}

	/// <summary>
	/// Convert image file to CHR and save
	/// </summary>
	public static void ConvertFile(string inputPath, string outputPath, ConversionOptions? options = null) {
		var bmpData = File.ReadAllBytes(inputPath);
		var chrData = ConvertBmpToChr(bmpData, options);
		Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
		File.WriteAllBytes(outputPath, chrData);
	}

	/// <summary>
	/// Convert image file to assembly source
	/// </summary>
	public static string ConvertToAsm(byte[] bmpData, string tableName, ConversionOptions? options = null) {
		options ??= new ConversionOptions();
		var chrData = ConvertBmpToChr(bmpData, options);

		var sb = new StringBuilder();
		sb.AppendLine("; " + new string('=', 76));
		sb.AppendLine($"; {tableName} CHR Data");
		sb.AppendLine("; " + new string('=', 76));
		sb.AppendLine($"; Format: {options.Format}");
		sb.AppendLine($"; Tiles: {chrData.Length / GetBytesPerTile(options.Format, options.BitsPerPixel)}");
		sb.AppendLine("; " + new string('=', 76));
		sb.AppendLine();

		string label = $"{options.LabelPrefix}{SanitizeLabel(tableName)}";
		sb.AppendLine($"{label}:");

		// Output bytes in rows of 16
		int bytesPerTile = GetBytesPerTile(options.Format, options.BitsPerPixel);
		int tileIndex = 0;

		for (int i = 0; i < chrData.Length; i += bytesPerTile) {
			sb.AppendLine($"; Tile ${tileIndex:x2}");
			sb.AppendLine($"{label}_{tileIndex:x2}:");

			int tileEnd = Math.Min(i + bytesPerTile, chrData.Length);
			for (int j = i; j < tileEnd; j += 8) {
				sb.Append("\t.byte ");
				var bytes = new List<string>();
				for (int k = j; k < Math.Min(j + 8, tileEnd); k++) {
					bytes.Add(FormatHex(chrData[k], options.LowercaseHex));
				}
				sb.AppendLine(string.Join(", ", bytes));
			}

			tileIndex++;
		}

		sb.AppendLine();
		sb.AppendLine($"{label}_end:");

		return sb.ToString();
	}

	/// <summary>
	/// Encode a single tile to the specified format
	/// </summary>
	public static byte[] EncodeTile(byte[,] tile, TileFormat format, int bpp) {
		return format switch {
			TileFormat.NesPlanar or TileFormat.Snes2bpp or TileFormat.GameBoy2bpp =>
				Encode2bppPlanar(tile),
			TileFormat.Snes4bpp =>
				Encode4bppSnesPlanar(tile),
			TileFormat.Gba4bpp =>
				Encode4bppLinear(tile),
			TileFormat.Gba8bpp =>
				Encode8bppLinear(tile),
			_ => throw new ArgumentException($"Unsupported format: {format}")
		};
	}

	/// <summary>
	/// Encode 2bpp planar (NES/SNES/GB format)
	/// </summary>
	public static byte[] Encode2bppPlanar(byte[,] tile) {
		int height = tile.GetLength(0);
		int width = tile.GetLength(1);
		var result = new byte[height * 2]; // 8 bytes low plane + 8 bytes high plane

		for (int y = 0; y < height; y++) {
			byte low = 0, high = 0;
			for (int x = 0; x < width; x++) {
				int shift = 7 - x;
				byte pixel = (byte)(tile[y, x] & 0x03);
				if ((pixel & 1) != 0) low |= (byte)(1 << shift);
				if ((pixel & 2) != 0) high |= (byte)(1 << shift);
			}
			result[y] = low;
			result[y + height] = high;
		}

		return result;
	}

	/// <summary>
	/// Encode 4bpp SNES planar format
	/// </summary>
	public static byte[] Encode4bppSnesPlanar(byte[,] tile) {
		int height = tile.GetLength(0);
		int width = tile.GetLength(1);
		var result = new byte[height * 4]; // 32 bytes per tile

		for (int y = 0; y < height; y++) {
			byte bp0 = 0, bp1 = 0, bp2 = 0, bp3 = 0;
			for (int x = 0; x < width; x++) {
				int shift = 7 - x;
				byte pixel = (byte)(tile[y, x] & 0x0f);
				if ((pixel & 1) != 0) bp0 |= (byte)(1 << shift);
				if ((pixel & 2) != 0) bp1 |= (byte)(1 << shift);
				if ((pixel & 4) != 0) bp2 |= (byte)(1 << shift);
				if ((pixel & 8) != 0) bp3 |= (byte)(1 << shift);
			}
			// SNES 4bpp interleaved: row0-bp0, row0-bp1, row1-bp0, row1-bp1...
			result[y * 2] = bp0;
			result[y * 2 + 1] = bp1;
			result[16 + y * 2] = bp2;
			result[16 + y * 2 + 1] = bp3;
		}

		return result;
	}

	/// <summary>
	/// Encode 4bpp linear format (GBA)
	/// </summary>
	public static byte[] Encode4bppLinear(byte[,] tile) {
		int height = tile.GetLength(0);
		int width = tile.GetLength(1);
		var result = new byte[height * width / 2]; // 4 bits per pixel

		int idx = 0;
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x += 2) {
				byte low = (byte)(tile[y, x] & 0x0f);
				byte high = (byte)(tile[y, x + 1] & 0x0f);
				result[idx++] = (byte)(low | (high << 4));
			}
		}

		return result;
	}

	/// <summary>
	/// Encode 8bpp linear format (GBA)
	/// </summary>
	public static byte[] Encode8bppLinear(byte[,] tile) {
		int height = tile.GetLength(0);
		int width = tile.GetLength(1);
		var result = new byte[height * width];

		int idx = 0;
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				result[idx++] = tile[y, x];
			}
		}

		return result;
	}

	/// <summary>
	/// Get bytes per tile for a given format
	/// </summary>
	public static int GetBytesPerTile(TileFormat format, int bpp) {
		return format switch {
			TileFormat.NesPlanar or TileFormat.Snes2bpp or TileFormat.GameBoy2bpp => 16,
			TileFormat.Snes4bpp => 32,
			TileFormat.Gba4bpp => 32,
			TileFormat.Gba8bpp => 64,
			_ => 16
		};
	}

	private static byte[,] ReadBmpPixels(byte[] bmpData, int dataOffset, int width, int height, int bpp, bool topDown) {
		var pixels = new byte[height, width];
		int stride = ((width * bpp + 31) / 32) * 4; // BMP row stride (aligned to 4 bytes)

		for (int y = 0; y < height; y++) {
			int srcY = topDown ? y : (height - 1 - y);
			int rowOffset = dataOffset + srcY * stride;

			for (int x = 0; x < width; x++) {
				byte pixel = bpp switch {
					8 => bmpData[rowOffset + x],
					4 => (byte)((bmpData[rowOffset + x / 2] >> (x % 2 == 0 ? 4 : 0)) & 0x0f),
					1 => (byte)((bmpData[rowOffset + x / 8] >> (7 - x % 8)) & 1),
					24 => GetGrayscale(bmpData, rowOffset + x * 3),
					32 => GetGrayscale(bmpData, rowOffset + x * 4),
					_ => 0
				};
				pixels[y, x] = pixel;
			}
		}

		return pixels;
	}

	private static byte GetGrayscale(byte[] data, int offset) {
		// Convert RGB to grayscale index (0-3 for 2bpp)
		int b = data[offset];
		int g = data[offset + 1];
		int r = data[offset + 2];
		int gray = (r * 30 + g * 59 + b * 11) / 100;
		return (byte)(gray / 64); // Map to 0-3 range
	}

	private static byte[,] ExtractTile(byte[,] pixels, int tileX, int tileY, int tileWidth, int tileHeight) {
		var tile = new byte[tileHeight, tileWidth];
		int startX = tileX * tileWidth;
		int startY = tileY * tileHeight;

		for (int y = 0; y < tileHeight; y++) {
			for (int x = 0; x < tileWidth; x++) {
				int srcY = startY + y;
				int srcX = startX + x;
				if (srcY < pixels.GetLength(0) && srcX < pixels.GetLength(1)) {
					tile[y, x] = pixels[srcY, srcX];
				}
			}
		}

		return tile;
	}

	private static byte[] CombineTiles(List<byte[]> tiles) {
		int totalBytes = 0;
		foreach (var tile in tiles) totalBytes += tile.Length;

		var result = new byte[totalBytes];
		int offset = 0;
		foreach (var tile in tiles) {
			Array.Copy(tile, 0, result, offset, tile.Length);
			offset += tile.Length;
		}

		return result;
	}

	private static string FormatHex(byte b, bool lowercase) {
		return lowercase ? $"${b:x2}" : $"${b:X2}";
	}

	private static string SanitizeLabel(string name) {
		var sb = new StringBuilder();
		foreach (char c in name) {
			if (char.IsLetterOrDigit(c)) {
				sb.Append(char.ToLowerInvariant(c));
			} else if (sb.Length > 0 && sb[^1] != '_') {
				sb.Append('_');
			}
		}
		while (sb.Length > 0 && sb[^1] == '_') sb.Length--;
		return sb.ToString();
	}
}
