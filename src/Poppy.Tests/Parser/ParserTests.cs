// ============================================================================
// ParserTests.cs - Parser Unit Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Xunit;

namespace Poppy.Tests.Parser;

/// <summary>
/// Unit tests for the Parser class.
/// </summary>
public class ParserTests {
	// ========================================================================
	// Helper Methods
	// ========================================================================

	/// <summary>
	/// Helper to parse source code and return the program node.
	/// </summary>
	private static ProgramNode Parse(string source) {
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		return parser.Parse();
	}

	/// <summary>
	/// Helper to parse and get errors.
	/// </summary>
	private static (ProgramNode Program, IReadOnlyList<ParseError> Errors) ParseWithErrors(string source) {
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();
		return (program, parser.Errors);
	}

	// ========================================================================
	// Basic Parsing Tests
	// ========================================================================

	[Fact]
	public void Parse_EmptySource_ReturnsEmptyProgram() {
		var program = Parse("");
		Assert.Empty(program.Statements);
	}

	[Fact]
	public void Parse_OnlyComments_ReturnsEmptyProgram() {
		var program = Parse("; This is a comment\n// Another comment");
		Assert.Empty(program.Statements);
	}

	[Fact]
	public void Parse_OnlyNewlines_ReturnsEmptyProgram() {
		var program = Parse("\n\n\n");
		Assert.Empty(program.Statements);
	}

	// ========================================================================
	// Label Tests
	// ========================================================================

	[Fact]
	public void Parse_SimpleLabel_ReturnsLabelNode() {
		var program = Parse("main:");
		Assert.Single(program.Statements);

		var label = Assert.IsType<LabelNode>(program.Statements[0]);
		Assert.Equal("main", label.Name);
		Assert.False(label.IsLocal);
	}

	[Fact]
	public void Parse_LabelWithUnderscore_ReturnsLabelNode() {
		var program = Parse("main_loop:");
		Assert.Single(program.Statements);

		var label = Assert.IsType<LabelNode>(program.Statements[0]);
		Assert.Equal("main_loop", label.Name);
	}

	[Fact]
	public void Parse_MultipleLabels_ReturnsMultipleLabelNodes() {
		var program = Parse("start:\nloop:\nend:");
		Assert.Equal(3, program.Statements.Count);

		Assert.Equal("start", Assert.IsType<LabelNode>(program.Statements[0]).Name);
		Assert.Equal("loop", Assert.IsType<LabelNode>(program.Statements[1]).Name);
		Assert.Equal("end", Assert.IsType<LabelNode>(program.Statements[2]).Name);
	}

	// ========================================================================
	// Instruction Tests - Implied Addressing
	// ========================================================================

	[Fact]
	public void Parse_ImpliedInstruction_NOP() {
		var program = Parse("nop");
		Assert.Single(program.Statements);

		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("nop", instr.Mnemonic);
		Assert.Equal(AddressingMode.Implied, instr.AddressingMode);
		Assert.Null(instr.Operand);
		Assert.Null(instr.SizeSuffix);
	}

	[Fact]
	public void Parse_ImpliedInstruction_RTS() {
		var program = Parse("rts");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("rts", instr.Mnemonic);
		Assert.Equal(AddressingMode.Implied, instr.AddressingMode);
	}

	[Fact]
	public void Parse_ImpliedInstruction_CLC() {
		var program = Parse("clc");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("clc", instr.Mnemonic);
		Assert.Equal(AddressingMode.Implied, instr.AddressingMode);
	}

	// ========================================================================
	// Instruction Tests - Immediate Addressing
	// ========================================================================

	[Fact]
	public void Parse_ImmediateHex_LDA() {
		var program = Parse("lda #$ff");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal(AddressingMode.Immediate, instr.AddressingMode);

		var operand = Assert.IsType<NumberLiteralNode>(instr.Operand);
		Assert.Equal(255, operand.Value);
	}

	[Fact]
	public void Parse_ImmediateDecimal_LDX() {
		var program = Parse("ldx #10");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("ldx", instr.Mnemonic);
		Assert.Equal(AddressingMode.Immediate, instr.AddressingMode);

		var operand = Assert.IsType<NumberLiteralNode>(instr.Operand);
		Assert.Equal(10, operand.Value);
	}

	[Fact]
	public void Parse_ImmediateBinary_LDY() {
		var program = Parse("ldy #%10101010");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("ldy", instr.Mnemonic);
		Assert.Equal(AddressingMode.Immediate, instr.AddressingMode);

		var operand = Assert.IsType<NumberLiteralNode>(instr.Operand);
		Assert.Equal(0b10101010, operand.Value);
	}

	[Fact]
	public void Parse_ImmediateExpression_LDA() {
		var program = Parse("lda #label");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal(AddressingMode.Immediate, instr.AddressingMode);

		var operand = Assert.IsType<IdentifierNode>(instr.Operand);
		Assert.Equal("label", operand.Name);
	}

	// ========================================================================
	// Instruction Tests - Absolute Addressing
	// ========================================================================

	[Fact]
	public void Parse_AbsoluteHex_LDA() {
		var program = Parse("lda $2000");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal(AddressingMode.Absolute, instr.AddressingMode);

		var operand = Assert.IsType<NumberLiteralNode>(instr.Operand);
		Assert.Equal(0x2000, operand.Value);
	}

	[Fact]
	public void Parse_AbsoluteLabel_JMP() {
		var program = Parse("jmp main_loop");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("jmp", instr.Mnemonic);
		Assert.Equal(AddressingMode.Absolute, instr.AddressingMode);

		var operand = Assert.IsType<IdentifierNode>(instr.Operand);
		Assert.Equal("main_loop", operand.Name);
	}

	// ========================================================================
	// Instruction Tests - Indexed Addressing
	// ========================================================================

	[Fact]
	public void Parse_AbsoluteX_LDA() {
		var program = Parse("lda $2000,x");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal(AddressingMode.AbsoluteX, instr.AddressingMode);

		var operand = Assert.IsType<NumberLiteralNode>(instr.Operand);
		Assert.Equal(0x2000, operand.Value);
	}

	[Fact]
	public void Parse_AbsoluteY_LDA() {
		var program = Parse("lda $2000,y");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal(AddressingMode.AbsoluteY, instr.AddressingMode);
	}

	[Fact]
	public void Parse_StackRelative_LDA() {
		var program = Parse("lda $01,s");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal(AddressingMode.StackRelative, instr.AddressingMode);

		var operand = Assert.IsType<NumberLiteralNode>(instr.Operand);
		Assert.Equal(1, operand.Value);
	}

	// ========================================================================
	// Instruction Tests - Indirect Addressing
	// ========================================================================

	[Fact]
	public void Parse_Indirect_JMP() {
		var program = Parse("jmp ($fffc)");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("jmp", instr.Mnemonic);
		Assert.Equal(AddressingMode.Indirect, instr.AddressingMode);

		var operand = Assert.IsType<NumberLiteralNode>(instr.Operand);
		Assert.Equal(0xfffc, operand.Value);
	}

	[Fact]
	public void Parse_IndexedIndirect_LDA() {
		var program = Parse("lda ($00,x)");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal(AddressingMode.IndexedIndirect, instr.AddressingMode);

		var operand = Assert.IsType<NumberLiteralNode>(instr.Operand);
		Assert.Equal(0, operand.Value);
	}

	[Fact]
	public void Parse_IndirectIndexed_LDA() {
		var program = Parse("lda ($00),y");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal(AddressingMode.IndirectIndexed, instr.AddressingMode);

		var operand = Assert.IsType<NumberLiteralNode>(instr.Operand);
		Assert.Equal(0, operand.Value);
	}

	// ========================================================================
	// Instruction Tests - 65816 Long Indirect
	// ========================================================================

	[Fact]
	public void Parse_DirectPageIndirectLong_LDA() {
		var program = Parse("lda [$00]");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal(AddressingMode.DirectPageIndirectLong, instr.AddressingMode);
	}

	[Fact]
	public void Parse_DirectPageIndirectLongY_LDA() {
		var program = Parse("lda [$00],y");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal(AddressingMode.DirectPageIndirectLongY, instr.AddressingMode);
	}

	// ========================================================================
	// Instruction Tests - Accumulator Addressing
	// ========================================================================

	[Fact]
	public void Parse_Accumulator_ASL() {
		var program = Parse("asl a");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("asl", instr.Mnemonic);
		Assert.Equal(AddressingMode.Accumulator, instr.AddressingMode);
		Assert.Null(instr.Operand);
	}

	[Fact]
	public void Parse_Accumulator_ROR() {
		var program = Parse("ror a");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("ror", instr.Mnemonic);
		Assert.Equal(AddressingMode.Accumulator, instr.AddressingMode);
	}

	// ========================================================================
	// Instruction Tests - Size Suffixes (65816)
	// ========================================================================

	[Fact]
	public void Parse_SizeSuffix_Byte() {
		var program = Parse("lda.b #$00");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal('b', instr.SizeSuffix);
	}

	[Fact]
	public void Parse_SizeSuffix_Word() {
		var program = Parse("lda.w #$0000");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal('w', instr.SizeSuffix);
	}

	[Fact]
	public void Parse_SizeSuffix_Long() {
		var program = Parse("lda.l $7e0000");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal("lda", instr.Mnemonic);
		Assert.Equal('l', instr.SizeSuffix);
	}

	// ========================================================================
	// Directive Tests
	// ========================================================================

	[Fact]
	public void Parse_OrgDirective() {
		var program = Parse(".org $8000");
		var directive = Assert.IsType<DirectiveNode>(program.Statements[0]);
		Assert.Equal("org", directive.Name);
		Assert.Single(directive.Arguments);

		var arg = Assert.IsType<NumberLiteralNode>(directive.Arguments[0]);
		Assert.Equal(0x8000, arg.Value);
	}

	[Fact]
	public void Parse_ByteDirective_SingleValue() {
		var program = Parse(".byte $ff");
		var directive = Assert.IsType<DirectiveNode>(program.Statements[0]);
		Assert.Equal("byte", directive.Name);
		Assert.Single(directive.Arguments);
	}

	[Fact]
	public void Parse_ByteDirective_MultipleValues() {
		var program = Parse(".byte $01, $02, $03, $04");
		var directive = Assert.IsType<DirectiveNode>(program.Statements[0]);
		Assert.Equal("byte", directive.Name);
		Assert.Equal(4, directive.Arguments.Count);
	}

	[Fact]
	public void Parse_WordDirective() {
		var program = Parse(".word $1234, $5678");
		var directive = Assert.IsType<DirectiveNode>(program.Statements[0]);
		Assert.Equal("word", directive.Name);
		Assert.Equal(2, directive.Arguments.Count);
	}

	[Fact]
	public void Parse_DbDirective_String() {
		var program = Parse(".db \"Hello\"");
		var directive = Assert.IsType<DirectiveNode>(program.Statements[0]);
		Assert.Equal("db", directive.Name);
		Assert.Single(directive.Arguments);

		var arg = Assert.IsType<StringLiteralNode>(directive.Arguments[0]);
		Assert.Equal("Hello", arg.Value);
	}

	[Fact]
	public void Parse_IncludeDirective() {
		var program = Parse(".include \"other.asm\"");
		var directive = Assert.IsType<DirectiveNode>(program.Statements[0]);
		Assert.Equal("include", directive.Name);
		Assert.Single(directive.Arguments);

		var arg = Assert.IsType<StringLiteralNode>(directive.Arguments[0]);
		Assert.Equal("other.asm", arg.Value);
	}

	[Fact]
	public void Parse_DefineDirective() {
		// .define CONSTANT, $ff  - comma-separated arguments
		var program = Parse(".define CONSTANT, $ff");
		var directive = Assert.IsType<DirectiveNode>(program.Statements[0]);
		Assert.Equal("define", directive.Name);
		Assert.Equal(2, directive.Arguments.Count);
	}

	// ========================================================================
	// Expression Tests - Literals
	// ========================================================================

	[Fact]
	public void Parse_Expression_HexNumber() {
		var program = Parse("lda #$abcd");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		var operand = Assert.IsType<NumberLiteralNode>(instr.Operand);
		Assert.Equal(0xabcd, operand.Value);
	}

	[Fact]
	public void Parse_Expression_DecimalNumber() {
		var program = Parse("lda #1234");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		var operand = Assert.IsType<NumberLiteralNode>(instr.Operand);
		Assert.Equal(1234, operand.Value);
	}

	[Fact]
	public void Parse_Expression_BinaryNumber() {
		var program = Parse("lda #%11110000");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		var operand = Assert.IsType<NumberLiteralNode>(instr.Operand);
		Assert.Equal(0xf0, operand.Value);
	}

	[Fact]
	public void Parse_Expression_Identifier() {
		var program = Parse("lda #my_constant");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		var operand = Assert.IsType<IdentifierNode>(instr.Operand);
		Assert.Equal("my_constant", operand.Name);
	}

	// ========================================================================
	// Expression Tests - Binary Operators
	// ========================================================================

	[Fact]
	public void Parse_Expression_Addition() {
		var program = Parse("lda #base + 5");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<BinaryExpressionNode>(instr.Operand);
		Assert.Equal(BinaryOperator.Add, expr.Operator);

		var left = Assert.IsType<IdentifierNode>(expr.Left);
		Assert.Equal("base", left.Name);

		var right = Assert.IsType<NumberLiteralNode>(expr.Right);
		Assert.Equal(5, right.Value);
	}

	[Fact]
	public void Parse_Expression_Subtraction() {
		var program = Parse("lda #end - start");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<BinaryExpressionNode>(instr.Operand);
		Assert.Equal(BinaryOperator.Subtract, expr.Operator);
	}

	[Fact]
	public void Parse_Expression_Multiplication() {
		var program = Parse("lda #count * 2");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<BinaryExpressionNode>(instr.Operand);
		Assert.Equal(BinaryOperator.Multiply, expr.Operator);
	}

	[Fact]
	public void Parse_Expression_Division() {
		var program = Parse("lda #total / 4");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<BinaryExpressionNode>(instr.Operand);
		Assert.Equal(BinaryOperator.Divide, expr.Operator);
	}

	[Fact]
	public void Parse_Expression_BitwiseAnd() {
		var program = Parse("lda #value & $0f");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<BinaryExpressionNode>(instr.Operand);
		Assert.Equal(BinaryOperator.BitwiseAnd, expr.Operator);
	}

	[Fact]
	public void Parse_Expression_BitwiseOr() {
		var program = Parse("lda #flags | $80");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<BinaryExpressionNode>(instr.Operand);
		Assert.Equal(BinaryOperator.BitwiseOr, expr.Operator);
	}

	[Fact]
	public void Parse_Expression_BitwiseXor() {
		var program = Parse("lda #mask ^ $ff");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<BinaryExpressionNode>(instr.Operand);
		Assert.Equal(BinaryOperator.BitwiseXor, expr.Operator);
	}

	[Fact]
	public void Parse_Expression_LeftShift() {
		var program = Parse("lda #value << 4");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<BinaryExpressionNode>(instr.Operand);
		Assert.Equal(BinaryOperator.LeftShift, expr.Operator);
	}

	[Fact]
	public void Parse_Expression_RightShift() {
		var program = Parse("lda #value >> 4");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<BinaryExpressionNode>(instr.Operand);
		Assert.Equal(BinaryOperator.RightShift, expr.Operator);
	}

	// ========================================================================
	// Expression Tests - Unary Operators
	// ========================================================================

	[Fact]
	public void Parse_Expression_Negation() {
		var program = Parse("lda #-5");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<UnaryExpressionNode>(instr.Operand);
		Assert.Equal(UnaryOperator.Negate, expr.Operator);

		var operand = Assert.IsType<NumberLiteralNode>(expr.Operand);
		Assert.Equal(5, operand.Value);
	}

	[Fact]
	public void Parse_Expression_BitwiseNot() {
		var program = Parse("lda #~mask");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<UnaryExpressionNode>(instr.Operand);
		Assert.Equal(UnaryOperator.BitwiseNot, expr.Operator);
	}

	[Fact]
	public void Parse_Expression_LowByte() {
		var program = Parse("lda #<address");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<UnaryExpressionNode>(instr.Operand);
		Assert.Equal(UnaryOperator.LowByte, expr.Operator);
	}

	[Fact]
	public void Parse_Expression_HighByte() {
		var program = Parse("lda #>address");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<UnaryExpressionNode>(instr.Operand);
		Assert.Equal(UnaryOperator.HighByte, expr.Operator);
	}

	[Fact]
	public void Parse_Expression_BankByte() {
		var program = Parse("lda #^address");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var expr = Assert.IsType<UnaryExpressionNode>(instr.Operand);
		Assert.Equal(UnaryOperator.BankByte, expr.Operator);
	}

	// ========================================================================
	// Expression Tests - Precedence
	// ========================================================================

	[Fact]
	public void Parse_Expression_MultiplicationBeforeAddition() {
		// a + b * c should parse as a + (b * c)
		var program = Parse("lda #a + b * c");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var add = Assert.IsType<BinaryExpressionNode>(instr.Operand);
		Assert.Equal(BinaryOperator.Add, add.Operator);

		Assert.IsType<IdentifierNode>(add.Left);

		var mul = Assert.IsType<BinaryExpressionNode>(add.Right);
		Assert.Equal(BinaryOperator.Multiply, mul.Operator);
	}

	[Fact]
	public void Parse_Expression_ParenthesesOverride() {
		// (a + b) * c should parse as (a + b) * c
		var program = Parse("lda #(a + b) * c");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);

		var mul = Assert.IsType<BinaryExpressionNode>(instr.Operand);
		Assert.Equal(BinaryOperator.Multiply, mul.Operator);

		var add = Assert.IsType<BinaryExpressionNode>(mul.Left);
		Assert.Equal(BinaryOperator.Add, add.Operator);

		Assert.IsType<IdentifierNode>(mul.Right);
	}

	// ========================================================================
	// Macro Tests
	// ========================================================================

	[Fact]
	public void Parse_MacroDefinition_NoParameters() {
		var program = Parse(".macro save_regs\n  pha\n  phx\n  phy\n.endmacro");
		var macro = Assert.IsType<MacroDefinitionNode>(program.Statements[0]);

		Assert.Equal("save_regs", macro.Name);
		Assert.Empty(macro.Parameters);
		Assert.Equal(3, macro.Body.Count);
	}

	[Fact]
	public void Parse_MacroDefinition_WithParameters() {
		var program = Parse(".macro load_value value\n  lda #value\n.endmacro");
		var macro = Assert.IsType<MacroDefinitionNode>(program.Statements[0]);

		Assert.Equal("load_value", macro.Name);
		Assert.Single(macro.Parameters);
		Assert.Equal("value", macro.Parameters[0]);
	}

	[Fact]
	public void Parse_MacroDefinition_MultipleParameters() {
		var program = Parse(".macro copy src, dest\n  lda src\n  sta dest\n.endmacro");
		var macro = Assert.IsType<MacroDefinitionNode>(program.Statements[0]);

		Assert.Equal("copy", macro.Name);
		Assert.Equal(2, macro.Parameters.Count);
		Assert.Equal("src", macro.Parameters[0]);
		Assert.Equal("dest", macro.Parameters[1]);
	}

	// ========================================================================
	// Combined Statements Tests
	// ========================================================================

	[Fact]
	public void Parse_CompleteProgram() {
		var source = """
			; Simple NES program
			.org $8000

			reset:
				sei
				cld
				ldx #$ff
				txs
				lda #$00
				sta $2000
				jmp main

			main:
				nop
				jmp main
			""";

		var program = Parse(source);
		Assert.True(program.Statements.Count > 0);

		// First should be .org directive
		Assert.IsType<DirectiveNode>(program.Statements[0]);

		// Second should be reset: label
		Assert.IsType<LabelNode>(program.Statements[1]);
	}

	[Fact]
	public void Parse_LabelFollowedByInstruction() {
		var program = Parse("loop:\n  nop");
		Assert.Equal(2, program.Statements.Count);

		Assert.IsType<LabelNode>(program.Statements[0]);
		Assert.IsType<InstructionNode>(program.Statements[1]);
	}

	// ========================================================================
	// Error Handling Tests
	// ========================================================================

	[Fact]
	public void Parse_InvalidIndexRegister_ReportsError() {
		var (_, errors) = ParseWithErrors("lda $2000,z");
		Assert.NotEmpty(errors);
	}

	[Fact]
	public void Parse_MissingOperand_ReportsError() {
		var (_, errors) = ParseWithErrors("lda #");
		Assert.NotEmpty(errors);
	}

	[Fact]
	public void Parse_UnclosedParenthesis_ReportsError() {
		var (_, errors) = ParseWithErrors("jmp ($fffc");
		Assert.NotEmpty(errors);
	}

	[Fact]
	public void Parse_UnclosedBracket_ReportsError() {
		var (_, errors) = ParseWithErrors("lda [$00");
		Assert.NotEmpty(errors);
	}

	// ========================================================================
	// Assignment (EQU-style) Tests
	// ========================================================================

	[Fact]
	public void Parse_Assignment_Constant() {
		var program = Parse("PPUCTRL = $2000");
		var directive = Assert.IsType<DirectiveNode>(program.Statements[0]);

		Assert.Equal("equ", directive.Name);
		Assert.Equal(2, directive.Arguments.Count);

		var name = Assert.IsType<IdentifierNode>(directive.Arguments[0]);
		Assert.Equal("PPUCTRL", name.Name);

		var value = Assert.IsType<NumberLiteralNode>(directive.Arguments[1]);
		Assert.Equal(0x2000, value.Value);
	}

	// ========================================================================
	// Location Tracking Tests
	// ========================================================================

	[Fact]
	public void Parse_TracksLineNumbers() {
		var program = Parse("nop\n\nlda #$00");
		Assert.Equal(2, program.Statements.Count);

		Assert.Equal(1, program.Statements[0].Location.Line);
		Assert.Equal(3, program.Statements[1].Location.Line);
	}

	[Fact]
	public void Parse_TracksColumnNumbers() {
		var program = Parse("  nop");
		var instr = Assert.IsType<InstructionNode>(program.Statements[0]);
		Assert.Equal(3, instr.Location.Column);
	}
}

