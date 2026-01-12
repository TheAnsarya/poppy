using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Semantics;

/// <summary>
/// Tests for macro default parameters feature.
/// Verifies that macros can define default parameter values and use them
/// when arguments are not provided during invocation.
/// </summary>
public class MacroDefaultParametersTests {
	[Fact]
	public void MacroDefaultParameter_SingleParameter_UsesDefault() {
		// arrange
		var source = @"
.macro load_default value=$42
	lda #value
.endmacro

@load_default
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - should use default value $42
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void MacroDefaultParameter_SingleParameter_OverrideDefault() {
		// arrange
		var source = @"
.macro load_default value=$42
	lda #value
.endmacro

@load_default $ff
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - should use provided value $ff
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void MacroDefaultParameter_MultipleDefaults_AllUsed() {
		// arrange
		var source = @"
.macro init_sprite x=$00, y=$00, tile=$01
	lda #x
	sta $0200
	lda #y
	sta $0201
	lda #tile
	sta $0202
.endmacro

@init_sprite
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - should use all default values
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void MacroDefaultParameter_MultipleDefaults_PartialOverride() {
		// arrange
		var source = @"
.macro init_sprite x=$00, y=$00, tile=$01
	lda #x
	sta $0200
	lda #y
	sta $0201
	lda #tile
	sta $0202
.endmacro

@init_sprite $10, $20
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - should use provided values for x/y, default for tile
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void MacroDefaultParameter_MixedRequiredAndOptional() {
		// arrange
		var source = @"
sprite_buffer = $0200

.macro sprite_dma addr, count=$40, channel=$00
	lda #>addr
	sta $2003
	lda #<addr
	sta $2004
	ldx #count
	ldy #channel
.endmacro

@sprite_dma sprite_buffer
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - required param provided, optional params use defaults
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void MacroDefaultParameter_MixedRequiredAndOptional_AllProvided() {
		// arrange
		var source = @"
sprite_buffer = $0200

.macro sprite_dma addr, count=$40, channel=$00
	lda #>addr
	sta $2003
	lda #<addr
	sta $2004
	ldx #count
	ldy #channel
.endmacro

@sprite_dma sprite_buffer, $80, $01
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - all parameters provided
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void MacroDefaultParameter_ExpressionAsDefault() {
		// arrange
		var source = @"
BUFFER_SIZE = 256

.macro fill_buffer value=$00, count=BUFFER_SIZE
	ldx #count
	lda #value
	sta $0200
.endmacro

@fill_buffer
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - expression defaults should work
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void MacroDefaultParameter_MissingRequiredParameter_ReportsError() {
		// arrange
		var source = @"
.macro sprite_dma addr, count=$40, channel=$00
	lda #>addr
	sta $2003
.endmacro

@sprite_dma
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - missing required parameter should report error
		Assert.True(analyzer.HasErrors);
	}

	[Fact]
	public void MacroDefaultParameter_BackwardCompatibility_NoDefaults() {
		// arrange - existing macro syntax without defaults should still work
		var source = @"
.macro simple_macro param1, param2
	lda param1
	sta param2
.endmacro

@simple_macro #$42, $2000
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - existing macros should continue to work
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void MacroDefaultParameter_OnlyOptionalParameters() {
		// arrange - simplified macro to test defaults
		var source = @"
.macro load_value value=$01
	lda #value
.endmacro

@load_value
@load_value $10
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// DEBUG: Output errors
		if (analyzer.HasErrors) {
			foreach (var e in analyzer.Errors) {
				Console.WriteLine($"ERROR: {e}");
			}
		}

		// assert - macro with all optional params should work
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void MacroDefaultParameter_DefaultsAfterRequired() {
		// arrange - test common pattern: required params first, defaults after
		var source = @"
.macro ppu_write addr, value, count=$01
	bit $2002
	lda #>addr
	sta $2006
	lda #<addr
	sta $2006
	lda #value
	ldx #count
	sta $2007
.endmacro

@ppu_write $2000, $ff
@ppu_write $2000, $00, $20
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - should work with both invocations
		Assert.False(analyzer.HasErrors);
	}

	[Fact]
	public void MacroDefaultParameter_HexValueAsDefault() {
		// arrange
		var source = @"
.macro set_byte addr=$2000, value=$ff
	lda #value
	sta addr
.endmacro

@set_byte
@set_byte $3000
@set_byte $3000, $42
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert - hex defaults should work
		Assert.False(analyzer.HasErrors);
	}
}
