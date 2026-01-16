// ============================================================================
// MasterSystemRomBuilder.cs - Sega Master System/Game Gear ROM Builder
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================
// The Sega Master System ROM header is located at $7ff0 for ROMs >= 32KB,
// $3ff0 for 16KB ROMs, or $1ff0 for 8KB ROMs. The header contains the
// "TMR SEGA" signature, checksum, product code, version, and region.
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builds Sega Master System and Game Gear ROM images with proper header format.
/// </summary>
public sealed class MasterSystemRomBuilder {
	/// <summary>
	/// Standard ROM sizes for Master System cartridges.
	/// </summary>
	public static class RomSizes {
		/// <summary>8 KB ROM</summary>
		public const int Size8K = 8 * 1024;
		/// <summary>16 KB ROM</summary>
		public const int Size16K = 16 * 1024;
		/// <summary>32 KB ROM</summary>
		public const int Size32K = 32 * 1024;
		/// <summary>64 KB ROM</summary>
		public const int Size64K = 64 * 1024;
		/// <summary>128 KB ROM</summary>
		public const int Size128K = 128 * 1024;
		/// <summary>256 KB ROM</summary>
		public const int Size256K = 256 * 1024;
		/// <summary>512 KB ROM (maximum)</summary>
		public const int Size512K = 512 * 1024;
	}

	/// <summary>
	/// Region codes for Master System cartridges.
	/// </summary>
	public enum RegionCode {
		/// <summary>SMS Japan</summary>
		SmsJapan = 3,
		/// <summary>SMS Export (USA/Europe)</summary>
		SmsExport = 4,
		/// <summary>Game Gear Japan</summary>
		GgJapan = 5,
		/// <summary>Game Gear Export</summary>
		GgExport = 6,
		/// <summary>Game Gear International</summary>
		GgInternational = 7,
	}

	/// <summary>
	/// ROM size encoding for header.
	/// </summary>
	public enum RomSizeCode {
		/// <summary>8 KB ROM (Sega Card)</summary>
		Size8K = 0xa,
		/// <summary>16 KB ROM (Sega Card)</summary>
		Size16K = 0xb,
		/// <summary>32 KB ROM</summary>
		Size32K = 0xc,
		/// <summary>48 KB ROM (non-standard)</summary>
		Size48K = 0xd,
		/// <summary>64 KB ROM</summary>
		Size64K = 0xe,
		/// <summary>128 KB ROM</summary>
		Size128K = 0xf,
		/// <summary>256 KB ROM</summary>
		Size256K = 0x0,
		/// <summary>512 KB ROM</summary>
		Size512K = 0x1,
		/// <summary>1 MB ROM (non-standard)</summary>
		Size1M = 0x2,
	}

	private readonly int _romSize;
	private readonly bool _isGameGear;
	private readonly List<(int Address, byte[] Data)> _segments;

	// Header fields
	private int _productCode = 0;          // 5-digit BCD product code
	private int _version = 0;               // Version number (0-15)
	private RegionCode _region = RegionCode.SmsExport;
	private RomSizeCode _sizeCode = RomSizeCode.Size32K;

	/// <summary>
	/// The TMR SEGA signature bytes.
	/// </summary>
	private static readonly byte[] TmrSega = [
		0x54, 0x4d, 0x52, 0x20,  // "TMR "
		0x53, 0x45, 0x47, 0x41   // "SEGA"
	];

	/// <summary>
	/// Creates a new Master System/Game Gear ROM builder.
	/// </summary>
	/// <param name="romSize">The target ROM size in bytes.</param>
	/// <param name="isGameGear">Whether this is a Game Gear ROM.</param>
	public MasterSystemRomBuilder(int romSize = RomSizes.Size32K, bool isGameGear = false) {
		_romSize = romSize;
		_isGameGear = isGameGear;
		_segments = [];

		// Set default region based on platform
		_region = isGameGear ? RegionCode.GgExport : RegionCode.SmsExport;

		// Auto-detect size code
		_sizeCode = GetSizeCode(romSize);
	}

	/// <summary>
	/// Sets the product code (5 digits, 0-99999).
	/// </summary>
	/// <param name="code">The product code.</param>
	/// <returns>This builder for chaining.</returns>
	public MasterSystemRomBuilder SetProductCode(int code) {
		_productCode = Math.Clamp(code, 0, 99999);
		return this;
	}

	/// <summary>
	/// Sets the version number (0-15).
	/// </summary>
	/// <param name="version">The version number.</param>
	/// <returns>This builder for chaining.</returns>
	public MasterSystemRomBuilder SetVersion(int version) {
		_version = Math.Clamp(version, 0, 15);
		return this;
	}

	/// <summary>
	/// Sets the region code.
	/// </summary>
	/// <param name="region">The region code.</param>
	/// <returns>This builder for chaining.</returns>
	public MasterSystemRomBuilder SetRegion(RegionCode region) {
		_region = region;
		return this;
	}

	/// <summary>
	/// Sets the ROM size code (usually auto-detected).
	/// </summary>
	/// <param name="sizeCode">The ROM size code.</param>
	/// <returns>This builder for chaining.</returns>
	public MasterSystemRomBuilder SetSizeCode(RomSizeCode sizeCode) {
		_sizeCode = sizeCode;
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

		// Calculate header offset based on ROM size
		var headerOffset = GetHeaderOffset();

		// Write header
		WriteHeader(rom, headerOffset);

		return rom;
	}

	/// <summary>
	/// Gets the header offset based on ROM size.
	/// </summary>
	private int GetHeaderOffset() {
		if (_romSize <= RomSizes.Size8K) return 0x1ff0;
		if (_romSize <= RomSizes.Size16K) return 0x3ff0;
		return 0x7ff0;  // 32KB and larger
	}

	/// <summary>
	/// Writes the ROM header at the specified offset.
	/// </summary>
	private void WriteHeader(byte[] rom, int offset) {
		// Check if header fits
		if (offset + 16 > rom.Length) return;

		// $7ff0-$7ff7: TMR SEGA signature (8 bytes)
		Array.Copy(TmrSega, 0, rom, offset, 8);

		// $7ff8-$7ff9: Reserved (2 bytes, usually $00 $00 or $ff $ff)
		rom[offset + 8] = 0x00;
		rom[offset + 9] = 0x00;

		// $7ffa-$7ffb: Checksum (2 bytes, little-endian)
		var checksum = CalculateChecksum(rom, offset);
		rom[offset + 10] = (byte)(checksum & 0xff);
		rom[offset + 11] = (byte)((checksum >> 8) & 0xff);

		// $7ffc-$7ffe: Product code (3 bytes, BCD)
		// Lower 4 digits in 2 bytes (BCD), upper digit + version in byte 3
		var bcd0 = (byte)((_productCode % 10) | ((_productCode / 10 % 10) << 4));
		var bcd1 = (byte)((_productCode / 100 % 10) | ((_productCode / 1000 % 10) << 4));
		var bcd2 = (byte)((_productCode / 10000 % 10) | (_version << 4));

		rom[offset + 12] = bcd0;
		rom[offset + 13] = bcd1;
		rom[offset + 14] = bcd2;

		// $7fff: Region and ROM size (1 byte)
		// Upper nibble = region, lower nibble = size
		rom[offset + 15] = (byte)((((int)_region) << 4) | ((int)_sizeCode & 0x0f));
	}

	/// <summary>
	/// Calculates the ROM checksum.
	/// Checksum is the sum of bytes from $0000 to header-1, excluding header.
	/// </summary>
	private ushort CalculateChecksum(byte[] rom, int headerOffset) {
		uint sum = 0;

		// Sum all bytes from start to just before header
		for (var i = 0; i < headerOffset; i++) {
			sum += rom[i];
		}

		// For ROMs larger than 32KB, also sum bytes after header
		// up to the 32KB mark (bank 0)
		// Additional banks are not included in checksum

		return (ushort)(sum & 0xffff);
	}

	/// <summary>
	/// Gets the appropriate size code for a given ROM size.
	/// </summary>
	private static RomSizeCode GetSizeCode(int romSize) {
		return romSize switch {
			<= RomSizes.Size8K => RomSizeCode.Size8K,
			<= RomSizes.Size16K => RomSizeCode.Size16K,
			<= RomSizes.Size32K => RomSizeCode.Size32K,
			<= 48 * 1024 => RomSizeCode.Size48K,
			<= RomSizes.Size64K => RomSizeCode.Size64K,
			<= RomSizes.Size128K => RomSizeCode.Size128K,
			<= RomSizes.Size256K => RomSizeCode.Size256K,
			<= RomSizes.Size512K => RomSizeCode.Size512K,
			_ => RomSizeCode.Size1M
		};
	}
}

