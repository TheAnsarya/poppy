// InstructionSetSPC700Tests.cs
// Unit tests for SPC700 instruction set implementation (SNES audio coprocessor)

using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Unit tests for the SPC700 instruction set.
/// </summary>
public class InstructionSetSPC700Tests {
	#region Mnemonic Validation Tests

	[Theory]
	[InlineData("mov")]
	[InlineData("MOV")]
	[InlineData("adc")]
	[InlineData("sbc")]
	[InlineData("and")]
	[InlineData("or")]
	[InlineData("eor")]
	[InlineData("cmp")]
	[InlineData("inc")]
	[InlineData("dec")]
	[InlineData("asl")]
	[InlineData("lsr")]
	[InlineData("rol")]
	[InlineData("ror")]
	[InlineData("push")]
	[InlineData("pop")]
	[InlineData("bra")]
	[InlineData("beq")]
	[InlineData("bne")]
	[InlineData("bcs")]
	[InlineData("bcc")]
	[InlineData("bvs")]
	[InlineData("bvc")]
	[InlineData("bmi")]
	[InlineData("bpl")]
	[InlineData("jmp")]
	[InlineData("call")]
	[InlineData("ret")]
	[InlineData("nop")]
	[InlineData("set1")]
	[InlineData("clr1")]
	[InlineData("bbc")]
	[InlineData("bbs")]
	[InlineData("tcall")]
	[InlineData("pcall")]
	[InlineData("mul")]
	[InlineData("div")]
	[InlineData("movw")]
	[InlineData("addw")]
	[InlineData("subw")]
	[InlineData("cmpw")]
	[InlineData("incw")]
	[InlineData("decw")]
	public void IsValidMnemonic_ValidMnemonics_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetSPC700.IsValidMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("")]
	[InlineData("xyz")]
	[InlineData("invalid")]
	[InlineData("ldax")]
	[InlineData("tsx")]
	public void IsValidMnemonic_InvalidMnemonics_ReturnsFalse(string? mnemonic) {
		Assert.False(InstructionSetSPC700.IsValidMnemonic(mnemonic!));
	}

	[Fact]
	public void IsValidMnemonic_Null_ReturnsFalse() {
		Assert.False(InstructionSetSPC700.IsValidMnemonic(null!));
	}

	[Theory]
	[InlineData("tcall0")]
	[InlineData("tcall1")]
	[InlineData("tcall15")]
	[InlineData("TCALL0")]
	public void IsValidMnemonic_TcallWithNumber_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetSPC700.IsValidMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("tcall16")]
	[InlineData("tcall-1")]
	[InlineData("tcallx")]
	public void IsValidMnemonic_InvalidTcall_ReturnsFalse(string mnemonic) {
		Assert.False(InstructionSetSPC700.IsValidMnemonic(mnemonic));
	}

	#endregion

	#region Instruction Size Tests

	[Theory]
	[InlineData(InstructionSetSPC700.AddressingMode.Implied, 1)]
	[InlineData(InstructionSetSPC700.AddressingMode.Accumulator, 1)]
	[InlineData(InstructionSetSPC700.AddressingMode.IndirectX, 1)]
	[InlineData(InstructionSetSPC700.AddressingMode.IndirectY, 1)]
	[InlineData(InstructionSetSPC700.AddressingMode.IndirectXInc, 1)]
	[InlineData(InstructionSetSPC700.AddressingMode.TableCall, 1)]
	public void GetInstructionSize_SingleByteMode_Returns1(InstructionSetSPC700.AddressingMode mode, int expected) {
		int size = InstructionSetSPC700.GetInstructionSize(mode);
		Assert.Equal(expected, size);
	}

	[Theory]
	[InlineData(InstructionSetSPC700.AddressingMode.Immediate, 2)]
	[InlineData(InstructionSetSPC700.AddressingMode.DirectPage, 2)]
	[InlineData(InstructionSetSPC700.AddressingMode.DirectPageX, 2)]
	[InlineData(InstructionSetSPC700.AddressingMode.DirectPageY, 2)]
	[InlineData(InstructionSetSPC700.AddressingMode.IndirectPageX, 2)]
	[InlineData(InstructionSetSPC700.AddressingMode.IndirectPageY, 2)]
	[InlineData(InstructionSetSPC700.AddressingMode.Relative, 2)]
	[InlineData(InstructionSetSPC700.AddressingMode.BitDirect, 2)]
	[InlineData(InstructionSetSPC700.AddressingMode.PCall, 2)]
	[InlineData(InstructionSetSPC700.AddressingMode.YA16, 2)]
	public void GetInstructionSize_TwoByteMode_Returns2(InstructionSetSPC700.AddressingMode mode, int expected) {
		int size = InstructionSetSPC700.GetInstructionSize(mode);
		Assert.Equal(expected, size);
	}

	[Theory]
	[InlineData(InstructionSetSPC700.AddressingMode.Absolute, 3)]
	[InlineData(InstructionSetSPC700.AddressingMode.AbsoluteX, 3)]
	[InlineData(InstructionSetSPC700.AddressingMode.AbsoluteY, 3)]
	[InlineData(InstructionSetSPC700.AddressingMode.AbsoluteBit, 3)]
	[InlineData(InstructionSetSPC700.AddressingMode.DirectPageRelative, 3)]
	[InlineData(InstructionSetSPC700.AddressingMode.DirectPageDirect, 3)]
	[InlineData(InstructionSetSPC700.AddressingMode.DirectPageImmediate, 3)]
	public void GetInstructionSize_ThreeByteMode_Returns3(InstructionSetSPC700.AddressingMode mode, int expected) {
		int size = InstructionSetSPC700.GetInstructionSize(mode);
		Assert.Equal(expected, size);
	}

	#endregion

	#region Branch Instruction Tests

	[Theory]
	[InlineData("bra")]
	[InlineData("beq")]
	[InlineData("bne")]
	[InlineData("bcs")]
	[InlineData("bcc")]
	[InlineData("bvs")]
	[InlineData("bvc")]
	[InlineData("bmi")]
	[InlineData("bpl")]
	[InlineData("cbne")]
	[InlineData("dbnz")]
	[InlineData("bbc")]
	[InlineData("bbs")]
	public void IsBranchInstruction_BranchOpcodes_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetSPC700.IsBranchInstruction(mnemonic));
	}

	[Theory]
	[InlineData("mov")]
	[InlineData("adc")]
	[InlineData("nop")]
	[InlineData("jmp")]
	[InlineData("call")]
	[InlineData("ret")]
	public void IsBranchInstruction_NonBranchOpcodes_ReturnsFalse(string mnemonic) {
		Assert.False(InstructionSetSPC700.IsBranchInstruction(mnemonic));
	}

	#endregion

	#region TCALL Encoding Tests

	[Theory]
	[InlineData(0, 0x01)]
	[InlineData(1, 0x11)]
	[InlineData(2, 0x21)]
	[InlineData(7, 0x71)]
	[InlineData(15, 0xf1)]
	public void EncodeTcall_ValidIndex_ReturnsCorrectOpcode(int index, int expected) {
		byte[] encoded = InstructionSetSPC700.EncodeTcall(index);
		Assert.Single(encoded);
		Assert.Equal((byte)expected, encoded[0]);
	}

	[Fact]
	public void EncodeTcall_NegativeIndex_ThrowsException() {
		Assert.Throws<ArgumentOutOfRangeException>(() => InstructionSetSPC700.EncodeTcall(-1));
	}

	[Fact]
	public void EncodeTcall_IndexTooLarge_ThrowsException() {
		Assert.Throws<ArgumentOutOfRangeException>(() => InstructionSetSPC700.EncodeTcall(16));
	}

	#endregion

	#region Bit Direct Encoding Tests

	[Theory]
	[InlineData(true, 0, 0x02)]   // SET1 dp.0
	[InlineData(true, 1, 0x22)]   // SET1 dp.1
	[InlineData(true, 7, 0xe2)]   // SET1 dp.7
	[InlineData(false, 0, 0x12)]  // CLR1 dp.0
	[InlineData(false, 1, 0x32)]  // CLR1 dp.1
	[InlineData(false, 7, 0xf2)]  // CLR1 dp.7
	public void EncodeBitDirect_ValidBit_ReturnsCorrectOpcode(bool isSet, int bit, int expectedOpcode) {
		byte[] encoded = InstructionSetSPC700.EncodeBitDirect(isSet, bit, 0x42);
		Assert.Equal(2, encoded.Length);
		Assert.Equal((byte)expectedOpcode, encoded[0]);
		Assert.Equal(0x42, encoded[1]);
	}

	[Fact]
	public void EncodeBitDirect_BitTooLarge_ThrowsException() {
		Assert.Throws<ArgumentOutOfRangeException>(() => InstructionSetSPC700.EncodeBitDirect(true, 8, 0x00));
	}

	[Fact]
	public void EncodeBitDirect_NegativeBit_ThrowsException() {
		Assert.Throws<ArgumentOutOfRangeException>(() => InstructionSetSPC700.EncodeBitDirect(true, -1, 0x00));
	}

	#endregion

	#region Bit Branch Encoding Tests

	[Theory]
	[InlineData(true, 0, 0x03)]   // BBS dp.0
	[InlineData(true, 1, 0x23)]   // BBS dp.1
	[InlineData(true, 7, 0xe3)]   // BBS dp.7
	[InlineData(false, 0, 0x13)]  // BBC dp.0
	[InlineData(false, 1, 0x33)]  // BBC dp.1
	[InlineData(false, 7, 0xf3)]  // BBC dp.7
	public void EncodeBitBranch_ValidBit_ReturnsCorrectOpcode(bool isSet, int bit, int expectedOpcode) {
		byte[] encoded = InstructionSetSPC700.EncodeBitBranch(isSet, bit, 0x42, 0x10);
		Assert.Equal(3, encoded.Length);
		Assert.Equal((byte)expectedOpcode, encoded[0]);
		Assert.Equal(0x42, encoded[1]);
		Assert.Equal(0x10, encoded[2]);
	}

	[Fact]
	public void EncodeBitBranch_NegativeOffset_EncodesCorrectly() {
		byte[] encoded = InstructionSetSPC700.EncodeBitBranch(true, 0, 0x42, -5);
		Assert.Equal(3, encoded.Length);
		Assert.Equal(0xfb, encoded[2]);  // -5 as unsigned byte
	}

	#endregion

	#region Implied Encoding Tests

	[Theory]
	[InlineData(0x00)]
	[InlineData(0x6f)]
	[InlineData(0xcf)]
	[InlineData(0xef)]
	[InlineData(0xff)]
	public void EncodeImplied_ValidOpcode_ReturnsCorrectBytes(byte opcode) {
		byte[] encoded = InstructionSetSPC700.EncodeImplied(opcode);
		Assert.Single(encoded);
		Assert.Equal(opcode, encoded[0]);
	}

	#endregion

	#region Direct Page Encoding Tests

	[Fact]
	public void EncodeDirectPage_ReturnsCorrectBytes() {
		byte[] bytes = InstructionSetSPC700.EncodeDirectPage(0xe4, 0x42);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0xe4, bytes[0]);  // Opcode
		Assert.Equal(0x42, bytes[1]);  // Direct page address
	}

	#endregion

	#region Immediate Encoding Tests

	[Fact]
	public void EncodeImmediate_ReturnsCorrectBytes() {
		byte[] bytes = InstructionSetSPC700.EncodeImmediate(0xe8, 0xff);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0xe8, bytes[0]);  // Opcode
		Assert.Equal(0xff, bytes[1]);  // Immediate value
	}

	#endregion

	#region Absolute Encoding Tests

	[Fact]
	public void EncodeAbsolute_ReturnsCorrectBytes() {
		byte[] bytes = InstructionSetSPC700.EncodeAbsolute(0xe5, 0x1234);

		Assert.Equal(3, bytes.Length);
		Assert.Equal(0xe5, bytes[0]);  // Opcode
		Assert.Equal(0x34, bytes[1]);  // Low byte
		Assert.Equal(0x12, bytes[2]);  // High byte
	}

	#endregion

	#region Relative Encoding Tests

	[Fact]
	public void EncodeRelative_PositiveOffset_ReturnsCorrectBytes() {
		byte[] bytes = InstructionSetSPC700.EncodeRelative(0x2f, 10);  // BRA +10

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0x2f, bytes[0]);  // BRA opcode
		Assert.Equal(0x0a, bytes[1]);  // +10 offset
	}

	[Fact]
	public void EncodeRelative_NegativeOffset_ReturnsCorrectBytes() {
		byte[] bytes = InstructionSetSPC700.EncodeRelative(0x2f, -10);  // BRA -10

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0x2f, bytes[0]);  // BRA opcode
		Assert.Equal(0xf6, bytes[1]);  // -10 as unsigned byte
	}

	#endregion

	#region Direct Page to Direct Page Encoding Tests

	[Fact]
	public void EncodeDirectPageDirect_ReturnsCorrectBytes() {
		byte[] bytes = InstructionSetSPC700.EncodeDirectPageDirect(0xfa, 0x10, 0x20);  // MOV dp,dp

		Assert.Equal(3, bytes.Length);
		Assert.Equal(0xfa, bytes[0]);  // Opcode
		Assert.Equal(0x20, bytes[1]);  // Source (note: source first in encoding)
		Assert.Equal(0x10, bytes[2]);  // Destination
	}

	#endregion

	#region Direct Page Relative Encoding Tests

	[Fact]
	public void EncodeDirectPageRelative_ReturnsCorrectBytes() {
		byte[] bytes = InstructionSetSPC700.EncodeDirectPageRelative(0x2e, 0x42, 0x10);  // CBNE dp,rel

		Assert.Equal(3, bytes.Length);
		Assert.Equal(0x2e, bytes[0]);  // Opcode
		Assert.Equal(0x42, bytes[1]);  // Direct page address
		Assert.Equal(0x10, bytes[2]);  // Relative offset
	}

	#endregion

	#region DSP Register Constants Tests

	[Fact]
	public void DspRegisters_GlobalRegistersCorrect() {
		Assert.Equal(0x0c, InstructionSetSPC700.DspRegisters.MVOLL);
		Assert.Equal(0x1c, InstructionSetSPC700.DspRegisters.MVOLR);
		Assert.Equal(0x2c, InstructionSetSPC700.DspRegisters.EVOLL);
		Assert.Equal(0x3c, InstructionSetSPC700.DspRegisters.EVOLR);
		Assert.Equal(0x4c, InstructionSetSPC700.DspRegisters.KON);
		Assert.Equal(0x5c, InstructionSetSPC700.DspRegisters.KOFF);
		Assert.Equal(0x6c, InstructionSetSPC700.DspRegisters.FLG);
		Assert.Equal(0x7c, InstructionSetSPC700.DspRegisters.ENDX);
	}

	[Fact]
	public void DspRegisters_EchoRegistersCorrect() {
		Assert.Equal(0x0d, InstructionSetSPC700.DspRegisters.EFB);
		Assert.Equal(0x2d, InstructionSetSPC700.DspRegisters.PMON);
		Assert.Equal(0x3d, InstructionSetSPC700.DspRegisters.NON);
		Assert.Equal(0x4d, InstructionSetSPC700.DspRegisters.EON);
		Assert.Equal(0x5d, InstructionSetSPC700.DspRegisters.DIR);
		Assert.Equal(0x6d, InstructionSetSPC700.DspRegisters.ESA);
		Assert.Equal(0x7d, InstructionSetSPC700.DspRegisters.EDL);
	}

	[Fact]
	public void DspRegisters_FirCoefficientsCorrect() {
		Assert.Equal(0x0f, InstructionSetSPC700.DspRegisters.C0);
		Assert.Equal(0x1f, InstructionSetSPC700.DspRegisters.C1);
		Assert.Equal(0x2f, InstructionSetSPC700.DspRegisters.C2);
		Assert.Equal(0x3f, InstructionSetSPC700.DspRegisters.C3);
		Assert.Equal(0x4f, InstructionSetSPC700.DspRegisters.C4);
		Assert.Equal(0x5f, InstructionSetSPC700.DspRegisters.C5);
		Assert.Equal(0x6f, InstructionSetSPC700.DspRegisters.C6);
		Assert.Equal(0x7f, InstructionSetSPC700.DspRegisters.C7);
	}

	[Fact]
	public void DspRegisters_VoiceHelpers_ReturnCorrectValues() {
		// Test voice 0
		Assert.Equal(0x00, InstructionSetSPC700.DspRegisters.VoiceVolL(0));
		Assert.Equal(0x01, InstructionSetSPC700.DspRegisters.VoiceVolR(0));
		Assert.Equal(0x02, InstructionSetSPC700.DspRegisters.VoicePitchL(0));
		Assert.Equal(0x03, InstructionSetSPC700.DspRegisters.VoicePitchH(0));
		Assert.Equal(0x04, InstructionSetSPC700.DspRegisters.VoiceSrcn(0));
		Assert.Equal(0x05, InstructionSetSPC700.DspRegisters.VoiceAdsr1(0));
		Assert.Equal(0x06, InstructionSetSPC700.DspRegisters.VoiceAdsr2(0));
		Assert.Equal(0x07, InstructionSetSPC700.DspRegisters.VoiceGain(0));

		// Test voice 1 (base + $10)
		Assert.Equal(0x10, InstructionSetSPC700.DspRegisters.VoiceVolL(1));
		Assert.Equal(0x11, InstructionSetSPC700.DspRegisters.VoiceVolR(1));

		// Test voice 7 (base + $70)
		Assert.Equal(0x70, InstructionSetSPC700.DspRegisters.VoiceVolL(7));
		Assert.Equal(0x78, InstructionSetSPC700.DspRegisters.VoiceEnvx(7));
		Assert.Equal(0x79, InstructionSetSPC700.DspRegisters.VoiceOutx(7));
	}

	#endregion

	#region I/O Register Constants Tests

	[Fact]
	public void IoRegisters_HasCorrectAddresses() {
		Assert.Equal(0xf0, InstructionSetSPC700.IoRegisters.TEST);
		Assert.Equal(0xf1, InstructionSetSPC700.IoRegisters.CONTROL);
		Assert.Equal(0xf2, InstructionSetSPC700.IoRegisters.DSPADDR);
		Assert.Equal(0xf3, InstructionSetSPC700.IoRegisters.DSPDATA);
		Assert.Equal(0xf4, InstructionSetSPC700.IoRegisters.CPUIO0);
		Assert.Equal(0xf5, InstructionSetSPC700.IoRegisters.CPUIO1);
		Assert.Equal(0xf6, InstructionSetSPC700.IoRegisters.CPUIO2);
		Assert.Equal(0xf7, InstructionSetSPC700.IoRegisters.CPUIO3);
	}

	[Fact]
	public void IoRegisters_TimerAddressesCorrect() {
		Assert.Equal(0xfa, InstructionSetSPC700.IoRegisters.T0TARGET);
		Assert.Equal(0xfb, InstructionSetSPC700.IoRegisters.T1TARGET);
		Assert.Equal(0xfc, InstructionSetSPC700.IoRegisters.T2TARGET);
		Assert.Equal(0xfd, InstructionSetSPC700.IoRegisters.T0OUT);
		Assert.Equal(0xfe, InstructionSetSPC700.IoRegisters.T1OUT);
		Assert.Equal(0xff, InstructionSetSPC700.IoRegisters.T2OUT);
	}

	#endregion

	#region Addressing Mode Tests

	[Fact]
	public void AddressingMode_HasAllModes() {
		var modes = Enum.GetValues<InstructionSetSPC700.AddressingMode>();

		Assert.Contains(InstructionSetSPC700.AddressingMode.Implied, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.Accumulator, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.Immediate, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.DirectPage, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.DirectPageX, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.DirectPageY, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.Absolute, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.AbsoluteX, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.AbsoluteY, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.IndirectX, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.IndirectY, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.IndirectXInc, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.IndirectPageX, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.IndirectPageY, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.DirectPageBit, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.AbsoluteBit, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.Relative, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.DirectPageRelative, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.DirectPageDirect, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.DirectPageImmediate, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.YA16, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.TableCall, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.PCall, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.BitDirect, modes);
		Assert.Contains(InstructionSetSPC700.AddressingMode.DirectPageBitRelative, modes);
	}

	#endregion
}
