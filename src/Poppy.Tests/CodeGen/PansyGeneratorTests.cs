// ============================================================================
// PansyGeneratorTests.cs - Unit Tests for Pansy File Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text;
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
		Assert.Equal(PansyGenerator.PLATFORM_NES, data[12]);
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
		Assert.Equal(PansyGenerator.PLATFORM_SNES, data[12]);
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
		Assert.Equal(PansyGenerator.PLATFORM_GB, data[12]);
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
		symbolTable.Define("TestLabel", SymbolType.Label, 0x8000, DummyLocation);
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
		var flags = (PansyGenerator.PansyFlags)BitConverter.ToUInt16(compressedData, 10);
		Assert.True(flags.HasFlag(PansyGenerator.PansyFlags.Compressed));
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
		generator.RegisterCrossRef(0x8000, 0x8100, PansyGenerator.CrossRefType.Jsr);
		generator.RegisterCrossRef(0x8010, 0x8200, PansyGenerator.CrossRefType.Jmp);
		var data = generator.Generate(romSize: 0x10000, compress: false);

		// Assert
		var flags = (PansyGenerator.PansyFlags)BitConverter.ToUInt16(data, 10);
		Assert.True(flags.HasFlag(PansyGenerator.PansyFlags.HasCrossRefs));
	}

	[Fact]
	public void ByteFlags_HasCorrectValues() {
		// Verify the byte flags match the specification
		Assert.Equal(0x00, (byte)PansyGenerator.ByteFlags.None);
		Assert.Equal(0x01, (byte)PansyGenerator.ByteFlags.Code);
		Assert.Equal(0x02, (byte)PansyGenerator.ByteFlags.Data);
		Assert.Equal(0x04, (byte)PansyGenerator.ByteFlags.JumpTarget);
		Assert.Equal(0x08, (byte)PansyGenerator.ByteFlags.SubEntry);
		Assert.Equal(0x10, (byte)PansyGenerator.ByteFlags.Opcode);
		Assert.Equal(0x20, (byte)PansyGenerator.ByteFlags.Drawn);
		Assert.Equal(0x40, (byte)PansyGenerator.ByteFlags.Read);
		Assert.Equal(0x80, (byte)PansyGenerator.ByteFlags.Indirect);
	}

	[Fact]
	public void SectionTypes_HaveCorrectValues() {
		// Verify section types match the specification
		Assert.Equal(0x0001u, PansyGenerator.SECTION_CODE_DATA_MAP);
		Assert.Equal(0x0002u, PansyGenerator.SECTION_SYMBOLS);
		Assert.Equal(0x0003u, PansyGenerator.SECTION_COMMENTS);
		Assert.Equal(0x0004u, PansyGenerator.SECTION_MEMORY_REGIONS);
		Assert.Equal(0x0005u, PansyGenerator.SECTION_DATA_TYPES);
		Assert.Equal(0x0006u, PansyGenerator.SECTION_CROSS_REFS);
		Assert.Equal(0x0007u, PansyGenerator.SECTION_SOURCE_MAP);
		Assert.Equal(0x0008u, PansyGenerator.SECTION_METADATA);
	}

	[Fact]
	public void PlatformIds_HaveCorrectValues() {
		// Verify platform IDs match the specification
		Assert.Equal(0x01, PansyGenerator.PLATFORM_NES);
		Assert.Equal(0x02, PansyGenerator.PLATFORM_SNES);
		Assert.Equal(0x03, PansyGenerator.PLATFORM_GB);
		Assert.Equal(0x04, PansyGenerator.PLATFORM_GBA);
		Assert.Equal(0x05, PansyGenerator.PLATFORM_GENESIS);
		Assert.Equal(0x06, PansyGenerator.PLATFORM_SMS);
		Assert.Equal(0x07, PansyGenerator.PLATFORM_PCE);
		Assert.Equal(0x08, PansyGenerator.PLATFORM_ATARI_2600);
		Assert.Equal(0x09, PansyGenerator.PLATFORM_LYNX);
		Assert.Equal(0x0a, PansyGenerator.PLATFORM_WONDERSWAN);
		Assert.Equal(0x0b, PansyGenerator.PLATFORM_NEOGEO);
		Assert.Equal(0x0c, PansyGenerator.PLATFORM_SPC700);
		Assert.Equal(0x0d, PansyGenerator.PLATFORM_C64);
		Assert.Equal(0x0e, PansyGenerator.PLATFORM_MSX);
		Assert.Equal(0x0f, PansyGenerator.PLATFORM_ATARI_7800);
		Assert.Equal(0x10, PansyGenerator.PLATFORM_ATARI_8BIT);
		Assert.Equal(0x11, PansyGenerator.PLATFORM_APPLE_II);
		Assert.Equal(0x12, PansyGenerator.PLATFORM_ZX_SPECTRUM);
		Assert.Equal(0x13, PansyGenerator.PLATFORM_COLECO);
		Assert.Equal(0x14, PansyGenerator.PLATFORM_INTELLIVISION);
		Assert.Equal(0x15, PansyGenerator.PLATFORM_VECTREX);
		Assert.Equal(0x16, PansyGenerator.PLATFORM_GAMEGEAR);
		Assert.Equal(0x17, PansyGenerator.PLATFORM_32X);
		Assert.Equal(0x18, PansyGenerator.PLATFORM_SEGACD);
		Assert.Equal(0x19, PansyGenerator.PLATFORM_VIRTUALBOY);
		Assert.Equal(0x1a, PansyGenerator.PLATFORM_AMSTRAD_CPC);
		Assert.Equal(0x1b, PansyGenerator.PLATFORM_BBC_MICRO);
		Assert.Equal(0x1c, PansyGenerator.PLATFORM_VIC20);
		Assert.Equal(0x1d, PansyGenerator.PLATFORM_PLUS4);
		Assert.Equal(0x1e, PansyGenerator.PLATFORM_C128);
		Assert.Equal(0xff, PansyGenerator.PLATFORM_CUSTOM);
	}

	[Fact]
	public void RegisterComment_GeneratesCommentsSection() {
		// Arrange
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();
		var generator = new PansyGenerator(symbolTable, TargetArchitecture.MOS6502, segments);
		generator.RegisterComment(0x8000, "Entry point", PansyGenerator.COMMENT_INLINE);
		generator.RegisterComment(0x8003, "Main loop", PansyGenerator.COMMENT_INLINE);

		// Act
		var data = generator.Generate(romSize: 0x8000, compress: false);

		// Assert - file should contain SECTION_COMMENTS (0x0003) somewhere
		bool foundCommentsSection = false;
		// Parse section table after 32-byte header
		int sectionCount = BitConverter.ToInt32(data, 24);
		int offset = 32; // after header
		for (int i = 0; i < sectionCount; i++) {
			var sectionType = BitConverter.ToUInt32(data, offset);
			if (sectionType == PansyGenerator.SECTION_COMMENTS) {
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
			Assert.NotEqual(PansyGenerator.SECTION_COMMENTS, sectionType);
			offset += 16;
		}
	}

	[Fact]
	public void CommentTypeConstants_MatchPansySpec() {
		Assert.Equal((byte)1, PansyGenerator.COMMENT_INLINE);
		Assert.Equal((byte)2, PansyGenerator.COMMENT_BLOCK);
		Assert.Equal((byte)3, PansyGenerator.COMMENT_TODO);
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
		var flags = (PansyGenerator.PansyFlags)BitConverter.ToUInt16(data, 10);
		Assert.True(flags.HasFlag(PansyGenerator.PansyFlags.HasCrossRefs));

		// Find cross-refs section
		bool foundCrossRefsSection = false;
		int sectionCount = BitConverter.ToInt32(data, 24);
		int offset = 32;
		for (int i = 0; i < sectionCount; i++) {
			var sectionType = BitConverter.ToUInt32(data, offset);
			if (sectionType == PansyGenerator.SECTION_CROSS_REFS) {
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

		// Assert - no cross-refs section
		var flags = (PansyGenerator.PansyFlags)BitConverter.ToUInt16(data, 10);
		Assert.False(flags.HasFlag(PansyGenerator.PansyFlags.HasCrossRefs));
	}

	[Fact]
	public void CrossRefTypes_MatchPansySpec() {
		Assert.Equal((byte)1, (byte)PansyGenerator.CrossRefType.Jsr);
		Assert.Equal((byte)2, (byte)PansyGenerator.CrossRefType.Jmp);
		Assert.Equal((byte)3, (byte)PansyGenerator.CrossRefType.Branch);
		Assert.Equal((byte)4, (byte)PansyGenerator.CrossRefType.Read);
		Assert.Equal((byte)5, (byte)PansyGenerator.CrossRefType.Write);
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
		var flags = (PansyGenerator.PansyFlags)BitConverter.ToUInt16(data, 10);
		Assert.True(flags.HasFlag(PansyGenerator.PansyFlags.HasCrossRefs));

		// Should have 2 cross-refs (JSR + JMP)
		Assert.Equal(2, codeGen.CrossReferences.Count);
	}
}
