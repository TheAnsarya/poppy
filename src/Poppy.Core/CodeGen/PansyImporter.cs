// ============================================================================
// PansyImporter.cs - Import Symbols from Pansy Metadata Files
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Pansy.Core;
using Poppy.Core.Lexer;
using Poppy.Core.Semantics;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Imports symbols from a Pansy metadata file into a Poppy symbol table.
/// Source-defined symbols take precedence over imported symbols.
/// </summary>
public sealed class PansyImporter {
	private readonly SymbolTable _symbolTable;

	/// <summary>
	/// Number of symbols successfully imported.
	/// </summary>
	public int ImportedCount { get; private set; }

	/// <summary>
	/// Number of symbols skipped because they were already defined in source.
	/// </summary>
	public int SkippedCount { get; private set; }

	/// <summary>
	/// Symbols that were skipped (source wins).
	/// </summary>
	public IReadOnlyList<string> SkippedSymbols => _skippedSymbols;
	private readonly List<string> _skippedSymbols = [];

	/// <summary>
	/// Creates a new PansyImporter.
	/// </summary>
	/// <param name="symbolTable">The symbol table to import into.</param>
	public PansyImporter(SymbolTable symbolTable) {
		_symbolTable = symbolTable;
	}

	/// <summary>
	/// Imports symbols from a Pansy file.
	/// Symbols already defined in the source take precedence (source wins).
	/// </summary>
	/// <param name="pansyFilePath">Path to the .pansy file.</param>
	public void Import(string pansyFilePath) {
		var data = File.ReadAllBytes(pansyFilePath);
		var loader = new PansyLoader(data);

		var importLocation = new SourceLocation(pansyFilePath, 0, 0, 0);

		foreach (var (address, entry) in loader.SymbolEntries) {
			var symbolType = MapSymbolType(entry.Type);

			if (_symbolTable.TryGetSymbol(entry.Name, out var existing) && existing!.IsDefined) {
				// Source-defined symbol takes precedence
				SkippedCount++;
				_skippedSymbols.Add(entry.Name);
				continue;
			}

			_symbolTable.Define(entry.Name, symbolType, address, importLocation);
			ImportedCount++;
		}
	}

	/// <summary>
	/// Maps Pansy SymbolType to Poppy SymbolType.
	/// </summary>
	private static Semantics.SymbolType MapSymbolType(Pansy.Core.SymbolType pansyType) {
		return pansyType switch {
			Pansy.Core.SymbolType.Label => Semantics.SymbolType.Label,
			Pansy.Core.SymbolType.Constant => Semantics.SymbolType.Constant,
			Pansy.Core.SymbolType.Function => Semantics.SymbolType.Label,
			Pansy.Core.SymbolType.InterruptVector => Semantics.SymbolType.Label,
			Pansy.Core.SymbolType.Local => Semantics.SymbolType.Label,
			Pansy.Core.SymbolType.Anonymous => Semantics.SymbolType.Label,
			Pansy.Core.SymbolType.Macro => Semantics.SymbolType.Macro,
			_ => Semantics.SymbolType.Label,
		};
	}
}
