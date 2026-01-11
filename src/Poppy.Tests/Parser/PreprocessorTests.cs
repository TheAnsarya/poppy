// ============================================================================
// PreprocessorTests.cs - Unit Tests for Preprocessor
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Xunit;

namespace Poppy.Tests.Parser;

/// <summary>
/// Unit tests for the Preprocessor class.
/// </summary>
public sealed class PreprocessorTests : IDisposable {
	private readonly string _tempDir;

	public PreprocessorTests() {
		_tempDir = Path.Combine(Path.GetTempPath(), $"poppy_test_{Guid.NewGuid():N}");
		Directory.CreateDirectory(_tempDir);
	}

	public void Dispose() {
		if (Directory.Exists(_tempDir)) {
			Directory.Delete(_tempDir, recursive: true);
		}
	}

	private string CreateTempFile(string filename, string content) {
		var path = Path.Combine(_tempDir, filename);
		var dir = Path.GetDirectoryName(path);
		if (dir is not null && !Directory.Exists(dir)) {
			Directory.CreateDirectory(dir);
		}
		File.WriteAllText(path, content);
		return path;
	}

	[Fact]
	public void Process_SimpleSourceWithoutIncludes_ReturnsAllTokens() {
		var source = "lda #$00\nsta $2000";
		var filePath = CreateTempFile("test.pasm", source);

		var preprocessor = new Preprocessor();
		var tokens = preprocessor.Process(source, filePath);

		Assert.False(preprocessor.HasErrors);
		Assert.Contains(tokens, t => t.Type == TokenType.Mnemonic && t.Text == "lda");
		Assert.Contains(tokens, t => t.Type == TokenType.Mnemonic && t.Text == "sta");
	}

	[Fact]
	public void Process_IncludeDirective_IncludesFileContent() {
		var includeContent = "CONST = $42\n";
		var includePath = CreateTempFile("constants.inc", includeContent);
		var mainContent = $".include \"constants.inc\"\nlda #CONST";
		var mainPath = CreateTempFile("main.pasm", mainContent);

		var preprocessor = new Preprocessor();
		var tokens = preprocessor.Process(mainContent, mainPath);

		Assert.False(preprocessor.HasErrors);
		Assert.Contains(tokens, t => t.Type == TokenType.Identifier && t.Text == "CONST");
		Assert.Contains(tokens, t => t.Type == TokenType.Mnemonic && t.Text == "lda");
	}

	[Fact]
	public void Process_NestedIncludes_ProcessesRecursively() {
		var level2Content = "inner_label:\n";
		CreateTempFile("level2.inc", level2Content);

		var level1Content = ".include \"level2.inc\"\nouter_label:\n";
		CreateTempFile("level1.inc", level1Content);

		var mainContent = ".include \"level1.inc\"\nmain_label:\n";
		var mainPath = CreateTempFile("main.pasm", mainContent);

		var preprocessor = new Preprocessor();
		var tokens = preprocessor.Process(mainContent, mainPath);

		Assert.False(preprocessor.HasErrors);
		Assert.Contains(tokens, t => t.Text == "inner_label");
		Assert.Contains(tokens, t => t.Text == "outer_label");
		Assert.Contains(tokens, t => t.Text == "main_label");
	}

	[Fact]
	public void Process_CircularInclude_ReportsError() {
		var file1Content = ".include \"file2.inc\"\n";
		CreateTempFile("file1.inc", file1Content);

		var file2Content = ".include \"file1.inc\"\n";
		CreateTempFile("file2.inc", file2Content);

		var mainContent = ".include \"file1.inc\"\n";
		var mainPath = CreateTempFile("main.pasm", mainContent);

		var preprocessor = new Preprocessor();
		var tokens = preprocessor.Process(mainContent, mainPath);

		Assert.True(preprocessor.HasErrors);
		Assert.Contains(preprocessor.Errors, e => e.Message.Contains("Circular"));
	}

	[Fact]
	public void Process_MissingIncludeFile_ReportsError() {
		var mainContent = ".include \"nonexistent.inc\"\n";
		var mainPath = CreateTempFile("main.pasm", mainContent);

		var preprocessor = new Preprocessor();
		var tokens = preprocessor.Process(mainContent, mainPath);

		Assert.True(preprocessor.HasErrors);
		Assert.Contains(preprocessor.Errors, e => e.Message.Contains("not found"));
	}

	[Fact]
	public void Process_IncludeWithIncludePath_FindsFile() {
		// Create include file in a separate directory
		var includeDir = Path.Combine(_tempDir, "includes");
		Directory.CreateDirectory(includeDir);
		var includeContent = "INCLUDED = 1\n";
		File.WriteAllText(Path.Combine(includeDir, "myinc.inc"), includeContent);

		var mainContent = ".include \"myinc.inc\"\n";
		var mainPath = CreateTempFile("main.pasm", mainContent);

		var preprocessor = new Preprocessor([includeDir]);
		var tokens = preprocessor.Process(mainContent, mainPath);

		Assert.False(preprocessor.HasErrors);
		Assert.Contains(tokens, t => t.Text == "INCLUDED");
	}

	[Fact]
	public void Process_IncludePreservesSourceLocations() {
		var includeContent = "included_label:\n";
		var includePath = CreateTempFile("inc.pasm", includeContent);
		var mainContent = ".include \"inc.pasm\"\nmain_label:\n";
		var mainPath = CreateTempFile("main.pasm", mainContent);

		var preprocessor = new Preprocessor();
		var tokens = preprocessor.Process(mainContent, mainPath);

		Assert.False(preprocessor.HasErrors);

		// Find the included label token and check its location
		var includedLabel = tokens.First(t => t.Text == "included_label");
		Assert.Contains("inc.pasm", includedLabel.Location.FilePath);

		// Find the main label and check its location
		var mainLabel = tokens.First(t => t.Text == "main_label");
		Assert.Contains("main.pasm", mainLabel.Location.FilePath);
	}

	[Fact]
	public void Process_MaxIncludeDepth_ReportsError() {
		// Create deep nesting
		for (int i = 0; i < 5; i++) {
			var content = i < 4 ? $".include \"level{i + 1}.inc\"\n" : "final:\n";
			CreateTempFile($"level{i}.inc", content);
		}

		var mainContent = ".include \"level0.inc\"\n";
		var mainPath = CreateTempFile("main.pasm", mainContent);

		// Set max depth to 3
		var preprocessor = new Preprocessor(maxIncludeDepth: 3);
		var tokens = preprocessor.Process(mainContent, mainPath);

		Assert.True(preprocessor.HasErrors);
		Assert.Contains(preprocessor.Errors, e => e.Message.Contains("depth"));
	}

	[Fact]
	public void Process_IncbinDirective_PassedThrough() {
		var mainContent = ".incbin \"data.bin\"\n";
		var mainPath = CreateTempFile("main.pasm", mainContent);

		var preprocessor = new Preprocessor();
		var tokens = preprocessor.Process(mainContent, mainPath);

		Assert.False(preprocessor.HasErrors);
		Assert.Contains(tokens, t => t.Type == TokenType.Directive && t.Text == ".incbin");
	}
}

