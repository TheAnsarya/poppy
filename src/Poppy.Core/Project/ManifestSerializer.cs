using System.Text.Json;
using System.Text.Json.Serialization;

namespace Poppy.Core.Project;

/// <summary>
/// Handles serialization and deserialization of project manifests.
/// </summary>
public static class ManifestSerializer {
	/// <summary>
	/// JSON serializer options for manifests.
	/// </summary>
	private static readonly JsonSerializerOptions Options = new() {
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = {
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
		}
	};

	/// <summary>
	/// Serializes a manifest to JSON string.
	/// </summary>
	/// <param name="manifest">The manifest to serialize.</param>
	/// <returns>JSON string representation.</returns>
	public static string Serialize(ProjectManifest manifest) {
		return JsonSerializer.Serialize(manifest, Options);
	}

	/// <summary>
	/// Deserializes a manifest from JSON string.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized manifest.</returns>
	/// <exception cref="JsonException">Thrown if JSON is invalid.</exception>
	public static ProjectManifest Deserialize(string json) {
		var manifest = JsonSerializer.Deserialize<ProjectManifest>(json, Options);
		if (manifest == null) {
			throw new JsonException("Failed to deserialize manifest: result was null");
		}
		return manifest;
	}

	/// <summary>
	/// Loads a manifest from a file.
	/// </summary>
	/// <param name="filePath">Path to the poppy.json file.</param>
	/// <returns>The loaded manifest.</returns>
	/// <exception cref="FileNotFoundException">Thrown if file doesn't exist.</exception>
	/// <exception cref="JsonException">Thrown if JSON is invalid.</exception>
	public static ProjectManifest LoadFromFile(string filePath) {
		if (!File.Exists(filePath)) {
			throw new FileNotFoundException($"Manifest file not found: {filePath}");
		}

		var json = File.ReadAllText(filePath);
		return Deserialize(json);
	}

	/// <summary>
	/// Saves a manifest to a file.
	/// </summary>
	/// <param name="manifest">The manifest to save.</param>
	/// <param name="filePath">Path to save the poppy.json file.</param>
	public static void SaveToFile(ProjectManifest manifest, string filePath) {
		var json = Serialize(manifest);
		File.WriteAllText(filePath, json);
	}

	/// <summary>
	/// Tries to load a manifest from a file.
	/// </summary>
	/// <param name="filePath">Path to the poppy.json file.</param>
	/// <param name="manifest">The loaded manifest (null if failed).</param>
	/// <param name="error">Error message if loading failed.</param>
	/// <returns>True if successful, false otherwise.</returns>
	public static bool TryLoadFromFile(string filePath, out ProjectManifest? manifest, out string? error) {
		try {
			manifest = LoadFromFile(filePath);
			error = null;
			return true;
		} catch (Exception ex) {
			manifest = null;
			error = ex.Message;
			return false;
		}
	}
}
