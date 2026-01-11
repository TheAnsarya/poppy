// ============================================================================
// CodeGeneratorTests.cs - Code Generator Unit Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

using PoppyLexer = Poppy.Core.Lexer.Lexer;
using PoppyParser = Poppy.Core.Parser.Parser;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Unit tests for the CodeGenerator class.
/// </summary>
public class CodeGeneratorTests {
	/// <summary>
	/// Helper to generate code from source.
	/// </summary>
	private static (byte[] Code, CodeGenerator Generator) GenerateCode(string source) {
		var lexer = new PoppyLexer(source);
		var tokens = lexer.Tokenize();
		var parser = new PoppyParser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer();
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer);
		var code = generator.Generate(program);

		return (code, generator);
	}

	// ========================================================================
	// Basic Instruction Encoding Tests
	// ========================================================================

	[Fact]
	public void Generate_LdaImmediate() {
		var source = "lda #$42";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xa9, 0x42], code);
	}

	[Fact]
	public void Generate_LdaZeroPage() {
		var source = "lda $42";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xa5, 0x42], code);
	}

	[Fact]
	public void Generate_LdaAbsolute() {
		var source = "lda $1234";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xad, 0x34, 0x12], code);
	}

	[Fact]
	public void Generate_LdaAbsoluteX() {
		var source = "lda $1234,x";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xbd, 0x34, 0x12], code);
	}

	[Fact]
	public void Generate_LdaAbsoluteY() {
		var source = "lda $1234,y";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xb9, 0x34, 0x12], code);
	}

	[Fact]
	public void Generate_LdaIndirectX() {
		var source = "lda ($42,x)";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xa1, 0x42], code);
	}

	[Fact]
	public void Generate_LdaIndirectY() {
		var source = "lda ($42),y";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xb1, 0x42], code);
	}

	// ========================================================================
	// Implied/Accumulator Mode Tests
	// ========================================================================

	[Fact]
	public void Generate_Nop() {
		var source = "nop";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xea], code);
	}

	[Fact]
	public void Generate_Inx() {
		var source = "inx";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xe8], code);
	}

	[Fact]
	public void Generate_Rts() {
		var source = "rts";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0x60], code);
	}

	[Fact]
	public void Generate_AslAccumulator() {
		var source = "asl a";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0x0a], code);
	}

	// ========================================================================
	// Multiple Instructions
	// ========================================================================

	[Fact]
	public void Generate_MultipleInstructions() {
		var source = """
			lda #$00
			sta $2000
			rts
			""";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xa9, 0x00, 0x8d, 0x00, 0x20, 0x60], code);
	}

	// ========================================================================
	// Label Resolution Tests
	// ========================================================================

	[Fact]
	public void Generate_JmpToLabel() {
		var source = """
			.org $8000
			start:
			jmp start
			""";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0x4c, 0x00, 0x80], code);
	}

	[Fact]
	public void Generate_JsrToLabel() {
		var source = """
			.org $8000
			jsr subroutine
			rts
			subroutine:
			lda #$42
			rts
			""";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		// jsr $8004 (3 bytes), rts (1 byte), lda #$42 (2 bytes), rts (1 byte)
		Assert.Equal([0x20, 0x04, 0x80, 0x60, 0xa9, 0x42, 0x60], code);
	}

	// ========================================================================
	// Branch Instruction Tests
	// ========================================================================

	[Fact]
	public void Generate_BranchForward() {
		var source = """
			.org $8000
			beq skip
			nop
			skip:
			rts
			""";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		// beq +1 (skip nop), nop, rts
		Assert.Equal([0xf0, 0x01, 0xea, 0x60], code);
	}

	[Fact]
	public void Generate_BranchBackward() {
		var source = """
			.org $8000
			loop:
			inx
			bne loop
			""";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		// inx, bne -3 (back to loop)
		// Offset is from PC after branch operand: $8003 - 3 = $8000
		Assert.Equal([0xe8, 0xd0, 0xfd], code);
	}

	// ========================================================================
	// Data Directive Tests
	// ========================================================================

	[Fact]
	public void Generate_ByteDirective() {
		var source = ".byte $01, $02, $03";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0x01, 0x02, 0x03], code);
	}

	[Fact]
	public void Generate_DbDirective() {
		var source = ".db $ff, $00, $aa";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xff, 0x00, 0xaa], code);
	}

	[Fact]
	public void Generate_WordDirective() {
		var source = ".word $1234, $5678";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0x34, 0x12, 0x78, 0x56], code);
	}

	[Fact]
	public void Generate_DwDirective() {
		var source = ".dw $abcd";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xcd, 0xab], code);
	}

	[Fact]
	public void Generate_StringData() {
		var source = ".byte \"ABC\"";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0x41, 0x42, 0x43], code);
	}

	// ========================================================================
	// Fill/Space Directive Tests
	// ========================================================================

	[Fact]
	public void Generate_FillDirective() {
		var source = ".fill 5, $ff";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xff, 0xff, 0xff, 0xff, 0xff], code);
	}

	[Fact]
	public void Generate_DsDirective() {
		var source = ".ds 3";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0x00, 0x00, 0x00], code);
	}

	[Fact]
	public void Generate_ResDirective() {
		var source = ".res 4, $ea";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xea, 0xea, 0xea, 0xea], code);
	}

	// ========================================================================
	// Constant/Expression Tests
	// ========================================================================

	[Fact]
	public void Generate_ConstantInOperand() {
		var source = """
			ADDR = $2000
			lda ADDR
			sta ADDR
			""";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xad, 0x00, 0x20, 0x8d, 0x00, 0x20], code);
	}

	[Fact]
	public void Generate_ExpressionInOperand() {
		var source = """
			BASE = $2000
			lda BASE+$10
			""";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xad, 0x10, 0x20], code);
	}

	// ========================================================================
	// Complete Program Tests
	// ========================================================================

	[Fact]
	public void Generate_SimpleProgram() {
		var source = """
			.org $8000

			; Reset vector handler
			reset:
				ldx #$ff
				txs          ; Initialize stack
				lda #$00
				sta $2000    ; Clear PPU control

			forever:
				jmp forever
			""";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		// ldx #$ff (2), txs (1), lda #$00 (2), sta $2000 (3), jmp $8008 (3) = 11 bytes
		Assert.Equal(11, code.Length);
		Assert.Equal(0xa2, code[0]); // ldx
		Assert.Equal(0xff, code[1]); // #$ff
		Assert.Equal(0x9a, code[2]); // txs
		Assert.Equal(0xa9, code[3]); // lda
		Assert.Equal(0x00, code[4]); // #$00
		Assert.Equal(0x8d, code[5]); // sta
		Assert.Equal(0x00, code[6]); // $00
		Assert.Equal(0x20, code[7]); // $20
		Assert.Equal(0x4c, code[8]); // jmp
		Assert.Equal(0x08, code[9]); // $08
		Assert.Equal(0x80, code[10]); // $80
	}

	// ========================================================================
	// All Instruction Mnemonic Tests
	// ========================================================================

	[Theory]
	[InlineData("adc #$42", new byte[] { 0x69, 0x42 })]
	[InlineData("and #$42", new byte[] { 0x29, 0x42 })]
	[InlineData("cmp #$42", new byte[] { 0xc9, 0x42 })]
	[InlineData("cpx #$42", new byte[] { 0xe0, 0x42 })]
	[InlineData("cpy #$42", new byte[] { 0xc0, 0x42 })]
	[InlineData("eor #$42", new byte[] { 0x49, 0x42 })]
	[InlineData("lda #$42", new byte[] { 0xa9, 0x42 })]
	[InlineData("ldx #$42", new byte[] { 0xa2, 0x42 })]
	[InlineData("ldy #$42", new byte[] { 0xa0, 0x42 })]
	[InlineData("ora #$42", new byte[] { 0x09, 0x42 })]
	[InlineData("sbc #$42", new byte[] { 0xe9, 0x42 })]
	public void Generate_ImmediateModeInstructions(string source, byte[] expected) {
		var (code, gen) = GenerateCode(source);
		Assert.False(gen.HasErrors);
		Assert.Equal(expected, code);
	}

	[Theory]
	[InlineData("clc", new byte[] { 0x18 })]
	[InlineData("cld", new byte[] { 0xd8 })]
	[InlineData("cli", new byte[] { 0x58 })]
	[InlineData("clv", new byte[] { 0xb8 })]
	[InlineData("dex", new byte[] { 0xca })]
	[InlineData("dey", new byte[] { 0x88 })]
	[InlineData("inx", new byte[] { 0xe8 })]
	[InlineData("iny", new byte[] { 0xc8 })]
	[InlineData("nop", new byte[] { 0xea })]
	[InlineData("pha", new byte[] { 0x48 })]
	[InlineData("php", new byte[] { 0x08 })]
	[InlineData("pla", new byte[] { 0x68 })]
	[InlineData("plp", new byte[] { 0x28 })]
	[InlineData("rti", new byte[] { 0x40 })]
	[InlineData("rts", new byte[] { 0x60 })]
	[InlineData("sec", new byte[] { 0x38 })]
	[InlineData("sed", new byte[] { 0xf8 })]
	[InlineData("sei", new byte[] { 0x78 })]
	[InlineData("tax", new byte[] { 0xaa })]
	[InlineData("tay", new byte[] { 0xa8 })]
	[InlineData("tsx", new byte[] { 0xba })]
	[InlineData("txa", new byte[] { 0x8a })]
	[InlineData("txs", new byte[] { 0x9a })]
	[InlineData("tya", new byte[] { 0x98 })]
	public void Generate_ImpliedModeInstructions(string source, byte[] expected) {
		var (code, gen) = GenerateCode(source);
		Assert.False(gen.HasErrors);
		Assert.Equal(expected, code);
	}

	[Theory]
	[InlineData("asl a", new byte[] { 0x0a })]
	[InlineData("lsr a", new byte[] { 0x4a })]
	[InlineData("rol a", new byte[] { 0x2a })]
	[InlineData("ror a", new byte[] { 0x6a })]
	public void Generate_AccumulatorModeInstructions(string source, byte[] expected) {
		var (code, gen) = GenerateCode(source);
		Assert.False(gen.HasErrors);
		Assert.Equal(expected, code);
	}

	[Theory]
	[InlineData("bit $42", new byte[] { 0x24, 0x42 })]
	[InlineData("bit $1234", new byte[] { 0x2c, 0x34, 0x12 })]
	public void Generate_BitInstruction(string source, byte[] expected) {
		var (code, gen) = GenerateCode(source);
		Assert.False(gen.HasErrors);
		Assert.Equal(expected, code);
	}

	[Theory]
	[InlineData("jmp $1234", new byte[] { 0x4c, 0x34, 0x12 })]
	[InlineData("jmp ($1234)", new byte[] { 0x6c, 0x34, 0x12 })]
	public void Generate_JmpInstruction(string source, byte[] expected) {
		var (code, gen) = GenerateCode(source);
		Assert.False(gen.HasErrors);
		Assert.Equal(expected, code);
	}

	[Fact]
	public void Generate_JsrInstruction() {
		var source = "jsr $1234";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0x20, 0x34, 0x12], code);
	}

	[Fact]
	public void Generate_BrkInstruction() {
		var source = "brk";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0x00], code);
	}

	// ========================================================================
	// Segment Tests
	// ========================================================================

	[Fact]
	public void Generate_MultipleOrgSegments() {
		var source = """
			.org $8000
			.byte $11, $22, $33
			.org $8010
			.byte $44, $55, $66
			""";
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal(2, gen.Segments.Count);
		Assert.Equal(0x8000, gen.Segments[0].StartAddress);
		Assert.Equal(0x8010, gen.Segments[1].StartAddress);
	}

	// ========================================================================
	// Error Handling Tests
	// ========================================================================

	[Fact]
	public void Generate_InvalidAddressingMode_ReportsError() {
		// STA doesn't support immediate mode
		var source = "sta #$42";
		var (code, gen) = GenerateCode(source);

		Assert.True(gen.HasErrors);
		Assert.Contains(gen.Errors, e => e.Message.Contains("Invalid addressing mode"));
	}

	// ========================================================================
	// Case Insensitivity Tests
	// ========================================================================

	[Theory]
	[InlineData("LDA #$42")]
	[InlineData("lda #$42")]
	[InlineData("Lda #$42")]
	public void Generate_CaseInsensitiveMnemonics(string source) {
		var (code, gen) = GenerateCode(source);

		Assert.False(gen.HasErrors);
		Assert.Equal([0xa9, 0x42], code);
	}
}

