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
	// CreateRomBuilder — Verify All Profiles Return An Adapter
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
	public void CreateRomBuilder_AllProfiles_ReturnsAdapter(TargetArchitecture arch) {
		var profile = TargetResolver.GetProfile(arch);
		Assert.NotNull(profile.CreateRomBuilder(null!));
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

	[Fact]
	public void Mnemonics_SPC700_HasAtLeast20() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.SPC700).Encoder;
		Assert.True(encoder.Mnemonics.Count >= 20, $"SPC700 should have 20+ mnemonics, got {encoder.Mnemonics.Count}");
	}

	[Fact]
	public void Mnemonics_HuC6280_HasAtLeast30() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.HuC6280).Encoder;
		Assert.True(encoder.Mnemonics.Count >= 30, $"HuC6280 should have 30+ mnemonics, got {encoder.Mnemonics.Count}");
	}

	[Fact]
	public void Mnemonics_V30MZ_HasAtLeast40() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.V30MZ).Encoder;
		Assert.True(encoder.Mnemonics.Count >= 40, $"V30MZ should have 40+ mnemonics, got {encoder.Mnemonics.Count}");
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

	// ========================================================================
	// Encoder Integration — SPC700 (recently fixed encoder)
	// ========================================================================

	[Fact]
	public void Encoder_Spc700_EncodesNop() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.SPC700).Encoder;
		Assert.True(encoder.TryEncode("nop", AddressingMode.Implied, out var enc));
		Assert.Equal(0x00, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_Spc700_EncodesClrc() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.SPC700).Encoder;
		Assert.True(encoder.TryEncode("clrc", AddressingMode.Implied, out var enc));
		Assert.Equal(0x60, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_Spc700_EncodesSetc() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.SPC700).Encoder;
		Assert.True(encoder.TryEncode("setc", AddressingMode.Implied, out var enc));
		Assert.Equal(0x80, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_Spc700_RejectsInvalidMnemonic() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.SPC700).Encoder;
		Assert.False(encoder.TryEncode("xyz", AddressingMode.Implied, out _));
	}

	// ========================================================================
	// Encoder Integration — HuC6280 (65C02-compatible)
	// ========================================================================

	[Fact]
	public void Encoder_Huc6280_EncodesNop() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.HuC6280).Encoder;
		Assert.True(encoder.TryEncode("nop", AddressingMode.Implied, out var enc));
		Assert.Equal(0xea, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_Huc6280_EncodesLdaImmediate() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.HuC6280).Encoder;
		Assert.True(encoder.TryEncode("lda", AddressingMode.Immediate, out var enc));
		Assert.Equal(0xa9, enc.Opcode);
		Assert.Equal(2, enc.Size);
	}

	[Fact]
	public void Encoder_Huc6280_EncodesInx() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.HuC6280).Encoder;
		Assert.True(encoder.TryEncode("inx", AddressingMode.Implied, out var enc));
		Assert.Equal(0xe8, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_Huc6280_RejectsInvalidMnemonic() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.HuC6280).Encoder;
		Assert.False(encoder.TryEncode("xyz", AddressingMode.Implied, out _));
	}

	// ========================================================================
	// Encoder Integration — V30MZ (x86-compatible implied ops)
	// ========================================================================

	[Fact]
	public void Encoder_V30mz_EncodesNop() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.V30MZ).Encoder;
		Assert.True(encoder.TryEncode("nop", AddressingMode.Implied, out var enc));
		Assert.Equal(0x90, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_V30mz_EncodesHlt() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.V30MZ).Encoder;
		Assert.True(encoder.TryEncode("hlt", AddressingMode.Implied, out var enc));
		Assert.Equal(0xf4, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_V30mz_EncodesCli() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.V30MZ).Encoder;
		Assert.True(encoder.TryEncode("cli", AddressingMode.Implied, out var enc));
		Assert.Equal(0xfa, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_V30mz_RejectsInvalidMnemonic() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.V30MZ).Encoder;
		Assert.False(encoder.TryEncode("xyz", AddressingMode.Implied, out _));
	}

	// ========================================================================
	// Encoder Integration — Z80 (single-byte implied ops)
	// ========================================================================

	[Fact]
	public void Encoder_Z80_EncodesNop() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.Z80).Encoder;
		Assert.True(encoder.TryEncode("nop", AddressingMode.Implied, out var enc));
		Assert.Equal(0x00, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_Z80_EncodesHalt() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.Z80).Encoder;
		Assert.True(encoder.TryEncode("halt", AddressingMode.Implied, out var enc));
		Assert.Equal(0x76, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_Z80_EncodesDaa() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.Z80).Encoder;
		Assert.True(encoder.TryEncode("daa", AddressingMode.Implied, out var enc));
		Assert.Equal(0x27, enc.Opcode);
		Assert.Equal(1, enc.Size);
	}

	[Fact]
	public void Encoder_Z80_RejectsInvalidMnemonic() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.Z80).Encoder;
		Assert.False(encoder.TryEncode("xyz", AddressingMode.Implied, out _));
	}

	// ========================================================================
	// Encoder Integration — M68000 (16-bit opcodes, limited implied ops)
	// ========================================================================

	[Fact]
	public void Encoder_M68000_EncodesNop() {
		// M68000 NOP exists as a special implied instruction
		var encoder = TargetResolver.GetProfile(TargetArchitecture.M68000).Encoder;
		Assert.True(encoder.TryEncode("nop", AddressingMode.Implied, out var enc));
	}

	[Fact]
	public void Encoder_M68000_RejectsInvalidMnemonic() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.M68000).Encoder;
		Assert.False(encoder.TryEncode("xyz", AddressingMode.Implied, out _));
	}

	// ========================================================================
	// Encoder Integration — ARM7TDMI (stub encoder, all return false)
	// ========================================================================

	[Fact]
	public void Encoder_Arm7tdmi_StubReturnsFlase_ForImplied() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.ARM7TDMI).Encoder;
		Assert.False(encoder.TryEncode("nop", AddressingMode.Implied, out _));
	}

	[Fact]
	public void Encoder_Arm7tdmi_StubReturnsFlase_ForImmediate() {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.ARM7TDMI).Encoder;
		Assert.False(encoder.TryEncode("mov", AddressingMode.Immediate, out _));
	}

	// ========================================================================
	// IsBranchInstruction — Additional Architectures
	// ========================================================================

	[Theory]
	[InlineData("bra")]
	[InlineData("beq")]
	[InlineData("bne")]
	public void IsBranch_Spc700_BranchInstructions_ReturnTrue(string mnemonic) {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.SPC700).Encoder;
		Assert.True(encoder.IsBranchInstruction(mnemonic));
	}

	[Theory]
	[InlineData("nop")]
	[InlineData("clrc")]
	public void IsBranch_Spc700_NonBranches_ReturnFalse(string mnemonic) {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.SPC700).Encoder;
		Assert.False(encoder.IsBranchInstruction(mnemonic));
	}

	[Theory]
	[InlineData("bcc")]
	[InlineData("bcs")]
	[InlineData("beq")]
	[InlineData("bne")]
	[InlineData("bra")]
	public void IsBranch_Huc6280_BranchInstructions_ReturnTrue(string mnemonic) {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.HuC6280).Encoder;
		Assert.True(encoder.IsBranchInstruction(mnemonic));
	}

	[Theory]
	[InlineData("nop")]
	[InlineData("lda")]
	public void IsBranch_Huc6280_NonBranches_ReturnFalse(string mnemonic) {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.HuC6280).Encoder;
		Assert.False(encoder.IsBranchInstruction(mnemonic));
	}

	[Theory]
	[InlineData("jr")]
	[InlineData("djnz")]
	public void IsBranch_Z80_BranchInstructions_ReturnTrue(string mnemonic) {
		var encoder = TargetResolver.GetProfile(TargetArchitecture.Z80).Encoder;
		Assert.True(encoder.IsBranchInstruction(mnemonic));
	}
}
