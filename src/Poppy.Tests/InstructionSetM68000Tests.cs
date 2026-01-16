// ============================================================================
// InstructionSetM68000Tests.cs - M68000 Instruction Set Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;

namespace Poppy.Tests;

/// <summary>
/// Tests for M68000 instruction set encoding.
/// </summary>
public class InstructionSetM68000Tests {
	[Fact]
	public void ValidMnemonic_Move_ReturnsTrue() {
		Assert.True(InstructionSetM68000.IsValidMnemonic("move"));
		Assert.True(InstructionSetM68000.IsValidMnemonic("move.w"));
		Assert.True(InstructionSetM68000.IsValidMnemonic("move.l"));
		Assert.True(InstructionSetM68000.IsValidMnemonic("MOVE.B"));
	}

	[Fact]
	public void ValidMnemonic_Arithmetic_ReturnsTrue() {
		Assert.True(InstructionSetM68000.IsValidMnemonic("add"));
		Assert.True(InstructionSetM68000.IsValidMnemonic("sub"));
		Assert.True(InstructionSetM68000.IsValidMnemonic("muls"));
		Assert.True(InstructionSetM68000.IsValidMnemonic("divu"));
	}

	[Fact]
	public void ValidMnemonic_Branch_ReturnsTrue() {
		Assert.True(InstructionSetM68000.IsValidMnemonic("bra"));
		Assert.True(InstructionSetM68000.IsValidMnemonic("bne"));
		Assert.True(InstructionSetM68000.IsValidMnemonic("beq"));
		Assert.True(InstructionSetM68000.IsValidMnemonic("bsr"));
	}

	[Fact]
	public void ValidMnemonic_Control_ReturnsTrue() {
		Assert.True(InstructionSetM68000.IsValidMnemonic("nop"));
		Assert.True(InstructionSetM68000.IsValidMnemonic("rts"));
		Assert.True(InstructionSetM68000.IsValidMnemonic("jmp"));
		Assert.True(InstructionSetM68000.IsValidMnemonic("jsr"));
	}

	[Fact]
	public void InvalidMnemonic_ReturnsFalse() {
		Assert.False(InstructionSetM68000.IsValidMnemonic("xyz"));
		Assert.False(InstructionSetM68000.IsValidMnemonic("lda"));
		Assert.False(InstructionSetM68000.IsValidMnemonic("sta"));
	}

	[Fact]
	public void StripSizeSuffix_RemovesSuffix() {
		Assert.Equal("move", InstructionSetM68000.StripSizeSuffix("move.b"));
		Assert.Equal("move", InstructionSetM68000.StripSizeSuffix("move.w"));
		Assert.Equal("move", InstructionSetM68000.StripSizeSuffix("move.l"));
		Assert.Equal("add", InstructionSetM68000.StripSizeSuffix("add.l"));
	}

	[Fact]
	public void StripSizeSuffix_NoSuffix_ReturnsOriginal() {
		Assert.Equal("nop", InstructionSetM68000.StripSizeSuffix("nop"));
		Assert.Equal("rts", InstructionSetM68000.StripSizeSuffix("rts"));
	}

	[Fact]
	public void GetSizeFromMnemonic_CorrectSize() {
		Assert.Equal(InstructionSetM68000.OperationSize.Byte, InstructionSetM68000.GetSizeFromMnemonic("move.b"));
		Assert.Equal(InstructionSetM68000.OperationSize.Word, InstructionSetM68000.GetSizeFromMnemonic("move.w"));
		Assert.Equal(InstructionSetM68000.OperationSize.Long, InstructionSetM68000.GetSizeFromMnemonic("move.l"));
		Assert.Equal(InstructionSetM68000.OperationSize.Word, InstructionSetM68000.GetSizeFromMnemonic("nop")); // Default
	}

	[Fact]
	public void TryGetBaseOpcode_ValidMnemonic_ReturnsTrue() {
		Assert.True(InstructionSetM68000.TryGetBaseOpcode("nop", out var opcode));
		Assert.Equal(0x4e71, opcode);
	}

	[Fact]
	public void TryGetBaseOpcode_Rts_CorrectOpcode() {
		Assert.True(InstructionSetM68000.TryGetBaseOpcode("rts", out var opcode));
		Assert.Equal(0x4e75, opcode);
	}

	[Fact]
	public void TryGetRegister_DataRegister_ReturnsCorrectEncoding() {
		Assert.True(InstructionSetM68000.TryGetRegister("d0", out var encoding, out var isAddress));
		Assert.Equal(0, encoding);
		Assert.False(isAddress);

		Assert.True(InstructionSetM68000.TryGetRegister("d7", out encoding, out isAddress));
		Assert.Equal(7, encoding);
		Assert.False(isAddress);
	}

	[Fact]
	public void TryGetRegister_AddressRegister_ReturnsCorrectEncoding() {
		Assert.True(InstructionSetM68000.TryGetRegister("a0", out var encoding, out var isAddress));
		Assert.Equal(0, encoding);
		Assert.True(isAddress);

		Assert.True(InstructionSetM68000.TryGetRegister("a7", out encoding, out isAddress));
		Assert.Equal(7, encoding);
		Assert.True(isAddress);

		Assert.True(InstructionSetM68000.TryGetRegister("sp", out encoding, out isAddress));
		Assert.Equal(7, encoding);  // SP is alias for A7
		Assert.True(isAddress);
	}

	[Fact]
	public void IsBranchInstruction_BranchMnemonics_ReturnsTrue() {
		Assert.True(InstructionSetM68000.IsBranchInstruction("bra"));
		Assert.True(InstructionSetM68000.IsBranchInstruction("bne"));
		Assert.True(InstructionSetM68000.IsBranchInstruction("beq"));
		Assert.True(InstructionSetM68000.IsBranchInstruction("bsr"));
		Assert.True(InstructionSetM68000.IsBranchInstruction("bge.s"));
	}

	[Fact]
	public void IsBranchInstruction_NonBranchMnemonics_ReturnsFalse() {
		Assert.False(InstructionSetM68000.IsBranchInstruction("move"));
		Assert.False(InstructionSetM68000.IsBranchInstruction("add"));
		Assert.False(InstructionSetM68000.IsBranchInstruction("jmp"));
		Assert.False(InstructionSetM68000.IsBranchInstruction("jsr"));
	}

	[Fact]
	public void WriteWord_BigEndian_CorrectOrder() {
		var bytes = InstructionSetM68000.WriteWord(0x1234);
		Assert.Equal(2, bytes.Length);
		Assert.Equal(0x12, bytes[0]);
		Assert.Equal(0x34, bytes[1]);
	}

	[Fact]
	public void WriteLong_BigEndian_CorrectOrder() {
		var bytes = InstructionSetM68000.WriteLong(0x12345678);
		Assert.Equal(4, bytes.Length);
		Assert.Equal(0x12, bytes[0]);
		Assert.Equal(0x34, bytes[1]);
		Assert.Equal(0x56, bytes[2]);
		Assert.Equal(0x78, bytes[3]);
	}

	[Fact]
	public void EncodeEffectiveAddress_DataRegister_CorrectEncoding() {
		var ea = InstructionSetM68000.EncodeEffectiveAddress(
			InstructionSetM68000.M68kAddressingMode.DataRegisterDirect, 3);
		Assert.Equal(0b000_011, ea);  // Mode 0, Register 3
	}

	[Fact]
	public void EncodeEffectiveAddress_AddressRegisterIndirect_CorrectEncoding() {
		var ea = InstructionSetM68000.EncodeEffectiveAddress(
			InstructionSetM68000.M68kAddressingMode.AddressRegisterIndirect, 2);
		Assert.Equal(0b010_010, ea);  // Mode 2, Register 2
	}

	[Fact]
	public void EncodeEffectiveAddress_Immediate_CorrectEncoding() {
		var ea = InstructionSetM68000.EncodeEffectiveAddress(
			InstructionSetM68000.M68kAddressingMode.Immediate, 0);
		Assert.Equal(0b111_100, ea);  // Mode 7, Register 4
	}

	[Fact]
	public void EncodeSizeField_AllSizes_CorrectEncoding() {
		Assert.Equal(0, InstructionSetM68000.EncodeSizeField(InstructionSetM68000.OperationSize.Byte));
		Assert.Equal(1, InstructionSetM68000.EncodeSizeField(InstructionSetM68000.OperationSize.Word));
		Assert.Equal(2, InstructionSetM68000.EncodeSizeField(InstructionSetM68000.OperationSize.Long));
	}

	[Fact]
	public void GetByteCount_AllSizes_CorrectCount() {
		Assert.Equal(1, InstructionSetM68000.GetByteCount(InstructionSetM68000.OperationSize.Byte));
		Assert.Equal(2, InstructionSetM68000.GetByteCount(InstructionSetM68000.OperationSize.Word));
		Assert.Equal(4, InstructionSetM68000.GetByteCount(InstructionSetM68000.OperationSize.Long));
	}
}

