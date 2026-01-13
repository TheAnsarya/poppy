// ============================================================================
// Symbol.cs - Symbol Table Entry
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;

namespace Poppy.Core.Semantics;

/// <summary>
/// Represents a symbol in the symbol table.
/// </summary>
public sealed class Symbol {
	/// <summary>
	/// The name of the symbol.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The type of the symbol.
	/// </summary>
	public SymbolType Type { get; }

	/// <summary>
	/// The value of the symbol (address or constant value).
	/// </summary>
	public long? Value { get; set; }

	/// <summary>
	/// Whether this symbol has been defined.
	/// </summary>
	public bool IsDefined { get; set; }

	/// <summary>
	/// The source location where this symbol was defined.
	/// </summary>
	public SourceLocation? DefinitionLocation { get; set; }

	/// <summary>
	/// List of locations where this symbol is referenced.
	/// </summary>
	public List<SourceLocation> References { get; } = [];

	/// <summary>
	/// The parent scope (for local labels).
	/// </summary>
	public string? ParentScope { get; set; }

	/// <summary>
	/// Whether this symbol is exported (visible to other modules).
	/// </summary>
	public bool IsExported { get; set; }

	/// <summary>
	/// Creates a new symbol.
	/// </summary>
	/// <param name="name">The name of the symbol.</param>
	/// <param name="type">The type of the symbol.</param>
	public Symbol(string name, SymbolType type) {
		Name = name;
		Type = type;
		IsDefined = false;
	}

	/// <inheritdoc />
	public override string ToString() {
		var valueStr = Value.HasValue ? $" = ${Value:x}" : " (undefined)";
		return $"{Type} {Name}{valueStr}";
	}
}

/// <summary>
/// Types of symbols in the symbol table.
/// </summary>
public enum SymbolType {
	/// <summary>A label representing a code or data address.</summary>
	Label,

	/// <summary>A constant value defined with .equ or =.</summary>
	Constant,

	/// <summary>A macro definition.</summary>
	Macro,

	/// <summary>An external symbol (imported from another module).</summary>
	External,
}

/// <summary>
/// Symbol table for managing labels, constants, and macros.
/// </summary>
public sealed class SymbolTable {
	private readonly Dictionary<string, Symbol> _symbols = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<SemanticError> _errors = [];
	private readonly List<(long Address, SourceLocation Location)> _anonymousForward = [];
	private readonly List<(long Address, SourceLocation Location)> _anonymousBackward = [];
	private readonly Dictionary<string, List<(long Address, SourceLocation Location)>> _namedAnonymous = new(StringComparer.OrdinalIgnoreCase);
	private string? _currentScope;

	/// <summary>
	/// Gets all symbols in the table.
	/// </summary>
	public IReadOnlyDictionary<string, Symbol> Symbols => _symbols;

	/// <summary>
	/// Gets all semantic errors encountered.
	/// </summary>
	public IReadOnlyList<SemanticError> Errors => _errors;

	/// <summary>
	/// Gets or sets the current scope (parent label for local labels).
	/// </summary>
	public string? CurrentScope {
		get => _currentScope;
		set => _currentScope = value;
	}

	/// <summary>
	/// Defines a new symbol or returns an existing one.
	/// </summary>
	/// <param name="name">The symbol name.</param>
	/// <param name="type">The symbol type.</param>
	/// <param name="value">The symbol value (if known).</param>
	/// <param name="location">The definition location.</param>
	/// <returns>The symbol.</returns>
	public Symbol Define(string name, SymbolType type, long? value, SourceLocation location) {
		var fullName = GetFullName(name);

		if (_symbols.TryGetValue(fullName, out var existing)) {
			if (existing.IsDefined) {
				_errors.Add(new SemanticError(
					$"Symbol '{name}' already defined at {existing.DefinitionLocation}",
					location));
				return existing;
			}

			// Symbol was forward-referenced, now defining it
			existing.IsDefined = true;
			existing.Value = value;
			existing.DefinitionLocation = location;
			return existing;
		}

		var symbol = new Symbol(fullName, type) {
			IsDefined = true,
			Value = value,
			DefinitionLocation = location,
			ParentScope = IsLocalName(name) ? _currentScope : null
		};

		_symbols[fullName] = symbol;

		// Update current scope for non-local labels
		if (type == SymbolType.Label && !IsLocalName(name)) {
			_currentScope = name;
		}

		return symbol;
	}

	/// <summary>
	/// References a symbol (creates forward reference if not defined).
	/// </summary>
	/// <param name="name">The symbol name.</param>
	/// <param name="location">The reference location.</param>
	/// <returns>The symbol, or null if it cannot be resolved.</returns>
	public Symbol? Reference(string name, SourceLocation location) {
		var fullName = GetFullName(name);

		if (_symbols.TryGetValue(fullName, out var existing)) {
			existing.References.Add(location);
			return existing;
		}

		// Create forward reference
		var symbol = new Symbol(fullName, SymbolType.Label) {
			IsDefined = false,
			ParentScope = IsLocalName(name) ? _currentScope : null
		};
		symbol.References.Add(location);

		_symbols[fullName] = symbol;
		return symbol;
	}

	/// <summary>
	/// Tries to get a symbol by name.
	/// </summary>
	/// <param name="name">The symbol name.</param>
	/// <param name="symbol">The symbol if found.</param>
	/// <returns>True if the symbol exists.</returns>
	public bool TryGetSymbol(string name, out Symbol? symbol) {
		var fullName = GetFullName(name);
		return _symbols.TryGetValue(fullName, out symbol);
	}

	/// <summary>
	/// Checks for undefined symbols and reports errors.
	/// </summary>
	public void ValidateAllDefined() {
		foreach (var symbol in _symbols.Values) {
			if (!symbol.IsDefined && symbol.References.Count > 0) {
				var firstRef = symbol.References[0];
				_errors.Add(new SemanticError(
					$"Undefined symbol: '{symbol.Name}'",
					firstRef));
			}
		}
	}

	/// <summary>
	/// Defines an anonymous label at the current address.
	/// </summary>
	/// <param name="isForward">True for + labels, false for - labels.</param>
	/// <param name="address">The address of the label.</param>
	/// <param name="location">The source location.</param>
	public void DefineAnonymousLabel(bool isForward, long address, SourceLocation location) {
		var list = isForward ? _anonymousForward : _anonymousBackward;
		list.Add((address, location));
	}

	/// <summary>
	/// Resolves an anonymous label reference.
	/// </summary>
	/// <param name="isForward">True for + references (look ahead), false for - (look back).</param>
	/// <param name="count">Number of +/- symbols (e.g., ++ = 2).</param>
	/// <param name="currentAddress">The current address for resolving relative references.</param>
	/// <param name="location">The source location of the reference.</param>
	/// <returns>The address of the anonymous label, or null if not found.</returns>
	public long? ResolveAnonymousLabel(bool isForward, int count, long currentAddress, SourceLocation location) {
		if (isForward) {
			// Find the Nth + label after the current address
			var candidates = _anonymousForward.Where(l => l.Address > currentAddress).ToList();
			if (count <= candidates.Count) {
				return candidates[count - 1].Address;
			}
			_errors.Add(new SemanticError($"Cannot find anonymous forward label (+ x{count})", location));
			return null;
		} else {
			// Find the Nth - label at or before the current address
			var candidates = _anonymousBackward.Where(l => l.Address <= currentAddress).Reverse().ToList();
			if (count <= candidates.Count) {
				return candidates[count - 1].Address;
			}
			_errors.Add(new SemanticError($"Cannot find anonymous backward label (- x{count})", location));
			return null;
		}
	}

	/// <summary>
	/// Defines a named anonymous label at the current address.
	/// Named anonymous labels use +name or -name syntax.
	/// </summary>
	/// <param name="name">The label name including prefix (e.g., "+loop" or "-loop").</param>
	/// <param name="address">The address of the label.</param>
	/// <param name="location">The source location.</param>
	public void DefineNamedAnonymousLabel(string name, long address, SourceLocation location) {
		// Get or create the list for this name (scope by current parent scope)
		var scopedName = _currentScope is not null ? $"{_currentScope}:{name}" : name;
		if (!_namedAnonymous.TryGetValue(scopedName, out var list)) {
			list = [];
			_namedAnonymous[scopedName] = list;
		}
		list.Add((address, location));
	}

	/// <summary>
	/// Resolves a named anonymous label reference.
	/// For +name: finds the next occurrence after the current address.
	/// For -name: finds the previous occurrence at or before the current address.
	/// </summary>
	/// <param name="name">The label reference (e.g., "+loop" or "-loop").</param>
	/// <param name="currentAddress">The current address for resolving relative references.</param>
	/// <param name="location">The source location of the reference.</param>
	/// <returns>The address of the named anonymous label, or null if not found.</returns>
	public long? ResolveNamedAnonymousLabel(string name, long currentAddress, SourceLocation location) {
		bool isForward = name.StartsWith('+');
		// Get the base name without prefix
		var baseName = name[1..];
		// Construct the lookup name with the forward prefix (labels are always defined with +)
		var lookupName = $"+{baseName}";
		var scopedName = _currentScope is not null ? $"{_currentScope}:{lookupName}" : lookupName;

		if (!_namedAnonymous.TryGetValue(scopedName, out var list) || list.Count == 0) {
			_errors.Add(new SemanticError($"Cannot find named anonymous label '{name}'", location));
			return null;
		}

		if (isForward) {
			// Find the first label after the current address
			var candidates = list.Where(l => l.Address > currentAddress).ToList();
			if (candidates.Count > 0) {
				return candidates[0].Address;
			}
			_errors.Add(new SemanticError($"Cannot find named anonymous forward label '{name}'", location));
			return null;
		} else {
			// Find the last label at or before the current address
			var candidates = list.Where(l => l.Address <= currentAddress).ToList();
			if (candidates.Count > 0) {
				return candidates[^1].Address;
			}
			_errors.Add(new SemanticError($"Cannot find named anonymous backward label '{name}'", location));
			return null;
		}
	}

	/// <summary>
	/// Clears anonymous labels (for multi-pass compilation).
	/// </summary>
	public void ClearAnonymousLabels() {
		_anonymousForward.Clear();
		_anonymousBackward.Clear();
		_namedAnonymous.Clear();
	}

	/// <summary>
	/// Gets the full name for a symbol (handling local labels).
	/// </summary>
	private string GetFullName(string name) {
		if (IsLocalName(name) && _currentScope is not null) {
			return $"{_currentScope}{name}";
		}
		return name;
	}

	/// <summary>
	/// Checks if a name is a local label (starts with . or @).
	/// </summary>
	private static bool IsLocalName(string name) {
		return name.Length > 0 && (name[0] == '.' || name[0] == '@');
	}
}

/// <summary>
/// Represents a semantic analysis error.
/// </summary>
public sealed class SemanticError {
	/// <summary>
	/// The error message.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// The source location where the error occurred.
	/// </summary>
	public SourceLocation Location { get; }

	/// <summary>
	/// Creates a new semantic error.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="location">The source location.</param>
	public SemanticError(string message, SourceLocation location) {
		Message = message;
		Location = location;
	}

	/// <inheritdoc />
	public override string ToString() => $"{Location}: error: {Message}";
}

