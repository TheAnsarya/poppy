// ============================================================================
// Ca65Converter.cs - ca65 to PASM Project Converter
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text;
using System.Text.RegularExpressions;

namespace Poppy.Core.Converters;

/// <summary>
/// Converts ca65 assembly projects to PASM format.
/// ca65 is part of the cc65 compiler suite, commonly used for NES development.
/// </summary>
public sealed partial class Ca65Converter : BaseConverter {
	/// <inheritdoc />
	public override string SourceAssembler => "CA65";

	/// <inheritdoc />
	public override IReadOnlyList<string> SupportedExtensions { get; } = [".s", ".asm", ".inc"];

	// Track state for multi-line constructs
#pragma warning disable CS0414 // Field assigned but never used - tracking state for future expansion
	private bool _inMacro;
	private bool _inScope;
	private bool _inProc;
	private bool _inStruct;
	private int _ifDepth;
	private string? _currentSegment;
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
		// Check for label definitions (ca65 labels end with colon)
		var labelMatch = LabelPattern().Match(code);
		if (labelMatch.Success) {
			var label = labelMatch.Groups[1].Value;
			var rest = labelMatch.Groups[2].Value.Trim();

			// Convert local labels (@ -> .)
			label = ConvertLocalLabel(label);

			if (string.IsNullOrEmpty(rest)) {
				return $"{label}:";
			}

			// Label with code on same line
			var restConverted = ConvertCode(rest, lineNumber, filePath, result, options);
			return $"{label}: {restConverted}";
		}

		// Check for directives (ca65 directives start with .)
		if (code.StartsWith('.')) {
			return ConvertDirective(code, lineNumber, filePath, result, options);
		}

		// Check for assignments (name = value or name := value)
		var assignMatch = AssignmentPattern().Match(code);
		if (assignMatch.Success) {
			return ConvertAssignment(assignMatch);
		}

		// Assume it's an instruction
		return ConvertInstruction(code, lineNumber, filePath, result, options);
	}

	/// <summary>
	/// Converts a ca65 directive to PASM.
	/// </summary>
	private string ConvertDirective(
		string code,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		var match = DirectivePattern().Match(code);
		if (!match.Success) {
			return code;
		}

		var directive = match.Groups[1].Value;
		var args = match.Groups[2].Value.Trim();

		// Handle by category
		return directive.ToLowerInvariant() switch {
			// Data directives
			"byte" or "db" or "byt" => $"db {args}",
			"word" or "dw" or "dbyt" => $"dw {ConvertWordArgs(args)}",
			"dword" or "dd" => $"dd {args}",
			"res" => ConvertReserve(args),
			"asciiz" => $"asciiz {args}",

			// Include directives
			"include" => ConvertInclude(args),
			"incbin" => $"incbin {args}",

			// Segment directives
			"segment" => ConvertSegment(args),
			"code" => SetSegment("CODE"),
			"data" => SetSegment("DATA"),
			"rodata" => SetSegment("RODATA"),
			"bss" => SetSegment("BSS"),
			"zeropage" => SetSegment("ZEROPAGE"),

			// Scope directives
			"scope" => ConvertScopeStart(args),
			"endscope" => ConvertScopeEnd(),
			"proc" => ConvertProcStart(args),
			"endproc" => ConvertProcEnd(),

			// Macro directives
			"macro" => ConvertMacroStart(args),
			"endmacro" or "endmac" => ConvertMacroEnd(),
			"exitmacro" or "exitmac" => "exitmacro",

			// Conditional assembly
			"if" => ConvertIf(args),
			"ifdef" => ConvertIfDef(args, false),
			"ifndef" => ConvertIfDef(args, true),
			"else" => "else",
			"elseif" => $"elseif {ConvertExpression(args)}",
			"endif" => ConvertEndIf(lineNumber, filePath, result),
			"ifblank" => $"ifblank {args}",
			"ifnblank" => $"ifnblank {args}",

			// Symbol directives
			"export" => $"export {args}",
			"import" => $"import {args}",
			"global" or "globalzp" => $"global {args}",
			"local" => $"local {args}",

			// Definition directives
			"define" => ConvertDefine(args),
			"set" => ConvertSet(args),
			"enum" => $"enum {args}",
			"endenum" => "endenum",
			"struct" => ConvertStructStart(args),
			"endstruct" => ConvertStructEnd(),

			// CPU directives
			"p02" or "pc02" => "arch 6502",
			"p816" or "p65816" => "arch 65816",
			"a8" => "a8",
			"a16" => "a16",
			"i8" => "i8",
			"i16" => "i16",
			"smart" => "smart",

			// Organization
			"org" => $"org {args}",
			"align" => $"align {args}",
			"reloc" => "reloc",

			// Address reference directives
			"addr" => $"addr {args}",
			"faraddr" => $"faraddr {args}",
			"bankbytes" => $"bankbytes {args}",

			// Output
			"out" => $"print {args}",
			"warning" => $"warn {args}",
			"error" or "fatal" => $"error {args}",
			"assert" => $"assert {args}",

			// Feature control
			"feature" => ConvertFeature(args, lineNumber, filePath, result, options),

			// Unknown - try generic translation
			_ => TranslateUnknownDirective(directive, args, lineNumber, filePath, result, options)
		};
	}

	/// <summary>
	/// Converts the .segment directive.
	/// </summary>
	private string ConvertSegment(string args) {
		// Remove quotes if present
		var segment = args.Trim('"', '\'');
		_currentSegment = segment;
		return $"segment \"{segment}\"";
	}

	/// <summary>
	/// Sets the current segment using a shorthand directive.
	/// </summary>
	private string SetSegment(string segment) {
		_currentSegment = segment;
		return $"segment \"{segment}\"";
	}

	/// <summary>
	/// Converts .res directive to fill.
	/// </summary>
	private static string ConvertReserve(string args) {
		// .res count[, fillvalue]
		var parts = args.Split(',');
		if (parts.Length == 1) {
			return $"fill {parts[0].Trim()}, $00";
		}
		return $"fill {parts[0].Trim()}, {parts[1].Trim()}";
	}

	/// <summary>
	/// Converts .include directive.
	/// </summary>
	private static string ConvertInclude(string args) {
		var filePath = args.Trim('"', '\'');

		// Change .s/.inc to .pasm for assembly includes
		if (filePath.EndsWith(".s", StringComparison.OrdinalIgnoreCase) ||
			filePath.EndsWith(".inc", StringComparison.OrdinalIgnoreCase)) {
			filePath = System.IO.Path.ChangeExtension(filePath, ".pasm");
		}

		return $"include \"{filePath}\"";
	}

	/// <summary>
	/// Converts .scope directive.
	/// </summary>
	private string ConvertScopeStart(string args) {
		_inScope = true;
		return string.IsNullOrEmpty(args) ? "scope" : $"scope {args}";
	}

	/// <summary>
	/// Converts .endscope directive.
	/// </summary>
	private string ConvertScopeEnd() {
		_inScope = false;
		return "endscope";
	}

	/// <summary>
	/// Converts .proc directive.
	/// </summary>
	private string ConvertProcStart(string args) {
		_inProc = true;
		return $"proc {args}";
	}

	/// <summary>
	/// Converts .endproc directive.
	/// </summary>
	private string ConvertProcEnd() {
		_inProc = false;
		return "endproc";
	}

	/// <summary>
	/// Converts .macro directive.
	/// </summary>
	private string ConvertMacroStart(string args) {
		_inMacro = true;

		// ca65: .macro name arg1, arg2
		// PASM: macro name(arg1, arg2)
		var parts = args.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 1) {
			return $"macro {parts[0]}()";
		}

		var name = parts[0];
		var parameters = parts[1];
		return $"macro {name}({parameters})";
	}

	/// <summary>
	/// Converts .endmacro directive.
	/// </summary>
	private string ConvertMacroEnd() {
		_inMacro = false;
		return "endmacro";
	}

	/// <summary>
	/// Converts .struct directive.
	/// </summary>
	private string ConvertStructStart(string args) {
		_inStruct = true;
		return string.IsNullOrEmpty(args) ? "struct" : $"struct {args}";
	}

	/// <summary>
	/// Converts .endstruct directive.
	/// </summary>
	private string ConvertStructEnd() {
		_inStruct = false;
		return "endstruct";
	}

	/// <summary>
	/// Converts .if directive.
	/// </summary>
	private string ConvertIf(string args) {
		_ifDepth++;
		return $"if {ConvertExpression(args)}";
	}

	/// <summary>
	/// Converts .ifdef/.ifndef directives.
	/// </summary>
	private string ConvertIfDef(string args, bool negate) {
		_ifDepth++;
		return negate ? $"ifndef {args}" : $"ifdef {args}";
	}

	/// <summary>
	/// Converts .endif directive.
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
				Message = "Unmatched .endif",
				Severity = MessageSeverity.Warning
			});
			_ifDepth = 0;
		}
		return "endif";
	}

	/// <summary>
	/// Converts .define directive.
	/// </summary>
	private static string ConvertDefine(string args) {
		// .define name value or .define name(args) value
		return $"define {args}";
	}

	/// <summary>
	/// Converts .set directive.
	/// </summary>
	private static string ConvertSet(string args) {
		// .set name, value -> name = value (reassignable)
		var parts = args.Split(',', 2);
		if (parts.Length == 2) {
			return $"{parts[0].Trim()} = {parts[1].Trim()}";
		}
		return $"set {args}";
	}

	/// <summary>
	/// Converts .feature directive.
	/// </summary>
	private string ConvertFeature(
		string args,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		// Some features map directly, others need warnings
		var feature = args.ToLowerInvariant().Trim();

		switch (feature) {
			case "at_in_identifiers":
			case "dollar_in_identifiers":
			case "labels_without_colons":
			case "pc_assignment":
			case "string_escapes":
				return $"feature {feature}";

			default:
				if (options.WarnOnUnsupportedFeatures) {
					result.Warnings.Add(new ConversionMessage {
						FilePath = filePath,
						Line = lineNumber,
						Code = "CONV300",
						Message = $".feature {feature} may not be supported",
						Severity = MessageSeverity.Warning
					});
				}
				return $"; TODO: .feature {feature}";
		}
	}

	/// <summary>
	/// Translates an unknown directive.
	/// </summary>
	private string TranslateUnknownDirective(
		string directive,
		string args,
		int lineNumber,
		string filePath,
		ConversionResult result,
		ConversionOptions options) {
		var fullDirective = $".{directive}";

		if (DirectiveMapping.Ca65Unsupported.Contains(fullDirective)) {
			if (options.WarnOnUnsupportedFeatures) {
				result.Warnings.Add(new ConversionMessage {
					FilePath = filePath,
					Line = lineNumber,
					Code = "CONV100",
					Message = $"Directive '{fullDirective}' is not supported in PASM",
					Severity = MessageSeverity.Warning
				});
			}
			return $"; UNSUPPORTED: {fullDirective} {args}".TrimEnd();
		}

		if (DirectiveMapping.Ca65ToPasm.TryGetValue(fullDirective, out var pasmDirective)) {
			return string.IsNullOrEmpty(args) ? pasmDirective : $"{pasmDirective} {args}";
		}

		if (options.WarnOnUnsupportedFeatures) {
			result.Warnings.Add(new ConversionMessage {
				FilePath = filePath,
				Line = lineNumber,
				Code = "CONV101",
				Message = $"Unknown directive '{fullDirective}' - passing through",
				Severity = MessageSeverity.Warning
			});
		}

		return string.IsNullOrEmpty(args) ? directive : $"{directive} {args}";
	}

	/// <summary>
	/// Converts an assignment expression.
	/// </summary>
	private static string ConvertAssignment(Match match) {
		var name = match.Groups[1].Value;
		var op = match.Groups[2].Value;
		var value = match.Groups[3].Value;

		// ca65: name = value or name := value
		// PASM: name = value
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
		var operand = parts.Length > 1 ? ConvertOperand(parts[1]) : string.Empty;

		return string.IsNullOrEmpty(operand) ? mnemonic : $"{mnemonic} {operand}";
	}

	/// <summary>
	/// Converts operand expressions.
	/// </summary>
	private static string ConvertOperand(string operand) {
		// Convert @local labels to .local
		operand = LocalLabelPattern().Replace(operand, ".$1");

		return operand;
	}

	/// <summary>
	/// Converts word arguments (handles big-endian .dbyt).
	/// </summary>
	private static string ConvertWordArgs(string args) {
		// ca65 .dbyt is big-endian, .word is little-endian
		// For now, just pass through
		return args;
	}

	/// <summary>
	/// Converts ca65 expressions to PASM.
	/// </summary>
	private static string ConvertExpression(string expr) {
		// Convert .sizeof() to sizeof()
		expr = SizeofPattern().Replace(expr, "sizeof($1)");

		// Convert .match() and other functions
		expr = MatchPattern().Replace(expr, "match($1)");

		return expr;
	}

	/// <inheritdoc />
	protected override string ConvertLocalLabel(string label) {
		// ca65 uses @ for local labels, PASM uses .
		if (label.StartsWith('@')) {
			return "." + label[1..];
		}
		return base.ConvertLocalLabel(label);
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
		}

		return (line, string.Empty);
	}

	// ========================================================================
	// Regex Patterns
	// ========================================================================

	/// <summary>
	/// Matches label definitions.
	/// </summary>
	[GeneratedRegex(@"^(@?[.\w]+):\s*(.*)$")]
	private static partial Regex LabelPattern();

	/// <summary>
	/// Matches directives (.name args)
	/// </summary>
	[GeneratedRegex(@"^\.(\w+)\s*(.*)$")]
	private static partial Regex DirectivePattern();

	/// <summary>
	/// Matches assignments (name = value or name := value)
	/// </summary>
	[GeneratedRegex(@"^(\w+)\s*(:?=)\s*(.+)$")]
	private static partial Regex AssignmentPattern();

	/// <summary>
	/// Matches local labels (@name)
	/// </summary>
	[GeneratedRegex(@"@(\w+)")]
	private static partial Regex LocalLabelPattern();

	/// <summary>
	/// Matches .sizeof() function
	/// </summary>
	[GeneratedRegex(@"\.sizeof\s*\(([^)]+)\)", RegexOptions.IgnoreCase)]
	private static partial Regex SizeofPattern();

	/// <summary>
	/// Matches .match() function
	/// </summary>
	[GeneratedRegex(@"\.match\s*\(([^)]+)\)", RegexOptions.IgnoreCase)]
	private static partial Regex MatchPattern();
}
