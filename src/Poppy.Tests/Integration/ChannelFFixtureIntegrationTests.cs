// ============================================================================
// ChannelFFixtureIntegrationTests.cs - Channel F fixture end-to-end tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;

using PoppyLexer = Poppy.Core.Lexer.Lexer;
using PoppyParser = Poppy.Core.Parser.Parser;

namespace Poppy.Tests.Integration;

/// <summary>
/// End-to-end fixture tests for Channel F (F8) scaffold support.
/// These fixtures lock down byte output and deterministic generation behavior.
/// </summary>
public sealed class ChannelFFixtureIntegrationTests {
	public static IEnumerable<object[]> FixtureVectors() {
		yield return ["f8_mvp_sequence.pasm", new byte[] { 0x20, 0x12, 0x34, 0x56, 0x9a, 0x78, 0x2b, 0x29, 0x20 }];
		yield return ["f8_loop_label.pasm", new byte[] { 0x2b, 0x20, 0x01, 0x29, 0x00 }];
		yield return ["f8_game_garden_smoke.pasm", new byte[] { 0x20, 0x42, 0x2b, 0x29, 0x00, 0x00 }];
	}

	[Theory]
	[MemberData(nameof(FixtureVectors))]
	public void Fixture_CompilesAndEmitsExpectedBytes(string fixtureName, byte[] expectedBytes) {
		var (code, analyzer, generator) = CompileFixture(fixtureName);

		Assert.False(analyzer.HasErrors, string.Join("\n", analyzer.Errors.Select(e => e.Message)));
		Assert.False(generator.HasErrors, string.Join("\n", generator.Errors.Select(e => e.Message)));
		Assert.Equal(TargetArchitecture.F8, analyzer.Target);
		Assert.Equal(expectedBytes, code);
	}

	[Fact]
	public void Fixture_OutputIsDeterministicAcrossRuns() {
		var (first, analyzer1, generator1) = CompileFixture("f8_game_garden_smoke.pasm");
		var (second, analyzer2, generator2) = CompileFixture("f8_game_garden_smoke.pasm");

		Assert.False(analyzer1.HasErrors, string.Join("\n", analyzer1.Errors.Select(e => e.Message)));
		Assert.False(generator1.HasErrors, string.Join("\n", generator1.Errors.Select(e => e.Message)));
		Assert.False(analyzer2.HasErrors, string.Join("\n", analyzer2.Errors.Select(e => e.Message)));
		Assert.False(generator2.HasErrors, string.Join("\n", generator2.Errors.Select(e => e.Message)));
		Assert.Equal(first, second);
	}

	private static (byte[] Code, SemanticAnalyzer Analyzer, CodeGenerator Generator) CompileFixture(string fixtureName) {
		var source = File.ReadAllText(GetFixturePath(fixtureName));
		var lexer = new PoppyLexer(source);
		var tokens = lexer.Tokenize();
		var parser = new PoppyParser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer();
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, analyzer.Target);
		var code = generator.Generate(program);

		return (code, analyzer, generator);
	}

	private static string GetFixturePath(string fixtureName) {
		return Path.Combine(AppContext.BaseDirectory, "Fixtures", "ChannelF", fixtureName);
	}
}
