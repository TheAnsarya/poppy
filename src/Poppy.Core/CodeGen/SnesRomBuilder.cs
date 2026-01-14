// ============================================================================
// SnesRomBuilder.cs - SNES ROM Layout and Header Placement
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Builds SNES ROM images with proper header placement and checksums.
/// </summary>
/// <remarks>
/// SNES ROMs require the internal header at specific locations:
/// - LoROM: $007fc0-$007fff (header at 32KB boundary - 64 bytes)
/// - HiROM: $00ffc0-$00ffff (header at 64KB boundary - 64 bytes)
/// - ExHiROM: $40ffc0-$40ffff (header in bank $40)
///
/// The ROM must be padded to a power of 2 size and have valid checksums.
/// </remarks>
public sealed class SnesRomBuilder {
	private readonly SnesMapMode _mapMode;
	private readonly byte[] _header;
	private readonly List<RomSegment> _segments;

	/// <summary>
	/// Header size in bytes.
	/// </summary>
	public const int HeaderSize = 64;

	/// <summary>
	/// Creates a new SNES ROM builder.
	/// </summary>
	/// <param name="mapMode">The ROM mapping mode.</param>
	/// <param name="header">The 64-byte SNES header.</param>
	public SnesRomBuilder(SnesMapMode mapMode, byte[] header) {
		if (header == null || header.Length != HeaderSize) {
			throw new ArgumentException($"Header must be exactly {HeaderSize} bytes", nameof(header));
		}

		_mapMode = mapMode;
		_header = (byte[])header.Clone();
		_segments = [];
	}

	/// <summary>
	/// Adds a segment of code/data to the ROM.
	/// </summary>
	/// <param name="address">The SNES address where this data appears.</param>
	/// <param name="data">The binary data.</param>
	public void AddSegment(long address, byte[] data) {
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
		// Calculate the header offset based on mapping mode
		var headerOffset = GetHeaderOffset();

		// Calculate required ROM size (must be power of 2)
		var requiredSize = CalculateRequiredSize(headerOffset);

		// Create ROM image
		var rom = new byte[requiredSize];

		// Fill with $00 (some assemblers use $ff)
		// Array.Fill(rom, (byte)0x00); // Already initialized to 0

		// Copy all segments to ROM
		foreach (var segment in _segments) {
			var romOffset = SnesAddressToRomOffset(segment.Address);
			if (romOffset >= 0 && romOffset + segment.Data.Length <= rom.Length) {
				Array.Copy(segment.Data, 0, rom, romOffset, segment.Data.Length);
			}
		}

		// Copy header to correct position
		Array.Copy(_header, 0, rom, headerOffset, HeaderSize);

		// Calculate and insert checksum
		InsertChecksum(rom, headerOffset);

		return rom;
	}

	/// <summary>
	/// Gets the ROM offset where the header should be placed.
	/// </summary>
	private int GetHeaderOffset() {
		return _mapMode switch {
			SnesMapMode.LoRom => 0x7fc0,     // $007fc0 - just before first 32KB boundary
			SnesMapMode.HiRom => 0xffc0,     // $00ffc0 - just before first 64KB boundary
			SnesMapMode.ExHiRom => 0x40ffc0, // $40ffc0 - in bank $40
			_ => 0x7fc0
		};
	}

	/// <summary>
	/// Calculates the minimum ROM size needed (power of 2).
	/// </summary>
	private int CalculateRequiredSize(int headerOffset) {
		// Find the highest address used
		var maxAddress = headerOffset + HeaderSize;

		foreach (var segment in _segments) {
			var romOffset = SnesAddressToRomOffset(segment.Address);
			if (romOffset >= 0) {
				var endOffset = romOffset + segment.Data.Length;
				if (endOffset > maxAddress) {
					maxAddress = endOffset;
				}
			}
		}

		// Round up to next power of 2
		var size = 1;
		while (size < maxAddress) {
			size <<= 1;
		}

		// Minimum ROM sizes
		var minSize = _mapMode switch {
			SnesMapMode.LoRom => 0x8000,    // 32KB minimum for LoROM
			SnesMapMode.HiRom => 0x10000,   // 64KB minimum for HiROM
			SnesMapMode.ExHiRom => 0x10000, // 64KB minimum
			_ => 0x8000
		};

		return Math.Max(size, minSize);
	}

	/// <summary>
	/// Converts a SNES address to a ROM file offset.
	/// </summary>
	private int SnesAddressToRomOffset(long address) {
		var bank = (int)((address >> 16) & 0xff);
		var offset = (int)(address & 0xffff);

		return _mapMode switch {
			SnesMapMode.LoRom => ConvertLoRomAddress(bank, offset),
			SnesMapMode.HiRom => ConvertHiRomAddress(bank, offset),
			SnesMapMode.ExHiRom => ConvertExHiRomAddress(bank, offset),
			_ => ConvertLoRomAddress(bank, offset)
		};
	}

	/// <summary>
	/// Converts LoROM address to ROM offset.
	/// </summary>
	/// <remarks>
	/// LoROM: ROM appears at $8000-$ffff in banks $00-$7d, $80-$ff
	/// Each bank contains 32KB of ROM data.
	/// </remarks>
	private static int ConvertLoRomAddress(int bank, int offset) {
		// LoROM only maps $8000-$ffff
		if (offset < 0x8000) {
			// Could be RAM or hardware - not ROM
			// For code placement, treat as if mirrored
			return -1;
		}

		// Handle bank mirroring
		if (bank >= 0x80) {
			bank -= 0x80;
		}

		// Calculate ROM offset
		// Bank 0: $8000-$ffff -> ROM $0000-$7fff
		// Bank 1: $8000-$ffff -> ROM $8000-$ffff
		// etc.
		return (bank * 0x8000) + (offset - 0x8000);
	}

	/// <summary>
	/// Converts HiROM address to ROM offset.
	/// </summary>
	/// <remarks>
	/// HiROM: ROM appears at $0000-$ffff in banks $c0-$ff (and $40-$7d)
	/// Each bank contains 64KB of ROM data.
	/// </remarks>
	private static int ConvertHiRomAddress(int bank, int offset) {
		// HiROM maps full 64KB banks
		// Banks $c0-$ff map to ROM
		if (bank >= 0xc0) {
			return ((bank - 0xc0) * 0x10000) + offset;
		}

		// Banks $40-$7d also map to ROM (mirror)
		if (bank >= 0x40 && bank < 0x7e) {
			return ((bank - 0x40) * 0x10000) + offset;
		}

		// Banks $00-$3f only map $8000-$ffff to ROM
		if (bank < 0x40 && offset >= 0x8000) {
			return (bank * 0x10000) + offset;
		}

		return -1;
	}

	/// <summary>
	/// Converts ExHiROM address to ROM offset.
	/// </summary>
	private static int ConvertExHiRomAddress(int bank, int offset) {
		// ExHiROM extends HiROM with additional banks
		// Similar to HiROM but with extended addressing
		return ConvertHiRomAddress(bank, offset);
	}

	/// <summary>
	/// Calculates and inserts the ROM checksum.
	/// </summary>
	/// <remarks>
	/// Checksum is at header offset $2c-$2f:
	/// - $2c-$2d: Complement checksum (inverted)
	/// - $2e-$2f: Checksum
	/// checksum + complement = $ffff
	/// </remarks>
	private static void InsertChecksum(byte[] rom, int headerOffset) {
		// Clear checksum bytes first (they shouldn't affect calculation)
		rom[headerOffset + 0x2c] = 0;
		rom[headerOffset + 0x2d] = 0;
		rom[headerOffset + 0x2e] = 0;
		rom[headerOffset + 0x2f] = 0;

		// Calculate checksum (sum of all bytes, 16-bit wraparound)
		uint sum = 0;
		foreach (var b in rom) {
			sum += b;
		}

		var checksum = (ushort)(sum & 0xffff);
		var complement = (ushort)(checksum ^ 0xffff);

		// Insert complement (little-endian)
		rom[headerOffset + 0x2c] = (byte)(complement & 0xff);
		rom[headerOffset + 0x2d] = (byte)((complement >> 8) & 0xff);

		// Insert checksum (little-endian)
		rom[headerOffset + 0x2e] = (byte)(checksum & 0xff);
		rom[headerOffset + 0x2f] = (byte)((checksum >> 8) & 0xff);
	}

	/// <summary>
	/// Represents a segment of ROM data.
	/// </summary>
	private sealed class RomSegment {
		public long Address { get; init; }
		public byte[] Data { get; init; } = [];
	}
}
