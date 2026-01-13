// ============================================================================
// GbHeaderBuilder.cs - Game Boy ROM Header Builder
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builds Game Boy ROM headers.
/// </summary>
/// <remarks>
/// The Game Boy ROM header is located at $0100-$014f in the ROM and contains:
/// - Entry point (4 bytes at $0100-$0103)
/// - Nintendo logo (48 bytes at $0104-$0133)
/// - Title (16 bytes at $0134-$0143, older ROMs: 15 bytes + manufacturer code)
/// - CGB flag (1 byte at $0143)
/// - New licensee code (2 bytes at $0144-$0145)
/// - SGB flag (1 byte at $0146)
/// - Cartridge type (1 byte at $0147)
/// - ROM size (1 byte at $0148)
/// - RAM size (1 byte at $0149)
/// - Destination code (1 byte at $014a)
/// - Old licensee code (1 byte at $014b)
/// - Mask ROM version (1 byte at $014c)
/// - Header checksum (1 byte at $014d)
/// - Global checksum (2 bytes at $014e-$014f)
/// </remarks>
public sealed class GbHeaderBuilder {
	private string _title = "";
	private GbCgbMode _cgbMode = GbCgbMode.DmgOnly;
	private bool _sgbEnabled = false;
	private GbCartridgeType _cartridgeType = GbCartridgeType.RomOnly;
	private int _romSizeKb = 32;
	private int _ramSizeKb = 0;
	private GbRegion _region = GbRegion.Japan;
	private byte _version = 0;

	/// <summary>
	/// Sets the ROM title (max 16 characters).
	/// </summary>
	public GbHeaderBuilder SetTitle(string title) {
		_title = title;
		return this;
	}

	/// <summary>
	/// Sets the CGB (Color Game Boy) mode.
	/// </summary>
	public GbHeaderBuilder SetCgbMode(GbCgbMode mode) {
		_cgbMode = mode;
		return this;
	}

	/// <summary>
	/// Enables Super Game Boy features.
	/// </summary>
	public GbHeaderBuilder SetSgbEnabled(bool enabled) {
		_sgbEnabled = enabled;
		return this;
	}

	/// <summary>
	/// Sets the cartridge type (MBC controller).
	/// </summary>
	public GbHeaderBuilder SetCartridgeType(GbCartridgeType type) {
		_cartridgeType = type;
		return this;
	}

	/// <summary>
	/// Sets the ROM size in kilobytes.
	/// </summary>
	public GbHeaderBuilder SetRomSize(int sizeKb) {
		_romSizeKb = sizeKb;
		return this;
	}

	/// <summary>
	/// Sets the RAM size in kilobytes.
	/// </summary>
	public GbHeaderBuilder SetRamSize(int sizeKb) {
		_ramSizeKb = sizeKb;
		return this;
	}

	/// <summary>
	/// Sets the region code.
	/// </summary>
	public GbHeaderBuilder SetRegion(GbRegion region) {
		_region = region;
		return this;
	}

	/// <summary>
	/// Sets the ROM version number.
	/// </summary>
	public GbHeaderBuilder SetVersion(byte version) {
		_version = version;
		return this;
	}

	/// <summary>
	/// Builds the complete 80-byte Game Boy header ($0100-$014f).
	/// </summary>
	public byte[] Build() {
		var header = new byte[80];

		// Entry point at $0100-$0103 (4 bytes): nop; jp $0150
		header[0] = 0x00;  // nop
		header[1] = 0xc3;  // jp
		header[2] = 0x50;  // $0150 (low byte)
		header[3] = 0x01;  // $0150 (high byte)

		// Nintendo logo at $0104-$0133 (48 bytes)
		var logo = GetNintendoLogo();
		Array.Copy(logo, 0, header, 4, 48);

		// Title at $0134-$0143 (16 bytes) or $0134-$0142 (15 bytes) for newer ROMs
		var titleBytes = System.Text.Encoding.ASCII.GetBytes(_title.Length > 16 ? _title.Substring(0, 16) : _title);
		Array.Copy(titleBytes, 0, header, 52, Math.Min(titleBytes.Length, 16));

		// CGB flag at $0143
		header[67] = (byte)_cgbMode;

		// New licensee code at $0144-$0145 (use "00" for unlicensed)
		header[68] = 0x30;  // '0'
		header[69] = 0x30;  // '0'

		// SGB flag at $0146
		header[70] = _sgbEnabled ? (byte)0x03 : (byte)0x00;

		// Cartridge type at $0147
		header[71] = (byte)_cartridgeType;

		// ROM size at $0148
		header[72] = CalculateRomSizeCode(_romSizeKb);

		// RAM size at $0149
		header[73] = CalculateRamSizeCode(_ramSizeKb);

		// Destination code at $014a
		header[74] = (byte)_region;

		// Old licensee code at $014b (0x33 = use new licensee code)
		header[75] = 0x33;

		// Mask ROM version at $014c
		header[76] = _version;

		// Header checksum at $014d
		header[77] = CalculateHeaderChecksum(header);

		// Global checksum at $014e-$014f (calculated by emulator, set to 0 for now)
		header[78] = 0x00;
		header[79] = 0x00;

		return header;
	}

	/// <summary>
	/// Returns the Nintendo logo bytes (48 bytes).
	/// </summary>
	private static byte[] GetNintendoLogo() {
		return new byte[] {
			0xce, 0xed, 0x66, 0x66, 0xcc, 0x0d, 0x00, 0x0b,
			0x03, 0x73, 0x00, 0x83, 0x00, 0x0c, 0x00, 0x0d,
			0x00, 0x08, 0x11, 0x1f, 0x88, 0x89, 0x00, 0x0e,
			0xdc, 0xcc, 0x6e, 0xe6, 0xdd, 0xdd, 0xd9, 0x99,
			0xbb, 0xbb, 0x67, 0x63, 0x6e, 0x0e, 0xec, 0xcc,
			0xdd, 0xdc, 0x99, 0x9f, 0xbb, 0xb9, 0x33, 0x3e
		};
	}

	/// <summary>
	/// Calculates the ROM size code from KB size.
	/// </summary>
	private static byte CalculateRomSizeCode(int sizeKb) {
		// ROM size codes: 0=$00 (32KB), 1=$01 (64KB), 2=$02 (128KB), etc.
		// Formula: code = log2(sizeKb / 32)
		if (sizeKb < 32) return 0;

		int code = 0;
		int size = sizeKb / 32;
		while (size > 1) {
			size >>= 1;
			code++;
		}

		return (byte)code;
	}

	/// <summary>
	/// Calculates the RAM size code from KB size.
	/// </summary>
	private static byte CalculateRamSizeCode(int sizeKb) {
		return sizeKb switch {
			0 => 0x00,     // No RAM
			2 => 0x01,     // Unused
			8 => 0x02,     // 8KB (1 bank)
			32 => 0x03,    // 32KB (4 banks)
			128 => 0x04,   // 128KB (16 banks)
			64 => 0x05,    // 64KB (8 banks)
			_ => 0x00
		};
	}

	/// <summary>
	/// Calculates the header checksum (sum of $0134-$014c XOR $ff).
	/// </summary>
	private static byte CalculateHeaderChecksum(byte[] header) {
		int checksum = 0;
		for (int i = 52; i <= 76; i++) {  // $0134-$014c
			checksum = checksum - header[i] - 1;
		}
		return (byte)(checksum & 0xff);
	}
}

/// <summary>
/// Game Boy CGB (Color) mode flags.
/// </summary>
public enum GbCgbMode : byte {
	/// <summary>DMG (original Game Boy) only.</summary>
	DmgOnly = 0x00,
	/// <summary>CGB compatible (works on both DMG and CGB).</summary>
	CgbCompatible = 0x80,
	/// <summary>CGB only (requires Color Game Boy).</summary>
	CgbOnly = 0xc0
}

/// <summary>
/// Game Boy cartridge types (MBC controllers).
/// </summary>
public enum GbCartridgeType : byte {
	/// <summary>ROM only (no MBC).</summary>
	RomOnly = 0x00,
	/// <summary>MBC1.</summary>
	Mbc1 = 0x01,
	/// <summary>MBC1 + RAM.</summary>
	Mbc1Ram = 0x02,
	/// <summary>MBC1 + RAM + Battery.</summary>
	Mbc1RamBattery = 0x03,
	/// <summary>MBC2.</summary>
	Mbc2 = 0x05,
	/// <summary>MBC2 + Battery.</summary>
	Mbc2Battery = 0x06,
	/// <summary>ROM + RAM.</summary>
	RomRam = 0x08,
	/// <summary>ROM + RAM + Battery.</summary>
	RomRamBattery = 0x09,
	/// <summary>MMM01.</summary>
	Mmm01 = 0x0b,
	/// <summary>MMM01 + RAM.</summary>
	Mmm01Ram = 0x0c,
	/// <summary>MMM01 + RAM + Battery.</summary>
	Mmm01RamBattery = 0x0d,
	/// <summary>MBC3 + Timer + Battery.</summary>
	Mbc3TimerBattery = 0x0f,
	/// <summary>MBC3 + Timer + RAM + Battery.</summary>
	Mbc3TimerRamBattery = 0x10,
	/// <summary>MBC3.</summary>
	Mbc3 = 0x11,
	/// <summary>MBC3 + RAM.</summary>
	Mbc3Ram = 0x12,
	/// <summary>MBC3 + RAM + Battery.</summary>
	Mbc3RamBattery = 0x13,
	/// <summary>MBC5.</summary>
	Mbc5 = 0x19,
	/// <summary>MBC5 + RAM.</summary>
	Mbc5Ram = 0x1a,
	/// <summary>MBC5 + RAM + Battery.</summary>
	Mbc5RamBattery = 0x1b,
	/// <summary>MBC5 + Rumble.</summary>
	Mbc5Rumble = 0x1c,
	/// <summary>MBC5 + Rumble + RAM.</summary>
	Mbc5RumbleRam = 0x1d,
	/// <summary>MBC5 + Rumble + RAM + Battery.</summary>
	Mbc5RumbleRamBattery = 0x1e
}

/// <summary>
/// Game Boy region codes.
/// </summary>
public enum GbRegion : byte {
	/// <summary>Japan.</summary>
	Japan = 0x00,
	/// <summary>International (non-Japan).</summary>
	International = 0x01
}
