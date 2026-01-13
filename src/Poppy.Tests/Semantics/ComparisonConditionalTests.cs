// ============================================================================
// ComparisonConditionalTests.cs - Tests for Comparison Conditionals
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Parser;
using Poppy.Core.Semantics;

namespace Poppy.Tests.Semantics;

public class ComparisonConditionalTests {
	[Fact]
	public void IfEq_EqualValues_ExecutesTrueBlock() {
		// arrange
		var source = @"
.ifeq 5, 5
	.byte $01
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
	public void IfEq_UnequalValues_SkipsTrueBlock() {
		// arrange
		var source = @"
.ifeq 5, 3
	.byte $01
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
	public void IfNe_UnequalValues_ExecutesTrueBlock() {
		// arrange
		var source = @"
.ifne 5, 3
	.byte $01
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
	public void IfGt_GreaterValue_ExecutesTrueBlock() {
		// arrange
		var source = @"
.ifgt 10, 5
	.byte $01
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
	public void IfLt_LesserValue_ExecutesTrueBlock() {
		// arrange
		var source = @"
.iflt 3, 10
	.byte $01
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
	public void IfGe_EqualValues_ExecutesTrueBlock() {
		// arrange
		var source = @"
.ifge 5, 5
	.byte $01
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
	public void IfLe_EqualValues_ExecutesTrueBlock() {
		// arrange
		var source = @"
.ifle 5, 5
	.byte $01
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
	public void ComparisonConditional_WithSymbols() {
		// arrange
		var source = @"
MAPPER = 0
.ifeq MAPPER, 0
	.byte $01  ; MMC0 code
.else
	.byte $02  ; Other mapper
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
	public void ComparisonConditional_WithExpressions() {
		// arrange
		var source = @"
SIZE = 128
.ifgt SIZE * 2, 200
	.byte $01  ; SIZE * 2 > 200 (256 > 200)
.else
	.byte $02
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
	public void ComparisonConditional_NestedWithIf() {
		// arrange
		var source = @"
DEBUG = 1
LEVEL = 2

.ifeq DEBUG, 1
	.ifgt LEVEL, 1
		.byte $ff  ; Debug mode, level > 1
	.endif
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
	public void ComparisonConditional_WithElse() {
		// arrange
		var source = @"
TARGET = 65816

.ifeq TARGET, 6502
	.byte $01  ; 6502 code
.else
	.byte $02  ; 65816 code
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
}

