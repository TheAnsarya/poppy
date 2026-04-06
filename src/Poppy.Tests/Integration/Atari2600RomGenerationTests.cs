// ============================================================================
// Atari2600RomGenerationTests.cs - Atari 2600 ROM Generation Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Integration;

/// <summary>
/// Integration tests for Atari 2600 ROM generation.
/// Verifies the full pipeline: lexer → parser → semantic analyzer → code generator
/// produces correct 6507 binary output with proper ROM layout and reset vectors.
/// </summary>
public sealed class Atari2600RomGenerationTests {
	[Fact]
	public void Generate_MinimalAtari2600Rom_CreatesCorrect4KBinary() {
		// arrange - minimal Atari 2600 ROM with reset code
		var source = @"
.target atari2600

.org $f000
reset:
	sei
	cld
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6507);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6507);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Atari 2600 default is 4K ROM
		Assert.Equal(4096, binary.Length);

		// $f000 masked to 13 bits = $1000, offset = $1000 - $1000 = $0000
		Assert.Equal(0x78, binary[0x0000]);  // sei
		Assert.Equal(0xd8, binary[0x0001]);  // cld
		Assert.Equal(0xea, binary[0x0002]);  // nop
	}

	[Fact]
	public void Generate_Atari2600Rom_HasResetVectors() {
		// arrange - ROM with labeled reset
		var source = @"
.target atari2600

.org $f000
reset:
	sei
	cld
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6507);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6507);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Reset vector at $fffc/$fffd → ROM offset $0ffc/$0ffd
		// Default reset vector points to $1000 (13-bit address for ROM start)
		Assert.Equal(0x00, binary[0x0ffc]);  // reset vector low = $00
		Assert.Equal(0x10, binary[0x0ffd]);  // reset vector high = $10
	}

	[Fact]
	public void Generate_Atari2600Rom_UnusedSpaceFilledWithFF() {
		// arrange
		var source = @"
.target atari2600

.org $f000
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6507);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6507);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Code at offset 0
		Assert.Equal(0xea, binary[0x0000]);  // nop

		// Unused space should be $ff
		Assert.Equal(0xff, binary[0x0002]);
		Assert.Equal(0xff, binary[0x0100]);
	}

	[Fact]
	public void Generate_Atari2600Rom_WithTypicalStartup() {
		// arrange - typical 2600 startup sequence
		var source = @"
.target atari2600

.org $f000
reset:
	sei             ; disable interrupts
	cld             ; clear decimal mode
	ldx #$ff
	txs             ; set up stack at $ff
	lda #$00        ; clear accumulator
loop:
	jmp loop        ; infinite loop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6507);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6507);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		int o = 0x0000;
		Assert.Equal(0x78, binary[o]);      // sei
		Assert.Equal(0xd8, binary[o + 1]);  // cld
		Assert.Equal(0xa2, binary[o + 2]);  // ldx #$ff
		Assert.Equal(0xff, binary[o + 3]);
		Assert.Equal(0x9a, binary[o + 4]);  // txs
		Assert.Equal(0xa9, binary[o + 5]);  // lda #$00
		Assert.Equal(0x00, binary[o + 6]);
		Assert.Equal(0x4c, binary[o + 7]);  // jmp loop ($f007)
		Assert.Equal(0x07, binary[o + 8]);  // low byte
		Assert.Equal(0xf0, binary[o + 9]);  // high byte
	}

	[Fact]
	public void Generate_Atari2600TargetAlias_AllAliasesWork() {
		// All target aliases should produce valid Atari 2600 ROMs
		string[] aliases = ["atari2600"];

		foreach (var alias in aliases) {
			var source = $@"
.target {alias}

.org $f000
	nop
";
			var lexer = new Core.Lexer.Lexer(source, "test.pasm");
			var tokens = lexer.Tokenize();
			var parser = new Core.Parser.Parser(tokens);
			var program = parser.Parse();

			var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6507);
			analyzer.Analyze(program);

			var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6507);
			var binary = generator.Generate(program);

			Assert.False(analyzer.HasErrors, $"Alias '{alias}' had analyzer errors: {GetErrorsString(analyzer)}");
			Assert.False(generator.HasErrors, $"Alias '{alias}' had generator errors: {GetErrorsString(generator)}");

			Assert.Equal(4096, binary.Length);
			Assert.Equal(0xea, binary[0x0000]);  // nop at $f000
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
