// ============================================================================
// GameBoyHeaderBuilder.cs - Game Boy ROM Header Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builds Game Boy ROM headers with cartridge metadata.
/// </summary>
public sealed class GameBoyHeaderBuilder {
	// Header locations (relative to $0100)
	private const int EntryPointOffset = 0x00;    // $0100-$0103: Entry point (NOP + JP)
	private const int LogoOffset = 0x04;          // $0104-$0133: Nintendo logo
	private const int TitleOffset = 0x34;         // $0134-$0143: Title (15/16 chars)
	private const int ManufacturerOffset = 0x3f;  // $013F-$0142: Manufacturer code (CGB)
	private const int CgbFlagOffset = 0x43;       // $0143: CGB flag
	private const int LicenseeOffset = 0x44;      // $0144-$0145: New licensee code
	private const int SgbFlagOffset = 0x46;       // $0146: SGB flag
	private const int CartridgeTypeOffset = 0x47; // $0147: Cartridge type (MBC)
	private const int RomSizeOffset = 0x48;       // $0148: ROM size
	private const int RamSizeOffset = 0x49;       // $0149: RAM size
	private const int DestinationOffset = 0x4a;   // $014A: Destination code
	private const int OldLicenseeOffset = 0x4b;   // $014B: Old licensee code
	private const int VersionOffset = 0x4c;       // $014C: ROM version
	private const int HeaderChecksumOffset = 0x4d; // $014D: Header checksum
	private const int GlobalChecksumOffset = 0x4e; // $014E-$014F: Global checksum

	private const int HeaderSize = 0x50; // Size of header area ($0100-$014F)

	// Nintendo logo (required for boot ROM validation)
	private static readonly byte[] NintendoLogo = [
		0xce, 0xed, 0x66, 0x66, 0xcc, 0x0d, 0x00, 0x0b,
		0x03, 0x73, 0x00, 0x83, 0x00, 0x0c, 0x00, 0x0d,
		0x00, 0x08, 0x11, 0x1f, 0x88, 0x89, 0x00, 0x0e,
		0xdc, 0xcc, 0x6e, 0xe6, 0xdd, 0xdd, 0xd9, 0x99,
		0xbb, 0xbb, 0x67, 0x63, 0x6e, 0x0e, 0xec, 0xcc,
		0xdd, 0xdc, 0x99, 0x9f, 0xbb, 0xb9, 0x33, 0x3e
	];

	private string _title = "";
	private string _manufacturerCode = "";
	private CgbFlag _cgbFlag = CgbFlag.DmgOnly;
	private bool _sgbFlag = false;
	private CartridgeType _cartridgeType = CartridgeType.RomOnly;
	private int _romSizeCode = 0;
	private int _ramSizeCode = 0;
	private bool _japanese = true;
	private string _licenseeCode = "00";
	private int _romVersion = 0;
	private int _entryPoint = 0x0150; // Default entry point after header

	/// <summary>
	/// Sets the game title (up to 15 characters for CGB, 16 for DMG).
	/// </summary>
	public GameBoyHeaderBuilder SetTitle(string title) {
		_title = title;
		return this;
	}

	/// <summary>
	/// Sets the manufacturer code (4 characters, CGB only).
	/// </summary>
	public GameBoyHeaderBuilder SetManufacturerCode(string code) {
		_manufacturerCode = code;
		return this;
	}

	/// <summary>
	/// Sets the CGB compatibility flag.
	/// </summary>
	public GameBoyHeaderBuilder SetCgbFlag(CgbFlag flag) {
		_cgbFlag = flag;
		return this;
	}

	/// <summary>
	/// Sets the SGB enhancement flag.
	/// </summary>
	public GameBoyHeaderBuilder SetSgbFlag(bool enabled) {
		_sgbFlag = enabled;
		return this;
	}

	/// <summary>
	/// Sets the cartridge type (MBC type).
	/// </summary>
	public GameBoyHeaderBuilder SetCartridgeType(CartridgeType type) {
		_cartridgeType = type;
		return this;
	}

	/// <summary>
	/// Sets the ROM size code.
	/// </summary>
	public GameBoyHeaderBuilder SetRomSize(int sizeCode) {
		_romSizeCode = sizeCode;
		return this;
	}

	/// <summary>
	/// Sets the RAM size code.
	/// </summary>
	public GameBoyHeaderBuilder SetRamSize(int sizeCode) {
		_ramSizeCode = sizeCode;
		return this;
	}

	/// <summary>
	/// Sets the destination (Japanese or overseas).
	/// </summary>
	public GameBoyHeaderBuilder SetJapanese(bool japanese) {
		_japanese = japanese;
		return this;
	}

	/// <summary>
	/// Sets the licensee code (2 characters).
	/// </summary>
	public GameBoyHeaderBuilder SetLicenseeCode(string code) {
		_licenseeCode = code;
		return this;
	}

	/// <summary>
	/// Sets the ROM version number.
	/// </summary>
	public GameBoyHeaderBuilder SetRomVersion(int version) {
		_romVersion = version;
		return this;
	}

	/// <summary>
	/// Sets the entry point address for the JP instruction.
	/// </summary>
	public GameBoyHeaderBuilder SetEntryPoint(int address) {
		_entryPoint = address;
		return this;
	}

	/// <summary>
	/// Builds the Game Boy ROM header (80 bytes from $0100-$014F).
	/// </summary>
	/// <returns>The header bytes.</returns>
	public byte[] Build() {
		var header = new byte[HeaderSize];

		// Entry point: NOP followed by JP $XXXX
		header[EntryPointOffset] = 0x00; // NOP
		header[EntryPointOffset + 1] = 0xc3; // JP
		header[EntryPointOffset + 2] = (byte)(_entryPoint & 0xff);
		header[EntryPointOffset + 3] = (byte)((_entryPoint >> 8) & 0xff);

		// Nintendo logo
		Array.Copy(NintendoLogo, 0, header, LogoOffset, NintendoLogo.Length);

		// Title (up to 15 characters for CGB, 16 for DMG)
		var maxTitleLength = _cgbFlag != CgbFlag.DmgOnly ? 15 : 16;
		var titleBytes = System.Text.Encoding.ASCII.GetBytes(_title.Length > maxTitleLength
			? _title[..maxTitleLength]
			: _title);
		Array.Copy(titleBytes, 0, header, TitleOffset, Math.Min(titleBytes.Length, maxTitleLength));

		// Manufacturer code (CGB only, bytes $013F-$0142)
		if (_cgbFlag != CgbFlag.DmgOnly && _manufacturerCode.Length > 0) {
			var mfrBytes = System.Text.Encoding.ASCII.GetBytes(_manufacturerCode.Length > 4
				? _manufacturerCode[..4]
				: _manufacturerCode);
			Array.Copy(mfrBytes, 0, header, ManufacturerOffset, Math.Min(mfrBytes.Length, 4));
		}

		// CGB flag
		header[CgbFlagOffset] = (byte)_cgbFlag;

		// New licensee code
		if (_licenseeCode.Length >= 2) {
			header[LicenseeOffset] = (byte)_licenseeCode[0];
			header[LicenseeOffset + 1] = (byte)_licenseeCode[1];
		}

		// SGB flag
		header[SgbFlagOffset] = _sgbFlag ? (byte)0x03 : (byte)0x00;

		// Cartridge type
		header[CartridgeTypeOffset] = (byte)_cartridgeType;

		// ROM size
		header[RomSizeOffset] = (byte)_romSizeCode;

		// RAM size
		header[RamSizeOffset] = (byte)_ramSizeCode;

		// Destination code
		header[DestinationOffset] = _japanese ? (byte)0x00 : (byte)0x01;

		// Old licensee code (use $33 for new licensee)
		header[OldLicenseeOffset] = 0x33;

		// ROM version
		header[VersionOffset] = (byte)_romVersion;

		// Header checksum (calculated over $0134-$014C)
		header[HeaderChecksumOffset] = CalculateHeaderChecksum(header);

		// Global checksum (set to 0, calculated later over entire ROM)
		header[GlobalChecksumOffset] = 0x00;
		header[GlobalChecksumOffset + 1] = 0x00;

		return header;
	}

	/// <summary>
	/// Calculates the header checksum.
	/// </summary>
	private static byte CalculateHeaderChecksum(byte[] header) {
		int checksum = 0;
		for (int i = TitleOffset; i < HeaderChecksumOffset; i++) {
			checksum = checksum - header[i] - 1;
		}
		return (byte)(checksum & 0xff);
	}

	/// <summary>
	/// Calculates the global checksum for a complete ROM.
	/// </summary>
	/// <param name="rom">The complete ROM data.</param>
	/// <returns>The 16-bit global checksum.</returns>
	public static ushort CalculateGlobalChecksum(byte[] rom) {
		int checksum = 0;
		for (int i = 0; i < rom.Length; i++) {
			// Skip the global checksum bytes at $014E-$014F
			if (i != 0x14e && i != 0x14f) {
				checksum += rom[i];
			}
		}
		return (ushort)(checksum & 0xffff);
	}

	/// <summary>
	/// Gets the ROM size in bytes from a size code.
	/// </summary>
	public static int GetRomSize(int sizeCode) {
		return sizeCode switch {
			0x00 => 32 * 1024,   // 32 KB (no banking)
			0x01 => 64 * 1024,   // 64 KB (4 banks)
			0x02 => 128 * 1024,  // 128 KB (8 banks)
			0x03 => 256 * 1024,  // 256 KB (16 banks)
			0x04 => 512 * 1024,  // 512 KB (32 banks)
			0x05 => 1024 * 1024, // 1 MB (64 banks)
			0x06 => 2048 * 1024, // 2 MB (128 banks)
			0x07 => 4096 * 1024, // 4 MB (256 banks)
			0x08 => 8192 * 1024, // 8 MB (512 banks)
			_ => 32 * 1024
		};
	}

	/// <summary>
	/// Gets the RAM size in bytes from a size code.
	/// </summary>
	public static int GetRamSize(int sizeCode) {
		return sizeCode switch {
			0x00 => 0,           // No RAM
			0x01 => 2 * 1024,    // 2 KB (unused)
			0x02 => 8 * 1024,    // 8 KB (1 bank)
			0x03 => 32 * 1024,   // 32 KB (4 banks)
			0x04 => 128 * 1024,  // 128 KB (16 banks)
			0x05 => 64 * 1024,   // 64 KB (8 banks)
			_ => 0
		};
	}
}

/// <summary>
/// CGB compatibility flags.
/// </summary>
public enum CgbFlag : byte {
	/// <summary>DMG only (no CGB features).</summary>
	DmgOnly = 0x00,
	/// <summary>CGB enhanced (works on both DMG and CGB).</summary>
	CgbEnhanced = 0x80,
	/// <summary>CGB only (does not work on DMG).</summary>
	CgbOnly = 0xc0
}

/// <summary>
/// Game Boy cartridge types (MBC).
/// </summary>
public enum CartridgeType : byte {
	/// <summary>ROM only, no mapper.</summary>
	RomOnly = 0x00,
	/// <summary>MBC1 mapper.</summary>
	Mbc1 = 0x01,
	/// <summary>MBC1 with RAM.</summary>
	Mbc1Ram = 0x02,
	/// <summary>MBC1 with RAM and battery backup.</summary>
	Mbc1RamBattery = 0x03,
	/// <summary>MBC2 mapper.</summary>
	Mbc2 = 0x05,
	/// <summary>MBC2 with battery backup.</summary>
	Mbc2Battery = 0x06,
	/// <summary>ROM with RAM.</summary>
	RomRam = 0x08,
	/// <summary>ROM with RAM and battery backup.</summary>
	RomRamBattery = 0x09,
	/// <summary>MMM01 mapper.</summary>
	Mmm01 = 0x0b,
	/// <summary>MMM01 with RAM.</summary>
	Mmm01Ram = 0x0c,
	/// <summary>MMM01 with RAM and battery backup.</summary>
	Mmm01RamBattery = 0x0d,
	/// <summary>MBC3 with timer and battery backup.</summary>
	Mbc3TimerBattery = 0x0f,
	/// <summary>MBC3 with timer, RAM, and battery backup.</summary>
	Mbc3TimerRamBattery = 0x10,
	/// <summary>MBC3 mapper.</summary>
	Mbc3 = 0x11,
	/// <summary>MBC3 with RAM.</summary>
	Mbc3Ram = 0x12,
	/// <summary>MBC3 with RAM and battery backup.</summary>
	Mbc3RamBattery = 0x13,
	/// <summary>MBC5 mapper.</summary>
	Mbc5 = 0x19,
	/// <summary>MBC5 with RAM.</summary>
	Mbc5Ram = 0x1a,
	/// <summary>MBC5 with RAM and battery backup.</summary>
	Mbc5RamBattery = 0x1b,
	/// <summary>MBC5 with rumble.</summary>
	Mbc5Rumble = 0x1c,
	/// <summary>MBC5 with rumble and RAM.</summary>
	Mbc5RumbleRam = 0x1d,
	/// <summary>MBC5 with rumble, RAM, and battery backup.</summary>
	Mbc5RumbleRamBattery = 0x1e,
	/// <summary>MBC6 mapper.</summary>
	Mbc6 = 0x20,
	/// <summary>MBC7 with sensor, rumble, RAM, and battery backup.</summary>
	Mbc7SensorRumbleRamBattery = 0x22,
	/// <summary>Pocket Camera.</summary>
	PocketCamera = 0xfc,
	/// <summary>Bandai TAMA5.</summary>
	BandaiTama5 = 0xfd,
	/// <summary>HuC3 mapper.</summary>
	Huc3 = 0xfe,
	/// <summary>HuC1 with RAM and battery backup.</summary>
	Huc1RamBattery = 0xff
}
