// ============================================================================
// WonderSwanRomBuilder.cs - WonderSwan ROM Image Builder
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================
// WonderSwan ROM format with header at end of ROM (like Game Boy).
// Supports both original WonderSwan and WonderSwan Color.
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builds WonderSwan ROM images with proper header structure.
/// </summary>
public static class WonderSwanRomBuilder {
	/// <summary>
	/// WonderSwan ROM header structure (10 bytes at end of ROM).
	/// </summary>
	public class WonderSwanHeader {
		/// <summary>Publisher ID (1 byte)</summary>
		public byte PublisherId { get; set; } = 0x01;

		/// <summary>Color mode: 0=mono, 1=color (1 byte)</summary>
		public byte ColorMode { get; set; } = 0x00;

		/// <summary>Game ID (1 byte)</summary>
		public byte GameId { get; set; } = 0x00;

		/// <summary>Game revision (1 byte)</summary>
		public byte Revision { get; set; } = 0x00;

		/// <summary>ROM size code (1 byte)</summary>
		public byte RomSize { get; set; } = 0x02;  // 4Mbit default

		/// <summary>Save type (1 byte): 0=none, 1=SRAM, 2=EEPROM</summary>
		public byte SaveType { get; set; } = 0x00;

		/// <summary>Additional flags (1 byte)</summary>
		public byte Flags { get; set; } = 0x00;

		/// <summary>RTC flag (1 byte): 0=none, 1=present</summary>
		public byte RtcFlag { get; set; } = 0x00;

		/// <summary>Checksum (2 bytes, little-endian)</summary>
		public ushort Checksum { get; set; } = 0x0000;
	}

	/// <summary>
	/// ROM size codes for WonderSwan.
	/// </summary>
	public static class RomSizeCodes {
		/// <summary>1 Megabit (128KB).</summary>
		public const byte Size1Mbit = 0x01;
		/// <summary>2 Megabit (256KB).</summary>
		public const byte Size2Mbit = 0x02;
		/// <summary>4 Megabit (512KB).</summary>
		public const byte Size4Mbit = 0x03;
		/// <summary>8 Megabit (1MB).</summary>
		public const byte Size8Mbit = 0x04;
		/// <summary>16 Megabit (2MB).</summary>
		public const byte Size16Mbit = 0x05;
		/// <summary>32 Megabit (4MB).</summary>
		public const byte Size32Mbit = 0x06;
		/// <summary>64 Megabit (8MB).</summary>
		public const byte Size64Mbit = 0x07;
		/// <summary>128 Megabit (16MB).</summary>
		public const byte Size128Mbit = 0x08;
	}

	/// <summary>
	/// Save type codes.
	/// </summary>
	public static class SaveTypes {
		/// <summary>No save functionality.</summary>
		public const byte None = 0x00;
		/// <summary>8KB SRAM.</summary>
		public const byte Sram8K = 0x01;
		/// <summary>32KB SRAM.</summary>
		public const byte Sram32K = 0x02;
		/// <summary>128KB SRAM.</summary>
		public const byte Sram128K = 0x03;
		/// <summary>256KB SRAM.</summary>
		public const byte Sram256K = 0x04;
		/// <summary>512KB SRAM.</summary>
		public const byte Sram512K = 0x05;
		/// <summary>1KB EEPROM.</summary>
		public const byte Eeprom1K = 0x10;
		/// <summary>16KB EEPROM.</summary>
		public const byte Eeprom16K = 0x20;
		/// <summary>8KB EEPROM.</summary>
		public const byte Eeprom8K = 0x50;
	}

	/// <summary>
	/// Standard ROM sizes in bytes.
	/// </summary>
	private static readonly int[] ValidRomSizes = {
		128 * 1024,    // 1Mbit
		256 * 1024,    // 2Mbit
		512 * 1024,    // 4Mbit
		1024 * 1024,   // 8Mbit
		2048 * 1024,   // 16Mbit
		4096 * 1024,   // 32Mbit
		8192 * 1024,   // 64Mbit
		16384 * 1024,  // 128Mbit
	};

	/// <summary>
	/// Builds a complete WonderSwan ROM with header.
	/// </summary>
	/// <param name="code">The assembled code bytes.</param>
	/// <param name="header">Optional header configuration.</param>
	/// <returns>Complete ROM image with header and padding.</returns>
	public static byte[] BuildRom(byte[] code, WonderSwanHeader? header = null) {
		header ??= new WonderSwanHeader();

		// Determine minimum ROM size (must be power of 2, minimum 128KB)
		int targetSize = GetMinimumRomSize(code.Length + 10);  // +10 for header
		header.RomSize = GetRomSizeCode(targetSize);

		// Create ROM buffer
		var rom = new byte[targetSize];

		// Fill with $ff (unprogrammed flash)
		Array.Fill(rom, (byte)0xff);

		// Copy code to beginning of ROM
		// WonderSwan maps ROM to end of address space, code starts at beginning
		Array.Copy(code, 0, rom, 0, Math.Min(code.Length, targetSize - 10));

		// Write header at end of ROM (last 10 bytes)
		int headerOffset = targetSize - 10;
		WriteHeader(rom, headerOffset, header);

		// Calculate and write checksum
		ushort checksum = CalculateChecksum(rom);
		rom[targetSize - 2] = (byte)(checksum & 0xff);
		rom[targetSize - 1] = (byte)((checksum >> 8) & 0xff);

		return rom;
	}

	/// <summary>
	/// Writes the header to the ROM buffer.
	/// </summary>
	private static void WriteHeader(byte[] rom, int offset, WonderSwanHeader header) {
		rom[offset + 0] = header.PublisherId;
		rom[offset + 1] = header.ColorMode;
		rom[offset + 2] = header.GameId;
		rom[offset + 3] = header.Revision;
		rom[offset + 4] = header.RomSize;
		rom[offset + 5] = header.SaveType;
		rom[offset + 6] = header.Flags;
		rom[offset + 7] = header.RtcFlag;
		// Checksum at offset + 8 and + 9 (written separately)
	}

	/// <summary>
	/// Gets the minimum valid ROM size for the given code length.
	/// </summary>
	private static int GetMinimumRomSize(int codeLength) {
		foreach (var size in ValidRomSizes) {
			if (codeLength <= size) {
				return size;
			}
		}

		// If code is too large, return maximum size
		return ValidRomSizes[^1];
	}

	/// <summary>
	/// Gets the ROM size code for a given byte size.
	/// </summary>
	private static byte GetRomSizeCode(int byteSize) {
		return byteSize switch {
			128 * 1024 => RomSizeCodes.Size1Mbit,
			256 * 1024 => RomSizeCodes.Size2Mbit,
			512 * 1024 => RomSizeCodes.Size4Mbit,
			1024 * 1024 => RomSizeCodes.Size8Mbit,
			2048 * 1024 => RomSizeCodes.Size16Mbit,
			4096 * 1024 => RomSizeCodes.Size32Mbit,
			8192 * 1024 => RomSizeCodes.Size64Mbit,
			_ => RomSizeCodes.Size128Mbit,
		};
	}

	/// <summary>
	/// Calculates the WonderSwan checksum.
	/// Sum of all bytes except the last 2 (checksum bytes).
	/// </summary>
	private static ushort CalculateChecksum(byte[] rom) {
		ushort sum = 0;
		for (int i = 0; i < rom.Length - 2; i++) {
			sum += rom[i];
		}

		return sum;
	}

	/// <summary>
	/// Parses a header from metadata directives.
	/// </summary>
	/// <param name="metadata">Dictionary of directive values.</param>
	/// <returns>Configured header.</returns>
	public static WonderSwanHeader ParseHeader(Dictionary<string, object> metadata) {
		var header = new WonderSwanHeader();

		if (metadata.TryGetValue("publisher", out var publisher)) {
			header.PublisherId = Convert.ToByte(publisher);
		}

		if (metadata.TryGetValue("color", out var color)) {
			header.ColorMode = Convert.ToByte(color);
		}

		if (metadata.TryGetValue("gameid", out var gameId)) {
			header.GameId = Convert.ToByte(gameId);
		}

		if (metadata.TryGetValue("revision", out var revision)) {
			header.Revision = Convert.ToByte(revision);
		}

		if (metadata.TryGetValue("savetype", out var saveType)) {
			header.SaveType = Convert.ToByte(saveType);
		}

		if (metadata.TryGetValue("rtc", out var rtc)) {
			header.RtcFlag = Convert.ToByte(rtc);
		}

		if (metadata.TryGetValue("flags", out var flags)) {
			header.Flags = Convert.ToByte(flags);
		}

		return header;
	}

	/// <summary>
	/// Gets WonderSwan memory map information.
	/// </summary>
	/// <returns>Description of the memory map.</returns>
	public static string GetMemoryMapDescription() {
		return """
			WonderSwan Memory Map (V30MZ):
			===============================
			$0000-$3FFF  Internal RAM (16KB, banked)
			$4000-$BFFF  Cartridge SRAM (if present)
			$C000-$FFFF  ROM Bank (16KB windows)

			I/O Ports:
			$00-$FF      Hardware registers

			ROM is mapped at the end of the 1MB address space.
			The last segment (seg:$FFF0) contains the reset vector.
			""";
	}
}
