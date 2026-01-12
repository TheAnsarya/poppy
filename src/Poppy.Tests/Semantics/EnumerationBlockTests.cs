using Poppy.Core.Semantics;
using Poppy.Core.Parser;

namespace Poppy.Tests.Semantics;

/// <summary>
/// Tests for enumeration blocks (.enum/.ende).
/// </summary>
public class EnumerationBlockTests {
	[Fact]
	public void EnumerationBlock_SimpleEnum_ParsesCorrectly() {
		// arrange
		var source = @"
.enum $00
	VAR1
	VAR2
	VAR3
.ende
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		var enumBlock = Assert.IsType<EnumerationBlockNode>(program.Statements[0]);
		Assert.Equal(3, enumBlock.Members.Count);
	}

	[Fact]
	public void EnumerationBlock_DefinesSymbols() {
		// arrange
		var source = @"
.enum $2000
	SPRITE_X
	SPRITE_Y
	SPRITE_TILE
.ende

	lda SPRITE_X
	sta $00
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - should define symbols correctly
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void EnumerationBlock_WithExplicitValues_ParsesCorrectly() {
		// arrange
		var source = @"
.enum $00
	VAR1
	VAR2 = $10
	VAR3
.ende
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		var enumBlock = Assert.IsType<EnumerationBlockNode>(program.Statements[0]);
		Assert.Equal(3, enumBlock.Members.Count);
	}

	[Fact]
	public void EnumerationBlock_WithSizeModifier_ParsesCorrectly() {
		// arrange
		var source = @"
.enum $00
	BYTE_VAR
	WORD_VAR .dw
	ANOTHER_BYTE
.ende
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.False(parser.HasErrors);
		Assert.Single(program.Statements);
		var enumBlock = Assert.IsType<EnumerationBlockNode>(program.Statements[0]);
		Assert.Equal(3, enumBlock.Members.Count);
	}

	[Fact]
	public void EnumerationBlock_MissingEnde_ReportsError() {
		// arrange
		var source = @"
.enum $00
	VAR1
	VAR2
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// assert
		Assert.True(parser.HasErrors);
		Assert.Contains(parser.Errors, e => e.Message.Contains(".ende"));
	}
}
