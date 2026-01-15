using Poppy.Core.Project;
using Xunit;

namespace Poppy.Tests.Project;

/// <summary>
/// Tests for ManifestValidator validation logic.
/// </summary>
public class ManifestValidatorTests {
	[Fact]
	public void Validate_MinimalValidManifest_ReturnsNoErrors() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "nes"
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.Empty(errors);
	}

	[Fact]
	public void Validate_MissingName_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "",
			Version = "1.0.0",
			Platform = "nes"
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("name is required"));
	}

	[Fact]
	public void Validate_InvalidNameFormat_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "Test Game",  // Spaces not allowed
			Version = "1.0.0",
			Platform = "nes"
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("lowercase"));
	}

	[Fact]
	public void Validate_UppercaseName_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "TestGame",  // Uppercase not allowed
			Version = "1.0.0",
			Platform = "nes"
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("lowercase"));
	}

	[Fact]
	public void Validate_ValidNameWithHyphens_ReturnsNoErrors() {
		var manifest = new ProjectManifest {
			Name = "test-game-2",
			Version = "1.0.0",
			Platform = "nes"
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.Empty(errors);
	}

	[Fact]
	public void Validate_MissingVersion_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "",
			Platform = "nes"
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("version is required"));
	}

	[Fact]
	public void Validate_InvalidVersionFormat_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0",  // Must be x.y.z
			Platform = "nes"
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("semantic version"));
	}

	[Theory]
	[InlineData("1.0.0")]
	[InlineData("0.1.0")]
	[InlineData("2.10.5")]
	[InlineData("1.0.0-alpha")]
	[InlineData("1.0.0-beta.1")]
	[InlineData("1.0.0+build.123")]
	public void Validate_ValidVersionFormats_ReturnsNoErrors(string version) {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = version,
			Platform = "nes"
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.Empty(errors);
	}

	[Fact]
	public void Validate_MissingPlatform_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = ""
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("Platform is required"));
	}

	[Fact]
	public void Validate_InvalidPlatform_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "playstation"  // Not a valid retro platform
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("Invalid platform"));
	}

	[Theory]
	[InlineData("nes")]
	[InlineData("snes")]
	[InlineData("gb")]
	[InlineData("gbc")]
	[InlineData("atari2600")]
	[InlineData("lynx")]
	[InlineData("genesis")]
	[InlineData("sms")]
	[InlineData("gba")]
	[InlineData("wonderswan")]
	[InlineData("tg16")]
	[InlineData("spc700")]
	public void Validate_ValidPlatforms_ReturnsNoErrors(string platform) {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = platform
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.Empty(errors);
	}

	[Fact]
	public void Validate_InvalidEntryPoint_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "nes",
			Entry = "main.txt"  // Must be .pasm
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains(".pasm"));
	}

	[Fact]
	public void Validate_CompilerTargetMismatch_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "nes",
			Compiler = new ManifestCompilerConfig {
				Target = "snes"  // Doesn't match platform
			}
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("must match platform"));
	}

	[Fact]
	public void Validate_InvalidWarningLevel_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "nes",
			Compiler = new ManifestCompilerConfig {
				Target = "nes",
				Options = new ManifestCompilerOptions {
					Warnings = "invalid"
				}
			}
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("warning level"));
	}

	[Theory]
	[InlineData("none")]
	[InlineData("errors")]
	[InlineData("all")]
	public void Validate_ValidWarningLevels_ReturnsNoErrors(string warningLevel) {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "nes",
			Compiler = new ManifestCompilerConfig {
				Target = "nes",
				Options = new ManifestCompilerOptions {
					Warnings = warningLevel
				}
			}
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.Empty(errors);
	}

	[Fact]
	public void Validate_InvalidHomepageUrl_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "nes",
			Metadata = new ManifestMetadata {
				Homepage = "not a url"
			}
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("Homepage"));
	}

	[Fact]
	public void Validate_InvalidRepositoryUrl_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "nes",
			Metadata = new ManifestMetadata {
				Repository = "not a url"
			}
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("Repository"));
	}

	[Fact]
	public void Validate_InvalidCreatedTimestamp_ReturnsError() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "nes",
			Metadata = new ManifestMetadata {
				Created = "not a timestamp"
			}
		};

		var errors = ManifestValidator.Validate(manifest);

		Assert.NotEmpty(errors);
		Assert.Contains(errors, e => e.Contains("Created timestamp"));
	}

	[Fact]
	public void ValidateOrThrow_ValidManifest_DoesNotThrow() {
		var manifest = new ProjectManifest {
			Name = "test-game",
			Version = "1.0.0",
			Platform = "nes"
		};

		var exception = Record.Exception(() => ManifestValidator.ValidateOrThrow(manifest));

		Assert.Null(exception);
	}

	[Fact]
	public void ValidateOrThrow_InvalidManifest_ThrowsException() {
		var manifest = new ProjectManifest {
			Name = "",  // Invalid
			Version = "1.0.0",
			Platform = "nes"
		};

		Assert.Throws<InvalidOperationException>(() => ManifestValidator.ValidateOrThrow(manifest));
	}

	[Fact]
	public void IsValidPlatform_ValidPlatform_ReturnsTrue() {
		Assert.True(ManifestValidator.IsValidPlatform("nes"));
		Assert.True(ManifestValidator.IsValidPlatform("snes"));
		Assert.True(ManifestValidator.IsValidPlatform("gb"));
	}

	[Fact]
	public void IsValidPlatform_InvalidPlatform_ReturnsFalse() {
		Assert.False(ManifestValidator.IsValidPlatform("playstation"));
		Assert.False(ManifestValidator.IsValidPlatform(""));
		Assert.False(ManifestValidator.IsValidPlatform(null!));
	}

	[Fact]
	public void GetValidPlatforms_ReturnsAllPlatforms() {
		var platforms = ManifestValidator.GetValidPlatforms();

		Assert.Contains("nes", platforms);
		Assert.Contains("snes", platforms);
		Assert.Contains("gb", platforms);
		Assert.Contains("genesis", platforms);
		Assert.Equal(12, platforms.Count);  // All supported platforms
	}
}
