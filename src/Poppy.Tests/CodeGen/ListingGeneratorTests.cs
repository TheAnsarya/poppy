using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for the ListingGenerator class.
/// </summary>
public class ListingGeneratorTests {
	[Fact]
	public void Generate_EmptyListing_HasHeaderAndFooter() {
		// arrange
		var generator = new ListingGenerator();

		// act
		var listing = generator.Generate();

		// assert
		Assert.Contains("; Poppy Assembler Listing", listing);
		Assert.Contains("; === End of Listing ===", listing);
		Assert.Contains("; Addr    Bytes", listing);
	}

	[Fact]
	public void AddEntry_SingleInstruction_ShowsAddressAndBytes() {
		// arrange
		var generator = new ListingGenerator();
		generator.RegisterSource("test.pasm", "lda #$42");
		var location = new SourceLocation("test.pasm", 1, 1, 0);

		// act
		generator.AddEntry(0x8000, [0xa9, 0x42], location);
		var listing = generator.Generate();

		// assert
		Assert.Contains("$8000", listing);
		Assert.Contains("a9 42", listing);
		Assert.Contains("lda #$42", listing);
	}

	[Fact]
	public void AddEntry_MultipleInstructions_ShowsAll() {
		// arrange
		var generator = new ListingGenerator();
		generator.RegisterSource("test.pasm", "lda #$42\nsta $2000\nnop");

		// act
		generator.AddEntry(0x8000, [0xa9, 0x42], new SourceLocation("test.pasm", 1, 1, 0));
		generator.AddEntry(0x8002, [0x8d, 0x00, 0x20], new SourceLocation("test.pasm", 2, 1, 0));
		generator.AddEntry(0x8005, [0xea], new SourceLocation("test.pasm", 3, 1, 0));
		var listing = generator.Generate();

		// assert
		Assert.Contains("$8000", listing);
		Assert.Contains("a9 42", listing);
		Assert.Contains("$8002", listing);
		Assert.Contains("8d 00 20", listing);
		Assert.Contains("$8005", listing);
		Assert.Contains("ea", listing);
	}

	[Fact]
	public void AddLabelEntry_NoBytes_ShowsLabelLine() {
		// arrange
		var generator = new ListingGenerator();
		generator.RegisterSource("test.pasm", "reset:");
		var location = new SourceLocation("test.pasm", 1, 1, 0);

		// act
		generator.AddLabelEntry(0x8000, location);
		var listing = generator.Generate();

		// assert
		Assert.Contains("reset:", listing);
	}

	[Fact]
	public void AddOrgEntry_ShowsAddress() {
		// arrange
		var generator = new ListingGenerator();
		generator.RegisterSource("test.pasm", ".org $c000");
		var location = new SourceLocation("test.pasm", 1, 1, 0);

		// act
		generator.AddOrgEntry(0xc000, location);
		var listing = generator.Generate();

		// assert
		Assert.Contains("$c000", listing);
		Assert.Contains(".org $c000", listing);
	}

	[Fact]
	public void Generate_WithSymbolTable_ShowsSymbols() {
		// arrange
		var generator = new ListingGenerator();
		var symbolTable = new SymbolTable();
		symbolTable.Define("reset", SymbolType.Label, 0x8000, new SourceLocation("test.pasm", 1, 1, 0));
		symbolTable.Define("PPUCTRL", SymbolType.Constant, 0x2000, new SourceLocation("test.pasm", 2, 1, 0));

		// act
		var listing = generator.Generate(symbolTable);

		// assert
		Assert.Contains("; === Symbol Table ===", listing);
		Assert.Contains("reset", listing);
		Assert.Contains("PPUCTRL", listing);
		Assert.Contains("$8000", listing);
		Assert.Contains("$2000", listing);
		Assert.Contains("Label", listing);
		Assert.Contains("Const", listing);
	}

	[Fact]
	public void Generate_LongByteSequence_Truncates() {
		// arrange
		var generator = new ListingGenerator();
		generator.RegisterSource("test.pasm", ".byte $01,$02,$03,$04,$05,$06,$07,$08,$09,$0a");
		var location = new SourceLocation("test.pasm", 1, 1, 0);
		var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a };

		// act
		generator.AddEntry(0x8000, bytes, location);
		var listing = generator.Generate();

		// assert
		Assert.Contains("...", listing); // Truncated
		Assert.Contains("01 02 03 04 05 06 07 08", listing);
	}

	[Fact]
	public void Generate_MultipleFiles_GroupsByFile() {
		// arrange
		var generator = new ListingGenerator();
		generator.RegisterSource("main.pasm", "lda #$00");
		generator.RegisterSource("include.pasm", "sta $2000");

		// act
		generator.AddEntry(0x8000, [0xa9, 0x00], new SourceLocation("main.pasm", 1, 1, 0));
		generator.AddEntry(0x8002, [0x8d, 0x00, 0x20], new SourceLocation("include.pasm", 1, 1, 0));
		var listing = generator.Generate();

		// assert
		Assert.Contains("=== File: main.pasm ===", listing);
		Assert.Contains("=== File: include.pasm ===", listing);
	}

	[Fact]
	public void Generate_UnregisteredSource_StillShowsAddressAndBytes() {
		// arrange
		var generator = new ListingGenerator();
		// Note: not registering source
		var location = new SourceLocation("unknown.pasm", 1, 1, 0);

		// act
		generator.AddEntry(0x8000, [0xea], location);
		var listing = generator.Generate();

		// assert
		Assert.Contains("$8000", listing);
		Assert.Contains("ea", listing);
	}

	[Fact]
	public void Generate_TimestampIncluded() {
		// arrange
		var generator = new ListingGenerator();

		// act
		var listing = generator.Generate();

		// assert
		Assert.Contains("; Generated:", listing);
	}

	[Fact]
	public void WriteToFile_CreatesFile() {
		// arrange
		var generator = new ListingGenerator();
		generator.RegisterSource("test.pasm", "nop");
		generator.AddEntry(0x8000, [0xea], new SourceLocation("test.pasm", 1, 1, 0));

		var tempFile = Path.Combine(Path.GetTempPath(), $"test_listing_{Guid.NewGuid():N}.lst");

		try {
			// act
			generator.WriteToFile(tempFile);

			// assert
			Assert.True(File.Exists(tempFile));
			var content = File.ReadAllText(tempFile);
			Assert.Contains("$8000", content);
			Assert.Contains("nop", content);
		} finally {
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}

	[Fact]
	public void Generate_SymbolTypes_ShowCorrectly() {
		// arrange
		var generator = new ListingGenerator();
		var symbolTable = new SymbolTable();
		symbolTable.Define("global_label", SymbolType.Label, 0x8000, new SourceLocation("test.pasm", 1, 1, 0));
		symbolTable.Define("CONSTANT_VAL", SymbolType.Constant, 0x42, new SourceLocation("test.pasm", 2, 1, 0));

		// act
		var listing = generator.Generate(symbolTable);

		// assert
		Assert.Contains("global_label", listing);
		Assert.Contains("CONSTANT_VAL", listing);
		Assert.Contains("$0042", listing);
	}
}
