// ============================================================================
// Atari2600BankswitchingTests.cs - Bankswitching Scheme Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Arch.MOS6502;
using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Arch.MOS6502.Tests.CodeGen;

/// <summary>
/// Comprehensive tests for Atari 2600 bankswitching schemes in Atari2600RomBuilder.
/// </summary>
public class Atari2600BankswitchingTests {
	// ========================================================================
	// F8 (8K) — 2 banks of 4K
	// ========================================================================

	[Fact]
	public void F8_Creates8KRom() {
		var builder = new Atari2600RomBuilder(8192, Atari2600RomBuilder.BankSwitchingMethod.F8);
		var rom = builder.Build();
		Assert.Equal(8192, rom.Length);
	}

	[Fact]
	public void F8_RejectsWrongSize() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.F8));
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(16384, Atari2600RomBuilder.BankSwitchingMethod.F8));
	}

	[Fact]
	public void F8_SegmentPlacement() {
		var builder = new Atari2600RomBuilder(8192, Atari2600RomBuilder.BankSwitchingMethod.F8);
		builder.AddSegment(0x1000, [0xa9, 0x42]); // LDA #$42
		var rom = builder.Build();

		// Verify data is placed in the ROM
		Assert.Contains(rom, b => b == 0xa9);
		Assert.Contains(rom, b => b == 0x42);
	}

	[Fact]
	public void F8_UnusedSpaceIsFilled() {
		var builder = new Atari2600RomBuilder(8192, Atari2600RomBuilder.BankSwitchingMethod.F8);
		var rom = builder.Build();

		// Most of the ROM should be $ff (unused)
		int ffCount = rom.Count(b => b == 0xff);
		Assert.True(ffCount > 8000, $"Expected most bytes to be $ff, got {ffCount}");
	}

	// ========================================================================
	// F6 (16K) — 4 banks of 4K
	// ========================================================================

	[Fact]
	public void F6_Creates16KRom() {
		var builder = new Atari2600RomBuilder(16384, Atari2600RomBuilder.BankSwitchingMethod.F6);
		var rom = builder.Build();
		Assert.Equal(16384, rom.Length);
	}

	[Fact]
	public void F6_RejectsWrongSize() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(8192, Atari2600RomBuilder.BankSwitchingMethod.F6));
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(32768, Atari2600RomBuilder.BankSwitchingMethod.F6));
	}

	[Fact]
	public void F6_SegmentPlacement() {
		var builder = new Atari2600RomBuilder(16384, Atari2600RomBuilder.BankSwitchingMethod.F6);
		builder.AddSegment(0x1000, [0xa2, 0x10]); // LDX #$10
		var rom = builder.Build();
		Assert.Contains(rom, b => b == 0xa2);
	}

	// ========================================================================
	// F4 (32K) — 8 banks of 4K
	// ========================================================================

	[Fact]
	public void F4_Creates32KRom() {
		var builder = new Atari2600RomBuilder(32768, Atari2600RomBuilder.BankSwitchingMethod.F4);
		var rom = builder.Build();
		Assert.Equal(32768, rom.Length);
	}

	[Fact]
	public void F4_RejectsWrongSize() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(16384, Atari2600RomBuilder.BankSwitchingMethod.F4));
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(65536, Atari2600RomBuilder.BankSwitchingMethod.F4));
	}

	[Fact]
	public void F4_SegmentPlacement() {
		var builder = new Atari2600RomBuilder(32768, Atari2600RomBuilder.BankSwitchingMethod.F4);
		builder.AddSegment(0x1000, [0xa0, 0x20]); // LDY #$20
		var rom = builder.Build();
		Assert.Contains(rom, b => b == 0xa0);
	}

	// ========================================================================
	// FE (8K) — Stack-based switching
	// ========================================================================

	[Fact]
	public void FE_Creates8KRom() {
		var builder = new Atari2600RomBuilder(8192, Atari2600RomBuilder.BankSwitchingMethod.FE);
		var rom = builder.Build();
		Assert.Equal(8192, rom.Length);
	}

	[Fact]
	public void FE_RejectsWrongSize() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.FE));
	}

	// ========================================================================
	// E0 (8K) — 4 banks of 2K
	// ========================================================================

	[Fact]
	public void E0_Creates8KRom() {
		var builder = new Atari2600RomBuilder(8192, Atari2600RomBuilder.BankSwitchingMethod.E0);
		var rom = builder.Build();
		Assert.Equal(8192, rom.Length);
	}

	[Fact]
	public void E0_RejectsWrongSize() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(16384, Atari2600RomBuilder.BankSwitchingMethod.E0));
	}

	// ========================================================================
	// 3F (up to 512K) — TIA-range hotspot switching
	// ========================================================================

	[Fact]
	public void ThreeF_Creates4KMinRom() {
		var builder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.ThreeF);
		var rom = builder.Build();
		Assert.Equal(4096, rom.Length);
	}

	[Fact]
	public void ThreeF_Creates512KMaxRom() {
		var builder = new Atari2600RomBuilder(524288, Atari2600RomBuilder.BankSwitchingMethod.ThreeF);
		var rom = builder.Build();
		Assert.Equal(524288, rom.Length);
	}

	[Fact]
	public void ThreeF_RejectsTooSmall() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(2048, Atari2600RomBuilder.BankSwitchingMethod.ThreeF));
	}

	[Fact]
	public void ThreeF_RejectsTooLarge() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(1048576, Atari2600RomBuilder.BankSwitchingMethod.ThreeF));
	}

	// ========================================================================
	// E7 (16K+2K RAM) — RAM+ROM mixed banking
	// ========================================================================

	[Fact]
	public void E7_Creates16KRom() {
		var builder = new Atari2600RomBuilder(16384, Atari2600RomBuilder.BankSwitchingMethod.E7);
		var rom = builder.Build();
		Assert.Equal(16384, rom.Length);
	}

	[Fact]
	public void E7_RejectsWrongSize() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(8192, Atari2600RomBuilder.BankSwitchingMethod.E7));
	}

	// ========================================================================
	// Cross-scheme validation
	// ========================================================================

	[Theory]
	[InlineData(Atari2600RomBuilder.BankSwitchingMethod.None, 2048)]
	[InlineData(Atari2600RomBuilder.BankSwitchingMethod.None, 4096)]
	[InlineData(Atari2600RomBuilder.BankSwitchingMethod.F8, 8192)]
	[InlineData(Atari2600RomBuilder.BankSwitchingMethod.FE, 8192)]
	[InlineData(Atari2600RomBuilder.BankSwitchingMethod.E0, 8192)]
	[InlineData(Atari2600RomBuilder.BankSwitchingMethod.F6, 16384)]
	[InlineData(Atari2600RomBuilder.BankSwitchingMethod.E7, 16384)]
	[InlineData(Atari2600RomBuilder.BankSwitchingMethod.F4, 32768)]
	[InlineData(Atari2600RomBuilder.BankSwitchingMethod.ThreeF, 4096)]
	public void ValidCombinations_DoNotThrow(Atari2600RomBuilder.BankSwitchingMethod method, int size) {
		var builder = new Atari2600RomBuilder(size, method);
		var rom = builder.Build();
		Assert.Equal(size, rom.Length);
	}

	[Fact]
	public void AllSchemes_ProduceRomFilledWithFF() {
		var schemes = new (Atari2600RomBuilder.BankSwitchingMethod Method, int Size)[] {
			(Atari2600RomBuilder.BankSwitchingMethod.None, 4096),
			(Atari2600RomBuilder.BankSwitchingMethod.F8, 8192),
			(Atari2600RomBuilder.BankSwitchingMethod.F6, 16384),
			(Atari2600RomBuilder.BankSwitchingMethod.F4, 32768),
			(Atari2600RomBuilder.BankSwitchingMethod.FE, 8192),
			(Atari2600RomBuilder.BankSwitchingMethod.E0, 8192),
			(Atari2600RomBuilder.BankSwitchingMethod.E7, 16384),
		};

		foreach (var (method, size) in schemes) {
			var builder = new Atari2600RomBuilder(size, method);
			var rom = builder.Build();
			// Except for reset vector area, should be $ff
			int nonFFCount = 0;
			for (int i = 0; i < rom.Length - 4; i++) {
				if (rom[i] != 0xff) nonFFCount++;
			}
			Assert.True(nonFFCount == 0,
				$"Scheme {method}: Expected empty ROM (except vectors) to be all $ff, found {nonFFCount} non-$ff bytes");
		}
	}

	[Fact]
	public void InvalidRomSize_TooSmall_Throws() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(1024, Atari2600RomBuilder.BankSwitchingMethod.None));
	}

	[Fact]
	public void InvalidRomSize_TooLarge_Throws() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(1048576, Atari2600RomBuilder.BankSwitchingMethod.None));
	}
}
