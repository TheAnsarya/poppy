// ============================================================================
// InstructionSet65816Tests.cs - 65816 Instruction Set Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Parser;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for WDC 65816 (SNES) instruction encoding.
/// </summary>
public sealed class InstructionSet65816Tests {
	// ========================================================================
	// 6502-Compatible Instructions
	// ========================================================================

	[Theory]
	[InlineData("lda", AddressingMode.Immediate, 0xa9, 2)]
	[InlineData("lda", AddressingMode.ZeroPage, 0xa5, 2)]
	[InlineData("lda", AddressingMode.Absolute, 0xad, 3)]
	[InlineData("sta", AddressingMode.ZeroPage, 0x85, 2)]
	[InlineData("sta", AddressingMode.Absolute, 0x8d, 3)]
	public void BasicLoadStore_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// 65816-Specific Long Addressing
	// ========================================================================

	[Theory]
	[InlineData("lda", AddressingMode.AbsoluteLong, 0xaf, 4)]
	[InlineData("lda", AddressingMode.AbsoluteLongX, 0xbf, 4)]
	[InlineData("sta", AddressingMode.AbsoluteLong, 0x8f, 4)]
	[InlineData("sta", AddressingMode.AbsoluteLongX, 0x9f, 4)]
	[InlineData("adc", AddressingMode.AbsoluteLong, 0x6f, 4)]
	[InlineData("sbc", AddressingMode.AbsoluteLong, 0xef, 4)]
	public void LongAddressing_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// Stack Relative Addressing
	// ========================================================================

	[Theory]
	[InlineData("lda", AddressingMode.StackRelative, 0xa3, 2)]
	[InlineData("sta", AddressingMode.StackRelative, 0x83, 2)]
	[InlineData("adc", AddressingMode.StackRelative, 0x63, 2)]
	[InlineData("and", AddressingMode.StackRelative, 0x23, 2)]
	public void StackRelative_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// Direct Page Indirect Long
	// ========================================================================

	[Theory]
	[InlineData("lda", AddressingMode.DirectPageIndirectLong, 0xa7, 2)]
	[InlineData("lda", AddressingMode.DirectPageIndirectLongY, 0xb7, 2)]
	[InlineData("sta", AddressingMode.DirectPageIndirectLong, 0x87, 2)]
	[InlineData("sta", AddressingMode.DirectPageIndirectLongY, 0x97, 2)]
	public void DirectPageIndirectLong_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// REP/SEP - Processor Status
	// ========================================================================

	[Fact]
	public void Rep_HasCorrectEncoding() {
		var result = InstructionSet65816.TryGetEncoding("rep", AddressingMode.Immediate, out var encoding);

		Assert.True(result);
		Assert.Equal(0xc2, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
	}

	[Fact]
	public void Sep_HasCorrectEncoding() {
		var result = InstructionSet65816.TryGetEncoding("sep", AddressingMode.Immediate, out var encoding);

		Assert.True(result);
		Assert.Equal(0xe2, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
	}

	// ========================================================================
	// Long Jumps and Calls
	// ========================================================================

	[Theory]
	[InlineData("jml", AddressingMode.AbsoluteLong, 0x5c, 4)]
	[InlineData("jsl", AddressingMode.AbsoluteLong, 0x22, 4)]
	[InlineData("rtl", AddressingMode.Implied, 0x6b, 1)]
	public void LongJumpCall_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// Block Move Instructions
	// ========================================================================

	[Theory]
	[InlineData("mvp", 0x44)]
	[InlineData("mvn", 0x54)]
	public void BlockMove_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, AddressingMode.BlockMove, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(3, encoding.Size);
	}

	// ========================================================================
	// Push Effective Address
	// ========================================================================

	[Theory]
	[InlineData("pea", AddressingMode.Absolute, 0xf4, 3)]
	[InlineData("pei", AddressingMode.ZeroPage, 0xd4, 2)]
	[InlineData("per", AddressingMode.Relative, 0x62, 3)]
	public void PushEffectiveAddress_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// 65816 Push/Pull Extensions
	// ========================================================================

	[Theory]
	[InlineData("phx", 0xda)]
	[InlineData("phy", 0x5a)]
	[InlineData("plx", 0xfa)]
	[InlineData("ply", 0x7a)]
	[InlineData("phb", 0x8b)]
	[InlineData("phd", 0x0b)]
	[InlineData("phk", 0x4b)]
	[InlineData("plb", 0xab)]
	[InlineData("pld", 0x2b)]
	public void ExtendedPushPull_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// 65816 Transfer Extensions
	// ========================================================================

	[Theory]
	[InlineData("tcd", 0x5b)]
	[InlineData("tcs", 0x1b)]
	[InlineData("tdc", 0x7b)]
	[InlineData("tsc", 0x3b)]
	[InlineData("txy", 0x9b)]
	[InlineData("tyx", 0xbb)]
	public void ExtendedTransfer_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// Test and Reset/Set Bits
	// ========================================================================

	[Theory]
	[InlineData("trb", AddressingMode.ZeroPage, 0x14, 2)]
	[InlineData("trb", AddressingMode.Absolute, 0x1c, 3)]
	[InlineData("tsb", AddressingMode.ZeroPage, 0x04, 2)]
	[InlineData("tsb", AddressingMode.Absolute, 0x0c, 3)]
	public void TestBits_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// Store Zero
	// ========================================================================

	[Theory]
	[InlineData("stz", AddressingMode.ZeroPage, 0x64, 2)]
	[InlineData("stz", AddressingMode.ZeroPageX, 0x74, 2)]
	[InlineData("stz", AddressingMode.Absolute, 0x9c, 3)]
	[InlineData("stz", AddressingMode.AbsoluteX, 0x9e, 3)]
	public void StoreZero_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// Special 65816 Instructions
	// ========================================================================

	[Theory]
	[InlineData("xba", 0xeb)]
	[InlineData("xce", 0xfb)]
	[InlineData("wai", 0xcb)]
	[InlineData("stp", 0xdb)]
	public void SpecialInstructions_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// Branch Instructions
	// ========================================================================

	[Theory]
	[InlineData("bra", AddressingMode.Relative, 0x80, 2)]
	[InlineData("brl", AddressingMode.Relative, 0x82, 3)]
	public void ExtendedBranch_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSet65816.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	// ========================================================================
	// Helper Methods
	// ========================================================================

	[Fact]
	public void GetAllMnemonics_ReturnsNonEmpty() {
		var mnemonics = InstructionSet65816.GetAllMnemonics().ToList();

		Assert.NotEmpty(mnemonics);
		Assert.Contains("lda", mnemonics, StringComparer.OrdinalIgnoreCase);
		Assert.Contains("jml", mnemonics, StringComparer.OrdinalIgnoreCase);
		Assert.Contains("rep", mnemonics, StringComparer.OrdinalIgnoreCase);
	}

	[Fact]
	public void IsBranchInstruction_IdentifiesBranches() {
		Assert.True(InstructionSet65816.IsBranchInstruction("bcc"));
		Assert.True(InstructionSet65816.IsBranchInstruction("bra"));
		Assert.True(InstructionSet65816.IsBranchInstruction("brl"));
		Assert.False(InstructionSet65816.IsBranchInstruction("jmp"));
		Assert.False(InstructionSet65816.IsBranchInstruction("jml"));
	}

	[Fact]
	public void Is65816Only_IdentifiesNewInstructions() {
		Assert.True(InstructionSet65816.Is65816Only("rep"));
		Assert.True(InstructionSet65816.Is65816Only("sep"));
		Assert.True(InstructionSet65816.Is65816Only("jml"));
		Assert.True(InstructionSet65816.Is65816Only("mvp"));
		Assert.False(InstructionSet65816.Is65816Only("lda"));
		Assert.False(InstructionSet65816.Is65816Only("sta"));
	}
}

