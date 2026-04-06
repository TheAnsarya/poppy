// ============================================================================
// CdlGeneratorTests.cs - Unit Tests for CDL (Code/Data Log) Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for the CDL (Code/Data Log) file generator.
/// </summary>
public sealed class CdlGeneratorTests {
	private static SourceLocation DummyLocation => new("test.pasm", 1, 1, 0);

	#region Mesen Format

	[Fact]
	public void Generate_MesenFormat_HasHeader() {
		var gen = CreateGenerator(TargetArchitecture.MOS6502);
		var result = gen.Generate(0x100, CdlGenerator.CdlFormat.Mesen);

		// "CDL\x01" header + 0x100 bytes of data
		Assert.Equal(4 + 0x100, result.Length);
		Assert.Equal((byte)'C', result[0]);
		Assert.Equal((byte)'D', result[1]);
		Assert.Equal((byte)'L', result[2]);
		Assert.Equal(0x01, result[3]);
	}

	[Fact]
	public void Generate_MesenFormat_EmptySegments_AllZero() {
		var gen = CreateGenerator(TargetArchitecture.MOS6502);
		var result = gen.Generate(0x100, CdlGenerator.CdlFormat.Mesen);

		// All CDL data should be zero (no segments marked)
		for (int i = 4; i < result.Length; i++) {
			Assert.Equal(0, result[i]);
		}
	}

	#endregion

	#region FCEUX Format

	[Fact]
	public void Generate_FceuxFormat_NoHeader() {
		var gen = CreateGenerator(TargetArchitecture.MOS6502);
		var result = gen.Generate(0x100, CdlGenerator.CdlFormat.FCEUX);

		// Raw bytes, no header
		Assert.Equal(0x100, result.Length);
	}

	[Fact]
	public void Generate_FceuxFormat_EmptySegments_AllZero() {
		var gen = CreateGenerator(TargetArchitecture.MOS6502);
		var result = gen.Generate(0x100, CdlGenerator.CdlFormat.FCEUX);

		for (int i = 0; i < result.Length; i++) {
			Assert.Equal(0, result[i]);
		}
	}

	#endregion

	#region Segment Marking

	[Fact]
	public void Generate_MesenFormat_MarksSegmentsAsCode() {
		var segment = new OutputSegment(0x0010);
		segment.Data.AddRange(new byte[] { 0xa9, 0xff, 0x60 }); // lda #$ff; rts

		var gen = CreateGenerator(TargetArchitecture.MOS6502, [segment]);
		var result = gen.Generate(0x100, CdlGenerator.CdlFormat.Mesen);

		// Offset 0x10-0x12 should be marked as code (MESEN_CODE = 0x01)
		Assert.Equal(0x01, result[4 + 0x10]);
		Assert.Equal(0x01, result[4 + 0x11]);
		Assert.Equal(0x01, result[4 + 0x12]);

		// Adjacent bytes should remain 0
		Assert.Equal(0, result[4 + 0x0f]);
		Assert.Equal(0, result[4 + 0x13]);
	}

	[Fact]
	public void Generate_FceuxFormat_MarksSegmentsAsCode() {
		var segment = new OutputSegment(0x0010);
		segment.Data.AddRange(new byte[] { 0xa9, 0xff });

		var gen = CreateGenerator(TargetArchitecture.MOS6502, [segment]);
		var result = gen.Generate(0x100, CdlGenerator.CdlFormat.FCEUX);

		// FCEUX_CODE = 0x01
		Assert.Equal(0x01, result[0x10]);
		Assert.Equal(0x01, result[0x11]);
	}

	[Fact]
	public void Generate_SegmentBeyondRomSize_Ignored() {
		var segment = new OutputSegment(0x200);
		segment.Data.AddRange(new byte[] { 0x01, 0x02 });

		var gen = CreateGenerator(TargetArchitecture.MOS6502, [segment]);
		// ROM size is smaller than segment address, so no marking should occur
		var result = gen.Generate(0x100, CdlGenerator.CdlFormat.FCEUX);

		for (int i = 0; i < result.Length; i++) {
			Assert.Equal(0, result[i]);
		}
	}

	[Fact]
	public void Generate_MultipleSegments_AllMarked() {
		var seg1 = new OutputSegment(0x10);
		seg1.Data.AddRange(new byte[] { 0xa9, 0xff });
		var seg2 = new OutputSegment(0x20);
		seg2.Data.AddRange(new byte[] { 0x60 });

		var gen = CreateGenerator(TargetArchitecture.MOS6502, [seg1, seg2]);
		var result = gen.Generate(0x100, CdlGenerator.CdlFormat.FCEUX);

		Assert.Equal(0x01, result[0x10]);
		Assert.Equal(0x01, result[0x11]);
		Assert.Equal(0x01, result[0x20]);
	}

	#endregion

	#region Symbol Marking - NES (MOS6502)

	[Fact]
	public void Generate_MesenFormat_LabelSymbol_MarksJumpTarget_Nes() {
		var symbolTable = new SymbolTable();
		// NES: CPU $8000 -> ROM offset $8000 - $8000 + 0x10 = 0x10
		symbolTable.Define("main", SymbolType.Label, 0x8000, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.MOS6502, [], symbolTable);
		var result = gen.Generate(0x8010, CdlGenerator.CdlFormat.Mesen);

		// ROM offset = $8000 - $8000 + 0x10 = 0x10
		var flags = result[4 + 0x10];
		Assert.True((flags & 0x04) != 0, "MESEN_JUMP_TARGET should be set");
	}

	[Fact]
	public void Generate_MesenFormat_SubroutinePrefix_MarksSubEntry_Nes() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("sub_init", SymbolType.Label, 0x8000, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.MOS6502, [], symbolTable);
		var result = gen.Generate(0x8010, CdlGenerator.CdlFormat.Mesen);

		var flags = result[4 + 0x10];
		Assert.True((flags & 0x08) != 0, "MESEN_SUB_ENTRY_POINT should be set for sub_ prefix");
		Assert.True((flags & 0x04) != 0, "MESEN_JUMP_TARGET should also be set");
	}

	[Fact]
	public void Generate_MesenFormat_FnPrefix_MarksSubEntry() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("fn_update", SymbolType.Label, 0x8000, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.MOS6502, [], symbolTable);
		var result = gen.Generate(0x8010, CdlGenerator.CdlFormat.Mesen);

		var flags = result[4 + 0x10];
		Assert.True((flags & 0x08) != 0, "MESEN_SUB_ENTRY_POINT for fn_ prefix");
	}

	[Fact]
	public void Generate_MesenFormat_FuncPrefix_MarksSubEntry() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("func_draw", SymbolType.Label, 0x8000, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.MOS6502, [], symbolTable);
		var result = gen.Generate(0x8010, CdlGenerator.CdlFormat.Mesen);

		var flags = result[4 + 0x10];
		Assert.True((flags & 0x08) != 0, "MESEN_SUB_ENTRY_POINT for func_ prefix");
	}

	[Fact]
	public void Generate_FceuxFormat_SubroutinePrefix_MarksIndirectCode() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("sub_init", SymbolType.Label, 0x8000, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.MOS6502, [], symbolTable);
		var result = gen.Generate(0x8010, CdlGenerator.CdlFormat.FCEUX);

		var flags = result[0x10];
		Assert.True((flags & 0x10) != 0, "FCEUX_INDIRECT_CODE for sub_ prefix");
	}

	[Fact]
	public void Generate_SymbolBelowPrgRom_Ignored_Nes() {
		var symbolTable = new SymbolTable();
		// CPU address $0000 is RAM on NES, not PRG ROM -> CpuToRomAddress returns -1
		symbolTable.Define("zp_var", SymbolType.Label, 0x0000, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.MOS6502, [], symbolTable);
		var result = gen.Generate(0x100, CdlGenerator.CdlFormat.Mesen);

		// Nothing should be marked (all zero except header)
		for (int i = 4; i < result.Length; i++) {
			Assert.Equal(0, result[i]);
		}
	}

	#endregion

	#region CpuToRomAddress - SNES

	[Fact]
	public void Generate_SnesLoRom_MapsCorrectly() {
		var segment = new OutputSegment(0x008000);
		segment.Data.AddRange(new byte[] { 0xa9, 0xff });

		var gen = CreateGenerator(TargetArchitecture.WDC65816, [segment]);
		// For LoROM: bank 0, offset $8000 -> ROM offset (0 & 0x7f) * 0x8000 + ($8000 - $8000) = 0
		// Segment direct address is 0x8000 (within 16-bit space for segment marking)
		// But CpuToRomAddress handles bank mapping for symbols
		var result = gen.Generate(0x10000, CdlGenerator.CdlFormat.FCEUX);

		Assert.Equal(0x01, result[0x8000]);
	}

	#endregion

	#region CpuToRomAddress - Game Boy

	[Fact]
	public void Generate_GameBoy_DirectMapping() {
		var symbolTable = new SymbolTable();
		symbolTable.Define("start", SymbolType.Label, 0x0100, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.SM83, [], symbolTable);
		var result = gen.Generate(0x8000, CdlGenerator.CdlFormat.Mesen);

		// GB: direct mapping -> ROM offset 0x0100
		var flags = result[4 + 0x0100];
		Assert.True((flags & 0x04) != 0, "MESEN_JUMP_TARGET");
	}

	[Fact]
	public void Generate_GameBoy_AboveRom_Ignored() {
		var symbolTable = new SymbolTable();
		// $8000+ is VRAM on Game Boy, not ROM
		symbolTable.Define("vram_tile", SymbolType.Label, 0x8000, DummyLocation);

		var gen = CreateGenerator(TargetArchitecture.SM83, [], symbolTable);
		var result = gen.Generate(0x8000, CdlGenerator.CdlFormat.Mesen);

		// Should not be marked (CpuToRomAddress returns -1 for $8000+)
		for (int i = 4; i < result.Length; i++) {
			Assert.Equal(0, result[i]);
		}
	}

	#endregion

	#region JSR/JMP Target Registration

	[Fact]
	public void RegisterSubroutineEntry_MesenFormat_MarksSubEntryPoint() {
		var gen = CreateGenerator(TargetArchitecture.MOS6502);
		gen.RegisterSubroutineEntry(0x8010);

		var result = gen.Generate(0x8020, CdlGenerator.CdlFormat.Mesen);

		// NES: $8010 -> ROM offset 0x10 + header = 0x20
		var flags = result[4 + 0x20];
		Assert.True((flags & 0x08) != 0, "MESEN_SUB_ENTRY_POINT from registered JSR target");
	}

	[Fact]
	public void RegisterSubroutineEntry_FceuxFormat_MarksIndirectCode() {
		var gen = CreateGenerator(TargetArchitecture.MOS6502);
		gen.RegisterSubroutineEntry(0x8010);

		var result = gen.Generate(0x8020, CdlGenerator.CdlFormat.FCEUX);

		var flags = result[0x20];
		Assert.True((flags & 0x10) != 0, "FCEUX_INDIRECT_CODE from registered JSR target");
	}

	[Fact]
	public void RegisterJumpTarget_MesenFormat_MarksJumpTarget() {
		var gen = CreateGenerator(TargetArchitecture.MOS6502);
		gen.RegisterJumpTarget(0x8010);

		var result = gen.Generate(0x8020, CdlGenerator.CdlFormat.Mesen);

		var flags = result[4 + 0x20];
		Assert.True((flags & 0x04) != 0, "MESEN_JUMP_TARGET from registered JMP target");
	}

	[Fact]
	public void SubroutineEntryCount_TracksRegistrations() {
		var gen = CreateGenerator(TargetArchitecture.MOS6502);
		Assert.Equal(0, gen.SubroutineEntryCount);

		gen.RegisterSubroutineEntry(0x8000);
		Assert.Equal(1, gen.SubroutineEntryCount);

		gen.RegisterSubroutineEntry(0x8010);
		Assert.Equal(2, gen.SubroutineEntryCount);

		// Duplicate should not increment (HashSet)
		gen.RegisterSubroutineEntry(0x8000);
		Assert.Equal(2, gen.SubroutineEntryCount);
	}

	[Fact]
	public void JumpTargetCount_TracksRegistrations() {
		var gen = CreateGenerator(TargetArchitecture.MOS6502);
		Assert.Equal(0, gen.JumpTargetCount);

		gen.RegisterJumpTarget(0x8000);
		Assert.Equal(1, gen.JumpTargetCount);

		gen.RegisterJumpTarget(0x8000);
		Assert.Equal(1, gen.JumpTargetCount); // Duplicate
	}

	#endregion

	#region CopyTargetsFrom

	[Fact]
	public void CopyTargetsFrom_CopiesJsrAndJmpTargets() {
		var source = CreateGenerator(TargetArchitecture.MOS6502);
		source.RegisterSubroutineEntry(0x8000);
		source.RegisterSubroutineEntry(0x8010);
		source.RegisterJumpTarget(0x8020);

		var dest = CreateGenerator(TargetArchitecture.MOS6502);
		dest.CopyTargetsFrom(source);

		Assert.Equal(2, dest.SubroutineEntryCount);
		Assert.Equal(1, dest.JumpTargetCount);
	}

	[Fact]
	public void CopyTargetsFrom_MergesWithExisting() {
		var source = CreateGenerator(TargetArchitecture.MOS6502);
		source.RegisterSubroutineEntry(0x8000);

		var dest = CreateGenerator(TargetArchitecture.MOS6502);
		dest.RegisterSubroutineEntry(0x8010);
		dest.CopyTargetsFrom(source);

		Assert.Equal(2, dest.SubroutineEntryCount);
	}

	#endregion

	#region Format Detection

	[Fact]
	public void Generate_DefaultFormat_IsMesen() {
		var gen = CreateGenerator(TargetArchitecture.MOS6502);

		// Default format parameter is Mesen
		var result = gen.Generate(0x100);

		// Should have "CDL\x01" header
		Assert.Equal(4 + 0x100, result.Length);
		Assert.Equal((byte)'C', result[0]);
	}

	#endregion

	#region Listing-Based Refinement

	[Fact]
	public void Generate_WithListing_MarksOpcodeBytes() {
		var listing = new ListingGenerator();
		listing.RegisterSource("test.pasm", "lda #$ff\nrts");

		// Simulate listing entries for NES PRG ROM
		// LDA #$ff at $8000 (2 bytes: $a9 $ff)
		listing.AddEntry(0x8000, [0xa9, 0xff], new SourceLocation("test.pasm", 1, 1, 0));
		// RTS at $8002 (1 byte: $60)
		listing.AddEntry(0x8002, [0x60], new SourceLocation("test.pasm", 2, 1, 5));

		var gen = CreateGenerator(TargetArchitecture.MOS6502, [], listing: listing);
		var result = gen.Generate(0x8010, CdlGenerator.CdlFormat.Mesen);

		// ROM offset for $8000 = $8000 - $8000 + 0x10 = 0x10
		Assert.Equal(0x01, result[4 + 0x10]); // opcode byte: CODE
		Assert.Equal(0x01, result[4 + 0x11]); // operand byte: CODE
		Assert.Equal(0x01, result[4 + 0x12]); // RTS: CODE
	}

	#endregion

	#region Helpers

	private static CdlGenerator CreateGenerator(
		TargetArchitecture target,
		List<OutputSegment>? segments = null,
		SymbolTable? symbolTable = null,
		ListingGenerator? listing = null) {
		return new CdlGenerator(
			symbolTable ?? new SymbolTable(),
			target,
			segments ?? [],
			listing);
	}

	#endregion
}
