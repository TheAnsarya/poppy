using Poppy.Core.Project;
using Xunit;

namespace Poppy.Tests.Project;

/// <summary>
/// Tests for ProjectManifest serialization and deserialization.
/// </summary>
public class ManifestSerializerTests {
	[Fact]
	public void Serialize_MinimalManifest_ProducesValidJson() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "nes"
		};

		var json = ManifestSerializer.Serialize(manifest);

		Assert.Contains("\"name\": \"test-game\"", json);
		Assert.Contains("\"version\": \"1.0.0\"", json);
		Assert.Contains("\"platform\": \"nes\"", json);
	}

	[Fact]
	public void Serialize_CompleteManifest_ProducesValidJson() {
		var manifest = new ProjectManifest {
			Schema = "https://poppy-compiler.org/schemas/project-v1.json",
			Name = "my-game",
			Version = "1.0.0",
			Description = "A retro platformer",
			Author = "Developer",
			License = "MIT",
			Platform = "nes",
			Entry = "src/main.pasm",
			Output = "build/game.nes",
			Compiler = new ManifestCompilerConfig {
				Version = "1.0.0",
				Target = "nes",
				Options = new ManifestCompilerOptions {
					Optimize = true,
					Debug = false,
					Warnings = "all"
				}
			},
			Build = new ManifestBuildConfig {
				IncludePaths = new List<string> { "include", "lib" },
				Defines = new Dictionary<string, object> {
					{ "DEBUG", false },
					{ "VERSION", "1.0.0" }
				},
				Scripts = new Dictionary<string, string> {
					{ "build", "poppy build" },
					{ "test", "poppy test" }
				}
			},
			Assets = new Dictionary<string, string> {
				{ "graphics", "assets/graphics" },
				{ "music", "assets/music" }
			},
			Dependencies = new Dictionary<string, string> {
				{ "poppy-stdlib", "^1.0.0" }
			},
			Metadata = new ManifestMetadata {
				Tags = new List<string> { "platformer", "nes" },
				Homepage = "https://example.com",
				Repository = "https://github.com/user/repo.git",
				Created = "2026-01-15T00:00:00Z",
				Modified = "2026-01-15T12:00:00Z"
			}
		};

		var json = ManifestSerializer.Serialize(manifest);

		Assert.Contains("\"name\": \"my-game\"", json);
		Assert.Contains("\"compiler\"", json);
		Assert.Contains("\"build\"", json);
		Assert.Contains("\"metadata\"", json);
	}

	[Fact]
	public void Deserialize_MinimalJson_CreatesValidManifest() {
		var json = @"{
			""name"": ""test-game"",
			""version"": ""1.0.0"",
			""platform"": ""nes""
		}";

		var manifest = ManifestSerializer.Deserialize(json);

		Assert.Equal("test-game", manifest.Name);
		Assert.Equal("1.0.0", manifest.Version);
		Assert.Equal("nes", manifest.Platform);
	}

	[Fact]
	public void Deserialize_CompleteJson_CreatesValidManifest() {
		var json = @"{
			""name"": ""my-game"",
			""version"": ""1.0.0"",
			""description"": ""A retro platformer"",
			""platform"": ""nes"",
			""compiler"": {
				""version"": ""1.0.0"",
				""target"": ""nes"",
				""options"": {
					""optimize"": true,
					""debug"": false,
					""warnings"": ""all""
				}
			}
		}";

		var manifest = ManifestSerializer.Deserialize(json);

		Assert.Equal("my-game", manifest.Name);
		Assert.NotNull(manifest.Compiler);
		Assert.Equal("nes", manifest.Compiler.Target);
		Assert.NotNull(manifest.Compiler.Options);
		Assert.True(manifest.Compiler.Options.Optimize);
	}

	[Fact]
	public void Deserialize_InvalidJson_ThrowsException() {
		var json = "{ invalid json }";

		Assert.Throws<System.Text.Json.JsonException>(() => ManifestSerializer.Deserialize(json));
	}

	[Fact]
	public void RoundTrip_PreservesData() {
		var original = new ProjectManifest {
			Name = "test-game",
			Version = "2.1.0",
			Platform = "snes",
			Compiler = new ManifestCompilerConfig {
				Target = "snes",
				Options = new ManifestCompilerOptions {
					Optimize = false,
					Debug = true,
					Warnings = "errors"
				}
			}
		};

		var json = ManifestSerializer.Serialize(original);
		var deserialized = ManifestSerializer.Deserialize(json);

		Assert.Equal(original.Name, deserialized.Name);
		Assert.Equal(original.Version, deserialized.Version);
		Assert.Equal(original.Platform, deserialized.Platform);
		Assert.NotNull(deserialized.Compiler);
		Assert.Equal(original.Compiler.Target, deserialized.Compiler.Target);
		Assert.NotNull(deserialized.Compiler.Options);
		Assert.Equal(original.Compiler.Options.Optimize, deserialized.Compiler.Options.Optimize);
		Assert.Equal(original.Compiler.Options.Debug, deserialized.Compiler.Options.Debug);
	}

	[Fact]
	public void SaveToFile_CreatesFile() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "nes"
		};

		var tempFile = Path.GetTempFileName();
		try {
			ManifestSerializer.SaveToFile(manifest, tempFile);

			Assert.True(File.Exists(tempFile));
			var content = File.ReadAllText(tempFile);
			Assert.Contains("test-game", content);
		} finally {
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}

	[Fact]
	public void LoadFromFile_ReadsFile() {
		var manifest = new ProjectManifest {
			Name = "file-test",
			Version = "1.0.0",
			Platform = "gb"
		};

		var tempFile = Path.GetTempFileName();
		try {
			ManifestSerializer.SaveToFile(manifest, tempFile);
			var loaded = ManifestSerializer.LoadFromFile(tempFile);

			Assert.Equal(manifest.Name, loaded.Name);
			Assert.Equal(manifest.Version, loaded.Version);
			Assert.Equal(manifest.Platform, loaded.Platform);
		} finally {
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}

	[Fact]
	public void LoadFromFile_NonExistentFile_ThrowsException() {
		Assert.Throws<FileNotFoundException>(() =>
			ManifestSerializer.LoadFromFile("nonexistent.json"));
	}

	[Fact]
	public void TryLoadFromFile_ValidFile_ReturnsTrue() {
		var manifest = new ProjectManifest {
			Name = "try-test",
			Version = "1.0.0",
			Platform = "nes"
		};

		var tempFile = Path.GetTempFileName();
		try {
			ManifestSerializer.SaveToFile(manifest, tempFile);

			var success = ManifestSerializer.TryLoadFromFile(tempFile, out var loaded, out var error);

			Assert.True(success);
			Assert.NotNull(loaded);
			Assert.Null(error);
			Assert.Equal(manifest.Name, loaded.Name);
		} finally {
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}

	[Fact]
	public void TryLoadFromFile_InvalidFile_ReturnsFalse() {
		var success = ManifestSerializer.TryLoadFromFile("nonexistent.json", out var loaded, out var error);

		Assert.False(success);
		Assert.Null(loaded);
		Assert.NotNull(error);
	}
}
