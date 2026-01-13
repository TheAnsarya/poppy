// ============================================================================
// SnesHeaderBuilder.cs - SNES ROM Header Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builds SNES ROM headers (internal header at $ffc0-$ffff for LoROM or $7fc0-$7fff for HiROM).
/// </summary>
public sealed class SnesHeaderBuilder {
	// Internal header size (64 bytes)
	private const int HeaderSize = 64;

	// Header field offsets (relative to start of header)
	private const int TitleOffset = 0x00;          // 21 bytes
	private const int MapModeOffset = 0x15;        // 1 byte
	private const int CartridgeTypeOffset = 0x16;  // 1 byte
	private const int RomSizeOffset = 0x17;        // 1 byte
	private const int RamSizeOffset = 0x18;        // 1 byte
	private const int RegionOffset = 0x19;         // 1 byte
	private const int DeveloperIdOffset = 0x1a;    // 1 byte
	private const int VersionOffset = 0x1b;        // 1 byte
	private const int ChecksumComplementOffset = 0x1c; // 2 bytes
	private const int ChecksumOffset = 0x1e;       // 2 bytes
	// Native vectors: $ffe0-$ffef
	// Emulation vectors: $fff0-$ffff

	// SMC copier header size
	private const int SmcHeaderSize = 512;

	private string _title = "";
	private SnesMapMode _mapMode = SnesMapMode.LoRom;
	private SnesCartridgeType _cartridgeType = SnesCartridgeType.RomOnly;
	private int _romSizeKb = 256; // KB
	private int _ramSizeKb = 0;   // KB
	private SnesRegion _region = SnesRegion.Japan;
	private byte _developerId = 0x00;
	private byte _version = 0x00;
	private bool _fastRom = false;

	// Interrupt vectors (native mode, 65816)
	private ushort _nativeNmi = 0xffff;
	private ushort _nativeReset = 0xffff;
	private ushort _nativeIrq = 0xffff;
	private ushort _nativeCop = 0xffff;
	private ushort _nativeBrk = 0xffff;
	private ushort _nativeAbort = 0xffff;

	// Interrupt vectors (emulation mode, 6502)
	private ushort _emulationNmi = 0xffff;
	private ushort _emulationReset = 0x8000;
	private ushort _emulationIrq = 0xffff;
	private ushort _emulationCop = 0xffff;
	private ushort _emulationAbort = 0xffff;

	/// <summary>
	/// Sets the game title (up to 21 characters).
	/// </summary>
	public SnesHeaderBuilder SetTitle(string title) {
		_title = title;
		return this;
	}

	/// <summary>
	/// Sets the ROM mapping mode.
	/// </summary>
	public SnesHeaderBuilder SetMapMode(SnesMapMode mode) {
		_mapMode = mode;
		return this;
	}

	/// <summary>
	/// Sets whether to use fast ROM (3.58 MHz vs 2.68 MHz).
	/// </summary>
	public SnesHeaderBuilder SetFastRom(bool fast) {
		_fastRom = fast;
		return this;
	}

	/// <summary>
	/// Sets the cartridge type (ROM only, ROM+RAM, ROM+RAM+Battery, etc.).
	/// </summary>
	public SnesHeaderBuilder SetCartridgeType(SnesCartridgeType type) {
		_cartridgeType = type;
		return this;
	}

	/// <summary>
	/// Sets the ROM size in kilobytes (must be power of 2: 256, 512, 1024, etc.).
	/// </summary>
	public SnesHeaderBuilder SetRomSize(int sizeKb) {
		_romSizeKb = sizeKb;
		return this;
	}

	/// <summary>
	/// Sets the RAM size in kilobytes (0, 2, 8, 32, etc.).
	/// </summary>
	public SnesHeaderBuilder SetRamSize(int sizeKb) {
		_ramSizeKb = sizeKb;
		return this;
	}

	/// <summary>
	/// Sets the region/destination.
	/// </summary>
	public SnesHeaderBuilder SetRegion(SnesRegion region) {
		_region = region;
		return this;
	}

	/// <summary>
	/// Sets the developer ID.
	/// </summary>
	public SnesHeaderBuilder SetDeveloperId(byte id) {
		_developerId = id;
		return this;
	}

	/// <summary>
	/// Sets the ROM version.
	/// </summary>
	public SnesHeaderBuilder SetVersion(byte version) {
		_version = version;
		return this;
	}

	/// <summary>
	/// Sets the native mode NMI vector.
	/// </summary>
	public SnesHeaderBuilder SetNativeNmi(ushort address) {
		_nativeNmi = address;
		return this;
	}

	/// <summary>
	/// Sets the native mode RESET vector (unused, but part of header).
	/// </summary>
	public SnesHeaderBuilder SetNativeReset(ushort address) {
		_nativeReset = address;
		return this;
	}

	/// <summary>
	/// Sets the native mode IRQ vector.
	/// </summary>
	public SnesHeaderBuilder SetNativeIrq(ushort address) {
		_nativeIrq = address;
		return this;
	}

	/// <summary>
	/// Sets the native mode COP vector.
	/// </summary>
	public SnesHeaderBuilder SetNativeCop(ushort address) {
		_nativeCop = address;
		return this;
	}

	/// <summary>
	/// Sets the native mode BRK vector.
	/// </summary>
	public SnesHeaderBuilder SetNativeBrk(ushort address) {
		_nativeBrk = address;
		return this;
	}

	/// <summary>
	/// Sets the native mode ABORT vector.
	/// </summary>
	public SnesHeaderBuilder SetNativeAbort(ushort address) {
		_nativeAbort = address;
		return this;
	}

	/// <summary>
	/// Sets the emulation mode NMI vector.
	/// </summary>
	public SnesHeaderBuilder SetEmulationNmi(ushort address) {
		_emulationNmi = address;
		return this;
	}

	/// <summary>
	/// Sets the emulation mode RESET vector.
	/// </summary>
	public SnesHeaderBuilder SetEmulationReset(ushort address) {
		_emulationReset = address;
		return this;
	}

	/// <summary>
	/// Sets the emulation mode IRQ/BRK vector.
	/// </summary>
	public SnesHeaderBuilder SetEmulationIrq(ushort address) {
		_emulationIrq = address;
		return this;
	}

	/// <summary>
	/// Sets the emulation mode COP vector.
	/// </summary>
	public SnesHeaderBuilder SetEmulationCop(ushort address) {
		_emulationCop = address;
		return this;
	}

	/// <summary>
	/// Sets the emulation mode ABORT vector.
	/// </summary>
	public SnesHeaderBuilder SetEmulationAbort(ushort address) {
		_emulationAbort = address;
		return this;
	}

	/// <summary>
	/// Builds the SNES internal header (64 bytes).
	/// </summary>
	/// <returns>The header bytes.</returns>
	public byte[] Build() {
		var header = new byte[HeaderSize];

		// Title (21 bytes, padded with spaces)
		var titleBytes = System.Text.Encoding.ASCII.GetBytes(_title.Length > 21
			? _title[..21]
			: _title.PadRight(21));
		Array.Copy(titleBytes, 0, header, TitleOffset, 21);

		// Map mode byte
		byte mapByte = (byte)_mapMode;
		if (_fastRom) {
			mapByte |= 0x10; // Set fast ROM bit
		}
		header[MapModeOffset] = mapByte;

		// Cartridge type
		header[CartridgeTypeOffset] = (byte)_cartridgeType;

		// ROM size (log2(size in KB))
		header[RomSizeOffset] = GetRomSizeCode(_romSizeKb);

		// RAM size (log2(size in KB))
		header[RamSizeOffset] = GetRamSizeCode(_ramSizeKb);

		// Region
		header[RegionOffset] = (byte)_region;

		// Developer ID
		header[DeveloperIdOffset] = _developerId;

		// Version
		header[VersionOffset] = _version;

		// Checksum complement and checksum (will be calculated on full ROM)
		// For now, use placeholder values
		header[ChecksumComplementOffset] = 0xff;
		header[ChecksumComplementOffset + 1] = 0xff;
		header[ChecksumOffset] = 0x00;
		header[ChecksumOffset + 1] = 0x00;

		// Native interrupt vectors ($ffe0-$ffef)
		WriteWord(header, 0x20, 0xffff);        // Unused ($ffe0)
		WriteWord(header, 0x22, 0xffff);        // Unused ($ffe2)
		WriteWord(header, 0x24, _nativeCop);    // COP ($ffe4)
		WriteWord(header, 0x26, _nativeBrk);    // BRK ($ffe6)
		WriteWord(header, 0x28, _nativeAbort);  // ABORT ($ffe8)
		WriteWord(header, 0x2a, _nativeNmi);    // NMI ($ffea)
		WriteWord(header, 0x2c, _nativeReset);  // RESET ($ffec) - unused in native mode
		WriteWord(header, 0x2e, _nativeIrq);    // IRQ ($ffee)

		// Emulation interrupt vectors ($fff0-$ffff)
		WriteWord(header, 0x30, 0xffff);          // Unused ($fff0)
		WriteWord(header, 0x32, 0xffff);          // Unused ($fff2)
		WriteWord(header, 0x34, _emulationCop);   // COP ($fff4)
		WriteWord(header, 0x36, 0xffff);          // Unused ($fff6)
		WriteWord(header, 0x38, _emulationAbort); // ABORT ($fff8)
		WriteWord(header, 0x3a, _emulationNmi);   // NMI ($fffa)
		WriteWord(header, 0x3c, _emulationReset); // RESET ($fffc)
		WriteWord(header, 0x3e, _emulationIrq);   // IRQ/BRK ($fffe)

		return header;
	}

	/// <summary>
	/// Gets the header offset in the ROM based on map mode.
	/// </summary>
	public int GetHeaderOffset() {
		return _mapMode switch {
			SnesMapMode.LoRom or SnesMapMode.LoRomSA1 => 0x7fc0,
			SnesMapMode.HiRom or SnesMapMode.HiRomSDD1 or SnesMapMode.HiRomSA1 => 0xffc0,
			SnesMapMode.ExHiRom => 0x40ffc0,
			_ => 0x7fc0
		};
	}

	/// <summary>
	/// Calculates the SNES checksum for a ROM.
	/// </summary>
	/// <param name="rom">The complete ROM data.</param>
	/// <returns>The 16-bit checksum.</returns>
	public static ushort CalculateChecksum(byte[] rom) {
		long sum = 0;
		int size = rom.Length;

		// Handle non-power-of-2 sizes by mirroring
		int mirroredSize = GetMirroredSize(size);

		for (int i = 0; i < mirroredSize; i++) {
			sum += rom[i % size];
		}

		return (ushort)(sum & 0xffff);
	}

	/// <summary>
	/// Updates the checksum fields in a ROM.
	/// </summary>
	/// <param name="rom">The ROM data to update.</param>
	/// <param name="headerOffset">The offset of the internal header.</param>
	public static void UpdateChecksum(byte[] rom, int headerOffset) {
		// Temporarily zero out checksum fields
		rom[headerOffset + ChecksumComplementOffset] = 0;
		rom[headerOffset + ChecksumComplementOffset + 1] = 0;
		rom[headerOffset + ChecksumOffset] = 0;
		rom[headerOffset + ChecksumOffset + 1] = 0;

		// Calculate checksum
		ushort checksum = CalculateChecksum(rom);
		ushort complement = (ushort)(checksum ^ 0xffff);

		// Write checksum and complement
		rom[headerOffset + ChecksumComplementOffset] = (byte)(complement & 0xff);
		rom[headerOffset + ChecksumComplementOffset + 1] = (byte)((complement >> 8) & 0xff);
		rom[headerOffset + ChecksumOffset] = (byte)(checksum & 0xff);
		rom[headerOffset + ChecksumOffset + 1] = (byte)((checksum >> 8) & 0xff);
	}

	/// <summary>
	/// Creates an SMC copier header (512 bytes).
	/// </summary>
	/// <param name="romSizeKb">ROM size in kilobytes.</param>
	/// <returns>The SMC header bytes.</returns>
	public static byte[] CreateSmcHeader(int romSizeKb) {
		var header = new byte[SmcHeaderSize];

		// SMC header format:
		// Bytes 0-1: ROM size in 8KB units (low/high)
		int units = romSizeKb / 8;
		header[0] = (byte)(units & 0xff);
		header[1] = (byte)((units >> 8) & 0xff);

		// Byte 2: Mode flags (usually 0 for SNES)
		header[2] = 0x00;

		// Rest is usually zeros or can contain loader code
		return header;
	}

	/// <summary>
	/// Gets the ROM size code for the header.
	/// </summary>
	private static byte GetRomSizeCode(int sizeKb) {
		// ROM size = 1 << (code + 10) bytes = 1 << code KB
		int code = 0;
		int size = 1;
		while (size < sizeKb && code < 15) {
			size <<= 1;
			code++;
		}
		return (byte)code;
	}

	/// <summary>
	/// Gets the RAM size code for the header.
	/// </summary>
	private static byte GetRamSizeCode(int sizeKb) {
		if (sizeKb == 0) return 0;
		// RAM size = 1 << (code + 10) bytes = 1 << code KB
		int code = 0;
		int size = 1;
		while (size < sizeKb && code < 15) {
			size <<= 1;
			code++;
		}
		return (byte)code;
	}

	/// <summary>
	/// Gets the mirrored ROM size for checksum calculation.
	/// </summary>
	private static int GetMirroredSize(int size) {
		// SNES checksums are calculated over power-of-2 sizes
		// with mirroring for smaller ROMs
		if (size <= 0) return 0;

		int pow2 = 1;
		while (pow2 < size) {
			pow2 <<= 1;
		}

		// If size is already power of 2, return it
		if (pow2 == size) return size;

		// Otherwise, mirror to fill to next power of 2
		return pow2;
	}

	/// <summary>
	/// Writes a 16-bit word in little-endian format.
	/// </summary>
	private static void WriteWord(byte[] data, int offset, ushort value) {
		data[offset] = (byte)(value & 0xff);
		data[offset + 1] = (byte)((value >> 8) & 0xff);
	}
}

/// <summary>
/// SNES ROM mapping modes.
/// </summary>
public enum SnesMapMode : byte {
	/// <summary>LoROM (mode $20).</summary>
	LoRom = 0x20,
	/// <summary>HiROM (mode $21).</summary>
	HiRom = 0x21,
	/// <summary>LoROM + S-DD1 (mode $22).</summary>
	LoRomSDD1 = 0x22,
	/// <summary>LoROM + SA-1 (mode $23).</summary>
	LoRomSA1 = 0x23,
	/// <summary>ExHiROM (mode $25).</summary>
	ExHiRom = 0x25,
	/// <summary>HiROM + S-DD1 (mode $32).</summary>
	HiRomSDD1 = 0x32,
	/// <summary>HiROM + SA-1 (mode $35).</summary>
	HiRomSA1 = 0x35
}

/// <summary>
/// SNES cartridge types.
/// </summary>
public enum SnesCartridgeType : byte {
	/// <summary>ROM only.</summary>
	RomOnly = 0x00,
	/// <summary>ROM + RAM.</summary>
	RomRam = 0x01,
	/// <summary>ROM + RAM + Battery.</summary>
	RomRamBattery = 0x02,
	/// <summary>ROM + DSP.</summary>
	RomDsp = 0x03,
	/// <summary>ROM + DSP + RAM.</summary>
	RomDspRam = 0x04,
	/// <summary>ROM + DSP + RAM + Battery.</summary>
	RomDspRamBattery = 0x05,
	/// <summary>ROM + Super FX.</summary>
	RomSuperFx = 0x13,
	/// <summary>ROM + Super FX + RAM.</summary>
	RomSuperFxRam = 0x14,
	/// <summary>ROM + Super FX + RAM + Battery.</summary>
	RomSuperFxRamBattery = 0x15,
	/// <summary>ROM + OBC1.</summary>
	RomObc1 = 0x23,
	/// <summary>ROM + OBC1 + RAM.</summary>
	RomObc1Ram = 0x24,
	/// <summary>ROM + OBC1 + RAM + Battery.</summary>
	RomObc1RamBattery = 0x25,
	/// <summary>ROM + SA-1.</summary>
	RomSa1 = 0x33,
	/// <summary>ROM + SA-1 + RAM.</summary>
	RomSa1Ram = 0x34,
	/// <summary>ROM + SA-1 + RAM + Battery.</summary>
	RomSa1RamBattery = 0x35
}

/// <summary>
/// SNES region codes.
/// </summary>
public enum SnesRegion : byte {
	/// <summary>Japan.</summary>
	Japan = 0x00,
	/// <summary>North America.</summary>
	NorthAmerica = 0x01,
	/// <summary>Europe.</summary>
	Europe = 0x02,
	/// <summary>Sweden/Scandinavia.</summary>
	Sweden = 0x03,
	/// <summary>Finland.</summary>
	Finland = 0x04,
	/// <summary>Denmark.</summary>
	Denmark = 0x05,
	/// <summary>France.</summary>
	France = 0x06,
	/// <summary>Netherlands.</summary>
	Netherlands = 0x07,
	/// <summary>Spain.</summary>
	Spain = 0x08,
	/// <summary>Germany.</summary>
	Germany = 0x09,
	/// <summary>Italy.</summary>
	Italy = 0x0a,
	/// <summary>China.</summary>
	China = 0x0b,
	/// <summary>Indonesia.</summary>
	Indonesia = 0x0c,
	/// <summary>South Korea.</summary>
	SouthKorea = 0x0d,
	/// <summary>International.</summary>
	International = 0x0e,
	/// <summary>Canada.</summary>
	Canada = 0x0f,
	/// <summary>Brazil.</summary>
	Brazil = 0x10,
	/// <summary>Australia.</summary>
	Australia = 0x11
}
