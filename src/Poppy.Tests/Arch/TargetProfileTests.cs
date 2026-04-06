// ============================================================================
// TargetProfileTests.cs - ITargetProfile Implementation Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================
// Tracks: #261 (Arch layer tests)

using Poppy.Core.Arch;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

namespace Poppy.Tests.Arch;

/// <summary>
/// Tests for all ITargetProfile implementations.
/// Validates properties, encoder behavior, and profile-specific overrides.
/// </summary>
public sealed class TargetProfileTests {
	// ========================================================================
	// Architecture Property
	// ========================================================================

	[Theory]
	[InlineData(TargetArchitecture.MOS6502)]
	[InlineData(TargetArchitecture.MOS6507)]
	[InlineData(TargetArchitecture.MOS65SC02)]
	[InlineData(TargetArchitecture.WDC65816)]
	[InlineData(TargetArchitecture.SM83)]
	[InlineData(TargetArchitecture.M68000)]
	[InlineData(TargetArchitecture.Z80)]
	[InlineData(TargetArchitecture.V30MZ)]
	[InlineData(TargetArchitecture.ARM7TDMI)]
	[InlineData(TargetArchitecture.SPC700)]
	[InlineData(TargetArchitecture.HuC6280)]
	public void Architecture_MatchesExpected(TargetArchitecture arch) {
		var profile = TargetResolver.GetProfile(arch);
		Assert.Equal(arch, profile.Architecture);
	}

	// ========================================================================
	// Encoder Property — Non-Null
	// ========================================================================

	[Theory]
	[InlineData(TargetArchitecture.MOS6502)]
	[InlineData(TargetArchitecture.MOS6507)]
	[InlineData(TargetArchitecture.MOS65SC02)]
	[InlineData(TargetArchitecture.WDC65816)]
	[InlineData(TargetArchitecture.SM83)]
	[InlineData(TargetArchitecture.M68000)]
	[InlineData(TargetArchitecture.Z80)]
	[InlineData(TargetArchitecture.V30MZ)]
	[InlineData(TargetArchitecture.ARM7TDMI)]
	[InlineData(TargetArchitecture.SPC700)]
	[InlineData(TargetArchitecture.HuC6280)]
	public void Encoder_IsNotNull(TargetArchitecture arch) {
		var profile = TargetResolver.GetProfile(arch);
		Assert.NotNull(profile.Encoder);
	}

	// ========================================================================
	// Mnemonics — Non-Empty
	// ========================================================================

	[Theory]
	[InlineData(TargetArchitecture.MOS6502)]
	[InlineData(TargetArchitecture.MOS6507)]
	[InlineData(TargetArchitecture.MOS65SC02)]
	[InlineData(TargetArchitecture.WDC65816)]
	[InlineData(TargetArchitecture.SM83)]
	[InlineData(TargetArchitecture.M68000)]
	[InlineData(TargetArchitecture.Z80)]
	[InlineData(TargetArchitecture.V30MZ)]
	[InlineData(TargetArchitecture.HuC6280)]
	public void Mnemonics_AreNotEmpty(TargetArchitecture arch) {
		var profile = TargetResolver.GetProfile(arch);
		Assert.NotEmpty(profile.Encoder.Mnemonics);
	}

	// ========================================================================
	// DefaultBankSize — Platform-Specific Values
	// ========================================================================

	[Theory]
	[InlineData(TargetArchitecture.MOS6502, 0x4000)]   // NES: 16KB PRG bank
	[InlineData(TargetArchitecture.MOS6507, 0x1000)]   // Atari 2600: 4KB
	[InlineData(TargetArchitecture.MOS65SC02, 0x4000)] // Lynx: 16KB
	[InlineData(TargetArchitecture.WDC65816, 0x8000)]  // SNES LoROM: 32KB
	[InlineData(TargetArchitecture.SM83, 0x4000)]      // Game Boy: 16KB
	public void DefaultBankSize_MatchesPlatform(TargetArchitecture arch, int expectedSize) {
		var profile = TargetResolver.GetProfile(arch);
		Assert.Equal(expectedSize, profile.DefaultBankSize);
	}

	[Theory]
	[InlineData(TargetArchitecture.M68000)]
	[InlineData(TargetArchitecture.Z80)]
	[InlineData(TargetArchitecture.V30MZ)]
	[InlineData(TargetArchitecture.ARM7TDMI)]
	[InlineData(TargetArchitecture.SPC700)]
	[InlineData(TargetArchitecture.HuC6280)]
	public void DefaultBankSize_IsPositive(TargetArchitecture arch) {
		var profile = TargetResolver.GetProfile(arch);
		Assert.True(profile.DefaultBankSize > 0, $"DefaultBankSize for {arch} should be positive");
	}

	// ========================================================================
	// GetBankCpuBase — Platform-Specific
	// ========================================================================

	[Fact]
	public void GetBankCpuBase_Mos6502_Returns0x8000() {
		var profile = TargetResolver.GetProfile(TargetArchitecture.MOS6502);
		Assert.Equal(0x8000, profile.GetBankCpuBase(0));
		Assert.Equal(0x8000, profile.GetBankCpuBase(1));
	}

	[Fact]
	public void GetBankCpuBase_Mos6507_Returns0xf000() {
		var profile = TargetResolver.GetProfile(TargetArchitecture.MOS6507);
		Assert.Equal(0xf000, profile.GetBankCpuBase(0));
	}

	[Fact]
	public void GetBankCpuBase_Wdc65816_Returns0x8000() {
		var profile = TargetResolver.GetProfile(TargetArchitecture.WDC65816);
		Assert.Equal(0x8000, profile.GetBankCpuBase(0));
		Assert.Equal(0x8000, profile.GetBankCpuBase(1));
	}

	[Fact]
	public void GetBankCpuBase_Sm83_Bank0Is0x0000_OthersAre0x4000() {
		var profile = TargetResolver.GetProfile(TargetArchitecture.SM83);
		Assert.Equal(0x0000, profile.GetBankCpuBase(0));
		Assert.Equal(0x4000, profile.GetBankCpuBase(1));
		Assert.Equal(0x4000, profile.GetBankCpuBase(2));
		Assert.Equal(0x4000, profile.GetBankCpuBase(127));
	}

	[Theory]
	[InlineData(TargetArchitecture.MOS65SC02)]
	[InlineData(TargetArchitecture.M68000)]
	[InlineData(TargetArchitecture.Z80)]
	[InlineData(TargetArchitecture.V30MZ)]
	[InlineData(TargetArchitecture.ARM7TDMI)]
	[InlineData(TargetArchitecture.SPC700)]
	[InlineData(TargetArchitecture.HuC6280)]
	public void GetBankCpuBase_UnbankedArchitectures_ReturnsNegativeOne(TargetArchitecture arch) {
		var profile = TargetResolver.GetProfile(arch);
		Assert.Equal(-1, profile.GetBankCpuBase(0));
	}

	// ========================================================================
	// LongDirectiveSize — Default vs 65816
	// ========================================================================

	[Fact]
	public void LongDirectiveSize_Wdc65816_IsThree() {
		var profile = TargetResolver.GetProfile(TargetArchitecture.WDC65816);
		Assert.Equal(3, profile.LongDirectiveSize);
	}

	[Theory]
	[InlineData(TargetArchitecture.MOS6502)]
	[InlineData(TargetArchitecture.MOS6507)]
	[InlineData(TargetArchitecture.MOS65SC02)]
	[InlineData(TargetArchitecture.SM83)]
	[InlineData(TargetArchitecture.M68000)]
	[InlineData(TargetArchitecture.Z80)]
	[InlineData(TargetArchitecture.V30MZ)]
	[InlineData(TargetArchitecture.ARM7TDMI)]
	[InlineData(TargetArchitecture.SPC700)]
	[InlineData(TargetArchitecture.HuC6280)]
	public void LongDirectiveSize_NonSnes_IsFour(TargetArchitecture arch) {
		var profile = TargetResolver.GetProfile(arch);
		Assert.Equal(4, profile.LongDirectiveSize);
	}

	// ========================================================================
	// AdjustAddressingMode — 65SC02 INC/DEC Special Case
	// ========================================================================

	[Theory]
	[InlineData("inc")]
	[InlineData("dec")]
	[InlineData("INC")]
	[InlineData("DEC")]
	public void AdjustAddressingMode_65sc02_IncDec_Implied_BecomesAccumulator(string mnemonic) {
		var profile = TargetResolver.GetProfile(TargetArchitecture.MOS65SC02);
		var result = profile.AdjustAddressingMode(mnemonic, AddressingMode.Implied);
		Assert.Equal(AddressingMode.Accumulator, result);
	}

	[Theory]
	[InlineData("lda")]
	[InlineData("sta")]
	[InlineData("nop")]
	public void AdjustAddressingMode_65sc02_OtherMnemonics_ReturnsNull(string mnemonic) {
		var profile = TargetResolver.GetProfile(TargetArchitecture.MOS65SC02);
		var result = profile.AdjustAddressingMode(mnemonic, AddressingMode.Implied);
		Assert.Null(result);
	}

	[Theory]
	[InlineData(TargetArchitecture.MOS6502)]
	[InlineData(TargetArchitecture.WDC65816)]
	[InlineData(TargetArchitecture.SM83)]
	[InlineData(TargetArchitecture.M68000)]
	[InlineData(TargetArchitecture.Z80)]
	public void AdjustAddressingMode_DefaultProfiles_ReturnsNull(TargetArchitecture arch) {
		var profile = TargetResolver.GetProfile(arch);
		var result = profile.AdjustAddressingMode("inc", AddressingMode.Implied);
		Assert.Null(result);
	}

	// ========================================================================
	// CreateRomBuilder — Currently All Return Null (Phase 2 TODO)
	// ========================================================================

	[Theory]
	[InlineData(TargetArchitecture.MOS6502)]
	[InlineData(TargetArchitecture.MOS6507)]
	[InlineData(TargetArchitecture.MOS65SC02)]
	[InlineData(TargetArchitecture.WDC65816)]
	[InlineData(TargetArchitecture.SM83)]
	[InlineData(TargetArchitecture.M68000)]
	[InlineData(TargetArchitecture.Z80)]
	[InlineData(TargetArchitecture.V30MZ)]
	[InlineData(TargetArchitecture.ARM7TDMI)]
	[InlineData(TargetArchitecture.SPC700)]
	[InlineData(TargetArchitecture.HuC6280)]
	public void CreateRomBuilder_AllProfiles_ReturnsNull(TargetArchitecture arch) {
		var profile = TargetResolver.GetProfile(arch);
		Assert.Null(profile.CreateRomBuilder(null!));
	}

	// ========================================================================
	// Encoder Integration — Verify Key Instructions Encode
	// ========================================================================

	[Fact]
	public void Encoder_Mos6502_EncodesNop() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.MOS6502).Encoder;
		Assert.True(encoder.TryEncode("nop", AddressingMode.Implied, out var enc));
		Assert.Equal(0xea, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_Mos6502_EncodesLdaImmediate() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.MOS6502).Encoder;
		Assert.True(encoder.TryEncode("lda", AddressingMode.Immediate, out var enc));
		Assert.Equal(0xa9, enc.Opcode);
		Assert.Equal(2, enc.Size);
	}

	[Fact]
	public void Encoder_Wdc65816_EncodesNop() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.WDC65816).Encoder;
		Assert.True(encoder.TryEncode("nop", AddressingMode.Implied, out var enc));
		Assert.Equal(0xea, enc.Opcode);
	}

	[Fact]
	public void Encoder_Sm83_EncodesNop() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.SM83).Encoder;
		Assert.True(encoder.TryEncode("nop", AddressingMode.Implied, out var enc));
		Assert.Equal(0x00, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_Mos6502_RejectsInvalidMnemonic() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.MOS6502).Encoder;
		Assert.False(encoder.TryEncode("xyz", AddressingMode.Implied, out _));
	}

	// ========================================================================
	// IsBranchInstruction — Profile Delegation
	// ========================================================================

	[Theory]
	[InlineData("bcc")]
	[InlineData("bcs")]
	[InlineData("beq")]
	[InlineData("bne")]
	[InlineData("bmi")]
	[InlineData("bpl")]
	[InlineData("bvc")]
	[InlineData("bvs")]
	public void IsBranch_Mos6502_StandardBranches_ReturnTrue(string mnemonic) {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.MOS6502).Encoder;
		Assert.True(encoder.IsBranchInstruction(mnemonic));
	}

	[Theory]
	[InlineData("lda")]
	[InlineData("sta")]
	[InlineData("nop")]
	[InlineData("jmp")]
	[InlineData("jsr")]
	public void IsBranch_Mos6502_NonBranches_ReturnFalse(string mnemonic) {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.MOS6502).Encoder;
		Assert.False(encoder.IsBranchInstruction(mnemonic));
	}

	// ========================================================================
	// Mnemonics Count — Sanity Checks
	// ========================================================================

	[Fact]
	public void Mnemonics_6502_HasAtLeast30() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.MOS6502).Encoder;
		Assert.True(encoder.Mnemonics.Count >= 30, $"6502 should have 30+ mnemonics, got {encoder.Mnemonics.Count}");
	}

	[Fact]
	public void Mnemonics_65816_HasMoreThan6502() {
		var enc6502 = TargetResolver.GetProfile(TargetArchitecture.MOS6502).Encoder;
		var enc65816 = TargetResolver.GetProfile(TargetArchitecture.WDC65816).Encoder;
		Assert.True(enc65816.Mnemonics.Count > enc6502.Mnemonics.Count,
			$"65816 ({enc65816.Mnemonics.Count}) should have more mnemonics than 6502 ({enc6502.Mnemonics.Count})");
	}

	[Fact]
	public void Mnemonics_Sm83_HasAtLeast20() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.SM83).Encoder;
		Assert.True(encoder.Mnemonics.Count >= 20, $"SM83 should have 20+ mnemonics, got {encoder.Mnemonics.Count}");
	}

	[Fact]
	public void Mnemonics_Z80_HasAtLeast40() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.Z80).Encoder;
		Assert.True(encoder.Mnemonics.Count >= 40, $"Z80 should have 40+ mnemonics, got {encoder.Mnemonics.Count}");
	}

	[Fact]
	public void Mnemonics_M68000_HasAtLeast50() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.M68000).Encoder;
		Assert.True(encoder.Mnemonics.Count >= 50, $"M68000 should have 50+ mnemonics, got {encoder.Mnemonics.Count}");
	}

	// ========================================================================
	// Mnemonics — Contains Known Instructions
	// ========================================================================

	[Theory]
	[InlineData(TargetArchitecture.MOS6502, "lda")]
	[InlineData(TargetArchitecture.MOS6502, "sta")]
	[InlineData(TargetArchitecture.MOS6502, "jmp")]
	[InlineData(TargetArchitecture.MOS6502, "nop")]
	[InlineData(TargetArchitecture.WDC65816, "rep")]
	[InlineData(TargetArchitecture.WDC65816, "sep")]
	[InlineData(TargetArchitecture.WDC65816, "xba")]
	[InlineData(TargetArchitecture.SM83, "halt")]
	[InlineData(TargetArchitecture.SM83, "stop")]
	[InlineData(TargetArchitecture.Z80, "djnz")]
	[InlineData(TargetArchitecture.Z80, "ldir")]
	[InlineData(TargetArchitecture.HuC6280, "tam")]
	[InlineData(TargetArchitecture.HuC6280, "tma")]
	public void Mnemonics_ContainsKnownInstructions(TargetArchitecture arch, string mnemonic) {
		var encoder = TargetResolver.GetProfile(arch).Encoder;
		Assert.Contains(mnemonic, encoder.Mnemonics);
	}
}
