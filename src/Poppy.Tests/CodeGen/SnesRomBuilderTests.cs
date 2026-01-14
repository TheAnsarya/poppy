// ============================================================================
// SnesRomBuilderTests.cs - Tests for SNES ROM Layout Builder
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for SNES ROM layout and checksum calculation.
/// </summary>
public sealed class SnesRomBuilderTests {
	/// <summary>
	/// Creates a minimal 64-byte header for testing.
	/// </summary>
	private static byte[] CreateTestHeader(string title = "TEST") {
		var header = new byte[64];

		// Title at offset 0 (21 bytes, space-padded)
		var titleBytes = System.Text.Encoding.ASCII.GetBytes(title);
		for (var i = 0; i < Math.Min(titleBytes.Length, 21); i++) {
			header[i] = titleBytes[i];
		}
		for (var i = titleBytes.Length; i < 21; i++) {
			header[i] = 0x20; // Space padding
		}

		// Map mode at offset $25 (0x20 = LoROM)
		header[0x25] = 0x20;

		// ROM type at offset $26
		header[0x26] = 0x00;

		// ROM size at offset $27 (2^n KB)
		header[0x27] = 0x08; // 256KB

		// RAM size at offset $28
		header[0x28] = 0x00;

		// Country at offset $29
		header[0x29] = 0x01; // USA

		// Dev ID at offset $2a
		header[0x2a] = 0x01;

		// Version at offset $2b
		header[0x2b] = 0x00;

		// Checksum complement at $2c-$2d (will be calculated)
		header[0x2c] = 0x00;
		header[0x2d] = 0x00;

		// Checksum at $2e-$2f (will be calculated)
		header[0x2e] = 0x00;
		header[0x2f] = 0x00;

		return header;
	}

	[Fact]
	public void Constructor_ThrowsForNullHeader() {
		Assert.Throws<ArgumentException>(() => new SnesRomBuilder(SnesMapMode.LoRom, null!));
	}

	[Fact]
	public void Constructor_ThrowsForWrongSizeHeader() {
		var shortHeader = new byte[32];
		Assert.Throws<ArgumentException>(() => new SnesRomBuilder(SnesMapMode.LoRom, shortHeader));
	}

	[Fact]
	public void Build_LoRom_HeaderAt7fc0() {
		var header = CreateTestHeader("LOROM TEST");
		var builder = new SnesRomBuilder(SnesMapMode.LoRom, header);

		var rom = builder.Build();

		// LoROM header should be at $7fc0
		Assert.True(rom.Length >= 0x8000, "ROM should be at least 32KB");

		// Check title is at header location
		Assert.Equal((byte)'L', rom[0x7fc0]);
		Assert.Equal((byte)'O', rom[0x7fc1]);
		Assert.Equal((byte)'R', rom[0x7fc2]);
		Assert.Equal((byte)'O', rom[0x7fc3]);
		Assert.Equal((byte)'M', rom[0x7fc4]);
	}

	[Fact]
	public void Build_HiRom_HeaderAtFfc0() {
		var header = CreateTestHeader("HIROM TEST");
		var builder = new SnesRomBuilder(SnesMapMode.HiRom, header);

		var rom = builder.Build();

		// HiROM header should be at $ffc0
		Assert.True(rom.Length >= 0x10000, "ROM should be at least 64KB");

		// Check title is at header location
		Assert.Equal((byte)'H', rom[0xffc0]);
		Assert.Equal((byte)'I', rom[0xffc1]);
		Assert.Equal((byte)'R', rom[0xffc2]);
		Assert.Equal((byte)'O', rom[0xffc3]);
		Assert.Equal((byte)'M', rom[0xffc4]);
	}

	[Fact]
	public void Build_LoRom_MinimumSize32KB() {
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.LoRom, header);

		var rom = builder.Build();

		Assert.Equal(0x8000, rom.Length); // 32KB minimum
	}

	[Fact]
	public void Build_HiRom_MinimumSize64KB() {
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.HiRom, header);

		var rom = builder.Build();

		Assert.Equal(0x10000, rom.Length); // 64KB minimum
	}

	[Fact]
	public void Build_RoundsToPowerOf2() {
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.HiRom, header);

		// Add segment that requires more than 64KB but less than 128KB
		var bigSegment = new byte[0x11000]; // 68KB of data
		builder.AddSegment(0xc00000, bigSegment); // HiROM bank $c0

		var rom = builder.Build();

		Assert.Equal(0x20000, rom.Length); // Rounded to 128KB
	}

	[Fact]
	public void Build_ChecksumIsValid() {
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.LoRom, header);

		var rom = builder.Build();

		// Read checksum and complement from header location
		var complement = (ushort)(rom[0x7fc0 + 0x2c] | (rom[0x7fc0 + 0x2d] << 8));
		var checksum = (ushort)(rom[0x7fc0 + 0x2e] | (rom[0x7fc0 + 0x2f] << 8));

		// Checksum + complement should equal $ffff
		Assert.Equal(0xffff, (ushort)(checksum + complement));
	}

	[Fact]
	public void Build_ChecksumMatchesActualSum() {
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.LoRom, header);

		// Add some test data
		builder.AddSegment(0x008000, [0xea, 0xea, 0xea, 0xea]); // NOP NOP NOP NOP

		var rom = builder.Build();

		// Calculate sum manually
		uint sum = 0;
		for (var i = 0; i < rom.Length; i++) {
			// Skip checksum bytes
			if (i < 0x7fc0 + 0x2c || i > 0x7fc0 + 0x2f) {
				sum += rom[i];
			}
		}

		var expectedChecksum = (ushort)(sum & 0xffff);
		var actualChecksum = (ushort)(rom[0x7fc0 + 0x2e] | (rom[0x7fc0 + 0x2f] << 8));

		Assert.Equal(expectedChecksum, actualChecksum);
	}

	[Fact]
	public void AddSegment_LoRom_CorrectPlacement() {
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.LoRom, header);

		// Add code at LoROM address $808000 (bank $80, offset $8000)
		// This should map to ROM offset $0000
		builder.AddSegment(0x808000, [0xa9, 0x42]); // LDA #$42

		var rom = builder.Build();

		Assert.Equal(0xa9, rom[0x0000]);
		Assert.Equal(0x42, rom[0x0001]);
	}

	[Fact]
	public void AddSegment_HiRom_CorrectPlacement() {
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.HiRom, header);

		// Add code at HiROM address $c00000 (bank $c0, offset $0000)
		// This should map to ROM offset $0000
		builder.AddSegment(0xc00000, [0xa9, 0x42]); // LDA #$42

		var rom = builder.Build();

		Assert.Equal(0xa9, rom[0x0000]);
		Assert.Equal(0x42, rom[0x0001]);
	}

	[Fact]
	public void AddSegment_MultipleSegments() {
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.LoRom, header);

		// Add segments at different locations
		builder.AddSegment(0x008000, [0xa9, 0x01]); // ROM offset $0000
		builder.AddSegment(0x008100, [0xa9, 0x02]); // ROM offset $0100
		builder.AddSegment(0x018000, [0xa9, 0x03]); // ROM offset $8000 (bank 1)

		var rom = builder.Build();

		Assert.Equal(0xa9, rom[0x0000]);
		Assert.Equal(0x01, rom[0x0001]);
		Assert.Equal(0xa9, rom[0x0100]);
		Assert.Equal(0x02, rom[0x0101]);
		Assert.Equal(0xa9, rom[0x8000]);
		Assert.Equal(0x03, rom[0x8001]);
	}

	[Fact]
	public void AddSegment_NullOrEmptyIgnored() {
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.LoRom, header);

		// These should not throw
		builder.AddSegment(0x008000, null!);
		builder.AddSegment(0x008000, []);

		var rom = builder.Build();
		Assert.NotNull(rom);
	}

	[Fact]
	public void Build_HeaderDoesNotOverwriteCode() {
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.LoRom, header);

		// Add code before header location
		builder.AddSegment(0x008000, new byte[0x7fc0]); // Fill up to header

		var rom = builder.Build();

		// Header should still be at correct location
		Assert.Equal((byte)'T', rom[0x7fc0]);
		Assert.Equal((byte)'E', rom[0x7fc1]);
		Assert.Equal((byte)'S', rom[0x7fc2]);
		Assert.Equal((byte)'T', rom[0x7fc3]);
	}

	[Fact]
	public void Build_LoRom_Bank00Address() {
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.LoRom, header);

		// Bank $00 address $8000 should map to ROM offset $0000
		builder.AddSegment(0x008000, [0xea]); // NOP

		var rom = builder.Build();

		Assert.Equal(0xea, rom[0x0000]);
	}

	[Fact]
	public void Build_HiRom_Bank40Address() {
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.HiRom, header);

		// HiROM bank $40 offset $0000 should map to ROM offset $0000
		builder.AddSegment(0x400000, [0xea]); // NOP

		var rom = builder.Build();

		Assert.Equal(0xea, rom[0x0000]);
	}

	[Fact]
	public void Build_BankAddressNotation() {
		// Test that the ROM builder works with bank:address style addresses
		// (even though parsing is done elsewhere)
		var header = CreateTestHeader();
		var builder = new SnesRomBuilder(SnesMapMode.LoRom, header);

		// $00:8000 format = bank 0, address $8000
		long address = (0x00L << 16) | 0x8000;
		builder.AddSegment(address, [0xa9, 0xff]); // LDA #$ff

		var rom = builder.Build();

		Assert.Equal(0xa9, rom[0x0000]);
		Assert.Equal(0xff, rom[0x0001]);
	}
}
