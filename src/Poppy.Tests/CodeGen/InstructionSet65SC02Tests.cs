// ============================================================================
// InstructionSet65SC02Tests.cs - 65SC02 Instruction Set Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Arch.MOS6502;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for WDC 65SC02 (Atari Lynx) instruction encoding.
/// The 65SC02 extends the 6502 with additional instructions and addressing modes.
/// </summary>
public sealed class InstructionSet65SC02Tests {
	// ========================================================================
	// 65SC02-Specific New Instructions
	// ========================================================================

	[Fact]
	public void BRA_HasCorrectEncoding() {
		var result = InstructionSet65SC02.TryGetEncoding("bra", AddressingMode.Relative, out var encoding);

		Assert.True(result);
		Assert.Equal(0x80, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
	}

	[Theory]
	[InlineData("phx", 0xda)]
	[InlineData("phy", 0x5a)]
	[InlineData("plx", 0xfa)]
	[InlineData("ply", 0x7a)]
	public void StackExtensions_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSet65SC02.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	[Theory]
	[InlineData("stz", AddressingMode.ZeroPage, 0x64, 2)]
	[InlineData("stz", AddressingMode.ZeroPageX, 0x74, 2)]
	[InlineData("stz", AddressingMode.Absolute, 0x9c, 3)]
	[InlineData("stz", AddressingMode.AbsoluteX, 0x9e, 3)]
	public void STZ_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65SC02.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	[Theory]
	[InlineData("trb", AddressingMode.ZeroPage, 0x14, 2)]
	[InlineData("trb", AddressingMode.Absolute, 0x1c, 3)]
	[InlineData("tsb", AddressingMode.ZeroPage, 0x04, 2)]
	[InlineData("tsb", AddressingMode.Absolute, 0x0c, 3)]
	public void TRB_TSB_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65SC02.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// INC/DEC Accumulator Mode (65SC02 addition)
	// ========================================================================

	[Fact]
	public void INC_Accumulator_HasCorrectEncoding() {
		var result = InstructionSet65SC02.TryGetEncoding("inc", AddressingMode.Accumulator, out var encoding);

		Assert.True(result);
		Assert.Equal(0x1a, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	[Fact]
	public void DEC_Accumulator_HasCorrectEncoding() {
		var result = InstructionSet65SC02.TryGetEncoding("dec", AddressingMode.Accumulator, out var encoding);

		Assert.True(result);
		Assert.Equal(0x3a, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// New BIT Addressing Modes (65SC02 addition)
	// ========================================================================

	[Theory]
	[InlineData(AddressingMode.Immediate, 0x89, 2)]
	[InlineData(AddressingMode.ZeroPageX, 0x34, 2)]
	[InlineData(AddressingMode.AbsoluteX, 0x3c, 3)]
	public void BIT_NewModes_HasCorrectEncoding(AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65SC02.TryGetEncoding("bit", mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// JMP Absolute Indexed Indirect (65SC02 fixes JMP bug)
	// ========================================================================

	[Fact]
	public void JMP_AbsoluteIndexedIndirect_HasCorrectEncoding() {
		var result = InstructionSet65SC02.TryGetEncoding("jmp", AddressingMode.AbsoluteIndexedIndirect, out var encoding);

		Assert.True(result);
		Assert.Equal(0x7c, encoding.Opcode);
		Assert.Equal(3, encoding.Size);
	}

	// ========================================================================
	// ZeroPage Indirect Mode (65SC02 addition for many instructions)
	// ========================================================================

	[Theory]
	[InlineData("adc", 0x72)]
	[InlineData("and", 0x32)]
	[InlineData("cmp", 0xd2)]
	[InlineData("eor", 0x52)]
	[InlineData("lda", 0xb2)]
	[InlineData("ora", 0x12)]
	[InlineData("sbc", 0xf2)]
	[InlineData("sta", 0x92)]
	public void ZeroPageIndirect_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSet65SC02.TryGetEncoding(mnemonic, AddressingMode.ZeroPageIndirect, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
	}

	// ========================================================================
	// 6502 Backward Compatibility (fallback to 6502 encoding)
	// ========================================================================

	[Theory]
	[InlineData("lda", AddressingMode.Immediate, 0xa9, 2)]
	[InlineData("lda", AddressingMode.ZeroPage, 0xa5, 2)]
	[InlineData("lda", AddressingMode.Absolute, 0xad, 3)]
	[InlineData("sta", AddressingMode.ZeroPage, 0x85, 2)]
	[InlineData("sta", AddressingMode.Absolute, 0x8d, 3)]
	[InlineData("nop", AddressingMode.Implied, 0xea, 1)]
	[InlineData("jmp", AddressingMode.Absolute, 0x4c, 3)]
	[InlineData("jmp", AddressingMode.Indirect, 0x6c, 3)]
	[InlineData("jsr", AddressingMode.Absolute, 0x20, 3)]
	[InlineData("rts", AddressingMode.Implied, 0x60, 1)]
	public void FallbackTo6502_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65SC02.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	[Theory]
	[InlineData("bcc", 0x90)]
	[InlineData("bcs", 0xb0)]
	[InlineData("beq", 0xf0)]
	[InlineData("bne", 0xd0)]
	[InlineData("bmi", 0x30)]
	[InlineData("bpl", 0x10)]
	public void Branch6502_FallbackWorks(string mnemonic, byte opcode) {
		var result = InstructionSet65SC02.TryGetEncoding(mnemonic, AddressingMode.Relative, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
	}

	// ========================================================================
	// IsBranchInstruction
	// ========================================================================

	[Theory]
	[InlineData("bcc", true)]
	[InlineData("bcs", true)]
	[InlineData("beq", true)]
	[InlineData("bmi", true)]
	[InlineData("bne", true)]
	[InlineData("bpl", true)]
	[InlineData("bvc", true)]
	[InlineData("bvs", true)]
	[InlineData("bra", true)]
	public void IsBranchInstruction_ReturnsTrueForBranches(string mnemonic, bool expected) {
		Assert.Equal(expected, InstructionSet65SC02.IsBranchInstruction(mnemonic));
	}

	[Theory]
	[InlineData("jmp")]
	[InlineData("jsr")]
	[InlineData("lda")]
	[InlineData("nop")]
	[InlineData("rts")]
	public void IsBranchInstruction_ReturnsFalseForNonBranches(string mnemonic) {
		Assert.False(InstructionSet65SC02.IsBranchInstruction(mnemonic));
	}

	[Theory]
	[InlineData("BRA")]
	[InlineData("BCC")]
	[InlineData("Beq")]
	public void IsBranchInstruction_IsCaseInsensitive(string mnemonic) {
		Assert.True(InstructionSet65SC02.IsBranchInstruction(mnemonic));
	}

	// ========================================================================
	// Case Insensitivity
	// ========================================================================

	[Theory]
	[InlineData("BRA", AddressingMode.Relative, 0x80)]
	[InlineData("Phx", AddressingMode.Implied, 0xda)]
	[InlineData("STZ", AddressingMode.ZeroPage, 0x64)]
	public void TryGetEncoding_IsCaseInsensitive(string mnemonic, AddressingMode mode, byte expectedOpcode) {
		var result = InstructionSet65SC02.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(expectedOpcode, encoding.Opcode);
	}

	// ========================================================================
	// Invalid Lookups
	// ========================================================================

	[Theory]
	[InlineData("xyz", AddressingMode.Implied)]
	[InlineData("bra", AddressingMode.Absolute)]
	[InlineData("phx", AddressingMode.Immediate)]
	public void TryGetEncoding_InvalidInstruction_ReturnsFalse(string mnemonic, AddressingMode mode) {
		var result = InstructionSet65SC02.TryGetEncoding(mnemonic, mode, out _);

		Assert.False(result);
	}

	// ========================================================================
	// GetAllMnemonics
	// ========================================================================

	[Fact]
	public void GetAllMnemonics_ReturnsNonEmpty() {
		var mnemonics = InstructionSet65SC02.GetAllMnemonics().ToList();

		Assert.NotEmpty(mnemonics);
	}

	[Fact]
	public void GetAllMnemonics_HasMoreThan6502() {
		var count6502 = InstructionSet6502.GetAllMnemonics().Count();
		var count65SC02 = InstructionSet65SC02.GetAllMnemonics().Count();

		Assert.True(count65SC02 > count6502,
			$"65SC02 ({count65SC02}) should have more mnemonics than 6502 ({count6502})");
	}

	[Theory]
	[InlineData("bra")]
	[InlineData("phx")]
	[InlineData("phy")]
	[InlineData("plx")]
	[InlineData("ply")]
	[InlineData("stz")]
	[InlineData("trb")]
	[InlineData("tsb")]
	public void GetAllMnemonics_Contains65SC02Additions(string expected) {
		var mnemonics = InstructionSet65SC02.GetAllMnemonics().ToList();

		Assert.Contains(expected, mnemonics, StringComparer.OrdinalIgnoreCase);
	}

	[Theory]
	[InlineData("lda")]
	[InlineData("sta")]
	[InlineData("jmp")]
	[InlineData("nop")]
	public void GetAllMnemonics_Contains6502Mnemonics(string expected) {
		var mnemonics = InstructionSet65SC02.GetAllMnemonics().ToList();

		Assert.Contains(expected, mnemonics, StringComparer.OrdinalIgnoreCase);
	}

	[Fact]
	public void GetAllMnemonics_NoDuplicates() {
		var mnemonics = InstructionSet65SC02.GetAllMnemonics().ToList();
		var distinct = mnemonics.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

		Assert.Equal(distinct.Count, mnemonics.Count);
	}

	// ========================================================================
	// GetSupportedModes
	// ========================================================================

	[Fact]
	public void GetSupportedModes_LDA_IncludesZeroPageIndirect() {
		var modes = InstructionSet65SC02.GetSupportedModes("lda").ToList();

		// 65SC02 adds ZeroPageIndirect to LDA
		Assert.Contains(AddressingMode.ZeroPageIndirect, modes);
		// Also has all 6502 modes
		Assert.Contains(AddressingMode.Immediate, modes);
		Assert.Contains(AddressingMode.ZeroPage, modes);
		Assert.Contains(AddressingMode.Absolute, modes);
	}

	[Fact]
	public void GetSupportedModes_BIT_HasExtendedModes() {
		var modes = InstructionSet65SC02.GetSupportedModes("bit").ToList();

		// 65SC02 adds Immediate, ZeroPageX, AbsoluteX to BIT
		Assert.Contains(AddressingMode.Immediate, modes);
		Assert.Contains(AddressingMode.ZeroPageX, modes);
		Assert.Contains(AddressingMode.AbsoluteX, modes);
		// Also has 6502 modes
		Assert.Contains(AddressingMode.ZeroPage, modes);
		Assert.Contains(AddressingMode.Absolute, modes);
	}

	[Fact]
	public void GetSupportedModes_BRA_ReturnsRelative() {
		var modes = InstructionSet65SC02.GetSupportedModes("bra").ToList();

		Assert.Single(modes);
		Assert.Contains(AddressingMode.Relative, modes);
	}

	[Fact]
	public void GetSupportedModes_UnknownMnemonic_ReturnsEmpty() {
		var modes = InstructionSet65SC02.GetSupportedModes("xyz").ToList();

		Assert.Empty(modes);
	}
}
