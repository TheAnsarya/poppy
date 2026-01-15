using System.Text.RegularExpressions;

namespace Poppy.Core.Project;

/// <summary>
/// Validates project manifest files against schema and business rules.
/// </summary>
public static class ManifestValidator {
	/// <summary>
	/// Valid platform identifiers.
	/// </summary>
	private static readonly HashSet<string> ValidPlatforms = new(StringComparer.OrdinalIgnoreCase) {
		"nes", "snes", "gb", "gbc", "atari2600", "lynx", 
		"genesis", "sms", "gba", "wonderswan", "tg16", "spc700"
	};

	/// <summary>
	/// Valid warning levels.
	/// </summary>
	private static readonly HashSet<string> ValidWarningLevels = new(StringComparer.OrdinalIgnoreCase) {
		"none", "errors", "all"
	};

	/// <summary>
	/// Regex for valid project names (lowercase, hyphens, numbers).
	/// </summary>
	private static readonly Regex NamePattern = new(@"^[a-z0-9-]+$", RegexOptions.Compiled);

	/// <summary>
	/// Regex for semantic version (x.y.z with optional pre-release and build metadata).
	/// </summary>
	private static readonly Regex VersionPattern = new(
		@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
		RegexOptions.Compiled
	);

	/// <summary>
	/// Validates a project manifest and returns validation errors.
	/// </summary>
	/// <param name="manifest">The manifest to validate.</param>
	/// <returns>List of validation error messages (empty if valid).</returns>
	public static List<string> Validate(ProjectManifest manifest) {
		var errors = new List<string>();

		// Required fields
		if (string.IsNullOrWhiteSpace(manifest.Name)) {
			errors.Add("Project name is required");
		} else if (!NamePattern.IsMatch(manifest.Name)) {
			errors.Add("Project name must be lowercase with hyphens and numbers only");
		}

		if (string.IsNullOrWhiteSpace(manifest.Version)) {
			errors.Add("Project version is required");
		} else if (!VersionPattern.IsMatch(manifest.Version)) {
			errors.Add("Project version must be valid semantic version (x.y.z)");
		}

		if (string.IsNullOrWhiteSpace(manifest.Platform)) {
			errors.Add("Platform is required");
		} else if (!ValidPlatforms.Contains(manifest.Platform)) {
			errors.Add($"Invalid platform '{manifest.Platform}'. Valid platforms: {string.Join(", ", ValidPlatforms)}");
		}

		// Entry point validation
		if (string.IsNullOrWhiteSpace(manifest.Entry)) {
			errors.Add("Entry point is required");
		} else if (!manifest.Entry.EndsWith(".pasm", StringComparison.OrdinalIgnoreCase)) {
			errors.Add("Entry point must be a .pasm file");
		}

		// Compiler configuration
		if (manifest.Compiler != null) {
			if (string.IsNullOrWhiteSpace(manifest.Compiler.Target)) {
				errors.Add("Compiler target is required when compiler section is present");
			} else if (!ValidPlatforms.Contains(manifest.Compiler.Target)) {
				errors.Add($"Invalid compiler target '{manifest.Compiler.Target}'");
			} else if (!manifest.Compiler.Target.Equals(manifest.Platform, StringComparison.OrdinalIgnoreCase)) {
				errors.Add("Compiler target must match platform");
			}

			// Compiler options
			if (manifest.Compiler.Options != null) {
				if (!string.IsNullOrWhiteSpace(manifest.Compiler.Options.Warnings) &&
					!ValidWarningLevels.Contains(manifest.Compiler.Options.Warnings)) {
					errors.Add($"Invalid warning level '{manifest.Compiler.Options.Warnings}'. Valid values: {string.Join(", ", ValidWarningLevels)}");
				}
			}
		}

		// Metadata validation
		if (manifest.Metadata != null) {
			// Validate URLs if present
			if (!string.IsNullOrWhiteSpace(manifest.Metadata.Homepage) && !Uri.IsWellFormedUriString(manifest.Metadata.Homepage, UriKind.Absolute)) {
				errors.Add("Homepage must be a valid absolute URL");
			}

			if (!string.IsNullOrWhiteSpace(manifest.Metadata.Repository) && !Uri.IsWellFormedUriString(manifest.Metadata.Repository, UriKind.Absolute)) {
				errors.Add("Repository must be a valid absolute URL");
			}

			// Validate timestamps if present
			if (!string.IsNullOrWhiteSpace(manifest.Metadata.Created) && !DateTime.TryParse(manifest.Metadata.Created, out _)) {
				errors.Add("Created timestamp must be valid ISO 8601 format");
			}

			if (!string.IsNullOrWhiteSpace(manifest.Metadata.Modified) && !DateTime.TryParse(manifest.Metadata.Modified, out _)) {
				errors.Add("Modified timestamp must be valid ISO 8601 format");
			}
		}

		return errors;
	}

	/// <summary>
	/// Validates a manifest and throws an exception if invalid.
	/// </summary>
	/// <param name="manifest">The manifest to validate.</param>
	/// <exception cref="InvalidOperationException">Thrown if manifest is invalid.</exception>
	public static void ValidateOrThrow(ProjectManifest manifest) {
		var errors = Validate(manifest);
		if (errors.Count > 0) {
			throw new InvalidOperationException($"Invalid manifest:\n- {string.Join("\n- ", errors)}");
		}
	}

	/// <summary>
	/// Checks if a platform identifier is valid.
	/// </summary>
	/// <param name="platform">The platform identifier to check.</param>
	/// <returns>True if valid, false otherwise.</returns>
	public static bool IsValidPlatform(string platform) {
		return !string.IsNullOrWhiteSpace(platform) && ValidPlatforms.Contains(platform);
	}

	/// <summary>
	/// Gets all valid platform identifiers.
	/// </summary>
	/// <returns>List of valid platform identifiers.</returns>
	public static IReadOnlyList<string> GetValidPlatforms() {
		return ValidPlatforms.OrderBy(p => p).ToList();
	}
}
