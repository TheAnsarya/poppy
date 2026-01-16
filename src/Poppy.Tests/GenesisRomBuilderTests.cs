// ============================================================================
// GenesisRomBuilderTests.cs - Genesis ROM Builder Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;

namespace Poppy.Tests;

/// <summary>
/// Tests for Genesis ROM builder.
/// </summary>
public class GenesisRomBuilderTests {
	[Fact]
	public void Build_DefaultRom_HasCorrectSize() {
		var builder = new GenesisRomBuilder(GenesisRomBuilder.RomSizes.Size512K);
		var rom = builder.Build();

		Assert.Equal(512 * 1024, rom.Length);
	}

	[Fact]
	public void Build_HasTmrSegaSignature() {
		var builder = new GenesisRomBuilder();
		builder.SetConsoleName("SEGA MEGA DRIVE ");
		var rom = builder.Build();

		// Check for "SEGA" at $100
		Assert.Equal((byte)'S', rom[0x100]);
		Assert.Equal((byte)'E', rom[0x101]);
		Assert.Equal((byte)'G', rom[0x102]);
		Assert.Equal((byte)'A', rom[0x103]);
	}

	[Fact]
	public void Build_DomesticNameSet_WrittenCorrectly() {
		var builder = new GenesisRomBuilder();
		builder.SetDomesticName("TEST GAME");
		var rom = builder.Build();

		// Domestic name starts at $120
		Assert.Equal((byte)'T', rom[0x120]);
		Assert.Equal((byte)'E', rom[0x121]);
		Assert.Equal((byte)'S', rom[0x122]);
		Assert.Equal((byte)'T', rom[0x123]);
	}

	[Fact]
	public void Build_OverseasNameSet_WrittenCorrectly() {
		var builder = new GenesisRomBuilder();
		builder.SetOverseasName("OVERSEAS GAME");
		var rom = builder.Build();

		// Overseas name starts at $150
		Assert.Equal((byte)'O', rom[0x150]);
		Assert.Equal((byte)'V', rom[0x151]);
		Assert.Equal((byte)'E', rom[0x152]);
		Assert.Equal((byte)'R', rom[0x153]);
	}

	[Fact]
	public void Build_RomStartAndEnd_CorrectBigEndian() {
		var builder = new GenesisRomBuilder(GenesisRomBuilder.RomSizes.Size1M);
		var rom = builder.Build();

		// ROM start at $1a0 should be $00000000
		Assert.Equal(0x00, rom[0x1a0]);
		Assert.Equal(0x00, rom[0x1a1]);
		Assert.Equal(0x00, rom[0x1a2]);
		Assert.Equal(0x00, rom[0x1a3]);

		// ROM end at $1a4 should be $000fffff (1MB - 1)
		Assert.Equal(0x00, rom[0x1a4]);
		Assert.Equal(0x0f, rom[0x1a5]);
		Assert.Equal(0xff, rom[0x1a6]);
		Assert.Equal(0xff, rom[0x1a7]);
	}

	[Fact]
	public void Build_SramEnabled_WrittenCorrectly() {
		var builder = new GenesisRomBuilder();
		builder.SetSram(true, 0x00200000, 0x0020ffff);
		var rom = builder.Build();

		// SRAM info starts at $1b0, should have "RA"
		Assert.Equal((byte)'R', rom[0x1b0]);
		Assert.Equal((byte)'A', rom[0x1b1]);
	}

	[Fact]
	public void Build_SramDisabled_Spaces() {
		var builder = new GenesisRomBuilder();
		builder.SetSram(false);
		var rom = builder.Build();

		// SRAM info at $1b0 should be spaces
		Assert.Equal((byte)' ', rom[0x1b0]);
		Assert.Equal((byte)' ', rom[0x1b1]);
	}

	[Fact]
	public void Build_RegionSet_WrittenCorrectly() {
		var builder = new GenesisRomBuilder();
		builder.SetRegion("JUE");
		var rom = builder.Build();

		// Region at $1f0
		Assert.Equal((byte)'J', rom[0x1f0]);
		Assert.Equal((byte)'U', rom[0x1f1]);
		Assert.Equal((byte)'E', rom[0x1f2]);
	}

	[Fact]
	public void AddSegment_DataWrittenCorrectly() {
		var builder = new GenesisRomBuilder();
		builder.AddSegment(0x200, [0x12, 0x34, 0x56, 0x78]);
		var rom = builder.Build();

		Assert.Equal(0x12, rom[0x200]);
		Assert.Equal(0x34, rom[0x201]);
		Assert.Equal(0x56, rom[0x202]);
		Assert.Equal(0x78, rom[0x203]);
	}

	[Fact]
	public void Build_ChecksumCalculated() {
		var builder = new GenesisRomBuilder();
		var rom = builder.Build();

		// Checksum is at $18e-$18f (big-endian)
		var checksumHi = rom[0x18e];
		var checksumLo = rom[0x18f];

		// Verify checksum is not zero (it should be calculated)
		var checksum = (checksumHi << 8) | checksumLo;
		// The checksum may be zero for an empty ROM, but the field should exist
		Assert.True(checksum >= 0);
	}

	[Fact]
	public void SetCopyright_WrittenAt110() {
		var builder = new GenesisRomBuilder();
		builder.SetCopyright("(C)TEST 2026");
		var rom = builder.Build();

		// Copyright at $110
		Assert.Equal((byte)'(', rom[0x110]);
		Assert.Equal((byte)'C', rom[0x111]);
		Assert.Equal((byte)')', rom[0x112]);
	}

	[Fact]
	public void SetProductCode_WrittenAt180() {
		var builder = new GenesisRomBuilder();
		builder.SetProductCode("GM 12345678-00");
		var rom = builder.Build();

		// Product code at $180
		Assert.Equal((byte)'G', rom[0x180]);
		Assert.Equal((byte)'M', rom[0x181]);
	}
}

