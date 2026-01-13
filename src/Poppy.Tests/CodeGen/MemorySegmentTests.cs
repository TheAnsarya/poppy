// ============================================================================
// MemorySegmentTests.cs - Unit Tests for Memory Segment System
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Semantics;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for the memory segment management system.
/// </summary>
public class MemorySegmentTests {
	private static SourceLocation Loc() => new("test.pasm", 1, 1, 0);

	// ========================================================================
	// MemorySegment Tests
	// ========================================================================

	[Fact]
	public void MemorySegment_Constructor_SetsProperties() {
		// Act
		var segment = new MemorySegment("CODE", 0x8000, 0x4000, SegmentType.Code);

		// Assert
		Assert.Equal("CODE", segment.Name);
		Assert.Equal(0x8000, segment.StartAddress);
		Assert.Equal(0x4000, segment.MaxSize);
		Assert.Equal(SegmentType.Code, segment.Type);
		Assert.Equal(0, segment.CurrentOffset);
		Assert.Equal(0x8000, segment.CurrentAddress);
		Assert.False(segment.HasOverflowed);
	}

	[Fact]
	public void MemorySegment_WriteByte_IncreasesOffset() {
		// Arrange
		var segment = new MemorySegment("CODE", 0x8000, 0x100);

		// Act
		segment.WriteByte(0xea);

		// Assert
		Assert.Equal(1, segment.CurrentOffset);
		Assert.Equal(0x8001, segment.CurrentAddress);
		Assert.Single(segment.Data);
		Assert.Equal(0xea, segment.Data[0]);
	}

	[Fact]
	public void MemorySegment_WriteByte_DetectsOverflow() {
		// Arrange
		var segment = new MemorySegment("CODE", 0x8000, 2);

		// Act
		segment.WriteByte(0x01);
		segment.WriteByte(0x02);
		var result = segment.WriteByte(0x03);

		// Assert
		Assert.False(result); // Should indicate overflow
		Assert.True(segment.HasOverflowed);
	}

	[Fact]
	public void MemorySegment_BssType_DoesNotStoreData() {
		// Arrange
		var segment = new MemorySegment("BSS", 0x0200, 0x100, SegmentType.Bss);

		// Act
		segment.WriteByte(0xff);
		segment.WriteByte(0xff);

		// Assert
		Assert.Equal(2, segment.CurrentOffset);
		Assert.Empty(segment.Data); // BSS doesn't store actual data
	}

	[Fact]
	public void MemorySegment_Reserve_IncreasesOffsetWithoutData() {
		// Arrange
		var segment = new MemorySegment("CODE", 0x8000, 0x100);

		// Act
		segment.Reserve(10);

		// Assert
		Assert.Equal(10, segment.CurrentOffset);
		Assert.Empty(segment.Data);
	}

	[Fact]
	public void MemorySegment_Fill_WritesRepeatedValue() {
		// Arrange
		var segment = new MemorySegment("DATA", 0x8000, 0x100, SegmentType.Data);

		// Act
		segment.Fill(5, 0x00);

		// Assert
		Assert.Equal(5, segment.CurrentOffset);
		Assert.Equal(5, segment.Data.Count);
		Assert.All(segment.Data, b => Assert.Equal(0x00, b));
	}

	[Fact]
	public void MemorySegment_RemainingSpace_CalculatesCorrectly() {
		// Arrange
		var segment = new MemorySegment("CODE", 0x8000, 0x100);
		segment.WriteByte(0x01);
		segment.WriteByte(0x02);

		// Assert
		Assert.Equal(0xfe, segment.RemainingSpace);
	}

	// ========================================================================
	// SegmentManager Tests
	// ========================================================================

	[Fact]
	public void SegmentManager_Define_CreatesSegment() {
		// Arrange
		var manager = new SegmentManager();

		// Act
		var segment = manager.Define("CODE", 0x8000, 0x4000, SegmentType.Code, Loc());

		// Assert
		Assert.NotNull(segment);
		Assert.Equal("CODE", segment.Name);
		Assert.True(manager.Segments.ContainsKey("CODE"));
	}

	[Fact]
	public void SegmentManager_Define_IsCaseInsensitive() {
		// Arrange
		var manager = new SegmentManager();
		manager.Define("CODE", 0x8000, 0x4000, SegmentType.Code, Loc());

		// Act & Assert
		Assert.True(manager.Segments.ContainsKey("code"));
		Assert.True(manager.Segments.ContainsKey("Code"));
		Assert.True(manager.Segments.ContainsKey("CODE"));
	}

	[Fact]
	public void SegmentManager_Define_Duplicate_ReportsError() {
		// Arrange
		var manager = new SegmentManager();
		manager.Define("CODE", 0x8000, 0x4000, SegmentType.Code, Loc());

		// Act
		manager.Define("CODE", 0xc000, 0x2000, SegmentType.Code, Loc());

		// Assert
		Assert.True(manager.HasErrors);
		Assert.Contains(manager.Errors, e => e.Message.Contains("already defined"));
	}

	[Fact]
	public void SegmentManager_SwitchTo_SetsCurrentSegment() {
		// Arrange
		var manager = new SegmentManager();
		var codeSegment = manager.Define("CODE", 0x8000, 0x4000, SegmentType.Code, Loc());

		// Act
		var result = manager.SwitchTo("CODE", Loc());

		// Assert
		Assert.True(result);
		Assert.Same(codeSegment, manager.CurrentSegment);
	}

	[Fact]
	public void SegmentManager_SwitchTo_UndefinedSegment_ReportsError() {
		// Arrange
		var manager = new SegmentManager();

		// Act
		var result = manager.SwitchTo("UNDEFINED", Loc());

		// Assert
		Assert.False(result);
		Assert.True(manager.HasErrors);
		Assert.Contains(manager.Errors, e => e.Message.Contains("Undefined segment"));
	}

	[Fact]
	public void SegmentManager_SwitchBank_SetsCurrentBank() {
		// Arrange
		var manager = new SegmentManager();

		// Act
		manager.SwitchBank(3, Loc());

		// Assert
		Assert.Equal(3, manager.CurrentBank);
	}

	[Fact]
	public void SegmentManager_SwitchBank_NegativeBank_ReportsError() {
		// Arrange
		var manager = new SegmentManager();

		// Act
		manager.SwitchBank(-1, Loc());

		// Assert
		Assert.True(manager.HasErrors);
		Assert.Contains(manager.Errors, e => e.Message.Contains("Invalid bank"));
	}

	[Fact]
	public void SegmentManager_ValidateSegments_DetectsOverflow() {
		// Arrange
		var manager = new SegmentManager();
		var segment = manager.Define("CODE", 0x8000, 2, SegmentType.Code, Loc());
		segment.WriteByte(0x01);
		segment.WriteByte(0x02);
		segment.WriteByte(0x03); // Overflow

		// Act
		manager.ValidateSegments();

		// Assert
		Assert.True(manager.HasErrors);
		Assert.Contains(manager.Errors, e => e.Message.Contains("overflow"));
	}

	[Fact]
	public void SegmentManager_GetOrderedSegments_SortsByAddress() {
		// Arrange
		var manager = new SegmentManager();
		manager.Define("HIGH", 0xc000, 0x2000, SegmentType.Code, Loc());
		manager.Define("LOW", 0x8000, 0x4000, SegmentType.Code, Loc());
		manager.Define("ZERO", 0x0000, 0x0100, SegmentType.ZeroPage, Loc());

		// Act
		var ordered = manager.GetOrderedSegments().ToList();

		// Assert
		Assert.Equal("ZERO", ordered[0].Name);
		Assert.Equal("LOW", ordered[1].Name);
		Assert.Equal("HIGH", ordered[2].Name);
	}

	[Fact]
	public void SegmentManager_TryGetSegment_FindsExisting() {
		// Arrange
		var manager = new SegmentManager();
		manager.Define("CODE", 0x8000, 0x4000, SegmentType.Code, Loc());

		// Act
		var found = manager.TryGetSegment("CODE", out var segment);

		// Assert
		Assert.True(found);
		Assert.NotNull(segment);
		Assert.Equal("CODE", segment.Name);
	}

	[Fact]
	public void SegmentManager_TryGetSegment_ReturnsFalseForMissing() {
		// Arrange
		var manager = new SegmentManager();

		// Act
		var found = manager.TryGetSegment("MISSING", out var segment);

		// Assert
		Assert.False(found);
		Assert.Null(segment);
	}

	// ========================================================================
	// Default Segments Tests
	// ========================================================================

	[Fact]
	public void SegmentManager_CreateDefaultSegments_NES_CreatesExpectedSegments() {
		// Arrange
		var manager = new SegmentManager();

		// Act
		manager.CreateDefaultSegments(TargetArchitecture.MOS6502);

		// Assert
		Assert.True(manager.Segments.ContainsKey("ZEROPAGE"));
		Assert.True(manager.Segments.ContainsKey("RAM"));
		Assert.True(manager.Segments.ContainsKey("CODE"));
		Assert.Equal(0x0000, manager.Segments["ZEROPAGE"].StartAddress);
		Assert.Equal(0x8000, manager.Segments["CODE"].StartAddress);
	}

	[Fact]
	public void SegmentManager_CreateDefaultSegments_SNES_CreatesExpectedSegments() {
		// Arrange
		var manager = new SegmentManager();

		// Act
		manager.CreateDefaultSegments(TargetArchitecture.WDC65816);

		// Assert
		Assert.True(manager.Segments.ContainsKey("ZEROPAGE"));
		Assert.True(manager.Segments.ContainsKey("RAM"));
		Assert.True(manager.Segments.ContainsKey("CODE"));
	}

	[Fact]
	public void SegmentManager_CreateDefaultSegments_GB_CreatesExpectedSegments() {
		// Arrange
		var manager = new SegmentManager();

		// Act
		manager.CreateDefaultSegments(TargetArchitecture.SM83);

		// Assert
		Assert.True(manager.Segments.ContainsKey("ROM0"));
		Assert.True(manager.Segments.ContainsKey("ROMX"));
		Assert.True(manager.Segments.ContainsKey("VRAM"));
		Assert.True(manager.Segments.ContainsKey("WRAM0"));
		Assert.True(manager.Segments.ContainsKey("HRAM"));
		Assert.Equal(0x0000, manager.Segments["ROM0"].StartAddress);
		Assert.Equal(0x4000, manager.Segments["ROMX"].StartAddress);
	}

	// ========================================================================
	// Bank Assignment Tests
	// ========================================================================

	[Fact]
	public void SegmentManager_Define_AfterBankSwitch_AssignsBank() {
		// Arrange
		var manager = new SegmentManager();
		manager.SwitchBank(2, Loc());

		// Act
		var segment = manager.Define("DATA", 0x8000, 0x4000, SegmentType.Data, Loc());

		// Assert
		Assert.Equal(2, segment.Bank);
	}

	// ========================================================================
	// SegmentType Tests
	// ========================================================================

	[Fact]
	public void SegmentType_HasAllExpectedValues() {
		// Assert
		Assert.True(Enum.IsDefined(typeof(SegmentType), SegmentType.Code));
		Assert.True(Enum.IsDefined(typeof(SegmentType), SegmentType.Data));
		Assert.True(Enum.IsDefined(typeof(SegmentType), SegmentType.Bss));
		Assert.True(Enum.IsDefined(typeof(SegmentType), SegmentType.ZeroPage));
		Assert.True(Enum.IsDefined(typeof(SegmentType), SegmentType.Rom));
		Assert.True(Enum.IsDefined(typeof(SegmentType), SegmentType.Ram));
	}
}

