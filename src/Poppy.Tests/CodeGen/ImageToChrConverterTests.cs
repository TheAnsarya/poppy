namespace Poppy.Tests.CodeGen;

using Poppy.Core.CodeGen;

public class ImageToChrConverterTests {
	#region BMP Parsing Tests

	private static byte[] CreateTestBmp(int width, int height, byte[] pixels, int bpp = 8) {
		// Create a minimal indexed BMP file
		int stride = ((width * bpp + 31) / 32) * 4;
		int paletteSize = bpp <= 8 ? (1 << bpp) * 4 : 0;
		int headerSize = 54 + paletteSize;
		int dataSize = stride * height;
		int fileSize = headerSize + dataSize;

		var bmp = new byte[fileSize];

		// BMP header
		bmp[0] = (byte)'B';
		bmp[1] = (byte)'M';
		BitConverter.GetBytes(fileSize).CopyTo(bmp, 2);
		BitConverter.GetBytes(headerSize).CopyTo(bmp, 10);

		// DIB header
		BitConverter.GetBytes(40).CopyTo(bmp, 14); // Header size
		BitConverter.GetBytes(width).CopyTo(bmp, 18);
		BitConverter.GetBytes(-height).CopyTo(bmp, 22); // Top-down
		BitConverter.GetBytes((short)1).CopyTo(bmp, 26); // Planes
		BitConverter.GetBytes((short)bpp).CopyTo(bmp, 28);

		// Palette (grayscale for 8bpp)
		if (bpp == 8) {
			for (int i = 0; i < 256; i++) {
				int offset = 54 + i * 4;
				bmp[offset] = (byte)i;     // B
				bmp[offset + 1] = (byte)i; // G
				bmp[offset + 2] = (byte)i; // R
				bmp[offset + 3] = 0;       // Reserved
			}
		} else if (bpp == 4) {
			for (int i = 0; i < 16; i++) {
				int offset = 54 + i * 4;
				byte val = (byte)(i * 17);
				bmp[offset] = val;
				bmp[offset + 1] = val;
				bmp[offset + 2] = val;
				bmp[offset + 3] = 0;
			}
		}

		// Pixel data
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				int srcIdx = y * width + x;
				if (bpp == 8) {
					bmp[headerSize + y * stride + x] = srcIdx < pixels.Length ? pixels[srcIdx] : (byte)0;
				} else if (bpp == 4) {
					int byteIdx = x / 2;
					byte pixel = srcIdx < pixels.Length ? (byte)(pixels[srcIdx] & 0x0f) : (byte)0;
					if (x % 2 == 0) {
						bmp[headerSize + y * stride + byteIdx] = (byte)(pixel << 4);
					} else {
						bmp[headerSize + y * stride + byteIdx] |= pixel;
					}
				}
			}
		}

		return bmp;
	}

	[Fact]
	public void ConvertBmpToChr_SingleTile_ConvertsCorrectly() {
		// Create an 8x8 pixel test image (single tile)
		// Pattern: alternating 0 and 1 pixels
		var pixels = new byte[64];
		for (int i = 0; i < 64; i++) {
			pixels[i] = (byte)(i % 2);
		}

		var bmp = CreateTestBmp(8, 8, pixels);
		var options = new ImageToChrConverter.ConversionOptions {
			Format = ImageToChrConverter.TileFormat.NesPlanar
		};

		var chr = ImageToChrConverter.ConvertBmpToChr(bmp, options);

		// NES 2bpp planar: 16 bytes per tile
		Assert.Equal(16, chr.Length);
	}

	[Fact]
	public void ConvertBmpToChr_MultipleTiles_ConvertsAll() {
		// Create a 16x8 pixel image (2 tiles)
		var pixels = new byte[128];
		var bmp = CreateTestBmp(16, 8, pixels);

		var chr = ImageToChrConverter.ConvertBmpToChr(bmp);

		// 2 tiles * 16 bytes = 32 bytes
		Assert.Equal(32, chr.Length);
	}

	[Fact]
	public void ConvertBmpToChr_4x4Tiles_ConvertsAll() {
		// Create a 32x32 pixel image (16 tiles in 4x4 arrangement)
		var pixels = new byte[32 * 32];
		var bmp = CreateTestBmp(32, 32, pixels);

		var chr = ImageToChrConverter.ConvertBmpToChr(bmp);

		// 16 tiles * 16 bytes = 256 bytes
		Assert.Equal(256, chr.Length);
	}

	#endregion

	#region 2bpp Planar Encoding Tests

	[Fact]
	public void Encode2bppPlanar_AllZeros_ReturnsZeros() {
		var tile = new byte[8, 8];
		var result = ImageToChrConverter.Encode2bppPlanar(tile);

		Assert.Equal(16, result.Length);
		Assert.All(result, b => Assert.Equal(0, b));
	}

	[Fact]
	public void Encode2bppPlanar_AllOnes_SetsLowPlane() {
		var tile = new byte[8, 8];
		for (int y = 0; y < 8; y++)
			for (int x = 0; x < 8; x++)
				tile[y, x] = 1;

		var result = ImageToChrConverter.Encode2bppPlanar(tile);

		// Low plane (first 8 bytes) should be $ff
		for (int i = 0; i < 8; i++) {
			Assert.Equal(0xff, result[i]);
		}
		// High plane (last 8 bytes) should be $00
		for (int i = 8; i < 16; i++) {
			Assert.Equal(0x00, result[i]);
		}
	}

	[Fact]
	public void Encode2bppPlanar_AllTwos_SetsHighPlane() {
		var tile = new byte[8, 8];
		for (int y = 0; y < 8; y++)
			for (int x = 0; x < 8; x++)
				tile[y, x] = 2;

		var result = ImageToChrConverter.Encode2bppPlanar(tile);

		// Low plane should be $00
		for (int i = 0; i < 8; i++) {
			Assert.Equal(0x00, result[i]);
		}
		// High plane should be $ff
		for (int i = 8; i < 16; i++) {
			Assert.Equal(0xff, result[i]);
		}
	}

	[Fact]
	public void Encode2bppPlanar_AllThrees_SetsBothPlanes() {
		var tile = new byte[8, 8];
		for (int y = 0; y < 8; y++)
			for (int x = 0; x < 8; x++)
				tile[y, x] = 3;

		var result = ImageToChrConverter.Encode2bppPlanar(tile);

		// Both planes should be $ff
		Assert.All(result, b => Assert.Equal(0xff, b));
	}

	[Fact]
	public void Encode2bppPlanar_CheckerPattern_EncodesCorrectly() {
		var tile = new byte[8, 8];
		for (int y = 0; y < 8; y++)
			for (int x = 0; x < 8; x++)
				tile[y, x] = (byte)((x + y) % 2);

		var result = ImageToChrConverter.Encode2bppPlanar(tile);

		// Even rows: 01010101 = $55
		// Odd rows:  10101010 = $aa
		Assert.Equal(0x55, result[0]); // Row 0
		Assert.Equal(0xaa, result[1]); // Row 1
	}

	#endregion

	#region 4bpp Encoding Tests

	[Fact]
	public void Encode4bppSnesPlanar_AllZeros_ReturnsZeros() {
		var tile = new byte[8, 8];
		var result = ImageToChrConverter.Encode4bppSnesPlanar(tile);

		Assert.Equal(32, result.Length);
		Assert.All(result, b => Assert.Equal(0, b));
	}

	[Fact]
	public void Encode4bppSnesPlanar_AllFifteens_SetsAllPlanes() {
		var tile = new byte[8, 8];
		for (int y = 0; y < 8; y++)
			for (int x = 0; x < 8; x++)
				tile[y, x] = 0x0f;

		var result = ImageToChrConverter.Encode4bppSnesPlanar(tile);

		Assert.Equal(32, result.Length);
		Assert.All(result, b => Assert.Equal(0xff, b));
	}

	[Fact]
	public void Encode4bppLinear_AllZeros_ReturnsZeros() {
		var tile = new byte[8, 8];
		var result = ImageToChrConverter.Encode4bppLinear(tile);

		Assert.Equal(32, result.Length);
		Assert.All(result, b => Assert.Equal(0, b));
	}

	[Fact]
	public void Encode4bppLinear_Sequential_PacksCorrectly() {
		var tile = new byte[8, 8];
		// First row: 0, 1, 2, 3, 4, 5, 6, 7
		for (int x = 0; x < 8; x++)
			tile[0, x] = (byte)x;

		var result = ImageToChrConverter.Encode4bppLinear(tile);

		// Packed: (0|1<<4), (2|3<<4), (4|5<<4), (6|7<<4)
		Assert.Equal(0x10, result[0]); // 0 | (1 << 4)
		Assert.Equal(0x32, result[1]); // 2 | (3 << 4)
		Assert.Equal(0x54, result[2]); // 4 | (5 << 4)
		Assert.Equal(0x76, result[3]); // 6 | (7 << 4)
	}

	#endregion

	#region 8bpp Encoding Tests

	[Fact]
	public void Encode8bppLinear_AllZeros_ReturnsZeros() {
		var tile = new byte[8, 8];
		var result = ImageToChrConverter.Encode8bppLinear(tile);

		Assert.Equal(64, result.Length);
		Assert.All(result, b => Assert.Equal(0, b));
	}

	[Fact]
	public void Encode8bppLinear_Sequential_PreservesOrder() {
		var tile = new byte[8, 8];
		byte val = 0;
		for (int y = 0; y < 8; y++)
			for (int x = 0; x < 8; x++)
				tile[y, x] = val++;

		var result = ImageToChrConverter.Encode8bppLinear(tile);

		Assert.Equal(64, result.Length);
		for (int i = 0; i < 64; i++) {
			Assert.Equal(i, result[i]);
		}
	}

	#endregion

	#region Assembly Output Tests

	[Fact]
	public void ConvertToAsm_GeneratesValidAsm() {
		var pixels = new byte[64];
		var bmp = CreateTestBmp(8, 8, pixels);

		var asm = ImageToChrConverter.ConvertToAsm(bmp, "TestGraphics");

		Assert.Contains("; TestGraphics CHR Data", asm);
		Assert.Contains("chr_testgraphics:", asm);
		Assert.Contains(".byte", asm);
		Assert.Contains("chr_testgraphics_end:", asm);
	}

	[Fact]
	public void ConvertToAsm_UsesCustomPrefix() {
		var pixels = new byte[64];
		var bmp = CreateTestBmp(8, 8, pixels);
		var options = new ImageToChrConverter.ConversionOptions {
			LabelPrefix = "gfx_"
		};

		var asm = ImageToChrConverter.ConvertToAsm(bmp, "Tiles", options);

		Assert.Contains("gfx_tiles:", asm);
		Assert.Contains("gfx_tiles_end:", asm);
	}

	[Fact]
	public void ConvertToAsm_IncludesTileCount() {
		var pixels = new byte[128];
		var bmp = CreateTestBmp(16, 8, pixels);

		var asm = ImageToChrConverter.ConvertToAsm(bmp, "Sprites");

		Assert.Contains("; Tiles: 2", asm);
	}

	[Fact]
	public void ConvertToAsm_LowercaseHex() {
		var pixels = new byte[64];
		for (int i = 0; i < 64; i++) pixels[i] = 0xff;
		var bmp = CreateTestBmp(8, 8, pixels);
		var options = new ImageToChrConverter.ConversionOptions {
			LowercaseHex = true
		};

		var asm = ImageToChrConverter.ConvertToAsm(bmp, "Test", options);

		Assert.Contains("$ff", asm);
		Assert.DoesNotContain("$FF", asm);
	}

	[Fact]
	public void ConvertToAsm_UppercaseHex() {
		var pixels = new byte[64];
		for (int i = 0; i < 64; i++) pixels[i] = 0xff;
		var bmp = CreateTestBmp(8, 8, pixels);
		var options = new ImageToChrConverter.ConversionOptions {
			LowercaseHex = false
		};

		var asm = ImageToChrConverter.ConvertToAsm(bmp, "Test", options);

		Assert.Contains("$FF", asm);
	}

	#endregion

	#region Bytes Per Tile Tests

	[Fact]
	public void GetBytesPerTile_ReturnsCorrectValues() {
		Assert.Equal(16, ImageToChrConverter.GetBytesPerTile(
			ImageToChrConverter.TileFormat.NesPlanar, 2));
		Assert.Equal(16, ImageToChrConverter.GetBytesPerTile(
			ImageToChrConverter.TileFormat.Snes2bpp, 2));
		Assert.Equal(16, ImageToChrConverter.GetBytesPerTile(
			ImageToChrConverter.TileFormat.GameBoy2bpp, 2));
		Assert.Equal(32, ImageToChrConverter.GetBytesPerTile(
			ImageToChrConverter.TileFormat.Snes4bpp, 4));
		Assert.Equal(32, ImageToChrConverter.GetBytesPerTile(
			ImageToChrConverter.TileFormat.Gba4bpp, 4));
		Assert.Equal(64, ImageToChrConverter.GetBytesPerTile(
			ImageToChrConverter.TileFormat.Gba8bpp, 8));
	}

	#endregion

	#region Edge Cases

	[Fact]
	public void ConvertBmpToChr_InvalidBmp_ThrowsException() {
		var invalidData = new byte[] { 0x00, 0x00, 0x00 };

		Assert.Throws<ArgumentException>(() =>
			ImageToChrConverter.ConvertBmpToChr(invalidData));
	}

	[Fact]
	public void EncodeTile_UnsupportedFormat_ThrowsException() {
		var tile = new byte[8, 8];

		Assert.Throws<ArgumentException>(() =>
			ImageToChrConverter.EncodeTile(tile, (ImageToChrConverter.TileFormat)99, 2));
	}

	#endregion
}
