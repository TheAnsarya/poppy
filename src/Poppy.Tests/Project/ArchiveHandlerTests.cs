using Poppy.Core.Project;
using Xunit;

namespace Poppy.Tests.Project;

/// <summary>
/// Tests for ArchiveHandler packing and unpacking functionality.
/// </summary>
public class ArchiveHandlerTests : IDisposable {
	private readonly string _tempDir;
	private readonly string _projectDir;

	public ArchiveHandlerTests() {
		_tempDir = Path.Combine(Path.GetTempPath(), $"poppy-test-{Guid.NewGuid()}");
		_projectDir = Path.Combine(_tempDir, "test-project");
		Directory.CreateDirectory(_projectDir);
	}

	public void Dispose() {
		if (Directory.Exists(_tempDir)) {
			Directory.Delete(_tempDir, true);
		}
	}

	[Fact]
	public void Pack_ValidProject_CreatesArchive() {
		// Setup test project
		CreateTestProject();

		// Pack archive
		var archivePath = ArchiveHandler.Pack(_projectDir);

		Assert.True(File.Exists(archivePath));
		Assert.EndsWith(".poppy", archivePath);
	}

	[Fact]
	public void Pack_NonExistentDirectory_ThrowsException() {
		Assert.Throws<DirectoryNotFoundException>(() =>
			ArchiveHandler.Pack("nonexistent"));
	}

	[Fact]
	public void Pack_MissingManifest_ThrowsException() {
		Directory.CreateDirectory(_projectDir);
		// No poppy.json file

		Assert.Throws<FileNotFoundException>(() =>
			ArchiveHandler.Pack(_projectDir));
	}

	[Fact]
	public void Pack_InvalidManifest_ThrowsException() {
		Directory.CreateDirectory(_projectDir);
		
		// Create invalid manifest
		var manifest = new ProjectManifest {
			Name = "",  // Invalid
			Version = "1.0.0",
			Platform = "nes"
		};
		ManifestSerializer.SaveToFile(manifest, Path.Combine(_projectDir, "poppy.json"));

		Assert.Throws<InvalidOperationException>(() =>
			ArchiveHandler.Pack(_projectDir));
	}

	[Fact]
	public void Pack_WithCustomOutput_CreatesAtSpecifiedPath() {
		CreateTestProject();
		var customPath = Path.Combine(_tempDir, "custom.poppy");

		var archivePath = ArchiveHandler.Pack(_projectDir, new ArchiveHandler.PackOptions {
			OutputPath = customPath
		});

		Assert.Equal(customPath, archivePath);
		Assert.True(File.Exists(customPath));
	}

	[Fact]
	public void Pack_IncludesSourceFiles() {
		CreateTestProject();
		var archivePath = ArchiveHandler.Pack(_projectDir);

		using var archive = System.IO.Compression.ZipFile.OpenRead(archivePath);
		var sourceEntry = archive.GetEntry("src/main.pasm");

		Assert.NotNull(sourceEntry);
	}

	[Fact]
	public void Pack_IncludesMetadata() {
		CreateTestProject();
		var archivePath = ArchiveHandler.Pack(_projectDir);

		using var archive = System.IO.Compression.ZipFile.OpenRead(archivePath);
		
		Assert.NotNull(archive.GetEntry(".poppy/version.txt"));
		Assert.NotNull(archive.GetEntry(".poppy/checksums.txt"));
		Assert.NotNull(archive.GetEntry(".poppy/build-info.json"));
	}

	[Fact]
	public void Pack_ExcludesBuildDirectory_ByDefault() {
		CreateTestProject(includeBuild: true);
		var archivePath = ArchiveHandler.Pack(_projectDir);

		using var archive = System.IO.Compression.ZipFile.OpenRead(archivePath);
		var buildEntry = archive.GetEntry("build/game.nes");

		Assert.Null(buildEntry);  // Should be excluded by default
	}

	[Fact]
	public void Pack_IncludesBuildDirectory_WhenRequested() {
		CreateTestProject(includeBuild: true);
		var archivePath = ArchiveHandler.Pack(_projectDir, new ArchiveHandler.PackOptions {
			IncludeBuild = true
		});

		using var archive = System.IO.Compression.ZipFile.OpenRead(archivePath);
		var buildEntry = archive.GetEntry("build/game.nes");

		Assert.NotNull(buildEntry);
	}

	[Fact]
	public void Unpack_ValidArchive_ExtractsFiles() {
		CreateTestProject();
		var archivePath = ArchiveHandler.Pack(_projectDir);
		var extractDir = Path.Combine(_tempDir, "extracted");

		var result = ArchiveHandler.Unpack(archivePath, new ArchiveHandler.UnpackOptions {
			TargetDirectory = extractDir
		});

		Assert.Equal(extractDir, result);
		Assert.True(Directory.Exists(extractDir));
		Assert.True(File.Exists(Path.Combine(extractDir, "poppy.json")));
		Assert.True(File.Exists(Path.Combine(extractDir, "src", "main.pasm")));
	}

	[Fact]
	public void Unpack_NonExistentArchive_ThrowsException() {
		Assert.Throws<FileNotFoundException>(() =>
			ArchiveHandler.Unpack("nonexistent.poppy"));
	}

	[Fact]
	public void Unpack_ExistingDirectory_ThrowsWithoutOverwrite() {
		CreateTestProject();
		var archivePath = ArchiveHandler.Pack(_projectDir);
		var extractDir = Path.Combine(_tempDir, "extracted");
		Directory.CreateDirectory(extractDir);

		Assert.Throws<InvalidOperationException>(() =>
			ArchiveHandler.Unpack(archivePath, new ArchiveHandler.UnpackOptions {
				TargetDirectory = extractDir,
				Overwrite = false
			}));
	}

	[Fact]
	public void Unpack_ExistingDirectory_OverwritesWhenRequested() {
		CreateTestProject();
		var archivePath = ArchiveHandler.Pack(_projectDir);
		var extractDir = Path.Combine(_tempDir, "extracted");
		Directory.CreateDirectory(extractDir);
		File.WriteAllText(Path.Combine(extractDir, "dummy.txt"), "test");

		var result = ArchiveHandler.Unpack(archivePath, new ArchiveHandler.UnpackOptions {
			TargetDirectory = extractDir,
			Overwrite = true
		});

		Assert.True(Directory.Exists(extractDir));
		Assert.False(File.Exists(Path.Combine(extractDir, "dummy.txt")));  // Old file removed
		Assert.True(File.Exists(Path.Combine(extractDir, "poppy.json")));
	}

	[Fact]
	public void Unpack_ValidatesManifest_ByDefault() {
		CreateTestProject();
		var archivePath = ArchiveHandler.Pack(_projectDir);
		var extractDir = Path.Combine(_tempDir, "extracted");

		// Should not throw
		var exception = Record.Exception(() =>
			ArchiveHandler.Unpack(archivePath, new ArchiveHandler.UnpackOptions {
				TargetDirectory = extractDir
			}));

		Assert.Null(exception);
	}

	[Fact]
	public void Validate_ValidArchive_ReturnsNoErrors() {
		CreateTestProject();
		var archivePath = ArchiveHandler.Pack(_projectDir);

		var errors = ArchiveHandler.Validate(archivePath);

		Assert.Empty(errors);
	}

	[Fact]
	public void Validate_NonExistentArchive_ReturnsError() {
		var errors = ArchiveHandler.Validate("nonexistent.poppy");

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("not found"));
	}

	[Fact]
	public void Validate_MissingManifest_ReturnsError() {
		var archivePath = Path.Combine(_tempDir, "invalid.poppy");
		
		// Create empty archive
		using (var archive = System.IO.Compression.ZipFile.Open(archivePath, System.IO.Compression.ZipArchiveMode.Create)) {
			// Empty
		}

		var errors = ArchiveHandler.Validate(archivePath);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("poppy.json"));
	}

	[Fact]
	public void RoundTrip_PreservesAllFiles() {
		CreateTestProject();
		var archivePath = ArchiveHandler.Pack(_projectDir);
		var extractDir = Path.Combine(_tempDir, "extracted");

		ArchiveHandler.Unpack(archivePath, new ArchiveHandler.UnpackOptions {
			TargetDirectory = extractDir
		});

		// Compare files
		var originalManifest = ManifestSerializer.LoadFromFile(Path.Combine(_projectDir, "poppy.json"));
		var extractedManifest = ManifestSerializer.LoadFromFile(Path.Combine(extractDir, "poppy.json"));

		Assert.Equal(originalManifest.Name, extractedManifest.Name);
		Assert.Equal(originalManifest.Version, extractedManifest.Version);
		Assert.Equal(originalManifest.Platform, extractedManifest.Platform);

		// Check source files
		var originalSource = File.ReadAllText(Path.Combine(_projectDir, "src", "main.pasm"));
		var extractedSource = File.ReadAllText(Path.Combine(extractDir, "src", "main.pasm"));
		Assert.Equal(originalSource, extractedSource);
	}

	/// <summary>
	/// Creates a test project structure.
	/// </summary>
	private void CreateTestProject(bool includeBuild = false) {
		// Create manifest
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "nes",
			Entry = "src/main.pasm"
		};
		ManifestSerializer.SaveToFile(manifest, Path.Combine(_projectDir, "poppy.json"));

		// Create source directory and files
		var srcDir = Path.Combine(_projectDir, "src");
		Directory.CreateDirectory(srcDir);
		File.WriteAllText(Path.Combine(srcDir, "main.pasm"), ".target nes\n.org $8000\nlda #$00\n");

		// Create include directory
		var includeDir = Path.Combine(_projectDir, "include");
		Directory.CreateDirectory(includeDir);
		File.WriteAllText(Path.Combine(includeDir, "constants.pasm"), "SCREEN_WIDTH = 256\n");

		// Create README
		File.WriteAllText(Path.Combine(_projectDir, "README.md"), "# Test Game\n");

		// Optionally create build output
		if (includeBuild) {
			var buildDir = Path.Combine(_projectDir, "build");
			Directory.CreateDirectory(buildDir);
			File.WriteAllBytes(Path.Combine(buildDir, "game.nes"), new byte[] { 0x4e, 0x45, 0x53, 0x1a });
		}
	}
}
