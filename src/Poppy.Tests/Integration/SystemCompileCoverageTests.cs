// ============================================================================
// SystemCompileCoverageTests.cs - Cross-system compile smoke tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;

using PoppyLexer = Poppy.Core.Lexer.Lexer;
using PoppyParser = Poppy.Core.Parser.Parser;

namespace Poppy.Tests.Integration;

/// <summary>
/// Verifies each supported target system can compile representative PASM source.
/// This gives a fast, architecture-wide signal that model profiles and codegen stay wired.
/// </summary>
public sealed class SystemCompileCoverageTests {
	public static IEnumerable<object[]> SystemVectors() {
		yield return ["nes", TargetArchitecture.MOS6502, @".target nes
.org $8000
nop
"];

		yield return ["atari2600", TargetArchitecture.MOS6507, @".target a26
.org $f000
nop
"];

		yield return ["lynx", TargetArchitecture.MOS65SC02, @".target lynx
.org $0200
nop
"];

		yield return ["snes", TargetArchitecture.WDC65816, @".target snes
.org $808000
rep #$30
sep #$30
nop
"];

		yield return ["gameboy", TargetArchitecture.SM83, @".target gb
.org $0100
nop
halt
"];

		yield return ["genesis", TargetArchitecture.M68000, @".target genesis
.org $0000
nop
"];

		yield return ["mastersystem", TargetArchitecture.Z80, @".target sms
.org $0000
nop
halt
"];

		yield return ["wonderswan", TargetArchitecture.V30MZ, @".target ws
.org $0000
nop
cli
"];

		yield return ["gba", TargetArchitecture.ARM7TDMI, @".target gba
.org $08000000
.byte $00, $00, $00, $00
"];

		yield return ["spc700", TargetArchitecture.SPC700, @".target spc700
.org $0200
nop
clrc
"];

		yield return ["tg16", TargetArchitecture.HuC6280, @".target tg16
.org $8000
nop
inx
"];

		yield return ["channelf", TargetArchitecture.F8, @".target channelf
.org $0800
start:
	nop
	ldi #$01
	jmp start
"];
	}

	[Theory]
	[MemberData(nameof(SystemVectors))]
	public void SystemSource_CompilesWithoutErrors(string systemName, TargetArchitecture expectedTarget, string source) {
		var lexer = new PoppyLexer(source);
		var tokens = lexer.Tokenize();
		var parser = new PoppyParser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer();
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, analyzer.Target);
		var code = generator.Generate(program);

		Assert.False(analyzer.HasErrors, $"Semantic errors for {systemName}: {string.Join("\n", analyzer.Errors.Select(e => e.Message))}");
		Assert.False(generator.HasErrors, $"Codegen errors for {systemName}: {string.Join("\n", generator.Errors.Select(e => e.Message))}");
		Assert.Equal(expectedTarget, analyzer.Target);
		Assert.NotEmpty(code);
	}
}
