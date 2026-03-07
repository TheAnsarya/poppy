// ============================================================================
// PansySourceMapTests.cs - Unit Tests for Source Map Generation via ListingGenerator
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Pansy.Core;
using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;
using Xunit;

using PoppyLexer = Poppy.Core.Lexer.Lexer;
using PoppyParser = Poppy.Core.Parser.Parser;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for source map generation in Pansy output when ListingGenerator is provided.
/// </summary>
public sealed class PansySourceMapTests {
	private static SourceLocation Loc(string file, int line, int col) => new(file, line, col, 0);

	[Fact]
	public void SourceMap_WithListingEntries_WritesSourceMapSection() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var listing = new ListingGenerator();

		// Simulate assembled instructions with source locations
		listing.AddEntry(0x8000, [0xa9, 0x42], Loc("main.pasm", 5, 1));    // lda #$42
		listing.AddEntry(0x8002, [0x8d, 0x00, 0x20], Loc("main.pasm", 6, 1)); // sta $2000
		listing.AddEntry(0x8005, [0x60], Loc("main.pasm", 7, 1));           // rts

		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments, listing);
		generator.ProjectName = "TestProject";

		// Act
		var data = generator.Generate(romSize: 0x8000, compress: false);

		// Assert — load with PansyLoader and verify source map
		var loader = new PansyLoader(data);
		Assert.True(loader.Flags.HasFlag(PansyFlags.HasSourceMap));
		Assert.NotEmpty(loader.SourceFiles);
		Assert.Contains("main.pasm", loader.SourceFiles);
		Assert.True(loader.SourceMapEntries.Count >= 3);
	}

	[Fact]
	public void SourceMap_WithMultipleFiles_TracksAllFiles() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var listing = new ListingGenerator();

		listing.AddEntry(0x8000, [0xa9, 0x00], Loc("main.pasm", 1, 1));
		listing.AddEntry(0x8002, [0x20, 0x00, 0x90], Loc("main.pasm", 2, 1));
		listing.AddEntry(0x9000, [0xa2, 0xff], Loc("lib.pasm", 10, 1));
		listing.AddEntry(0x9002, [0x60], Loc("lib.pasm", 11, 1));

		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments, listing);

		// Act
		var data = generator.Generate(romSize: 0x10000, compress: false);

		// Assert
		var loader = new PansyLoader(data);
		Assert.True(loader.Flags.HasFlag(PansyFlags.HasSourceMap));
		Assert.True(loader.SourceFiles.Count >= 2);
		Assert.Contains("main.pasm", loader.SourceFiles);
		Assert.Contains("lib.pasm", loader.SourceFiles);
	}

	[Fact]
	public void SourceMap_WithoutListing_NoSourceMapSection() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		// No listing generator provided
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);

		// Act
		var data = generator.Generate(romSize: 0x8000, compress: false);

		// Assert
		var loader = new PansyLoader(data);
		Assert.False(loader.Flags.HasFlag(PansyFlags.HasSourceMap));
		Assert.Empty(loader.SourceMapEntries);
	}

	[Fact]
	public void SourceMap_PreservesLineAndColumn() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var listing = new ListingGenerator();

		listing.AddEntry(0x8000, [0xea], Loc("test.pasm", 42, 5));

		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments, listing);

		// Act
		var data = generator.Generate(romSize: 0x8000, compress: false);

		// Assert
		var loader = new PansyLoader(data);
		Assert.True(loader.Flags.HasFlag(PansyFlags.HasSourceMap));

		var entry = loader.SourceMapEntries.FirstOrDefault(e => e.Line == 42);
		Assert.NotNull(entry);
		Assert.Equal(42, entry.Line);
		Assert.Equal(5, entry.Column);
	}

	[Fact]
	public void CodeGenerator_WithListingGenerator_PopulatesEntries() {
		// Arrange — use full pipeline: source → lex → parse → analyze → generate
		var source = """
			.org $8000
			lda #$42
			sta $2000
			rts
			""";

		var lexer = new PoppyLexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new PoppyParser(tokens);
		var program = parser.Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		var listing = new ListingGenerator();
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6502, listingGenerator: listing);

		// Act
		var code = generator.Generate(program);

		// Assert
		Assert.False(generator.HasErrors);
		Assert.True(listing.Entries.Count > 0, "ListingGenerator should have entries from code generation");

		// Each instruction should have entries
		Assert.True(listing.Entries.Count >= 3, "Should have at least 3 entries (lda, sta, rts)");
	}
}
