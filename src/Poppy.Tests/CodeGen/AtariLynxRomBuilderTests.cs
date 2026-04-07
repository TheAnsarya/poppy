// ============================================================================
// AtariLynxRomBuilderTests.cs - Atari Lynx LNX ROM Builder Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Arch.MOS6502;
using Poppy.Core.CodeGen;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for the Atari Lynx LNX ROM builder.
/// </summary>
public sealed class AtariLynxRomBuilderTests {
	// ========================================================================
	// Constructor / Validation
	// ========================================================================

	[Fact]
	public void Constructor_Default_Succeeds() {
		var builder = new AtariLynxRomBuilder();

		Assert.Equal(131072, builder.RomSize);
		Assert.Equal(512, builder.Bank0Pages);
		Assert.Equal(0, builder.Bank1Pages);
	}

	[Fact]
	public void Constructor_CustomBankSizes_Succeeds() {
		var builder = new AtariLynxRomBuilder(bank0Size: 65536, bank1Size: 32768);

		Assert.Equal(98304, builder.RomSize);
		Assert.Equal(256, builder.Bank0Pages);
		Assert.Equal(128, builder.Bank1Pages);
	}

	[Fact]
	public void Constructor_Bank0NotMultipleOfPageSize_Throws() {
		Assert.Throws<ArgumentException>(() =>
			new AtariLynxRomBuilder(bank0Size: 1000));
	}

	[Fact]
	public void Constructor_Bank1NotMultipleOfPageSize_Throws() {
		Assert.Throws<ArgumentException>(() =>
			new AtariLynxRomBuilder(bank1Size: 100));
	}

	[Fact]
	public void Constructor_NegativeBank0_Throws() {
		Assert.Throws<ArgumentException>(() =>
			new AtariLynxRomBuilder(bank0Size: -256));
	}

	[Fact]
	public void Constructor_Bank1WithoutBank0_Throws() {
		Assert.Throws<ArgumentException>(() =>
			new AtariLynxRomBuilder(bank0Size: 0, bank1Size: 256));
	}

	// ========================================================================
	// Build - LNX Header
	// ========================================================================

	[Fact]
	public void Build_HasLynxMagic() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256);
		var rom = builder.Build();

		Assert.Equal((byte)'L', rom[0]);
		Assert.Equal((byte)'Y', rom[1]);
		Assert.Equal((byte)'N', rom[2]);
		Assert.Equal((byte)'X', rom[3]);
	}

	[Fact]
	public void Build_HasCorrectBank0PageCount() {
		var builder = new AtariLynxRomBuilder(bank0Size: 1024); // 4 pages
		var rom = builder.Build();

		// Bank 0 page count at offset 4-5, little-endian
		var pages = rom[4] | (rom[5] << 8);
		Assert.Equal(4, pages);
	}

	[Fact]
	public void Build_HasCorrectBank1PageCount() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256, bank1Size: 512); // 1 + 2 pages
		var rom = builder.Build();

		// Bank 1 page count at offset 6-7, little-endian
		var pages = rom[6] | (rom[7] << 8);
		Assert.Equal(2, pages);
	}

	[Fact]
	public void Build_HasVersion() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256, version: 1);
		var rom = builder.Build();

		// Version at offset 8-9, little-endian
		var version = rom[8] | (rom[9] << 8);
		Assert.Equal(1, version);
	}

	[Fact]
	public void Build_HasGameName() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256, gameName: "TEST");
		var rom = builder.Build();

		// Cart name at offset 10-41
		Assert.Equal((byte)'T', rom[10]);
		Assert.Equal((byte)'E', rom[11]);
		Assert.Equal((byte)'S', rom[12]);
		Assert.Equal((byte)'T', rom[13]);
		Assert.Equal(0, rom[14]); // null terminator
	}

	[Fact]
	public void Build_HasManufacturer() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256, manufacturer: "DEV");
		var rom = builder.Build();

		// Manufacturer at offset 42-57
		Assert.Equal((byte)'D', rom[42]);
		Assert.Equal((byte)'E', rom[43]);
		Assert.Equal((byte)'V', rom[44]);
		Assert.Equal(0, rom[45]); // null terminator
	}

	[Fact]
	public void Build_HasRotation() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256, rotation: LynxRotation.Left);
		var rom = builder.Build();

		// Rotation at offset 58
		Assert.Equal(1, rom[58]);
	}

	[Theory]
	[InlineData(LynxRotation.None, 0)]
	[InlineData(LynxRotation.Left, 1)]
	[InlineData(LynxRotation.Right, 2)]
	public void Build_RotationValues_Correct(LynxRotation rotation, byte expected) {
		var builder = new AtariLynxRomBuilder(bank0Size: 256, rotation: rotation);
		var rom = builder.Build();

		Assert.Equal(expected, rom[58]);
	}

	// ========================================================================
	// Build - Size
	// ========================================================================

	[Fact]
	public void Build_IncludesHeaderAndRom() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256);
		var rom = builder.Build();

		// Header (64) + ROM (256)
		Assert.Equal(320, rom.Length);
	}

	[Fact]
	public void Build_TwoBanks_CorrectSize() {
		var builder = new AtariLynxRomBuilder(bank0Size: 512, bank1Size: 256);
		var rom = builder.Build();

		// Header (64) + Bank0 (512) + Bank1 (256)
		Assert.Equal(832, rom.Length);
	}

	[Fact]
	public void BuildRaw_NoHeader() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256);
		var rawRom = builder.BuildRaw();

		Assert.Equal(256, rawRom.Length);
	}

	// ========================================================================
	// Build - ROM Data Init
	// ========================================================================

	[Fact]
	public void Build_EmptyRom_RomAreaInitializedToFF() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256);
		var rom = builder.Build();

		// ROM data starts after 64-byte header
		Assert.Equal(0xff, rom[64]);
		Assert.Equal(0xff, rom[rom.Length - 1]);
	}

	// ========================================================================
	// AddSegment
	// ========================================================================

	[Fact]
	public void AddSegment_CpuAddress_WritesCorrectly() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256);
		var data = new byte[] { 0xa9, 0x42 };

		// CPU address $0200 = ROM offset 0
		builder.AddSegment(0x0200, data);
		var rom = builder.Build();

		Assert.Equal(0xa9, rom[64]);  // header offset + 0
		Assert.Equal(0x42, rom[65]);
	}

	[Fact]
	public void AddSegment_RawOffset_WritesCorrectly() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256);
		var data = new byte[] { 0xea };

		// Raw offset below LoadAddress is treated as-is
		builder.AddSegment(0, data);
		var rom = builder.Build();

		Assert.Equal(0xea, rom[64]);
	}

	[Fact]
	public void AddSegment_Bank1_WritesCorrectly() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256, bank1Size: 256);
		var data = new byte[] { 0x4c };

		builder.AddSegment(0, data, bank: 1);
		var rom = builder.Build();

		// Bank 1 starts after header + bank 0
		Assert.Equal(0x4c, rom[64 + 256]);
	}

	[Fact]
	public void AddSegment_InvalidBank_Throws() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256);

		Assert.Throws<ArgumentException>(() =>
			builder.AddSegment(0, new byte[] { 0x00 }, bank: 2));
	}

	[Fact]
	public void AddSegment_Bank1WhenNoBank1_Throws() {
		var builder = new AtariLynxRomBuilder(bank0Size: 256, bank1Size: 0);

		Assert.Throws<InvalidOperationException>(() =>
			builder.AddSegment(0, new byte[] { 0x00 }, bank: 1));
	}

	// ========================================================================
	// Properties
	// ========================================================================

	[Fact]
	public void RomSize_SingleBank_Correct() {
		var builder = new AtariLynxRomBuilder(bank0Size: 65536);

		Assert.Equal(65536, builder.RomSize);
	}

	[Fact]
	public void RomSize_TwoBanks_SumOfBoth() {
		var builder = new AtariLynxRomBuilder(bank0Size: 65536, bank1Size: 32768);

		Assert.Equal(98304, builder.RomSize);
	}

	[Fact]
	public void HeaderSize_Is64() {
		Assert.Equal(64, AtariLynxRomBuilder.HeaderSize);
	}

	[Fact]
	public void PageSize_Is256() {
		Assert.Equal(256, AtariLynxRomBuilder.PageSize);
	}

	[Fact]
	public void LoadAddress_Is0x0200() {
		Assert.Equal(0x0200, AtariLynxRomBuilder.LoadAddress);
	}

	// ========================================================================
	// LynxRotation Enum
	// ========================================================================

	[Fact]
	public void LynxRotation_AllValuesExist() {
		var values = Enum.GetValues<LynxRotation>();

		Assert.Contains(LynxRotation.None, values);
		Assert.Contains(LynxRotation.Left, values);
		Assert.Contains(LynxRotation.Right, values);
	}
}
