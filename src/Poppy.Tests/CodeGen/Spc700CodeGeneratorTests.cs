// ============================================================================
// Spc700CodeGeneratorTests.cs - SPC700 Code Generator Integration Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text;
using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

using PoppyLexer = Poppy.Core.Lexer.Lexer;
using PoppyParser = Poppy.Core.Parser.Parser;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Integration tests for SPC700 (.spc) file generation pipeline.
/// Tests the full lexer → parser → analyzer → code generator flow with SPC directives.
/// </summary>
public class Spc700CodeGeneratorTests {
	/// <summary>
	/// SPC file total size: 256 header + 65536 RAM + 128 DSP + 64 extra RAM = 65984 bytes.
	/// </summary>
	private const int SpcFileSize = 65984;

	/// <summary>
	/// Helper to run the full compilation pipeline for SPC700 source.
	/// </summary>
	private static (byte[] Code, CodeGenerator Generator, SemanticAnalyzer Analyzer) GenerateSpcCode(string source) {
		var lexer = new PoppyLexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new PoppyParser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer();
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.SPC700);
		var code = generator.Generate(program);

		return (code, generator, analyzer);
	}

	// ========================================================================
	// SPC File Structure Tests
	// ========================================================================

	[Fact]
	public void Generate_Spc_ProducesCorrectFileSize() {
		var source = @"
.spc700
.org $0200
	.byte $00
";
		var (code, gen, _) = GenerateSpcCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal(SpcFileSize, code.Length);
	}

	[Fact]
	public void Generate_Spc_HasCorrectSignature() {
		var source = @"
.spc700
.org $0200
	.byte $00
";
		var (code, gen, _) = GenerateSpcCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));

		// SPC signature: "SNES-SPC700 Sound File Data v0.30"
		var signature = Encoding.ASCII.GetString(code, 0, 33);
		Assert.Equal("SNES-SPC700 Sound File Data v0.30", signature);
	}

	[Fact]
	public void Generate_Spc_CodePlacedInRam() {
		var source = @"
.spc700
.org $0200
	.byte $aa, $bb, $cc
";
		var (code, gen, _) = GenerateSpcCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));

		// RAM starts at offset $100 (256) in the SPC file
		// Code at address $0200 → file offset $100 + $0200 = $0300
		Assert.Equal(0xaa, code[0x100 + 0x0200]);
		Assert.Equal(0xbb, code[0x100 + 0x0201]);
		Assert.Equal(0xcc, code[0x100 + 0x0202]);
	}

	// ========================================================================
	// SPC Directive Tests
	// ========================================================================

	[Fact]
	public void Generate_Spc_SongTitleInHeader() {
		var source = @"
.spc700
.spc_song_title ""Test Song""
.org $0200
	.byte $00
";
		var (code, gen, _) = GenerateSpcCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));

		// Song title at offset $2e (32 bytes, null-padded)
		var songTitle = Encoding.ASCII.GetString(code, 0x2e, 9);
		Assert.Equal("Test Song", songTitle);
	}

	[Fact]
	public void Generate_Spc_GameTitleInHeader() {
		var source = @"
.spc700
.spc_game_title ""My Game""
.org $0200
	.byte $00
";
		var (code, gen, _) = GenerateSpcCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));

		// Game title at offset $4e (32 bytes, null-padded)
		var gameTitle = Encoding.ASCII.GetString(code, 0x4e, 7);
		Assert.Equal("My Game", gameTitle);
	}

	[Fact]
	public void Generate_Spc_ArtistNameInHeader() {
		var source = @"
.spc700
.spc_artist ""Composer""
.org $0200
	.byte $00
";
		var (code, gen, _) = GenerateSpcCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));

		// Artist at offset $b0 (32 bytes, null-padded)
		var artist = Encoding.ASCII.GetString(code, 0xb0, 8);
		Assert.Equal("Composer", artist);
	}

	[Fact]
	public void Generate_Spc_EntryPointSetsPc() {
		var source = @"
.spc700
.spc_entry $0400
.org $0400
	.byte $ff
";
		var (code, gen, _) = GenerateSpcCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));

		// PC register at offset $25 (2 bytes, little-endian)
		var pc = code[0x25] | (code[0x26] << 8);
		Assert.Equal(0x0400, pc);
	}

	[Fact]
	public void Generate_Spc_AutoDetectsPcFromFirstSegment() {
		var source = @"
.spc700
.org $0300
	.byte $42
";
		var (code, gen, _) = GenerateSpcCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));

		// PC should be auto-set to $0300 (first segment address)
		var pc = code[0x25] | (code[0x26] << 8);
		Assert.Equal(0x0300, pc);
	}

	[Fact]
	public void Generate_Spc_TargetShorthandAlias() {
		var source = @"
.spc
.org $0200
	.byte $ff
";
		var (code, gen, _) = GenerateSpcCode(source);

		Assert.False(gen.HasErrors, string.Join("; ", gen.Errors.Select(e => e.Message)));
		Assert.Equal(SpcFileSize, code.Length);
	}

	// ========================================================================
	// SPC Directive Validation Tests
	// ========================================================================

	[Fact]
	public void Analyze_Spc_SongTitleTooLongProducesError() {
		var source = @"
.spc700
.spc_song_title ""This title is way too long to fit in 32 characters""
.org $0200
	.byte $00
";
		var (_, _, analyzer) = GenerateSpcCode(source);
		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("too long"));
	}

	[Fact]
	public void Analyze_Spc_EntryPointOutOfRangeProducesError() {
		var source = @"
.spc700
.spc_entry $10000
.org $0200
	.byte $00
";
		var (_, _, analyzer) = GenerateSpcCode(source);
		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("$0000-$ffff"));
	}
}
