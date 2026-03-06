// ============================================================================
// BaseExporter.cs - Base Class for PASM Exporters
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text;
using System.Text.RegularExpressions;

namespace Poppy.Core.Converters;

/// <summary>
/// Base class providing common functionality for PASM exporters.
/// Converts PASM source files to other assembler formats.
/// </summary>
public abstract partial class BaseExporter : IPasmExporter {
	/// <inheritdoc />
	public abstract string TargetAssembler { get; }

	/// <inheritdoc />
	public abstract string DefaultExtension { get; }

	/// <summary>
	/// Gets the reverse directive mapping (PASM → target assembler).
	/// </summary>
	protected abstract IReadOnlyDictionary<string, string> ReverseDirectiveMap { get; }

	/// <inheritdoc />
	public virtual bool CanExport(string filePath) {
		var extension = Path.GetExtension(filePath);
		return extension.Equals(".pasm", StringComparison.OrdinalIgnoreCase);
	}

	/// <inheritdoc />
	public virtual ConversionResult ExportFile(string sourceFile, ConversionOptions options) {
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
					Code = "EXP001",
					Message = "Source file not found",
					Severity = MessageSeverity.Error
				});
				return result with { Success = false };
			}

			var lines = File.ReadAllLines(sourceFile);
			var output = new StringBuilder();

			// Add header comment
			var commentPrefix = GetCommentPrefix();
			output.AppendLine($"{commentPrefix} Converted from PASM to {TargetAssembler} by Poppy");
			output.AppendLine($"{commentPrefix} Original file: {Path.GetFileName(sourceFile)}");
			output.AppendLine($"{commentPrefix} Conversion date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
			output.AppendLine();

			for (int i = 0; i < lines.Length; i++) {
				var lineNumber = i + 1;
				var line = lines[i];

				var convertedLine = ExportLine(line, lineNumber, sourceFile, result, options);
				output.AppendLine(convertedLine);
			}

			return result with {
				Success = result.Errors.Count == 0,
				Content = output.ToString()
			};
		} catch (Exception ex) {
			result.Errors.Add(new ConversionMessage {
				FilePath = sourceFile,
				Line = 0,
				Column = 0,
				Code = "EXP999",
				Message = $"Unexpected error: {ex.Message}",
				Severity = MessageSeverity.Error
			});
			return result with { Success = false };
		}
	}

	/// <inheritdoc />
	public virtual ProjectConversionResult ExportProject(
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
					Code = "EXP002",
					Message = "Source directory not found",
					Severity = MessageSeverity.Error
				});
				result.FileResults.Add(error);
				return result with { Success = false };
			}

			Directory.CreateDirectory(outputDirectory);

			var sourceFiles = Directory.GetFiles(sourceDirectory, "*.pasm", SearchOption.AllDirectories)
				.OrderBy(f => f)
				.ToList();

			options.LogWriter?.WriteLine($"Found {sourceFiles.Count} PASM files to export");

			foreach (var sourceFile in sourceFiles) {
				var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
				var outputFile = Path.Combine(
					outputDirectory,
					Path.ChangeExtension(relativePath, DefaultExtension));

				options.LogWriter?.WriteLine($"Exporting: {relativePath}");

				var outputSubDir = Path.GetDirectoryName(outputFile);
				if (!string.IsNullOrEmpty(outputSubDir)) {
					Directory.CreateDirectory(outputSubDir);
				}

				var fileResult = ExportFile(sourceFile, options);
				fileResult = fileResult with { OutputPath = outputFile };

				if (fileResult.Success) {
					File.WriteAllText(outputFile, fileResult.Content);
				}

				result.FileResults.Add(fileResult);
			}

			return result with {
				Success = result.FailedFiles == 0
			};
		} catch (Exception ex) {
			var error = new ConversionResult {
				SourcePath = sourceDirectory,
				Success = false
			};
			error.Errors.Add(new ConversionMessage {
				FilePath = sourceDirectory,
				Code = "EXP999",
				Message = $"Unexpected error: {ex.Message}",
				Severity = MessageSeverity.Error
			});
			result.FileResults.Add(error);
			return result with { Success = false };
		}
	}

	// ========================================================================
	// Abstract Methods
	// ========================================================================

	/// <summary>
	/// Exports a single line of PASM source code to the target format.
	/// </summary>
	protected abstract string ExportLine(
		string line,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options);

	// ========================================================================
	// Protected Helper Methods
	// ========================================================================

	/// <summary>
	/// Gets the comment prefix for the target assembler.
	/// </summary>
	protected virtual string GetCommentPrefix() => ";";

	/// <summary>
	/// Translates a PASM directive to the target assembler's equivalent.
	/// </summary>
	protected string TranslateDirective(
		string directive,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		if (ReverseDirectiveMap.TryGetValue(directive, out var targetDirective)) {
			return targetDirective!;
		}

		if (options.CustomDirectiveMappings.TryGetValue(directive, out var customDirective)) {
			return customDirective;
		}

		if (options.WarnOnUnsupportedFeatures) {
			result.Warnings.Add(new ConversionMessage {
				FilePath = filePath,
				Line = lineNumber,
				Code = "EXP100",
				Message = $"No mapping for PASM directive '{directive}' in {TargetAssembler}",
				Severity = MessageSeverity.Warning
			});
		}

		return directive;
	}

	/// <summary>
	/// Converts a PASM local label to the target assembler's format.
	/// </summary>
	protected virtual string ConvertLocalLabel(string label) => label;

	/// <summary>
	/// Gets the output file path for a PASM source file.
	/// </summary>
	protected virtual string GetOutputPath(string sourcePath) {
		return Path.ChangeExtension(sourcePath, DefaultExtension);
	}

	/// <summary>
	/// Gets leading whitespace from a line.
	/// </summary>
	protected static string GetLeadingWhitespace(string line) {
		int i = 0;
		while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) {
			i++;
		}
		return line[..i];
	}

	/// <summary>
	/// Splits a PASM line into code and comment parts.
	/// PASM always uses ; for comments.
	/// </summary>
	protected static (string code, string? comment) SplitCodeAndComment(string line) {
		bool inString = false;
		char stringChar = '\0';

		for (int i = 0; i < line.Length; i++) {
			var ch = line[i];

			if (inString) {
				if (ch == stringChar) {
					inString = false;
				}
				continue;
			}

			if (ch is '"' or '\'') {
				inString = true;
				stringChar = ch;
				continue;
			}

			if (ch == ';') {
				var code = line[..i].TrimEnd();
				var comment = line[i..]; // includes the ;
				return (code, comment);
			}
		}

		return (line, null);
	}

	/// <summary>
	/// Converts a PASM comment to the target assembler's format.
	/// Default: keep as-is (;). Override for assemblers that use different comment syntax.
	/// </summary>
	protected virtual string ConvertComment(string comment) => comment;

	[GeneratedRegex(@"^(\s*)(\S+:)\s*(.*)$")]
	protected static partial Regex LabelPattern();

	[GeneratedRegex(@"^(\w+)\s+(.*)$")]
	protected static partial Regex DirectiveWithArgsPattern();

	[GeneratedRegex(@"^(\w+)$")]
	protected static partial Regex DirectiveAlonePattern();

	[GeneratedRegex(@"^include\s+""([^""]+)""", RegexOptions.IgnoreCase)]
	protected static partial Regex IncludePattern();

	[GeneratedRegex(@"^\.(\w+)", RegexOptions.IgnoreCase)]
	protected static partial Regex LocalLabelRefPattern();
}
