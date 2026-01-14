// ============================================================================
// GbRomBuilderTests.cs - Tests for Game Boy ROM Layout Builder
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for Game Boy ROM layout and checksum calculation.
/// </summary>
public sealed class GbRomBuilderTests {
	/// <summary>
	/// Creates a minimal 80-byte header for testing.
	/// </summary>
	private static byte[] CreateTestHeader(string title = "TEST") {
		var builder = new GbHeaderBuilder()
			.SetTitle(title)
			.SetCgbMode(GbCgbMode.DmgOnly)
			.SetCartridgeType(GbCartridgeType.RomOnly)
			.SetRomSize(32)
			.SetRamSize(0)
			.SetRegion(GbRegion.Japan);

		return builder.Build();
	}

	[Fact]
	public void Constructor_ThrowsForNullHeader() {
		Assert.Throws<ArgumentException>(() => new GbRomBuilder(null!));
	}

	[Fact]
	public void Constructor_ThrowsForWrongSizeHeader() {
		var shortHeader = new byte[32];
		Assert.Throws<ArgumentException>(() => new GbRomBuilder(shortHeader));
	}

	[Fact]
	public void Build_HeaderAtCorrectOffset() {
		var header = CreateTestHeader("GBTEST");
		var builder = new GbRomBuilder(header);

		var rom = builder.Build();

		// Header should be at $0100-$014f
		// Entry point: nop; jp $0150
		Assert.Equal(0x00, rom[0x100]);  // nop
		Assert.Equal(0xc3, rom[0x101]);  // jp
		Assert.Equal(0x50, rom[0x102]);  // $0150 low
		Assert.Equal(0x01, rom[0x103]);  // $0150 high

		// Nintendo logo starts at $0104
		Assert.Equal(0xce, rom[0x104]);  // First byte of Nintendo logo
		Assert.Equal(0xed, rom[0x105]);  // Second byte

		// Title at $0134
		Assert.Equal((byte)'G', rom[0x134]);
		Assert.Equal((byte)'B', rom[0x135]);
		Assert.Equal((byte)'T', rom[0x136]);
		Assert.Equal((byte)'E', rom[0x137]);
		Assert.Equal((byte)'S', rom[0x138]);
		Assert.Equal((byte)'T', rom[0x139]);
	}

	[Fact]
	public void Build_MinimumSize32KB() {
		var header = CreateTestHeader();
		var builder = new GbRomBuilder(header);

		var rom = builder.Build();

		Assert.Equal(0x8000, rom.Length); // 32KB minimum
	}

	[Fact]
	public void Build_RoundsToPowerOf2() {
		var header = CreateTestHeader();
		var builder = new GbRomBuilder(header);

		// Add segment that requires more than 32KB
		var bigSegment = new byte[0x9000]; // 36KB of data
		builder.AddSegment(0x0150, bigSegment);

		var rom = builder.Build();

		Assert.Equal(0x10000, rom.Length); // Rounded to 64KB
	}

	[Fact]
	public void Build_HeaderChecksumIsValid() {
		var header = CreateTestHeader();
		var builder = new GbRomBuilder(header);

		var rom = builder.Build();

		// Manually calculate expected checksum
		int expectedChecksum = 0;
		for (var i = 0x134; i <= 0x14c; i++) {
			expectedChecksum = expectedChecksum - rom[i] - 1;
		}
		expectedChecksum &= 0xff;

		Assert.Equal(expectedChecksum, rom[0x14d]);
	}

	[Fact]
	public void Build_GlobalChecksumIsValid() {
		var header = CreateTestHeader();
		var builder = new GbRomBuilder(header);

		// Add some test data
		builder.AddSegment(0x0150, [0x00, 0xc3, 0x50, 0x01]); // nop; jp $0150

		var rom = builder.Build();

		// Calculate expected global checksum (sum of all bytes except checksum bytes)
		uint sum = 0;
		for (var i = 0; i < rom.Length; i++) {
			if (i != 0x14e && i != 0x14f) {
				sum += rom[i];
			}
		}
		var expectedChecksum = (ushort)(sum & 0xffff);

		// Global checksum is big-endian at $014e-$014f
		var actualChecksum = (ushort)((rom[0x14e] << 8) | rom[0x14f]);

		Assert.Equal(expectedChecksum, actualChecksum);
	}

	[Fact]
	public void AddSegment_CorrectPlacement() {
		var header = CreateTestHeader();
		var builder = new GbRomBuilder(header);

		// Add code at $0150 (right after header)
		builder.AddSegment(0x0150, [0x3e, 0x42]); // ld a, $42

		var rom = builder.Build();

		Assert.Equal(0x3e, rom[0x0150]);
		Assert.Equal(0x42, rom[0x0151]);
	}

	[Fact]
	public void AddSegment_MultipleSegments() {
		var header = CreateTestHeader();
		var builder = new GbRomBuilder(header);

		// Add segments at different locations
		builder.AddSegment(0x0150, [0x3e, 0x01]); // ld a, $01
		builder.AddSegment(0x0200, [0x3e, 0x02]); // ld a, $02
		builder.AddSegment(0x4000, [0x3e, 0x03]); // ld a, $03 (bank 1 area)

		var rom = builder.Build();

		Assert.Equal(0x3e, rom[0x0150]);
		Assert.Equal(0x01, rom[0x0151]);
		Assert.Equal(0x3e, rom[0x0200]);
		Assert.Equal(0x02, rom[0x0201]);
		Assert.Equal(0x3e, rom[0x4000]);
		Assert.Equal(0x03, rom[0x4001]);
	}

	[Fact]
	public void AddSegment_NullOrEmptyIgnored() {
		var header = CreateTestHeader();
		var builder = new GbRomBuilder(header);

		// These should not throw
		builder.AddSegment(0x0150, null!);
		builder.AddSegment(0x0150, []);

		var rom = builder.Build();
		Assert.NotNull(rom);
	}

	[Fact]
	public void Build_NintendoLogoPresent() {
		var header = CreateTestHeader();
		var builder = new GbRomBuilder(header);

		var rom = builder.Build();

		// Full Nintendo logo (48 bytes at $0104-$0133)
		byte[] expectedLogo = [
			0xce, 0xed, 0x66, 0x66, 0xcc, 0x0d, 0x00, 0x0b,
			0x03, 0x73, 0x00, 0x83, 0x00, 0x0c, 0x00, 0x0d,
			0x00, 0x08, 0x11, 0x1f, 0x88, 0x89, 0x00, 0x0e,
			0xdc, 0xcc, 0x6e, 0xe6, 0xdd, 0xdd, 0xd9, 0x99,
			0xbb, 0xbb, 0x67, 0x63, 0x6e, 0x0e, 0xec, 0xcc,
			0xdd, 0xdc, 0x99, 0x9f, 0xbb, 0xb9, 0x33, 0x3e
		];

		for (var i = 0; i < 48; i++) {
			Assert.Equal(expectedLogo[i], rom[0x104 + i]);
		}
	}

	[Fact]
	public void Build_RestartVectorsPreserved() {
		var header = CreateTestHeader();
		var builder = new GbRomBuilder(header);

		// Add RST handlers
		builder.AddSegment(0x0000, [0xc9]); // RST $00: ret
		builder.AddSegment(0x0008, [0xc9]); // RST $08: ret
		builder.AddSegment(0x0040, [0xc9]); // VBlank interrupt: ret

		var rom = builder.Build();

		Assert.Equal(0xc9, rom[0x0000]);
		Assert.Equal(0xc9, rom[0x0008]);
		Assert.Equal(0xc9, rom[0x0040]);
	}

	[Fact]
	public void Build_SegmentBeforeHeaderPreserved() {
		var header = CreateTestHeader();
		var builder = new GbRomBuilder(header);

		// Add data at $00 (before header)
		builder.AddSegment(0x0000, [0x00, 0xc3, 0x50, 0x01]); // nop; jp $0150

		var rom = builder.Build();

		Assert.Equal(0x00, rom[0x0000]);
		Assert.Equal(0xc3, rom[0x0001]);
		Assert.Equal(0x50, rom[0x0002]);
		Assert.Equal(0x01, rom[0x0003]);
	}

	[Fact]
	public void Build_LargeRomSize() {
		var header = CreateTestHeader();
		var builder = new GbRomBuilder(header);

		// Add segment that requires 64KB ROM
		builder.AddSegment(0x8000, [0xea]); // Just past 32KB boundary

		var rom = builder.Build();

		Assert.Equal(0x10000, rom.Length); // 64KB
	}

	[Fact]
	public void Build_GlobalChecksumBigEndian() {
		var header = CreateTestHeader();
		var builder = new GbRomBuilder(header);

		var rom = builder.Build();

		// Read checksum bytes
		var highByte = rom[0x14e];
		var lowByte = rom[0x14f];

		// Calculate what the checksum should be
		uint sum = 0;
		for (var i = 0; i < rom.Length; i++) {
			if (i != 0x14e && i != 0x14f) {
				sum += rom[i];
			}
		}

		var expectedHigh = (byte)((sum >> 8) & 0xff);
		var expectedLow = (byte)(sum & 0xff);

		Assert.Equal(expectedHigh, highByte);
		Assert.Equal(expectedLow, lowByte);
	}
}
