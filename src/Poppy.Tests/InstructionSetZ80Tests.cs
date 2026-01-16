// ============================================================================
// InstructionSetZ80Tests.cs - Z80 Instruction Set Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;

namespace Poppy.Tests;

/// <summary>
/// Tests for Z80 instruction set encoding.
/// </summary>
public class InstructionSetZ80Tests {
	[Fact]
	public void ValidMnemonic_Load_ReturnsTrue() {
		Assert.True(InstructionSetZ80.IsValidMnemonic("ld"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("ld a,(bc)"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("push"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("pop"));
	}

	[Fact]
	public void ValidMnemonic_Arithmetic_ReturnsTrue() {
		Assert.True(InstructionSetZ80.IsValidMnemonic("add a"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("sub"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("and"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("or"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("xor"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("cp"));
	}

	[Fact]
	public void ValidMnemonic_Jump_ReturnsTrue() {
		Assert.True(InstructionSetZ80.IsValidMnemonic("jp"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("jr"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("call"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("ret"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("djnz"));
	}

	[Fact]
	public void ValidMnemonic_BitOps_ReturnsTrue() {
		Assert.True(InstructionSetZ80.IsValidMnemonic("bit"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("set"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("res"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("rlc"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("rrc"));
	}

	[Fact]
	public void ValidMnemonic_BlockOps_ReturnsTrue() {
		Assert.True(InstructionSetZ80.IsValidMnemonic("ldi"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("ldir"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("ldd"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("lddr"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("cpi"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("cpir"));
	}

	[Fact]
	public void ValidMnemonic_Control_ReturnsTrue() {
		Assert.True(InstructionSetZ80.IsValidMnemonic("nop"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("halt"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("di"));
		Assert.True(InstructionSetZ80.IsValidMnemonic("ei"));
	}

	[Fact]
	public void TryGetRegister_8BitRegister_ReturnsCorrect() {
		Assert.True(InstructionSetZ80.TryGetRegister("a", out var encoding, out var is16Bit));
		Assert.Equal(InstructionSetZ80.Reg8.A, encoding);
		Assert.False(is16Bit);

		Assert.True(InstructionSetZ80.TryGetRegister("b", out encoding, out is16Bit));
		Assert.Equal(InstructionSetZ80.Reg8.B, encoding);
		Assert.False(is16Bit);
	}

	[Fact]
	public void TryGetRegister_16BitRegister_ReturnsCorrect() {
		Assert.True(InstructionSetZ80.TryGetRegister("hl", out var encoding, out var is16Bit));
		Assert.Equal(InstructionSetZ80.Reg16.HL, encoding);
		Assert.True(is16Bit);

		Assert.True(InstructionSetZ80.TryGetRegister("sp", out encoding, out is16Bit));
		Assert.Equal(InstructionSetZ80.Reg16.SP, encoding);
		Assert.True(is16Bit);
	}

	[Fact]
	public void TryGetCondition_ValidConditions_ReturnsCorrect() {
		Assert.True(InstructionSetZ80.TryGetCondition("nz", out var encoding));
		Assert.Equal(InstructionSetZ80.Conditions.NZ, encoding);

		Assert.True(InstructionSetZ80.TryGetCondition("z", out encoding));
		Assert.Equal(InstructionSetZ80.Conditions.Z, encoding);

		Assert.True(InstructionSetZ80.TryGetCondition("nc", out encoding));
		Assert.Equal(InstructionSetZ80.Conditions.NC, encoding);

		Assert.True(InstructionSetZ80.TryGetCondition("c", out encoding));
		Assert.Equal(InstructionSetZ80.Conditions.C, encoding);
	}

	[Fact]
	public void IsRelativeBranch_JrAndDjnz_ReturnsTrue() {
		Assert.True(InstructionSetZ80.IsRelativeBranch("jr"));
		Assert.True(InstructionSetZ80.IsRelativeBranch("djnz"));
		Assert.True(InstructionSetZ80.IsRelativeBranch("jr nz"));
		Assert.True(InstructionSetZ80.IsRelativeBranch("jr z"));
	}

	[Fact]
	public void IsRelativeBranch_JpAndCall_ReturnsFalse() {
		Assert.False(InstructionSetZ80.IsRelativeBranch("jp"));
		Assert.False(InstructionSetZ80.IsRelativeBranch("call"));
		Assert.False(InstructionSetZ80.IsRelativeBranch("ret"));
	}

	[Fact]
	public void GetIndexPrefix_IX_ReturnsDD() {
		Assert.Equal(InstructionSetZ80.Prefixes.DD, InstructionSetZ80.GetIndexPrefix("ix"));
		Assert.Equal(InstructionSetZ80.Prefixes.DD, InstructionSetZ80.GetIndexPrefix("ixh"));
		Assert.Equal(InstructionSetZ80.Prefixes.DD, InstructionSetZ80.GetIndexPrefix("ixl"));
	}

	[Fact]
	public void GetIndexPrefix_IY_ReturnsFD() {
		Assert.Equal(InstructionSetZ80.Prefixes.FD, InstructionSetZ80.GetIndexPrefix("iy"));
		Assert.Equal(InstructionSetZ80.Prefixes.FD, InstructionSetZ80.GetIndexPrefix("iyh"));
		Assert.Equal(InstructionSetZ80.Prefixes.FD, InstructionSetZ80.GetIndexPrefix("iyl"));
	}

	[Fact]
	public void TryGetEncoding_Nop_ReturnsCorrect() {
		Assert.True(InstructionSetZ80.TryGetEncoding("nop", InstructionSetZ80.Z80AddressingMode.Implied, out var encoding));
		Assert.Equal(0x00, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
		Assert.Empty(encoding.Prefix);
	}

	[Fact]
	public void TryGetEncoding_Halt_ReturnsCorrect() {
		Assert.True(InstructionSetZ80.TryGetEncoding("halt", InstructionSetZ80.Z80AddressingMode.Implied, out var encoding));
		Assert.Equal(0x76, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	[Fact]
	public void TryGetEncoding_Di_ReturnsCorrect() {
		Assert.True(InstructionSetZ80.TryGetEncoding("di", InstructionSetZ80.Z80AddressingMode.Implied, out var encoding));
		Assert.Equal(0xf3, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	[Fact]
	public void TryGetEncoding_Ei_ReturnsCorrect() {
		Assert.True(InstructionSetZ80.TryGetEncoding("ei", InstructionSetZ80.Z80AddressingMode.Implied, out var encoding));
		Assert.Equal(0xfb, encoding.Opcode);
		Assert.Equal(1, encoding.Size);
	}

	[Fact]
	public void TryGetEncoding_Ldir_ReturnsEdPrefix() {
		Assert.True(InstructionSetZ80.TryGetEncoding("ldir", InstructionSetZ80.Z80AddressingMode.Implied, out var encoding));
		Assert.Equal(0xb0, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
		Assert.Single(encoding.Prefix);
		Assert.Equal(InstructionSetZ80.Prefixes.ED, encoding.Prefix[0]);
	}

	[Fact]
	public void TryGetEncoding_CbPrefixed_ReturnsCbPrefix() {
		Assert.True(InstructionSetZ80.TryGetEncoding("rlc", InstructionSetZ80.Z80AddressingMode.Register8, out var encoding));
		Assert.Equal(0x00, encoding.Opcode);
		Assert.Equal(2, encoding.Size);
		Assert.Single(encoding.Prefix);
		Assert.Equal(InstructionSetZ80.Prefixes.CB, encoding.Prefix[0]);
	}

	[Fact]
	public void Prefixes_CorrectValues() {
		Assert.Equal(0xcb, InstructionSetZ80.Prefixes.CB);
		Assert.Equal(0xdd, InstructionSetZ80.Prefixes.DD);
		Assert.Equal(0xed, InstructionSetZ80.Prefixes.ED);
		Assert.Equal(0xfd, InstructionSetZ80.Prefixes.FD);
	}
}

