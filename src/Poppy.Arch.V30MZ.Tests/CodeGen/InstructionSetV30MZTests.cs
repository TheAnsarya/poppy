// ============================================================================
// InstructionSetV30MZTests.cs - V30MZ Instruction Set Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================
// Tests for V30MZ (8086-compatible) instruction encoding used in WonderSwan.
// ============================================================================

using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;
using Xunit;

using PoppyLexer = Poppy.Core.Lexer.Lexer;
using PoppyParser = Poppy.Core.Parser.Parser;

namespace Poppy.Arch.V30MZ.Tests.CodeGen;

/// <summary>
/// Unit tests for the V30MZ (8086-compatible) instruction set implementation.
/// </summary>
public class InstructionSetV30MZTests {
	/// <summary>
	/// Helper to generate V30MZ code from source and return raw bytes.
	/// </summary>
	private static (byte[] Code, CodeGenerator Generator) GenerateV30MZCode(string source) {
		var lexer = new PoppyLexer(source);
		var tokens = lexer.Tokenize();
		var parser = new PoppyParser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer();
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.V30MZ);
		var code = generator.Generate(program);

		return (code, generator);
	}

	#region Implied Instruction Tests

	[Theory]
	[InlineData("nop", 0x90)]
	[InlineData("hlt", 0xf4)]
	[InlineData("cmc", 0xf5)]
	[InlineData("clc", 0xf8)]
	[InlineData("stc", 0xf9)]
	[InlineData("cli", 0xfa)]
	[InlineData("sti", 0xfb)]
	[InlineData("cld", 0xfc)]
	[InlineData("std", 0xfd)]
	public void ImpliedInstruction_FlagOps_EncodesCorrectly(string mnemonic, byte expected) {
		var source = $".target wonderswan\n{mnemonic}";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(expected, code[0]);
	}

	[Theory]
	[InlineData("pushf", 0x9c)]
	[InlineData("popf", 0x9d)]
	[InlineData("pusha", 0x60)]
	[InlineData("popa", 0x61)]
	[InlineData("sahf", 0x9e)]
	[InlineData("lahf", 0x9f)]
	[InlineData("cbw", 0x98)]
	[InlineData("cwd", 0x99)]
	[InlineData("wait", 0x9b)]
	[InlineData("xlat", 0xd7)]
	[InlineData("xlatb", 0xd7)]
	public void ImpliedInstruction_StackAndData_EncodesCorrectly(string mnemonic, byte expected) {
		var source = $".target wonderswan\n{mnemonic}";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(expected, code[0]);
	}

	[Theory]
	[InlineData("movsb", 0xa4)]
	[InlineData("movsw", 0xa5)]
	[InlineData("cmpsb", 0xa6)]
	[InlineData("cmpsw", 0xa7)]
	[InlineData("stosb", 0xaa)]
	[InlineData("stosw", 0xab)]
	[InlineData("lodsb", 0xac)]
	[InlineData("lodsw", 0xad)]
	[InlineData("scasb", 0xae)]
	[InlineData("scasw", 0xaf)]
	public void ImpliedInstruction_StringOps_EncodesCorrectly(string mnemonic, byte expected) {
		var source = $".target wonderswan\n{mnemonic}";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(expected, code[0]);
	}

	[Theory]
	[InlineData("rep", 0xf3)]
	[InlineData("repe", 0xf3)]
	[InlineData("repz", 0xf3)]
	[InlineData("repne", 0xf2)]
	[InlineData("repnz", 0xf2)]
	public void ImpliedInstruction_RepPrefixes_EncodesCorrectly(string mnemonic, byte expected) {
		var source = $".target wonderswan\n{mnemonic}";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(expected, code[0]);
	}

	[Theory]
	[InlineData("ret", 0xc3)]
	[InlineData("retf", 0xcb)]
	[InlineData("iret", 0xcf)]
	public void ImpliedInstruction_Returns_EncodesCorrectly(string mnemonic, byte expected) {
		var source = $".target wonderswan\n{mnemonic}";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(expected, code[0]);
	}

	[Theory]
	[InlineData("daa", 0x27)]
	[InlineData("das", 0x2f)]
	[InlineData("aaa", 0x37)]
	[InlineData("aas", 0x3f)]
	[InlineData("int3", 0xcc)]
	[InlineData("into", 0xce)]
	public void ImpliedInstruction_BcdAndInterrupt_EncodesCorrectly(string mnemonic, byte expected) {
		var source = $".target wonderswan\n{mnemonic}";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(expected, code[0]);
	}

	[Fact]
	public void ImpliedInstruction_AAM_EncodesAsTwoBytes() {
		var source = ".target wonderswan\naam";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0xd4, code[0]);
		Assert.Equal(0x0a, code[1]);
	}

	[Fact]
	public void ImpliedInstruction_AAD_EncodesAsTwoBytes() {
		var source = ".target wonderswan\naad";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0xd5, code[0]);
		Assert.Equal(0x0a, code[1]);
	}

	#endregion

	#region Register Map Tests

	[Theory]
	[InlineData("ax", 0, true)]
	[InlineData("cx", 1, true)]
	[InlineData("dx", 2, true)]
	[InlineData("bx", 3, true)]
	[InlineData("sp", 4, true)]
	[InlineData("bp", 5, true)]
	[InlineData("si", 6, true)]
	[InlineData("di", 7, true)]
	public void RegisterMap_16BitRegisters_CorrectEncoding(string name, int expectedEnc, bool expectedWord) {
		Assert.True(InstructionSetV30MZ.TryGetRegister(name, out var encoding, out var isWord));
		Assert.Equal(expectedEnc, encoding);
		Assert.Equal(expectedWord, isWord);
	}

	[Theory]
	[InlineData("al", 0, false)]
	[InlineData("cl", 1, false)]
	[InlineData("dl", 2, false)]
	[InlineData("bl", 3, false)]
	[InlineData("ah", 4, false)]
	[InlineData("ch", 5, false)]
	[InlineData("dh", 6, false)]
	[InlineData("bh", 7, false)]
	public void RegisterMap_8BitRegisters_CorrectEncoding(string name, int expectedEnc, bool expectedWord) {
		Assert.True(InstructionSetV30MZ.TryGetRegister(name, out var encoding, out var isWord));
		Assert.Equal(expectedEnc, encoding);
		Assert.Equal(expectedWord, isWord);
	}

	[Theory]
	[InlineData("es")]
	[InlineData("cs")]
	[InlineData("ss")]
	[InlineData("ds")]
	public void RegisterMap_SegmentRegisters_AreRecognized(string name) {
		Assert.True(InstructionSetV30MZ.IsRegister(name));
		Assert.True(InstructionSetV30MZ.IsSegmentRegister(name));
	}

	[Theory]
	[InlineData("ax")]
	[InlineData("bx")]
	[InlineData("al")]
	[InlineData("bh")]
	public void RegisterMap_GeneralRegisters_NotSegment(string name) {
		Assert.True(InstructionSetV30MZ.IsRegister(name));
		Assert.False(InstructionSetV30MZ.IsSegmentRegister(name));
	}

	[Theory]
	[InlineData("xyz")]
	[InlineData("rax")]
	[InlineData("eax")]
	public void RegisterMap_InvalidNames_ReturnsFalse(string name) {
		Assert.False(InstructionSetV30MZ.IsRegister(name));
	}

	#endregion

	#region ModR/M Encoding Tests

	[Fact]
	public void EncodeModRM_RegToReg_CorrectByte() {
		// mod=3 (register), reg=0 (AX), rm=3 (BX)
		var result = InstructionSetV30MZ.EncodeModRM(3, 0, 3);
		Assert.Equal(0xc3, result); // 11_000_011
	}

	[Fact]
	public void EncodeModRM_AllBitsSet() {
		// mod=3, reg=7, rm=7
		var result = InstructionSetV30MZ.EncodeModRM(3, 7, 7);
		Assert.Equal(0xff, result); // 11_111_111
	}

	[Fact]
	public void EncodeModRM_IndirectBxSi() {
		// mod=0 (indirect), reg=0 (AX), rm=0 ([BX+SI])
		var result = InstructionSetV30MZ.EncodeModRM(0, 0, 0);
		Assert.Equal(0x00, result); // 00_000_000
	}

	[Fact]
	public void EncodeModRM_Displacement8() {
		// mod=1 (8-bit disp), reg=2 (DX), rm=6 ([BP+disp8])
		var result = InstructionSetV30MZ.EncodeModRM(1, 2, 6);
		Assert.Equal(0x56, result); // 01_010_110
	}

	#endregion

	#region Conditional Jump Tests

	[Theory]
	[InlineData("jo", 0x70)]
	[InlineData("jno", 0x71)]
	[InlineData("jb", 0x72)]
	[InlineData("jc", 0x72)]
	[InlineData("jnae", 0x72)]
	[InlineData("jnb", 0x73)]
	[InlineData("jnc", 0x73)]
	[InlineData("jae", 0x73)]
	[InlineData("jz", 0x74)]
	[InlineData("je", 0x74)]
	[InlineData("jnz", 0x75)]
	[InlineData("jne", 0x75)]
	[InlineData("jbe", 0x76)]
	[InlineData("jna", 0x76)]
	[InlineData("jnbe", 0x77)]
	[InlineData("ja", 0x77)]
	[InlineData("js", 0x78)]
	[InlineData("jns", 0x79)]
	[InlineData("jp", 0x7a)]
	[InlineData("jpe", 0x7a)]
	[InlineData("jnp", 0x7b)]
	[InlineData("jpo", 0x7b)]
	[InlineData("jl", 0x7c)]
	[InlineData("jnge", 0x7c)]
	[InlineData("jnl", 0x7d)]
	[InlineData("jge", 0x7d)]
	[InlineData("jle", 0x7e)]
	[InlineData("jng", 0x7e)]
	[InlineData("jnle", 0x7f)]
	[InlineData("jg", 0x7f)]
	public void ConditionalJump_Lookup_CorrectOpcode(string mnemonic, byte expected) {
		Assert.True(InstructionSetV30MZ.TryGetConditionalJump(mnemonic, out var opcode));
		Assert.Equal(expected, opcode);
	}

	#endregion

	#region Loop Instruction Tests

	[Theory]
	[InlineData("loopnz", 0xe0)]
	[InlineData("loopne", 0xe0)]
	[InlineData("loopz", 0xe1)]
	[InlineData("loope", 0xe1)]
	[InlineData("loop", 0xe2)]
	[InlineData("jcxz", 0xe3)]
	public void LoopInstruction_Lookup_CorrectOpcode(string mnemonic, byte expected) {
		Assert.True(InstructionSetV30MZ.TryGetLoopInstruction(mnemonic, out var opcode));
		Assert.Equal(expected, opcode);
	}

	#endregion

	#region Branch Detection Tests

	[Theory]
	[InlineData("jmp", true)]
	[InlineData("call", true)]
	[InlineData("jz", true)]
	[InlineData("jne", true)]
	[InlineData("ja", true)]
	[InlineData("loop", true)]
	[InlineData("jcxz", true)]
	[InlineData("nop", false)]
	[InlineData("hlt", false)]
	[InlineData("ret", false)]
	[InlineData("push", false)]
	[InlineData("mov", false)]
	public void IsBranchInstruction_CorrectDetection(string mnemonic, bool expected) {
		Assert.Equal(expected, InstructionSetV30MZ.IsBranchInstruction(mnemonic));
	}

	#endregion

	#region TryGetEncodingFromShared Tests

	[Fact]
	public void TryGetEncodingFromShared_Nop_ReturnsCorrect() {
		Assert.True(InstructionSetV30MZ.TryGetEncodingFromShared("nop", AddressingMode.Implied, out var opcode, out var size));
		Assert.Equal(0x90, opcode);
		Assert.Equal(1, size);
	}

	[Fact]
	public void TryGetEncodingFromShared_Hlt_ReturnsCorrect() {
		Assert.True(InstructionSetV30MZ.TryGetEncodingFromShared("hlt", AddressingMode.Implied, out var opcode, out var size));
		Assert.Equal(0xf4, opcode);
		Assert.Equal(1, size);
	}

	[Fact]
	public void TryGetEncodingFromShared_AAM_ReturnsSizeTwo() {
		Assert.True(InstructionSetV30MZ.TryGetEncodingFromShared("aam", AddressingMode.Implied, out var opcode, out var size));
		Assert.Equal(0xd4, opcode);
		Assert.Equal(2, size);
	}

	[Fact]
	public void TryGetEncodingFromShared_ConditionalJump_WithRelativeMode() {
		Assert.True(InstructionSetV30MZ.TryGetEncodingFromShared("jz", AddressingMode.Relative, out var opcode, out var size));
		Assert.Equal(0x74, opcode);
		Assert.Equal(2, size);
	}

	[Fact]
	public void TryGetEncodingFromShared_ConditionalJump_WithAbsoluteMode() {
		// Absolute is also accepted (gets resolved to relative by CodeGenerator)
		Assert.True(InstructionSetV30MZ.TryGetEncodingFromShared("jne", AddressingMode.Absolute, out var opcode, out var size));
		Assert.Equal(0x75, opcode);
		Assert.Equal(2, size);
	}

	[Fact]
	public void TryGetEncodingFromShared_Loop_Correct() {
		Assert.True(InstructionSetV30MZ.TryGetEncodingFromShared("loop", AddressingMode.Relative, out var opcode, out var size));
		Assert.Equal(0xe2, opcode);
		Assert.Equal(2, size);
	}

	[Fact]
	public void TryGetEncodingFromShared_UnknownMnemonic_ReturnsFalse() {
		Assert.False(InstructionSetV30MZ.TryGetEncodingFromShared("xyz", AddressingMode.Implied, out _, out _));
	}

	[Fact]
	public void TryGetEncodingFromShared_ImpliedWithWrongMode_ReturnsFalse() {
		// NOP with Immediate mode should not match
		Assert.False(InstructionSetV30MZ.TryGetEncodingFromShared("nop", AddressingMode.Immediate, out _, out _));
	}

	#endregion

	#region PUSH/POP Register Code Generation Tests

	[Theory]
	[InlineData("push ax", 0x50)]
	[InlineData("push cx", 0x51)]
	[InlineData("push dx", 0x52)]
	[InlineData("push bx", 0x53)]
	[InlineData("push sp", 0x54)]
	[InlineData("push bp", 0x55)]
	[InlineData("push si", 0x56)]
	[InlineData("push di", 0x57)]
	public void CodeGen_PushReg16_CorrectOpcode(string instruction, byte expected) {
		var source = $".target wonderswan\n{instruction}";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(expected, code[0]);
	}

	[Theory]
	[InlineData("pop ax", 0x58)]
	[InlineData("pop cx", 0x59)]
	[InlineData("pop dx", 0x5a)]
	[InlineData("pop bx", 0x5b)]
	[InlineData("pop sp", 0x5c)]
	[InlineData("pop bp", 0x5d)]
	[InlineData("pop si", 0x5e)]
	[InlineData("pop di", 0x5f)]
	public void CodeGen_PopReg16_CorrectOpcode(string instruction, byte expected) {
		var source = $".target wonderswan\n{instruction}";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(expected, code[0]);
	}

	#endregion

	#region PUSH/POP Segment Register Tests

	[Theory]
	[InlineData("push es", 0x06)]
	[InlineData("push cs", 0x0e)]
	[InlineData("push ss", 0x16)]
	[InlineData("push ds", 0x1e)]
	public void CodeGen_PushSegment_CorrectOpcode(string instruction, byte expected) {
		var source = $".target wonderswan\n{instruction}";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(expected, code[0]);
	}

	[Theory]
	[InlineData("pop es", 0x07)]
	[InlineData("pop ss", 0x17)]
	[InlineData("pop ds", 0x1f)]
	public void CodeGen_PopSegment_CorrectOpcode(string instruction, byte expected) {
		var source = $".target wonderswan\n{instruction}";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(expected, code[0]);
	}

	#endregion

	#region INC/DEC Register Tests

	[Theory]
	[InlineData("inc ax", 0x40)]
	[InlineData("inc cx", 0x41)]
	[InlineData("inc dx", 0x42)]
	[InlineData("inc bx", 0x43)]
	[InlineData("inc sp", 0x44)]
	[InlineData("inc bp", 0x45)]
	[InlineData("inc si", 0x46)]
	[InlineData("inc di", 0x47)]
	public void CodeGen_IncReg16_CorrectOpcode(string instruction, byte expected) {
		var source = $".target wonderswan\n{instruction}";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(expected, code[0]);
	}

	[Theory]
	[InlineData("dec ax", 0x48)]
	[InlineData("dec cx", 0x49)]
	[InlineData("dec dx", 0x4a)]
	[InlineData("dec bx", 0x4b)]
	[InlineData("dec sp", 0x4c)]
	[InlineData("dec bp", 0x4d)]
	[InlineData("dec si", 0x4e)]
	[InlineData("dec di", 0x4f)]
	public void CodeGen_DecReg16_CorrectOpcode(string instruction, byte expected) {
		var source = $".target wonderswan\n{instruction}";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(expected, code[0]);
	}

	#endregion

	#region INT n Tests

	[Fact]
	public void CodeGen_IntN_EncodesCorrectly() {
		var source = ".target wonderswan\nint #$21";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0xcd, code[0]); // INT opcode
		Assert.Equal(0x21, code[1]); // Interrupt number
	}

	[Fact]
	public void CodeGen_IntZero_EncodesCorrectly() {
		var source = ".target wonderswan\nint #$00";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0xcd, code[0]);
		Assert.Equal(0x00, code[1]);
	}

	#endregion

	#region Near JMP/CALL Tests

	[Fact]
	public void CodeGen_NearJmp_EmitsCorrectBytes() {
		// JMP to address $0003 from address $0000
		// Instruction is 3 bytes: $e9 + rel16
		// offset = target - (instruction_addr + 3) = $0003 - $0003 = $0000
		var source = ".target wonderswan\njmp target\ntarget:\nnop";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0xe9, code[0]); // JMP near opcode
		Assert.Equal(0x00, code[1]); // rel16 low byte
		Assert.Equal(0x00, code[2]); // rel16 high byte
		Assert.Equal(0x90, code[3]); // NOP at target
	}

	[Fact]
	public void CodeGen_NearCall_EmitsCorrectBytes() {
		// CALL to address $0003 from address $0000
		// offset = target - (instruction_addr + 3) = $0003 - $0003 = $0000
		var source = ".target wonderswan\ncall target\ntarget:\nnop";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0xe8, code[0]); // CALL near opcode
		Assert.Equal(0x00, code[1]); // rel16 low byte
		Assert.Equal(0x00, code[2]); // rel16 high byte
		Assert.Equal(0x90, code[3]); // NOP at target
	}

	[Fact]
	public void CodeGen_NearJmp_ForwardOffset() {
		// JMP over 2 NOPs:
		// $0000: JMP target (3 bytes: $e9, lo, hi)
		// $0003: NOP
		// $0004: NOP
		// $0005: target: NOP
		// offset = $0005 - ($0000 + 3) = $0002
		var source = ".target wonderswan\njmp target\nnop\nnop\ntarget:\nnop";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0xe9, code[0]);
		Assert.Equal(0x02, code[1]); // offset = 2
		Assert.Equal(0x00, code[2]);
	}

	#endregion

	#region RET/RETF with Immediate Tests

	[Fact]
	public void CodeGen_RetImm16_EncodesCorrectly() {
		var source = ".target wonderswan\nret #$04";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0xc2, code[0]); // RET imm16 opcode
		Assert.Equal(0x04, code[1]); // imm16 low
		Assert.Equal(0x00, code[2]); // imm16 high
	}

	[Fact]
	public void CodeGen_RetfImm16_EncodesCorrectly() {
		var source = ".target wonderswan\nretf #$08";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0xca, code[0]); // RETF imm16 opcode
		Assert.Equal(0x08, code[1]); // imm16 low
		Assert.Equal(0x00, code[2]); // imm16 high
	}

	[Fact]
	public void CodeGen_RetWithoutOperand_IsImplied() {
		var source = ".target wonderswan\nret";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0xc3, code[0]); // RET (implied, no stack adjustment)
	}

	#endregion

	#region PUSH Immediate Tests

	[Fact]
	public void CodeGen_PushImm8_EncodesCorrectly() {
		var source = ".target wonderswan\npush #$42";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0x6a, code[0]); // PUSH imm8 (80186+)
		Assert.Equal(0x42, code[1]);
	}

	[Fact]
	public void CodeGen_PushImm16_EncodesCorrectly() {
		var source = ".target wonderswan\npush #$1234";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0x68, code[0]); // PUSH imm16 (80186+)
		Assert.Equal(0x34, code[1]); // low byte
		Assert.Equal(0x12, code[2]); // high byte
	}

	#endregion

	#region Mnemonic List Tests

	[Fact]
	public void GetAllMnemonics_ContainsImplied() {
		var mnemonics = InstructionSetV30MZ.GetAllMnemonics().ToList();
		Assert.Contains("nop", mnemonics);
		Assert.Contains("hlt", mnemonics);
		Assert.Contains("ret", mnemonics);
		Assert.Contains("iret", mnemonics);
	}

	[Fact]
	public void GetAllMnemonics_ContainsConditionalJumps() {
		var mnemonics = InstructionSetV30MZ.GetAllMnemonics().ToList();
		Assert.Contains("jz", mnemonics);
		Assert.Contains("jne", mnemonics);
		Assert.Contains("jg", mnemonics);
	}

	[Fact]
	public void GetAllMnemonics_ContainsParametric() {
		var mnemonics = InstructionSetV30MZ.GetAllMnemonics().ToList();
		Assert.Contains("mov", mnemonics);
		Assert.Contains("add", mnemonics);
		Assert.Contains("sub", mnemonics);
		Assert.Contains("cmp", mnemonics);
		Assert.Contains("push", mnemonics);
		Assert.Contains("pop", mnemonics);
	}

	[Fact]
	public void GetAllMnemonics_NoDuplicates() {
		var mnemonics = InstructionSetV30MZ.GetAllMnemonics().ToList();
		var distinct = mnemonics.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		Assert.Equal(distinct.Count, mnemonics.Count);
	}

	#endregion

	#region Combined Instruction Sequences

	[Fact]
	public void CodeGen_MultipleImplied_SequentialBytes() {
		var source = ".target wonderswan\nnop\nhlt\ncli\nsti";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0x90, code[0]); // NOP
		Assert.Equal(0xf4, code[1]); // HLT
		Assert.Equal(0xfa, code[2]); // CLI
		Assert.Equal(0xfb, code[3]); // STI
	}

	[Fact]
	public void CodeGen_PushPopSequence_CorrectBytes() {
		var source = ".target wonderswan\npush ax\npush bx\npop bx\npop ax";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0x50, code[0]); // PUSH AX
		Assert.Equal(0x53, code[1]); // PUSH BX
		Assert.Equal(0x5b, code[2]); // POP BX
		Assert.Equal(0x58, code[3]); // POP AX
	}

	[Fact]
	public void CodeGen_IncDecSequence_CorrectBytes() {
		var source = ".target wonderswan\ninc cx\ndec dx\ninc si\ndec di";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0x41, code[0]); // INC CX
		Assert.Equal(0x4a, code[1]); // DEC DX
		Assert.Equal(0x46, code[2]); // INC SI
		Assert.Equal(0x4f, code[3]); // DEC DI
	}

	[Fact]
	public void CodeGen_MixedInstructions_CorrectOutput() {
		var source = ".target wonderswan\npush bp\nint #$10\nnop\npop bp\nret";
		var (code, gen) = GenerateV30MZCode(source);

		Assert.False(gen.HasErrors, $"Errors: {string.Join(", ", gen.Errors)}");
		Assert.Equal(0x55, code[0]); // PUSH BP
		Assert.Equal(0xcd, code[1]); // INT
		Assert.Equal(0x10, code[2]); // $10
		Assert.Equal(0x90, code[3]); // NOP
		Assert.Equal(0x5d, code[4]); // POP BP
		Assert.Equal(0xc3, code[5]); // RET
	}

	#endregion
}
