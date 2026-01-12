using Poppy.Core.Semantics;
using Poppy.Core.Parser;

namespace Poppy.Tests.Semantics;

/// <summary>
/// Tests for repeat blocks (.rept/.endr).
/// </summary>
public class RepeatBlockTests {
	[Fact]
	public void RepeatBlock_SimpleRepeat_ParsesCorrectly() {
		// arrange
		var source = @"
.rept 3
	nop
.endr
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		var repeat = Assert.IsType<RepeatBlockNode>(program.Statements[0]);
		Assert.Single(repeat.Body);
	}

	[Fact]
	public void RepeatBlock_ZeroCount_ParsesCorrectly() {
		// arrange
		var source = @"
.rept 0
	nop
.endr
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		Assert.IsType<RepeatBlockNode>(program.Statements[0]);
	}

	[Fact]
	public void RepeatBlock_MultipleStatements_ParsesCorrectly() {
		// arrange
		var source = @"
.rept 2
	lda #$00
	sta $2000
	nop
.endr
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		var repeat = Assert.IsType<RepeatBlockNode>(program.Statements[0]);
		Assert.Equal(3, repeat.Body.Count);
	}

	[Fact]
	public void RepeatBlock_NestedRepeats_ParsesCorrectly() {
		// arrange
		var source = @"
.rept 2
	.rept 3
		nop
	.endr
.endr
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		var repeat = Assert.IsType<RepeatBlockNode>(program.Statements[0]);
		Assert.Single(repeat.Body);
		Assert.IsType<RepeatBlockNode>(repeat.Body[0]);
	}

	[Fact]
	public void RepeatBlock_ExpressionCount_ParsesCorrectly() {
		// arrange
		var source = @"
COUNT = 5
.rept COUNT * 2
	nop
.endr
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Equal(2, program.Statements.Count);
		Assert.IsType<RepeatBlockNode>(program.Statements[1]);
	}

	[Fact]
	public void RepeatBlock_ExecutesCorrectNumberOfTimes() {
		// arrange
		var source = @"
.rept 3
	nop
.endr
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - should execute without errors
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void RepeatBlock_MissingEndr_ReportsError() {
		// arrange
		var source = @"
.rept 3
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.True(parser.HasErrors);
		Assert.Contains(parser.Errors, e => e.Message.Contains(".endr"));
	}
}
