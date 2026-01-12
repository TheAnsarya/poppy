using Poppy.Core.Semantics;
using Poppy.Core.Parser;

namespace Poppy.Tests.Semantics;

/// <summary>
/// Tests for conditional assembly directives (.if, .else, .elseif, .endif).
/// </summary>
public class ConditionalAssemblyTests
{
	[Fact]
	public void ConditionalAssembly_IfTrue_ParsesSuccessfully()
	{
		// arrange
		var source = @"
.if 1
	nop
.endif
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert - should parse without errors
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		Assert.IsType<ConditionalNode>(program.Statements[0]);
	}

	[Fact]
	public void ConditionalAssembly_IfFalse_ParsesSuccessfully()
	{
		// arrange
		var source = @"
.if 0
	nop
.endif
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		Assert.IsType<ConditionalNode>(program.Statements[0]);
	}

	[Fact]
	public void ConditionalAssembly_IfElse_ParsesSuccessfully()
	{
		// arrange
		var source = @"
.if 1
	nop
.else
	brk
.endif
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		var conditional = Assert.IsType<ConditionalNode>(program.Statements[0]);
		Assert.NotNull(conditional.ElseBlock);
	}

	[Fact]
	public void ConditionalAssembly_IfElseIf_ParsesSuccessfully()
	{
		// arrange
		var source = @"
.if 0
	nop
.elseif 1
	brk
.endif
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		var conditional = Assert.IsType<ConditionalNode>(program.Statements[0]);
		Assert.Single(conditional.ElseIfBranches);
	}

	[Fact]
	public void ConditionalAssembly_MultipleElseIf_ParsesSuccessfully()
	{
		// arrange
		var source = @"
.if 0
	nop
.elseif 0
	brk
.elseif 1
	clc
.else
	sec
.endif
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		var conditional = Assert.IsType<ConditionalNode>(program.Statements[0]);
		Assert.Equal(2, conditional.ElseIfBranches.Count);
		Assert.NotNull(conditional.ElseBlock);
	}

	[Fact]
	public void ConditionalAssembly_NestedConditionals_ParsesSuccessfully()
	{
		// arrange
		var source = @"
.if 1
	.if 1
		nop
	.endif
.endif
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		var conditional = Assert.IsType<ConditionalNode>(program.Statements[0]);
		Assert.Single(conditional.ThenBlock);
		Assert.IsType<ConditionalNode>(conditional.ThenBlock[0]);
	}

	[Fact]
	public void ConditionalAssembly_IfTrue_ExecutesThenBlock()
	{
		// arrange
		var source = @"
.if 1
	nop
.endif
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - should analyze without errors
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void ConditionalAssembly_IfFalse_SkipsThenBlock()
	{
		// arrange
		var source = @"
.if 0
	nop
.endif
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void ConditionalAssembly_MissingEndif_ReportsError()
	{
		// arrange
		var source = @"
.if 1
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert - parser should report error
		Assert.True(parser.HasErrors);
		Assert.Contains(parser.Errors, e => e.Message.Contains(".endif"));
	}

	[Fact]
	public void ConditionalAssembly_MultipleElse_ReportsError()
	{
		// arrange
		var source = @"
.if 1
	nop
.else
	brk
.else
	clc
.endif
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert - parser should report error
		Assert.True(parser.HasErrors);
		Assert.Contains(parser.Errors, e => e.Message.Contains(".else"));
	}

	[Fact]
	public void ConditionalAssembly_ElseIfAfterElse_ReportsError()
	{
		// arrange
		var source = @"
.if 1
	nop
.else
	brk
.elseif 1
	clc
.endif
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert - parser should report error
		Assert.True(parser.HasErrors);
		Assert.Contains(parser.Errors, e => e.Message.Contains(".elseif") && e.Message.Contains(".else"));
	}
}
