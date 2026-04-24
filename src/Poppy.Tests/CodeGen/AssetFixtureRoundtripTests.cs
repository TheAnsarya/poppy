using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Poppy.Tests.CodeGen;

public class AssetFixtureRoundtripTests {
	public static IEnumerable<object[]> RoundtripTargets() {
		yield return [TargetArchitecture.MOS6502, "nes"];
		yield return [TargetArchitecture.WDC65816, "snes"];
		yield return [TargetArchitecture.SM83, "gb"];
		yield return [TargetArchitecture.Z80, "sms"];
		yield return [TargetArchitecture.HuC6280, "tg16"];
		yield return [TargetArchitecture.F8, "channelf"];
	}

	[Theory]
	[MemberData(nameof(RoundtripTargets))]
	public void AssetRoundtrip_JsonAndPng_EmitDeterministicBlobAcrossTargets(TargetArchitecture target, string targetName) {
		using var temp = new TempDirectory();
		var jsonPath = Path.Combine(temp.Path, "editable.json");
		var pngPath = Path.Combine(temp.Path, "tile.png");
		var manifestPath = Path.Combine(temp.Path, "assets.json");
		var sourcePath = Path.Combine(temp.Path, "main.pasm");

		File.WriteAllText(jsonPath, "{\"bytes\":[1,2,3,255]}");
		File.WriteAllBytes(pngPath, CreateSolidPng(1, 1, 0));
		File.WriteAllText(manifestPath,
			"{\"assets\":[" +
			"{\"type\":\"json-u8\",\"path\":\"editable.json\",\"jsonPath\":\"bytes\"}" +
			"]}");

		var source = $".target {targetName}\n.asset_manifest \"assets.json\"\n.asset \"tile.png\", \"chr\", \"gba8\", 8, 1, 1";
		var code = Compile(source, sourcePath, target);

		AssertStartsWith(code, [1, 2, 3, 255, 0]);
	}

	private static byte[] Compile(string source, string filename, TargetArchitecture target) {
		var lexer = new Core.Lexer.Lexer(source, filename);
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(target);
		analyzer.Analyze(program);
		Assert.False(analyzer.HasErrors, $"Semantic errors: {string.Join("; ", analyzer.Errors.Select(e => e.Message))}");

		var generator = new CodeGenerator(analyzer, target);
		var code = generator.Generate(program);
		Assert.False(generator.HasErrors, $"Codegen errors: {string.Join("; ", generator.Errors.Select(e => e.Message))}");
		return code;
	}

	private static void AssertStartsWith(byte[] output, byte[] expectedPrefix) {
		Assert.True(output.Length >= expectedPrefix.Length,
			$"Output length {output.Length} is smaller than expected prefix length {expectedPrefix.Length}.");

		for (var i = 0; i < expectedPrefix.Length; i++) {
			Assert.Equal(expectedPrefix[i], output[i]);
		}
	}

	private static byte[] CreateSolidPng(int width, int height, byte gray) {
		using var image = new Image<Rgba32>(width, height);
		for (var y = 0; y < height; y++) {
			for (var x = 0; x < width; x++) {
				image[x, y] = new Rgba32(gray, gray, gray, 255);
			}
		}

		using var ms = new MemoryStream();
		image.Save(ms, new PngEncoder());
		return ms.ToArray();
	}

	private sealed class TempDirectory : IDisposable {
		public string Path { get; }

		public TempDirectory() {
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "poppy-tests-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Path);
		}

		public void Dispose() {
			try {
				Directory.Delete(Path, recursive: true);
			} catch {
				// Ignore cleanup failures in test temp directories.
			}
		}
	}
}
