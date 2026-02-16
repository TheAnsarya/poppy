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
	/// Build configurations (debug, release, etc.).
	/// </summary>
	[JsonPropertyName("configurations")]
	public Dictionary<string, BuildConfiguration> Configurations { get; set; } = [];

	/// <summary>
	/// Default configuration name to use when none specified.
	/// </summary>
	[JsonPropertyName("defaultConfiguration")]
	public string DefaultConfiguration { get; set; } = "debug";

	/// <summary>
	/// Gets the target architecture enum from the string.
	/// </summary>
	[JsonIgnore]
	public TargetArchitecture TargetArchitecture => Target?.ToLowerInvariant() switch {
		"nes" or "6502" => Semantics.TargetArchitecture.MOS6502,
		"atari2600" or "2600" or "6507" => Semantics.TargetArchitecture.MOS6507,
		"lynx" or "65sc02" => Semantics.TargetArchitecture.MOS65SC02,
		"snes" or "65816" => Semantics.TargetArchitecture.WDC65816,
		"gb" or "gbc" or "gameboy" or "sm83" => Semantics.TargetArchitecture.SM83,
		"genesis" or "megadrive" or "68000" or "m68000" => Semantics.TargetArchitecture.M68000,
		"sms" or "gg" or "z80" => Semantics.TargetArchitecture.Z80,
		"wonderswan" or "ws" or "wsc" or "v30mz" => Semantics.TargetArchitecture.V30MZ,
		"gba" or "arm7" or "arm7tdmi" => Semantics.TargetArchitecture.ARM7TDMI,
		"spc" or "spc700" => Semantics.TargetArchitecture.SPC700,
		"tg16" or "pce" or "huc6280" => Semantics.TargetArchitecture.HuC6280,
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
		return target?.ToLowerInvariant() is
			// NES / 6502
			"nes" or "6502" or
			// Atari 2600 / 6507
			"atari2600" or "2600" or "6507" or
			// Atari Lynx / 65SC02
			"lynx" or "65sc02" or
			// SNES / 65816
			"snes" or "65816" or
			// Game Boy / SM83
			"gb" or "gbc" or "gameboy" or "sm83" or
			// Sega Genesis / M68000
			"genesis" or "megadrive" or "68000" or "m68000" or
			// Sega Master System / Z80
			"sms" or "gg" or "z80" or
			// WonderSwan / V30MZ
			"wonderswan" or "ws" or "wsc" or "v30mz" or
			// GBA / ARM7TDMI
			"gba" or "arm7" or "arm7tdmi" or
			// SNES APU / SPC700
			"spc" or "spc700" or
			// TurboGrafx-16 / HuC6280
			"tg16" or "pce" or "huc6280";
	}

	/// <summary>
	/// Gets the effective configuration, merging base settings with config overrides.
	/// </summary>
	/// <param name="configName">Name of the configuration (e.g., "debug", "release").</param>
	/// <returns>Merged configuration settings.</returns>
	public BuildConfiguration GetEffectiveConfiguration(string? configName) {
		// Use default config if none specified
		configName ??= DefaultConfiguration;

		// Start with base project settings
		var effective = new BuildConfiguration {
			Output = Output,
			Symbols = Symbols,
			Listing = Listing,
			MapFile = MapFile,
			Defines = new Dictionary<string, long>(Defines)
		};

		// Merge in config-specific settings if they exist
		if (Configurations.TryGetValue(configName, out var config)) {
			if (config.Output is not null) {
				effective.Output = config.Output;
			}

			if (config.Symbols is not null) {
				effective.Symbols = config.Symbols;
			}

			if (config.Listing is not null) {
				effective.Listing = config.Listing;
			}

			if (config.MapFile is not null) {
				effective.MapFile = config.MapFile;
			}

			// Merge defines (config defines override base defines)
			foreach (var define in config.Defines) {
				effective.Defines[define.Key] = define.Value;
			}
		}

		return effective;
	}
}

/// <summary>
/// Represents a build configuration (debug, release, etc.).
/// </summary>
public sealed class BuildConfiguration {
	/// <summary>
	/// Output file path override.
	/// </summary>
	[JsonPropertyName("output")]
	public string? Output { get; set; }

	/// <summary>
	/// Symbol file output path override.
	/// </summary>
	[JsonPropertyName("symbols")]
	public string? Symbols { get; set; }

	/// <summary>
	/// Listing file output path override.
	/// </summary>
	[JsonPropertyName("listing")]
	public string? Listing { get; set; }

	/// <summary>
	/// Memory map file output path override.
	/// </summary>
	[JsonPropertyName("mapfile")]
	public string? MapFile { get; set; }

	/// <summary>
	/// Additional preprocessor definitions for this config.
	/// </summary>
	[JsonPropertyName("defines")]
	public Dictionary<string, long> Defines { get; set; } = [];
}
