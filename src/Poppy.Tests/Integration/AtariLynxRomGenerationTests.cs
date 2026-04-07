// ============================================================================
// AtariLynxRomGenerationTests.cs - Atari Lynx ROM Generation Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Integration;

/// <summary>
/// Integration tests for Atari Lynx ROM generation.
/// Verifies the full pipeline: lexer → parser → semantic analyzer → code generator
/// produces correct 65SC02 binary output with proper LNX header and ROM layout.
/// </summary>
public sealed class AtariLynxRomGenerationTests {
	[Fact]
	public void Generate_MinimalLynxRom_CreatesCorrectBinary() {
		// arrange - minimal Lynx ROM with reset code
		var source = @"
.target lynx

.org $0200
reset:
	sei
	cld
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS65SC02);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS65SC02);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Lynx ROM = 64-byte header + 128K ROM data
		Assert.Equal(64 + 131072, binary.Length);

		// Code at $0200 → ROM offset 0 → file offset 64 (after LNX header)
		Assert.Equal(0x78, binary[64]);      // sei
		Assert.Equal(0xd8, binary[64 + 1]);  // cld
		Assert.Equal(0xea, binary[64 + 2]);  // nop
	}

	[Fact]
	public void Generate_LynxRom_HasLynxMagicSignature() {
		// arrange
		var source = @"
.target lynx

.org $0200
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS65SC02);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS65SC02);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// LNX header starts with "LYNX" magic (4 bytes)
		Assert.Equal((byte)'L', binary[0]);
		Assert.Equal((byte)'Y', binary[1]);
		Assert.Equal((byte)'N', binary[2]);
		Assert.Equal((byte)'X', binary[3]);
	}

	[Fact]
	public void Generate_LynxRom_UnusedSpaceFilledWithFF() {
		// arrange
		var source = @"
.target atarilynx

.org $0200
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS65SC02);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS65SC02);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Code at file offset 64
		Assert.Equal(0xea, binary[64]);  // nop

		// Unused ROM area (after code, before end) should be $ff
		Assert.Equal(0xff, binary[64 + 2]);
		Assert.Equal(0xff, binary[64 + 0x100]);
	}

	[Fact]
	public void Generate_LynxRom_With65C02Instructions() {
		// arrange - Lynx ROM with 65C02-specific instructions
		var source = @"
.target lynx

.org $0200
reset:
	sei             ; $78
	cld             ; $d8
	ldx #$ff        ; $a2, $ff
	txs             ; $9a
	stz $00         ; $64, $00 - 65C02 store zero to zero-page
	lda #$42        ; $a9, $42
loop:
	bra loop        ; $80, $fe - 65C02 branch always
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS65SC02);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS65SC02);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		int o = 64;  // after LNX header
		Assert.Equal(0x78, binary[o]);       // sei
		Assert.Equal(0xd8, binary[o + 1]);   // cld
		Assert.Equal(0xa2, binary[o + 2]);   // ldx #$ff
		Assert.Equal(0xff, binary[o + 3]);
		Assert.Equal(0x9a, binary[o + 4]);   // txs
		Assert.Equal(0x64, binary[o + 5]);   // stz $00 (65C02-specific)
		Assert.Equal(0x00, binary[o + 6]);
		Assert.Equal(0xa9, binary[o + 7]);   // lda #$42
		Assert.Equal(0x42, binary[o + 8]);
		Assert.Equal(0x80, binary[o + 9]);   // bra loop (65C02-specific)
		Assert.Equal(0xfe, binary[o + 10]);  // relative offset -2
	}

	[Fact]
	public void Generate_LynxTargetAlias_AllAliasesWork() {
		// All target aliases should produce valid Lynx ROMs
		string[] aliases = ["lynx", "atarilynx", "mos65sc02"];

		foreach (var alias in aliases) {
			var source = $@"
.target {alias}

.org $0200
	nop
";
			var lexer = new Core.Lexer.Lexer(source, "test.pasm");
			var tokens = lexer.Tokenize();
			var parser = new Core.Parser.Parser(tokens);
			var program = parser.Parse();

			var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS65SC02);
			analyzer.Analyze(program);

			var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS65SC02);
			var binary = generator.Generate(program);

			Assert.False(analyzer.HasErrors, $"Alias '{alias}' had analyzer errors: {GetErrorsString(analyzer)}");
			Assert.False(generator.HasErrors, $"Alias '{alias}' had generator errors: {GetErrorsString(generator)}");

			// Lynx ROM = 64-byte header + 128K
			Assert.Equal(64 + 131072, binary.Length);

			// LYNX magic
			Assert.Equal((byte)'L', binary[0]);

			// Code at file offset 64
			Assert.Equal(0xea, binary[64]);  // nop
		}
	}

	/// <summary>
	/// Helper to format error messages for assertion output.
	/// </summary>
	private static string GetErrorsString(SemanticAnalyzer analyzer) {
		if (!analyzer.HasErrors) return string.Empty;
		return string.Join("\n", analyzer.Errors.Select(e => e.Message));
	}

	/// <summary>
	/// Helper to format error messages for assertion output.
	/// </summary>
	private static string GetErrorsString(CodeGenerator generator) {
		if (!generator.HasErrors) return string.Empty;
		return string.Join("\n", generator.Errors.Select(e => e.Message));
	}
}
