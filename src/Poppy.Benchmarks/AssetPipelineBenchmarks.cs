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
	private string _source = null!;
	private string _sourcePath = null!;
	private string _tempDir = null!;

	[GlobalSetup]
	public void Setup() {
		Poppy.Arch.MOS6502.Registration.RegisterAll();

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

		_source = ".target nes\n.org $8000\n.asset_manifest \"assets.json\"\n.byte $ea\n";
		File.WriteAllText(_sourcePath, _source);
	}

	[GlobalCleanup]
	public void Cleanup() {
		if (Directory.Exists(_tempDir)) {
			Directory.Delete(_tempDir, recursive: true);
		}
	}

	[Benchmark(Description = "Compile: asset manifest (binary + json-u8)")]
	public byte[] Compile_AssetManifest() {
		var tokens = new Lexer(_source, _sourcePath).Tokenize();
		var ast = new Parser(tokens).Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(ast);
		return new CodeGenerator(analyzer, TargetArchitecture.MOS6502).Generate(ast);
	}
}
