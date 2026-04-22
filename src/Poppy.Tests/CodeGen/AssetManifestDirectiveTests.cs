using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Poppy.Tests.CodeGen;

public class AssetManifestDirectiveTests {
	private static (byte[] Code, CodeGenerator Generator) Compile(string source, string filename) {
		var lexer = new Core.Lexer.Lexer(source, filename);
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6502);
		var code = generator.Generate(program);
		return (code, generator);
	}

	[Fact]
	public void AssetManifest_BinaryAndJson_EmitsExpectedBytes() {
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

		var source = ".asset_manifest \"assets.json\"";
		var (code, gen) = Compile(source, sourcePath);

		Assert.False(gen.HasErrors);
		Assert.Equal(new byte[] { 2, 3, 5, 6, 255 }, code);
	}

	[Fact]
	public void AssetDirective_ChrPng_EmitsChrBytes() {
		using var temp = new TempDirectory();
		var pngPath = Path.Combine(temp.Path, "pixel.png");
		var sourcePath = Path.Combine(temp.Path, "main.pasm");

		File.WriteAllBytes(pngPath, CreatePng(1, 1, 200));

		var source = ".asset \"pixel.png\", \"chr\", \"gba8\", 8, 1, 1";
		var (code, gen) = Compile(source, sourcePath);

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
}
