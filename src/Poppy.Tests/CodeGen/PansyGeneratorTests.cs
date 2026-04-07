// ============================================================================
// PansyGeneratorTests.cs - Unit Tests for Pansy File Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text;
using Pansy.Core;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for the Pansy (Program ANalysis SYstem) file generator.
/// </summary>
public sealed class PansyGeneratorTests {
	// Helper to create a dummy source location
	private static SourceLocation DummyLocation => new("test.pasm", 1, 1, 0);

	[Fact]
	public void Generate_CreatesValidHeader() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);

		// Act
		var data = generator.Generate(romSize: 0x8000);

		// Assert
		Assert.NotNull(data);
		Assert.True(data.Length >= 32, "Header should be at least 32 bytes");

		// Check magic "PANSY\0\0\0"
		Assert.Equal((byte)'P', data[0]);
		Assert.Equal((byte)'A', data[1]);
		Assert.Equal((byte)'N', data[2]);
		Assert.Equal((byte)'S', data[3]);
		Assert.Equal((byte)'Y', data[4]);
		Assert.Equal(0, data[5]);
		Assert.Equal(0, data[6]);
		Assert.Equal(0, data[7]);

		// Check version (0x0100 = v1.0)
		var version = BitConverter.ToUInt16(data, 8);
		Assert.Equal(0x0100, version);
	}

	[Fact]
	public void Generate_SetsPlatformId_ForNes() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);

		// Act
		var data = generator.Generate(romSize: 0x8000);

		// Assert
		Assert.Equal(PansyLoader.PLATFORM_NES, data[12]);
	}

	[Fact]
	public void Generate_SetsPlatformId_ForSnes() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.WDC65816, segments);

		// Act
		var data = generator.Generate(romSize: 0x80000);

		// Assert
		Assert.Equal(PansyLoader.PLATFORM_SNES, data[12]);
	}

	[Fact]
	public void Generate_SetsPlatformId_ForGameBoy() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.SM83, segments);

		// Act
		var data = generator.Generate(romSize: 0x8000);

		// Assert
		Assert.Equal(PansyLoader.PLATFORM_GB, data[12]);
	}

	[Fact]
	public void Generate_WritesRomSize() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);
		const int expectedRomSize = 0x10000;

		// Act
		var data = generator.Generate(romSize: expectedRomSize);

		// Assert
		var romSize = BitConverter.ToUInt32(data, 16);
		Assert.Equal((uint)expectedRomSize, romSize);
	}

	[Fact]
	public void Generate_WritesRomCrc32() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);
		const uint expectedCrc = 0xdeadbeef;

		// Act
		var data = generator.Generate(romSize: 0x8000, romCrc32: expectedCrc);

		// Assert
		var crc = BitConverter.ToUInt32(data, 20);
		Assert.Equal(expectedCrc, crc);
	}

	[Fact]
	public void Generate_IncludesSymbolSection() {
		// Arrange
		var symbolTable = new SymbolTable();
		symbolTable.Define("TestLabel", Poppy.Core.Semantics.SymbolType.Label, 0x8000, DummyLocation);
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);

		// Act
		var data = generator.Generate(romSize: 0x8000);

		// Assert
		// Section count should be > 0
		var sectionCount = BitConverter.ToUInt32(data, 24);
		Assert.True(sectionCount > 0, "Should have at least one section");
	}

	[Fact]
	public void Generate_IncludesMetadataSection() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments) {
			ProjectName = "Test Project",
			Author = "Test Author",
			Version = "2.0.0"
		};

		// Act
		var data = generator.Generate(romSize: 0x8000, compress: false);

		// Assert
		// The metadata section should be present
		var sectionCount = BitConverter.ToUInt32(data, 24);
		Assert.True(sectionCount >= 1, "Should have at least one section for metadata");

		// Metadata should be somewhere in the file
		var dataStr = Encoding.UTF8.GetString(data);
		Assert.Contains("Test Project", dataStr);
	}

	[Fact]
	public void Generate_CompressesDataByDefault() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment> { new(0x8000) };
		// Add some data to the segment
		for (int i = 0; i < 1000; i++) {
			segments[0].Data.Add(0xea); // NOP instruction (should compress well)
		}
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);

		// Act
		var compressedData = generator.Generate(romSize: 0x10000, compress: true);
		var uncompressedData = generator.Generate(romSize: 0x10000, compress: false);

		// Assert
		// Note: Small files might not compress smaller, but the flag should be set
		var flags = BitConverter.ToUInt16(compressedData, 10);
		Assert.True((flags & 0x0001) != 0, "Compressed flag should be set");
	}

	[Fact]
	public void Export_CreatesFile() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);
		var tempFile = Path.GetTempFileName() + ".pansy";

		try {
			// Act
			generator.Export(tempFile, romSize: 0x8000);

			// Assert
			Assert.True(File.Exists(tempFile));
			var data = File.ReadAllBytes(tempFile);
			Assert.True(data.Length >= 32);
			Assert.Equal((byte)'P', data[0]);
			Assert.Equal((byte)'A', data[1]);
			Assert.Equal((byte)'N', data[2]);
			Assert.Equal((byte)'S', data[3]);
			Assert.Equal((byte)'Y', data[4]);
		} finally {
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	[Fact]
	public void RegisterCrossRef_TracksReferences() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);

		// Act
		generator.RegisterCrossRef(0x8000, 0x8100, Pansy.Core.CrossRefType.Jsr);
		generator.RegisterCrossRef(0x8010, 0x8200, Pansy.Core.CrossRefType.Jmp);
		var data = generator.Generate(romSize: 0x10000, compress: false);

		// Assert - cross-refs section should exist
		var sectionCount = BitConverter.ToInt32(data, 24);
		Assert.True(sectionCount > 0);
	}

	[Fact]
	public void PansyCorePlatformIds_HaveCorrectValues() {
		// Verify Pansy.Core platform IDs match the specification
		Assert.Equal(0x01, PansyLoader.PLATFORM_NES);
		Assert.Equal(0x02, PansyLoader.PLATFORM_SNES);
		Assert.Equal(0x03, PansyLoader.PLATFORM_GB);
		Assert.Equal(0x04, PansyLoader.PLATFORM_GBA);
		Assert.Equal(0x05, PansyLoader.PLATFORM_GENESIS);
		Assert.Equal(0x06, PansyLoader.PLATFORM_SMS);
		Assert.Equal(0x07, PansyLoader.PLATFORM_PCE);
		Assert.Equal(0x08, PansyLoader.PLATFORM_ATARI_2600);
		Assert.Equal(0x09, PansyLoader.PLATFORM_LYNX);
		Assert.Equal(0x0a, PansyLoader.PLATFORM_WONDERSWAN);
		Assert.Equal(0xff, PansyLoader.PLATFORM_CUSTOM);
	}

	[Fact]
	public void RegisterComment_GeneratesCommentsSection() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);
		generator.RegisterComment(0x8000, "Entry point", 1); // Inline
		generator.RegisterComment(0x8003, "Main loop", 1); // Inline

		// Act
		var data = generator.Generate(romSize: 0x8000, compress: false);

		// Assert - file should contain SECTION_COMMENTS (0x0003) somewhere
		bool foundCommentsSection = false;
		// Parse section table after 32-byte header
		int sectionCount = BitConverter.ToInt32(data, 24);
		int offset = 32; // after header
		for (int i = 0; i < sectionCount; i++) {
			var sectionType = BitConverter.ToUInt32(data, offset);
			if (sectionType == 0x0003) { // SECTION_COMMENTS
				foundCommentsSection = true;
				break;
			}
			offset += 16; // type(4) + offset(4) + compressedSize(4) + uncompressedSize(4)
		}
		Assert.True(foundCommentsSection, "Comments section (0x0003) should be present");
	}

	[Fact]
	public void RegisterComment_NoComments_NoSection() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);

		// Act
		var data = generator.Generate(romSize: 0x8000, compress: false);

		// Assert - should NOT contain SECTION_COMMENTS
		int sectionCount = BitConverter.ToInt32(data, 24);
		int offset = 32;
		for (int i = 0; i < sectionCount; i++) {
			var sectionType = BitConverter.ToUInt32(data, offset);
			Assert.NotEqual(0x0003u, sectionType); // SECTION_COMMENTS
			offset += 16;
		}
	}

	[Fact]
	public void PansyCoreCommentTypes_MatchSpec() {
		Assert.Equal((byte)1, (byte)CommentType.Inline);
		Assert.Equal((byte)2, (byte)CommentType.Block);
		Assert.Equal((byte)3, (byte)CommentType.Todo);
	}

	[Fact]
	public void PopulateCrossRefsFromCodeGenerator_PopulatesCrossRefs() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);

		var crossRefs = new List<(uint From, uint To, byte Type)> {
			(0x8000, 0x8100, 1), // Jsr
			(0x8010, 0x8200, 2), // Jmp
			(0x8020, 0x8050, 3), // Branch
		};

		// Act
		generator.PopulateCrossRefsFromCodeGenerator(crossRefs);
		var data = generator.Generate(romSize: 0x10000, compress: false);

		// Assert - HasCrossRefs flag should be set
		var flags = BitConverter.ToUInt16(data, 10);
		Assert.True((flags & 0x0004) != 0, "HasCrossRefs flag should be set");

		// Find cross-refs section
		bool foundCrossRefsSection = false;
		int sectionCount = BitConverter.ToInt32(data, 24);
		int offset = 32;
		for (int i = 0; i < sectionCount; i++) {
			var sectionType = BitConverter.ToUInt32(data, offset);
			if (sectionType == 0x0006) { // SECTION_CROSS_REFS
				foundCrossRefsSection = true;
				break;
			}
			offset += 16;
		}
		Assert.True(foundCrossRefsSection, "Cross-refs section (0x0006) should be present");
	}

	[Fact]
	public void PopulateCrossRefsFromCodeGenerator_EmptyList_NoCrossRefSection() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);

		// Act - populate with empty list
		generator.PopulateCrossRefsFromCodeGenerator([]);
		var data = generator.Generate(romSize: 0x8000, compress: false);

		// Assert - no cross-refs flag (0x0004)
		var flags = BitConverter.ToUInt16(data, 10);
		Assert.True((flags & 0x0004) == 0, "HasCrossRefs flag should not be set");
	}

	[Fact]
	public void PansyCoreCrossRefTypes_MatchSpec() {
		Assert.Equal((byte)1, (byte)Pansy.Core.CrossRefType.Jsr);
		Assert.Equal((byte)2, (byte)Pansy.Core.CrossRefType.Jmp);
		Assert.Equal((byte)3, (byte)Pansy.Core.CrossRefType.Branch);
		Assert.Equal((byte)4, (byte)Pansy.Core.CrossRefType.Read);
		Assert.Equal((byte)5, (byte)Pansy.Core.CrossRefType.Write);
	}

	[Fact]
	public void CodeGenerator_TracksJsrCrossRef() {
		// Assemble "jsr $8100" at address $8000
		var source = ".org $8000\njsr $8100\n";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		var cdlGen = new CdlGenerator(analyzer.SymbolTable, TargetArchitecture.MOS6502, []);
		var codeGen = new CodeGenerator(analyzer, TargetArchitecture.MOS6502, cdlGen);
		codeGen.Generate(program);

		// Assert cross-refs contain JSR entry
		Assert.Single(codeGen.CrossReferences);
		var xref = codeGen.CrossReferences[0];
		Assert.Equal((uint)0x8000, xref.From);
		Assert.Equal((uint)0x8100, xref.To);
		Assert.Equal((byte)1, xref.Type); // Jsr=1
	}

	[Fact]
	public void CodeGenerator_TracksJmpCrossRef() {
		// Assemble "jmp $8200" at address $8000
		var source = ".org $8000\njmp $8200\n";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		var cdlGen = new CdlGenerator(analyzer.SymbolTable, TargetArchitecture.MOS6502, []);
		var codeGen = new CodeGenerator(analyzer, TargetArchitecture.MOS6502, cdlGen);
		codeGen.Generate(program);

		Assert.Single(codeGen.CrossReferences);
		var xref = codeGen.CrossReferences[0];
		Assert.Equal((uint)0x8000, xref.From);
		Assert.Equal((uint)0x8200, xref.To);
		Assert.Equal((byte)2, xref.Type); // Jmp=2
	}

	[Fact]
	public void CodeGenerator_TracksConditionalBranchCrossRef() {
		// Assemble "loop: nop\nbne loop" — bne should generate branch cross-ref
		var source = ".org $8000\nloop:\nnop\nbne loop\n";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		var cdlGen = new CdlGenerator(analyzer.SymbolTable, TargetArchitecture.MOS6502, []);
		var codeGen = new CodeGenerator(analyzer, TargetArchitecture.MOS6502, cdlGen);
		codeGen.Generate(program);

		// Should have one branch cross-ref
		Assert.Single(codeGen.CrossReferences);
		var xref = codeGen.CrossReferences[0];
		Assert.Equal((uint)0x8001, xref.From); // bne is at $8001 (after nop)
		Assert.Equal((uint)0x8000, xref.To);   // target is "loop" at $8000
		Assert.Equal((byte)3, xref.Type);       // Branch=3
	}

	[Fact]
	public void CodeGenerator_CrossRefsFlowToPansyGenerator() {
		// Full pipeline: assemble → cross-refs → PansyGenerator
		var source = ".org $8000\njsr $8100\njmp $8200\n";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		var cdlGen = new CdlGenerator(analyzer.SymbolTable, TargetArchitecture.MOS6502, []);
		var codeGen = new CodeGenerator(analyzer, TargetArchitecture.MOS6502, cdlGen);
		var code = codeGen.Generate(program);

		// Create PansyGenerator and populate cross-refs
		var pansyGen = new PansyGenerator(
			analyzer.SymbolTable,
			TargetArchitecture.MOS6502,
			codeGen.Segments,
			cdlGenerator: cdlGen);
		pansyGen.PopulateCrossRefsFromCodeGenerator(codeGen.CrossReferences);

		var data = pansyGen.Generate(romSize: code.Length, compress: false);

		// Assert cross-refs section present
		var flags = BitConverter.ToUInt16(data, 10);
		Assert.True((flags & 0x0004) != 0, "HasCrossRefs flag should be set");

		// Should have 2 cross-refs (JSR + JMP)
		Assert.Equal(2, codeGen.CrossReferences.Count);
	}
}
