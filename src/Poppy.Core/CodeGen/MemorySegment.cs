// ============================================================================
// MemorySegment.cs - Named Memory Segments for Code Organization
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Represents a named memory segment with specific properties.
/// </summary>
public sealed class MemorySegment {
	/// <summary>
	/// The segment name (e.g., "CODE", "DATA", "ZEROPAGE").
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The starting address of the segment.
	/// </summary>
	public long StartAddress { get; }

	/// <summary>
	/// The maximum size of the segment in bytes.
	/// </summary>
	public long MaxSize { get; }

	/// <summary>
	/// The segment type (code, data, bss).
	/// </summary>
	public SegmentType Type { get; }

	/// <summary>
	/// The bank number for bank-switched systems.
	/// </summary>
	public int Bank { get; set; }

	/// <summary>
	/// The data bytes in this segment.
	/// </summary>
	public List<byte> Data { get; } = [];

	/// <summary>
	/// The current write position within the segment.
	/// </summary>
	public long CurrentOffset { get; set; }

	/// <summary>
	/// The location where this segment was defined.
	/// </summary>
	public SourceLocation? DefinitionLocation { get; set; }

	/// <summary>
	/// Gets the current absolute address within the segment.
	/// </summary>
	public long CurrentAddress => StartAddress + CurrentOffset;

	/// <summary>
	/// Gets the remaining space in the segment.
	/// </summary>
	public long RemainingSpace => MaxSize - CurrentOffset;

	/// <summary>
	/// Gets whether the segment has overflowed its maximum size.
	/// </summary>
	public bool HasOverflowed => CurrentOffset > MaxSize;

	/// <summary>
	/// Creates a new memory segment.
	/// </summary>
	/// <param name="name">The segment name.</param>
	/// <param name="startAddress">The starting address.</param>
	/// <param name="maxSize">The maximum size in bytes.</param>
	/// <param name="type">The segment type.</param>
	public MemorySegment(string name, long startAddress, long maxSize, SegmentType type = SegmentType.Code) {
		Name = name;
		StartAddress = startAddress;
		MaxSize = maxSize;
		Type = type;
		Bank = 0;
		CurrentOffset = 0;
	}

	/// <summary>
	/// Writes a byte to the segment.
	/// </summary>
	/// <param name="value">The byte value to write.</param>
	/// <returns>True if successful, false if segment overflow.</returns>
	public bool WriteByte(byte value) {
		if (Type == SegmentType.Bss) {
			// BSS segments don't store data, just reserve space
			CurrentOffset++;
			return CurrentOffset <= MaxSize;
		}

		Data.Add(value);
		CurrentOffset++;
		return CurrentOffset <= MaxSize;
	}

	/// <summary>
	/// Reserves space without writing data.
	/// </summary>
	/// <param name="count">Number of bytes to reserve.</param>
	/// <returns>True if successful, false if segment overflow.</returns>
	public bool Reserve(long count) {
		CurrentOffset += count;
		return CurrentOffset <= MaxSize;
	}

	/// <summary>
	/// Fills space with a specific value.
	/// </summary>
	/// <param name="count">Number of bytes to fill.</param>
	/// <param name="value">The fill value.</param>
	/// <returns>True if successful, false if segment overflow.</returns>
	public bool Fill(long count, byte value) {
		if (Type == SegmentType.Bss) {
			return Reserve(count);
		}

		for (long i = 0; i < count; i++) {
			Data.Add(value);
		}

		CurrentOffset += count;
		return CurrentOffset <= MaxSize;
	}
}

/// <summary>
/// Types of memory segments.
/// </summary>
public enum SegmentType {
	/// <summary>
	/// Code segment - contains executable instructions.
	/// </summary>
	Code,

	/// <summary>
	/// Data segment - contains initialized data.
	/// </summary>
	Data,

	/// <summary>
	/// BSS segment - uninitialized data (reserves space only).
	/// </summary>
	Bss,

	/// <summary>
	/// Zero page segment - for 6502 zero page addressing.
	/// </summary>
	ZeroPage,

	/// <summary>
	/// ROM segment - read-only memory.
	/// </summary>
	Rom,

	/// <summary>
	/// RAM segment - read-write memory.
	/// </summary>
	Ram
}

/// <summary>
/// Manages named memory segments for the assembler.
/// </summary>
public sealed class SegmentManager {
	private readonly Dictionary<string, MemorySegment> _segments = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<SegmentError> _errors = [];
	private MemorySegment? _currentSegment;
	private int _currentBank;

	/// <summary>
	/// Gets all defined segments.
	/// </summary>
	public IReadOnlyDictionary<string, MemorySegment> Segments => _segments;

	/// <summary>
	/// Gets the currently active segment.
	/// </summary>
	public MemorySegment? CurrentSegment => _currentSegment;

	/// <summary>
	/// Gets the current bank number.
	/// </summary>
	public int CurrentBank => _currentBank;

	/// <summary>
	/// Gets all segment errors.
	/// </summary>
	public IReadOnlyList<SegmentError> Errors => _errors;

	/// <summary>
	/// Gets whether any errors have occurred.
	/// </summary>
	public bool HasErrors => _errors.Count > 0;

	/// <summary>
	/// Defines a new segment.
	/// </summary>
	/// <param name="name">The segment name.</param>
	/// <param name="startAddress">The starting address.</param>
	/// <param name="maxSize">The maximum size.</param>
	/// <param name="type">The segment type.</param>
	/// <param name="location">The definition location.</param>
	/// <returns>The created segment.</returns>
	public MemorySegment Define(string name, long startAddress, long maxSize, SegmentType type, SourceLocation location) {
		if (_segments.TryGetValue(name, out var existing)) {
			_errors.Add(new SegmentError(
				$"Segment '{name}' already defined at {existing.DefinitionLocation}",
				location));
			return existing;
		}

		var segment = new MemorySegment(name, startAddress, maxSize, type) {
			DefinitionLocation = location,
			Bank = _currentBank
		};

		_segments[name] = segment;
		return segment;
	}

	/// <summary>
	/// Switches to a named segment.
	/// </summary>
	/// <param name="name">The segment name.</param>
	/// <param name="location">The source location.</param>
	/// <returns>True if successful.</returns>
	public bool SwitchTo(string name, SourceLocation location) {
		if (!_segments.TryGetValue(name, out var segment)) {
			_errors.Add(new SegmentError(
				$"Undefined segment: '{name}'",
				location));
			return false;
		}

		_currentSegment = segment;
		return true;
	}

	/// <summary>
	/// Switches to a bank.
	/// </summary>
	/// <param name="bank">The bank number.</param>
	/// <param name="location">The source location.</param>
	public void SwitchBank(int bank, SourceLocation location) {
		if (bank < 0) {
			_errors.Add(new SegmentError(
				$"Invalid bank number: {bank}",
				location));
			return;
		}

		_currentBank = bank;
	}

	/// <summary>
	/// Gets a segment by name.
	/// </summary>
	/// <param name="name">The segment name.</param>
	/// <param name="segment">The segment if found.</param>
	/// <returns>True if the segment exists.</returns>
	public bool TryGetSegment(string name, out MemorySegment? segment) {
		return _segments.TryGetValue(name, out segment);
	}

	/// <summary>
	/// Validates all segments for overflow.
	/// </summary>
	public void ValidateSegments() {
		foreach (var (name, segment) in _segments) {
			if (segment.HasOverflowed) {
				_errors.Add(new SegmentError(
					$"Segment '{name}' overflow: {segment.CurrentOffset} bytes exceeds {segment.MaxSize} byte limit",
					segment.DefinitionLocation ?? new SourceLocation("", 0, 0, 0)));
			}
		}
	}

	/// <summary>
	/// Gets all segments in order of start address.
	/// </summary>
	public IEnumerable<MemorySegment> GetOrderedSegments() {
		return _segments.Values.OrderBy(s => s.StartAddress);
	}

	/// <summary>
	/// Creates default segments for a target architecture.
	/// </summary>
	/// <param name="target">The target architecture.</param>
	public void CreateDefaultSegments(Semantics.TargetArchitecture target) {
		switch (target) {
			case Semantics.TargetArchitecture.MOS6502:
				// NES default segments
				Define("ZEROPAGE", 0x0000, 0x0100, SegmentType.ZeroPage, new SourceLocation("", 0, 0, 0));
				Define("RAM", 0x0200, 0x0600, SegmentType.Ram, new SourceLocation("", 0, 0, 0));
				Define("CODE", 0x8000, 0x8000, SegmentType.Code, new SourceLocation("", 0, 0, 0));
				break;

			case Semantics.TargetArchitecture.WDC65816:
				// SNES default segments
				Define("ZEROPAGE", 0x0000, 0x0100, SegmentType.ZeroPage, new SourceLocation("", 0, 0, 0));
				Define("RAM", 0x7e0000, 0x020000, SegmentType.Ram, new SourceLocation("", 0, 0, 0));
				Define("CODE", 0x008000, 0x008000, SegmentType.Code, new SourceLocation("", 0, 0, 0));
				break;

			case Semantics.TargetArchitecture.SM83:
				// Game Boy default segments
				Define("ROM0", 0x0000, 0x4000, SegmentType.Rom, new SourceLocation("", 0, 0, 0));
				Define("ROMX", 0x4000, 0x4000, SegmentType.Rom, new SourceLocation("", 0, 0, 0));
				Define("VRAM", 0x8000, 0x2000, SegmentType.Ram, new SourceLocation("", 0, 0, 0));
				Define("WRAM0", 0xc000, 0x1000, SegmentType.Ram, new SourceLocation("", 0, 0, 0));
				Define("HRAM", 0xff80, 0x007f, SegmentType.Ram, new SourceLocation("", 0, 0, 0));
				break;
		}
	}
}

/// <summary>
/// Represents a segment-related error.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Location">The source location.</param>
public sealed record SegmentError(string Message, SourceLocation Location) {
	/// <inheritdoc />
	public override string ToString() => $"{Location}: error: {Message}";
}

