// ============================================================================
// PansyImporterTests.cs - Unit Tests for Pansy Symbol Import
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Pansy.Core;
using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for importing symbols from Pansy metadata files into the Poppy symbol table.
/// </summary>
public sealed class PansyImporterTests : IDisposable {
	private readonly string _tempDir;

	public PansyImporterTests() {
		_tempDir = Path.Combine(Path.GetTempPath(), $"poppy-test-{Guid.NewGuid():N}");
		Directory.CreateDirectory(_tempDir);
	}

	public void Dispose() {
		if (Directory.Exists(_tempDir)) {
			Directory.Delete(_tempDir, recursive: true);
		}
	}

	private string CreatePansyFile(Action<PansyWriter> configure) {
		var writer = new PansyWriter();
		writer.Platform = PansyLoader.PLATFORM_NES;
		writer.RomSize = 0x8000;
		configure(writer);
		var data = writer.Generate();
		var path = Path.Combine(_tempDir, $"test-{Guid.NewGuid():N}.pansy");
		File.WriteAllBytes(path, data);
		return path;
	}

	[Fact]
	public void Import_ReadsSymbolsIntoSymbolTable() {
		// Arrange
		var pansyFile = CreatePansyFile(w => {
			w.AddSymbol(0x8000, "reset_handler", Pansy.Core.SymbolType.Label);
			w.AddSymbol(0x8100, "nmi_handler", Pansy.Core.SymbolType.Function);
			w.AddSymbol(0x00ff, "MAX_HEALTH", Pansy.Core.SymbolType.Constant);
		});

		var symbolTable = new SymbolTable();
		var importer = new PansyImporter(symbolTable);

		// Act
		importer.Import(pansyFile);

		// Assert
		Assert.Equal(3, importer.ImportedCount);
		Assert.Equal(0, importer.SkippedCount);

		Assert.True(symbolTable.TryGetSymbol("reset_handler", out var resetSym));
		Assert.Equal(0x8000, resetSym!.Value);
		Assert.True(resetSym.IsDefined);

		Assert.True(symbolTable.TryGetSymbol("nmi_handler", out var nmiSym));
		Assert.Equal(0x8100, nmiSym!.Value);

		Assert.True(symbolTable.TryGetSymbol("MAX_HEALTH", out var constSym));
		Assert.Equal(0x00ff, constSym!.Value);
		Assert.Equal(Poppy.Core.Semantics.SymbolType.Constant, constSym.Type);
	}

	[Fact]
	public void Import_SourceDefinedSymbolsWin() {
		// Arrange
		var pansyFile = CreatePansyFile(w => {
			w.AddSymbol(0x8000, "reset", Pansy.Core.SymbolType.Label);
			w.AddSymbol(0x9000, "other_label", Pansy.Core.SymbolType.Label);
		});

		var symbolTable = new SymbolTable();
		var sourceLocation = new SourceLocation("main.pasm", 10, 1, 0);
		symbolTable.Define("reset", Poppy.Core.Semantics.SymbolType.Label, 0xc000, sourceLocation);

		var importer = new PansyImporter(symbolTable);

		// Act
		importer.Import(pansyFile);

		// Assert — source-defined "reset" keeps its original value
		Assert.Equal(1, importer.ImportedCount);
		Assert.Equal(1, importer.SkippedCount);
		Assert.Contains("reset", importer.SkippedSymbols);

		Assert.True(symbolTable.TryGetSymbol("reset", out var resetSym));
		Assert.Equal(0xc000, resetSym!.Value); // Source value preserved

		Assert.True(symbolTable.TryGetSymbol("other_label", out var otherSym));
		Assert.Equal(0x9000, otherSym!.Value); // Imported
	}

	[Fact]
	public void Import_EmptyPansyFile_ImportsNothing() {
		// Arrange
		var pansyFile = CreatePansyFile(w => { });

		var symbolTable = new SymbolTable();
		var importer = new PansyImporter(symbolTable);

		// Act
		importer.Import(pansyFile);

		// Assert
		Assert.Equal(0, importer.ImportedCount);
		Assert.Equal(0, importer.SkippedCount);
	}

	[Fact]
	public void Import_MapsSymbolTypes() {
		// Arrange
		var pansyFile = CreatePansyFile(w => {
			w.AddSymbol(0x8000, "func_label", Pansy.Core.SymbolType.Function);
			w.AddSymbol(0x8100, "irq_vector", Pansy.Core.SymbolType.InterruptVector);
			w.AddSymbol(0x0042, "CONST_VAL", Pansy.Core.SymbolType.Constant);
		});

		var symbolTable = new SymbolTable();
		var importer = new PansyImporter(symbolTable);

		// Act
		importer.Import(pansyFile);

		// Assert
		Assert.True(symbolTable.TryGetSymbol("func_label", out var funcSym));
		Assert.Equal(Poppy.Core.Semantics.SymbolType.Label, funcSym!.Type);

		Assert.True(symbolTable.TryGetSymbol("irq_vector", out var irqSym));
		Assert.Equal(Poppy.Core.Semantics.SymbolType.Label, irqSym!.Type);

		Assert.True(symbolTable.TryGetSymbol("CONST_VAL", out var constSym));
		Assert.Equal(Poppy.Core.Semantics.SymbolType.Constant, constSym!.Type);
	}
}
