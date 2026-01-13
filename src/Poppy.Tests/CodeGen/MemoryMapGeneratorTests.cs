// Poppy Compiler - Memory Map Generator Unit Tests
// Copyright © 2026

using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Semantics;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for the MemoryMapGenerator class.
/// </summary>
public sealed class MemoryMapGeneratorTests {
	[Fact]
	public void Generate_EmptySegments_ReturnsValidMap() {
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.MOS6502);
		var result = generator.Generate();

		Assert.Contains("; === Segments ===", result);
		Assert.Contains("; (no segments)", result);
		Assert.Contains("; === Labels ===", result);
		Assert.Contains("; (no labels)", result);
		Assert.Contains("; === Statistics ===", result);
		Assert.Contains("Total bytes:      0", result);
	}

	[Fact]
	public void Generate_WithSegments_ListsSegmentInfo() {
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment> {
			CreateSegment(0x8000, 100),
			CreateSegment(0x9000, 50)
		};

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.MOS6502);
		var result = generator.Generate();

		Assert.Contains("$8000", result);
		Assert.Contains("$8063", result); // end address: $8000 + 99 = $8063
		Assert.Contains("$9000", result);
		Assert.Contains("$9031", result); // end address: $9000 + 49 = $9031
		Assert.Contains("100", result);
		Assert.Contains("50", result);
	}

	[Fact]
	public void Generate_WithLabels_ListsLabelInfo() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("reset", SymbolType.Label, 0x8000, new SourceLocation("test.pasm", 1, 1, 0));
		symbolTable.Define("nmi", SymbolType.Label, 0x8010, new SourceLocation("test.pasm", 2, 1, 0));
		symbolTable.Define("irq", SymbolType.Label, 0x8020, new SourceLocation("test.pasm", 3, 1, 0));
		var segments = new List<OutputSegment>();

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.MOS6502);
		var result = generator.Generate();

		Assert.Contains("$8000  reset", result);
		Assert.Contains("$8010  nmi", result);
		Assert.Contains("$8020  irq", result);
	}

	[Fact]
	public void Generate_LabelsAreSortedByAddress() {
		var symbolTable = new SymbolTable();
		// Define labels out of order
		symbolTable.Define("irq", SymbolType.Label, 0x8020, new SourceLocation("test.pasm", 1, 1, 0));
		symbolTable.Define("reset", SymbolType.Label, 0x8000, new SourceLocation("test.pasm", 2, 1, 0));
		symbolTable.Define("nmi", SymbolType.Label, 0x8010, new SourceLocation("test.pasm", 3, 1, 0));
		var segments = new List<OutputSegment>();

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.MOS6502);
		var result = generator.Generate();

		// Check order by finding indices
		var resetIndex = result.IndexOf("$8000  reset");
		var nmiIndex = result.IndexOf("$8010  nmi");
		var irqIndex = result.IndexOf("$8020  irq");

		Assert.True(resetIndex < nmiIndex, "reset should appear before nmi");
		Assert.True(nmiIndex < irqIndex, "nmi should appear before irq");
	}

	[Fact]
	public void Generate_SkipsConstants() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("reset", SymbolType.Label, 0x8000, new SourceLocation("test.pasm", 1, 1, 0));
		symbolTable.Define("PPUCTRL", SymbolType.Constant, 0x2000, new SourceLocation("test.pasm", 2, 1, 0));
		var segments = new List<OutputSegment>();

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.MOS6502);
		var result = generator.Generate();

		// Check in the Labels section specifically
		var labelsStart = result.IndexOf("; === Labels ===");
		var statsStart = result.IndexOf("; === Statistics ===");
		var labelsSection = result[labelsStart..statsStart];

		Assert.Contains("reset", labelsSection);
		Assert.DoesNotContain("PPUCTRL", labelsSection);
	}

	[Fact]
	public void Generate_ShowsStatistics() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("reset", SymbolType.Label, 0x8000, new SourceLocation("test.pasm", 1, 1, 0));
		symbolTable.Define("nmi", SymbolType.Label, 0x8010, new SourceLocation("test.pasm", 2, 1, 0));
		symbolTable.Define("PPUCTRL", SymbolType.Constant, 0x2000, new SourceLocation("test.pasm", 3, 1, 0));
		var segments = new List<OutputSegment> {
			CreateSegment(0x8000, 100)
		};

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.MOS6502);
		var result = generator.Generate();

		Assert.Contains("Total bytes:      100", result);
		Assert.Contains("Segments:         1", result);
		Assert.Contains("Labels:           2", result);
		Assert.Contains("Constants:        1", result);
	}

	[Fact]
	public void Generate_NES_ShowsMemoryRegions() {
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment> {
			CreateSegment(0x8000, 1000)
		};

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.MOS6502);
		var result = generator.Generate();

		Assert.Contains("; === Memory Regions ===", result);
		Assert.Contains("Zero Page", result);
		Assert.Contains("Stack", result);
		Assert.Contains("RAM", result);
		Assert.Contains("PRG-ROM", result);
	}

	[Fact]
	public void Generate_SNES_ShowsMemoryRegions() {
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.WDC65816);
		var result = generator.Generate();

		Assert.Contains("Direct Page", result);
		Assert.Contains("Low RAM", result);
	}

	[Fact]
	public void Generate_GameBoy_ShowsMemoryRegions() {
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.SM83);
		var result = generator.Generate();

		Assert.Contains("ROM Bank 0", result);
		Assert.Contains("ROM Bank N", result);
		Assert.Contains("VRAM", result);
		Assert.Contains("Work RAM", result);
	}

	[Fact]
	public void Generate_RegionUsageCalculation() {
		var symbolTable = new SymbolTable();
		// Create a segment in PRG-ROM region ($8000-$ffff)
		var segments = new List<OutputSegment> {
			CreateSegment(0x8000, 1000)
		};

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.MOS6502);
		var result = generator.Generate();

		// PRG-ROM region should show 1000 bytes used
		Assert.Contains("PRG-ROM", result);
		Assert.Contains("1000", result);
	}

	[Fact]
	public void Export_WritesToFile() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("test", SymbolType.Label, 0x8000, new SourceLocation("test.pasm", 1, 1, 0));
		var segments = new List<OutputSegment> {
			CreateSegment(0x8000, 50)
		};

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.MOS6502);
		var tempFile = Path.GetTempFileName();
		try {
			generator.Export(tempFile);
			var content = File.ReadAllText(tempFile);

			Assert.Contains("; Poppy Assembler Memory Map", content);
			Assert.Contains("$8000  test", content);
		}
		finally {
			if (File.Exists(tempFile)) File.Delete(tempFile);
		}
	}

	[Fact]
	public void Generate_ShowsAddressRange() {
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment> {
			CreateSegment(0x8000, 100),
			CreateSegment(0xc000, 200)
		};

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.MOS6502);
		var result = generator.Generate();

		// Should show address range from lowest to highest
		Assert.Contains("$8000 - $c0c7", result);
	}

	[Fact]
	public void Generate_MultipleSegmentsInSameRegion() {
		var symbolTable = new SymbolTable();
		// Multiple segments in PRG-ROM region
		var segments = new List<OutputSegment> {
			CreateSegment(0x8000, 100),
			CreateSegment(0x9000, 200),
			CreateSegment(0xa000, 300)
		};

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.MOS6502);
		var result = generator.Generate();

		// Total used in PRG-ROM should be 600 bytes
		var statsStart = result.IndexOf("; === Statistics ===");
		var statsSection = result[statsStart..];

		Assert.Contains("Total bytes:      600", statsSection);
	}

	[Fact]
	public void Generate_IncludesTargetArchitecture() {
		var symbolTable = new SymbolTable();
		var segments = new List<OutputSegment>();

		var generator = new MemoryMapGenerator(segments, symbolTable, TargetArchitecture.MOS6502);
		var result = generator.Generate();

		Assert.Contains("Target: MOS6502", result);
	}

	/// <summary>
	/// Creates a test segment with the specified start address and size.
	/// </summary>
	private static OutputSegment CreateSegment(long startAddress, int size) {
		var segment = new OutputSegment(startAddress);
		for (int i = 0; i < size; i++) {
			segment.Data.Add((byte)(i & 0xff));
		}
		return segment;
	}
}

