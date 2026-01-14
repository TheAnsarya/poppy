// ============================================================================
// GbRomBuilder.cs - Game Boy ROM Layout and Checksum Builder
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builds Game Boy ROM images with proper header placement and checksums.
/// </summary>
/// <remarks>
/// Game Boy ROM layout:
/// - $0000-$00ff: Restart vectors (RST $00-$38) and interrupt vectors
/// - $0100-$014f: ROM header (80 bytes)
/// - $0150+: Code and data
///
/// The ROM must be a valid size (32KB, 64KB, 128KB, 256KB, etc.)
/// and have valid header and global checksums.
/// </remarks>
public sealed class GbRomBuilder {
	private readonly byte[] _header;
	private readonly List<RomSegment> _segments;

	/// <summary>
	/// Header offset in ROM.
	/// </summary>
	public const int HeaderOffset = 0x100;

	/// <summary>
	/// Header size in bytes.
	/// </summary>
	public const int HeaderSize = 80;

	/// <summary>
	/// Code entry point (start of user code after header).
	/// </summary>
	public const int CodeEntryPoint = 0x150;

	/// <summary>
	/// Creates a new Game Boy ROM builder.
	/// </summary>
	/// <param name="header">The 80-byte Game Boy header from GbHeaderBuilder.</param>
	public GbRomBuilder(byte[] header) {
		if (header == null || header.Length != HeaderSize) {
			throw new ArgumentException($"Header must be exactly {HeaderSize} bytes", nameof(header));
		}

		_header = (byte[])header.Clone();
		_segments = [];
	}

	/// <summary>
	/// Adds a segment of code/data to the ROM.
	/// </summary>
	/// <param name="address">The Game Boy address where this data appears.</param>
	/// <param name="data">The binary data.</param>
	public void AddSegment(int address, byte[] data) {
		if (data == null || data.Length == 0) return;

		_segments.Add(new RomSegment {
			Address = address,
			Data = (byte[])data.Clone()
		});
	}

	/// <summary>
	/// Builds the final ROM image with header at correct position.
	/// </summary>
	/// <returns>The complete ROM binary.</returns>
	public byte[] Build() {
		// Calculate required ROM size (must be valid GB ROM size)
		var requiredSize = CalculateRequiredSize();

		// Create ROM image filled with $00
		var rom = new byte[requiredSize];

		// Copy all segments to ROM
		foreach (var segment in _segments) {
			var romOffset = GbAddressToRomOffset(segment.Address);
			if (romOffset >= 0 && romOffset + segment.Data.Length <= rom.Length) {
				Array.Copy(segment.Data, 0, rom, romOffset, segment.Data.Length);
			}
		}

		// Copy header to $0100-$014f
		Array.Copy(_header, 0, rom, HeaderOffset, HeaderSize);

		// Recalculate header checksum (in case code was placed at header location)
		RecalculateHeaderChecksum(rom);

		// Calculate and insert global checksum
		InsertGlobalChecksum(rom);

		return rom;
	}

	/// <summary>
	/// Calculates the minimum ROM size needed.
	/// </summary>
	/// <remarks>
	/// Valid Game Boy ROM sizes: 32KB, 64KB, 128KB, 256KB, 512KB, 1MB, 2MB, 4MB, 8MB
	/// </remarks>
	private int CalculateRequiredSize() {
		// Find the highest address used
		var maxAddress = HeaderOffset + HeaderSize;

		foreach (var segment in _segments) {
			var endAddress = segment.Address + segment.Data.Length;
			if (endAddress > maxAddress) {
				maxAddress = endAddress;
			}
		}

		// Round up to valid GB ROM size (power of 2, minimum 32KB)
		var size = 0x8000; // 32KB minimum
		while (size < maxAddress) {
			size *= 2;
		}

		return size;
	}

	/// <summary>
	/// Converts a Game Boy address to a ROM file offset.
	/// </summary>
	/// <remarks>
	/// Game Boy addressing:
	/// - $0000-$3fff: ROM Bank 0 (always mapped)
	/// - $4000-$7fff: Switchable ROM bank (bank 1+ via MBC)
	///
	/// For single-bank ROMs (32KB), addresses map directly to ROM offsets.
	/// For multi-bank ROMs, the bank number must be tracked separately.
	/// </remarks>
	private static int GbAddressToRomOffset(int address) {
		// For ROM addresses ($0000-$7fff in first bank)
		if (address >= 0 && address < 0x8000) {
			return address;
		}

		// Addresses above $8000 are RAM/VRAM/etc., not ROM
		// But we allow high addresses to support banked ROM access
		// Format: (bank << 14) | (address & 0x3fff) for addresses $4000+
		// Or simply treat as direct offset for banked ROM data

		// For now, return the address as-is for banked access
		// The caller is responsible for providing correct banked addresses
		return address;
	}

	/// <summary>
	/// Recalculates the header checksum at $014d.
	/// </summary>
	/// <remarks>
	/// Header checksum = 0 - (sum of bytes from $0134 to $014c) - 1
	/// Or equivalently: checksum = 0; for each byte: checksum = checksum - byte - 1
	/// </remarks>
	private static void RecalculateHeaderChecksum(byte[] rom) {
		int checksum = 0;
		for (var i = 0x134; i <= 0x14c; i++) {
			checksum = checksum - rom[i] - 1;
		}
		rom[0x14d] = (byte)(checksum & 0xff);
	}

	/// <summary>
	/// Calculates and inserts the global checksum at $014e-$014f.
	/// </summary>
	/// <remarks>
	/// Global checksum = sum of all bytes in ROM except the checksum bytes themselves.
	/// Stored as big-endian (high byte at $014e, low byte at $014f).
	/// </remarks>
	private static void InsertGlobalChecksum(byte[] rom) {
		// Clear checksum bytes first
		rom[0x14e] = 0;
		rom[0x14f] = 0;

		// Calculate sum of all bytes
		uint sum = 0;
		foreach (var b in rom) {
			sum += b;
		}

		var checksum = (ushort)(sum & 0xffff);

		// Store as big-endian (different from SNES!)
		rom[0x14e] = (byte)((checksum >> 8) & 0xff);  // High byte
		rom[0x14f] = (byte)(checksum & 0xff);         // Low byte
	}

	/// <summary>
	/// Represents a segment of ROM data.
	/// </summary>
	private sealed class RomSegment {
		public int Address { get; init; }
		public byte[] Data { get; init; } = [];
	}
}
