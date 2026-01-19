// ============================================================================
// BaseConverter.cs - Base Class for Project Converters
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text;
using System.Text.RegularExpressions;

namespace Poppy.Core.Converters;

/// <summary>
/// Base class providing common functionality for assembly project converters.
/// </summary>
public abstract partial class BaseConverter : IProjectConverter {
	/// <inheritdoc />
	public abstract string SourceAssembler { get; }

	/// <inheritdoc />
	public abstract IReadOnlyList<string> SupportedExtensions { get; }

	/// <summary>
	/// Gets the directive mapping for this converter's source assembler.
	/// </summary>
	protected IReadOnlyDictionary<string, string> DirectiveMap =>
		DirectiveMapping.GetMapping(SourceAssembler);

	/// <summary>
	/// Gets the unsupported directives for this converter's source assembler.
	/// </summary>
	protected IReadOnlySet<string> UnsupportedDirectives =>
		DirectiveMapping.GetUnsupported(SourceAssembler);

	/// <inheritdoc />
	public virtual bool CanConvert(string filePath) {
		var extension = Path.GetExtension(filePath);
		return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
	}

	/// <inheritdoc />
	public virtual ConversionResult ConvertFile(string sourceFile, ConversionOptions options) {
		var result = new ConversionResult {
			SourcePath = sourceFile,
			OutputPath = GetOutputPath(sourceFile)
		};

		try {
			if (!File.Exists(sourceFile)) {
				result.Errors.Add(new ConversionMessage {
					FilePath = sourceFile,
					Line = 0,
					Column = 0,
					Code = "CONV001",
					Message = "Source file not found",
					Severity = MessageSeverity.Error
				});
				return result with { Success = false };
			}

			var lines = File.ReadAllLines(sourceFile);
			var output = new StringBuilder();

			// Add header comment
			output.AppendLine($"; Converted from {SourceAssembler} to PASM by Poppy");
			output.AppendLine($"; Original file: {Path.GetFileName(sourceFile)}");
			output.AppendLine($"; Conversion date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
			output.AppendLine();

			for (int i = 0; i < lines.Length; i++) {
				var lineNumber = i + 1;
				var line = lines[i];

				var convertedLine = ConvertLine(line, lineNumber, sourceFile, result, options);
				output.AppendLine(convertedLine);

				// Track referenced files
				var reference = ExtractFileReference(convertedLine);
				if (reference is not null) {
					result.ReferencedFiles.Add(reference);
				}
			}

			return result with {
				Success = result.Errors.Count == 0,
				Content = output.ToString()
			};
		}
		catch (Exception ex) {
			result.Errors.Add(new ConversionMessage {
				FilePath = sourceFile,
				Line = 0,
				Column = 0,
				Code = "CONV999",
				Message = $"Unexpected error: {ex.Message}",
				Severity = MessageSeverity.Error
			});
			return result with { Success = false };
		}
	}

	/// <inheritdoc />
	public virtual ProjectConversionResult ConvertProject(
		string sourceDirectory,
		string outputDirectory,
		ConversionOptions options) {
		var result = new ProjectConversionResult();

		try {
			if (!Directory.Exists(sourceDirectory)) {
				var error = new ConversionResult {
					SourcePath = sourceDirectory,
					Success = false
				};
				error.Errors.Add(new ConversionMessage {
					FilePath = sourceDirectory,
					Code = "CONV002",
					Message = "Source directory not found",
					Severity = MessageSeverity.Error
				});
				result.FileResults.Add(error);
				return result with { Success = false };
			}

			// Create output directory
			Directory.CreateDirectory(outputDirectory);

			// Find all source files
			var sourceFiles = FindSourceFiles(sourceDirectory);
			options.LogWriter?.WriteLine($"Found {sourceFiles.Count} source files to convert");

			foreach (var sourceFile in sourceFiles) {
				var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
				var outputFile = Path.Combine(
					outputDirectory,
					Path.ChangeExtension(relativePath, ".pasm"));

				options.LogWriter?.WriteLine($"Converting: {relativePath}");

				// Ensure output subdirectory exists
				var outputSubDir = Path.GetDirectoryName(outputFile);
				if (!string.IsNullOrEmpty(outputSubDir)) {
					Directory.CreateDirectory(outputSubDir);
				}

				var fileResult = ConvertFile(sourceFile, options);
				fileResult = fileResult with { OutputPath = outputFile };

				if (fileResult.Success) {
					File.WriteAllText(outputFile, fileResult.Content);
				}

				result.FileResults.Add(fileResult);
			}

			// Generate build script if requested
			if (options.GenerateBuildScript) {
				result = result with {
					BuildScript = GenerateBuildScript(outputDirectory, sourceFiles, options)
				};

				var buildScriptPath = Path.Combine(outputDirectory, "build.pproj");
				if (result.BuildScript is not null) {
					File.WriteAllText(buildScriptPath, result.BuildScript);
					options.LogWriter?.WriteLine($"Generated build script: {buildScriptPath}");
				}
			}

			return result with {
				Success = result.FailedFiles == 0
			};
		}
		catch (Exception ex) {
			var error = new ConversionResult {
				SourcePath = sourceDirectory,
				Success = false
			};
			error.Errors.Add(new ConversionMessage {
				FilePath = sourceDirectory,
				Code = "CONV999",
				Message = $"Unexpected error: {ex.Message}",
				Severity = MessageSeverity.Error
			});
			result.FileResults.Add(error);
			return result with { Success = false };
		}
	}

	// ========================================================================
	// Abstract Methods - Must be implemented by derived classes
	// ========================================================================

	/// <summary>
	/// Converts a single line of source code.
	/// </summary>
	/// <param name="line">The source line.</param>
	/// <param name="lineNumber">The 1-based line number.</param>
	/// <param name="filePath">The source file path.</param>
	/// <param name="result">The conversion result to add warnings/errors to.</param>
	/// <param name="options">Conversion options.</param>
	/// <returns>The converted line.</returns>
	protected abstract string ConvertLine(
		string line,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options);

	// ========================================================================
	// Protected Helper Methods
	// ========================================================================

	/// <summary>
	/// Translates a directive to its PASM equivalent.
	/// </summary>
	/// <param name="directive">The source directive.</param>
	/// <param name="lineNumber">The 1-based line number.</param>
	/// <param name="filePath">The source file path.</param>
	/// <param name="result">The conversion result to add warnings to.</param>
	/// <param name="options">Conversion options.</param>
	/// <returns>The translated directive, or the original if no translation exists.</returns>
	protected string TranslateDirective(
		string directive,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		// Check if explicitly unsupported
		if (UnsupportedDirectives.Contains(directive)) {
			if (options.WarnOnUnsupportedFeatures) {
				result.Warnings.Add(new ConversionMessage {
					FilePath = filePath,
					Line = lineNumber,
					Code = "CONV100",
					Message = $"Directive '{directive}' is not supported in PASM",
					Severity = MessageSeverity.Warning
				});
			}
			return $"; UNSUPPORTED: {directive}";
		}

		// Try to translate
		if (DirectiveMap.TryGetValue(directive, out var pasmDirective)) {
			return pasmDirective!;
		}

		// Check custom mappings
		if (options.CustomDirectiveMappings.TryGetValue(directive, out var customDirective)) {
			return customDirective;
		}

		// Unknown directive - pass through with warning
		if (options.WarnOnUnsupportedFeatures) {
			result.Warnings.Add(new ConversionMessage {
				FilePath = filePath,
				Line = lineNumber,
				Code = "CONV101",
				Message = $"Unknown directive '{directive}' - passing through unchanged",
				Severity = MessageSeverity.Warning
			});
		}

		return directive;
	}

	/// <summary>
	/// Converts local label syntax.
	/// </summary>
	/// <param name="label">The local label.</param>
	/// <returns>The converted local label in PASM format.</returns>
	protected virtual string ConvertLocalLabel(string label) {
		// Most assemblers use . for local labels, which PASM also uses
		if (label.StartsWith('.')) {
			return label;
		}

		// ca65 uses @ for local labels
		if (label.StartsWith('@')) {
			return "." + label[1..];
		}

		// ASAR/xkas use + and - for anonymous labels
		if (label is "+" or "-") {
			return label; // PASM supports these
		}

		return label;
	}

	/// <summary>
	/// Extracts a file reference from a line (include/incbin).
	/// </summary>
	/// <param name="line">The line to check.</param>
	/// <returns>The referenced file path, or null if none.</returns>
	protected virtual string? ExtractFileReference(string line) {
		var match = IncludePattern().Match(line);
		if (match.Success) {
			return match.Groups[1].Value;
		}
		return null;
	}

	/// <summary>
	/// Finds all source files in a directory.
	/// </summary>
	/// <param name="directory">The directory to search.</param>
	/// <returns>List of source file paths.</returns>
	protected virtual List<string> FindSourceFiles(string directory) {
		var files = new List<string>();

		foreach (var extension in SupportedExtensions) {
			files.AddRange(Directory.GetFiles(
				directory,
				$"*{extension}",
				SearchOption.AllDirectories));
		}

		return [.. files.OrderBy(f => f)];
	}

	/// <summary>
	/// Generates an output file path for a source file.
	/// </summary>
	/// <param name="sourcePath">The source file path.</param>
	/// <returns>The output file path with .pasm extension.</returns>
	protected virtual string GetOutputPath(string sourcePath) {
		return Path.ChangeExtension(sourcePath, ".pasm");
	}

	/// <summary>
	/// Generates a build script for the converted project.
	/// </summary>
	/// <param name="outputDirectory">The output directory.</param>
	/// <param name="sourceFiles">The list of source files.</param>
	/// <param name="options">Conversion options.</param>
	/// <returns>The build script content.</returns>
	protected virtual string GenerateBuildScript(
		string outputDirectory,
		List<string> sourceFiles,
		ConversionOptions options) {
		var sb = new StringBuilder();

		sb.AppendLine("; Poppy Project File");
		sb.AppendLine($"; Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
		sb.AppendLine($"; Converted from: {SourceAssembler}");
		sb.AppendLine();

		sb.AppendLine("[project]");
		sb.AppendLine($"name = \"{Path.GetFileName(outputDirectory)}\"");
		sb.AppendLine($"version = \"1.0.0\"");
		sb.AppendLine();

		sb.AppendLine("[build]");
		if (options.TargetArchitecture is not null) {
			sb.AppendLine($"arch = \"{options.TargetArchitecture}\"");
		}

		// Find main file (usually named main.asm or similar)
		var mainFile = sourceFiles.FirstOrDefault(f =>
			Path.GetFileNameWithoutExtension(f).Equals("main", StringComparison.OrdinalIgnoreCase))
			?? sourceFiles.FirstOrDefault();

		if (mainFile is not null) {
			var mainPasm = Path.ChangeExtension(
				Path.GetFileName(mainFile),
				".pasm");
			sb.AppendLine($"main = \"{mainPasm}\"");
		}

		sb.AppendLine("output = \"output.rom\"");
		sb.AppendLine();

		return sb.ToString();
	}

	// ========================================================================
	// Regex Patterns
	// ========================================================================

	[GeneratedRegex(@"(?:include|incbin|incsrc)\s+""?([^""]+)""?", RegexOptions.IgnoreCase)]
	private static partial Regex IncludePattern();
}
