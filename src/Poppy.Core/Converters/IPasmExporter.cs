// ============================================================================
// IPasmExporter.cs - PASM Export Interface
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.Converters;

/// <summary>
/// Defines the interface for PASM exporters that convert PASM source files
/// to other assembler formats (ASAR, ca65, xkas).
/// </summary>
public interface IPasmExporter {
	/// <summary>
	/// Gets the name of the target assembler.
	/// </summary>
	string TargetAssembler { get; }

	/// <summary>
	/// Gets the default file extension for the target format.
	/// </summary>
	string DefaultExtension { get; }

	/// <summary>
	/// Exports a single PASM source file to the target assembler format.
	/// </summary>
	/// <param name="sourceFile">Path to the PASM source file.</param>
	/// <param name="options">Conversion options.</param>
	/// <returns>The conversion result containing target format content.</returns>
	ConversionResult ExportFile(string sourceFile, ConversionOptions options);

	/// <summary>
	/// Exports an entire PASM project directory to the target assembler format.
	/// </summary>
	/// <param name="sourceDirectory">Path to the source PASM project directory.</param>
	/// <param name="outputDirectory">Path to the output directory.</param>
	/// <param name="options">Conversion options.</param>
	/// <returns>The project conversion result.</returns>
	ProjectConversionResult ExportProject(
		string sourceDirectory,
		string outputDirectory,
		ConversionOptions options);

	/// <summary>
	/// Validates whether a file can be exported by this exporter.
	/// </summary>
	/// <param name="filePath">Path to the file to validate.</param>
	/// <returns>True if the file can be exported.</returns>
	bool CanExport(string filePath);
}
