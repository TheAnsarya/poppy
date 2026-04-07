// InstructionSetHuC6280Tests.cs
// Unit tests for HuC6280 instruction set (TurboGrafx-16 / PC Engine)

using Poppy.Arch.HuC6280;
using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Arch.HuC6280.Tests.CodeGen;

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
		Assert.True(InstructionSetHuC6280.TryGetOpcode("lda", HuC6280AddressingMode.Immediate, out byte opcode));
		Assert.Equal(0xa9, opcode);
	}

	[Fact]
	public void TryGetOpcode_LdaZeroPage_ReturnsA5() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("lda", HuC6280AddressingMode.ZeroPage, out byte opcode));
		Assert.Equal(0xa5, opcode);
	}

	[Fact]
	public void TryGetOpcode_JmpAbsolute_Returns4C() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("jmp", HuC6280AddressingMode.Absolute, out byte opcode));
		Assert.Equal(0x4c, opcode);
	}

	[Fact]
	public void TryGetOpcode_Tii_Returns73() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("tii", HuC6280AddressingMode.BlockTransfer, out byte opcode));
		Assert.Equal(0x73, opcode);
	}

	[Fact]
	public void TryGetOpcode_Csl_Returns54() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("csl", HuC6280AddressingMode.Implied, out byte opcode));
		Assert.Equal(0x54, opcode);
	}

	[Fact]
	public void TryGetOpcode_Csh_ReturnsD4() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("csh", HuC6280AddressingMode.Implied, out byte opcode));
		Assert.Equal(0xd4, opcode);
	}

	[Fact]
	public void TryGetOpcode_InvalidMode_ReturnsFalse() {
		Assert.False(InstructionSetHuC6280.TryGetOpcode("lda", HuC6280AddressingMode.Implied, out _));
	}

	#endregion

	#region Instruction Size Tests

	[Theory]
	[InlineData(HuC6280AddressingMode.Implied, 1)]
	[InlineData(HuC6280AddressingMode.Accumulator, 1)]
	[InlineData(HuC6280AddressingMode.Immediate, 2)]
	[InlineData(HuC6280AddressingMode.ZeroPage, 2)]
	[InlineData(HuC6280AddressingMode.Absolute, 3)]
	[InlineData(HuC6280AddressingMode.BlockTransfer, 7)]
	public void GetInstructionSize_ReturnsCorrectSize(HuC6280AddressingMode mode, int expected) {
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

	[Fact]
	public void VdcRegisters_AllRegistersPresent() {
		Assert.Equal(0x02, InstructionSetHuC6280.VdcRegisters.VRR);
		Assert.Equal(0x06, InstructionSetHuC6280.VdcRegisters.RCR);
		Assert.Equal(0x07, InstructionSetHuC6280.VdcRegisters.BXR);
		Assert.Equal(0x08, InstructionSetHuC6280.VdcRegisters.BYR);
		Assert.Equal(0x09, InstructionSetHuC6280.VdcRegisters.MWR);
		Assert.Equal(0x0a, InstructionSetHuC6280.VdcRegisters.HSR);
		Assert.Equal(0x0b, InstructionSetHuC6280.VdcRegisters.HDR);
		Assert.Equal(0x0c, InstructionSetHuC6280.VdcRegisters.VPR);
		Assert.Equal(0x0d, InstructionSetHuC6280.VdcRegisters.VDW);
		Assert.Equal(0x0e, InstructionSetHuC6280.VdcRegisters.VCR);
		Assert.Equal(0x0f, InstructionSetHuC6280.VdcRegisters.DCR);
		Assert.Equal(0x10, InstructionSetHuC6280.VdcRegisters.SOUR);
		Assert.Equal(0x11, InstructionSetHuC6280.VdcRegisters.DESR);
		Assert.Equal(0x12, InstructionSetHuC6280.VdcRegisters.LENR);
		Assert.Equal(0x13, InstructionSetHuC6280.VdcRegisters.SATB);
	}

	#endregion

	#region Block Transfer Opcode Verification Tests

	[Theory]
	[InlineData("tii", 0x73)]
	[InlineData("tdd", 0xc3)]
	[InlineData("tin", 0xd3)]
	[InlineData("tia", 0xe3)]
	[InlineData("tai", 0xf3)]
	public void TryGetOpcode_BlockTransfer_ReturnsCorrectOpcode(string mnemonic, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.BlockTransfer, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Fact]
	public void EncodeBlockTransfer_Tdd_EncodesCorrectly() {
		var bytes = InstructionSetHuC6280.EncodeBlockTransfer(0xc3, 0x4000, 0x2100, 0x0800);

		Assert.Equal(7, bytes.Length);
		Assert.Equal(0xc3, bytes[0]);
		Assert.Equal(0x00, bytes[1]); // src low
		Assert.Equal(0x40, bytes[2]); // src high
		Assert.Equal(0x00, bytes[3]); // dst low
		Assert.Equal(0x21, bytes[4]); // dst high
		Assert.Equal(0x00, bytes[5]); // len low
		Assert.Equal(0x08, bytes[6]); // len high
	}

	[Fact]
	public void EncodeBlockTransfer_Tai_EncodesCorrectly() {
		var bytes = InstructionSetHuC6280.EncodeBlockTransfer(0xf3, 0x0200, 0x2108, 0x0010);

		Assert.Equal(7, bytes.Length);
		Assert.Equal(0xf3, bytes[0]);
		Assert.Equal(0x00, bytes[1]);
		Assert.Equal(0x02, bytes[2]);
		Assert.Equal(0x08, bytes[3]);
		Assert.Equal(0x21, bytes[4]);
		Assert.Equal(0x10, bytes[5]);
		Assert.Equal(0x00, bytes[6]);
	}

	[Fact]
	public void BlockTransferSize_Is7Bytes() {
		Assert.Equal(7, InstructionSetHuC6280.GetInstructionSize(HuC6280AddressingMode.BlockTransfer));
	}

	[Theory]
	[InlineData("tii")]
	[InlineData("tdd")]
	[InlineData("tin")]
	[InlineData("tia")]
	[InlineData("tai")]
	public void BlockTransfer_InvalidMode_ReturnsFalse(string mnemonic) {
		Assert.False(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.Absolute, out _));
	}

	#endregion

	#region TAM/TMA Memory Mapping Tests

	[Fact]
	public void TryGetOpcode_Tam_Returns53() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("tam", HuC6280AddressingMode.Immediate, out byte opcode));
		Assert.Equal(0x53, opcode);
	}

	[Fact]
	public void TryGetOpcode_Tma_Returns43() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("tma", HuC6280AddressingMode.Immediate, out byte opcode));
		Assert.Equal(0x43, opcode);
	}

	[Fact]
	public void EncodeTam_WithMpr7_Encodes2Bytes() {
		var bytes = InstructionSetHuC6280.EncodeImmediate(0x53, InstructionSetHuC6280.MemoryPages.MPR7);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0x53, bytes[0]);
		Assert.Equal(0x80, bytes[1]);
	}

	[Fact]
	public void EncodeTma_WithMpr0_Encodes2Bytes() {
		var bytes = InstructionSetHuC6280.EncodeImmediate(0x43, InstructionSetHuC6280.MemoryPages.MPR0);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0x43, bytes[0]);
		Assert.Equal(0x01, bytes[1]);
	}

	[Fact]
	public void Tam_OnlyAllowsImmediate() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("tam", HuC6280AddressingMode.Immediate, out _));
		Assert.False(InstructionSetHuC6280.TryGetOpcode("tam", HuC6280AddressingMode.ZeroPage, out _));
		Assert.False(InstructionSetHuC6280.TryGetOpcode("tam", HuC6280AddressingMode.Absolute, out _));
	}

	[Fact]
	public void Tma_OnlyAllowsImmediate() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("tma", HuC6280AddressingMode.Immediate, out _));
		Assert.False(InstructionSetHuC6280.TryGetOpcode("tma", HuC6280AddressingMode.ZeroPage, out _));
		Assert.False(InstructionSetHuC6280.TryGetOpcode("tma", HuC6280AddressingMode.Absolute, out _));
	}

	#endregion

	#region ST0/ST1/ST2 VDC Store Tests

	[Theory]
	[InlineData("st0", 0x03)]
	[InlineData("st1", 0x13)]
	[InlineData("st2", 0x23)]
	public void TryGetOpcode_VdcStore_ReturnsCorrectOpcode(string mnemonic, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.Immediate, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Theory]
	[InlineData("st0")]
	[InlineData("st1")]
	[InlineData("st2")]
	public void VdcStore_OnlyAllowsImmediate(string mnemonic) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.Immediate, out _));
		Assert.False(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.ZeroPage, out _));
		Assert.False(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.Absolute, out _));
	}

	[Fact]
	public void EncodeSt0_WithVdcRegister_Encodes2Bytes() {
		var bytes = InstructionSetHuC6280.EncodeImmediate(0x03, InstructionSetHuC6280.VdcRegisters.CR);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0x03, bytes[0]);
		Assert.Equal(0x05, bytes[1]); // CR = 0x05
	}

	#endregion

	#region CSL/CSH Speed Control Tests

	[Fact]
	public void TryGetOpcode_Csl_Returns54Implied() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("csl", HuC6280AddressingMode.Implied, out byte opcode));
		Assert.Equal(0x54, opcode);
	}

	[Fact]
	public void TryGetOpcode_Csh_ReturnsD4Implied() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("csh", HuC6280AddressingMode.Implied, out byte opcode));
		Assert.Equal(0xd4, opcode);
	}

	[Fact]
	public void CslCsh_OnlyAllowsImplied() {
		Assert.False(InstructionSetHuC6280.TryGetOpcode("csl", HuC6280AddressingMode.Immediate, out _));
		Assert.False(InstructionSetHuC6280.TryGetOpcode("csh", HuC6280AddressingMode.Immediate, out _));
	}

	#endregion

	#region SET Flag / Register Swap Tests

	[Theory]
	[InlineData("set", 0xf4)]
	[InlineData("sax", 0x22)]
	[InlineData("say", 0x42)]
	[InlineData("sxy", 0x02)]
	public void TryGetOpcode_HuC6280Implied_ReturnsCorrectOpcode(string mnemonic, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.Implied, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	#endregion

	#region Bit Operation Tests (RMB/SMB/BBR/BBS)

	[Theory]
	[InlineData("rmb0", 0x07)]
	[InlineData("rmb1", 0x17)]
	[InlineData("rmb2", 0x27)]
	[InlineData("rmb3", 0x37)]
	[InlineData("rmb4", 0x47)]
	[InlineData("rmb5", 0x57)]
	[InlineData("rmb6", 0x67)]
	[InlineData("rmb7", 0x77)]
	public void TryGetOpcode_Rmb_ReturnsCorrectOpcode(string mnemonic, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.ZeroPageBit, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Theory]
	[InlineData("smb0", 0x87)]
	[InlineData("smb1", 0x97)]
	[InlineData("smb2", 0xa7)]
	[InlineData("smb3", 0xb7)]
	[InlineData("smb4", 0xc7)]
	[InlineData("smb5", 0xd7)]
	[InlineData("smb6", 0xe7)]
	[InlineData("smb7", 0xf7)]
	public void TryGetOpcode_Smb_ReturnsCorrectOpcode(string mnemonic, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.ZeroPageBit, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Theory]
	[InlineData("bbr0", 0x0f)]
	[InlineData("bbr1", 0x1f)]
	[InlineData("bbr2", 0x2f)]
	[InlineData("bbr3", 0x3f)]
	[InlineData("bbr4", 0x4f)]
	[InlineData("bbr5", 0x5f)]
	[InlineData("bbr6", 0x6f)]
	[InlineData("bbr7", 0x7f)]
	public void TryGetOpcode_Bbr_ReturnsCorrectOpcode(string mnemonic, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.ZeroPageRelative, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Theory]
	[InlineData("bbs0", 0x8f)]
	[InlineData("bbs1", 0x9f)]
	[InlineData("bbs2", 0xaf)]
	[InlineData("bbs3", 0xbf)]
	[InlineData("bbs4", 0xcf)]
	[InlineData("bbs5", 0xdf)]
	[InlineData("bbs6", 0xef)]
	[InlineData("bbs7", 0xff)]
	public void TryGetOpcode_Bbs_ReturnsCorrectOpcode(string mnemonic, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.ZeroPageRelative, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Theory]
	[InlineData("rmb0")]
	[InlineData("smb3")]
	public void BitInstruction_WrongMode_ReturnsFalse(string mnemonic) {
		Assert.False(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.Absolute, out _));
		Assert.False(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.Immediate, out _));
	}

	[Fact]
	public void BitInstruction_InvalidBitNumber_ReturnsFalse() {
		Assert.False(InstructionSetHuC6280.IsValidMnemonic("rmb8"));
		Assert.False(InstructionSetHuC6280.IsValidMnemonic("smb9"));
		Assert.False(InstructionSetHuC6280.IsValidMnemonic("bbra"));
	}

	[Fact]
	public void EncodeZeroPageRelative_BbrEncoding() {
		// BBR3 $42, offset +5: opcode=$3f, zp=$42, rel=$05
		var bytes = InstructionSetHuC6280.EncodeZeroPageRelative(0x3f, 0x42, 5);

		Assert.Equal(3, bytes.Length);
		Assert.Equal(0x3f, bytes[0]);
		Assert.Equal(0x42, bytes[1]);
		Assert.Equal(0x05, bytes[2]);
	}

	[Fact]
	public void EncodeZeroPageRelative_BbsEncoding_NegativeOffset() {
		// BBS5 $10, offset -8: opcode=$df, zp=$10, rel=$f8
		var bytes = InstructionSetHuC6280.EncodeZeroPageRelative(0xdf, 0x10, -8);

		Assert.Equal(3, bytes.Length);
		Assert.Equal(0xdf, bytes[0]);
		Assert.Equal(0x10, bytes[1]);
		Assert.Equal(0xf8, bytes[2]);
	}

	[Fact]
	public void ZeroPageBitSize_Is2Bytes() {
		Assert.Equal(2, InstructionSetHuC6280.GetInstructionSize(HuC6280AddressingMode.ZeroPageBit));
	}

	[Fact]
	public void ZeroPageRelativeSize_Is3Bytes() {
		Assert.Equal(3, InstructionSetHuC6280.GetInstructionSize(HuC6280AddressingMode.ZeroPageRelative));
	}

	[Theory]
	[InlineData("bbr0")]
	[InlineData("bbr7")]
	[InlineData("bbs0")]
	[InlineData("bbs7")]
	public void BbrBbs_AreBranchInstructions(string mnemonic) {
		Assert.True(InstructionSetHuC6280.IsBranchInstruction(mnemonic));
	}

	[Theory]
	[InlineData("rmb0")]
	[InlineData("smb7")]
	public void RmbSmb_AreNotBranchInstructions(string mnemonic) {
		Assert.False(InstructionSetHuC6280.IsBranchInstruction(mnemonic));
	}

	#endregion

	#region TST Instruction Tests

	[Theory]
	[InlineData(HuC6280AddressingMode.ZeroPage, 0x83)]
	[InlineData(HuC6280AddressingMode.Absolute, 0x93)]
	[InlineData(HuC6280AddressingMode.ZeroPageX, 0xa3)]
	[InlineData(HuC6280AddressingMode.AbsoluteX, 0xb3)]
	public void TryGetOpcode_Tst_AllModes(HuC6280AddressingMode mode, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("tst", mode, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Fact]
	public void Tst_InvalidModes_ReturnsFalse() {
		Assert.False(InstructionSetHuC6280.TryGetOpcode("tst", HuC6280AddressingMode.Immediate, out _));
		Assert.False(InstructionSetHuC6280.TryGetOpcode("tst", HuC6280AddressingMode.Implied, out _));
	}

	#endregion

	#region 65C02 Base Opcode Compatibility Tests

	[Theory]
	[InlineData("lda", HuC6280AddressingMode.ZeroPageIndirect, 0xb2)]
	[InlineData("sta", HuC6280AddressingMode.ZeroPageIndirect, 0x92)]
	[InlineData("adc", HuC6280AddressingMode.ZeroPageIndirect, 0x72)]
	[InlineData("sbc", HuC6280AddressingMode.ZeroPageIndirect, 0xf2)]
	[InlineData("and", HuC6280AddressingMode.ZeroPageIndirect, 0x32)]
	[InlineData("ora", HuC6280AddressingMode.ZeroPageIndirect, 0x12)]
	[InlineData("eor", HuC6280AddressingMode.ZeroPageIndirect, 0x52)]
	[InlineData("cmp", HuC6280AddressingMode.ZeroPageIndirect, 0xd2)]
	public void TryGetOpcode_65C02_ZeroPageIndirect_CorrectOpcodes(string mnemonic, HuC6280AddressingMode mode, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, mode, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Theory]
	[InlineData("inc", HuC6280AddressingMode.Accumulator, 0x1a)]
	[InlineData("dec", HuC6280AddressingMode.Accumulator, 0x3a)]
	public void TryGetOpcode_65C02_IncDecAccumulator(string mnemonic, HuC6280AddressingMode mode, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, mode, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Theory]
	[InlineData("stz", HuC6280AddressingMode.ZeroPage, 0x64)]
	[InlineData("stz", HuC6280AddressingMode.ZeroPageX, 0x74)]
	[InlineData("stz", HuC6280AddressingMode.Absolute, 0x9c)]
	[InlineData("stz", HuC6280AddressingMode.AbsoluteX, 0x9e)]
	public void TryGetOpcode_65C02_Stz_AllModes(string mnemonic, HuC6280AddressingMode mode, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, mode, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Theory]
	[InlineData("trb", HuC6280AddressingMode.ZeroPage, 0x14)]
	[InlineData("trb", HuC6280AddressingMode.Absolute, 0x1c)]
	[InlineData("tsb", HuC6280AddressingMode.ZeroPage, 0x04)]
	[InlineData("tsb", HuC6280AddressingMode.Absolute, 0x0c)]
	public void TryGetOpcode_65C02_TrbTsb(string mnemonic, HuC6280AddressingMode mode, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, mode, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Theory]
	[InlineData("phx", 0xda)]
	[InlineData("phy", 0x5a)]
	[InlineData("plx", 0xfa)]
	[InlineData("ply", 0x7a)]
	public void TryGetOpcode_65C02_StackExtensions(string mnemonic, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.Implied, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Fact]
	public void TryGetOpcode_65C02_Bra_Returns80() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("bra", HuC6280AddressingMode.Relative, out byte opcode));
		Assert.Equal(0x80, opcode);
	}

	[Fact]
	public void TryGetOpcode_65C02_JmpAbsoluteIndirectX_Returns7C() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("jmp", HuC6280AddressingMode.AbsoluteIndirectX, out byte opcode));
		Assert.Equal(0x7c, opcode);
	}

	[Fact]
	public void TryGetOpcode_65C02_BitExtendedModes() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("bit", HuC6280AddressingMode.Immediate, out byte opcode));
		Assert.Equal(0x89, opcode);

		Assert.True(InstructionSetHuC6280.TryGetOpcode("bit", HuC6280AddressingMode.ZeroPageX, out opcode));
		Assert.Equal(0x34, opcode);

		Assert.True(InstructionSetHuC6280.TryGetOpcode("bit", HuC6280AddressingMode.AbsoluteX, out opcode));
		Assert.Equal(0x3c, opcode);
	}

	#endregion

	#region Standard 6502 Opcode Verification Tests

	[Theory]
	[InlineData("lda", HuC6280AddressingMode.Immediate, 0xa9)]
	[InlineData("lda", HuC6280AddressingMode.AbsoluteX, 0xbd)]
	[InlineData("lda", HuC6280AddressingMode.AbsoluteY, 0xb9)]
	[InlineData("lda", HuC6280AddressingMode.IndirectX, 0xa1)]
	[InlineData("lda", HuC6280AddressingMode.IndirectY, 0xb1)]
	[InlineData("sta", HuC6280AddressingMode.ZeroPage, 0x85)]
	[InlineData("sta", HuC6280AddressingMode.AbsoluteX, 0x9d)]
	[InlineData("sta", HuC6280AddressingMode.IndirectX, 0x81)]
	[InlineData("sta", HuC6280AddressingMode.IndirectY, 0x91)]
	public void TryGetOpcode_Standard6502_LoadStoreOpcodes(string mnemonic, HuC6280AddressingMode mode, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, mode, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Theory]
	[InlineData("asl", HuC6280AddressingMode.Accumulator, 0x0a)]
	[InlineData("lsr", HuC6280AddressingMode.Accumulator, 0x4a)]
	[InlineData("rol", HuC6280AddressingMode.Accumulator, 0x2a)]
	[InlineData("ror", HuC6280AddressingMode.Accumulator, 0x6a)]
	public void TryGetOpcode_Standard6502_ShiftAccumulator(string mnemonic, HuC6280AddressingMode mode, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, mode, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Theory]
	[InlineData("bcc", 0x90)]
	[InlineData("bcs", 0xb0)]
	[InlineData("beq", 0xf0)]
	[InlineData("bmi", 0x30)]
	[InlineData("bne", 0xd0)]
	[InlineData("bpl", 0x10)]
	[InlineData("bvc", 0x50)]
	[InlineData("bvs", 0x70)]
	public void TryGetOpcode_Standard6502_Branches(string mnemonic, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.Relative, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Theory]
	[InlineData("pha", 0x48)]
	[InlineData("php", 0x08)]
	[InlineData("pla", 0x68)]
	[InlineData("plp", 0x28)]
	[InlineData("tax", 0xaa)]
	[InlineData("tay", 0xa8)]
	[InlineData("txa", 0x8a)]
	[InlineData("tya", 0x98)]
	[InlineData("tsx", 0xba)]
	[InlineData("txs", 0x9a)]
	[InlineData("clc", 0x18)]
	[InlineData("cld", 0xd8)]
	[InlineData("cli", 0x58)]
	[InlineData("clv", 0xb8)]
	[InlineData("sec", 0x38)]
	[InlineData("sed", 0xf8)]
	[InlineData("sei", 0x78)]
	[InlineData("nop", 0xea)]
	[InlineData("brk", 0x00)]
	[InlineData("rts", 0x60)]
	[InlineData("rti", 0x40)]
	[InlineData("inx", 0xe8)]
	[InlineData("iny", 0xc8)]
	[InlineData("dex", 0xca)]
	[InlineData("dey", 0x88)]
	public void TryGetOpcode_Standard6502_ImpliedOpcodes(string mnemonic, byte expected) {
		Assert.True(InstructionSetHuC6280.TryGetOpcode(mnemonic, HuC6280AddressingMode.Implied, out byte opcode));
		Assert.Equal(expected, opcode);
	}

	[Fact]
	public void TryGetOpcode_Jsr_Returns20() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("jsr", HuC6280AddressingMode.Absolute, out byte opcode));
		Assert.Equal(0x20, opcode);
	}

	[Fact]
	public void TryGetOpcode_JmpIndirect_Returns6C() {
		Assert.True(InstructionSetHuC6280.TryGetOpcode("jmp", HuC6280AddressingMode.Indirect, out byte opcode));
		Assert.Equal(0x6c, opcode);
	}

	#endregion

	#region Instruction Size Comprehensive Tests

	[Theory]
	[InlineData(HuC6280AddressingMode.Implied, 1)]
	[InlineData(HuC6280AddressingMode.Accumulator, 1)]
	[InlineData(HuC6280AddressingMode.Immediate, 2)]
	[InlineData(HuC6280AddressingMode.ZeroPage, 2)]
	[InlineData(HuC6280AddressingMode.ZeroPageX, 2)]
	[InlineData(HuC6280AddressingMode.ZeroPageY, 2)]
	[InlineData(HuC6280AddressingMode.Relative, 2)]
	[InlineData(HuC6280AddressingMode.ZeroPageIndirect, 2)]
	[InlineData(HuC6280AddressingMode.IndirectX, 2)]
	[InlineData(HuC6280AddressingMode.IndirectY, 2)]
	[InlineData(HuC6280AddressingMode.ZeroPageBit, 2)]
	[InlineData(HuC6280AddressingMode.Absolute, 3)]
	[InlineData(HuC6280AddressingMode.AbsoluteX, 3)]
	[InlineData(HuC6280AddressingMode.AbsoluteY, 3)]
	[InlineData(HuC6280AddressingMode.Indirect, 3)]
	[InlineData(HuC6280AddressingMode.AbsoluteIndirectX, 3)]
	[InlineData(HuC6280AddressingMode.ZeroPageRelative, 3)]
	[InlineData(HuC6280AddressingMode.BlockTransfer, 7)]
	public void GetInstructionSize_AllModes_ReturnsCorrectSize(HuC6280AddressingMode mode, int expected) {
		Assert.Equal(expected, InstructionSetHuC6280.GetInstructionSize(mode));
	}

	#endregion

	#region TryGetEncoding (Shared Mode Mapping) Tests

	[Fact]
	public void TryGetEncoding_LdaImmediate_MapsCorrectly() {
		Assert.True(InstructionSetHuC6280.TryGetEncoding("lda", Poppy.Core.Parser.AddressingMode.Immediate, out byte opcode, out int size));
		Assert.Equal(0xa9, opcode);
		Assert.Equal(2, size);
	}

	[Fact]
	public void TryGetEncoding_StaZeroPageIndirect_MapsCorrectly() {
		Assert.True(InstructionSetHuC6280.TryGetEncoding("sta", Poppy.Core.Parser.AddressingMode.ZeroPageIndirect, out byte opcode, out int size));
		Assert.Equal(0x92, opcode);
		Assert.Equal(2, size);
	}

	[Fact]
	public void TryGetEncoding_JmpAbsoluteIndexedIndirect_MapsCorrectly() {
		Assert.True(InstructionSetHuC6280.TryGetEncoding("jmp", Poppy.Core.Parser.AddressingMode.AbsoluteIndexedIndirect, out byte opcode, out int size));
		Assert.Equal(0x7c, opcode);
		Assert.Equal(3, size);
	}

	[Fact]
	public void TryGetEncoding_InvalidMnemonic_ReturnsFalse() {
		Assert.False(InstructionSetHuC6280.TryGetEncoding("xyz", Poppy.Core.Parser.AddressingMode.Immediate, out _, out _));
	}

	#endregion

	#region Memory Page Constants Comprehensive Tests

	[Fact]
	public void MemoryPages_AllPagesArePowerOf2() {
		Assert.Equal(0x01, InstructionSetHuC6280.MemoryPages.MPR0);
		Assert.Equal(0x02, InstructionSetHuC6280.MemoryPages.MPR1);
		Assert.Equal(0x04, InstructionSetHuC6280.MemoryPages.MPR2);
		Assert.Equal(0x08, InstructionSetHuC6280.MemoryPages.MPR3);
		Assert.Equal(0x10, InstructionSetHuC6280.MemoryPages.MPR4);
		Assert.Equal(0x20, InstructionSetHuC6280.MemoryPages.MPR5);
		Assert.Equal(0x40, InstructionSetHuC6280.MemoryPages.MPR6);
		Assert.Equal(0x80, InstructionSetHuC6280.MemoryPages.MPR7);
	}

	#endregion

	#region Case Insensitivity Tests

	[Theory]
	[InlineData("LDA")]
	[InlineData("Lda")]
	[InlineData("lDa")]
	public void IsValidMnemonic_CaseInsensitive(string mnemonic) {
		Assert.True(InstructionSetHuC6280.IsValidMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("SMB0")]
	[InlineData("Smb0")]
	[InlineData("BBR7")]
	public void IsValidMnemonic_BitOps_CaseInsensitive(string mnemonic) {
		Assert.True(InstructionSetHuC6280.IsValidMnemonic(mnemonic));
	}

	#endregion
}
