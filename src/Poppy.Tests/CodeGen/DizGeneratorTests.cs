// ============================================================================
// DizGeneratorTests.cs - Unit Tests for DiztinGUIsh Project File Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text.Json;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for the DiztinGUIsh (.diz) project file generator.
/// </summary>
public sealed class DizGeneratorTests {
	private static SourceLocation DummyLocation => new("test.pasm", 1, 1, 0);

	#region JSON Output Structure

	[Fact]
	public void Generate_ReturnsValidJson() {
		var gen = CreateGenerator(TargetArchitecture.WDC65816);
		var json = gen.Generate(new byte[0x100]);

		Assert.NotNull(json);
		Assert.NotEmpty(json);

		// Should parse without error
		var doc = JsonDocument.Parse(json);
		Assert.NotNull(doc);
	}

	[Fact]
	public void Generate_HasProjectName() {
		var gen = CreateGenerator(TargetArchitecture.WDC65816, projectName: "MyProject");
		var json = gen.Generate(new byte[0x100]);
		var doc = JsonDocument.Parse(json);

		Assert.Equal("MyProject", doc.RootElement.GetProperty("ProjectName").GetString());
	}

	[Fact]
	public void Generate_HasVersion() {
		var gen = CreateGenerator(TargetArchitecture.WDC65816);
		var json = gen.Generate(new byte[0x100]);
		var doc = JsonDocument.Parse(json);

		Assert.Equal("3.0", doc.RootElement.GetProperty("Version").GetString());
	}

	[Fact]
	public void Generate_HasGenerator() {
		var gen = CreateGenerator(TargetArchitecture.WDC65816);
		var json = gen.Generate(new byte[0x100]);
		var doc = JsonDocument.Parse(json);

		Assert.Equal("Poppy Compiler", doc.RootElement.GetProperty("Generator").GetString());
	}

	[Fact]
	public void Generate_HasRomSize() {
		var romData = new byte[0x8000];
		var gen = CreateGenerator(TargetArchitecture.WDC65816);
		var json = gen.Generate(romData);
		var doc = JsonDocument.Parse(json);

		Assert.Equal(0x8000, doc.RootElement.GetProperty("RomSize").GetInt32());
	}

	[Fact]
	public void Generate_HasRomChecksum() {
		var gen = CreateGenerator(TargetArchitecture.WDC65816);
		var json = gen.Generate(new byte[0x100]);
		var doc = JsonDocument.Parse(json);

		Assert.True(doc.RootElement.TryGetProperty("RomChecksum", out _));
	}

	#endregion

	#region Map Mode

	[Fact]
	public void Generate_SnesTarget_MapModeIsLoRom() {
		var gen = CreateGenerator(TargetArchitecture.WDC65816);
		var json = gen.Generate(new byte[0x100]);
		var doc = JsonDocument.Parse(json);

		Assert.Equal("LoRom", doc.RootElement.GetProperty("RomMapMode").GetString());
	}

	[Fact]
	public void Generate_NonSnesTarget_MapModeIsUnknown() {
		var gen = CreateGenerator(TargetArchitecture.MOS6502);
		var json = gen.Generate(new byte[0x100]);
		var doc = JsonDocument.Parse(json);

		Assert.Equal("Unknown", doc.RootElement.GetProperty("RomMapMode").GetString());
	}

	[Fact]
	public void Generate_RomSpeed_IsSlowRom() {
		var gen = CreateGenerator(TargetArchitecture.WDC65816);
		var json = gen.Generate(new byte[0x100]);
		var doc = JsonDocument.Parse(json);

		Assert.Equal("SlowRom", doc.RootElement.GetProperty("RomSpeed").GetString());
	}

	#endregion

	#region Labels from Symbol Table

	[Fact]
	public void Generate_WithLabels_IncludesInOutput() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("main", SymbolType.Label, 0x8000, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.WDC65816, symbolTable: symbolTable);
		var json = gen.Generate(new byte[0x100]);
		var doc = JsonDocument.Parse(json);

		var labels = doc.RootElement.GetProperty("Labels");
		Assert.True(labels.TryGetProperty("32768", out var labelData)); // 0x8000 = 32768
		Assert.Equal("main", labelData.GetProperty("Name").GetString());
	}

	[Fact]
	public void Generate_LabelComment_IsLabel() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("my_label", SymbolType.Label, 0x100, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.WDC65816, symbolTable: symbolTable);
		var json = gen.Generate(new byte[0x200]);
		var doc = JsonDocument.Parse(json);

		var labels = doc.RootElement.GetProperty("Labels");
		var labelData = labels.GetProperty("256"); // 0x100
		Assert.Equal("Label", labelData.GetProperty("Comment").GetString());
	}

	[Fact]
	public void Generate_ConstantComment_IsConstant() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("my_const", SymbolType.Constant, 0x42, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.WDC65816, symbolTable: symbolTable);
		var json = gen.Generate(new byte[0x200]);
		var doc = JsonDocument.Parse(json);

		var labels = doc.RootElement.GetProperty("Labels");
		var labelData = labels.GetProperty("66"); // 0x42
		Assert.Equal("Constant", labelData.GetProperty("Comment").GetString());
	}

	[Fact]
	public void Generate_MacroSymbol_Excluded() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("my_macro", SymbolType.Macro, 0x100, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.WDC65816, symbolTable: symbolTable);
		var json = gen.Generate(new byte[0x200]);
		var doc = JsonDocument.Parse(json);

		var labels = doc.RootElement.GetProperty("Labels");
		// Macros should be skipped
		Assert.Equal(JsonValueKind.Object, labels.ValueKind);
		Assert.False(labels.TryGetProperty("256", out _));
	}

	[Fact]
	public void Generate_LabelDataType_IsOpcode() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("code_label", SymbolType.Label, 0x100, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.WDC65816, symbolTable: symbolTable);
		var json = gen.Generate(new byte[0x200]);
		var doc = JsonDocument.Parse(json);

		var labels = doc.RootElement.GetProperty("Labels");
		var labelData = labels.GetProperty("256");
		Assert.Equal((int)DizGenerator.DizDataType.Opcode, labelData.GetProperty("DataType").GetInt32());
	}

	[Fact]
	public void Generate_ConstantDataType_IsData8() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("my_const", SymbolType.Constant, 0x42, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.WDC65816, symbolTable: symbolTable);
		var json = gen.Generate(new byte[0x200]);
		var doc = JsonDocument.Parse(json);

		var labels = doc.RootElement.GetProperty("Labels");
		var labelData = labels.GetProperty("66");
		Assert.Equal((int)DizGenerator.DizDataType.Data8, labelData.GetProperty("DataType").GetInt32());
	}

	#endregion

	#region Data Type Array

	[Fact]
	public void Generate_EmptySegments_AllUnreached() {
		var gen = CreateGenerator(TargetArchitecture.WDC65816);
		var romData = new byte[0x10];
		var json = gen.Generate(romData);
		var doc = JsonDocument.Parse(json);

		var dataTypes = doc.RootElement.GetProperty("DataTypes");
		Assert.Equal(0x10, dataTypes.GetArrayLength());

		// All should be Unreached (0)
		foreach (var element in dataTypes.EnumerateArray()) {
			Assert.Equal(0, element.GetInt32());
		}
	}

	[Fact]
	public void Generate_WithSegment_MarksAsOpcode() {
		var segment = new OutputSegment(0x008000);
		segment.Data.AddRange(new byte[] { 0xa9, 0xff, 0x60 });

		var gen = CreateGenerator(TargetArchitecture.WDC65816, segments: [segment]);
		var romData = new byte[0x10000];
		var json = gen.Generate(romData);
		var doc = JsonDocument.Parse(json);

		var dataTypes = doc.RootElement.GetProperty("DataTypes");
		// SNES LoROM: bank 0, offset $8000 -> ROM offset (0 & 0x7f) * 0x8000 + (0x8000 - 0x8000) = 0
		Assert.Equal((int)DizGenerator.DizDataType.Opcode, dataTypes[0].GetInt32());
		Assert.Equal((int)DizGenerator.DizDataType.Opcode, dataTypes[1].GetInt32());
		Assert.Equal((int)DizGenerator.DizDataType.Opcode, dataTypes[2].GetInt32());
	}

	#endregion

	#region Checksum

	[Fact]
	public void Generate_AllZeroRom_ChecksumIsZero() {
		var gen = CreateGenerator(TargetArchitecture.WDC65816);
		var json = gen.Generate(new byte[0x100]);
		var doc = JsonDocument.Parse(json);

		Assert.Equal(0, doc.RootElement.GetProperty("RomChecksum").GetInt32());
	}

	[Fact]
	public void Generate_NonZeroRom_ChecksumNonZero() {
		var gen = CreateGenerator(TargetArchitecture.WDC65816);
		var romData = new byte[0x10];
		romData[0] = 0xff;
		romData[1] = 0x01;
		var json = gen.Generate(romData);
		var doc = JsonDocument.Parse(json);

		// Checksum = (0xff + 0x01) & 0xffff = 0x0100
		Assert.Equal(0x0100, doc.RootElement.GetProperty("RomChecksum").GetInt32());
	}

	#endregion

	#region DizDataType Enum Values

	[Fact]
	public void DizDataType_HasExpectedValues() {
		Assert.Equal(0, (int)DizGenerator.DizDataType.Unreached);
		Assert.Equal(1, (int)DizGenerator.DizDataType.Opcode);
		Assert.Equal(2, (int)DizGenerator.DizDataType.Operand);
		Assert.Equal(3, (int)DizGenerator.DizDataType.Data8);
		Assert.Equal(4, (int)DizGenerator.DizDataType.Graphics);
		Assert.Equal(5, (int)DizGenerator.DizDataType.Music);
		Assert.Equal(6, (int)DizGenerator.DizDataType.Empty);
		Assert.Equal(7, (int)DizGenerator.DizDataType.Data16);
		Assert.Equal(8, (int)DizGenerator.DizDataType.Pointer16);
		Assert.Equal(9, (int)DizGenerator.DizDataType.Data24);
		Assert.Equal(10, (int)DizGenerator.DizDataType.Pointer24);
		Assert.Equal(11, (int)DizGenerator.DizDataType.Data32);
		Assert.Equal(12, (int)DizGenerator.DizDataType.Pointer32);
		Assert.Equal(13, (int)DizGenerator.DizDataType.Text);
	}

	#endregion

	#region Export (File I/O)

	[Fact]
	public void Export_Uncompressed_WritesJson() {
		var gen = CreateGenerator(TargetArchitecture.WDC65816);
		var path = Path.Combine(Path.GetTempPath(), $"poppy-test-diz-{Guid.NewGuid()}.diz");

		try {
			gen.Export(path, new byte[0x100], compress: false);

			Assert.True(File.Exists(path));
			var content = File.ReadAllText(path);
			var doc = JsonDocument.Parse(content);
			Assert.Equal("TestProject", doc.RootElement.GetProperty("ProjectName").GetString());
		} finally {
			if (File.Exists(path)) File.Delete(path);
		}
	}

	[Fact]
	public void Export_Compressed_WritesGzip() {
		var gen = CreateGenerator(TargetArchitecture.WDC65816);
		var path = Path.Combine(Path.GetTempPath(), $"poppy-test-diz-{Guid.NewGuid()}.diz");

		try {
			gen.Export(path, new byte[0x100], compress: true);

			Assert.True(File.Exists(path));
			var bytes = File.ReadAllBytes(path);

			// GZip magic: 0x1f 0x8b
			Assert.Equal(0x1f, bytes[0]);
			Assert.Equal(0x8b, bytes[1]);
		} finally {
			if (File.Exists(path)) File.Delete(path);
		}
	}

	#endregion

	#region Helpers

	private static DizGenerator CreateGenerator(
		TargetArchitecture target,
		List<OutputSegment>? segments = null,
		SymbolTable? symbolTable = null,
		string projectName = "TestProject",
		ListingGenerator? listing = null) {
		return new DizGenerator(
			symbolTable ?? new SymbolTable(),
			target,
			segments ?? [],
			projectName,
			listing);
	}

	#endregion
}
