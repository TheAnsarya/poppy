// ============================================================================
// IProjectConverter.cs - Project Converter Interface
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.Converters;

/// <summary>
/// Defines the interface for assembly project converters.
/// Converters transform projects from other assemblers (ASAR, ca65, xkas)
/// to Poppy's PASM format.
/// </summary>
public interface IProjectConverter {
	/// <summary>
	/// Gets the name of the source assembler.
	/// </summary>
	string SourceAssembler { get; }

	/// <summary>
	/// Gets the file extensions supported by this converter.
	/// </summary>
	IReadOnlyList<string> SupportedExtensions { get; }

	/// <summary>
	/// Converts a single source file to PASM format.
	/// </summary>
	/// <param name="sourceFile">Path to the source file.</param>
	/// <param name="options">Conversion options.</param>
	/// <returns>The conversion result containing PASM content.</returns>
	ConversionResult ConvertFile(string sourceFile, ConversionOptions options);

	/// <summary>
	/// Converts an entire project directory to PASM format.
	/// </summary>
	/// <param name="sourceDirectory">Path to the source project directory.</param>
	/// <param name="outputDirectory">Path to the output directory.</param>
	/// <param name="options">Conversion options.</param>
	/// <returns>The project conversion result.</returns>
	ProjectConversionResult ConvertProject(
		string sourceDirectory,
		string outputDirectory,
		ConversionOptions options);

	/// <summary>
	/// Validates whether a file can be converted by this converter.
	/// </summary>
	/// <param name="filePath">Path to the file to validate.</param>
	/// <returns>True if the file can be converted.</returns>
	bool CanConvert(string filePath);
}

/// <summary>
/// Options for assembly project conversion.
/// </summary>
public sealed class ConversionOptions {
	/// <summary>
	/// Gets or sets whether to preserve original comments.
	/// </summary>
	public bool PreserveComments { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to preserve original formatting where possible.
	/// </summary>
	public bool PreserveFormatting { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to convert local labels to Poppy's format.
	/// </summary>
	public bool ConvertLocalLabels { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to expand macros during conversion.
	/// </summary>
	public bool ExpandMacros { get; set; } = false;

	/// <summary>
	/// Gets or sets whether to generate a build script.
	/// </summary>
	public bool GenerateBuildScript { get; set; } = true;

	/// <summary>
	/// Gets or sets the target architecture (e.g., "6502", "65816", "z80").
	/// </summary>
	public string? TargetArchitecture { get; set; }

	/// <summary>
	/// Gets or sets custom directive mappings.
	/// Key: source directive, Value: PASM directive.
	/// </summary>
	public Dictionary<string, string> CustomDirectiveMappings { get; } = [];

	/// <summary>
	/// Gets or sets whether to emit warnings for unsupported features.
	/// </summary>
	public bool WarnOnUnsupportedFeatures { get; set; } = true;

	/// <summary>
	/// Gets or sets the log output writer.
	/// </summary>
	public TextWriter? LogWriter { get; set; }
}

/// <summary>
/// Result of converting a single file.
/// </summary>
public sealed record ConversionResult {
	/// <summary>
	/// Gets or sets whether the conversion was successful.
	/// </summary>
	public bool Success { get; init; }

	/// <summary>
	/// Gets or sets the converted PASM content.
	/// </summary>
	public string Content { get; init; } = string.Empty;

	/// <summary>
	/// Gets the source file path.
	/// </summary>
	public string SourcePath { get; init; } = string.Empty;

	/// <summary>
	/// Gets the output file path.
	/// </summary>
	public string OutputPath { get; init; } = string.Empty;

	/// <summary>
	/// Gets the list of warnings generated during conversion.
	/// </summary>
	public List<ConversionMessage> Warnings { get; init; } = [];

	/// <summary>
	/// Gets the list of errors encountered during conversion.
	/// </summary>
	public List<ConversionMessage> Errors { get; init; } = [];

	/// <summary>
	/// Gets the list of referenced files (includes, incbins).
	/// </summary>
	public List<string> ReferencedFiles { get; init; } = [];
}

/// <summary>
/// Result of converting an entire project.
/// </summary>
public sealed record ProjectConversionResult {
	/// <summary>
	/// Gets or sets whether the overall conversion was successful.
	/// </summary>
	public bool Success { get; init; }

	/// <summary>
	/// Gets the results for individual files.
	/// </summary>
	public List<ConversionResult> FileResults { get; init; } = [];

	/// <summary>
	/// Gets the total number of files processed.
	/// </summary>
	public int TotalFiles => FileResults.Count;

	/// <summary>
	/// Gets the number of successfully converted files.
	/// </summary>
	public int SuccessfulFiles => FileResults.Count(r => r.Success);

	/// <summary>
	/// Gets the number of failed files.
	/// </summary>
	public int FailedFiles => FileResults.Count(r => !r.Success);

	/// <summary>
	/// Gets all warnings across all files.
	/// </summary>
	public IEnumerable<ConversionMessage> AllWarnings =>
		FileResults.SelectMany(r => r.Warnings);

	/// <summary>
	/// Gets all errors across all files.
	/// </summary>
	public IEnumerable<ConversionMessage> AllErrors =>
		FileResults.SelectMany(r => r.Errors);

	/// <summary>
	/// Gets the generated build script content, if any.
	/// </summary>
	public string? BuildScript { get; init; }

	/// <summary>
	/// Gets the generated project file content, if any.
	/// </summary>
	public string? ProjectFile { get; init; }
}

/// <summary>
/// A message (warning or error) from the conversion process.
/// </summary>
public sealed class ConversionMessage {
	/// <summary>
	/// Gets the file path where the message originated.
	/// </summary>
	public string FilePath { get; init; } = string.Empty;

	/// <summary>
	/// Gets the line number (1-based) where the issue was found.
	/// </summary>
	public int Line { get; init; }

	/// <summary>
	/// Gets the column number (1-based) where the issue was found.
	/// </summary>
	public int Column { get; init; }

	/// <summary>
	/// Gets the message code (e.g., "CONV001").
	/// </summary>
	public string Code { get; init; } = string.Empty;

	/// <summary>
	/// Gets the message text.
	/// </summary>
	public string Message { get; init; } = string.Empty;

	/// <summary>
	/// Gets the severity of the message.
	/// </summary>
	public MessageSeverity Severity { get; init; }

	/// <summary>
	/// Gets the original source line, if available.
	/// </summary>
	public string? SourceLine { get; init; }

	/// <inheritdoc />
	public override string ToString() =>
		$"{FilePath}({Line},{Column}): {Severity.ToString().ToLowerInvariant()} {Code}: {Message}";
}

/// <summary>
/// Severity levels for conversion messages.
/// </summary>
public enum MessageSeverity {
	/// <summary>Informational message.</summary>
	Info,

	/// <summary>Warning that doesn't prevent conversion.</summary>
	Warning,

	/// <summary>Error that prevents conversion.</summary>
	Error
}
