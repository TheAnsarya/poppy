// ============================================================================
// DirectiveMapping.cs - Directive Translation Mappings
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.Converters;

/// <summary>
/// Provides directive translation mappings between different assemblers and PASM.
/// </summary>
public static class DirectiveMapping {
	// ========================================================================
	// ASAR Directive Mappings
	// ========================================================================

	/// <summary>
	/// Maps ASAR directives to their PASM equivalents.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> AsarToPasm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
		// Data directives
		["db"] = "db",
		["dw"] = "dw",
		["dl"] = "dl",
		["dd"] = "dd",
		["byte"] = "db",
		["word"] = "dw",
		["long"] = "dl",

		// Text directives
		["table"] = "table",
		["cleartable"] = "cleartable",

		// Include directives
		["incsrc"] = "include",
		["incbin"] = "incbin",

		// Organization directives
		["org"] = "org",
		["base"] = "base",
		["skip"] = "skip",
		["align"] = "align",

		// Free space directives
		["freecode"] = "freecode",
		["freedata"] = "freedata",
		["freespacebyte"] = "freespacebyte",

		// Fill directives
		["fill"] = "fill",
		["fillbyte"] = "fillbyte",
		["padbyte"] = "padbyte",
		["pad"] = "pad",

		// Namespace/scope directives
		["namespace"] = "namespace",
		["endnamespace"] = "endnamespace",

		// Output directives
		["print"] = "print",
		["error"] = "error",
		["warn"] = "warn",
		["assert"] = "assert",

		// Architecture directives
		["arch"] = "arch",

		// Bank directives
		["bank"] = "bank",
		["hirom"] = "hirom",
		["lorom"] = "lorom",
		["exhirom"] = "exhirom",
		["exlorom"] = "exlorom",
		["norom"] = "norom",
	};

	/// <summary>
	/// ASAR directives that have no PASM equivalent and should generate warnings.
	/// </summary>
	public static readonly IReadOnlySet<string> AsarUnsupported = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
		"optimize",
		"dpbase",
		"pushpc",
		"pullpc",
		"pushbase",
		"pullbase",
	};

	// ========================================================================
	// ca65 Directive Mappings
	// ========================================================================

	/// <summary>
	/// Maps ca65 directives to their PASM equivalents.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> Ca65ToPasm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
		// Data directives
		[".byte"] = "db",
		[".word"] = "dw",
		[".dword"] = "dd",
		[".dbyt"] = "dw",   // Big-endian word
		[".res"] = "fill",
		[".asciiz"] = "asciiz",
		[".literal"] = "db",

		// Include directives
		[".include"] = "include",
		[".incbin"] = "incbin",

		// Organization/segment directives
		[".org"] = "org",
		[".segment"] = "segment",
		[".code"] = "segment \"CODE\"",
		[".data"] = "segment \"DATA\"",
		[".rodata"] = "segment \"RODATA\"",
		[".bss"] = "segment \"BSS\"",
		[".zeropage"] = "segment \"ZEROPAGE\"",

		// Scope/namespace directives
		[".scope"] = "scope",
		[".endscope"] = "endscope",
		[".proc"] = "proc",
		[".endproc"] = "endproc",

		// Alignment
		[".align"] = "align",

		// Symbol directives
		[".export"] = "export",
		[".import"] = "import",
		[".global"] = "global",
		[".local"] = "local",

		// Conditional assembly
		[".if"] = "if",
		[".else"] = "else",
		[".elseif"] = "elseif",
		[".endif"] = "endif",
		[".ifdef"] = "ifdef",
		[".ifndef"] = "ifndef",
		[".ifblank"] = "ifblank",
		[".ifnblank"] = "ifnblank",

		// Macro directives
		[".macro"] = "macro",
		[".endmacro"] = "endmacro",
		[".exitmacro"] = "exitmacro",

		// Assignment/definition
		[".define"] = "define",
		[".set"] = "set",
		[".enum"] = "enum",
		[".endenum"] = "endenum",
		[".struct"] = "struct",
		[".endstruct"] = "endstruct",
		[".union"] = "union",
		[".endunion"] = "endunion",

		// CPU directives
		[".p02"] = "arch 6502",
		[".p816"] = "arch 65816",
		[".smart"] = "smart",
		[".feature"] = "feature",

		// Output
		[".out"] = "print",
		[".warning"] = "warn",
		[".error"] = "error",
		[".fatal"] = "error",
		[".assert"] = "assert",

		// Linker directives
		[".reloc"] = "reloc",
		[".addr"] = "addr",
		[".faraddr"] = "faraddr",
		[".bankbytes"] = "bankbytes",
	};

	/// <summary>
	/// ca65 directives that have no PASM equivalent and should generate warnings.
	/// </summary>
	public static readonly IReadOnlySet<string> Ca65Unsupported = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
		".constructor",
		".destructor",
		".debuginfo",
		".dbg",
		".paramcount",
		".condes",
		".forceimport",
		".autoimport",
	};

	// ========================================================================
	// xkas Directive Mappings
	// ========================================================================

	/// <summary>
	/// Maps xkas directives to their PASM equivalents.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, string> XkasToPasm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
		// Data directives
		["db"] = "db",
		["dw"] = "dw",
		["dl"] = "dl",
		["dd"] = "dd",

		// Text directives
		["table"] = "table",
		["cleartable"] = "cleartable",

		// Include directives
		["incsrc"] = "include",
		["incbin"] = "incbin",

		// Organization directives
		["org"] = "org",
		["base"] = "base",

		// Fill directives
		["fill"] = "fill",
		["fillbyte"] = "fillbyte",

		// Architecture
		["arch"] = "arch",

		// Header directives
		["header"] = "header",
		["lorom"] = "lorom",
		["hirom"] = "hirom",
	};

	/// <summary>
	/// xkas directives that have no PASM equivalent and should generate warnings.
	/// </summary>
	public static readonly IReadOnlySet<string> XkasUnsupported = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
		"rep",      // Repeat (may be supported differently)
	};

	// ========================================================================
	// Helper Methods
	// ========================================================================

	/// <summary>
	/// Gets the directive mapping for a specific source assembler.
	/// </summary>
	/// <param name="assembler">The source assembler name.</param>
	/// <returns>The directive mapping dictionary.</returns>
	public static IReadOnlyDictionary<string, string> GetMapping(string assembler) {
		return assembler.ToUpperInvariant() switch {
			"ASAR" => AsarToPasm,
			"CA65" => Ca65ToPasm,
			"XKAS" => XkasToPasm,
			_ => throw new ArgumentException($"Unknown assembler: {assembler}", nameof(assembler))
		};
	}

	/// <summary>
	/// Gets the unsupported directives set for a specific source assembler.
	/// </summary>
	/// <param name="assembler">The source assembler name.</param>
	/// <returns>The set of unsupported directive names.</returns>
	public static IReadOnlySet<string> GetUnsupported(string assembler) {
		return assembler.ToUpperInvariant() switch {
			"ASAR" => AsarUnsupported,
			"CA65" => Ca65Unsupported,
			"XKAS" => XkasUnsupported,
			_ => throw new ArgumentException($"Unknown assembler: {assembler}", nameof(assembler))
		};
	}

	/// <summary>
	/// Translates a directive from a source assembler to PASM.
	/// </summary>
	/// <param name="assembler">The source assembler name.</param>
	/// <param name="directive">The directive to translate.</param>
	/// <param name="pasmDirective">The translated PASM directive.</param>
	/// <returns>True if the directive was found and translated.</returns>
	public static bool TryTranslate(string assembler, string directive, out string? pasmDirective) {
		var mapping = GetMapping(assembler);
		if (mapping.TryGetValue(directive, out pasmDirective)) {
			return true;
		}

		pasmDirective = null;
		return false;
	}

	/// <summary>
	/// Checks if a directive is known to be unsupported.
	/// </summary>
	/// <param name="assembler">The source assembler name.</param>
	/// <param name="directive">The directive to check.</param>
	/// <returns>True if the directive is explicitly unsupported.</returns>
	public static bool IsUnsupported(string assembler, string directive) {
		return GetUnsupported(assembler).Contains(directive);
	}
}
