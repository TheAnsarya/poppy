// ============================================================================
// AsarConverter.cs - ASAR to PASM Project Converter
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text;
using System.Text.RegularExpressions;

namespace Poppy.Core.Converters;

/// <summary>
/// Converts ASAR assembly projects to PASM format.
/// ASAR is a popular SNES assembler used for SMW hacking.
/// </summary>
public sealed partial class AsarConverter : BaseConverter {
	/// <inheritdoc />
	public override string SourceAssembler => "ASAR";

	/// <inheritdoc />
	public override IReadOnlyList<string> SupportedExtensions { get; } = [".asm"];

	// Track state for multi-line constructs
#pragma warning disable CS0414 // Field assigned but never used - tracking state for future expansion
	private bool _inMacro;
	private bool _inNamespace;
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
		if (trimmedLine.StartsWith(';')) {
			return options.PreserveComments ? line : string.Empty;
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

		// Check for directives (no leading dot in ASAR for most directives)
		var directiveMatch = DirectivePattern().Match(code);
		if (directiveMatch.Success) {
			return ConvertDirective(
				directiveMatch,
				code,
				lineNumber,
				filePath,
				result,
				options);
		}

		// Check for macro definitions
		var macroDefMatch = MacroDefinitionPattern().Match(code);
		if (macroDefMatch.Success) {
			return ConvertMacroDefinition(macroDefMatch, result, options);
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

		// Assume it's an instruction - convert operands
		return ConvertInstruction(code, lineNumber, filePath, result, options);
	}

	/// <summary>
	/// Converts an ASAR directive to PASM.
	/// </summary>
	private string ConvertDirective(
		Match match,
		string fullLine,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		var directive = match.Groups[1].Value;
		var args = match.Groups[2].Value.Trim();

		// Handle conditional assembly
		if (IsConditionalDirective(directive)) {
			return ConvertConditional(directive, args, lineNumber, filePath, result, options);
		}

		// Handle namespace
		if (directive.Equals("namespace", StringComparison.OrdinalIgnoreCase)) {
			_inNamespace = !string.IsNullOrEmpty(args);
			return $"namespace {args}";
		}

		if (directive.Equals("endnamespace", StringComparison.OrdinalIgnoreCase)) {
			_inNamespace = false;
			return "endnamespace";
		}

		// Handle ROM mapping directives
		if (IsRomMappingDirective(directive)) {
			return ConvertRomMapping(directive, args);
		}

		// Handle include directives
		if (IsIncludeDirective(directive)) {
			return ConvertInclude(directive, args);
		}

		// Handle data directives
		if (IsDataDirective(directive)) {
			return ConvertDataDirective(directive, args);
		}

		// Handle fill directives
		if (IsFillDirective(directive)) {
			return ConvertFillDirective(directive, args);
		}

		// Handle print/assert/error/warn
		if (IsOutputDirective(directive)) {
			return ConvertOutputDirective(directive, args);
		}

		// Generic directive translation
		var translated = TranslateDirective(directive, lineNumber, filePath, result, options);
		return string.IsNullOrEmpty(args) ? translated : $"{translated} {args}";
	}

	/// <summary>
	/// Converts conditional assembly directives.
	/// </summary>
	private string ConvertConditional(
		string directive,
		string args,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		switch (directive.ToLowerInvariant()) {
			case "if":
				_ifDepth++;
				return $"if {ConvertExpression(args)}";

			case "elseif":
				return $"elseif {ConvertExpression(args)}";

			case "else":
				return "else";

			case "endif":
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

			default:
				return $"; TODO: {directive} {args}";
		}
	}

	/// <summary>
	/// Converts ROM mapping directives.
	/// </summary>
	private static string ConvertRomMapping(string directive, string args) {
		return directive.ToLowerInvariant() switch {
			"lorom" => "lorom",
			"hirom" => "hirom",
			"exlorom" => "exlorom",
			"exhirom" => "exhirom",
			"norom" => "norom",
			"sa1rom" => "sa1rom",
			"freecode" => "freecode",
			"freedata" => "freedata",
			_ => $"{directive} {args}".TrimEnd()
		};
	}

	/// <summary>
	/// Converts include directives.
	/// </summary>
	private string ConvertInclude(string directive, string args) {
		// Normalize file path quotes
		var filePath = args.Trim('"', '\'');

		// Change extension to .pasm for source includes
		if (directive.Equals("incsrc", StringComparison.OrdinalIgnoreCase)) {
			filePath = System.IO.Path.ChangeExtension(filePath, ".pasm");
			return $"include \"{filePath}\"";
		}

		// incbin stays the same
		return $"incbin \"{filePath}\"";
	}

	/// <summary>
	/// Converts data directives.
	/// </summary>
	private static string ConvertDataDirective(string directive, string args) {
		var pasmDirective = directive.ToLowerInvariant() switch {
			"db" or "byte" => "db",
			"dw" or "word" => "dw",
			"dl" or "long" => "dl",
			"dd" or "dword" => "dd",
			_ => directive.ToLowerInvariant()
		};

		return $"{pasmDirective} {args}";
	}

	/// <summary>
	/// Converts fill directives.
	/// </summary>
	private static string ConvertFillDirective(string directive, string args) {
		return directive.ToLowerInvariant() switch {
			"fill" => $"fill {args}",
			"fillbyte" => $"fillbyte {args}",
			"padbyte" => $"padbyte {args}",
			"pad" => $"pad {args}",
			"align" => $"align {args}",
			_ => $"{directive} {args}"
		};
	}

	/// <summary>
	/// Converts output directives (print, assert, error, warn).
	/// </summary>
	private static string ConvertOutputDirective(string directive, string args) {
		return directive.ToLowerInvariant() switch {
			"print" => $"print {args}",
			"assert" => $"assert {args}",
			"error" => $"error {args}",
			"warn" => $"warn {args}",
			_ => $"{directive} {args}"
		};
	}

	/// <summary>
	/// Converts macro definitions.
	/// </summary>
	private string ConvertMacroDefinition(Match match, ConversionResult result, ConversionOptions options) {
		_inMacro = true;
		var name = match.Groups[1].Value;
		var args = match.Groups[2].Value;

		if (string.IsNullOrEmpty(args)) {
			return $"macro {name}()";
		}

		// ASAR uses <arg> for parameters, PASM uses %arg
		var convertedArgs = ConvertMacroParameters(args);
		return $"macro {name}({convertedArgs})";
	}

	/// <summary>
	/// Converts macro parameter syntax.
	/// </summary>
	private static string ConvertMacroParameters(string args) {
		// ASAR: <arg1>, <arg2>
		// PASM: arg1, arg2
		return AngleBracketPattern().Replace(args, "$1");
	}

	/// <summary>
	/// Converts macro invocations.
	/// </summary>
	private static string ConvertMacroInvocation(Match match) {
		var name = match.Groups[1].Value;
		var args = match.Groups[2].Value;

		// ASAR uses %macro(args), PASM uses macro args or macro(args)
		if (string.IsNullOrEmpty(args)) {
			return $"%{name}";
		}

		return $"%{name}({args})";
	}

	/// <summary>
	/// Converts assignment expressions.
	/// </summary>
	private static string ConvertAssignment(Match match) {
		var name = match.Groups[1].Value;
		var value = match.Groups[2].Value;

		// ASAR: name = value
		// PASM: name = value (same syntax)
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

		// Handle ASAR-specific instruction syntax
		if (!string.IsNullOrEmpty(operand)) {
			operand = ConvertOperand(operand);
		}

		return string.IsNullOrEmpty(operand) ? mnemonic : $"{mnemonic} {operand}";
	}

	/// <summary>
	/// Converts operand expressions.
	/// </summary>
	private static string ConvertOperand(string operand) {
		// ASAR uses .b, .w, .l suffixes for size hints
		// PASM uses the same convention
		return operand;
	}

	/// <summary>
	/// Converts an ASAR expression to PASM.
	/// </summary>
	private static string ConvertExpression(string expr) {
		// ASAR and PASM have similar expression syntax
		// Main differences are in function calls and special operators
		return expr;
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
		// Find comment start (semicolon not in string)
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
		}

		return (line, string.Empty);
	}

	private static bool IsConditionalDirective(string directive) =>
		directive.Equals("if", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("elseif", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("else", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("endif", StringComparison.OrdinalIgnoreCase);

	private static bool IsRomMappingDirective(string directive) =>
		directive.Equals("lorom", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("hirom", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("exlorom", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("exhirom", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("norom", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("sa1rom", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("freecode", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("freedata", StringComparison.OrdinalIgnoreCase);

	private static bool IsIncludeDirective(string directive) =>
		directive.Equals("incsrc", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("incbin", StringComparison.OrdinalIgnoreCase);

	private static bool IsDataDirective(string directive) =>
		directive.Equals("db", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("dw", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("dl", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("dd", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("byte", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("word", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("long", StringComparison.OrdinalIgnoreCase);

	private static bool IsFillDirective(string directive) =>
		directive.Equals("fill", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("fillbyte", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("padbyte", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("pad", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("align", StringComparison.OrdinalIgnoreCase);

	private static bool IsOutputDirective(string directive) =>
		directive.Equals("print", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("assert", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("error", StringComparison.OrdinalIgnoreCase) ||
		directive.Equals("warn", StringComparison.OrdinalIgnoreCase);

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
	/// Matches macro definitions: macro name(args) or macro name()
	/// </summary>
	[GeneratedRegex(@"^macro\s+(\w+)\s*\(([^)]*)\)", RegexOptions.IgnoreCase)]
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

	/// <summary>
	/// Matches angle bracket parameters: &lt;param&gt;
	/// </summary>
	[GeneratedRegex(@"<(\w+)>")]
	private static partial Regex AngleBracketPattern();
}
