// ============================================================================
// MemoryMapGenerator.cs - Memory Map File Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text;
using Poppy.Core.Semantics;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Generates memory map files showing how code and data are organized in the output.
/// </summary>
public sealed class MemoryMapGenerator {
	private readonly IReadOnlyList<OutputSegment> _segments;
	private readonly SymbolTable _symbolTable;
	private readonly TargetArchitecture _target;

	/// <summary>
	/// Creates a new memory map generator.
	/// </summary>
	/// <param name="segments">The output segments from code generation.</param>
	/// <param name="symbolTable">The symbol table containing all labels.</param>
	/// <param name="target">The target architecture.</param>
	public MemoryMapGenerator(IReadOnlyList<OutputSegment> segments, SymbolTable symbolTable, TargetArchitecture target) {
		_segments = segments;
		_symbolTable = symbolTable;
		_target = target;
	}

	/// <summary>
	/// Generates a memory map string.
	/// </summary>
	/// <returns>The formatted memory map content.</returns>
	public string Generate() {
		var sb = new StringBuilder();

		// Header
		sb.AppendLine("; ============================================================================");
		sb.AppendLine("; Poppy Assembler Memory Map");
		sb.AppendLine($"; Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
		sb.AppendLine($"; Target: {_target}");
		sb.AppendLine("; ============================================================================");
		sb.AppendLine();

		// Segment summary
		GenerateSegmentSummary(sb);

		// Memory regions (based on target architecture)
		GenerateMemoryRegions(sb);

		// Label listing
		GenerateLabelListing(sb);

		// Statistics
		GenerateStatistics(sb);

		return sb.ToString();
	}

	/// <summary>
	/// Generates the segment summary section.
	/// </summary>
	private void GenerateSegmentSummary(StringBuilder sb) {
		sb.AppendLine("; === Segments ===");
		sb.AppendLine(";");
		sb.AppendLine("; #    Start     End       Size      Used");
		sb.AppendLine("; --   ------    ------    ------    ------");

		if (_segments.Count == 0) {
			sb.AppendLine("; (no segments)");
		} else {
			int index = 0;
			foreach (var segment in _segments.OrderBy(s => s.StartAddress)) {
				var startAddr = segment.StartAddress;
				var size = segment.Data.Count;
				var endAddr = startAddr + size - 1;

				sb.AppendLine($"; {index,2}   ${startAddr:x4}     ${endAddr:x4}     {size,6}    {size,6}");
				index++;
			}
		}

		sb.AppendLine();
	}

	/// <summary>
	/// Generates memory region analysis based on target architecture.
	/// </summary>
	private void GenerateMemoryRegions(StringBuilder sb) {
		sb.AppendLine("; === Memory Regions ===");
		sb.AppendLine(";");

		var regions = GetMemoryRegions();
		if (regions.Count == 0) {
			sb.AppendLine("; (no predefined regions for this target)");
			sb.AppendLine();
			return;
		}

		sb.AppendLine("; Region        Start     End       Size      Used      Free      Usage");
		sb.AppendLine("; ----------    ------    ------    ------    ------    ------    -----");

		foreach (var region in regions) {
			var used = CalculateUsedInRegion(region.Start, region.End);
			var size = region.End - region.Start + 1;
			var free = size - used;
			var usagePercent = size > 0 ? (used * 100.0 / size) : 0;

			sb.AppendLine($"; {region.Name,-12}  ${region.Start:x4}     ${region.End:x4}     {size,6}    {used,6}    {free,6}    {usagePercent,4:F1}%");
		}

		sb.AppendLine();
	}

	/// <summary>
	/// Generates the label listing section.
	/// </summary>
	private void GenerateLabelListing(StringBuilder sb) {
		sb.AppendLine("; === Labels ===");
		sb.AppendLine(";");

		var labels = _symbolTable.Symbols.Values
			.Where(s => s.IsDefined && s.Value.HasValue && s.Type == SymbolType.Label)
			.OrderBy(s => s.Value!.Value)
			.ToList();

		if (labels.Count == 0) {
			sb.AppendLine("; (no labels)");
		} else {
			foreach (var label in labels) {
				sb.AppendLine($"; ${label.Value!.Value:x4}  {label.Name}");
			}
		}

		sb.AppendLine();
	}

	/// <summary>
	/// Generates statistics section.
	/// </summary>
	private void GenerateStatistics(StringBuilder sb) {
		sb.AppendLine("; === Statistics ===");
		sb.AppendLine(";");

		var totalBytes = _segments.Sum(s => s.Data.Count);
		var labelCount = _symbolTable.Symbols.Values.Count(s => s.IsDefined && s.Type == SymbolType.Label);
		var constantCount = _symbolTable.Symbols.Values.Count(s => s.IsDefined && s.Type == SymbolType.Constant);
		var macroCount = _symbolTable.Symbols.Values.Count(s => s.IsDefined && s.Type == SymbolType.Macro);

		sb.AppendLine($"; Total bytes:      {totalBytes}");
		sb.AppendLine($"; Segments:         {_segments.Count}");
		sb.AppendLine($"; Labels:           {labelCount}");
		sb.AppendLine($"; Constants:        {constantCount}");
		sb.AppendLine($"; Macros:           {macroCount}");

		if (_segments.Count > 0) {
			var minAddr = _segments.Min(s => s.StartAddress);
			var maxAddr = _segments.Max(s => s.StartAddress + s.Data.Count - 1);
			sb.AppendLine($"; Address range:    ${minAddr:x4} - ${maxAddr:x4}");
		}

		sb.AppendLine();
	}

	/// <summary>
	/// Calculates how many bytes are used in a given address range.
	/// </summary>
	private long CalculateUsedInRegion(long start, long end) {
		long used = 0;

		foreach (var segment in _segments) {
			var segStart = segment.StartAddress;
			var segEnd = segStart + segment.Data.Count - 1;

			// Check for overlap
			if (segEnd >= start && segStart <= end) {
				var overlapStart = Math.Max(segStart, start);
				var overlapEnd = Math.Min(segEnd, end);
				used += overlapEnd - overlapStart + 1;
			}
		}

		return used;
	}

	/// <summary>
	/// Gets predefined memory regions for the target architecture.
	/// </summary>
	private List<MemoryRegion> GetMemoryRegions() {
		return _target switch {
			TargetArchitecture.MOS6502 => [
				new MemoryRegion("Zero Page", 0x0000, 0x00ff),
				new MemoryRegion("Stack", 0x0100, 0x01ff),
				new MemoryRegion("RAM", 0x0200, 0x07ff),
				new MemoryRegion("PRG-ROM", 0x8000, 0xffff),
			],
			TargetArchitecture.WDC65816 => [
				new MemoryRegion("Direct Page", 0x0000, 0x00ff),
				new MemoryRegion("Stack", 0x0100, 0x01ff),
				new MemoryRegion("Low RAM", 0x0200, 0x1fff),
				new MemoryRegion("High RAM", 0x7e0000, 0x7fffff),
			],
			TargetArchitecture.SM83 => [
				new MemoryRegion("ROM Bank 0", 0x0000, 0x3fff),
				new MemoryRegion("ROM Bank N", 0x4000, 0x7fff),
				new MemoryRegion("VRAM", 0x8000, 0x9fff),
				new MemoryRegion("Work RAM", 0xc000, 0xdfff),
				new MemoryRegion("High RAM", 0xff80, 0xfffe),
			],
			_ => []
		};
	}

	/// <summary>
	/// Exports the memory map to a file.
	/// </summary>
	/// <param name="path">Output file path.</param>
	public void Export(string path) {
		var content = Generate();
		File.WriteAllText(path, content, Encoding.UTF8);
	}
}

/// <summary>
/// Represents a named memory region.
/// </summary>
internal sealed record MemoryRegion(string Name, long Start, long End);

