// ============================================================================
// MacroTable.cs - Macro Definition Storage and Lookup
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;
using Poppy.Core.Parser;

namespace Poppy.Core.Semantics;

/// <summary>
/// Stores and manages macro definitions.
/// </summary>
public sealed class MacroTable
{
	private readonly Dictionary<string, MacroDefinition> _macros = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<SemanticError> _errors = [];

	// Reserved words that cannot be used as macro names
	private static readonly HashSet<string> ReservedWords = new(StringComparer.OrdinalIgnoreCase)
	{
		// 6502 opcodes
		"adc", "and", "asl", "bcc", "bcs", "beq", "bit", "bmi",
		"bne", "bpl", "brk", "bvc", "bvs", "clc", "cld", "cli",
		"clv", "cmp", "cpx", "cpy", "dec", "dex", "dey", "eor",
		"inc", "inx", "iny", "jmp", "jsr", "lda", "ldx", "ldy",
		"lsr", "nop", "ora", "pha", "php", "pla", "plp", "rol",
		"ror", "rti", "rts", "sbc", "sec", "sed", "sei", "sta",
		"stx", "sty", "tax", "tay", "tsx", "txa", "txs", "tya",

		// Common directives
		"org", "byte", "word", "long", "db", "dw", "dl", "dd",
		"ds", "fill", "res", "equ", "define", "include", "incbin",
		"align", "pad", "assert", "error", "warning",
		"nes", "snes", "gb", "gameboy", "target",
		"lorom", "hirom", "exhirom", "mapper",
		"macro", "endmacro", "endm",
		"if", "else", "elseif", "endif",
		"ifdef", "ifndef", "ifexist",
		"ifeq", "ifne", "ifgt", "iflt", "ifge", "ifle",
		"rept", "endr", "enum", "ende"
	};

	/// <summary>
	/// Gets all errors encountered during macro processing.
	/// </summary>
	public IReadOnlyList<SemanticError> Errors => _errors;

	/// <summary>
	/// Gets whether any errors have been recorded.
	/// </summary>
	public bool HasErrors => _errors.Count > 0;

	/// <summary>
	/// Defines a new macro.
	/// </summary>
	/// <param name="name">The macro name.</param>
	/// <param name="parameters">The macro parameters.</param>
	/// <param name="body">The macro body statements.</param>
	/// <param name="location">The source location.</param>
	public void Define(
		string name,
		IReadOnlyList<MacroParameter> parameters,
		IReadOnlyList<StatementNode> body,
		SourceLocation location)
	{
		// Validate macro name
		if (string.IsNullOrWhiteSpace(name))
		{
			_errors.Add(new SemanticError(
				"Macro name cannot be empty",
				location));
			return;
		}

		// Check for reserved words
		if (ReservedWords.Contains(name))
		{
			_errors.Add(new SemanticError(
				$"Cannot use reserved word '{name}' as macro name",
				location));
			return;
		}

		// Check for duplicate definition
		if (_macros.ContainsKey(name))
		{
			_errors.Add(new SemanticError(
				$"Macro '{name}' is already defined",
				location));
			return;
		}

		// Validate parameters (no duplicates)
		var paramNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var param in parameters)
		{
			if (!paramNames.Add(param.Name))
			{
				_errors.Add(new SemanticError(
					$"Duplicate parameter name '{param.Name}' in macro '{name}'",
					location));
				return;
			}
		}

		// Create and store macro definition
		var macro = new MacroDefinition(name, parameters, body, location);
		_macros[name] = macro;
	}

	/// <summary>
	/// Checks if a macro with the given name is defined.
	/// </summary>
	/// <param name="name">The macro name to check.</param>
	/// <returns>True if the macro is defined, false otherwise.</returns>
	public bool IsDefined(string name)
	{
		return _macros.ContainsKey(name);
	}

	/// <summary>
	/// Gets the definition for a macro.
	/// </summary>
	/// <param name="name">The macro name.</param>
	/// <returns>The macro definition, or null if not defined.</returns>
	public MacroDefinition? Get(string name)
	{
		return _macros.TryGetValue(name, out var macro) ? macro : null;
	}

	/// <summary>
	/// Gets the number of macros defined.
	/// </summary>
	public int Count => _macros.Count;

	/// <summary>
	/// Clears all macros and errors.
	/// </summary>
	public void Clear()
	{
		_macros.Clear();
		_errors.Clear();
	}

	/// <summary>
	/// Checks if a name is a reserved word.
	/// </summary>
	/// <param name="name">The name to check.</param>
	/// <returns>True if the name is reserved, false otherwise.</returns>
	public static bool IsReservedWord(string name)
	{
		return ReservedWords.Contains(name);
	}
}
