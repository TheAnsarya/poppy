// ============================================================================
// PasmToXkasExporter.cs - PASM to xkas Exporter
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.Converters;

/// <summary>
/// Exports PASM source files to xkas format.
/// xkas uses // for comments and has a limited directive set.
/// </summary>
public sealed partial class PasmToXkasExporter : BaseExporter {
	/// <inheritdoc />
	public override string TargetAssembler => "XKAS";

	/// <inheritdoc />
	public override string DefaultExtension => ".asm";

	/// <inheritdoc />
	protected override IReadOnlyDictionary<string, string> ReverseDirectiveMap =>
		DirectiveMapping.PasmToXkas;

	/// <inheritdoc />
	protected override string GetCommentPrefix() => "//";

	/// <inheritdoc />
	protected override string ConvertComment(string comment) {
		// Convert ; comments to // comments
		if (comment.StartsWith(';')) {
			return "//" + comment[1..];
		}
		return comment;
	}

	/// <inheritdoc />
	protected override string HandleFullLineComment(
		string originalLine,
		string trimmedLine,
		string leadingWhitespace,
		ConversionOptions options) {
		return $"{leadingWhitespace}//{trimmedLine[1..]}";
	}

	/// <inheritdoc />
	protected override string ConvertCode(
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

			// Check if this directive is supported in xkas
			if (!ReverseDirectiveMap.ContainsKey(directive)) {
				// Many PASM directives have no xkas equivalent
				if (IsUnsupportedInXkas(directive)) {
					if (options.WarnOnUnsupportedFeatures) {
						result.Warnings.Add(new ConversionMessage {
							FilePath = filePath,
							Line = lineNumber,
							Code = "EXP200",
							Message = $"Directive '{directive}' has no xkas equivalent",
							Severity = MessageSeverity.Warning
						});
					}
					return $"// UNSUPPORTED: {directive} {args}";
				}
			}

			var translated = TranslateDirective(directive, lineNumber, filePath, result, options);

			// Special case: include → incsrc (change .pasm extension to .asm)
			if (directive.Equals("include", StringComparison.OrdinalIgnoreCase)) {
				args = ConvertIncludeExtension(args);
				return $"{translated} {args}";
			}

			// Special case: arch directive — xkas uses different format
			if (directive.Equals("arch", StringComparison.OrdinalIgnoreCase)) {
				return ConvertArchDirective(args);
			}

			return $"{translated} {args}";
		}

		// Standalone directive
		var aloneMatch = DirectiveAlonePattern().Match(code);
		if (aloneMatch.Success) {
			var directive = aloneMatch.Groups[1].Value;

			if (IsUnsupportedInXkas(directive) && !ReverseDirectiveMap.ContainsKey(directive)) {
				if (options.WarnOnUnsupportedFeatures) {
					result.Warnings.Add(new ConversionMessage {
						FilePath = filePath,
						Line = lineNumber,
						Code = "EXP200",
						Message = $"Directive '{directive}' has no xkas equivalent",
						Severity = MessageSeverity.Warning
					});
				}
				return $"// UNSUPPORTED: {directive}";
			}

			var translated = TranslateDirective(directive, lineNumber, filePath, result, options);
			return translated;
		}

		// Instructions — pass through (same syntax)
		return code;
	}

	/// <summary>
	/// Converts PASM arch directives to xkas format.
	/// xkas uses "arch 65816.wdc" style notation.
	/// </summary>
	private static string ConvertArchDirective(string args) {
		return args.Trim().ToLowerInvariant() switch {
			"65816" or "65c816" => "arch 65816",
			"6502" => "arch 65816", // xkas primarily targets 65816
			"spc700" => "arch spc700",
			_ => $"arch {args}"
		};
	}

	/// <summary>
	/// Checks if a PASM directive is unsupported in xkas.
	/// xkas has a very limited directive set compared to PASM.
	/// </summary>
	private static bool IsUnsupportedInXkas(string directive) {
		return directive.ToLowerInvariant() switch {
			"macro" or "endmacro" or "exitmacro" => true,
			"namespace" or "endnamespace" => true,
			"scope" or "endscope" => true,
			"proc" or "endproc" => true,
			"ifdef" or "ifndef" or "if" or "else" or "elseif" or "endif" => true,
			"segment" => true,
			"export" or "import" or "global" or "local" => true,
			"define" or "set" => true,
			"enum" or "endenum" => true,
			"struct" or "endstruct" => true,
			"union" or "endunion" => true,
			"print" or "error" or "warn" or "assert" => true,
			"smart" or "feature" => true,
			"reloc" or "addr" or "faraddr" or "bankbytes" => true,
			"freecode" or "freedata" or "freespacebyte" => true,
			"skip" or "align" => true,
			"asciiz" => true,
			_ => false
		};
	}

}
