// ============================================================================
// Atari2600RomBuilderTests.cs - Atari 2600 ROM Builder Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Arch.MOS6502;
using Poppy.Core.CodeGen;

namespace Poppy.Arch.MOS6502.Tests.CodeGen;

/// <summary>
/// Tests for the Atari 2600 ROM builder.
/// </summary>
public sealed class Atari2600RomBuilderTests {
	// ========================================================================
	// Constructor / Validation
	// ========================================================================

	[Fact]
	public void Constructor_Default4K_Succeeds() {
		var builder = new Atari2600RomBuilder();
		var rom = builder.Build();

		Assert.Equal(4096, rom.Length);
	}

	[Theory]
	[InlineData(2048)]
	[InlineData(4096)]
	public void Constructor_ValidStandardSizes_Succeeds(int size) {
		var builder = new Atari2600RomBuilder(size);
		var rom = builder.Build();

		Assert.Equal(size, rom.Length);
	}

	[Theory]
	[InlineData(8192, Atari2600RomBuilder.BankSwitchingMethod.F8)]
	[InlineData(8192, Atari2600RomBuilder.BankSwitchingMethod.FE)]
	[InlineData(8192, Atari2600RomBuilder.BankSwitchingMethod.E0)]
	[InlineData(16384, Atari2600RomBuilder.BankSwitchingMethod.F6)]
	[InlineData(16384, Atari2600RomBuilder.BankSwitchingMethod.E7)]
	[InlineData(32768, Atari2600RomBuilder.BankSwitchingMethod.F4)]
	public void Constructor_BankSwitched_Succeeds(int size, Atari2600RomBuilder.BankSwitchingMethod method) {
		var builder = new Atari2600RomBuilder(size, method);
		var rom = builder.Build();

		Assert.Equal(size, rom.Length);
	}

	[Fact]
	public void Constructor_TooSmall_Throws() {
		Assert.Throws<ArgumentException>(() => new Atari2600RomBuilder(1024));
	}

	[Fact]
	public void Constructor_TooBig_Throws() {
		Assert.Throws<ArgumentException>(() => new Atari2600RomBuilder(1048576));
	}

	[Fact]
	public void Constructor_WrongSizeForF8_Throws() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.F8));
	}

	[Fact]
	public void Constructor_WrongSizeForF6_Throws() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(8192, Atari2600RomBuilder.BankSwitchingMethod.F6));
	}

	[Fact]
	public void Constructor_WrongSizeForF4_Throws() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.F4));
	}

	[Fact]
	public void Constructor_NonStandardSizeNoBankSwitch_Throws() {
		// None requires 2K or 4K only
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(8192, Atari2600RomBuilder.BankSwitchingMethod.None));
	}

	// ========================================================================
	// Build Output
	// ========================================================================

	[Fact]
	public void Build_EmptyRom_InitializedToFF() {
		var builder = new Atari2600RomBuilder();
		var rom = builder.Build();

		// Most of the ROM should be $ff (except default reset vector area)
		// Check a middle byte
		Assert.Equal(0xff, rom[2000]);
	}

	[Fact]
	public void Build_4K_HasDefaultResetVector() {
		var builder = new Atari2600RomBuilder();
		var rom = builder.Build();

		// Reset vector is at ROM_SIZE-4 (the $fffc mapping)
		// For a 4K ROM: offset 4092, 4093
		var lowByte = rom[4092];
		var highByte = rom[4093];
		var resetVector = lowByte | (highByte << 8);

		// Default reset vector should point to $1000 (start of ROM space)
		Assert.Equal(0x1000, resetVector);
	}

	// ========================================================================
	// AddSegment
	// ========================================================================

	[Fact]
	public void AddSegment_WritesDataToRom() {
		var builder = new Atari2600RomBuilder();
		var data = new byte[] { 0xa9, 0x42, 0x85, 0x80 }; // lda #$42; sta $80

		// Write to $1000 (start of 4K ROM space)
		builder.AddSegment(0x1000, data);
		var rom = builder.Build();

		Assert.Equal(0xa9, rom[0]); // mapped: $1000 - $1000 = offset 0
		Assert.Equal(0x42, rom[1]);
		Assert.Equal(0x85, rom[2]);
		Assert.Equal(0x80, rom[3]);
	}

	[Fact]
	public void AddSegment_2K_WritesCorrectly() {
		var builder = new Atari2600RomBuilder(2048);
		var data = new byte[] { 0xea }; // NOP

		builder.AddSegment(0x1000, data);
		var rom = builder.Build();

		Assert.Equal(0xea, rom[0]);
	}

	[Fact]
	public void AddSegment_MultipleSegments_AllPresent() {
		var builder = new Atari2600RomBuilder();

		builder.AddSegment(0x1000, new byte[] { 0xa9 });
		builder.AddSegment(0x1100, new byte[] { 0x4c });

		var rom = builder.Build();

		Assert.Equal(0xa9, rom[0x000]);    // $1000 → offset 0
		Assert.Equal(0x4c, rom[0x100]);    // $1100 → offset 256
	}

	// ========================================================================
	// Bank Switching Methods (F8)
	// ========================================================================

	[Fact]
	public void F8_Build_Returns8KRom() {
		var builder = new Atari2600RomBuilder(8192, Atari2600RomBuilder.BankSwitchingMethod.F8);
		var rom = builder.Build();

		Assert.Equal(8192, rom.Length);
	}

	// ========================================================================
	// Bank Switching Methods (F6)
	// ========================================================================

	[Fact]
	public void F6_Build_Returns16KRom() {
		var builder = new Atari2600RomBuilder(16384, Atari2600RomBuilder.BankSwitchingMethod.F6);
		var rom = builder.Build();

		Assert.Equal(16384, rom.Length);
	}

	// ========================================================================
	// Bank Switching Methods (F4)
	// ========================================================================

	[Fact]
	public void F4_Build_Returns32KRom() {
		var builder = new Atari2600RomBuilder(32768, Atari2600RomBuilder.BankSwitchingMethod.F4);
		var rom = builder.Build();

		Assert.Equal(32768, rom.Length);
	}

	// ========================================================================
	// Bank Switching Methods (3F)
	// ========================================================================

	[Theory]
	[InlineData(4096)]
	[InlineData(8192)]
	[InlineData(65536)]
	[InlineData(524288)]
	public void ThreeF_ValidSizes_Succeeds(int size) {
		var builder = new Atari2600RomBuilder(size, Atari2600RomBuilder.BankSwitchingMethod.ThreeF);
		var rom = builder.Build();

		Assert.Equal(size, rom.Length);
	}

	[Fact]
	public void ThreeF_TooSmall_Throws() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(2048, Atari2600RomBuilder.BankSwitchingMethod.ThreeF));
	}

	// ========================================================================
	// BankSwitchingMethod Enum Values
	// ========================================================================

	[Fact]
	public void BankSwitchingMethod_AllValuesExist() {
		var values = Enum.GetValues<Atari2600RomBuilder.BankSwitchingMethod>();

		Assert.Contains(Atari2600RomBuilder.BankSwitchingMethod.None, values);
		Assert.Contains(Atari2600RomBuilder.BankSwitchingMethod.F8, values);
		Assert.Contains(Atari2600RomBuilder.BankSwitchingMethod.F6, values);
		Assert.Contains(Atari2600RomBuilder.BankSwitchingMethod.F4, values);
		Assert.Contains(Atari2600RomBuilder.BankSwitchingMethod.FE, values);
		Assert.Contains(Atari2600RomBuilder.BankSwitchingMethod.E0, values);
		Assert.Contains(Atari2600RomBuilder.BankSwitchingMethod.ThreeF, values);
		Assert.Contains(Atari2600RomBuilder.BankSwitchingMethod.E7, values);
	}
}
