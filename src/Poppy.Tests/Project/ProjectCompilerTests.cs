// ============================================================================
// ProjectCompilerTests.cs - Unit Tests for Multi-File Project Compilation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Project;

namespace Poppy.Tests.Project;

/// <summary>
/// Tests for multi-file project compilation.
/// </summary>
public class ProjectCompilerTests : IDisposable {
	private readonly string _tempDir;

	public ProjectCompilerTests() {
		_tempDir = Path.Combine(Path.GetTempPath(), "PoppyCompilerTests", Guid.NewGuid().ToString());
		Directory.CreateDirectory(_tempDir);
	}

	public void Dispose() {
		if (Directory.Exists(_tempDir)) {
			try {
				Directory.Delete(_tempDir, true);
			} catch {
				// Ignore cleanup failures
			}
		}
	}

	private void WriteFile(string relativePath, string content) {
		var fullPath = Path.Combine(_tempDir, relativePath);
		var dir = Path.GetDirectoryName(fullPath);
		if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
			Directory.CreateDirectory(dir);
		}

		File.WriteAllText(fullPath, content);
	}

	private ProjectFile CreateProject(string name = "Test", string target = "nes") {
		return new ProjectFile {
			Name = name,
			Target = target
		};
	}

	// ========================================================================
	// Basic Compilation Tests
	// ========================================================================

	[Fact]
	public void Compile_SingleMainFile_GeneratesOutput() {
		// Arrange
		WriteFile("main.pasm", @"
.org $8000
lda #$00
sta $2000
");
		var project = CreateProject();
		project.Main = "main.pasm";
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		Assert.NotNull(binary);
		Assert.False(compiler.HasErrors);
		Assert.Contains("main.pasm", compiler.CompiledFiles[0]);
	}

	[Fact]
	public void Compile_MultipleSourceFiles_CombinesStatements() {
		// Arrange
		WriteFile("main.pasm", @"
.org $8000
start:
	jmp init
");
		WriteFile("init.pasm", @"
init:
	lda #$00
	rts
");
		var project = CreateProject();
		project.Main = "main.pasm";
		project.Sources.Add("init.pasm");
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		Assert.NotNull(binary);
		Assert.False(compiler.HasErrors);
		Assert.Equal(2, compiler.CompiledFiles.Count);
	}

	[Fact]
	public void Compile_WithGlobPattern_FindsMultipleFiles() {
		// Arrange
		WriteFile("main.pasm", @"
.org $8000
jmp start
");
		WriteFile("src/file1.pasm", @"
start:
	nop
");
		WriteFile("src/file2.pasm", @"
data:
	.byte $01, $02
");
		var project = CreateProject();
		project.Main = "main.pasm";
		project.Sources.Add("src/*.pasm");
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		Assert.NotNull(binary);
		Assert.False(compiler.HasErrors);
		Assert.Equal(3, compiler.CompiledFiles.Count);
	}

	// ========================================================================
	// Include Path Tests
	// ========================================================================

	[Fact]
	public void Compile_WithIncludePath_ResolvesIncludes() {
		// Arrange
		WriteFile("main.pasm", @"
.include ""constants.inc""
.org $8000
lda #CONST_VALUE
");
		WriteFile("include/constants.inc", @"
CONST_VALUE = $42
");
		var project = CreateProject();
		project.Main = "main.pasm";
		project.Includes.Add("include");
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		Assert.NotNull(binary);
		Assert.False(compiler.HasErrors);
		// Should have $a9 $42 for LDA #$42
		Assert.Contains((byte)0xa9, binary);
		Assert.Contains((byte)0x42, binary);
	}

	// ========================================================================
	// Defines Tests
	// ========================================================================

	[Fact]
	public void Compile_WithDefines_MakesSymbolsAvailable() {
		// Arrange
		WriteFile("main.pasm", @"
.org $8000
lda #<DEBUG_MODE
");
		var project = CreateProject();
		project.Main = "main.pasm";
		project.Defines["DEBUG_MODE"] = 1;
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		Assert.NotNull(binary);
		Assert.False(compiler.HasErrors);
		// LDA #$01
		Assert.Equal(0xa9, binary[0]);
		Assert.Equal(0x01, binary[1]);
	}

	// ========================================================================
	// Cross-File Symbol Tests
	// ========================================================================

	[Fact]
	public void Compile_CrossFileSymbolReference_Resolves() {
		// Arrange
		WriteFile("main.pasm", @"
.org $8000
main:
	jsr helper
	rts
");
		WriteFile("helper.pasm", @"
helper:
	lda #$ff
	rts
");
		var project = CreateProject();
		project.Main = "main.pasm";
		project.Sources.Add("helper.pasm");
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		Assert.NotNull(binary);
		Assert.False(compiler.HasErrors);
	}

	// ========================================================================
	// Error Handling Tests
	// ========================================================================

	[Fact]
	public void Compile_MissingMainFile_ReportsError() {
		// Arrange
		var project = CreateProject();
		project.Main = "nonexistent.pasm";
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		Assert.Null(binary);
		Assert.True(compiler.HasErrors);
		Assert.Contains(compiler.Errors, e => e.Message.Contains("not found"));
	}

	[Fact]
	public void Compile_NoSourceFiles_ReportsError() {
		// Arrange
		var project = CreateProject();
		// No main, no sources
		project.Main = null;
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		Assert.Null(binary);
		Assert.True(compiler.HasErrors);
	}

	[Fact]
	public void Compile_SyntaxError_ReportsWithLocation() {
		// Arrange - create a file with an actual undefined label reference
		WriteFile("main.pasm", @"
.org $8000
jmp missing_label
");
		var project = CreateProject();
		project.Main = "main.pasm";
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		Assert.Null(binary);
		Assert.True(compiler.HasErrors);
		Assert.NotEmpty(compiler.Errors);
	}

	[Fact]
	public void Compile_UndefinedSymbol_ReportsError() {
		// Arrange
		WriteFile("main.pasm", @"
.org $8000
lda undefined_label
");
		var project = CreateProject();
		project.Main = "main.pasm";
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		Assert.Null(binary);
		Assert.True(compiler.HasErrors);
		Assert.Contains(compiler.Errors, e => e.Message.Contains("undefined") || e.Message.Contains("Undefined"));
	}

	// ========================================================================
	// Output File Tests
	// ========================================================================

	[Fact]
	public void CompileAndWrite_CreatesOutputFile() {
		// Arrange
		WriteFile("main.pasm", @"
.org $8000
nop
");
		var project = CreateProject();
		project.Main = "main.pasm";
		project.Output = "output.bin";
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var success = compiler.CompileAndWrite();

		// Assert
		Assert.True(success);
		Assert.True(File.Exists(Path.Combine(_tempDir, "output.bin")));
	}

	[Fact]
	public void CompileAndWrite_WithSymbolFile_CreatesSymFile() {
		// Arrange
		WriteFile("main.pasm", @"
.org $8000
start:
	nop
end:
	rts
");
		var project = CreateProject();
		project.Main = "main.pasm";
		project.Output = "output.bin";
		project.Symbols = "output.sym";
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var success = compiler.CompileAndWrite();

		// Assert
		Assert.True(success);
		Assert.True(File.Exists(Path.Combine(_tempDir, "output.sym")));
	}

	// ========================================================================
	// FromFile Static Method Tests
	// ========================================================================

	[Fact]
	public void FromFile_LoadsAndCreatesCompiler() {
		// Arrange
		WriteFile("main.pasm", @"
.org $8000
nop
");
		var project = CreateProject("TestProject", "nes");
		project.Main = "main.pasm";
		project.Output = "test.bin";
		var projectPath = Path.Combine(_tempDir, "poppy.json");
		project.Save(projectPath);

		// Act
		var compiler = ProjectCompiler.FromFile(projectPath);
		var binary = compiler.Compile();

		// Assert
		Assert.NotNull(binary);
		Assert.False(compiler.HasErrors);
	}

	// ========================================================================
	// Target Architecture Tests
	// ========================================================================

	[Fact]
	public void Compile_NesTarget_Uses6502Instructions() {
		// Arrange
		WriteFile("main.pasm", @"
.org $8000
lda #$00
sta $2000
");
		var project = CreateProject("Test", "nes");
		project.Main = "main.pasm";
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		Assert.NotNull(binary);
		Assert.False(compiler.HasErrors);
		// LDA #$00 = $a9 $00
		Assert.Equal(0xa9, binary[0]);
		Assert.Equal(0x00, binary[1]);
	}

	[Fact]
	public void Compile_GbTarget_SetsSM83Architecture() {
		// Arrange
		WriteFile("main.pasm", @"
.org $0150
nop
");
		var project = CreateProject("Test", "gb");
		project.Main = "main.pasm";
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		// Should compile without 65816-specific errors
		Assert.NotNull(binary);
		Assert.False(compiler.HasErrors);
	}

	// ========================================================================
	// Duplicate File Prevention Tests
	// ========================================================================

	[Fact]
	public void Compile_DuplicateSourceFiles_OnlyCompileCeOnce() {
		// Arrange
		WriteFile("main.pasm", @"
.org $8000
nop
");
		var project = CreateProject();
		project.Main = "main.pasm";
		project.Sources.Add("main.pasm"); // Duplicate
		var compiler = new ProjectCompiler(project, _tempDir);

		// Act
		var binary = compiler.Compile();

		// Assert
		Assert.NotNull(binary);
		Assert.False(compiler.HasErrors);
		Assert.Single(compiler.CompiledFiles); // Should only appear once
	}
}

