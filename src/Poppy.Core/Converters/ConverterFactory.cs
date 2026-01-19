// ============================================================================
// ConverterFactory.cs - Factory for Project Converters
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.Converters;

/// <summary>
/// Factory for creating project converters.
/// </summary>
public static class ConverterFactory {
	/// <summary>
	/// Registered converters by assembler name.
	/// </summary>
	private static readonly Dictionary<string, Func<IProjectConverter>> _converters = new(StringComparer.OrdinalIgnoreCase) {
		["ASAR"] = () => new AsarConverter(),
		["CA65"] = () => new Ca65Converter(),
		["XKAS"] = () => new XkasConverter(),
	};

	/// <summary>
	/// Gets a list of all supported source assemblers.
	/// </summary>
	public static IReadOnlyList<string> SupportedAssemblers =>
		[.. _converters.Keys.OrderBy(k => k)];

	/// <summary>
	/// Creates a converter for the specified source assembler.
	/// </summary>
	/// <param name="assembler">The source assembler name (e.g., "ASAR", "ca65", "xkas").</param>
	/// <returns>The project converter.</returns>
	/// <exception cref="ArgumentException">Thrown when the assembler is not supported.</exception>
	public static IProjectConverter Create(string assembler) {
		if (_converters.TryGetValue(assembler, out var factory)) {
			return factory();
		}

		throw new ArgumentException(
			$"Unknown assembler: {assembler}. Supported assemblers: {string.Join(", ", SupportedAssemblers)}",
			nameof(assembler));
	}

	/// <summary>
	/// Attempts to create a converter for the specified source assembler.
	/// </summary>
	/// <param name="assembler">The source assembler name.</param>
	/// <param name="converter">The created converter, or null if not supported.</param>
	/// <returns>True if the converter was created.</returns>
	public static bool TryCreate(string assembler, out IProjectConverter? converter) {
		if (_converters.TryGetValue(assembler, out var factory)) {
			converter = factory();
			return true;
		}

		converter = null;
		return false;
	}

	/// <summary>
	/// Detects the most likely source assembler for a file based on its content.
	/// </summary>
	/// <param name="filePath">The path to the file to analyze.</param>
	/// <returns>The detected assembler name, or null if unknown.</returns>
	public static string? DetectAssembler(string filePath) {
		if (!File.Exists(filePath)) {
			return null;
		}

		var content = File.ReadAllText(filePath);

		// ca65 indicators: .byte, .word, .segment, .include (with dot prefix)
		if (content.Contains(".byte", StringComparison.OrdinalIgnoreCase) ||
			content.Contains(".word", StringComparison.OrdinalIgnoreCase) ||
			content.Contains(".segment", StringComparison.OrdinalIgnoreCase) ||
			content.Contains(".proc", StringComparison.OrdinalIgnoreCase)) {
			return "CA65";
		}

		// ASAR indicators: freecode, freedata, hirom/lorom without dot
		if (content.Contains("freecode", StringComparison.OrdinalIgnoreCase) ||
			content.Contains("freedata", StringComparison.OrdinalIgnoreCase) ||
			(content.Contains("lorom", StringComparison.OrdinalIgnoreCase) &&
			 !content.Contains(".lorom", StringComparison.OrdinalIgnoreCase))) {
			return "ASAR";
		}

		// xkas indicators: limited feature set, similar to ASAR but older
		// Hard to distinguish from ASAR, default to ASAR for ambiguous cases
		if (content.Contains("arch 65816", StringComparison.OrdinalIgnoreCase) ||
			content.Contains("arch spc700", StringComparison.OrdinalIgnoreCase)) {
			// Could be either, check for ASAR-specific features
			if (content.Contains("namespace", StringComparison.OrdinalIgnoreCase) ||
				content.Contains("assert", StringComparison.OrdinalIgnoreCase)) {
				return "ASAR";
			}
			return "XKAS";
		}

		// Default to ASAR for .asm files
		var extension = Path.GetExtension(filePath);
		if (extension.Equals(".asm", StringComparison.OrdinalIgnoreCase)) {
			return "ASAR";
		}

		// Default to ca65 for .s files
		if (extension.Equals(".s", StringComparison.OrdinalIgnoreCase)) {
			return "CA65";
		}

		return null;
	}

	/// <summary>
	/// Detects the most likely source assembler for a project based on its files.
	/// </summary>
	/// <param name="projectDirectory">The project directory to analyze.</param>
	/// <returns>The detected assembler name, or null if unknown.</returns>
	public static string? DetectProjectAssembler(string projectDirectory) {
		if (!Directory.Exists(projectDirectory)) {
			return null;
		}

		// Count indicators for each assembler
		var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
			["ASAR"] = 0,
			["CA65"] = 0,
			["XKAS"] = 0
		};

		// Check for linker configuration (ld65/ca65)
		var linkerConfigs = Directory.GetFiles(projectDirectory, "*.cfg", SearchOption.AllDirectories);
		if (linkerConfigs.Length > 0) {
			scores["CA65"] += 10;
		}

		// Check for make files
		var makefiles = Directory.GetFiles(projectDirectory, "Makefile*", SearchOption.TopDirectoryOnly);
		foreach (var makefile in makefiles) {
			var content = File.ReadAllText(makefile);
			if (content.Contains("ca65", StringComparison.OrdinalIgnoreCase) ||
				content.Contains("ld65", StringComparison.OrdinalIgnoreCase)) {
				scores["CA65"] += 5;
			}
			if (content.Contains("asar", StringComparison.OrdinalIgnoreCase)) {
				scores["ASAR"] += 5;
			}
		}

		// Sample some assembly files
		var asmFiles = Directory.GetFiles(projectDirectory, "*.asm", SearchOption.AllDirectories)
			.Concat(Directory.GetFiles(projectDirectory, "*.s", SearchOption.AllDirectories))
			.Take(10);

		foreach (var file in asmFiles) {
			var detected = DetectAssembler(file);
			if (detected is not null) {
				scores[detected]++;
			}
		}

		// Return highest scoring assembler
		var winner = scores.MaxBy(kv => kv.Value);
		return winner.Value > 0 ? winner.Key : null;
	}

	/// <summary>
	/// Registers a custom converter.
	/// </summary>
	/// <param name="assembler">The assembler name.</param>
	/// <param name="factory">The factory function to create the converter.</param>
	public static void Register(string assembler, Func<IProjectConverter> factory) {
		_converters[assembler] = factory;
	}

	/// <summary>
	/// Unregisters a converter.
	/// </summary>
	/// <param name="assembler">The assembler name to remove.</param>
	/// <returns>True if the converter was removed.</returns>
	public static bool Unregister(string assembler) {
		return _converters.Remove(assembler);
	}
}
