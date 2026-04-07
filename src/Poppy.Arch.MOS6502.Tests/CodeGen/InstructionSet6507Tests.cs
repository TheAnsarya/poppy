// ============================================================================
// InstructionSet6507Tests.cs - 6507 Instruction Set Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Arch.MOS6502;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;

namespace Poppy.Arch.MOS6502.Tests.CodeGen;

/// <summary>
/// Tests for MOS 6507 (Atari 2600) instruction encoding.
/// The 6507 delegates entirely to the 6502 instruction set —
/// these tests verify correct delegation and encoding parity.
/// </summary>
public sealed class InstructionSet6507Tests {
	// ========================================================================
	// Core Instruction Encoding (delegates to 6502)
	// ========================================================================

	[Theory]
	[InlineData("lda", AddressingMode.Immediate, 0xa9, 2)]
	[InlineData("lda", AddressingMode.ZeroPage, 0xa5, 2)]
	[InlineData("lda", AddressingMode.Absolute, 0xad, 3)]
	[InlineData("lda", AddressingMode.IndexedIndirect, 0xa1, 2)]
	[InlineData("lda", AddressingMode.IndirectIndexed, 0xb1, 2)]
	[InlineData("sta", AddressingMode.ZeroPage, 0x85, 2)]
	[InlineData("sta", AddressingMode.Absolute, 0x8d, 3)]
	[InlineData("ldx", AddressingMode.Immediate, 0xa2, 2)]
	[InlineData("ldy", AddressingMode.Immediate, 0xa0, 2)]
	public void LoadStore_MatcheS6502(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6507.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	[Theory]
	[InlineData("adc", AddressingMode.Immediate, 0x69, 2)]
	[InlineData("sbc", AddressingMode.Immediate, 0xe9, 2)]
	[InlineData("and", AddressingMode.Immediate, 0x29, 2)]
	[InlineData("ora", AddressingMode.Immediate, 0x09, 2)]
	[InlineData("eor", AddressingMode.Immediate, 0x49, 2)]
	[InlineData("cmp", AddressingMode.Immediate, 0xc9, 2)]
	public void Arithmetic_Matches6502(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6507.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	[Theory]
	[InlineData("nop", AddressingMode.Implied, 0xea, 1)]
	[InlineData("brk", AddressingMode.Implied, 0x00, 1)]
	[InlineData("rts", AddressingMode.Implied, 0x60, 1)]
	[InlineData("rti", AddressingMode.Implied, 0x40, 1)]
	[InlineData("pha", AddressingMode.Implied, 0x48, 1)]
	[InlineData("pla", AddressingMode.Implied, 0x68, 1)]
	[InlineData("tax", AddressingMode.Implied, 0xaa, 1)]
	[InlineData("txa", AddressingMode.Implied, 0x8a, 1)]
	[InlineData("inx", AddressingMode.Implied, 0xe8, 1)]
	[InlineData("dex", AddressingMode.Implied, 0xca, 1)]
	public void ImpliedOps_Matches6502(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet6507.TryGetEncoding(mnemonic, mode, out var encoding);

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
	public void Branch_Matches6502(string mnemonic, byte opcode) {
		var result = InstructionSet6507.TryGetEncoding(mnemonic, AddressingMode.Relative, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
	}

	[Fact]
	public void JMP_Absolute_Matches6502() {
		var result = InstructionSet6507.TryGetEncoding("jmp", AddressingMode.Absolute, out var encoding);

		Assert.True(result);
		Assert.Equal(0x4c, encoding.Opcode);
		Assert.Equal(3, encoding.Size);
	}

	[Fact]
	public void JSR_Matches6502() {
		var result = InstructionSet6507.TryGetEncoding("jsr", AddressingMode.Absolute, out var encoding);

		Assert.True(result);
		Assert.Equal(0x20, encoding.Opcode);
		Assert.Equal(3, encoding.Size);
	}

	// ========================================================================
	// 6502 Parity — Encoding values must be identical
	// ========================================================================

	[Theory]
	[InlineData("lda", AddressingMode.Immediate)]
	[InlineData("lda", AddressingMode.ZeroPage)]
	[InlineData("lda", AddressingMode.Absolute)]
	[InlineData("sta", AddressingMode.ZeroPage)]
	[InlineData("jmp", AddressingMode.Absolute)]
	[InlineData("jmp", AddressingMode.Indirect)]
	[InlineData("nop", AddressingMode.Implied)]
	[InlineData("asl", AddressingMode.Accumulator)]
	[InlineData("ror", AddressingMode.ZeroPage)]
	public void EncodingValues_IdenticalTo6502(string mnemonic, AddressingMode mode) {
		InstructionSet6502.TryGetEncoding(mnemonic, mode, out var enc6502);
		InstructionSet6507.TryGetEncoding(mnemonic, mode, out var enc6507);

		Assert.Equal(enc6502.Opcode, enc6507.Opcode);
		Assert.Equal(enc6502.Size, enc6507.Size);
	}

	// ========================================================================
	// Case Insensitivity
	// ========================================================================

	[Theory]
	[InlineData("LDA", AddressingMode.Immediate, 0xa9)]
	[InlineData("Nop", AddressingMode.Implied, 0xea)]
	public void TryGetEncoding_IsCaseInsensitive(string mnemonic, AddressingMode mode, byte expectedOpcode) {
		var result = InstructionSet6507.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(expectedOpcode, encoding.Opcode);
	}

	// ========================================================================
	// Invalid Lookups
	// ========================================================================

	[Theory]
	[InlineData("xyz", AddressingMode.Implied)]
	[InlineData("lda", AddressingMode.BlockMove)]
	public void TryGetEncoding_InvalidInstruction_ReturnsFalse(string mnemonic, AddressingMode mode) {
		var result = InstructionSet6507.TryGetEncoding(mnemonic, mode, out _);

		Assert.False(result);
	}

	// ========================================================================
	// GetAllMnemonics
	// ========================================================================

	[Fact]
	public void GetAllMnemonics_ReturnsNonEmpty() {
		var mnemonics = InstructionSet6507.GetAllMnemonics().ToList();

		Assert.NotEmpty(mnemonics);
		Assert.True(mnemonics.Count >= 50, $"Expected at least 50 mnemonics, got {mnemonics.Count}");
	}

	[Fact]
	public void GetAllMnemonics_MatcheS6502() {
		var mnemonics6502 = InstructionSet6502.GetAllMnemonics()
			.Select(m => m.ToLowerInvariant())
			.OrderBy(m => m)
			.ToList();
		var mnemonics6507 = InstructionSet6507.GetAllMnemonics()
			.Select(m => m.ToLowerInvariant())
			.OrderBy(m => m)
			.ToList();

		Assert.Equal(mnemonics6502, mnemonics6507);
	}

	[Theory]
	[InlineData("lda")]
	[InlineData("sta")]
	[InlineData("jmp")]
	[InlineData("jsr")]
	[InlineData("nop")]
	public void GetAllMnemonics_ContainsExpectedMnemonics(string expected) {
		var mnemonics = InstructionSet6507.GetAllMnemonics().ToList();

		Assert.Contains(expected, mnemonics, StringComparer.OrdinalIgnoreCase);
	}

	// ========================================================================
	// GetSupportedModes
	// ========================================================================

	[Fact]
	public void GetSupportedModes_LDA_Matches6502() {
		var modes6502 = InstructionSet6502.GetSupportedModes("lda").OrderBy(m => m).ToList();
		var modes6507 = InstructionSet6507.GetSupportedModes("lda").OrderBy(m => m).ToList();

		Assert.Equal(modes6502, modes6507);
	}

	[Fact]
	public void GetSupportedModes_NOP_ReturnsImplied() {
		var modes = InstructionSet6507.GetSupportedModes("nop").ToList();

		Assert.Single(modes);
		Assert.Contains(AddressingMode.Implied, modes);
	}

	[Fact]
	public void GetSupportedModes_UnknownMnemonic_ReturnsEmpty() {
		var modes = InstructionSet6507.GetSupportedModes("xyz").ToList();

		Assert.Empty(modes);
	}
}
