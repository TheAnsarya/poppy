// ============================================================================
// InstructionSet6502Tests.cs - 6502 Instruction Set Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Parser;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for MOS 6502 instruction encoding.
/// </summary>
public sealed class InstructionSet6502Tests {
	// ========================================================================
	// Load Instructions
	// ========================================================================

	[Theory]
	[InlineData("lda", AddressingMode.Immediate, 0xa9, 2)]
	[InlineData("lda", AddressingMode.ZeroPage, 0xa5, 2)]
	[InlineData("lda", AddressingMode.ZeroPageX, 0xb5, 2)]
	[InlineData("lda", AddressingMode.Absolute, 0xad, 3)]
	[InlineData("lda", AddressingMode.AbsoluteX, 0xbd, 3)]
	[InlineData("lda", AddressingMode.AbsoluteY, 0xb9, 3)]
	[InlineData("lda", AddressingMode.IndexedIndirect, 0xa1, 2)]
	[InlineData("lda", AddressingMode.IndirectIndexed, 0xb1, 2)]
	public void LDA_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	[Theory]
	[InlineData("ldx", AddressingMode.Immediate, 0xa2, 2)]
	[InlineData("ldx", AddressingMode.ZeroPage, 0xa6, 2)]
	[InlineData("ldx", AddressingMode.ZeroPageY, 0xb6, 2)]
	[InlineData("ldx", AddressingMode.Absolute, 0xae, 3)]
	[InlineData("ldx", AddressingMode.AbsoluteY, 0xbe, 3)]
	public void LDX_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	[Theory]
	[InlineData("ldy", AddressingMode.Immediate, 0xa0, 2)]
	[InlineData("ldy", AddressingMode.ZeroPage, 0xa4, 2)]
	[InlineData("ldy", AddressingMode.ZeroPageX, 0xb4, 2)]
	[InlineData("ldy", AddressingMode.Absolute, 0xac, 3)]
	[InlineData("ldy", AddressingMode.AbsoluteX, 0xbc, 3)]
	public void LDY_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// Store Instructions
	// ========================================================================

	[Theory]
	[InlineData("sta", AddressingMode.ZeroPage, 0x85, 2)]
	[InlineData("sta", AddressingMode.ZeroPageX, 0x95, 2)]
	[InlineData("sta", AddressingMode.Absolute, 0x8d, 3)]
	[InlineData("sta", AddressingMode.AbsoluteX, 0x9d, 3)]
	[InlineData("sta", AddressingMode.AbsoluteY, 0x99, 3)]
	[InlineData("sta", AddressingMode.IndexedIndirect, 0x81, 2)]
	[InlineData("sta", AddressingMode.IndirectIndexed, 0x91, 2)]
	public void STA_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	[Theory]
	[InlineData("stx", AddressingMode.ZeroPage, 0x86, 2)]
	[InlineData("stx", AddressingMode.ZeroPageY, 0x96, 2)]
	[InlineData("stx", AddressingMode.Absolute, 0x8e, 3)]
	public void STX_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	[Theory]
	[InlineData("sty", AddressingMode.ZeroPage, 0x84, 2)]
	[InlineData("sty", AddressingMode.ZeroPageX, 0x94, 2)]
	[InlineData("sty", AddressingMode.Absolute, 0x8c, 3)]
	public void STY_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// Arithmetic Instructions
	// ========================================================================

	[Theory]
	[InlineData("adc", AddressingMode.Immediate, 0x69, 2)]
	[InlineData("adc", AddressingMode.ZeroPage, 0x65, 2)]
	[InlineData("adc", AddressingMode.ZeroPageX, 0x75, 2)]
	[InlineData("adc", AddressingMode.Absolute, 0x6d, 3)]
	[InlineData("adc", AddressingMode.AbsoluteX, 0x7d, 3)]
	[InlineData("adc", AddressingMode.AbsoluteY, 0x79, 3)]
	[InlineData("adc", AddressingMode.IndexedIndirect, 0x61, 2)]
	[InlineData("adc", AddressingMode.IndirectIndexed, 0x71, 2)]
	public void ADC_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	[Theory]
	[InlineData("sbc", AddressingMode.Immediate, 0xe9, 2)]
	[InlineData("sbc", AddressingMode.ZeroPage, 0xe5, 2)]
	[InlineData("sbc", AddressingMode.ZeroPageX, 0xf5, 2)]
	[InlineData("sbc", AddressingMode.Absolute, 0xed, 3)]
	[InlineData("sbc", AddressingMode.AbsoluteX, 0xfd, 3)]
	[InlineData("sbc", AddressingMode.AbsoluteY, 0xf9, 3)]
	[InlineData("sbc", AddressingMode.IndexedIndirect, 0xe1, 2)]
	[InlineData("sbc", AddressingMode.IndirectIndexed, 0xf1, 2)]
	public void SBC_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// Logical Instructions
	// ========================================================================

	[Theory]
	[InlineData("and", AddressingMode.Immediate, 0x29, 2)]
	[InlineData("and", AddressingMode.ZeroPage, 0x25, 2)]
	[InlineData("and", AddressingMode.Absolute, 0x2d, 3)]
	[InlineData("eor", AddressingMode.Immediate, 0x49, 2)]
	[InlineData("eor", AddressingMode.ZeroPage, 0x45, 2)]
	[InlineData("eor", AddressingMode.Absolute, 0x4d, 3)]
	[InlineData("ora", AddressingMode.Immediate, 0x09, 2)]
	[InlineData("ora", AddressingMode.ZeroPage, 0x05, 2)]
	[InlineData("ora", AddressingMode.Absolute, 0x0d, 3)]
	public void LogicalOps_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// Compare Instructions
	// ========================================================================

	[Theory]
	[InlineData("cmp", AddressingMode.Immediate, 0xc9, 2)]
	[InlineData("cmp", AddressingMode.ZeroPage, 0xc5, 2)]
	[InlineData("cmp", AddressingMode.Absolute, 0xcd, 3)]
	[InlineData("cpx", AddressingMode.Immediate, 0xe0, 2)]
	[InlineData("cpx", AddressingMode.ZeroPage, 0xe4, 2)]
	[InlineData("cpx", AddressingMode.Absolute, 0xec, 3)]
	[InlineData("cpy", AddressingMode.Immediate, 0xc0, 2)]
	[InlineData("cpy", AddressingMode.ZeroPage, 0xc4, 2)]
	[InlineData("cpy", AddressingMode.Absolute, 0xcc, 3)]
	public void Compare_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// Increment / Decrement
	// ========================================================================

	[Theory]
	[InlineData("inc", AddressingMode.ZeroPage, 0xe6, 2)]
	[InlineData("inc", AddressingMode.ZeroPageX, 0xf6, 2)]
	[InlineData("inc", AddressingMode.Absolute, 0xee, 3)]
	[InlineData("inc", AddressingMode.AbsoluteX, 0xfe, 3)]
	[InlineData("dec", AddressingMode.ZeroPage, 0xc6, 2)]
	[InlineData("dec", AddressingMode.ZeroPageX, 0xd6, 2)]
	[InlineData("dec", AddressingMode.Absolute, 0xce, 3)]
	[InlineData("dec", AddressingMode.AbsoluteX, 0xde, 3)]
	public void IncDec_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	[Theory]
	[InlineData("inx", 0xe8)]
	[InlineData("iny", 0xc8)]
	[InlineData("dex", 0xca)]
	[InlineData("dey", 0x88)]
	public void IncDecRegister_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// Shift / Rotate Instructions
	// ========================================================================

	[Theory]
	[InlineData("asl", AddressingMode.Accumulator, 0x0a, 1)]
	[InlineData("asl", AddressingMode.ZeroPage, 0x06, 2)]
	[InlineData("asl", AddressingMode.ZeroPageX, 0x16, 2)]
	[InlineData("asl", AddressingMode.Absolute, 0x0e, 3)]
	[InlineData("asl", AddressingMode.AbsoluteX, 0x1e, 3)]
	[InlineData("lsr", AddressingMode.Accumulator, 0x4a, 1)]
	[InlineData("lsr", AddressingMode.ZeroPage, 0x46, 2)]
	[InlineData("lsr", AddressingMode.Absolute, 0x4e, 3)]
	[InlineData("rol", AddressingMode.Accumulator, 0x2a, 1)]
	[InlineData("rol", AddressingMode.ZeroPage, 0x26, 2)]
	[InlineData("rol", AddressingMode.Absolute, 0x2e, 3)]
	[InlineData("ror", AddressingMode.Accumulator, 0x6a, 1)]
	[InlineData("ror", AddressingMode.ZeroPage, 0x66, 2)]
	[InlineData("ror", AddressingMode.Absolute, 0x6e, 3)]
	public void ShiftRotate_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	[Theory]
	[InlineData("asl", 0x0a)]
	[InlineData("lsr", 0x4a)]
	[InlineData("rol", 0x2a)]
	[InlineData("ror", 0x6a)]
	public void ShiftRotate_ImpliedAlias_HasCorrectEncoding(string mnemonic, byte opcode) {
		// ASL/LSR/ROL/ROR with implied mode should encode same as accumulator
		var result = InstructionSet6502.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// Branch Instructions
	// ========================================================================

	[Theory]
	[InlineData("bcc", 0x90)]
	[InlineData("bcs", 0xb0)]
	[InlineData("beq", 0xf0)]
	[InlineData("bmi", 0x30)]
	[InlineData("bne", 0xd0)]
	[InlineData("bpl", 0x10)]
	[InlineData("bvc", 0x50)]
	[InlineData("bvs", 0x70)]
	public void Branch_Relative_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, AddressingMode.Relative, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
	}

	[Theory]
	[InlineData("bcc", 0x90)]
	[InlineData("bcs", 0xb0)]
	[InlineData("beq", 0xf0)]
	[InlineData("bne", 0xd0)]
	public void Branch_AbsoluteAlias_HasCorrectEncoding(string mnemonic, byte opcode) {
		// Branches registered with Absolute mode as alias for Relative
		var result = InstructionSet6502.TryGetEncoding(mnemonic, AddressingMode.Absolute, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
	}

	// ========================================================================
	// Jump / Subroutine Instructions
	// ========================================================================

	[Fact]
	public void JMP_Absolute_HasCorrectEncoding() {
		var result = InstructionSet6502.TryGetEncoding("jmp", AddressingMode.Absolute, out var encoding);

		Assert.True(result);
		Assert.Equal(0x4c, encoding.Opcode);
		Assert.Equal(3, encoding.Size);
	}

	[Fact]
	public void JMP_Indirect_HasCorrectEncoding() {
		var result = InstructionSet6502.TryGetEncoding("jmp", AddressingMode.Indirect, out var encoding);

		Assert.True(result);
		Assert.Equal(0x6c, encoding.Opcode);
		Assert.Equal(3, encoding.Size);
	}

	[Fact]
	public void JSR_HasCorrectEncoding() {
		var result = InstructionSet6502.TryGetEncoding("jsr", AddressingMode.Absolute, out var encoding);

		Assert.True(result);
		Assert.Equal(0x20, encoding.Opcode);
		Assert.Equal(3, encoding.Size);
	}

	[Fact]
	public void RTS_HasCorrectEncoding() {
		var result = InstructionSet6502.TryGetEncoding("rts", AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(0x60, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	[Fact]
	public void RTI_HasCorrectEncoding() {
		var result = InstructionSet6502.TryGetEncoding("rti", AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(0x40, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// Stack Instructions
	// ========================================================================

	[Theory]
	[InlineData("pha", 0x48)]
	[InlineData("php", 0x08)]
	[InlineData("pla", 0x68)]
	[InlineData("plp", 0x28)]
	public void Stack_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// Transfer Instructions
	// ========================================================================

	[Theory]
	[InlineData("tax", 0xaa)]
	[InlineData("tay", 0xa8)]
	[InlineData("tsx", 0xba)]
	[InlineData("txa", 0x8a)]
	[InlineData("txs", 0x9a)]
	[InlineData("tya", 0x98)]
	public void Transfer_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// Flag Instructions
	// ========================================================================

	[Theory]
	[InlineData("clc", 0x18)]
	[InlineData("cld", 0xd8)]
	[InlineData("cli", 0x58)]
	[InlineData("clv", 0xb8)]
	[InlineData("sec", 0x38)]
	[InlineData("sed", 0xf8)]
	[InlineData("sei", 0x78)]
	public void FlagOps_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode: AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// Miscellaneous Instructions
	// ========================================================================

	[Fact]
	public void NOP_HasCorrectEncoding() {
		var result = InstructionSet6502.TryGetEncoding("nop", AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(0xea, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	[Fact]
	public void BRK_HasCorrectEncoding() {
		var result = InstructionSet6502.TryGetEncoding("brk", AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(0x00, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	[Theory]
	[InlineData("bit", AddressingMode.ZeroPage, 0x24, 2)]
	[InlineData("bit", AddressingMode.Absolute, 0x2c, 3)]
	public void BIT_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// Case Insensitivity
	// ========================================================================

	[Theory]
	[InlineData("LDA", AddressingMode.Immediate, 0xa9)]
	[InlineData("Lda", AddressingMode.Immediate, 0xa9)]
	[InlineData("NOP", AddressingMode.Implied, 0xea)]
	[InlineData("Jmp", AddressingMode.Absolute, 0x4c)]
	public void TryGetEncoding_IsCaseInsensitive(string mnemonic, AddressingMode mode, byte expectedOpcode) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(expectedOpcode, encoding.Opcode);
	}

	// ========================================================================
	// Unknown / Invalid Lookups
	// ========================================================================

	[Theory]
	[InlineData("xyz", AddressingMode.Implied)]
	[InlineData("lda", AddressingMode.BlockMove)]
	[InlineData("nop", AddressingMode.Immediate)]
	[InlineData("jsr", AddressingMode.ZeroPage)]
	public void TryGetEncoding_InvalidInstruction_ReturnsFalse(string mnemonic, AddressingMode mode) {
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out _);

		Assert.False(result);
	}

	// ========================================================================
	// GetAllMnemonics
	// ========================================================================

	[Fact]
	public void GetAllMnemonics_ReturnsNonEmpty() {
		var mnemonics = InstructionSet6502.GetAllMnemonics().ToList();

		Assert.NotEmpty(mnemonics);
		// The 6502 has 56 official mnemonics
		Assert.True(mnemonics.Count >= 50, $"Expected at least 50 mnemonics, got {mnemonics.Count}");
	}

	[Theory]
	[InlineData("lda")]
	[InlineData("sta")]
	[InlineData("jmp")]
	[InlineData("jsr")]
	[InlineData("rts")]
	[InlineData("nop")]
	[InlineData("brk")]
	[InlineData("adc")]
	[InlineData("sbc")]
	[InlineData("and")]
	[InlineData("ora")]
	[InlineData("eor")]
	public void GetAllMnemonics_ContainsExpectedMnemonics(string expected) {
		var mnemonics = InstructionSet6502.GetAllMnemonics().ToList();

		Assert.Contains(expected, mnemonics, StringComparer.OrdinalIgnoreCase);
	}

	[Fact]
	public void GetAllMnemonics_NoDuplicates() {
		var mnemonics = InstructionSet6502.GetAllMnemonics().ToList();
		var distinct = mnemonics.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

		Assert.Equal(distinct.Count, mnemonics.Count);
	}

	// ========================================================================
	// GetSupportedModes
	// ========================================================================

	[Fact]
	public void GetSupportedModes_LDA_ReturnsAllModes() {
		var modes = InstructionSet6502.GetSupportedModes("lda").ToList();

		Assert.Contains(AddressingMode.Immediate, modes);
		Assert.Contains(AddressingMode.ZeroPage, modes);
		Assert.Contains(AddressingMode.ZeroPageX, modes);
		Assert.Contains(AddressingMode.Absolute, modes);
		Assert.Contains(AddressingMode.AbsoluteX, modes);
		Assert.Contains(AddressingMode.AbsoluteY, modes);
		Assert.Contains(AddressingMode.IndexedIndirect, modes);
		Assert.Contains(AddressingMode.IndirectIndexed, modes);
	}

	[Fact]
	public void GetSupportedModes_NOP_ReturnsImplied() {
		var modes = InstructionSet6502.GetSupportedModes("nop").ToList();

		Assert.Single(modes);
		Assert.Contains(AddressingMode.Implied, modes);
	}

	[Fact]
	public void GetSupportedModes_JMP_ReturnsAbsoluteAndIndirect() {
		var modes = InstructionSet6502.GetSupportedModes("jmp").ToList();

		Assert.Contains(AddressingMode.Absolute, modes);
		Assert.Contains(AddressingMode.Indirect, modes);
	}

	[Fact]
	public void GetSupportedModes_UnknownMnemonic_ReturnsEmpty() {
		var modes = InstructionSet6502.GetSupportedModes("xyz").ToList();

		Assert.Empty(modes);
	}

	[Fact]
	public void GetSupportedModes_IsCaseInsensitive() {
		var modesLower = InstructionSet6502.GetSupportedModes("lda").ToHashSet();
		var modesUpper = InstructionSet6502.GetSupportedModes("LDA").ToHashSet();

		Assert.Equal(modesLower, modesUpper);
	}
}
