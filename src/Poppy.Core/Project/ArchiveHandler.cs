using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Poppy.Core.Project;

/// <summary>
/// Handles packing and unpacking of .poppy project archives.
/// </summary>
public class ArchiveHandler {
	private const string ManifestFileName = "poppy.json";
	private const string MetadataDir = ".poppy";
	private const string VersionFile = "version.txt";
	private const string ChecksumsFile = "checksums.txt";
	private const string BuildInfoFile = "build-info.json";
	private const string FormatVersion = "1.0";

	/// <summary>
	/// Options for packing an archive.
	/// </summary>
	public class PackOptions {
		/// <summary>
		/// Output file path for the archive.
		/// </summary>
		public string? OutputPath { get; set; }

		/// <summary>
		/// Patterns to exclude from the archive (glob patterns).
		/// </summary>
		public List<string> ExcludePatterns { get; set; } = new() {
			".git/**",
			".vs/**",
			"bin/**",
			"obj/**",
			"*.user",
			"*.suo"
		};

		/// <summary>
		/// Include build directory in archive.
		/// </summary>
		public bool IncludeBuild { get; set; }

		/// <summary>
		/// Compression level (0-9, default 6).
		/// </summary>
		public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

		/// <summary>
		/// Calculate checksums for integrity verification.
		/// </summary>
		public bool CalculateChecksums { get; set; } = true;
	}

	/// <summary>
	/// Options for unpacking an archive.
	/// </summary>
	public class UnpackOptions {
		/// <summary>
		/// Target directory for extraction.
		/// </summary>
		public string? TargetDirectory { get; set; }

		/// <summary>
		/// Overwrite existing files.
		/// </summary>
		public bool Overwrite { get; set; }

		/// <summary>
		/// Validate checksums after extraction.
		/// </summary>
		public bool ValidateChecksums { get; set; } = true;

		/// <summary>
		/// Validate manifest schema.
		/// </summary>
		public bool ValidateManifest { get; set; } = true;
	}

	/// <summary>
	/// Packs a project directory into a .poppy archive.
	/// </summary>
	/// <param name="projectDirectory">Path to the project directory.</param>
	/// <param name="options">Packing options.</param>
	/// <returns>Path to the created archive.</returns>
	/// <exception cref="InvalidOperationException">Thrown if packing fails.</exception>
	public static string Pack(string projectDirectory, PackOptions? options = null) {
		options ??= new PackOptions();

		// Validate project directory
		if (!Directory.Exists(projectDirectory)) {
			throw new DirectoryNotFoundException($"Project directory not found: {projectDirectory}");
		}

		var manifestPath = Path.Combine(projectDirectory, ManifestFileName);
		if (!File.Exists(manifestPath)) {
			throw new FileNotFoundException($"Manifest file not found: {manifestPath}");
		}

		// Load and validate manifest
		var manifest = ManifestSerializer.LoadFromFile(manifestPath);
		ManifestValidator.ValidateOrThrow(manifest);

		// Determine output path
		var outputPath = options.OutputPath ?? Path.Combine(
			Directory.GetParent(projectDirectory)?.FullName ?? projectDirectory,
			$"{manifest.Name}.poppy"
		);

		// Create temporary directory for metadata
		var tempDir = Path.Combine(Path.GetTempPath(), $"poppy-{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			// Create metadata directory
			var metadataDir = Path.Combine(tempDir, MetadataDir);
			Directory.CreateDirectory(metadataDir);

			// Write version file
			File.WriteAllText(Path.Combine(metadataDir, VersionFile), FormatVersion);

			// Calculate checksums if requested
			Dictionary<string, string>? checksums = null;
			if (options.CalculateChecksums) {
				checksums = CalculateChecksums(projectDirectory, options);
				WriteChecksumsFile(Path.Combine(metadataDir, ChecksumsFile), checksums);
			}

			// Create build info
			WriteBuildInfo(Path.Combine(metadataDir, BuildInfoFile), manifest);

			// Create archive
			if (File.Exists(outputPath)) {
				File.Delete(outputPath);
			}

			using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);

			// Add all files from project directory
			AddDirectoryToArchive(archive, projectDirectory, "", options);

			// Add metadata files
			AddDirectoryToArchive(archive, metadataDir, MetadataDir, new PackOptions());

			return outputPath;
		} finally {
			// Clean up temp directory
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	/// <summary>
	/// Unpacks a .poppy archive to a directory.
	/// </summary>
	/// <param name="archivePath">Path to the .poppy archive.</param>
	/// <param name="options">Unpacking options.</param>
	/// <returns>Path to the extracted directory.</returns>
	/// <exception cref="InvalidOperationException">Thrown if unpacking fails.</exception>
	public static string Unpack(string archivePath, UnpackOptions? options = null) {
		options ??= new UnpackOptions();

		if (!File.Exists(archivePath)) {
			throw new FileNotFoundException($"Archive not found: {archivePath}");
		}

		// Determine target directory
		var targetDir = options.TargetDirectory ?? Path.Combine(
			Directory.GetParent(archivePath)?.FullName ?? Path.GetDirectoryName(archivePath)!,
			Path.GetFileNameWithoutExtension(archivePath)
		);

		// Check if directory exists
		if (Directory.Exists(targetDir) && !options.Overwrite) {
			throw new InvalidOperationException($"Target directory already exists: {targetDir}. Use --overwrite to replace.");
		}

		// Extract archive
		if (Directory.Exists(targetDir) && options.Overwrite) {
			Directory.Delete(targetDir, true);
		}
		Directory.CreateDirectory(targetDir);

		ZipFile.ExtractToDirectory(archivePath, targetDir, true);

		// Validate manifest if requested
		if (options.ValidateManifest) {
			var manifestPath = Path.Combine(targetDir, ManifestFileName);
			if (!File.Exists(manifestPath)) {
				throw new FileNotFoundException($"Archive does not contain {ManifestFileName}");
			}

			var manifest = ManifestSerializer.LoadFromFile(manifestPath);
			ManifestValidator.ValidateOrThrow(manifest);
		}

		// Validate checksums if requested
		if (options.ValidateChecksums) {
			var checksumsPath = Path.Combine(targetDir, MetadataDir, ChecksumsFile);
			if (File.Exists(checksumsPath)) {
				ValidateChecksums(targetDir, checksumsPath);
			}
		}

		return targetDir;
	}

	/// <summary>
	/// Validates an archive without extracting it.
	/// </summary>
	/// <param name="archivePath">Path to the .poppy archive.</param>
	/// <returns>List of validation errors (empty if valid).</returns>
	public static List<string> Validate(string archivePath) {
		var errors = new List<string>();

		if (!File.Exists(archivePath)) {
			errors.Add($"Archive not found: {archivePath}");
			return errors;
		}

		try {
			using var archive = ZipFile.OpenRead(archivePath);

			// Check for manifest
			var manifestEntry = archive.GetEntry(ManifestFileName);
			if (manifestEntry == null) {
				errors.Add($"Archive does not contain {ManifestFileName}");
				return errors;
			}

			// Validate manifest content
			using var manifestStream = manifestEntry.Open();
			using var reader = new StreamReader(manifestStream);
			var manifestJson = reader.ReadToEnd();

			try {
				var manifest = ManifestSerializer.Deserialize(manifestJson);
				var validationErrors = ManifestValidator.Validate(manifest);
				errors.AddRange(validationErrors);
			} catch (Exception ex) {
				errors.Add($"Invalid manifest JSON: {ex.Message}");
			}

			// Check for version file
			var versionEntry = archive.GetEntry($"{MetadataDir}/{VersionFile}");
			if (versionEntry == null) {
				errors.Add("Archive missing version file");
			}
		} catch (Exception ex) {
			errors.Add($"Failed to open archive: {ex.Message}");
		}

		return errors;
	}

	/// <summary>
	/// Calculates SHA256 checksums for all files in a directory.
	/// </summary>
	private static Dictionary<string, string> CalculateChecksums(string directory, PackOptions options) {
		var checksums = new Dictionary<string, string>();
		var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

		foreach (var file in files) {
			var relativePath = Path.GetRelativePath(directory, file).Replace('\\', '/');

			// Skip excluded files
			if (ShouldExclude(relativePath, options)) {
				continue;
			}

			var checksum = CalculateFileChecksum(file);
			checksums[relativePath] = checksum;
		}

		return checksums;
	}

	/// <summary>
	/// Calculates SHA256 checksum for a single file.
	/// </summary>
	private static string CalculateFileChecksum(string filePath) {
		using var sha256 = SHA256.Create();
		using var stream = File.OpenRead(filePath);
		var hash = sha256.ComputeHash(stream);
		return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
	}

	/// <summary>
	/// Writes checksums to a file.
	/// </summary>
	private static void WriteChecksumsFile(string filePath, Dictionary<string, string> checksums) {
		var lines = checksums.Select(kvp => $"SHA256:{kvp.Key}:{kvp.Value}");
		File.WriteAllLines(filePath, lines);
	}

	/// <summary>
	/// Writes build information to a JSON file.
	/// </summary>
	private static void WriteBuildInfo(string filePath, ProjectManifest manifest) {
		var buildInfo = new {
			poppyVersion = "1.0.0",  // TODO: Get from assembly version
			buildDate = DateTime.UtcNow.ToString("o"),
			platform = manifest.Platform,
			builder = "Poppy Compiler 1.0.0"
		};

		var json = System.Text.Json.JsonSerializer.Serialize(buildInfo, new System.Text.Json.JsonSerializerOptions {
			WriteIndented = true
		});

		File.WriteAllText(filePath, json);
	}

	/// <summary>
	/// Validates checksums after extraction.
	/// </summary>
	private static void ValidateChecksums(string directory, string checksumsPath) {
		var lines = File.ReadAllLines(checksumsPath);
		var errors = new List<string>();

		foreach (var line in lines) {
			var parts = line.Split(':');
			if (parts.Length != 3) continue;

			var algorithm = parts[0];
			var relativePath = parts[1];
			var expectedChecksum = parts[2];

			var filePath = Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
			if (!File.Exists(filePath)) {
				errors.Add($"Missing file: {relativePath}");
				continue;
			}

			var actualChecksum = CalculateFileChecksum(filePath);
			if (!actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase)) {
				errors.Add($"Checksum mismatch: {relativePath}");
			}
		}

		if (errors.Count > 0) {
			throw new InvalidOperationException($"Checksum validation failed:\n- {string.Join("\n- ", errors)}");
		}
	}

	/// <summary>
	/// Adds a directory recursively to a ZIP archive.
	/// </summary>
	private static void AddDirectoryToArchive(ZipArchive archive, string sourceDir, string entryPrefix, PackOptions options) {
		var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);

		foreach (var file in files) {
			var relativePath = Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
			var entryName = string.IsNullOrEmpty(entryPrefix) ? relativePath : $"{entryPrefix}/{relativePath}";

			// Skip excluded files
			if (ShouldExclude(relativePath, options)) {
				continue;
			}

			archive.CreateEntryFromFile(file, entryName, options.CompressionLevel);
		}
	}

	/// <summary>
	/// Checks if a file should be excluded based on patterns.
	/// </summary>
	private static bool ShouldExclude(string relativePath, PackOptions options) {
		// Always exclude .poppy metadata directory from source
		if (relativePath.StartsWith($"{MetadataDir}/", StringComparison.OrdinalIgnoreCase)) {
			return true;
		}

		// Exclude build directory if not requested
		if (!options.IncludeBuild && relativePath.StartsWith("build/", StringComparison.OrdinalIgnoreCase)) {
			return true;
		}

		// Check against exclude patterns (simple glob matching)
		foreach (var pattern in options.ExcludePatterns) {
			if (MatchesGlobPattern(relativePath, pattern)) {
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Simple glob pattern matching.
	/// </summary>
	private static bool MatchesGlobPattern(string path, string pattern) {
		// Simple implementation - supports ** and *
		pattern = pattern.Replace('\\', '/');
		path = path.Replace('\\', '/');

		if (pattern.Contains("**")) {
			var parts = pattern.Split("**");
			return path.Contains(parts[0], StringComparison.OrdinalIgnoreCase);
		}

		if (pattern.Contains("*")) {
			var parts = pattern.Split('*');
			var index = 0;
			foreach (var part in parts) {
				if (string.IsNullOrEmpty(part)) continue;
				index = path.IndexOf(part, index, StringComparison.OrdinalIgnoreCase);
				if (index == -1) return false;
				index += part.Length;
			}
			return true;
		}

		return path.Equals(pattern, StringComparison.OrdinalIgnoreCase);
	}
}
