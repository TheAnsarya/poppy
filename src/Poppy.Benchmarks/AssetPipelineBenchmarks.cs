using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;
using System.Text;

namespace Poppy.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class AssetPipelineBenchmarks {
	private string _sourceNes = null!;
	private string _sourceA26 = null!;
	private string _sourceSnes = null!;
	private string _sourceGb = null!;
	private string _sourceSms = null!;
	private string _sourceTg16 = null!;
	private string _sourceGenesis = null!;
	private string _sourceChannelF = null!;
	private string _sourcePath = null!;
	private string _tempDir = null!;

	[GlobalSetup]
	public void Setup() {
		Poppy.Arch.MOS6502.Registration.RegisterAll();
		Poppy.Arch.WDC65816.Registration.RegisterAll();
		Poppy.Arch.SM83.Registration.RegisterAll();
		Poppy.Arch.M68000.Registration.RegisterAll();
		Poppy.Arch.Z80.Registration.RegisterAll();
		Poppy.Arch.HuC6280.Registration.RegisterAll();

		_tempDir = Path.Combine(Path.GetTempPath(), "poppy-bench-assets-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);

		var dataPath = Path.Combine(_tempDir, "blob.bin");
		var jsonPath = Path.Combine(_tempDir, "table.json");
		var manifestPath = Path.Combine(_tempDir, "assets.json");
		_sourcePath = Path.Combine(_tempDir, "bench.pasm");

		var random = new Random(42);
		var blob = new byte[4096];
		random.NextBytes(blob);
		File.WriteAllBytes(dataPath, blob);

		var numbers = Enumerable.Range(0, 2048).Select(i => (i * 7) & 0xff);
		var json = "{\"bytes\":[" + string.Join(',', numbers) + "]}";
		File.WriteAllText(jsonPath, json);

		var manifest = "{\"assets\":[" +
			"{\"type\":\"binary\",\"path\":\"blob.bin\",\"offset\":256,\"length\":1024}," +
			"{\"type\":\"json-u8\",\"path\":\"table.json\",\"jsonPath\":\"bytes\"}" +
			"]}";
		File.WriteAllText(manifestPath, manifest);

		_sourceNes = ".target nes\n.org $8000\n.asset_manifest \"assets.json\"\n.byte $ea\n";
		_sourceA26 = ".target a26\n.org $f000\n.asset_manifest \"assets.json\"\n.byte $ea\n";
		_sourceSnes = ".target snes\n.org $808000\n.asset_manifest \"assets.json\"\n.byte $ea\n";
		_sourceGb = ".target gb\n.org $0100\n.asset_manifest \"assets.json\"\n.byte $00\n";
		_sourceSms = ".target sms\n.org $0000\n.asset_manifest \"assets.json\"\n.byte $00\n";
		_sourceTg16 = ".target tg16\n.org $8000\n.asset_manifest \"assets.json\"\n.byte $ea\n";
		_sourceGenesis = ".target genesis\n.org $000200\n.asset_manifest \"assets.json\"\nmoveq #$2a, d0\n";
		_sourceChannelF = ".target channelf\n.org $0800\n.asset_manifest \"assets.json\"\n.byte $00\n";
		File.WriteAllText(_sourcePath, _sourceNes);
	}

	[GlobalCleanup]
	public void Cleanup() {
		if (Directory.Exists(_tempDir)) {
			Directory.Delete(_tempDir, recursive: true);
		}
	}

	[Benchmark(Description = "Asset-heavy compile: NES")]
	public byte[] Compile_AssetManifest_Nes() {
		return Compile(_sourceNes, TargetArchitecture.MOS6502);
	}

	[Benchmark(Description = "Asset-heavy compile: Atari 2600")]
	public byte[] Compile_AssetManifest_A26() {
		return Compile(_sourceA26, TargetArchitecture.MOS6507);
	}

	[Benchmark(Description = "Asset-heavy compile: SNES")]
	public byte[] Compile_AssetManifest_Snes() {
		return Compile(_sourceSnes, TargetArchitecture.WDC65816);
	}

	[Benchmark(Description = "Asset-heavy compile: Game Boy")]
	public byte[] Compile_AssetManifest_Gb() {
		return Compile(_sourceGb, TargetArchitecture.SM83);
	}

	[Benchmark(Description = "Asset-heavy compile: Master System")]
	public byte[] Compile_AssetManifest_Sms() {
		return Compile(_sourceSms, TargetArchitecture.Z80);
	}

	[Benchmark(Description = "Asset-heavy compile: TG16")]
	public byte[] Compile_AssetManifest_Tg16() {
		return Compile(_sourceTg16, TargetArchitecture.HuC6280);
	}

	[Benchmark(Description = "Asset-heavy compile: Genesis")]
	public byte[] Compile_AssetManifest_Genesis() {
		return Compile(_sourceGenesis, TargetArchitecture.M68000);
	}

	[Benchmark(Description = "Asset-heavy compile: Channel F")]
	public byte[] Compile_AssetManifest_ChannelF() {
		return Compile(_sourceChannelF, TargetArchitecture.F8);
	}

	private byte[] Compile(string source, TargetArchitecture target) {
		var tokens = new Lexer(source, _sourcePath).Tokenize();
		var ast = new Parser(tokens).Parse();
		var analyzer = new SemanticAnalyzer(target);
		analyzer.Analyze(ast);
		return new CodeGenerator(analyzer, target).Generate(ast);
	}
}
