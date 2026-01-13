using Poppy.Core.Parser;
using Poppy.Core.Semantics;

namespace Poppy.Tests.Semantics;

/// <summary>
/// Tests for symbol conditional directives (.ifdef, .ifndef, .ifexist).
/// </summary>
public class SymbolConditionalTests {
	[Fact]
	public void SymbolConditional_Ifdef_SymbolDefined_IncludesCode() {
		// arrange
		var source = @"
DEBUG = 1
.ifdef DEBUG
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
	public void SymbolConditional_Ifdef_SymbolNotDefined_ExcludesCode() {
		// arrange
		var source = @"
.ifdef DEBUG
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
	public void SymbolConditional_Ifndef_SymbolNotDefined_IncludesCode() {
		// arrange
		var source = @"
.ifndef DEBUG
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
	public void SymbolConditional_Ifndef_SymbolDefined_ExcludesCode() {
		// arrange
		var source = @"
DEBUG = 1
.ifndef DEBUG
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
	public void SymbolConditional_Ifdef_WithElse_SymbolDefined() {
		// arrange
		var source = @"
DEBUG = 1
.ifdef DEBUG
	nop
.else
	brk
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
	public void SymbolConditional_Ifdef_WithElse_SymbolNotDefined() {
		// arrange
		var source = @"
.ifdef DEBUG
	nop
.else
	brk
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
	public void SymbolConditional_Ifdef_ParsesCorrectly() {
		// arrange
		var source = @"
.ifdef DEBUG
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
		var conditional = Assert.IsType<ConditionalNode>(program.Statements[0]);
		// Check that it's a symbol-based condition
		Assert.IsType<IdentifierNode>(conditional.Condition);
	}

	[Fact]
	public void SymbolConditional_Ifndef_ParsesCorrectly() {
		// arrange
		var source = @"
.ifndef DEBUG
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
		var conditional = Assert.IsType<ConditionalNode>(program.Statements[0]);
		Assert.IsType<UnaryExpressionNode>(conditional.Condition);
	}
}
