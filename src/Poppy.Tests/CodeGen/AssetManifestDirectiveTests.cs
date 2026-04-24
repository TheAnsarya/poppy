using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Poppy.Tests.CodeGen;

public class AssetManifestDirectiveTests {
	public static IEnumerable<object[]> AssetManifestTargets() {
		yield return [TargetArchitecture.MOS6502, "nes"];
		yield return [TargetArchitecture.MOS6507, "atari2600"];
		yield return [TargetArchitecture.SM83, "gameboy"];
		yield return [TargetArchitecture.M68000, "genesis"];
		yield return [TargetArchitecture.F8, "channelf"];
	}

	private static (byte[] Code, CodeGenerator Generator) Compile(string source, string filename, TargetArchitecture target) {
		var lexer = new Core.Lexer.Lexer(source, filename);
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(target);
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, target);
		var code = generator.Generate(program);
		return (code, generator);
	}

	[Theory]
	[MemberData(nameof(AssetManifestTargets))]
	public void AssetManifest_BinaryAndJson_EmitsExpectedBytesAcrossTargets(TargetArchitecture target, string targetName) {
		using var temp = new TempDirectory();
		var dataPath = Path.Combine(temp.Path, "data.bin");
		var jsonPath = Path.Combine(temp.Path, "numbers.json");
		var manifestPath = Path.Combine(temp.Path, "assets.json");
		var sourcePath = Path.Combine(temp.Path, "main.pasm");

		File.WriteAllBytes(dataPath, [1, 2, 3, 4]);
		File.WriteAllText(jsonPath, "{\"bytes\":[5,6,255]}");
		File.WriteAllText(manifestPath,
			"{\"assets\":[" +
			"{\"type\":\"binary\",\"path\":\"data.bin\",\"offset\":1,\"length\":2}," +
			"{\"type\":\"json-u8\",\"path\":\"numbers.json\",\"jsonPath\":\"bytes\"}" +
			"]}");

		var source = $".target {targetName}\n.asset_manifest \"assets.json\"";
		var (code, gen) = Compile(source, sourcePath, target);

		Assert.False(gen.HasErrors);
		AssertAssetBytesPresentInOrder(code, [2, 3, 5, 6, 255]);
	}

	[Fact]
	public void AssetDirective_ChrPng_EmitsChrBytes() {
		using var temp = new TempDirectory();
		var pngPath = Path.Combine(temp.Path, "pixel.png");
		var sourcePath = Path.Combine(temp.Path, "main.pasm");

		File.WriteAllBytes(pngPath, CreatePng(1, 1, 200));

		var source = ".target nes\n.asset \"pixel.png\", \"chr\", \"gba8\", 8, 1, 1";
		var (code, gen) = Compile(source, sourcePath, TargetArchitecture.MOS6502);

		Assert.False(gen.HasErrors);
		Assert.Single(code);
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

	private static byte[] CreatePng(int width, int height, byte grayValue) {
		using var image = new Image<Rgba32>(width, height);
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				image[x, y] = new Rgba32(grayValue, grayValue, grayValue, 255);
			}
		}

		using var ms = new MemoryStream();
		image.Save(ms, new PngEncoder());
		return ms.ToArray();
	}

	private static void AssertAssetBytesPresentInOrder(byte[] output, byte[] expected) {
		for (var start = 0; start <= output.Length - expected.Length; start++) {
			var match = true;
			for (var i = 0; i < expected.Length; i++) {
				if (output[start + i] != expected[i]) {
					match = false;
					break;
				}
			}

			if (match) {
				return;
			}
		}

		Assert.Fail($"Expected asset byte sequence [{string.Join(", ", expected)}] was not found in output of length {output.Length}.");
	}
}
