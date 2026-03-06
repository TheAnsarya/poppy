// ============================================================================
// PasmToCa65Exporter.cs - PASM to ca65 Exporter
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text.RegularExpressions;

namespace Poppy.Core.Converters;

/// <summary>
/// Exports PASM source files to ca65 format.
/// ca65 uses dot-prefixed directives (.byte, .word, .include) and @-prefixed local labels.
/// </summary>
public sealed partial class PasmToCa65Exporter : BaseExporter {
	/// <inheritdoc />
	public override string TargetAssembler => "CA65";

	/// <inheritdoc />
	public override string DefaultExtension => ".s";

	/// <inheritdoc />
	protected override IReadOnlyDictionary<string, string> ReverseDirectiveMap =>
		DirectiveMapping.PasmToCa65;

	/// <inheritdoc />
	protected override string ConvertLocalLabel(string label) {
		// ca65 uses @ for local labels instead of .
		if (label.StartsWith('.') && !label.StartsWith("..")) {
			return "@" + label[1..];
		}
		return label;
	}

	/// <inheritdoc />
	protected override string ExportLine(
		string line,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		if (string.IsNullOrWhiteSpace(line)) {
			return line;
		}

		var whitespace = GetLeadingWhitespace(line);
		var trimmed = line.TrimStart();

		// Full-line comment — pass through (ca65 also uses ;)
		if (trimmed.StartsWith(';')) {
			return line;
		}

		var (code, comment) = SplitCodeAndComment(trimmed);

		var converted = ConvertCode(code.Trim(), lineNumber, filePath, result, options);

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

			// Convert local labels: .label: → @label:
			label = ConvertLabelName(label);

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

			// Special case: include → .include (change .pasm to .s)
			if (directive.Equals("include", StringComparison.OrdinalIgnoreCase)) {
				args = ConvertIncludeExtension(args);
				return $"{translated} {args}";
			}

			// Special case: arch → .p02/.p816
			if (directive.Equals("arch", StringComparison.OrdinalIgnoreCase)) {
				return ConvertArchDirective(args);
			}

			// Convert local label references in arguments
			args = ConvertLocalLabelReferences(args);

			return $"{translated} {args}";
		}

		// Standalone directive
		var aloneMatch = DirectiveAlonePattern().Match(code);
		if (aloneMatch.Success) {
			var directive = aloneMatch.Groups[1].Value;
			var translated = TranslateDirective(directive, lineNumber, filePath, result, options);
			return translated;
		}

		// Instructions — convert any local label references in operands
		var parts = code.Split([' ', '\t'], 2);
		if (parts.Length == 2) {
			var mnemonic = parts[0];
			var operand = ConvertLocalLabelReferences(parts[1]);
			return $"{mnemonic} {operand}";
		}

		return code;
	}

	/// <summary>
	/// Converts a label definition: .local: → @local:
	/// </summary>
	private static string ConvertLabelName(string label) {
		if (label.StartsWith('.') && label.EndsWith(':')) {
			return "@" + label[1..];
		}
		return label;
	}

	/// <summary>
	/// Converts local label references in arguments: .label → @label
	/// </summary>
	private static string ConvertLocalLabelReferences(string args) {
		return Ca65LocalLabelPattern().Replace(args, m => {
			var name = m.Groups[1].Value;
			return $"@{name}";
		});
	}

	private static string ConvertIncludeExtension(string args) {
		return PasmExtensionPattern().Replace(args, ".s");
	}

	/// <summary>
	/// Converts PASM arch directives to ca65 CPU directives.
	/// </summary>
	private static string ConvertArchDirective(string args) {
		return args.Trim().ToLowerInvariant() switch {
			"6502" => ".p02",
			"65816" or "65c816" => ".p816",
			"65c02" or "65sc02" => ".pc02",
			_ => $"; UNSUPPORTED arch: {args}"
		};
	}

	// Match .localLabel references (but not .. or directive-like patterns)
	[GeneratedRegex(@"(?<!\w)\.([a-zA-Z_]\w*)\b(?!:)")]
	private static partial Regex Ca65LocalLabelPattern();

	[GeneratedRegex(@"\.pasm\b", RegexOptions.IgnoreCase)]
	private static partial Regex PasmExtensionPattern();
}
