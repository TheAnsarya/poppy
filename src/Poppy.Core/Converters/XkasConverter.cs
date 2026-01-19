// ============================================================================
// XkasConverter.cs - xkas to PASM Project Converter
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text;
using System.Text.RegularExpressions;

namespace Poppy.Core.Converters;

/// <summary>
/// Converts xkas assembly projects to PASM format.
/// xkas is a legacy SNES assembler that preceded ASAR.
/// </summary>
public sealed partial class XkasConverter : BaseConverter {
	/// <inheritdoc />
	public override string SourceAssembler => "XKAS";

	/// <inheritdoc />
	public override IReadOnlyList<string> SupportedExtensions { get; } = [".asm"];

	// Track state for multi-line constructs
#pragma warning disable CS0414 // Field assigned but never used - tracking state for future expansion
	private bool _inMacro;
	private int _ifDepth;
#pragma warning restore CS0414

	/// <inheritdoc />
	protected override string ConvertLine(
		string line,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		// Handle empty lines
		if (string.IsNullOrWhiteSpace(line)) {
			return line;
		}

		// Preserve leading whitespace
		var leadingWhitespace = GetLeadingWhitespace(line);
		var trimmedLine = line.TrimStart();

		// Handle full-line comments
		if (trimmedLine.StartsWith(';') || trimmedLine.StartsWith("//")) {
			if (!options.PreserveComments) {
				return string.Empty;
			}
			// Convert // comments to ; comments
			if (trimmedLine.StartsWith("//")) {
				return leadingWhitespace + ";" + trimmedLine[2..];
			}
			return line;
		}

		// Split into code and comment
		var (code, comment) = SplitCodeAndComment(trimmedLine);

		if (string.IsNullOrWhiteSpace(code)) {
			return options.PreserveComments ? line : string.Empty;
		}

		// Convert the code portion
		var convertedCode = ConvertCode(code, lineNumber, filePath, result, options);

		// Reconstruct with comment if present
		var converted = options.PreserveComments && !string.IsNullOrEmpty(comment)
			? $"{convertedCode} {comment}"
			: convertedCode;

		return leadingWhitespace + converted;
	}

	/// <summary>
	/// Converts the code portion of a line.
	/// </summary>
	private string ConvertCode(
		string code,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		// Check for label definitions first
		var labelMatch = LabelPattern().Match(code);
		if (labelMatch.Success) {
			var label = labelMatch.Groups[1].Value;
			var rest = labelMatch.Groups[2].Value.Trim();

			// Handle local labels
			label = ConvertLocalLabel(label);

			if (string.IsNullOrEmpty(rest)) {
				return $"{label}:";
			}

			// Label with code on same line
			var restConverted = ConvertCode(rest, lineNumber, filePath, result, options);
			return $"{label}: {restConverted}";
		}

		// Check for directives
		var directiveMatch = DirectivePattern().Match(code);
		if (directiveMatch.Success) {
			return ConvertDirective(
				directiveMatch,
				lineNumber,
				filePath,
				result,
				options);
		}

		// Check for macro definitions
		var macroDefMatch = MacroDefinitionPattern().Match(code);
		if (macroDefMatch.Success) {
			return ConvertMacroDefinition(macroDefMatch);
		}

		// Check for macro invocations
		var macroInvokeMatch = MacroInvocationPattern().Match(code);
		if (macroInvokeMatch.Success) {
			return ConvertMacroInvocation(macroInvokeMatch);
		}

		// Check for EQU/= assignments
		var assignMatch = AssignmentPattern().Match(code);
		if (assignMatch.Success) {
			return ConvertAssignment(assignMatch);
		}

		// Assume it's an instruction
		return ConvertInstruction(code, lineNumber, filePath, result, options);
	}

	/// <summary>
	/// Converts an xkas directive to PASM.
	/// </summary>
	private string ConvertDirective(
		Match match,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		var directive = match.Groups[1].Value;
		var args = match.Groups[2].Value.Trim();

		// Handle by category
		return directive.ToLowerInvariant() switch {
			// Data directives
			"db" or "byte" => $"db {args}",
			"dw" or "word" => $"dw {args}",
			"dl" or "long" => $"dl {args}",
			"dd" or "dword" => $"dd {args}",

			// Text/table directives
			"table" => $"table {ConvertTablePath(args)}",
			"cleartable" => "cleartable",

			// Include directives
			"incsrc" => $"include {ConvertIncludePath(args)}",
			"incbin" => $"incbin {args}",

			// Organization directives
			"org" => $"org {args}",
			"base" => $"base {args}",

			// Fill directives
			"fill" => $"fill {args}",
			"fillbyte" => $"fillbyte {args}",

			// Architecture
			"arch" => ConvertArch(args, lineNumber, filePath, result, options),

			// ROM type
			"header" => "header",
			"lorom" => "lorom",
			"hirom" => "hirom",

			// Conditional (if xkas supports it)
			"if" => ConvertIf(args),
			"else" => "else",
			"endif" => ConvertEndIf(lineNumber, filePath, result),

			// Endmacro
			"endmacro" => ConvertEndMacro(),

			// Rep (repeat) - may need special handling
			"rep" => ConvertRep(args, lineNumber, filePath, result, options),

			// Unknown - try generic translation
			_ => TranslateDirective(directive, lineNumber, filePath, result, options) +
				 (string.IsNullOrEmpty(args) ? "" : $" {args}")
		};
	}

	/// <summary>
	/// Converts arch directive.
	/// </summary>
	private string ConvertArch(
		string args,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		var arch = args.ToLowerInvariant().Trim();

		return arch switch {
			"65816" or "snes" => "arch 65816",
			"spc700" => "arch spc700",
			"6502" or "nes" => "arch 6502",
			_ => HandleUnknownArch(arch, lineNumber, filePath, result, options)
		};
	}

	/// <summary>
	/// Handles unknown architecture.
	/// </summary>
	private static string HandleUnknownArch(
		string arch,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		if (options.WarnOnUnsupportedFeatures) {
			result.Warnings.Add(new ConversionMessage {
				FilePath = filePath,
				Line = lineNumber,
				Code = "CONV400",
				Message = $"Unknown architecture '{arch}'",
				Severity = MessageSeverity.Warning
			});
		}
		return $"arch {arch}";
	}

	/// <summary>
	/// Converts if directive.
	/// </summary>
	private string ConvertIf(string args) {
		_ifDepth++;
		return $"if {args}";
	}

	/// <summary>
	/// Converts endif directive.
	/// </summary>
	private string ConvertEndIf(
		int lineNumber,
		string filePath,
		ConversionResult result) {
		_ifDepth--;
		if (_ifDepth < 0) {
			result.Warnings.Add(new ConversionMessage {
				FilePath = filePath,
				Line = lineNumber,
				Code = "CONV200",
				Message = "Unmatched endif",
				Severity = MessageSeverity.Warning
			});
			_ifDepth = 0;
		}
		return "endif";
	}

	/// <summary>
	/// Converts rep (repeat) directive.
	/// </summary>
	private static string ConvertRep(
		string args,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		// rep N is a repeat block
		// PASM may handle this differently
		if (options.WarnOnUnsupportedFeatures) {
			result.Warnings.Add(new ConversionMessage {
				FilePath = filePath,
				Line = lineNumber,
				Code = "CONV401",
				Message = "rep directive may need manual adjustment",
				Severity = MessageSeverity.Warning
			});
		}
		return $"rep {args}";
	}

	/// <summary>
	/// Converts table file path.
	/// </summary>
	private static string ConvertTablePath(string args) {
		var path = args.Trim('"', '\'');
		return $"\"{path}\"";
	}

	/// <summary>
	/// Converts include file path.
	/// </summary>
	private static string ConvertIncludePath(string args) {
		var path = args.Trim('"', '\'');

		// Change extension to .pasm
		if (path.EndsWith(".asm", StringComparison.OrdinalIgnoreCase)) {
			path = System.IO.Path.ChangeExtension(path, ".pasm");
		}

		return $"\"{path}\"";
	}

	/// <summary>
	/// Converts macro definition.
	/// </summary>
	private string ConvertMacroDefinition(Match match) {
		_inMacro = true;
		var name = match.Groups[1].Value;
		var args = match.Groups[2].Value;

		if (string.IsNullOrEmpty(args)) {
			return $"macro {name}()";
		}

		// xkas uses %arg for parameters
		return $"macro {name}({args})";
	}

	/// <summary>
	/// Converts endmacro directive.
	/// </summary>
	private string ConvertEndMacro() {
		_inMacro = false;
		return "endmacro";
	}

	/// <summary>
	/// Converts macro invocation.
	/// </summary>
	private static string ConvertMacroInvocation(Match match) {
		var name = match.Groups[1].Value;
		var args = match.Groups[2].Value;

		if (string.IsNullOrEmpty(args)) {
			return $"%{name}";
		}

		return $"%{name}({args})";
	}

	/// <summary>
	/// Converts assignment expression.
	/// </summary>
	private static string ConvertAssignment(Match match) {
		var name = match.Groups[1].Value;
		var value = match.Groups[2].Value;
		return $"{name} = {value}";
	}

	/// <summary>
	/// Converts an instruction.
	/// </summary>
	private string ConvertInstruction(
		string code,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		var parts = code.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);

		if (parts.Length == 0) {
			return code;
		}

		var mnemonic = parts[0].ToLowerInvariant();
		var operand = parts.Length > 1 ? parts[1] : string.Empty;

		return string.IsNullOrEmpty(operand) ? mnemonic : $"{mnemonic} {operand}";
	}

	// ========================================================================
	// Helper Methods
	// ========================================================================

	private static string GetLeadingWhitespace(string line) {
		int i = 0;
		while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) {
			i++;
		}
		return line[..i];
	}

	private static (string code, string comment) SplitCodeAndComment(string line) {
		bool inString = false;
		char stringChar = '\0';

		for (int i = 0; i < line.Length; i++) {
			var c = line[i];

			if (inString) {
				if (c == stringChar) {
					inString = false;
				}
			}
			else if (c == '"' || c == '\'') {
				inString = true;
				stringChar = c;
			}
			else if (c == ';') {
				return (line[..i].TrimEnd(), line[i..]);
			}
			else if (i < line.Length - 1 && c == '/' && line[i + 1] == '/') {
				// Convert // to ; style comment
				return (line[..i].TrimEnd(), ";" + line[(i + 2)..]);
			}
		}

		return (line, string.Empty);
	}

	// ========================================================================
	// Regex Patterns
	// ========================================================================

	/// <summary>
	/// Matches label definitions (name: or .localname:)
	/// </summary>
	[GeneratedRegex(@"^([.\w]+):\s*(.*)$")]
	private static partial Regex LabelPattern();

	/// <summary>
	/// Matches directive lines (directive args)
	/// </summary>
	[GeneratedRegex(@"^(\w+)\s*(.*)$")]
	private static partial Regex DirectivePattern();

	/// <summary>
	/// Matches macro definitions: macro name(args) or macro name
	/// </summary>
	[GeneratedRegex(@"^macro\s+(\w+)\s*(?:\(([^)]*)\))?", RegexOptions.IgnoreCase)]
	private static partial Regex MacroDefinitionPattern();

	/// <summary>
	/// Matches macro invocations: %name(args) or %name
	/// </summary>
	[GeneratedRegex(@"^%(\w+)(?:\(([^)]*)\))?")]
	private static partial Regex MacroInvocationPattern();

	/// <summary>
	/// Matches assignments: name = value or name equ value
	/// </summary>
	[GeneratedRegex(@"^(\w+)\s*(?:=|equ)\s*(.+)$", RegexOptions.IgnoreCase)]
	private static partial Regex AssignmentPattern();
}
