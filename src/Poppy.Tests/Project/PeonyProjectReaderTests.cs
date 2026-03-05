// ============================================================================
// PeonyProjectReaderTests.cs - Unit Tests for Peony Project Config Reading
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text.Json;
using Poppy.Core.Project;

namespace Poppy.Tests.Project;

/// <summary>
/// Tests for reading peony.json project configuration files.
/// </summary>
public class PeonyProjectReaderTests : IDisposable {
	private readonly string _tempDir;

	public PeonyProjectReaderTests() {
		_tempDir = Path.Combine(Path.GetTempPath(), "PoppyTests", Guid.NewGuid().ToString());
		Directory.CreateDirectory(_tempDir);
	}

	private const string ValidPeonyJson = """
		{
			"version": "1.0",
			"platform": "nes",
			"rom": { "path": "rom/game.nes", "crc32": "d445f698", "size": 40976 },
			"metadata": { "cdl": "metadata/game.cdl", "pansy": "metadata/game.pansy" },
			"output": { "format": "poppy", "directory": "source/", "splitBanks": true },
			"source": { "nexenPack": "original-pack.nexen-pack.zip", "importDate": "2026-01-15T10:30:00Z" }
		}
		""";

	[Fact]
	public void LoadFromString_ValidJson_ParsesAllFields() {
		// Act
		var project = PeonyProjectReader.LoadFromString(ValidPeonyJson, _tempDir);

		// Assert
		Assert.Equal("1.0", project.Version);
		Assert.Equal("nes", project.Platform);
		Assert.NotNull(project.Rom);
		Assert.Equal("rom/game.nes", project.Rom.Path);
		Assert.Equal("d445f698", project.Rom.Crc32);
		Assert.Equal(40976, project.Rom.Size);
		Assert.NotNull(project.Metadata);
		Assert.Equal("metadata/game.cdl", project.Metadata.Cdl);
		Assert.Equal("metadata/game.pansy", project.Metadata.Pansy);
		Assert.NotNull(project.Output);
		Assert.Equal("poppy", project.Output.Format);
		Assert.Equal("source/", project.Output.Directory);
		Assert.True(project.Output.SplitBanks);
		Assert.NotNull(project.Source);
		Assert.Equal("original-pack.nexen-pack.zip", project.Source.NexenPack);
		Assert.Equal("2026-01-15T10:30:00Z", project.Source.ImportDate);
	}

	[Fact]
	public void LoadFromString_SetsProjectDirectory() {
		// Act
		var project = PeonyProjectReader.LoadFromString(ValidPeonyJson, _tempDir);

		// Assert
		Assert.Equal(_tempDir, project.ProjectDirectory);
	}

	[Fact]
	public void LoadFromString_MinimalJson_ParsesRequired() {
		// Arrange
		const string json = """
			{
				"version": "1.0",
				"platform": "snes",
				"rom": { "path": "rom/game.sfc" }
			}
			""";

		// Act
		var project = PeonyProjectReader.LoadFromString(json, _tempDir);

		// Assert
		Assert.Equal("1.0", project.Version);
		Assert.Equal("snes", project.Platform);
		Assert.Equal("rom/game.sfc", project.Rom!.Path);
		Assert.Null(project.Metadata);
		Assert.Null(project.Output);
		Assert.Null(project.Source);
	}

	[Fact]
	public void Load_FromDirectory_FindsPeonyJson() {
		// Arrange
		File.WriteAllText(Path.Combine(_tempDir, "peony.json"), ValidPeonyJson);

		// Act
		var project = PeonyProjectReader.Load(_tempDir);

		// Assert
		Assert.Equal("1.0", project.Version);
		Assert.Equal("nes", project.Platform);
		Assert.Equal(_tempDir, project.ProjectDirectory);
	}

	[Fact]
	public void LoadFromJson_SpecificPath_Works() {
		// Arrange
		var jsonPath = Path.Combine(_tempDir, "custom-name.json");
		File.WriteAllText(jsonPath, ValidPeonyJson);

		// Act
		var project = PeonyProjectReader.LoadFromJson(jsonPath);

		// Assert
		Assert.Equal("1.0", project.Version);
		Assert.Equal(_tempDir, project.ProjectDirectory);
	}

	[Fact]
	public void Load_MissingFile_ThrowsFileNotFound() {
		// Act & Assert
		Assert.Throws<FileNotFoundException>(() => PeonyProjectReader.Load(_tempDir));
	}

	[Fact]
	public void LoadFromJson_MissingFile_ThrowsFileNotFound() {
		// Act & Assert
		var ex = Assert.Throws<FileNotFoundException>(
			() => PeonyProjectReader.LoadFromJson(Path.Combine(_tempDir, "nonexistent.json")));
		Assert.Contains("nonexistent.json", ex.Message);
	}

	[Fact]
	public void LoadFromString_InvalidJson_ThrowsJsonException() {
		// Act & Assert
		Assert.Throws<JsonException>(() => PeonyProjectReader.LoadFromString("not json", _tempDir));
	}

	[Fact]
	public void LoadFromString_EmptyObject_ReturnsDefaults() {
		// Act
		var project = PeonyProjectReader.LoadFromString("{}", _tempDir);

		// Assert
		Assert.Null(project.Version);
		Assert.Null(project.Platform);
		Assert.Null(project.Rom);
	}

	[Fact]
	public void TryLoad_ValidProject_ReturnsTrue() {
		// Arrange
		File.WriteAllText(Path.Combine(_tempDir, "peony.json"), ValidPeonyJson);

		// Act
		var result = PeonyProjectReader.TryLoad(_tempDir, out var project, out var error);

		// Assert
		Assert.True(result);
		Assert.NotNull(project);
		Assert.Null(error);
		Assert.Equal("nes", project.Platform);
	}

	[Fact]
	public void TryLoad_MissingFile_ReturnsFalse() {
		// Act
		var result = PeonyProjectReader.TryLoad(_tempDir, out var project, out var error);

		// Assert
		Assert.False(result);
		Assert.Null(project);
		Assert.NotNull(error);
	}

	[Fact]
	public void ResolvePath_RelativePath_ResolvesAgainstProjectDir() {
		// Arrange
		var project = PeonyProjectReader.LoadFromString(ValidPeonyJson, _tempDir);

		// Act
		var resolved = project.ResolvePath("rom/game.nes");

		// Assert
		Assert.Equal(Path.GetFullPath(Path.Combine(_tempDir, "rom/game.nes")), resolved);
	}

	[Fact]
	public void ResolvePath_AbsolutePath_ReturnsUnchanged() {
		// Arrange
		var project = PeonyProjectReader.LoadFromString(ValidPeonyJson, _tempDir);
		var absPath = Path.GetFullPath(@"C:\some\absolute\path.nes");

		// Act
		var resolved = project.ResolvePath(absPath);

		// Assert
		Assert.Equal(absPath, resolved);
	}

	[Fact]
	public void ResolvePath_EmptyPath_ReturnsProjectDirectory() {
		// Arrange
		var project = PeonyProjectReader.LoadFromString(ValidPeonyJson, _tempDir);

		// Act
		var resolved = project.ResolvePath("");

		// Assert
		Assert.Equal(_tempDir, resolved);
	}

	[Fact]
	public void ResolvedRomPath_WhenSet_ResolvesCorrectly() {
		// Arrange
		var project = PeonyProjectReader.LoadFromString(ValidPeonyJson, _tempDir);

		// Act & Assert
		Assert.Equal(Path.GetFullPath(Path.Combine(_tempDir, "rom/game.nes")), project.ResolvedRomPath);
	}

	[Fact]
	public void ResolvedPansyPath_WhenSet_ResolvesCorrectly() {
		// Arrange
		var project = PeonyProjectReader.LoadFromString(ValidPeonyJson, _tempDir);

		// Act & Assert
		Assert.Equal(Path.GetFullPath(Path.Combine(_tempDir, "metadata/game.pansy")), project.ResolvedPansyPath);
	}

	[Fact]
	public void ResolvedCdlPath_WhenSet_ResolvesCorrectly() {
		// Arrange
		var project = PeonyProjectReader.LoadFromString(ValidPeonyJson, _tempDir);

		// Act & Assert
		Assert.Equal(Path.GetFullPath(Path.Combine(_tempDir, "metadata/game.cdl")), project.ResolvedCdlPath);
	}

	[Fact]
	public void ResolvedOutputDirectory_WhenSet_ResolvesCorrectly() {
		// Arrange
		var project = PeonyProjectReader.LoadFromString(ValidPeonyJson, _tempDir);

		// Act & Assert
		Assert.Equal(Path.GetFullPath(Path.Combine(_tempDir, "source/")), project.ResolvedOutputDirectory);
	}

	[Fact]
	public void ResolvedPaths_WhenNull_ReturnsNull() {
		// Arrange
		var project = PeonyProjectReader.LoadFromString("{}", _tempDir);

		// Act & Assert
		Assert.Null(project.ResolvedRomPath);
		Assert.Null(project.ResolvedPansyPath);
		Assert.Null(project.ResolvedCdlPath);
		Assert.Null(project.ResolvedOutputDirectory);
	}

	[Fact]
	public void Validate_ValidProject_ReturnsNoErrors() {
		// Arrange
		var project = PeonyProjectReader.LoadFromString(ValidPeonyJson, _tempDir);

		// Act
		var errors = project.Validate();

		// Assert
		Assert.Empty(errors);
	}

	[Fact]
	public void Validate_MissingVersion_ReturnsError() {
		// Arrange
		const string json = """{ "platform": "nes", "rom": { "path": "game.nes" } }""";
		var project = PeonyProjectReader.LoadFromString(json, _tempDir);

		// Act
		var errors = project.Validate();

		// Assert
		Assert.Contains(errors, e => e.Contains("version"));
	}

	[Fact]
	public void Validate_MissingPlatform_ReturnsError() {
		// Arrange
		const string json = """{ "version": "1.0", "rom": { "path": "game.nes" } }""";
		var project = PeonyProjectReader.LoadFromString(json, _tempDir);

		// Act
		var errors = project.Validate();

		// Assert
		Assert.Contains(errors, e => e.Contains("platform"));
	}

	[Fact]
	public void Validate_MissingRom_ReturnsError() {
		// Arrange
		const string json = """{ "version": "1.0", "platform": "nes" }""";
		var project = PeonyProjectReader.LoadFromString(json, _tempDir);

		// Act
		var errors = project.Validate();

		// Assert
		Assert.Contains(errors, e => e.Contains("rom"));
	}

	[Fact]
	public void Validate_MissingRomPath_ReturnsError() {
		// Arrange
		const string json = """{ "version": "1.0", "platform": "nes", "rom": { "crc32": "abc" } }""";
		var project = PeonyProjectReader.LoadFromString(json, _tempDir);

		// Act
		var errors = project.Validate();

		// Assert
		Assert.Contains(errors, e => e.Contains("rom.path"));
	}

	[Fact]
	public void Validate_EmptyObject_ReturnsMultipleErrors() {
		// Arrange
		var project = PeonyProjectReader.LoadFromString("{}", _tempDir);

		// Act
		var errors = project.Validate();

		// Assert
		Assert.True(errors.Count >= 3); // version, platform, rom
	}

	[Fact]
	public void LoadFromString_WithComments_ParsesSuccessfully() {
		// Arrange — JSON with comments (allowed by options)
		const string json = """
			{
				// Project version
				"version": "1.0",
				"platform": "gb",
				"rom": { "path": "game.gb" }
			}
			""";

		// Act
		var project = PeonyProjectReader.LoadFromString(json, _tempDir);

		// Assert
		Assert.Equal("1.0", project.Version);
		Assert.Equal("gb", project.Platform);
	}

	[Fact]
	public void LoadFromString_WithTrailingCommas_ParsesSuccessfully() {
		// Arrange
		const string json = """
			{
				"version": "1.0",
				"platform": "nes",
				"rom": { "path": "game.nes", },
			}
			""";

		// Act
		var project = PeonyProjectReader.LoadFromString(json, _tempDir);

		// Assert
		Assert.Equal("1.0", project.Version);
	}

	[Fact]
	public void LoadFromString_CaseInsensitive_ParsesCorrectly() {
		// Arrange
		const string json = """
			{
				"Version": "1.0",
				"Platform": "nes",
				"Rom": { "Path": "game.nes", "CRC32": "abcd1234" }
			}
			""";

		// Act
		var project = PeonyProjectReader.LoadFromString(json, _tempDir);

		// Assert
		Assert.Equal("1.0", project.Version);
		Assert.Equal("nes", project.Platform);
		Assert.Equal("game.nes", project.Rom!.Path);
	}

	[Fact]
	public void Load_Roundtrip_FileWriteAndRead() {
		// Arrange — write a peony.json, then read it back
		File.WriteAllText(Path.Combine(_tempDir, "peony.json"), ValidPeonyJson);

		// Act
		var project = PeonyProjectReader.Load(_tempDir);

		// Assert
		Assert.Equal("1.0", project.Version);
		Assert.Equal("nes", project.Platform);
		Assert.Equal("rom/game.nes", project.Rom!.Path);
		Assert.Equal("d445f698", project.Rom.Crc32);
		Assert.Equal(40976, project.Rom.Size);
		Assert.Equal("metadata/game.pansy", project.Metadata!.Pansy);
		Assert.Equal("metadata/game.cdl", project.Metadata.Cdl);
		Assert.True(project.Output!.SplitBanks);
		Assert.Equal("poppy", project.Output.Format);
		Assert.Equal("original-pack.nexen-pack.zip", project.Source!.NexenPack);
	}

	public void Dispose() {
		try {
			if (Directory.Exists(_tempDir))
				Directory.Delete(_tempDir, true);
		} catch {
			// Cleanup is best-effort
		}
	}
}
