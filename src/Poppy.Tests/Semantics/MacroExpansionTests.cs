// ============================================================================
// MacroExpansionTests.cs - Tests for Macro Expansion
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

namespace Poppy.Tests.Semantics;

public class MacroExpansionTests
{
	[Fact]
	public void SimpleMacroExpansion_NoParameters()
	{
		// arrange
		var source = @"
.macro nop3
	nop
	nop
	nop
.endmacro

reset:
	@nop3
	rts
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// act - expand the macro invocation
		var invocation = program.Statements
			.OfType<MacroInvocationNode>()
			.First();

		var expander = new MacroExpander(analyzer.MacroTable);
		var expanded = expander.Expand(invocation, []);

		// assert
		Assert.False(expander.HasErrors);
		Assert.Equal(3, expanded.Count);
		Assert.All(expanded, stmt => Assert.IsType<InstructionNode>(stmt));

		var instructions = expanded.Cast<InstructionNode>().ToList();
		Assert.All(instructions, instr => Assert.Equal("nop", instr.Mnemonic));
	}

	[Fact]
	public void MacroExpansion_WithParameters()
	{
		// arrange
		var source = @"
.macro load_value, addr
	lda addr
	sta $00
.endmacro

reset:
	@load_value #$42
	rts
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// act
		var invocation = program.Statements
			.OfType<MacroInvocationNode>()
			.First();

		var expander = new MacroExpander(analyzer.MacroTable);
		var expanded = expander.Expand(invocation, invocation.Arguments);

		// assert
		Assert.False(expander.HasErrors);
		Assert.Equal(2, expanded.Count);

		var lda = Assert.IsType<InstructionNode>(expanded[0]);
		Assert.Equal("lda", lda.Mnemonic);

		// The operand should be the substituted #$42
		Assert.NotNull(lda.Operand);
	}

	[Fact]
	public void MacroExpansion_LocalLabelRenaming()
	{
		// arrange
		var source = @"
.macro wait_loop
@loop:
	bit $2002
	bpl @loop
.endmacro

reset:
	@wait_loop
	@wait_loop
	rts
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// act - expand both invocations
		var invocations = program.Statements
			.OfType<MacroInvocationNode>()
			.ToList();

		var expander = new MacroExpander(analyzer.MacroTable);
		var expanded1 = expander.Expand(invocations[0], []);
		var expanded2 = expander.Expand(invocations[1], []);

		// assert
		Assert.False(expander.HasErrors);

		// First expansion should have unique labels
		var label1 = Assert.IsType<LabelNode>(expanded1[0]);
		Assert.Contains("wait_loop@loop_1", label1.Name);

		// Second expansion should have different unique labels
		var label2 = Assert.IsType<LabelNode>(expanded2[0]);
		Assert.Contains("wait_loop@loop_2", label2.Name);

		// Labels should be different
		Assert.NotEqual(label1.Name, label2.Name);
	}

	[Fact]
	public void MacroExpansion_UndefinedMacro_ReportsError()
	{
		// arrange
		var macroTable = new MacroTable();
		var expander = new MacroExpander(macroTable);

		var invocation = new MacroInvocationNode(
			new SourceLocation("test.pasm", 1, 1, 0),
			"undefined_macro",
			[]);

		// act
		var expanded = expander.Expand(invocation, []);

		// assert
		Assert.True(expander.HasErrors);
		Assert.Empty(expanded);
		Assert.Contains(expander.Errors, e => e.Message.Contains("Undefined macro"));
	}

	[Fact]
	public void MacroExpansion_WrongArgumentCount_ReportsError()
	{
		// arrange
		var source = @"
.macro two_params, a, b
	lda a
	sta b
.endmacro
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// act - call with wrong number of arguments
		var invocation = new MacroInvocationNode(
			new SourceLocation("test.pasm", 10, 1, 0),
			"two_params",
			[new NumberLiteralNode(new SourceLocation("test.pasm", 10, 12, 0), 42)]);  // Only 1 arg

		var expander = new MacroExpander(analyzer.MacroTable);
		var expanded = expander.Expand(invocation, invocation.Arguments);

		// assert
		Assert.True(expander.HasErrors);
		Assert.Empty(expanded);
		Assert.Contains(expander.Errors, e => e.Message.Contains("expects 2 argument(s), got 1"));
	}

	[Fact]
	public void MacroExpansion_ParameterInExpression()
	{
		// arrange
		var source = @"
.macro add_offset, base, offset
	lda base + offset
	sta $00
.endmacro
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// act
		var macro = analyzer.MacroTable.Get("add_offset");
		Assert.NotNull(macro);

		// Create test invocation
		var baseArg = new NumberLiteralNode(new SourceLocation("test.pasm", 10, 1, 0), 0x2000);
		var offsetArg = new NumberLiteralNode(new SourceLocation("test.pasm", 10, 8, 0), 5);
		var invocation = new MacroInvocationNode(
			new SourceLocation("test.pasm", 10, 1, 0),
			"add_offset",
			[baseArg, offsetArg]);

		var expander = new MacroExpander(analyzer.MacroTable);
		var expanded = expander.Expand(invocation, invocation.Arguments);

		// assert
		Assert.False(expander.HasErrors);
		Assert.Equal(2, expanded.Count);

		var lda = Assert.IsType<InstructionNode>(expanded[0]);
		Assert.Equal("lda", lda.Mnemonic);

		// Operand should be a binary expression with substituted parameters
		var binary = Assert.IsType<BinaryExpressionNode>(lda.Operand);
		Assert.Equal(BinaryOperator.Add, binary.Operator);
	}

	[Fact]
	public void MacroExpansion_MultipleParameters()
	{
		// arrange
		var source = @"
.macro sprite_dma, addr, count, page
	lda #>addr
	sta $2003
	lda #<addr
	sta $2004
	ldx count
	ldy page
.endmacro
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// act
		var args = new List<ExpressionNode>
		{
			new NumberLiteralNode(new SourceLocation("test.pasm", 10, 1, 0), 0x0200),
			new NumberLiteralNode(new SourceLocation("test.pasm", 10, 8, 0), 64),
			new NumberLiteralNode(new SourceLocation("test.pasm", 10, 12, 0), 2)
		};

		var invocation = new MacroInvocationNode(
			new SourceLocation("test.pasm", 10, 1, 0),
			"sprite_dma",
			args);

		var expander = new MacroExpander(analyzer.MacroTable);
		var expanded = expander.Expand(invocation, args);

		// assert
		Assert.False(expander.HasErrors);
		Assert.Equal(6, expanded.Count);
		Assert.All(expanded, stmt => Assert.IsType<InstructionNode>(stmt));
	}

	[Fact]
	public void MacroExpansion_NestedExpressions()
	{
		// arrange
		var source = @"
.macro complex, value
	lda #<(value * 2 + 1)
	ldx #>(value * 2 + 1)
.endmacro
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// act
		var arg = new NumberLiteralNode(new SourceLocation("test.pasm", 10, 1, 0), 100);
		var invocation = new MacroInvocationNode(
			new SourceLocation("test.pasm", 10, 1, 0),
			"complex",
			[arg]);

		var expander = new MacroExpander(analyzer.MacroTable);
		var expanded = expander.Expand(invocation, invocation.Arguments);

		// assert
		Assert.False(expander.HasErrors);
		Assert.Equal(2, expanded.Count);

		// Both instructions should have unary expressions (low/high byte)
		Assert.All(expanded.Cast<InstructionNode>(),
			instr => Assert.IsType<UnaryExpressionNode>(instr.Operand));
	}

	[Fact]
	public void MacroExpansion_DirectiveWithParameters()
	{
		// arrange
		var source = @"
.macro define_bytes, value
	.byte value, value + 1, value + 2
.endmacro
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// act
		var arg = new NumberLiteralNode(new SourceLocation("test.pasm", 10, 1, 0), 10);
		var invocation = new MacroInvocationNode(
			new SourceLocation("test.pasm", 10, 1, 0),
			"define_bytes",
			[arg]);

		var expander = new MacroExpander(analyzer.MacroTable);
		var expanded = expander.Expand(invocation, invocation.Arguments);

		// assert
		Assert.False(expander.HasErrors);
		Assert.Single(expanded);

		var directive = Assert.IsType<DirectiveNode>(expanded[0]);
		Assert.Equal("byte", directive.Name);
		Assert.Equal(3, directive.Arguments.Count);
	}

	[Fact]
	public void MacroExpansion_ClearResetsState()
	{
		// arrange
		var macroTable = new MacroTable();
		var expander = new MacroExpander(macroTable);

		// Create error
		var invocation = new MacroInvocationNode(
			new SourceLocation("test.pasm", 1, 1, 0),
			"undefined",
			[]);
		expander.Expand(invocation, []);

		Assert.True(expander.HasErrors);

		// act
		expander.Clear();

		// assert
		Assert.False(expander.HasErrors);
		Assert.Empty(expander.Errors);
	}
}
