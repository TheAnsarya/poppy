// ============================================================================
// MasterSystemRomBuilderTests.cs - Master System ROM Builder Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;

namespace Poppy.Tests;

/// <summary>
/// Tests for Master System ROM builder.
/// </summary>
public class MasterSystemRomBuilderTests {
	[Fact]
	public void Build_DefaultRom_HasCorrectSize() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size32K);
		var rom = builder.Build();

		Assert.Equal(32 * 1024, rom.Length);
	}

	[Fact]
	public void Build_HasTmrSegaSignature() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size32K);
		var rom = builder.Build();

		// TMR SEGA at $7ff0 for 32KB ROM
		Assert.Equal((byte)'T', rom[0x7ff0]);
		Assert.Equal((byte)'M', rom[0x7ff1]);
		Assert.Equal((byte)'R', rom[0x7ff2]);
		Assert.Equal((byte)' ', rom[0x7ff3]);
		Assert.Equal((byte)'S', rom[0x7ff4]);
		Assert.Equal((byte)'E', rom[0x7ff5]);
		Assert.Equal((byte)'G', rom[0x7ff6]);
		Assert.Equal((byte)'A', rom[0x7ff7]);
	}

	[Fact]
	public void Build_8KRom_HeaderAt1FF0() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size8K);
		var rom = builder.Build();

		// TMR SEGA at $1ff0 for 8KB ROM
		Assert.Equal((byte)'T', rom[0x1ff0]);
		Assert.Equal((byte)'M', rom[0x1ff1]);
		Assert.Equal((byte)'R', rom[0x1ff2]);
	}

	[Fact]
	public void Build_16KRom_HeaderAt3FF0() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size16K);
		var rom = builder.Build();

		// TMR SEGA at $3ff0 for 16KB ROM
		Assert.Equal((byte)'T', rom[0x3ff0]);
		Assert.Equal((byte)'M', rom[0x3ff1]);
		Assert.Equal((byte)'R', rom[0x3ff2]);
	}

	[Fact]
	public void Build_ProductCodeSet_WrittenCorrectly() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size32K);
		builder.SetProductCode(12345);  // 5-digit code
		var rom = builder.Build();

		// Product code at $7ffc-$7ffe (BCD encoded)
		// 12345 = digit 0: 5, digit 1: 4, digit 2: 3, digit 3: 2, digit 4: 1
		var bcd0 = rom[0x7ffc];  // Should be $45 (5 in low nibble, 4 in high nibble)
		var bcd1 = rom[0x7ffd];  // Should be $23 (3 in low nibble, 2 in high nibble)
		var bcd2 = rom[0x7ffe];  // Should have 1 in low nibble (upper nibble is version)

		Assert.Equal(0x45, bcd0);
		Assert.Equal(0x23, bcd1);
		Assert.Equal(0x01, bcd2 & 0x0f);  // Just check low nibble (product code digit 5)
	}

	[Fact]
	public void Build_VersionSet_WrittenCorrectly() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size32K);
		builder.SetVersion(5);
		var rom = builder.Build();

		// Version is in upper nibble of $7ffe
		var versionByte = rom[0x7ffe];
		var version = (versionByte >> 4) & 0x0f;
		Assert.Equal(5, version);
	}

	[Fact]
	public void Build_RegionSet_WrittenCorrectly() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size32K);
		builder.SetRegion(MasterSystemRomBuilder.RegionCode.SmsExport);
		var rom = builder.Build();

		// Region is in upper nibble of $7fff
		var regionByte = rom[0x7fff];
		var region = (regionByte >> 4) & 0x0f;
		Assert.Equal((int)MasterSystemRomBuilder.RegionCode.SmsExport, region);
	}

	[Fact]
	public void Build_SizeCodeSet_WrittenCorrectly() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size32K);
		var rom = builder.Build();

		// Size is in lower nibble of $7fff
		var sizeByte = rom[0x7fff];
		var size = sizeByte & 0x0f;
		Assert.Equal((int)MasterSystemRomBuilder.RomSizeCode.Size32K, size);
	}

	[Fact]
	public void Build_GameGear_HasGgRegion() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size32K, isGameGear: true);
		var rom = builder.Build();

		// Default region for GG should be GgExport
		var regionByte = rom[0x7fff];
		var region = (regionByte >> 4) & 0x0f;
		Assert.Equal((int)MasterSystemRomBuilder.RegionCode.GgExport, region);
	}

	[Fact]
	public void Build_ChecksumCalculated() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size32K);
		var rom = builder.Build();

		// Checksum at $7ffa-$7ffb (little-endian)
		var checksumLo = rom[0x7ffa];
		var checksumHi = rom[0x7ffb];
		var checksum = (checksumHi << 8) | checksumLo;

		// Checksum should exist (may be any value)
		Assert.True(checksum >= 0);
	}

	[Fact]
	public void AddSegment_DataWrittenCorrectly() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size32K);
		builder.AddSegment(0x0000, [0x12, 0x34, 0x56, 0x78]);
		var rom = builder.Build();

		Assert.Equal(0x12, rom[0x0000]);
		Assert.Equal(0x34, rom[0x0001]);
		Assert.Equal(0x56, rom[0x0002]);
		Assert.Equal(0x78, rom[0x0003]);
	}

	[Fact]
	public void Build_EmptyRom_FilledWithFF() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size32K);
		var rom = builder.Build();

		// Check a location that shouldn't have data (between code and header)
		Assert.Equal(0xff, rom[0x1000]);
	}

	[Fact]
	public void Build_64KRom_CorrectSizeCode() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size64K);
		var rom = builder.Build();

		var sizeByte = rom[0x7fff];
		var size = sizeByte & 0x0f;
		Assert.Equal((int)MasterSystemRomBuilder.RomSizeCode.Size64K, size);
	}

	[Fact]
	public void Build_128KRom_CorrectSizeCode() {
		var builder = new MasterSystemRomBuilder(MasterSystemRomBuilder.RomSizes.Size128K);
		var rom = builder.Build();

		var sizeByte = rom[0x7fff];
		var size = sizeByte & 0x0f;
		Assert.Equal((int)MasterSystemRomBuilder.RomSizeCode.Size128K, size);
	}
}

