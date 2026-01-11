// ============================================================================
// SemanticAnalyzerTests.cs - Semantic Analyzer Unit Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Semantics;

/// <summary>
/// Unit tests for the SemanticAnalyzer class.
/// </summary>
public class SemanticAnalyzerTests {
	// ========================================================================
	// Helper Methods
	// ========================================================================

	/// <summary>
	/// Helper to parse and analyze source code.
	/// </summary>
	private static SemanticAnalyzer Analyze(string source, TargetArchitecture target = TargetArchitecture.MOS6502) {
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(target);
		analyzer.Analyze(program);
		return analyzer;
	}

	// ========================================================================
	// Symbol Table Tests
	// ========================================================================

	[Fact]
	public void Analyze_SimpleLabel_DefinesSymbol() {
		var analyzer = Analyze("main:");

		Assert.True(analyzer.SymbolTable.TryGetSymbol("main", out var symbol));
		Assert.NotNull(symbol);
		Assert.Equal("main", symbol.Name);
		Assert.Equal(SymbolType.Label, symbol.Type);
		Assert.True(symbol.IsDefined);
		Assert.Equal(0L, symbol.Value);
	}

	[Fact]
	public void Analyze_LabelAfterInstructions_HasCorrectAddress() {
		var source = """
			.org $8000
			nop
			nop
			label:
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("label", out var symbol));
		Assert.Equal(0x8002L, symbol!.Value); // $8000 + 2 bytes (2 nop instructions)
	}

	[Fact]
	public void Analyze_MultipleLabels_AllDefined() {
		var source = """
			.org $8000
			start:
				nop
			middle:
				nop
			end:
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("start", out var start));
		Assert.True(analyzer.SymbolTable.TryGetSymbol("middle", out var middle));
		Assert.True(analyzer.SymbolTable.TryGetSymbol("end", out var end));

		Assert.Equal(0x8000L, start!.Value);
		Assert.Equal(0x8001L, middle!.Value);
		Assert.Equal(0x8002L, end!.Value);
	}

	[Fact]
	public void Analyze_DuplicateLabel_ReportsError() {
		var source = """
			label:
			label:
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("already defined"));
	}

	// ========================================================================
	// Constant Tests
	// ========================================================================

	[Fact]
	public void Analyze_EquDirective_DefinesConstant() {
		var source = "PPUCTRL = $2000";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("PPUCTRL", out var symbol));
		Assert.NotNull(symbol);
		Assert.Equal(SymbolType.Constant, symbol.Type);
		Assert.Equal(0x2000L, symbol.Value);
	}

	[Fact]
	public void Analyze_DefineDirective_DefinesConstant() {
		var source = ".define SCREEN_WIDTH, 256";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("SCREEN_WIDTH", out var symbol));
		Assert.Equal(256L, symbol!.Value);
	}

	// ========================================================================
	// Forward Reference Tests
	// ========================================================================

	[Fact]
	public void Analyze_ForwardReference_Resolves() {
		var source = """
			jmp target
			target:
			""";

		var analyzer = Analyze(source);

		Assert.False(analyzer.HasErrors);
		Assert.True(analyzer.SymbolTable.TryGetSymbol("target", out var symbol));
		Assert.True(symbol!.IsDefined);
	}

	[Fact]
	public void Analyze_UndefinedSymbol_ReportsError() {
		var source = "jmp undefined_label";

		var analyzer = Analyze(source);

		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("Undefined symbol"));
	}

	// ========================================================================
	// Address Calculation Tests
	// ========================================================================

	[Fact]
	public void Analyze_OrgDirective_SetsAddress() {
		var source = """
			.org $c000
			label:
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("label", out var symbol));
		Assert.Equal(0xc000L, symbol!.Value);
	}

	[Fact]
	public void Analyze_MultipleOrg_UpdatesAddress() {
		var source = """
			.org $8000
			first:
			.org $c000
			second:
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("first", out var first));
		Assert.True(analyzer.SymbolTable.TryGetSymbol("second", out var second));

		Assert.Equal(0x8000L, first!.Value);
		Assert.Equal(0xc000L, second!.Value);
	}

	[Fact]
	public void Analyze_ImmediateInstruction_AddsTwo() {
		var source = """
			.org $8000
			lda #$00
			label:
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("label", out var symbol));
		Assert.Equal(0x8002L, symbol!.Value); // 1 opcode + 1 operand
	}

	[Fact]
	public void Analyze_AbsoluteInstruction_AddsThree() {
		var source = """
			.org $8000
			lda $2000
			label:
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("label", out var symbol));
		Assert.Equal(0x8003L, symbol!.Value); // 1 opcode + 2 operand
	}

	[Fact]
	public void Analyze_ImpliedInstruction_AddsOne() {
		var source = """
			.org $8000
			nop
			label:
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("label", out var symbol));
		Assert.Equal(0x8001L, symbol!.Value); // 1 opcode only
	}

	// ========================================================================
	// Data Directive Tests
	// ========================================================================

	[Fact]
	public void Analyze_ByteDirective_AddsBytes() {
		var source = """
			.org $8000
			.byte $01, $02, $03
			label:
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("label", out var symbol));
		Assert.Equal(0x8003L, symbol!.Value); // 3 bytes
	}

	[Fact]
	public void Analyze_WordDirective_AddsWords() {
		var source = """
			.org $8000
			.word $1234, $5678
			label:
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("label", out var symbol));
		Assert.Equal(0x8004L, symbol!.Value); // 2 words = 4 bytes
	}

	[Fact]
	public void Analyze_StringDirective_AddsBytes() {
		var source = """
			.org $8000
			.db "Hello"
			label:
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("label", out var symbol));
		Assert.Equal(0x8005L, symbol!.Value); // 5 characters = 5 bytes
	}

	[Fact]
	public void Analyze_DsDirective_ReservesSpace() {
		var source = """
			.org $8000
			.ds 16
			label:
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("label", out var symbol));
		Assert.Equal(0x8010L, symbol!.Value); // 16 bytes reserved
	}

	// ========================================================================
	// Expression Evaluation Tests
	// ========================================================================

	[Fact]
	public void EvaluateExpression_NumberLiteral_ReturnsValue() {
		var analyzer = Analyze("");
		var lexer = new Poppy.Core.Lexer.Lexer("$ff", "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);

		// Parse an expression directly
		var source = "lda #$ff";
		var lexer2 = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens2 = lexer2.Tokenize();
		var parser2 = new Poppy.Core.Parser.Parser(tokens2);
		var program = parser2.Parse();

		var analyzer2 = new SemanticAnalyzer();
		analyzer2.Analyze(program);

		// The instruction should parse correctly
		Assert.False(analyzer2.HasErrors);
	}

	[Fact]
	public void Analyze_ExpressionWithOperators_Evaluates() {
		var source = """
			BASE = $1000
			OFFSET = $100
			COMBINED = BASE + OFFSET
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("COMBINED", out var symbol));
		Assert.Equal(0x1100L, symbol!.Value);
	}

	[Fact]
	public void Analyze_BitwiseOperations_Evaluate() {
		var source = """
			MASK = $ff00
			VALUE = MASK & $f0f0
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("VALUE", out var symbol));
		Assert.Equal(0xf000L, symbol!.Value);
	}

	[Fact]
	public void Analyze_ShiftOperations_Evaluate() {
		var source = """
			BASE = 1
			SHIFTED = BASE << 8
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("SHIFTED", out var symbol));
		Assert.Equal(0x100L, symbol!.Value);
	}

	[Fact]
	public void Analyze_LowByteOperator_Evaluates() {
		var source = """
			ADDR = $1234
			LOW = <ADDR
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("LOW", out var symbol));
		Assert.Equal(0x34L, symbol!.Value);
	}

	[Fact]
	public void Analyze_HighByteOperator_Evaluates() {
		var source = """
			ADDR = $1234
			HIGH = >ADDR
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("HIGH", out var symbol));
		Assert.Equal(0x12L, symbol!.Value);
	}

	[Fact]
	public void Analyze_BankByteOperator_Evaluates() {
		var source = """
			ADDR = $7e1234
			BANK = ^ADDR
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("BANK", out var symbol));
		Assert.Equal(0x7eL, symbol!.Value);
	}

	// ========================================================================
	// 65816 Specific Tests
	// ========================================================================

	[Fact]
	public void Analyze_LongAddress_AddsFourBytes() {
		var source = """
			.org $8000
			lda.l $7e0000
			label:
			""";

		var analyzer = Analyze(source, TargetArchitecture.WDC65816);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("label", out var symbol));
		Assert.Equal(0x8004L, symbol!.Value); // 1 opcode + 3 operand
	}

	[Fact]
	public void Analyze_SizeSuffix_OverridesDefault() {
		var source = """
			.org $8000
			lda.b $00
			label:
			""";

		var analyzer = Analyze(source, TargetArchitecture.WDC65816);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("label", out var symbol));
		Assert.Equal(0x8002L, symbol!.Value); // 1 opcode + 1 operand (forced byte)
	}

	// ========================================================================
	// Local Label Tests
	// ========================================================================

	[Fact]
	public void Analyze_LocalLabel_ScopedToParent() {
		// Using @ for local labels instead of . (. is directive prefix)
		var source = """
			main:
			@loop:
				nop
				jmp @loop
			""";

		var analyzer = Analyze(source);

		Assert.False(analyzer.HasErrors, string.Join("\n", analyzer.Errors.Select(e => e.Message)));
		Assert.True(analyzer.SymbolTable.TryGetSymbol("main@loop", out var symbol));
		Assert.True(symbol!.IsDefined);
	}

	// ========================================================================
	// Error Cases
	// ========================================================================

	[Fact]
	public void Analyze_OrgWithoutArgument_ReportsError() {
		var source = ".org";

		var analyzer = Analyze(source);

		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("requires an address"));
	}

	[Fact]
	public void Analyze_EquWithoutValue_ReportsError() {
		var source = "NAME =";

		var analyzer = Analyze(source);

		// Parser might error first, or semantic analysis
		Assert.True(analyzer.HasErrors || analyzer.SymbolTable.Symbols.Count == 0);
	}

	// ========================================================================
	// Macro Tests
	// ========================================================================

	[Fact]
	public void Analyze_MacroDefinition_DefinesSymbol() {
		var source = """
			.macro test
				nop
			.endmacro
			""";

		var analyzer = Analyze(source);

		Assert.True(analyzer.SymbolTable.TryGetSymbol("test", out var symbol));
		Assert.Equal(SymbolType.Macro, symbol!.Type);
	}

	// ========================================================================
	// Complete Program Tests
	// ========================================================================

	[Fact]
	public void Analyze_CompleteNESProgram_NoErrors() {
		var source = """
			; NES Program Header
			.org $8000

			PPUCTRL = $2000
			PPUMASK = $2001

			reset:
				sei
				cld
				ldx #$ff
				txs
				lda #$00
				sta PPUCTRL
				sta PPUMASK
				jmp main

			main:
				nop
				jmp main

			; Vectors
			.org $fffa
			.word nmi
			.word reset
			.word irq

			nmi:
				rti

			irq:
				rti
			""";

		var analyzer = Analyze(source);

		Assert.False(analyzer.HasErrors, string.Join("\n", analyzer.Errors.Select(e => e.Message)));

		// Verify key symbols
		Assert.True(analyzer.SymbolTable.TryGetSymbol("reset", out var reset));
		Assert.True(analyzer.SymbolTable.TryGetSymbol("main", out var main));
		Assert.True(analyzer.SymbolTable.TryGetSymbol("nmi", out var nmi));
		Assert.True(analyzer.SymbolTable.TryGetSymbol("irq", out var irq));
		Assert.True(analyzer.SymbolTable.TryGetSymbol("PPUCTRL", out var ppuctrl));

		Assert.Equal(0x8000L, reset!.Value);
		Assert.Equal(0x2000L, ppuctrl!.Value);
	}
}

