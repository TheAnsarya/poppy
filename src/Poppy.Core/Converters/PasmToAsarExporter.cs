// ============================================================================
// PasmToAsarExporter.cs - PASM to ASAR Exporter
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text.RegularExpressions;

namespace Poppy.Core.Converters;

/// <summary>
/// Exports PASM source files to ASAR format.
/// ASAR has very similar syntax to PASM, so most conversions are straightforward.
/// </summary>
public sealed partial class PasmToAsarExporter : BaseExporter {
	/// <inheritdoc />
	public override string TargetAssembler => "ASAR";

	/// <inheritdoc />
	public override string DefaultExtension => ".asm";

	/// <inheritdoc />
	protected override IReadOnlyDictionary<string, string> ReverseDirectiveMap =>
		DirectiveMapping.PasmToAsar;

	/// <inheritdoc />
	protected override string ExportLine(
		string line,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		// Empty lines pass through
		if (string.IsNullOrWhiteSpace(line)) {
			return line;
		}

		var whitespace = GetLeadingWhitespace(line);
		var trimmed = line.TrimStart();

		// Full-line comment — pass through (ASAR also uses ;)
		if (trimmed.StartsWith(';')) {
			return line;
		}

		// Split code and comment
		var (code, comment) = SplitCodeAndComment(trimmed);

		// Convert the code part
		var converted = ConvertCode(code.Trim(), lineNumber, filePath, result, options);

		// Reassemble with whitespace and comment
		if (comment is not null) {
			return $"{whitespace}{converted} {comment}";
		}

		return $"{whitespace}{converted}";
	}

	private string ConvertCode(
		string code,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		if (string.IsNullOrEmpty(code)) {
			return code;
		}

		// Check for label with optional following code
		var labelMatch = LabelPattern().Match(code);
		if (labelMatch.Success) {
			var label = labelMatch.Groups[2].Value;
			var rest = labelMatch.Groups[3].Value.Trim();

			if (string.IsNullOrEmpty(rest)) {
				return label;
			}

			var convertedRest = ConvertCode(rest, lineNumber, filePath, result, options);
			return $"{label} {convertedRest}";
		}

		// Check for directive with arguments
		var directiveMatch = DirectiveWithArgsPattern().Match(code);
		if (directiveMatch.Success) {
			var directive = directiveMatch.Groups[1].Value;
			var args = directiveMatch.Groups[2].Value;

			var translated = TranslateDirective(directive, lineNumber, filePath, result, options);

			// Special case: include → incsrc (change .pasm extension to .asm)
			if (directive.Equals("include", StringComparison.OrdinalIgnoreCase)) {
				args = ConvertIncludeExtension(args);
				return $"{translated} {args}";
			}

			// Special case: arch directives
			if (directive.Equals("arch", StringComparison.OrdinalIgnoreCase)) {
				return ConvertArchDirective(args);
			}

			return $"{translated} {args}";
		}

		// Standalone directive (no args)
		var aloneMatch = DirectiveAlonePattern().Match(code);
		if (aloneMatch.Success) {
			var directive = aloneMatch.Groups[1].Value;
			var translated = TranslateDirective(directive, lineNumber, filePath, result, options);
			return translated;
		}

		// Instructions and everything else — pass through (same syntax)
		return code;
	}

	/// <summary>
	/// Converts .pasm include references to .asm.
	/// </summary>
	private static string ConvertIncludeExtension(string args) {
		return PasmExtensionPattern().Replace(args, ".asm");
	}

	/// <summary>
	/// Converts PASM arch directives to ASAR format.
	/// ASAR uses implied architecture or specific names.
	/// </summary>
	private static string ConvertArchDirective(string args) {
		return args.Trim().ToLowerInvariant() switch {
			"6502" => "arch 65816", // ASAR defaults to 65816 mode
			"65816" => "arch 65816",
			"65c816" => "arch 65816",
			"spc700" => "arch spc700",
			"superfx" => "arch superfx",
			_ => $"arch {args}"
		};
	}

	[GeneratedRegex(@"\.pasm\b", RegexOptions.IgnoreCase)]
	private static partial Regex PasmExtensionPattern();
}
