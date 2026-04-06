// ============================================================================
// PansyGenerator.cs - Program ANalysis SYstem File Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================
// Delegates all binary serialization to Pansy.Core.PansyWriter.
// Integrates LabelMergeEngine for hardware register auto-labeling.
// ============================================================================

using Pansy.Core;
using Poppy.Core.Semantics;
using PansyCrossRefType = Pansy.Core.CrossRefType;
using PansySymbolType = Pansy.Core.SymbolType;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Generates Pansy (Program ANalysis SYstem) files for comprehensive metadata exchange.
/// Pansy files contain code/data maps, symbols, comments, cross-references, and more,
/// providing complete roundtrip support between Poppy (assembler) and Peony (disassembler).
/// </summary>
public sealed class PansyGenerator {
	private readonly SymbolTable _symbolTable;
	private readonly TargetArchitecture _target;
	private readonly IReadOnlyList<OutputSegment> _segments;
	private readonly ListingGenerator? _listing;
	private readonly CdlGenerator? _cdlGenerator;

	// Track cross-references
	private readonly List<(uint From, uint To, PansyCrossRefType Type)> _crossRefs = [];

	// Track comments
	private readonly List<(uint Address, string Text, CommentType Type)> _comments = [];

	/// <summary>Project name for metadata section.</summary>
	public string ProjectName { get; set; } = "";

	/// <summary>Author name for metadata section.</summary>
	public string Author { get; set; } = "";

	/// <summary>Version string for metadata section.</summary>
	public string Version { get; set; } = "1.0.0";

	/// <summary>
	/// Creates a new Pansy generator.
	/// </summary>
	/// <param name="symbolTable">The symbol table from compilation.</param>
	/// <param name="target">The target architecture.</param>
	/// <param name="segments">The output segments from code generation.</param>
	/// <param name="listing">Optional listing generator for detailed tracking.</param>
	/// <param name="cdlGenerator">Optional CDL generator for cross-reference data.</param>
	public PansyGenerator(
		SymbolTable symbolTable,
		TargetArchitecture target,
		IReadOnlyList<OutputSegment> segments,
		ListingGenerator? listing = null,
		CdlGenerator? cdlGenerator = null) {
		_symbolTable = symbolTable;
		_target = target;
		_segments = segments;
		_listing = listing;
		_cdlGenerator = cdlGenerator;
	}

	/// <summary>
	/// Registers a cross-reference from one address to another.
	/// </summary>
	public void RegisterCrossRef(uint fromAddress, uint toAddress, PansyCrossRefType type) {
		_crossRefs.Add((fromAddress, toAddress, type));
	}

	/// <summary>
	/// Populates cross-references from instruction analysis data collected by CodeGenerator.
	/// </summary>
	/// <param name="crossRefs">Cross-references from CodeGenerator (From, To, TypeByte).</param>
	public void PopulateCrossRefsFromCodeGenerator(IReadOnlyList<(uint From, uint To, byte Type)> crossRefs) {
		foreach (var (from, to, type) in crossRefs) {
			_crossRefs.Add((from, to, (PansyCrossRefType)type));
		}
	}

	/// <summary>
	/// Registers a comment at a ROM address.
	/// </summary>
	/// <param name="address">The ROM address to attach the comment to.</param>
	/// <param name="text">The comment text.</param>
	/// <param name="commentType">The comment type (1=Inline, 2=Block, 3=Todo).</param>
	public void RegisterComment(uint address, string text, byte commentType = 1) {
		_comments.Add((address, text, (CommentType)commentType));
	}

	/// <summary>
	/// Generates Pansy file data using PansyWriter from Pansy.Core.
	/// </summary>
	/// <param name="romSize">The total ROM size in bytes.</param>
	/// <param name="romCrc32">CRC32 of the ROM (optional).</param>
	/// <param name="compress">Whether to compress section data.</param>
	/// <returns>The complete Pansy file bytes.</returns>
	public byte[] Generate(int romSize, uint romCrc32 = 0, bool compress = true) {
		var platformId = GetPlatformId();
		var writer = new PansyWriter {
			Platform = platformId,
			RomSize = (uint)romSize,
			RomCrc32 = romCrc32,
			EnableCompression = compress,
			ProjectName = ProjectName,
			Author = Author,
			ProjectVersion = Version,
		};

		// Populate code/data map
		PopulateCodeDataMap(writer, romSize);

		// Build and merge symbols using LabelMergeEngine
		PopulateSymbols(writer, platformId);

		// Add comments
		foreach (var (address, text, type) in _comments.OrderBy(c => c.Address)) {
			writer.AddComment(address, text, type);
		}

		// Add memory regions from output segments
		PopulateMemoryRegions(writer);

		// Add cross-references
		foreach (var (from, to, type) in _crossRefs) {
			writer.AddCrossReference(new CrossReference(from, to, type));
		}

		// Add source map if listing is available
		if (_listing is not null) {
			PopulateSourceMap(writer);
		}

		return writer.Generate();
	}

	/// <summary>
	/// Exports Pansy data to a file.
	/// </summary>
	/// <param name="path">Output file path.</param>
	/// <param name="romSize">Total ROM size in bytes.</param>
	/// <param name="romCrc32">CRC32 of the ROM (optional).</param>
	/// <param name="compress">Whether to compress section data.</param>
	public void Export(string path, int romSize, uint romCrc32 = 0, bool compress = true) {
		var data = Generate(romSize, romCrc32, compress);
		File.WriteAllBytes(path, data);
	}

	/// <summary>
	/// Populates the code/data map in the PansyWriter.
	/// </summary>
	private void PopulateCodeDataMap(PansyWriter writer, int romSize) {
		// Mark all code segments
		foreach (var segment in _segments) {
			var startAddr = (int)segment.StartAddress;
			var segmentLength = segment.Data.Count;

			for (int i = 0; i < segmentLength && (startAddr + i) < romSize; i++) {
				var romOffset = CpuToRomAddress(startAddr + i);
				if (romOffset >= 0 && romOffset < romSize) {
					writer.MarkAsCode((uint)romOffset);
				}
			}
		}

		// Use listing data for precise opcode/operand marking
		if (_listing is not null) {
			foreach (var entry in _listing.Entries) {
				if (entry.Bytes.Length == 0) continue;

				var romOffset = CpuToRomAddress((int)entry.Address);
				if (romOffset < 0 || romOffset >= romSize) continue;

				// First byte is the opcode
				writer.MarkAsCode((uint)romOffset);
				writer.MarkAsOpcode((uint)romOffset);

				// Remaining bytes are operands (code but not opcode)
				for (int i = 1; i < entry.Bytes.Length; i++) {
					var operandOffset = romOffset + i;
					if (operandOffset >= 0 && operandOffset < romSize) {
						writer.MarkAsCode((uint)operandOffset);
					}
				}
			}
		}

		// Mark symbol targets (jump targets, sub entries)
		foreach (var symbol in _symbolTable.Symbols.Values) {
			if (!symbol.IsDefined || !symbol.Value.HasValue) continue;

			var romOffset = CpuToRomAddress((int)symbol.Value.Value);
			if (romOffset < 0 || romOffset >= romSize) continue;

			if (symbol.Type == Semantics.SymbolType.Label) {
				if (symbol.Name.StartsWith("sub_", StringComparison.OrdinalIgnoreCase) || symbol.Name.StartsWith("fn_", StringComparison.OrdinalIgnoreCase) || symbol.Name.StartsWith("func_", StringComparison.OrdinalIgnoreCase)) {
					writer.MarkAsSubroutine((uint)romOffset);
				}
				writer.MarkAsJumpTarget((uint)romOffset);
			}
		}
	}

	/// <summary>
	/// Populates symbols using LabelMergeEngine with hardware register enrichment.
	/// </summary>
	private void PopulateSymbols(PansyWriter writer, byte platformId) {
		var mergeEngine = new LabelMergeEngine();

		// Add user symbols from the symbol table (highest priority)
		foreach (var symbol in _symbolTable.Symbols.Values) {
			if (!symbol.IsDefined || !symbol.Value.HasValue) continue;

			var address = (uint)symbol.Value.Value;
			PansySymbolType entryType = symbol.Type switch {
				Semantics.SymbolType.Label => PansySymbolType.Label,
				Semantics.SymbolType.Constant => PansySymbolType.Constant,
				Semantics.SymbolType.Macro => PansySymbolType.Macro,
				_ => PansySymbolType.Label,
			};
			mergeEngine.Add(new MergedLabel(address, symbol.Name, entryType, LabelSource.User));
		}

		// Add hardware register names for the platform
		mergeEngine.AddHardwareRegisters(platformId);

		// Write merged result to the writer
		writer.AddSymbols(mergeEngine.GetMergedSymbols().ToList());
	}

	/// <summary>
	/// Populates memory regions from output segments.
	/// </summary>
	private void PopulateMemoryRegions(PansyWriter writer) {
		int segmentIndex = 0;
		foreach (var segment in _segments) {
			var startAddr = (uint)segment.StartAddress;
			var endAddr = (uint)(segment.StartAddress + segment.Data.Count - 1);
			var bank = (byte)((segment.StartAddress >> 16) & 0xff);
			var name = $"seg_{segmentIndex:d2}_${segment.StartAddress:x6}";

			writer.AddMemoryRegion(new Pansy.Core.MemoryRegion(
				startAddr, endAddr, (byte)Pansy.Core.MemoryRegionType.ROM, bank, name));
			segmentIndex++;
		}
	}

	/// <summary>
	/// Populates source map entries from listing data.
	/// </summary>
	private void PopulateSourceMap(PansyWriter writer) {
		if (_listing is null) return;

		// Register all unique file paths
		var fileIndex = new Dictionary<string, ushort>();
		foreach (var entry in _listing.Entries) {
			var filePath = entry.Location.FilePath;
			if (!string.IsNullOrEmpty(filePath) && !fileIndex.ContainsKey(filePath)) {
				fileIndex[filePath] = writer.AddSourceFile(filePath);
			}
		}

		// Add source map entries
		foreach (var entry in _listing.Entries) {
			if (entry.Bytes.Length == 0) continue;
			var filePath = entry.Location.FilePath;
			if (string.IsNullOrEmpty(filePath)) continue;

			var romOffset = CpuToRomAddress((int)entry.Address);
			if (romOffset < 0) continue;

			writer.AddSourceMapping(new SourceMapEntry(
				(uint)romOffset,
				fileIndex[filePath],
				(ushort)entry.Location.Line,
				(ushort)entry.Location.Column));
		}
	}

	/// <summary>
	/// Gets the Pansy platform ID for the target architecture.
	/// </summary>
	private byte GetPlatformId() {
		return _target switch {
			TargetArchitecture.MOS6502 => PansyLoader.PLATFORM_NES,
			TargetArchitecture.WDC65816 => PansyLoader.PLATFORM_SNES,
			TargetArchitecture.SM83 => PansyLoader.PLATFORM_GB,
			TargetArchitecture.Z80 => PansyLoader.PLATFORM_SMS,
			TargetArchitecture.M68000 => PansyLoader.PLATFORM_GENESIS,
			TargetArchitecture.SPC700 => PansyLoader.PLATFORM_SPC700,
			TargetArchitecture.ARM7TDMI => PansyLoader.PLATFORM_GBA,
			TargetArchitecture.MOS65SC02 => PansyLoader.PLATFORM_LYNX,
			TargetArchitecture.HuC6280 => PansyLoader.PLATFORM_PCE,
			TargetArchitecture.V30MZ => PansyLoader.PLATFORM_WONDERSWAN,
			TargetArchitecture.MOS6507 => PansyLoader.PLATFORM_ATARI_2600,
			TargetArchitecture.F8 => PansyLoader.PLATFORM_CHANNEL_F,
			_ => PansyLoader.PLATFORM_CUSTOM,
		};
	}

	/// <summary>
	/// Maps CPU address to ROM file offset.
	/// </summary>
	private int CpuToRomAddress(int cpuAddress) {
		// NES: PRG ROM starts at $8000, typically maps to offset 0x10 (after iNES header)
		if (_target == TargetArchitecture.MOS6502) {
			if (cpuAddress >= 0x8000) {
				return cpuAddress - 0x8000 + 0x10; // 16-byte iNES header
			}
			return -1; // Not PRG ROM
		}

		// SNES: Complex banking, simplified for LoROM
		if (_target == TargetArchitecture.WDC65816) {
			// LoROM mapping (simplified)
			var bank = (cpuAddress >> 16) & 0xff;
			var offset = cpuAddress & 0xffff;
			if (offset >= 0x8000) {
				return ((bank & 0x7f) * 0x8000) + (offset - 0x8000);
			}
			return -1;
		}

		// Game Boy: Direct mapping with header offset
		if (_target == TargetArchitecture.SM83) {
			if (cpuAddress >= 0 && cpuAddress < 0x8000) {
				return cpuAddress;
			}
			return -1;
		}

		// Default: assume direct mapping
		return cpuAddress;
	}
}
