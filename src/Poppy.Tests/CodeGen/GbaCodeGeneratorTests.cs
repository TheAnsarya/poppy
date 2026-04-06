// ============================================================================
// GbaCodeGeneratorTests.cs - GBA Code Generator Integration Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

using PoppyLexer = Poppy.Core.Lexer.Lexer;
using PoppyParser = Poppy.Core.Parser.Parser;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Integration tests for GBA (ARM7TDMI) ROM generation pipeline.
/// Tests the full lexer → parser → analyzer → code generator flow with GBA directives.
/// </summary>
public class GbaCodeGeneratorTests {
	/// <summary>
	/// Helper to run the full compilation pipeline for GBA source.
	/// </summary>
	private static (byte[] Code, CodeGenerator Generator, SemanticAnalyzer Analyzer) GenerateGbaCode(string source) {
		var lexer = new PoppyLexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new PoppyParser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer();
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.ARM7TDMI);
		var code = generator.Generate(program);

		return (code, generator, analyzer);
	}

	// ========================================================================
	// GBA ROM Header Tests
	// ========================================================================

	[Fact]
	public void Generate_Gba_ProducesValidHeader() {
		var source = @"
.gba
.gba_title ""TESTGAME""
.gba_game_code ""ATXE""
.gba_maker_code ""01""
.org $080000c0
	.byte $00
";
		var (code, gen, _) = GenerateGbaCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.True(code.Length >= 193);

		// Nintendo logo at offset $04 (first byte: $24)
		Assert.Equal(0x24, code[4]);

		// Fixed value at $b2 must be $96
		Assert.Equal(0x96, code[0xb2]);

		// Full header validation (logo + checksum + fixed byte)
		Assert.True(GbaRomBuilder.ValidateHeader(code));
	}

	[Fact]
	public void Generate_Gba_TitleAppearsInHeader() {
		var source = @"
.gba
.gba_title ""HELLO""
.gba_game_code ""AXVE""
.gba_maker_code ""01""
.org $080000c0
	.byte $00
";
		var (code, gen, _) = GenerateGbaCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal("HELLO", GbaRomBuilder.GetTitle(code));
	}

	[Fact]
	public void Generate_Gba_GameCodeAppearsInHeader() {
		var source = @"
.gba
.gba_game_code ""AXVE""
.gba_maker_code ""01""
.org $080000c0
	.byte $00
";
		var (code, gen, _) = GenerateGbaCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal("AXVE", GbaRomBuilder.GetGameCode(code));
	}

	[Fact]
	public void Generate_Gba_MakerCodeAppearsInHeader() {
		var source = @"
.gba
.gba_maker_code ""01""
.org $080000c0
	.byte $00
";
		var (code, gen, _) = GenerateGbaCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal("01", GbaRomBuilder.GetMakerCode(code));
	}

	[Fact]
	public void Generate_Gba_DefaultHeaderWhenNoDirectives() {
		var source = @"
.gba
.org $080000c0
	.byte $42
";
		var (code, gen, _) = GenerateGbaCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		// Empty header still produced (192 bytes of defaults)
		Assert.True(code.Length >= 193);
		// No builder configured → fallback empty 192-byte header has no logo/checksum
		// but still has correct size
	}

	// ========================================================================
	// GBA Code Segment Placement Tests
	// ========================================================================

	[Fact]
	public void Generate_Gba_CodePlacedAtCorrectFileOffset() {
		var source = @"
.gba
.org $080000c0
	.byte $aa, $bb, $cc
";
		var (code, gen, _) = GenerateGbaCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		// $080000c0 - $08000000 = $c0 file offset
		Assert.True(code.Length >= 0xc3);
		Assert.Equal(0xaa, code[0xc0]);
		Assert.Equal(0xbb, code[0xc1]);
		Assert.Equal(0xcc, code[0xc2]);
	}

	[Fact]
	public void Generate_Gba_TargetShorthandAlias() {
		var source = @"
.gameboyadvance
.org $080000c0
	.byte $ff
";
		var (code, gen, _) = GenerateGbaCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.True(code.Length >= 0xc1);
		Assert.Equal(0xff, code[0xc0]);
	}

	// ========================================================================
	// GBA Directive Validation Tests
	// ========================================================================

	[Fact]
	public void Analyze_Gba_TitleTooLongProducesError() {
		var source = @"
.gba
.gba_title ""THISISTOOLONG!""
.org $080000c0
	.byte $00
";
		var (_, _, analyzer) = GenerateGbaCode(source);
		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("too long"));
	}

	[Fact]
	public void Analyze_Gba_InvalidGameCodeLengthProducesError() {
		var source = @"
.gba
.gba_game_code ""AB""
.org $080000c0
	.byte $00
";
		var (_, _, analyzer) = GenerateGbaCode(source);
		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("exactly 4 characters"));
	}

	[Fact]
	public void Analyze_Gba_InvalidMakerCodeLengthProducesError() {
		var source = @"
.gba
.gba_maker_code ""ABC""
.org $080000c0
	.byte $00
";
		var (_, _, analyzer) = GenerateGbaCode(source);
		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("exactly 2 characters"));
	}
}
