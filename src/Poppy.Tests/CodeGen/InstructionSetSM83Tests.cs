// ============================================================================
// InstructionSetSM83Tests.cs - SM83 Instruction Set Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Parser;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for SM83 (Game Boy) instruction encoding.
/// </summary>
public sealed class InstructionSetSM83Tests {
	// ========================================================================
	// Basic Load Instructions
	// ========================================================================

	[Theory]
	[InlineData("ld a", AddressingMode.Immediate, 0x3e, 2)]
	[InlineData("ld b", AddressingMode.Immediate, 0x06, 2)]
	[InlineData("ld c", AddressingMode.Immediate, 0x0e, 2)]
	[InlineData("ld d", AddressingMode.Immediate, 0x16, 2)]
	[InlineData("ld e", AddressingMode.Immediate, 0x1e, 2)]
	[InlineData("ld h", AddressingMode.Immediate, 0x26, 2)]
	[InlineData("ld l", AddressingMode.Immediate, 0x2e, 2)]
	public void LoadImmediate_HasCorrectEncoding(string mnemonic, AddressingMode mode, byte opcode, int size) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, mode, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
		Assert.False(encoding.IsCBPrefixed);
	}

	[Theory]
	[InlineData("ld a,b", 0x78)]
	[InlineData("ld a,c", 0x79)]
	[InlineData("ld a,d", 0x7a)]
	[InlineData("ld a,e", 0x7b)]
	[InlineData("ld a,h", 0x7c)]
	[InlineData("ld a,l", 0x7d)]
	[InlineData("ld b,a", 0x47)]
	[InlineData("ld c,a", 0x4f)]
	[InlineData("ld d,a", 0x57)]
	[InlineData("ld e,a", 0x5f)]
	public void LoadRegisterToRegister_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// 16-Bit Load Instructions
	// ========================================================================

	[Theory]
	[InlineData("ld bc", 0x01, 3)]
	[InlineData("ld de", 0x11, 3)]
	[InlineData("ld hl", 0x21, 3)]
	[InlineData("ld sp", 0x31, 3)]
	public void Load16BitImmediate_HasCorrectEncoding(string mnemonic, byte opcode, int size) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Immediate, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(size, encoding.Size);
	}

	[Theory]
	[InlineData("push bc", 0xc5)]
	[InlineData("push de", 0xd5)]
	[InlineData("push hl", 0xe5)]
	[InlineData("push af", 0xf5)]
	[InlineData("pop bc", 0xc1)]
	[InlineData("pop de", 0xd1)]
	[InlineData("pop hl", 0xe1)]
	[InlineData("pop af", 0xf1)]
	public void PushPop_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// 8-Bit Arithmetic
	// ========================================================================

	[Theory]
	[InlineData("add a,b", 0x80)]
	[InlineData("add a,c", 0x81)]
	[InlineData("add a,(hl)", 0x86)]
	[InlineData("sub b", 0x90)]
	[InlineData("sub (hl)", 0x96)]
	[InlineData("and a", 0xa7)]
	[InlineData("or b", 0xb0)]
	[InlineData("xor a", 0xaf)]
	[InlineData("cp b", 0xb8)]
	public void Arithmetic8Bit_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	[Theory]
	[InlineData("inc a", 0x3c)]
	[InlineData("inc b", 0x04)]
	[InlineData("dec a", 0x3d)]
	[InlineData("dec b", 0x05)]
	public void IncDec8Bit_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// 16-Bit Arithmetic
	// ========================================================================

	[Theory]
	[InlineData("add hl,bc", 0x09)]
	[InlineData("add hl,de", 0x19)]
	[InlineData("add hl,hl", 0x29)]
	[InlineData("add hl,sp", 0x39)]
	public void AddHL_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	[Theory]
	[InlineData("inc bc", 0x03)]
	[InlineData("inc de", 0x13)]
	[InlineData("inc hl", 0x23)]
	[InlineData("inc sp", 0x33)]
	[InlineData("dec bc", 0x0b)]
	[InlineData("dec de", 0x1b)]
	[InlineData("dec hl", 0x2b)]
	[InlineData("dec sp", 0x3b)]
	public void IncDec16Bit_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// Jump/Call Instructions
	// ========================================================================

	[Fact]
	public void JpAbsolute_HasCorrectEncoding() {
		var result = InstructionSetSM83.TryGetEncoding("jp", AddressingMode.Absolute, out var encoding);

		Assert.True(result);
		Assert.Equal(0xc3, encoding.Opcode);
		Assert.Equal(3, encoding.Size);
	}

	[Theory]
	[InlineData("jp nz", 0xc2)]
	[InlineData("jp z", 0xca)]
	[InlineData("jp nc", 0xd2)]
	[InlineData("jp c", 0xda)]
	public void JpConditional_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Absolute, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(3, encoding.Size);
	}

	[Theory]
	[InlineData("jr", 0x18)]
	[InlineData("jr nz", 0x20)]
	[InlineData("jr z", 0x28)]
	[InlineData("jr nc", 0x30)]
	[InlineData("jr c", 0x38)]
	public void JrRelative_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Relative, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
	}

	[Fact]
	public void Call_HasCorrectEncoding() {
		var result = InstructionSetSM83.TryGetEncoding("call", AddressingMode.Absolute, out var encoding);

		Assert.True(result);
		Assert.Equal(0xcd, encoding.Opcode);
		Assert.Equal(3, encoding.Size);
	}

	[Fact]
	public void Ret_HasCorrectEncoding() {
		var result = InstructionSetSM83.TryGetEncoding("ret", AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(0xc9, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// Miscellaneous Instructions
	// ========================================================================

	[Theory]
	[InlineData("nop", 0x00)]
	[InlineData("halt", 0x76)]
	[InlineData("di", 0xf3)]
	[InlineData("ei", 0xfb)]
	[InlineData("ccf", 0x3f)]
	[InlineData("scf", 0x37)]
	[InlineData("daa", 0x27)]
	[InlineData("cpl", 0x2f)]
	public void Miscellaneous_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	// ========================================================================
	// CB-Prefixed Instructions
	// ========================================================================

	[Theory]
	[InlineData("rlc a", 0x07)]
	[InlineData("rlc b", 0x00)]
	[InlineData("rrc a", 0x0f)]
	[InlineData("rl a", 0x17)]
	[InlineData("rr a", 0x1f)]
	[InlineData("sla a", 0x27)]
	[InlineData("sra a", 0x2f)]
	[InlineData("swap a", 0x37)]
	[InlineData("srl a", 0x3f)]
	public void CBRotateShift_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
		Assert.True(encoding.IsCBPrefixed);
	}

	[Theory]
	[InlineData("bit 0,a", 0x47)]
	[InlineData("bit 0,b", 0x40)]
	[InlineData("bit 7,a", 0x7f)]
	[InlineData("bit 7,(hl)", 0x7e)]
	public void CBBit_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
		Assert.True(encoding.IsCBPrefixed);
	}

	[Theory]
	[InlineData("set 0,a", 0xc7)]
	[InlineData("set 7,a", 0xff)]
	[InlineData("res 0,a", 0x87)]
	[InlineData("res 7,a", 0xbf)]
	public void CBSetRes_HasCorrectEncoding(string mnemonic, byte opcode) {
		var result = InstructionSetSM83.TryGetEncoding(mnemonic, AddressingMode.Implied, out var encoding);

		Assert.True(result);
		Assert.Equal(opcode, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
		Assert.True(encoding.IsCBPrefixed);
	}

	// ========================================================================
	// Helper Methods
	// ========================================================================

	[Fact]
	public void GetAllMnemonics_ReturnsNonEmpty() {
		var mnemonics = InstructionSetSM83.GetAllMnemonics().ToList();

		Assert.NotEmpty(mnemonics);
		Assert.Contains("nop", mnemonics, StringComparer.OrdinalIgnoreCase);
		Assert.Contains("ld a,b", mnemonics, StringComparer.OrdinalIgnoreCase);
	}

	[Fact]
	public void IsRelativeBranch_IdentifiesJR() {
		Assert.True(InstructionSetSM83.IsRelativeBranch("jr"));
		Assert.True(InstructionSetSM83.IsRelativeBranch("jr nz"));
		Assert.True(InstructionSetSM83.IsRelativeBranch("JR Z"));
		Assert.False(InstructionSetSM83.IsRelativeBranch("jp"));
		Assert.False(InstructionSetSM83.IsRelativeBranch("call"));
	}
}

