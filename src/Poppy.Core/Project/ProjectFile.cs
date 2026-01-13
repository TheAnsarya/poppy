// ============================================================================
// ProjectFile.cs - Poppy Project File Model
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using Poppy.Core.Semantics;

namespace Poppy.Core.Project;

/// <summary>
/// Represents a Poppy project configuration file (poppy.json).
/// </summary>
public sealed class ProjectFile {
	/// <summary>
	/// Project name.
	/// </summary>
	[JsonPropertyName("name")]
	public string Name { get; set; } = "Untitled";

	/// <summary>
	/// Project version.
	/// </summary>
	[JsonPropertyName("version")]
	public string? Version { get; set; }

	/// <summary>
	/// Target architecture (nes, snes, gb).
	/// </summary>
	[JsonPropertyName("target")]
	public string Target { get; set; } = "nes";

	/// <summary>
	/// Output file path.
	/// </summary>
	[JsonPropertyName("output")]
	public string? Output { get; set; }

	/// <summary>
	/// Main source file (entry point).
	/// </summary>
	[JsonPropertyName("main")]
	public string? Main { get; set; }

	/// <summary>
	/// Source file patterns to compile.
	/// </summary>
	[JsonPropertyName("sources")]
	public List<string> Sources { get; set; } = [];

	/// <summary>
	/// Include directories for .include directive.
	/// </summary>
	[JsonPropertyName("includes")]
	public List<string> Includes { get; set; } = [];

	/// <summary>
	/// Preprocessor definitions.
	/// </summary>
	[JsonPropertyName("defines")]
	public Dictionary<string, long> Defines { get; set; } = [];

	/// <summary>
	/// Symbol file output path.
	/// </summary>
	[JsonPropertyName("symbols")]
	public string? Symbols { get; set; }

	/// <summary>
	/// Listing file output path.
	/// </summary>
	[JsonPropertyName("listing")]
	public string? Listing { get; set; }

	/// <summary>
	/// Memory map file output path.
	/// </summary>
	[JsonPropertyName("mapfile")]
	public string? MapFile { get; set; }

	/// <summary>
	/// Enable auto-generation of routine labels.
	/// </summary>
	[JsonPropertyName("autoLabels")]
	public bool AutoLabels { get; set; }

	/// <summary>
	/// Gets the target architecture enum from the string.
	/// </summary>
	[JsonIgnore]
	public TargetArchitecture TargetArchitecture => Target?.ToLowerInvariant() switch {
		"nes" or "6502" => Semantics.TargetArchitecture.MOS6502,
		"snes" or "65816" => Semantics.TargetArchitecture.WDC65816,
		"gb" or "gameboy" or "sm83" => Semantics.TargetArchitecture.SM83,
		_ => Semantics.TargetArchitecture.MOS6502
	};

	/// <summary>
	/// JSON serializer options for project files.
	/// </summary>
	private static readonly JsonSerializerOptions JsonOptions = new() {
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		WriteIndented = true
	};

	/// <summary>
	/// Loads a project file from disk.
	/// </summary>
	/// <param name="path">Path to the poppy.json file.</param>
	/// <returns>The loaded project file.</returns>
	public static ProjectFile Load(string path) {
		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<ProjectFile>(json, JsonOptions)
			?? throw new InvalidOperationException("Failed to parse project file");
	}

	/// <summary>
	/// Saves the project file to disk.
	/// </summary>
	/// <param name="path">Path to save the file.</param>
	public void Save(string path) {
		var json = JsonSerializer.Serialize(this, JsonOptions);
		File.WriteAllText(path, json);
	}

	/// <summary>
	/// Creates a new project file with default values.
	/// </summary>
	/// <param name="name">Project name.</param>
	/// <param name="target">Target architecture.</param>
	/// <returns>A new project file.</returns>
	public static ProjectFile Create(string name, string target = "nes") {
		return new ProjectFile {
			Name = name,
			Target = target,
			Main = "main.pasm",
			Output = $"{name}.bin"
		};
	}

	/// <summary>
	/// Validates the project file configuration.
	/// </summary>
	/// <returns>List of validation errors, empty if valid.</returns>
	public List<string> Validate() {
		var errors = new List<string>();

		if (string.IsNullOrWhiteSpace(Name)) {
			errors.Add("Project name is required");
		}

		if (string.IsNullOrWhiteSpace(Main) && Sources.Count == 0) {
			errors.Add("Either 'main' or 'sources' must be specified");
		}

		if (!IsValidTarget(Target)) {
			errors.Add($"Invalid target architecture: {Target}");
		}

		return errors;
	}

	/// <summary>
	/// Checks if a target string is valid.
	/// </summary>
	private static bool IsValidTarget(string? target) {
		return target?.ToLowerInvariant() is "nes" or "6502" or "snes" or "65816" or "gb" or "gameboy" or "sm83";
	}
}
