// ============================================================================
// PeonyProjectReader.cs - Read peony.json Project Configuration
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Poppy.Core.Project;

/// <summary>
/// ROM information from a Peony project file.
/// </summary>
public sealed class PeonyRomInfo {
	/// <summary>Relative path to the original ROM file.</summary>
	[JsonPropertyName("path")]
	public string? Path { get; set; }

	/// <summary>CRC32 hash of the ROM (lowercase hex string).</summary>
	[JsonPropertyName("crc32")]
	public string? Crc32 { get; set; }

	/// <summary>ROM file size in bytes.</summary>
	[JsonPropertyName("size")]
	public long Size { get; set; }
}

/// <summary>
/// Metadata file references from a Peony project file.
/// </summary>
public sealed class PeonyMetadataInfo {
	/// <summary>Relative path to the CDL file.</summary>
	[JsonPropertyName("cdl")]
	public string? Cdl { get; set; }

	/// <summary>Relative path to the Pansy metadata file.</summary>
	[JsonPropertyName("pansy")]
	public string? Pansy { get; set; }
}

/// <summary>
/// Output configuration from a Peony project file.
/// </summary>
public sealed class PeonyOutputConfig {
	/// <summary>Output format (e.g., "poppy").</summary>
	[JsonPropertyName("format")]
	public string? Format { get; set; }

	/// <summary>Output directory for generated source files.</summary>
	[JsonPropertyName("directory")]
	public string? Directory { get; set; }

	/// <summary>Whether to split output into per-bank files.</summary>
	[JsonPropertyName("splitBanks")]
	public bool SplitBanks { get; set; }
}

/// <summary>
/// Source/import information from a Peony project file.
/// </summary>
public sealed class PeonySourceInfo {
	/// <summary>Path to the original Nexen pack zip file.</summary>
	[JsonPropertyName("nexenPack")]
	public string? NexenPack { get; set; }

	/// <summary>ISO 8601 timestamp of the import.</summary>
	[JsonPropertyName("importDate")]
	public string? ImportDate { get; set; }
}

/// <summary>
/// Represents a Peony disassembly project configuration (peony.json).
/// </summary>
public sealed class PeonyProject {
	/// <summary>Peony project format version.</summary>
	[JsonPropertyName("version")]
	public string? Version { get; set; }

	/// <summary>Target platform (e.g., "nes", "snes", "gb").</summary>
	[JsonPropertyName("platform")]
	public string? Platform { get; set; }

	/// <summary>ROM file information.</summary>
	[JsonPropertyName("rom")]
	public PeonyRomInfo? Rom { get; set; }

	/// <summary>Metadata file references (CDL, Pansy).</summary>
	[JsonPropertyName("metadata")]
	public PeonyMetadataInfo? Metadata { get; set; }

	/// <summary>Output configuration for disassembly.</summary>
	[JsonPropertyName("output")]
	public PeonyOutputConfig? Output { get; set; }

	/// <summary>Source/import provenance information.</summary>
	[JsonPropertyName("source")]
	public PeonySourceInfo? Source { get; set; }

	/// <summary>
	/// The directory containing the peony.json file. Set after loading.
	/// </summary>
	[JsonIgnore]
	public string ProjectDirectory { get; set; } = string.Empty;

	/// <summary>
	/// Resolves a relative path against the project directory.
	/// Returns the path unchanged if it is already rooted.
	/// </summary>
	public string ResolvePath(string relativePath) {
		if (string.IsNullOrEmpty(relativePath))
			return ProjectDirectory;
		if (Path.IsPathRooted(relativePath))
			return relativePath;
		return Path.GetFullPath(Path.Combine(ProjectDirectory, relativePath));
	}

	/// <summary>
	/// Gets the resolved ROM file path, or null if not specified.
	/// </summary>
	[JsonIgnore]
	public string? ResolvedRomPath => Rom?.Path is not null ? ResolvePath(Rom.Path) : null;

	/// <summary>
	/// Gets the resolved Pansy metadata file path, or null if not specified.
	/// </summary>
	[JsonIgnore]
	public string? ResolvedPansyPath => Metadata?.Pansy is not null ? ResolvePath(Metadata.Pansy) : null;

	/// <summary>
	/// Gets the resolved CDL file path, or null if not specified.
	/// </summary>
	[JsonIgnore]
	public string? ResolvedCdlPath => Metadata?.Cdl is not null ? ResolvePath(Metadata.Cdl) : null;

	/// <summary>
	/// Gets the resolved output directory, or null if not specified.
	/// </summary>
	[JsonIgnore]
	public string? ResolvedOutputDirectory => Output?.Directory is not null ? ResolvePath(Output.Directory) : null;

	/// <summary>
	/// Validates the project configuration.
	/// </summary>
	/// <returns>List of validation errors, empty if valid.</returns>
	public List<string> Validate() {
		var errors = new List<string>();

		if (string.IsNullOrWhiteSpace(Version))
			errors.Add("'version' is required");

		if (string.IsNullOrWhiteSpace(Platform))
			errors.Add("'platform' is required");

		if (Rom is null)
			errors.Add("'rom' section is required");
		else if (string.IsNullOrWhiteSpace(Rom.Path))
			errors.Add("'rom.path' is required");

		return errors;
	}
}

/// <summary>
/// Reads peony.json project configuration files produced by Peony's import command.
/// </summary>
public static class PeonyProjectReader {
	private static readonly JsonSerializerOptions JsonOptions = new() {
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true
	};

	/// <summary>
	/// Loads a Peony project from a directory containing peony.json.
	/// </summary>
	/// <param name="projectDir">Directory containing peony.json.</param>
	/// <returns>The loaded project configuration.</returns>
	/// <exception cref="FileNotFoundException">Thrown if peony.json is not found.</exception>
	/// <exception cref="JsonException">Thrown if JSON is invalid.</exception>
	public static PeonyProject Load(string projectDir) {
		var jsonPath = Path.Combine(projectDir, "peony.json");
		return LoadFromJson(jsonPath);
	}

	/// <summary>
	/// Loads a Peony project from a specific JSON file path.
	/// </summary>
	/// <param name="jsonPath">Path to the peony.json file.</param>
	/// <returns>The loaded project configuration.</returns>
	/// <exception cref="FileNotFoundException">Thrown if the file is not found.</exception>
	/// <exception cref="JsonException">Thrown if JSON is invalid.</exception>
	public static PeonyProject LoadFromJson(string jsonPath) {
		if (!File.Exists(jsonPath))
			throw new FileNotFoundException($"Peony project file not found: {jsonPath}", jsonPath);

		var json = File.ReadAllText(jsonPath);
		var project = JsonSerializer.Deserialize<PeonyProject>(json, JsonOptions)
			?? throw new JsonException("Failed to deserialize peony.json: result was null");

		project.ProjectDirectory = Path.GetDirectoryName(Path.GetFullPath(jsonPath)) ?? string.Empty;
		return project;
	}

	/// <summary>
	/// Loads a Peony project from a JSON string (for testing).
	/// </summary>
	/// <param name="json">JSON content.</param>
	/// <param name="projectDir">Project directory for path resolution.</param>
	/// <returns>The loaded project configuration.</returns>
	/// <exception cref="JsonException">Thrown if JSON is invalid.</exception>
	public static PeonyProject LoadFromString(string json, string projectDir = "") {
		var project = JsonSerializer.Deserialize<PeonyProject>(json, JsonOptions)
			?? throw new JsonException("Failed to deserialize peony.json: result was null");

		project.ProjectDirectory = projectDir;
		return project;
	}

	/// <summary>
	/// Tries to load a Peony project from a directory.
	/// </summary>
	/// <param name="projectDir">Directory containing peony.json.</param>
	/// <param name="project">The loaded project, or null on failure.</param>
	/// <param name="error">Error message on failure.</param>
	/// <returns>True if successful.</returns>
	public static bool TryLoad(string projectDir, out PeonyProject? project, out string? error) {
		try {
			project = Load(projectDir);
			error = null;
			return true;
		} catch (Exception ex) {
			project = null;
			error = ex.Message;
			return false;
		}
	}
}
