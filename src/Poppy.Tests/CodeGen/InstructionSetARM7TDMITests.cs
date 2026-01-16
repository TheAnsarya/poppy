// InstructionSetARM7TDMITests.cs
// Unit tests for ARM7TDMI instruction set (GBA)

using Poppy.Core.CodeGen;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Unit tests for the ARM7TDMI instruction set implementation.
/// </summary>
public class InstructionSetARM7TDMITests {
	#region Mnemonic Validation Tests

	[Theory]
	[InlineData("mov")]
	[InlineData("add")]
	[InlineData("sub")]
	[InlineData("and")]
	[InlineData("orr")]
	[InlineData("eor")]
	[InlineData("bic")]
	[InlineData("mvn")]
	[InlineData("cmp")]
	[InlineData("cmn")]
	[InlineData("tst")]
	[InlineData("teq")]
	public void IsValidArmMnemonic_DataProcessing_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetARM7TDMI.IsValidArmMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("movs")]
	[InlineData("adds")]
	[InlineData("subs")]
	[InlineData("ands")]
	public void IsValidArmMnemonic_WithSFlag_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetARM7TDMI.IsValidArmMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("addeq")]
	[InlineData("subne")]
	[InlineData("movgt")]
	[InlineData("andlt")]
	public void IsValidArmMnemonic_WithCondition_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetARM7TDMI.IsValidArmMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("b")]
	[InlineData("bl")]
	[InlineData("bx")]
	[InlineData("blx")]
	public void IsValidArmMnemonic_Branch_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetARM7TDMI.IsValidArmMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("ldr")]
	[InlineData("str")]
	[InlineData("ldrb")]
	[InlineData("strb")]
	[InlineData("ldm")]
	[InlineData("stm")]
	public void IsValidArmMnemonic_LoadStore_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetARM7TDMI.IsValidArmMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("xyz")]
	[InlineData("foo")]
	[InlineData("")]
	public void IsValidArmMnemonic_Invalid_ReturnsFalse(string mnemonic) {
		Assert.False(InstructionSetARM7TDMI.IsValidArmMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("mov")]
	[InlineData("add")]
	[InlineData("sub")]
	[InlineData("ldr")]
	[InlineData("str")]
	[InlineData("b")]
	[InlineData("push")]
	[InlineData("pop")]
	public void IsValidThumbMnemonic_Common_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetARM7TDMI.IsValidThumbMnemonic(mnemonic));
	}

	[Theory]
	[InlineData("beq")]
	[InlineData("bne")]
	[InlineData("bcs")]
	[InlineData("bcc")]
	public void IsValidThumbMnemonic_ConditionalBranch_ReturnsTrue(string mnemonic) {
		Assert.True(InstructionSetARM7TDMI.IsValidThumbMnemonic(mnemonic));
	}

	#endregion

	#region Register Tests

	[Theory]
	[InlineData("r0", 0)]
	[InlineData("r1", 1)]
	[InlineData("r15", 15)]
	[InlineData("sp", 13)]
	[InlineData("lr", 14)]
	[InlineData("pc", 15)]
	public void TryGetRegister_ValidRegister_ReturnsCorrectNumber(string name, int expected) {
		Assert.True(InstructionSetARM7TDMI.TryGetRegister(name, out int register));
		Assert.Equal(expected, register);
	}

	[Theory]
	[InlineData("r16")]
	[InlineData("")]
	[InlineData("xyz")]
	public void TryGetRegister_InvalidRegister_ReturnsFalse(string name) {
		Assert.False(InstructionSetARM7TDMI.TryGetRegister(name, out _));
	}

	#endregion

	#region Condition Parsing Tests

	[Fact]
	public void TryParseCondition_NoCondition_ReturnsAL() {
		Assert.True(InstructionSetARM7TDMI.TryParseCondition("add", out string baseMnemonic, out byte condition));
		Assert.Equal("add", baseMnemonic);
		Assert.Equal(InstructionSetARM7TDMI.Conditions.AL, condition);
	}

	[Fact]
	public void TryParseCondition_WithCondition_ParsesCorrectly() {
		Assert.True(InstructionSetARM7TDMI.TryParseCondition("addeq", out string baseMnemonic, out byte condition));
		Assert.Equal("add", baseMnemonic);
		Assert.Equal(InstructionSetARM7TDMI.Conditions.EQ, condition);
	}

	#endregion

	#region Instruction Classification Tests

	[Theory]
	[InlineData("b", true)]
	[InlineData("bl", true)]
	[InlineData("bx", true)]
	[InlineData("beq", true)]
	[InlineData("bne", true)]
	[InlineData("mov", false)]
	[InlineData("ldr", false)]
	public void IsBranchInstruction_ReturnsCorrectResult(string mnemonic, bool expected) {
		Assert.Equal(expected, InstructionSetARM7TDMI.IsBranchInstruction(mnemonic));
	}

	[Theory]
	[InlineData("ldr", true)]
	[InlineData("str", true)]
	[InlineData("ldrb", true)]
	[InlineData("ldm", true)]
	[InlineData("stm", true)]
	[InlineData("push", true)]
	[InlineData("pop", true)]
	[InlineData("mov", false)]
	[InlineData("add", false)]
	public void IsLoadStoreInstruction_ReturnsCorrectResult(string mnemonic, bool expected) {
		Assert.Equal(expected, InstructionSetARM7TDMI.IsLoadStoreInstruction(mnemonic));
	}

	[Theory]
	[InlineData("adds", true)]
	[InlineData("movs", true)]
	[InlineData("cmp", true)]
	[InlineData("tst", true)]
	[InlineData("add", false)]
	[InlineData("mov", false)]
	public void SetsFlags_ReturnsCorrectResult(string mnemonic, bool expected) {
		Assert.Equal(expected, InstructionSetARM7TDMI.SetsFlags(mnemonic));
	}

	#endregion

	#region Encoding Tests - Data Processing

	[Fact]
	public void EncodeDataProcessingImmediate_MovR0Imm_EncodesCorrectly() {
		// MOV R0,#42 (condition AL)
		var bytes = InstructionSetARM7TDMI.EncodeDataProcessingImmediate(
			InstructionSetARM7TDMI.ArmOpcodes.MOV, 0, 0, 42);

		Assert.Equal(4, bytes.Length);
		// Check condition is AL (0xe) in bits 28-31
		Assert.Equal(0xe0, bytes[3] & 0xf0);
		// Check immediate bit is set
		Assert.Equal(0x02, bytes[3] & 0x02);
	}

	[Fact]
	public void EncodeDataProcessingRegister_AddR0R1R2_EncodesCorrectly() {
		// ADD R0,R1,R2 (condition AL)
		var bytes = InstructionSetARM7TDMI.EncodeDataProcessingRegister(
			InstructionSetARM7TDMI.ArmOpcodes.ADD, 0, 1, 2);

		Assert.Equal(4, bytes.Length);
		// Check destination register is R0
		Assert.Equal(0x00, bytes[1] & 0x0f);
	}

	#endregion

	#region Encoding Tests - Branch

	[Fact]
	public void EncodeBranch_Unconditional_EncodesCorrectly() {
		// B +8 (branch forward 8 bytes)
		var bytes = InstructionSetARM7TDMI.EncodeBranch(8);

		Assert.Equal(4, bytes.Length);
		// Check branch opcode (101) in bits 25-27
		Assert.Equal(0x0a, bytes[3] & 0x0f);
	}

	[Fact]
	public void EncodeBranch_WithLink_EncodesCorrectly() {
		// BL +8 (branch with link)
		var bytes = InstructionSetARM7TDMI.EncodeBranch(8, link: true);

		Assert.Equal(4, bytes.Length);
		// Check link bit is set (bit 24)
		Assert.Equal(0x0b, bytes[3] & 0x0f);
	}

	[Fact]
	public void EncodeBranchExchange_R0_EncodesCorrectly() {
		// BX R0
		var bytes = InstructionSetARM7TDMI.EncodeBranchExchange(0);

		Assert.Equal(4, bytes.Length);
		// Check Rm is R0
		Assert.Equal(0x10, bytes[0]);  // 0001 0000
		Assert.Equal(0xff, bytes[1]);  // 1111 1111
		Assert.Equal(0x2f, bytes[2]);  // 0010 1111
	}

	#endregion

	#region Encoding Tests - Load/Store

	[Fact]
	public void EncodeLoadStoreImmediate_LdrR0R1_EncodesCorrectly() {
		// LDR R0,[R1,#4]
		var bytes = InstructionSetARM7TDMI.EncodeLoadStoreImmediate(
			isLoad: true, rd: 0, rn: 1, offset: 4);

		Assert.Equal(4, bytes.Length);
	}

	[Fact]
	public void EncodeLoadStoreMultiple_Push_EncodesCorrectly() {
		// PUSH {R0,R1,LR}
		ushort regList = InstructionSetARM7TDMI.CreateRegisterList(0, 1, 14);
		var bytes = InstructionSetARM7TDMI.EncodeLoadStoreMultiple(
			isLoad: false, rn: 13, registerList: regList,
			increment: false, before: true, writeBack: true);

		Assert.Equal(4, bytes.Length);
	}

	#endregion

	#region Encoding Tests - Thumb

	[Fact]
	public void EncodeThumbMovImmediate_R0Imm42_EncodesCorrectly() {
		// MOV R0,#42
		var bytes = InstructionSetARM7TDMI.EncodeThumbMovImmediate(0, 42);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(42, bytes[0]);
		Assert.Equal(0x20, bytes[1]);  // 0010 0000 (MOV R0)
	}

	[Fact]
	public void EncodeThumbAddSubRegister_Add_EncodesCorrectly() {
		// ADD R0,R1,R2
		var bytes = InstructionSetARM7TDMI.EncodeThumbAddSubRegister(0, 1, 2);

		Assert.Equal(2, bytes.Length);
	}

	[Fact]
	public void EncodeThumbBranch_Forward_EncodesCorrectly() {
		// B +16
		var bytes = InstructionSetARM7TDMI.EncodeThumbBranch(16);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0xe0, bytes[1] & 0xf8);  // 11100 xxx
	}

	[Fact]
	public void EncodeThumbPush_WithLR_EncodesCorrectly() {
		// PUSH {R0,R1,LR}
		var bytes = InstructionSetARM7TDMI.EncodeThumbPush(0x03, pushLr: true);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0x03, bytes[0]);  // R0, R1
		Assert.Equal(0xb5, bytes[1]);  // 1011 0101 (PUSH with LR)
	}

	[Fact]
	public void EncodeThumbPop_WithPC_EncodesCorrectly() {
		// POP {R0,R1,PC}
		var bytes = InstructionSetARM7TDMI.EncodeThumbPop(0x03, popPc: true);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0x03, bytes[0]);  // R0, R1
		Assert.Equal(0xbd, bytes[1]);  // 1011 1101 (POP with PC)
	}

	#endregion

	#region Utility Tests

	[Fact]
	public void CreateRegisterList_MultipleRegisters_CreatesCorrectMask() {
		ushort mask = InstructionSetARM7TDMI.CreateRegisterList(0, 1, 4, 14, 15);

		Assert.Equal(0xc013, mask);  // R0, R1, R4, LR, PC
	}

	[Fact]
	public void TryEncodeImmediate_SimpleValue_EncodesCorrectly() {
		Assert.True(InstructionSetARM7TDMI.TryEncodeImmediate(255, out byte imm, out byte rot));
		Assert.Equal(255, imm);
		Assert.Equal(0, rot);
	}

	[Fact]
	public void TryEncodeImmediate_RotatedValue_EncodesCorrectly() {
		// 0x00ff0000 can be encoded as 0xff rotated right by 16 (rot=8)
		Assert.True(InstructionSetARM7TDMI.TryEncodeImmediate(0x00ff0000, out byte imm, out byte rot));
		Assert.Equal(0xff, imm);
		Assert.Equal(8, rot);  // 8 * 2 = 16 bits rotation
	}

	[Fact]
	public void WriteLong_Value_WritesLittleEndian() {
		var bytes = InstructionSetARM7TDMI.WriteLong(0x12345678);

		Assert.Equal(4, bytes.Length);
		Assert.Equal(0x78, bytes[0]);
		Assert.Equal(0x56, bytes[1]);
		Assert.Equal(0x34, bytes[2]);
		Assert.Equal(0x12, bytes[3]);
	}

	[Fact]
	public void WriteHalf_Value_WritesLittleEndian() {
		var bytes = InstructionSetARM7TDMI.WriteHalf(0x1234);

		Assert.Equal(2, bytes.Length);
		Assert.Equal(0x34, bytes[0]);
		Assert.Equal(0x12, bytes[1]);
	}

	#endregion
}
