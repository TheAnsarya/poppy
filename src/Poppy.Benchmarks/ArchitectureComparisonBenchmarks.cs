using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

namespace Poppy.Benchmarks;

/// <summary>
/// Compares full compile pipeline times across all target architectures.
/// Each benchmark compiles a hello-world example for its platform.
/// Compare compile time per architecture to identify outliers.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ArchitectureComparisonBenchmarks {
	private readonly record struct PlatformSource(string Source, TargetArchitecture Architecture);

	private PlatformSource _nes;
	private PlatformSource _snes;
	private PlatformSource _gb;
	private PlatformSource _genesis;
	private PlatformSource _gba;
	private PlatformSource _lynx;
	private PlatformSource _atari2600;
	private PlatformSource _spc700;
	private PlatformSource _masterSystem;
	private PlatformSource _turbografx;
	private PlatformSource _wonderswan;
	private PlatformSource _channelF;

	[GlobalSetup]
	public void Setup() {
		// Register architecture profiles
		Poppy.Arch.MOS6502.Registration.RegisterAll();
		Poppy.Arch.WDC65816.Registration.RegisterAll();
		Poppy.Arch.SM83.Registration.RegisterAll();
		Poppy.Arch.M68000.Registration.RegisterAll();
		Poppy.Arch.Z80.Registration.RegisterAll();
		Poppy.Arch.V30MZ.Registration.RegisterAll();
		Poppy.Arch.ARM7TDMI.Registration.RegisterAll();
		Poppy.Arch.SPC700.Registration.RegisterAll();
		Poppy.Arch.HuC6280.Registration.RegisterAll();

		var examplesDir = FindExamplesDirectory();

		_nes = new(File.ReadAllText(Path.Combine(examplesDir, "nes-hello-world", "main.pasm")), TargetArchitecture.MOS6502);
		_snes = new(File.ReadAllText(Path.Combine(examplesDir, "snes-hello-world", "main.pasm")), TargetArchitecture.WDC65816);
		_gb = new(File.ReadAllText(Path.Combine(examplesDir, "gb-hello-world", "main.pasm")), TargetArchitecture.SM83);
		_genesis = new(File.ReadAllText(Path.Combine(examplesDir, "genesis-hello-world", "main.pasm")), TargetArchitecture.M68000);
		_gba = new(File.ReadAllText(Path.Combine(examplesDir, "gba-hello-world", "main.pasm")), TargetArchitecture.ARM7TDMI);
		_lynx = new(File.ReadAllText(Path.Combine(examplesDir, "lynx-hello-world", "main.pasm")), TargetArchitecture.MOS65SC02);
		_atari2600 = new(File.ReadAllText(Path.Combine(examplesDir, "atari2600-hello-world", "main.pasm")), TargetArchitecture.MOS6507);
		_spc700 = new(File.ReadAllText(Path.Combine(examplesDir, "spc700-hello-world", "main.pasm")), TargetArchitecture.SPC700);
		_masterSystem = new(File.ReadAllText(Path.Combine(examplesDir, "mastersystem-hello-world", "main.pasm")), TargetArchitecture.Z80);
		_turbografx = new(File.ReadAllText(Path.Combine(examplesDir, "turbografx-hello-world", "main.pasm")), TargetArchitecture.HuC6280);
		_wonderswan = new(File.ReadAllText(Path.Combine(examplesDir, "wonderswan-hello-world", "main.pasm")), TargetArchitecture.V30MZ);
		_channelF = new(File.ReadAllText(Path.Combine(examplesDir, "channelf-hello-world", "main.pasm")), TargetArchitecture.F8);
	}

	// ========================================================================
	// Full Compile per Architecture
	// ========================================================================

	[Benchmark(Description = "NES (6502)")]
	public byte[] Compile_NES() => FullCompile(_nes);

	[Benchmark(Description = "SNES (65816)")]
	public byte[] Compile_SNES() => FullCompile(_snes);

	[Benchmark(Description = "GB (SM83)")]
	public byte[] Compile_GB() => FullCompile(_gb);

	[Benchmark(Description = "Genesis (M68000)")]
	public byte[] Compile_Genesis() => FullCompile(_genesis);

	[Benchmark(Description = "GBA (ARM7TDMI)")]
	public byte[] Compile_GBA() => FullCompile(_gba);

	[Benchmark(Description = "Lynx (65SC02)")]
	public byte[] Compile_Lynx() => FullCompile(_lynx);

	[Benchmark(Description = "Atari 2600 (6507)")]
	public byte[] Compile_Atari2600() => FullCompile(_atari2600);

	[Benchmark(Description = "SPC700")]
	public byte[] Compile_SPC700() => FullCompile(_spc700);

	[Benchmark(Description = "Master System (Z80)")]
	public byte[] Compile_MasterSystem() => FullCompile(_masterSystem);

	[Benchmark(Description = "TurboGrafx (HuC6280)")]
	public byte[] Compile_TurboGrafx() => FullCompile(_turbografx);

	[Benchmark(Description = "WonderSwan (V30MZ)")]
	public byte[] Compile_WonderSwan() => FullCompile(_wonderswan);

	[Benchmark(Description = "Channel F (F8)")]
	public byte[] Compile_ChannelF() => FullCompile(_channelF);

	// ========================================================================
	// Helpers
	// ========================================================================

	private static byte[] FullCompile(PlatformSource platform) {
		var tokens = new Lexer(platform.Source, "bench.pasm").Tokenize();
		var ast = new Parser(tokens).Parse();
		var analyzer = new SemanticAnalyzer(platform.Architecture);
		analyzer.Analyze(ast);
		return new CodeGenerator(analyzer, platform.Architecture).Generate(ast);
	}

	private static string FindExamplesDirectory() {
		var candidates = new[] {
			Path.Combine(Environment.CurrentDirectory, "examples"),
			Path.Combine(Environment.CurrentDirectory, "..", "..", "examples"),
			Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "examples"),
			Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "examples"),
			Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "..", "examples"),
		};

		foreach (var candidate in candidates) {
			var full = Path.GetFullPath(candidate);
			if (Directory.Exists(full)) {
				return full;
			}
		}

		var dir = AppContext.BaseDirectory;
		for (int i = 0; i < 15; i++) {
			var candidate = Path.Combine(dir, "examples");
			if (Directory.Exists(candidate)) {
				return candidate;
			}

			var parent = Path.GetDirectoryName(dir);
			if (parent == null || parent == dir) break;
			dir = parent;
		}

		throw new DirectoryNotFoundException(
			$"Could not find examples/ directory. CWD={Environment.CurrentDirectory}, Base={AppContext.BaseDirectory}");
	}
}
