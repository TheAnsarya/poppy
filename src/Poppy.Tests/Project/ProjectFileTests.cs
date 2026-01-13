// ============================================================================
// ProjectFileTests.cs - Unit Tests for Project File Loading/Saving
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Project;
using Poppy.Core.Semantics;

namespace Poppy.Tests.Project;

/// <summary>
/// Tests for project file loading, saving, and validation.
/// </summary>
public class ProjectFileTests : IDisposable {
	private readonly string _tempDir;

	public ProjectFileTests() {
		_tempDir = Path.Combine(Path.GetTempPath(), "PoppyTests", Guid.NewGuid().ToString());
		Directory.CreateDirectory(_tempDir);
	}

	[Fact]
	public void Create_WithNameAndTarget_SetsProperties() {
		// Act
		var project = ProjectFile.Create("MyGame", "nes");

		// Assert
		Assert.Equal("MyGame", project.Name);
		Assert.Equal("nes", project.Target);
	}

	[Fact]
	public void Create_DefaultValues_AreCorrect() {
		// Act
		var project = ProjectFile.Create("Test", "nes");

		// Assert
		Assert.Equal("main.pasm", project.Main);
		Assert.Equal("Test.bin", project.Output);
		Assert.Empty(project.Sources);
		Assert.Empty(project.Includes);
		Assert.Empty(project.Defines);
		Assert.Null(project.Symbols);
		Assert.Null(project.Listing);
		Assert.Null(project.MapFile);
		Assert.False(project.AutoLabels);
	}

	[Fact]
	public void TargetArchitecture_Nes_Returns6502() {
		// Arrange
		var project = ProjectFile.Create("Test", "nes");

		// Act & Assert
		Assert.Equal(TargetArchitecture.MOS6502, project.TargetArchitecture);
	}

	[Fact]
	public void TargetArchitecture_Snes_Returns65816() {
		// Arrange
		var project = ProjectFile.Create("Test", "snes");

		// Act & Assert
		Assert.Equal(TargetArchitecture.WDC65816, project.TargetArchitecture);
	}

	[Fact]
	public void TargetArchitecture_Gb_ReturnsSM83() {
		// Arrange
		var project = ProjectFile.Create("Test", "gb");

		// Act & Assert
		Assert.Equal(TargetArchitecture.SM83, project.TargetArchitecture);
	}

	[Fact]
	public void TargetArchitecture_Unknown_DefaultsTo6502() {
		// Arrange
		var project = ProjectFile.Create("Test", "unknown");

		// Act & Assert
		Assert.Equal(TargetArchitecture.MOS6502, project.TargetArchitecture);
	}

	[Fact]
	public void TargetArchitecture_CaseInsensitive() {
		// Arrange
		var project1 = ProjectFile.Create("Test", "NES");
		var project2 = ProjectFile.Create("Test", "Snes");
		var project3 = ProjectFile.Create("Test", "GB");

		// Assert
		Assert.Equal(TargetArchitecture.MOS6502, project1.TargetArchitecture);
		Assert.Equal(TargetArchitecture.WDC65816, project2.TargetArchitecture);
		Assert.Equal(TargetArchitecture.SM83, project3.TargetArchitecture);
	}

	[Fact]
	public void Save_CreatesJsonFile() {
		// Arrange
		var project = ProjectFile.Create("TestGame", "nes");
		project.Output = "test.nes";
		project.Main = "main.pasm";
		var filePath = Path.Combine(_tempDir, "poppy.json");

		// Act
		project.Save(filePath);

		// Assert
		Assert.True(File.Exists(filePath));
	}

	[Fact]
	public void SaveAndLoad_RoundTrip_PreservesData() {
		// Arrange
		var project = ProjectFile.Create("TestGame", "snes");
		project.Version = "2.0.0";
		project.Output = "game.sfc";
		project.Main = "main.pasm";
		project.Sources.Add("src/*.pasm");
		project.Includes.Add("include/");
		project.Defines["DEBUG"] = 1;
		project.Symbols = "game.sym";
		project.Listing = "game.lst";
		project.MapFile = "game.map";
		project.AutoLabels = true;
		var filePath = Path.Combine(_tempDir, "test.json");

		// Act
		project.Save(filePath);
		var loaded = ProjectFile.Load(filePath);

		// Assert
		Assert.Equal("TestGame", loaded.Name);
		Assert.Equal("snes", loaded.Target);
		Assert.Equal("2.0.0", loaded.Version);
		Assert.Equal("game.sfc", loaded.Output);
		Assert.Equal("main.pasm", loaded.Main);
		Assert.Single(loaded.Sources);
		Assert.Contains("src/*.pasm", loaded.Sources);
		Assert.Single(loaded.Includes);
		Assert.Contains("include/", loaded.Includes);
		Assert.Single(loaded.Defines);
		Assert.Equal(1, loaded.Defines["DEBUG"]);
		Assert.Equal("game.sym", loaded.Symbols);
		Assert.Equal("game.lst", loaded.Listing);
		Assert.Equal("game.map", loaded.MapFile);
		Assert.True(loaded.AutoLabels);
	}

	[Fact]
	public void Load_WithComments_Succeeds() {
		// Arrange
		var json = """
		{
			// This is a comment
			"name": "CommentTest",
			"target": "nes"
		}
		""";
		var filePath = Path.Combine(_tempDir, "comments.json");
		File.WriteAllText(filePath, json);

		// Act
		var project = ProjectFile.Load(filePath);

		// Assert
		Assert.Equal("CommentTest", project.Name);
		Assert.Equal("nes", project.Target);
	}

	[Fact]
	public void Load_WithTrailingComma_Succeeds() {
		// Arrange
		var json = """
		{
			"name": "TrailingCommaTest",
			"target": "gb",
			"sources": [
				"src/main.pasm",
			],
		}
		""";
		var filePath = Path.Combine(_tempDir, "trailing.json");
		File.WriteAllText(filePath, json);

		// Act
		var project = ProjectFile.Load(filePath);

		// Assert
		Assert.Equal("TrailingCommaTest", project.Name);
		Assert.Single(project.Sources);
	}

	[Fact]
	public void Load_CaseInsensitivePropertyNames() {
		// Arrange
		var json = """
		{
			"Name": "CaseTest",
			"TARGET": "snes",
			"Sources": ["main.pasm"]
		}
		""";
		var filePath = Path.Combine(_tempDir, "case.json");
		File.WriteAllText(filePath, json);

		// Act
		var project = ProjectFile.Load(filePath);

		// Assert
		Assert.Equal("CaseTest", project.Name);
		Assert.Equal("snes", project.Target);
		Assert.Single(project.Sources);
	}

	[Fact]
	public void Load_NonExistentFile_Throws() {
		// Arrange
		var filePath = Path.Combine(_tempDir, "nonexistent.json");

		// Act & Assert
		Assert.Throws<FileNotFoundException>(() => ProjectFile.Load(filePath));
	}

	[Fact]
	public void Validate_EmptyName_ReturnsError() {
		// Arrange
		var project = new ProjectFile { Name = "", Target = "nes", Main = "main.pasm" };

		// Act
		var errors = project.Validate();

		// Assert
		Assert.Contains(errors, e => e.Contains("name", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void Validate_InvalidTarget_ReturnsError() {
		// Arrange
		var project = new ProjectFile { Name = "Test", Target = "invalid_target", Main = "main.pasm" };

		// Act
		var errors = project.Validate();

		// Assert
		Assert.Contains(errors, e => e.Contains("target", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void Validate_NoSources_NoMain_ReturnsError() {
		// Arrange
		var project = new ProjectFile { Name = "Test", Target = "nes" };
		// No sources and no main file

		// Act
		var errors = project.Validate();

		// Assert
		Assert.Contains(errors, e => e.Contains("main", StringComparison.OrdinalIgnoreCase) || e.Contains("sources", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void Validate_ValidProject_ReturnsNoErrors() {
		// Arrange
		var project = ProjectFile.Create("ValidGame", "nes");
		project.Main = "main.pasm";
		project.Output = "game.nes";

		// Act
		var errors = project.Validate();

		// Assert
		Assert.Empty(errors);
	}

	[Fact]
	public void Validate_WithSources_NoMainRequired() {
		// Arrange
		var project = new ProjectFile {
			Name = "Test",
			Target = "nes",
			Output = "game.nes"
		};
		project.Sources.Add("src/*.pasm");

		// Act
		var errors = project.Validate();

		// Assert
		Assert.Empty(errors);
	}

	[Fact]
	public void Load_DefineValues_ParsesCorrectly() {
		// Arrange
		var json = """
		{
			"name": "DefineTest",
			"target": "nes",
			"main": "main.pasm",
			"defines": {
				"BASE_ADDR": 32768,
				"MAX_VALUE": 65535
			}
		}
		""";
		var filePath = Path.Combine(_tempDir, "defines.json");
		File.WriteAllText(filePath, json);

		// Act
		var project = ProjectFile.Load(filePath);

		// Assert
		Assert.Equal(32768, project.Defines["BASE_ADDR"]); // $8000
		Assert.Equal(65535, project.Defines["MAX_VALUE"]); // $ffff
	}

	[Fact]
	public void MultipleDefines_AllPreserved() {
		// Arrange
		var project = ProjectFile.Create("Test", "nes");
		project.Defines["DEBUG"] = 1;
		project.Defines["VERSION"] = 10;
		project.Defines["FEATURE_A"] = 0;
		var filePath = Path.Combine(_tempDir, "defines.json");

		// Act
		project.Save(filePath);
		var loaded = ProjectFile.Load(filePath);

		// Assert
		Assert.Equal(3, loaded.Defines.Count);
		Assert.Equal(1, loaded.Defines["DEBUG"]);
		Assert.Equal(10, loaded.Defines["VERSION"]);
		Assert.Equal(0, loaded.Defines["FEATURE_A"]);
	}

	[Fact]
	public void TargetArchitecture_AlternateNames_Work() {
		// Arrange & Act
		var p6502 = new ProjectFile { Target = "6502" };
		var p65816 = new ProjectFile { Target = "65816" };
		var pGameboy = new ProjectFile { Target = "gameboy" };
		var pSM83 = new ProjectFile { Target = "sm83" };

		// Assert
		Assert.Equal(TargetArchitecture.MOS6502, p6502.TargetArchitecture);
		Assert.Equal(TargetArchitecture.WDC65816, p65816.TargetArchitecture);
		Assert.Equal(TargetArchitecture.SM83, pGameboy.TargetArchitecture);
		Assert.Equal(TargetArchitecture.SM83, pSM83.TargetArchitecture);
	}

	public void Dispose() {
		try {
			if (Directory.Exists(_tempDir)) {
				Directory.Delete(_tempDir, true);
			}
		} catch {
			// Ignore cleanup errors
		}
	}
}
