using System.Text.Json.Serialization;

namespace Poppy.Core.Project;

/// <summary>
/// Represents the complete project manifest (poppy.json).
/// </summary>
public class ProjectManifest {
	/// <summary>
	/// JSON schema URL for validation (optional).
	/// </summary>
	[JsonPropertyName("$schema")]
	public string? Schema { get; set; }

	/// <summary>
	/// Project name (required, lowercase with hyphens).
	/// </summary>
	[JsonPropertyName("name")]
	[JsonRequired]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Semantic version (x.y.z format, required).
	/// </summary>
	[JsonPropertyName("version")]
	[JsonRequired]
	public string Version { get; set; } = string.Empty;

	/// <summary>
	/// Short project description (optional).
	/// </summary>
	[JsonPropertyName("description")]
	public string? Description { get; set; }

	/// <summary>
	/// Author name or organization (optional).
	/// </summary>
	[JsonPropertyName("author")]
	public string? Author { get; set; }

	/// <summary>
	/// SPDX license identifier (optional).
	/// </summary>
	[JsonPropertyName("license")]
	public string? License { get; set; }

	/// <summary>
	/// Target platform (required).
	/// Valid values: nes, snes, gb, gbc, atari2600, lynx, genesis, sms, gba, wonderswan, tg16, spc700.
	/// </summary>
	[JsonPropertyName("platform")]
	[JsonRequired]
	public string Platform { get; set; } = string.Empty;

	/// <summary>
	/// Entry point file path (optional, default: src/main.pasm).
	/// </summary>
	[JsonPropertyName("entry")]
	public string Entry { get; set; } = "src/main.pasm";

	/// <summary>
	/// Output file path (optional, default: build/{name}.{ext}).
	/// </summary>
	[JsonPropertyName("output")]
	public string? Output { get; set; }

	/// <summary>
	/// Compiler configuration (optional).
	/// </summary>
	[JsonPropertyName("compiler")]
	public ManifestCompilerConfig? Compiler { get; set; }

	/// <summary>
	/// Build configuration (optional).
	/// </summary>
	[JsonPropertyName("build")]
	public ManifestBuildConfig? Build { get; set; }

	/// <summary>
	/// Asset directory mappings (optional).
	/// </summary>
	[JsonPropertyName("assets")]
	public Dictionary<string, string>? Assets { get; set; }

	/// <summary>
	/// External dependencies with version ranges (optional).
	/// </summary>
	[JsonPropertyName("dependencies")]
	public Dictionary<string, string>? Dependencies { get; set; }

	/// <summary>
	/// Additional project metadata (optional).
	/// </summary>
	[JsonPropertyName("metadata")]
	public ManifestMetadata? Metadata { get; set; }
}

/// <summary>
/// Compiler configuration section.
/// </summary>
public class ManifestCompilerConfig {
	/// <summary>
	/// Minimum Poppy compiler version (semver range, optional).
	/// </summary>
	[JsonPropertyName("version")]
	public string? Version { get; set; }

	/// <summary>
	/// Compilation target (should match platform, required).
	/// </summary>
	[JsonPropertyName("target")]
	[JsonRequired]
	public string Target { get; set; } = string.Empty;

	/// <summary>
	/// Compiler options/flags (optional).
	/// </summary>
	[JsonPropertyName("options")]
	public ManifestCompilerOptions? Options { get; set; }
}

/// <summary>
/// Compiler options and flags.
/// </summary>
public class ManifestCompilerOptions {
	/// <summary>
	/// Enable optimizations (default: true).
	/// </summary>
	[JsonPropertyName("optimize")]
	public bool Optimize { get; set; } = true;

	/// <summary>
	/// Include debug information (default: false).
	/// </summary>
	[JsonPropertyName("debug")]
	public bool Debug { get; set; }

	/// <summary>
	/// Warning level (default: "all").
	/// Valid values: "none", "errors", "all".
	/// </summary>
	[JsonPropertyName("warnings")]
	public string Warnings { get; set; } = "all";
}

/// <summary>
/// Build configuration section.
/// </summary>
public class ManifestBuildConfig {
	/// <summary>
	/// Additional include directories (optional).
	/// </summary>
	[JsonPropertyName("includePaths")]
	public List<string>? IncludePaths { get; set; }

	/// <summary>
	/// Preprocessor defines (optional).
	/// </summary>
	[JsonPropertyName("defines")]
	public Dictionary<string, object>? Defines { get; set; }

	/// <summary>
	/// Build scripts/commands (optional).
	/// </summary>
	[JsonPropertyName("scripts")]
	public Dictionary<string, string>? Scripts { get; set; }
}

/// <summary>
/// Project metadata section.
/// </summary>
public class ManifestMetadata {
	/// <summary>
	/// Project tags/keywords (optional).
	/// </summary>
	[JsonPropertyName("tags")]
	public List<string>? Tags { get; set; }

	/// <summary>
	/// Project homepage URL (optional).
	/// </summary>
	[JsonPropertyName("homepage")]
	public string? Homepage { get; set; }

	/// <summary>
	/// Git repository URL (optional).
	/// </summary>
	[JsonPropertyName("repository")]
	public string? Repository { get; set; }

	/// <summary>
	/// ISO 8601 creation timestamp (optional).
	/// </summary>
	[JsonPropertyName("created")]
	public string? Created { get; set; }

	/// <summary>
	/// ISO 8601 last modified timestamp (optional).
	/// </summary>
	[JsonPropertyName("modified")]
	public string? Modified { get; set; }
}
