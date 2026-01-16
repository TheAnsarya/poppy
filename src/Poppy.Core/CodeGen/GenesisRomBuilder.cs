// ============================================================================
// GenesisRomBuilder.cs - Sega Genesis/Mega Drive ROM Builder
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================
// The Sega Genesis uses a 512-byte header at ROM offset $100-$1ff.
// The header contains console name, copyright, title, and technical info.
// All multi-byte values are big-endian.
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builds Sega Genesis/Mega Drive ROM images with proper header format.
/// </summary>
public sealed class GenesisRomBuilder {
	/// <summary>
	/// Standard ROM sizes for Genesis cartridges.
	/// </summary>
	public static class RomSizes {
		/// <summary>512 KB ROM</summary>
		public const int Size512K = 512 * 1024;
		/// <summary>1 MB ROM</summary>
		public const int Size1M = 1024 * 1024;
		/// <summary>2 MB ROM</summary>
		public const int Size2M = 2 * 1024 * 1024;
		/// <summary>4 MB ROM (maximum without mapper)</summary>
		public const int Size4M = 4 * 1024 * 1024;
	}

	/// <summary>
	/// Region codes for Genesis cartridges.
	/// </summary>
	public static class RegionCodes {
		/// <summary>Japan region</summary>
		public const string Japan = "J";
		/// <summary>USA region</summary>
		public const string USA = "U";
		/// <summary>Europe region</summary>
		public const string Europe = "E";
		/// <summary>All regions (JUE)</summary>
		public const string World = "JUE";
	}

	private readonly int _romSize;
	private readonly List<(int Address, byte[] Data)> _segments;

	// Header fields
	private string _consoleName = "SEGA MEGA DRIVE ";  // 16 chars, padded
	private string _copyright = "(C)POPPY  2026  ";   // 16 chars: (C)XXXX YYYY.MMM
	private string _domesticName = "";                 // 48 chars, domestic (Japanese) title
	private string _overseasName = "";                 // 48 chars, overseas title
	private string _productCode = "GM 00000000-00";    // 14 chars
	private ushort _checksum = 0;                      // Calculated checksum
	private string _ioSupport = "J               ";    // 16 chars, I/O device support
	private uint _romStart = 0x00000000;               // ROM start address
	private uint _romEnd = 0x003fffff;                 // ROM end address
	private uint _ramStart = 0x00ff0000;               // RAM start (backup RAM)
	private uint _ramEnd = 0x00ffffff;                 // RAM end
	private bool _hasSram = false;                     // SRAM enabled
	private bool _sramOddBytes = false;                // SRAM on odd bytes
	private bool _sramEvenBytes = true;                // SRAM on even bytes
	private uint _sramStart = 0x00200000;              // SRAM start address
	private uint _sramEnd = 0x0020ffff;                // SRAM end address
	private string _modem = "            ";            // 12 chars, modem support
	private string _memo = "                                        "; // 40 chars, memo
	private string _region = "JUE             ";       // 16 chars, region codes

	/// <summary>
	/// Creates a new Genesis ROM builder.
	/// </summary>
	/// <param name="romSize">The target ROM size in bytes.</param>
	public GenesisRomBuilder(int romSize = RomSizes.Size1M) {
		_romSize = romSize;
		_segments = [];
		_romEnd = (uint)(romSize - 1);
	}

	/// <summary>
	/// Sets the console name (usually "SEGA MEGA DRIVE " or "SEGA GENESIS    ").
	/// </summary>
	/// <param name="name">The console name (max 16 chars).</param>
	/// <returns>This builder for chaining.</returns>
	public GenesisRomBuilder SetConsoleName(string name) {
		_consoleName = PadString(name, 16);
		return this;
	}

	/// <summary>
	/// Sets the copyright string (format: "(C)XXXX YYYY.MMM").
	/// </summary>
	/// <param name="copyright">The copyright string (max 16 chars).</param>
	/// <returns>This builder for chaining.</returns>
	public GenesisRomBuilder SetCopyright(string copyright) {
		_copyright = PadString(copyright, 16);
		return this;
	}

	/// <summary>
	/// Sets the domestic (Japanese) title.
	/// </summary>
	/// <param name="title">The domestic title (max 48 chars).</param>
	/// <returns>This builder for chaining.</returns>
	public GenesisRomBuilder SetDomesticName(string title) {
		_domesticName = PadString(title, 48);
		return this;
	}

	/// <summary>
	/// Sets the overseas title.
	/// </summary>
	/// <param name="title">The overseas title (max 48 chars).</param>
	/// <returns>This builder for chaining.</returns>
	public GenesisRomBuilder SetOverseasName(string title) {
		_overseasName = PadString(title, 48);
		return this;
	}

	/// <summary>
	/// Sets the product code.
	/// </summary>
	/// <param name="code">The product code (max 14 chars).</param>
	/// <returns>This builder for chaining.</returns>
	public GenesisRomBuilder SetProductCode(string code) {
		_productCode = PadString(code, 14);
		return this;
	}

	/// <summary>
	/// Sets the I/O support string (e.g., "J" for joypad, "6" for 6-button, etc.).
	/// </summary>
	/// <param name="ioSupport">The I/O support string (max 16 chars).</param>
	/// <returns>This builder for chaining.</returns>
	public GenesisRomBuilder SetIOSupport(string ioSupport) {
		_ioSupport = PadString(ioSupport, 16);
		return this;
	}

	/// <summary>
	/// Configures SRAM (battery backup) support.
	/// </summary>
	/// <param name="enabled">Whether SRAM is enabled.</param>
	/// <param name="startAddress">SRAM start address (default $200000).</param>
	/// <param name="endAddress">SRAM end address (default $20ffff for 64KB).</param>
	/// <param name="oddBytes">SRAM on odd bytes.</param>
	/// <param name="evenBytes">SRAM on even bytes.</param>
	/// <returns>This builder for chaining.</returns>
	public GenesisRomBuilder SetSram(bool enabled, uint startAddress = 0x00200000, uint endAddress = 0x0020ffff, bool oddBytes = false, bool evenBytes = true) {
		_hasSram = enabled;
		_sramStart = startAddress;
		_sramEnd = endAddress;
		_sramOddBytes = oddBytes;
		_sramEvenBytes = evenBytes;
		return this;
	}

	/// <summary>
	/// Sets the memo field.
	/// </summary>
	/// <param name="memo">The memo text (max 40 chars).</param>
	/// <returns>This builder for chaining.</returns>
	public GenesisRomBuilder SetMemo(string memo) {
		_memo = PadString(memo, 40);
		return this;
	}

	/// <summary>
	/// Sets the region codes.
	/// </summary>
	/// <param name="region">The region string (e.g., "J", "U", "E", "JUE").</param>
	/// <returns>This builder for chaining.</returns>
	public GenesisRomBuilder SetRegion(string region) {
		_region = PadString(region, 16);
		return this;
	}

	/// <summary>
	/// Adds a code/data segment to the ROM.
	/// </summary>
	/// <param name="address">The start address in ROM.</param>
	/// <param name="data">The data bytes.</param>
	public void AddSegment(int address, byte[] data) {
		_segments.Add((address, data));
	}

	/// <summary>
	/// Builds the final ROM image.
	/// </summary>
	/// <returns>The complete ROM data with header.</returns>
	public byte[] Build() {
		var rom = new byte[_romSize];

		// Fill with $ff (typical for empty ROM space)
		Array.Fill(rom, (byte)0xff);

		// Write segments to ROM
		foreach (var (address, data) in _segments) {
			if (address >= 0 && address + data.Length <= _romSize) {
				Array.Copy(data, 0, rom, address, data.Length);
			}
		}

		// Write header at $100
		WriteHeader(rom);

		// Calculate and write checksum
		_checksum = CalculateChecksum(rom);
		WriteBigEndianWord(rom, 0x18e, _checksum);

		return rom;
	}

	/// <summary>
	/// Writes the ROM header at offset $100.
	/// </summary>
	private void WriteHeader(byte[] rom) {
		var offset = 0x100;

		// $100-$10f: Console name (16 bytes)
		WriteString(rom, offset, _consoleName, 16);
		offset += 16;

		// $110-$11f: Copyright (16 bytes)
		WriteString(rom, offset, _copyright, 16);
		offset += 16;

		// $120-$14f: Domestic name (48 bytes)
		WriteString(rom, offset, _domesticName, 48);
		offset += 48;

		// $150-$17f: Overseas name (48 bytes)
		WriteString(rom, offset, _overseasName, 48);
		offset += 48;

		// $180-$18d: Product code (14 bytes)
		WriteString(rom, offset, _productCode, 14);
		offset += 14;

		// $18e-$18f: Checksum (2 bytes, big-endian) - written later
		offset += 2;

		// $190-$19f: I/O support (16 bytes)
		WriteString(rom, offset, _ioSupport, 16);
		offset += 16;

		// $1a0-$1a3: ROM start address (4 bytes, big-endian)
		WriteBigEndianLong(rom, offset, _romStart);
		offset += 4;

		// $1a4-$1a7: ROM end address (4 bytes, big-endian)
		WriteBigEndianLong(rom, offset, _romEnd);
		offset += 4;

		// $1a8-$1ab: RAM start address (4 bytes, big-endian)
		WriteBigEndianLong(rom, offset, _ramStart);
		offset += 4;

		// $1ac-$1af: RAM end address (4 bytes, big-endian)
		WriteBigEndianLong(rom, offset, _ramEnd);
		offset += 4;

		// $1b0-$1bb: SRAM info (12 bytes)
		if (_hasSram) {
			// "RA" + type byte + space
			rom[offset] = (byte)'R';
			rom[offset + 1] = (byte)'A';

			// SRAM type: F8 for 8-bit wide on even addresses
			byte sramType = 0xa0;
			if (_sramOddBytes && _sramEvenBytes) sramType = 0xa0;  // 16-bit
			else if (_sramEvenBytes) sramType = 0xb0;              // Even bytes only
			else if (_sramOddBytes) sramType = 0xb8;               // Odd bytes only

			rom[offset + 2] = sramType;
			rom[offset + 3] = 0x20;  // Space

			// SRAM start address
			WriteBigEndianLong(rom, offset + 4, _sramStart);
			// SRAM end address
			WriteBigEndianLong(rom, offset + 8, _sramEnd);
		} else {
			// No SRAM: fill with spaces
			for (var i = 0; i < 12; i++) {
				rom[offset + i] = 0x20;
			}
		}
		offset += 12;

		// $1bc-$1c7: Modem support (12 bytes)
		WriteString(rom, offset, _modem, 12);
		offset += 12;

		// $1c8-$1ef: Memo (40 bytes)
		WriteString(rom, offset, _memo, 40);
		offset += 40;

		// $1f0-$1ff: Region codes (16 bytes)
		WriteString(rom, offset, _region, 16);
	}

	/// <summary>
	/// Calculates the ROM checksum.
	/// Checksum is the sum of all 16-bit words from $200 to end of ROM.
	/// </summary>
	private ushort CalculateChecksum(byte[] rom) {
		uint sum = 0;

		// Sum all 16-bit words starting at $200
		for (var i = 0x200; i < rom.Length - 1; i += 2) {
			var word = (rom[i] << 8) | rom[i + 1];
			sum += (uint)word;
		}

		return (ushort)(sum & 0xffff);
	}

	/// <summary>
	/// Writes a string to ROM, padding with spaces.
	/// </summary>
	private static void WriteString(byte[] rom, int offset, string text, int length) {
		var padded = PadString(text, length);
		for (var i = 0; i < length && offset + i < rom.Length; i++) {
			rom[offset + i] = (byte)padded[i];
		}
	}

	/// <summary>
	/// Writes a 16-bit value in big-endian format.
	/// </summary>
	private static void WriteBigEndianWord(byte[] rom, int offset, ushort value) {
		rom[offset] = (byte)((value >> 8) & 0xff);
		rom[offset + 1] = (byte)(value & 0xff);
	}

	/// <summary>
	/// Writes a 32-bit value in big-endian format.
	/// </summary>
	private static void WriteBigEndianLong(byte[] rom, int offset, uint value) {
		rom[offset] = (byte)((value >> 24) & 0xff);
		rom[offset + 1] = (byte)((value >> 16) & 0xff);
		rom[offset + 2] = (byte)((value >> 8) & 0xff);
		rom[offset + 3] = (byte)(value & 0xff);
	}

	/// <summary>
	/// Pads a string to the specified length with spaces.
	/// </summary>
	private static string PadString(string text, int length) {
		if (string.IsNullOrEmpty(text)) {
			return new string(' ', length);
		}

		if (text.Length >= length) {
			return text[..length];
		}

		return text.PadRight(length, ' ');
	}
}

