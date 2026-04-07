// ============================================================================
// GenesisRomGenerationTests.cs - Sega Genesis ROM Generation Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Integration;

/// <summary>
/// Integration tests for Sega Genesis / Mega Drive ROM generation.
/// Verifies the full pipeline: lexer → parser → semantic analyzer → code generator
/// produces correct M68000 binary output with proper header and ROM layout.
/// Note: M68000 uses 16-bit opcodes; full byte-level verification is blocked until
/// the CodeGenerator supports multi-byte opcode emission.
/// </summary>
public sealed class GenesisRomGenerationTests {
	[Fact]
	public void Generate_MinimalGenesisRom_CreatesCorrectSizeBinary() {
		// arrange - minimal Genesis ROM with nop
		var source = @"
.target genesis

.org $0200
reset:
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.M68000);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.M68000);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Default Genesis ROM size is 1MB
		Assert.Equal(1024 * 1024, binary.Length);
	}

	[Fact]
	public void Generate_GenesisRom_HasSegaHeader() {
		// arrange
		var source = @"
.target megadrive

.org $0200
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.M68000);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.M68000);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Genesis header is at ROM offset $100-$1ff
		// Console name starts at $100 (16 bytes)
		// Should contain "SEGA" or similar (filled by GenesisRomBuilder)
		// Verify the header region is not all $ff (was written to)
		var headerBytes = binary[0x100..0x110];
		Assert.Contains(headerBytes, b => b != 0xff);
	}

	[Fact]
	public void Generate_GenesisRom_UnusedSpaceFilledWithFF() {
		// arrange
		var source = @"
.target genesis

.org $0200
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.M68000);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.M68000);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Far unused area should be $ff
		Assert.Equal(0xff, binary[0x1000]);
		Assert.Equal(0xff, binary[0x10000]);
	}

	[Fact]
	public void Generate_GenesisTargetAlias_AllAliasesWork() {
		// All target aliases should produce valid Genesis ROMs
		// Numeric alias (68000) works because SemanticAnalyzer
		// accepts NumberLiteralNode for .target directive
		string[] aliases = ["genesis", "megadrive", "m68000", "68000", "m68k", "md"];

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

			var analyzer = new SemanticAnalyzer(TargetArchitecture.M68000);
			analyzer.Analyze(program);

			var generator = new CodeGenerator(analyzer, TargetArchitecture.M68000);
			var binary = generator.Generate(program);

			Assert.False(analyzer.HasErrors, $"Alias '{alias}' had analyzer errors: {GetErrorsString(analyzer)}");
			Assert.False(generator.HasErrors, $"Alias '{alias}' had generator errors: {GetErrorsString(generator)}");

			// All should produce 1MB Genesis ROM
			Assert.Equal(1024 * 1024, binary.Length);
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
