// ============================================================================
// MasterSystemRomGenerationTests.cs - Sega Master System ROM Generation Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Integration;

/// <summary>
/// Integration tests for Sega Master System ROM generation.
/// Verifies the full pipeline: lexer → parser → semantic analyzer → code generator
/// produces correct Z80 binary output with proper "TMR SEGA" header and ROM layout.
/// </summary>
public sealed class MasterSystemRomGenerationTests {
	[Fact]
	public void Generate_MinimalSmsRom_CreatesCorrect32KBinary() {
		// arrange - minimal SMS ROM with startup code
		var source = @"
.target sms

.org $0000
reset:
	di
	nop
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.Z80);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.Z80);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Default SMS ROM size is 32K
		Assert.Equal(32 * 1024, binary.Length);

		// Code at address $0000 = ROM offset $0000
		Assert.Equal(0xf3, binary[0x0000]);  // di
		Assert.Equal(0x00, binary[0x0001]);  // nop
		Assert.Equal(0x00, binary[0x0002]);  // nop
	}

	[Fact]
	public void Generate_SmsRom_HasTmrSegaHeader() {
		// arrange
		var source = @"
.target mastersystem

.org $0000
	di
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.Z80);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.Z80);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// "TMR SEGA" signature at $7ff0 for 32K ROMs
		var sig = System.Text.Encoding.ASCII.GetString(binary, 0x7ff0, 8);
		Assert.Equal("TMR SEGA", sig);
	}

	[Fact]
	public void Generate_SmsRom_UnusedSpaceFilledWithFF() {
		// arrange
		var source = @"
.target sms

.org $0000
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.Z80);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.Z80);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Code at $0000
		Assert.Equal(0x00, binary[0x0000]);  // nop

		// Unused space should be $ff
		Assert.Equal(0xff, binary[0x0002]);
		Assert.Equal(0xff, binary[0x0100]);
	}

	[Fact]
	public void Generate_SmsRom_WithTypicalStartup() {
		// arrange - typical SMS startup sequence using single-operand instructions
		// (multi-operand like 'ld a, $00' requires parser extension — see issue #246)
		var source = @"
.target sms

.org $0000
reset:
	di              ; $f3 - disable interrupts
	nop             ; $00
	daa             ; $27 - decimal adjust accumulator
	halt            ; $76 - halt CPU
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.Z80);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.Z80);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		Assert.Equal(0xf3, binary[0x0000]);  // di
		Assert.Equal(0x00, binary[0x0001]);  // nop
		Assert.Equal(0x27, binary[0x0002]);  // daa
		Assert.Equal(0x76, binary[0x0003]);  // halt
	}

	[Fact]
	public void Generate_SmsTargetAlias_AllAliasesWork() {
		// All target aliases should produce valid SMS ROMs
		string[] aliases = ["sms", "mastersystem", "z80"];

		foreach (var alias in aliases) {
			var source = $@"
.target {alias}

.org $0000
	nop
";
			var lexer = new Core.Lexer.Lexer(source, "test.pasm");
			var tokens = lexer.Tokenize();
			var parser = new Core.Parser.Parser(tokens);
			var program = parser.Parse();

			var analyzer = new SemanticAnalyzer(TargetArchitecture.Z80);
			analyzer.Analyze(program);

			var generator = new CodeGenerator(analyzer, TargetArchitecture.Z80);
			var binary = generator.Generate(program);

			Assert.False(analyzer.HasErrors, $"Alias '{alias}' had analyzer errors: {GetErrorsString(analyzer)}");
			Assert.False(generator.HasErrors, $"Alias '{alias}' had generator errors: {GetErrorsString(generator)}");

			Assert.Equal(32 * 1024, binary.Length);
			Assert.Equal(0x00, binary[0x0000]);  // nop
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
