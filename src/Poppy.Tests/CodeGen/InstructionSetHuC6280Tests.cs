// InstructionSetHuC6280Tests.cs
// Unit tests for HuC6280 instruction set (TurboGrafx-16 / PC Engine)

using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Unit tests for the HuC6280 instruction set implementation.
/// </summary>
public class InstructionSetHuC6280Tests {
	#region Mnemonic Validation Tests

	[Theory]
	[InlineData("lda")]
	[InlineData("ldx")]
	[InlineData("ldy")]
	[InlineData("sta")]
	[InlineData("stx")]
	[InlineData("sty")]
	[InlineData("stz")]
	public void IsValidMnemonic_LoadStore_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetHuC6280.IsValidMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("adc")]
	[InlineData("sbc")]
	[InlineData("inc")]
	[InlineData("dec")]
	[InlineData("inx")]
	[InlineData("iny")]
	[InlineData("dex")]
	[InlineData("dey")]
	public void IsValidMnemonic_Arithmetic_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetHuC6280.IsValidMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("and")]
	[InlineData("ora")]
	[InlineData("eor")]
	[InlineData("asl")]
	[InlineData("lsr")]
	[InlineData("rol")]
	[InlineData("ror")]
	public void IsValidMnemonic_Logical_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetHuC6280.IsValidMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("bcc")]
	[InlineData("bcs")]
	[InlineData("beq")]
	[InlineData("bne")]
	[InlineData("bmi")]
	[InlineData("bpl")]
	[InlineData("bvc")]
	[InlineData("bvs")]
	[InlineData("bra")]
	public void IsValidMnemonic_Branch_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetHuC6280.IsValidMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("tii")]
	[InlineData("tdd")]
	[InlineData("tin")]
	[InlineData("tia")]
	[InlineData("tai")]
	public void IsValidMnemonic_BlockTransfer_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetHuC6280.IsValidMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("tam")]
	[InlineData("tma")]
	[InlineData("csl")]
	[InlineData("csh")]
	public void IsValidMnemonic_HuC6280Specific_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetHuC6280.IsValidMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("st0")]
	[InlineData("st1")]
	[InlineData("st2")]
	[InlineData("sax")]
	[InlineData("say")]
	[InlineData("sxy")]
	public void IsValidMnemonic_HuC6280Extensions_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetHuC6280.IsValidMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("phx")]
	[InlineData("phy")]
	[InlineData("plx")]
	[InlineData("ply")]
	[InlineData("trb")]
	[InlineData("tsb")]
	public void IsValidMnemonic_65C02Extensions_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetHuC6280.IsValidMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("smb0")]
	[InlineData("smb7")]
	[InlineData("rmb0")]
	[InlineData("rmb7")]
	[InlineData("bbr0")]
	[InlineData("bbs7")]
	public void IsValidMnemonic_BitOperations_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetHuC6280.IsValidMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("xyz")]
	[InlineData("foo")]
	[InlineData("")]
	[InlineData("smb8")]
	[InlineData("rmb9")]
	public void IsValidMnemonic_Invalid_ReturnsFalse(string mnemonic) {
		Assert.False(InstructionSetHuC6280.IsValidMnemonic(mnemonic));
	}

	#endregion

	#region Opcode Tests

	[Fact]
	public void TryGetOpcode_LdaImmediate_ReturnsA9() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("lda", InstructionSetHuC6280.AddressingMode.Immediate, out byte opcode));
		Assert.Equal(0xa9, opcode);
	}

	[Fact]
	public void TryGetOpcode_LdaZeroPage_ReturnsA5() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("lda", InstructionSetHuC6280.AddressingMode.ZeroPage, out byte opcode));
		Assert.Equal(0xa5, opcode);
	}

	[Fact]
	public void TryGetOpcode_JmpAbsolute_Returns4C() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("jmp", InstructionSetHuC6280.AddressingMode.Absolute, out byte opcode));
		Assert.Equal(0x4c, opcode);
	}

	[Fact]
	public void TryGetOpcode_Tii_Returns73() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("tii", InstructionSetHuC6280.AddressingMode.BlockTransfer, out byte opcode));
		Assert.Equal(0x73, opcode);
	}

	[Fact]
	public void TryGetOpcode_Csl_Returns54() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("csl", InstructionSetHuC6280.AddressingMode.Implied, out byte opcode));
		Assert.Equal(0x54, opcode);
	}

	[Fact]
	public void TryGetOpcode_Csh_ReturnsD4() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("csh", InstructionSetHuC6280.AddressingMode.Implied, out byte opcode));
		Assert.Equal(0xd4, opcode);
	}

	[Fact]
	public void TryGetOpcode_InvalidMode_ReturnsFalse() {
		Assert.False(InstructionSetHuC6280.TryGetOpcode("lda", InstructionSetHuC6280.AddressingMode.Implied, out _));
	}

	#endregion

	#region Instruction Size Tests

	[Theory]
	[InlineData(InstructionSetHuC6280.AddressingMode.Implied, 1)]
	[InlineData(InstructionSetHuC6280.AddressingMode.Accumulator, 1)]
	[InlineData(InstructionSetHuC6280.AddressingMode.Immediate, 2)]
	[InlineData(InstructionSetHuC6280.AddressingMode.ZeroPage, 2)]
	[InlineData(InstructionSetHuC6280.AddressingMode.Absolute, 3)]
	[InlineData(InstructionSetHuC6280.AddressingMode.BlockTransfer, 7)]
	public void GetInstructionSize_ReturnsCorrectSize(InstructionSetHuC6280.AddressingMode mode, int expected) {
		Assert.Equal(expected, InstructionSetHuC6280.GetInstructionSize(mode));
	}

	#endregion

	#region Branch Detection Tests

	[Theory]
	[InlineData("bcc", true)]
	[InlineData("bcs", true)]
	[InlineData("beq", true)]
	[InlineData("bne", true)]
	[InlineData("bra", true)]
	[InlineData("bbr0", true)]
	[InlineData("bbs7", true)]
	[InlineData("lda", false)]
	[InlineData("jmp", false)]
	public void IsBranchInstruction_ReturnsCorrectResult(string mnemonic, bool expected) {
		Assert.Equal(expected, InstructionSetHuC6280.IsBranchInstruction(mnemonic));
	}

	#endregion

	#region Block Transfer Detection Tests

	[Theory]
	[InlineData("tii", true)]
	[InlineData("tdd", true)]
	[InlineData("tin", true)]
	[InlineData("tia", true)]
	[InlineData("tai", true)]
	[InlineData("lda", false)]
	[InlineData("sta", false)]
	public void IsBlockTransfer_ReturnsCorrectResult(string mnemonic, bool expected) {
		Assert.Equal(expected, InstructionSetHuC6280.IsBlockTransfer(mnemonic));
	}

	#endregion

	#region Encoding Tests

	[Fact]
	public void EncodeZeroPage_Returns2Bytes() {
		var bytes = InstructionSetHuC6280.EncodeZeroPage(0xa5, 0x42);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0xa5, bytes[0]);
		Assert.Equal(0x42, bytes[1]);
	}

	[Fact]
	public void EncodeImmediate_Returns2Bytes() {
		var bytes = InstructionSetHuC6280.EncodeImmediate(0xa9, 0xff);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0xa9, bytes[0]);
		Assert.Equal(0xff, bytes[1]);
	}

	[Fact]
	public void EncodeAbsolute_Returns3BytesLittleEndian() {
		var bytes = InstructionSetHuC6280.EncodeAbsolute(0xad, 0x1234);

		Assert.Equal(3, bytes.Length);
		Assert.Equal(0xad, bytes[0]);
		Assert.Equal(0x34, bytes[1]);  // Low byte first
		Assert.Equal(0x12, bytes[2]);  // High byte second
	}

	[Fact]
	public void EncodeRelative_Returns2Bytes() {
		var bytes = InstructionSetHuC6280.EncodeRelative(0xf0, -10);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0xf0, bytes[0]);
		Assert.Equal(0xf6, bytes[1]);  // -10 as unsigned byte
	}

	[Fact]
	public void EncodeBlockTransfer_Returns7Bytes() {
		var bytes = InstructionSetHuC6280.EncodeBlockTransfer(0x73, 0x1000, 0x2000, 0x0100);

		Assert.Equal(7, bytes.Length);
		Assert.Equal(0x73, bytes[0]);  // Opcode
		Assert.Equal(0x00, bytes[1]);  // Source low
		Assert.Equal(0x10, bytes[2]);  // Source high
		Assert.Equal(0x00, bytes[3]);  // Dest low
		Assert.Equal(0x20, bytes[4]);  // Dest high
		Assert.Equal(0x00, bytes[5]);  // Length low
		Assert.Equal(0x01, bytes[6]);  // Length high
	}

	[Fact]
	public void EncodeZeroPageRelative_Returns3Bytes() {
		var bytes = InstructionSetHuC6280.EncodeZeroPageRelative(0x0f, 0x42, 5);

		Assert.Equal(3, bytes.Length);
		Assert.Equal(0x0f, bytes[0]);
		Assert.Equal(0x42, bytes[1]);
		Assert.Equal(0x05, bytes[2]);
	}

	#endregion

	#region Memory Page Constants Tests

	[Fact]
	public void MemoryPages_HasCorrectValues() {
		Assert.Equal(0x01, InstructionSetHuC6280.MemoryPages.MPR0);
		Assert.Equal(0x02, InstructionSetHuC6280.MemoryPages.MPR1);
		Assert.Equal(0x80, InstructionSetHuC6280.MemoryPages.MPR7);
	}

	#endregion

	#region VDC Register Constants Tests

	[Fact]
	public void VdcRegisters_HasCorrectValues() {
		Assert.Equal(0x00, InstructionSetHuC6280.VdcRegisters.MAWR);
		Assert.Equal(0x01, InstructionSetHuC6280.VdcRegisters.MARR);
		Assert.Equal(0x05, InstructionSetHuC6280.VdcRegisters.CR);
	}

	#endregion
}
