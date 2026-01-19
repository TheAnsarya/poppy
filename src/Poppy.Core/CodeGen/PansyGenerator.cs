// ============================================================================
// PansyGenerator.cs - Program ANalysis SYstem File Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.IO.Compression;
using System.Text;
using Poppy.Core.Semantics;

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

	// Pansy file magic and version
	private static readonly byte[] Magic = "PANSY\0\0\0"u8.ToArray();
	private const ushort FormatVersion = 0x0100; // v1.0

	#region Platform IDs
	/// <summary>Platform ID for NES (MOS 6502).</summary>
	public const byte PLATFORM_NES = 0x01;
	/// <summary>Platform ID for SNES (WDC 65816).</summary>
	public const byte PLATFORM_SNES = 0x02;
	/// <summary>Platform ID for Game Boy (Sharp SM83).</summary>
	public const byte PLATFORM_GB = 0x03;
	/// <summary>Platform ID for Game Boy Advance (ARM7TDMI).</summary>
	public const byte PLATFORM_GBA = 0x04;
	/// <summary>Platform ID for Sega Genesis (Motorola 68000).</summary>
	public const byte PLATFORM_GENESIS = 0x05;
	/// <summary>Platform ID for Sega Master System (Zilog Z80).</summary>
	public const byte PLATFORM_SMS = 0x06;
	/// <summary>Platform ID for TurboGrafx-16 (HuC6280).</summary>
	public const byte PLATFORM_PCE = 0x07;
	/// <summary>Platform ID for Atari 2600 (MOS 6507).</summary>
	public const byte PLATFORM_ATARI_2600 = 0x08;
	/// <summary>Platform ID for Atari Lynx (WDC 65SC02).</summary>
	public const byte PLATFORM_LYNX = 0x09;
	/// <summary>Platform ID for WonderSwan (NEC V30MZ).</summary>
	public const byte PLATFORM_WONDERSWAN = 0x0a;
	/// <summary>Platform ID for Neo Geo (Motorola 68000).</summary>
	public const byte PLATFORM_NEOGEO = 0x0b;
	/// <summary>Platform ID for SPC700 audio processor.</summary>
	public const byte PLATFORM_SPC700 = 0x0c;
	/// <summary>Platform ID for Commodore 64 (MOS 6510).</summary>
	public const byte PLATFORM_C64 = 0x0d;
	/// <summary>Platform ID for MSX (Zilog Z80).</summary>
	public const byte PLATFORM_MSX = 0x0e;
	/// <summary>Platform ID for Atari 7800 (6502C/SALLY).</summary>
	public const byte PLATFORM_ATARI_7800 = 0x0f;
	/// <summary>Platform ID for Atari 8-bit (400/800/XL/XE) (MOS 6502).</summary>
	public const byte PLATFORM_ATARI_8BIT = 0x10;
	/// <summary>Platform ID for Apple II (MOS 6502).</summary>
	public const byte PLATFORM_APPLE_II = 0x11;
	/// <summary>Platform ID for ZX Spectrum (Zilog Z80).</summary>
	public const byte PLATFORM_ZX_SPECTRUM = 0x12;
	/// <summary>Platform ID for ColecoVision (Zilog Z80).</summary>
	public const byte PLATFORM_COLECO = 0x13;
	/// <summary>Platform ID for Intellivision (GI CP1610).</summary>
	public const byte PLATFORM_INTELLIVISION = 0x14;
	/// <summary>Platform ID for Vectrex (Motorola 6809).</summary>
	public const byte PLATFORM_VECTREX = 0x15;
	/// <summary>Platform ID for Sega Game Gear (Zilog Z80).</summary>
	public const byte PLATFORM_GAMEGEAR = 0x16;
	/// <summary>Platform ID for Sega 32X (SH-2).</summary>
	public const byte PLATFORM_32X = 0x17;
	/// <summary>Platform ID for Sega CD (Motorola 68000).</summary>
	public const byte PLATFORM_SEGACD = 0x18;
	/// <summary>Platform ID for Nintendo Virtual Boy (NEC V810).</summary>
	public const byte PLATFORM_VIRTUALBOY = 0x19;
	/// <summary>Platform ID for Amstrad CPC (Zilog Z80).</summary>
	public const byte PLATFORM_AMSTRAD_CPC = 0x1a;
	/// <summary>Platform ID for BBC Micro (MOS 6502).</summary>
	public const byte PLATFORM_BBC_MICRO = 0x1b;
	/// <summary>Platform ID for Commodore VIC-20 (MOS 6502).</summary>
	public const byte PLATFORM_VIC20 = 0x1c;
	/// <summary>Platform ID for Commodore Plus/4 (MOS 7501/8501).</summary>
	public const byte PLATFORM_PLUS4 = 0x1d;
	/// <summary>Platform ID for Commodore 128 (MOS 8502 + Zilog Z80).</summary>
	public const byte PLATFORM_C128 = 0x1e;
	/// <summary>Platform ID for custom/unknown platform.</summary>
	public const byte PLATFORM_CUSTOM = 0xff;
	#endregion

	#region Pansy Flags
	/// <summary>
	/// Flags for Pansy file features.
	/// </summary>
	[Flags]
	public enum PansyFlags : ushort {
		/// <summary>No flags.</summary>
		None = 0,
		/// <summary>Section data is compressed.</summary>
		Compressed = 0x0001,
		/// <summary>File contains source map section.</summary>
		HasSourceMap = 0x0002,
		/// <summary>File contains cross-reference section.</summary>
		HasCrossRefs = 0x0004,
		/// <summary>File has detailed CDL with instruction tracking.</summary>
		DetailedCdl = 0x0008,
	}
	#endregion

	#region Section Types
	/// <summary>Section type for code/data map.</summary>
	public const uint SECTION_CODE_DATA_MAP = 0x0001;
	/// <summary>Section type for symbols (labels, constants).</summary>
	public const uint SECTION_SYMBOLS = 0x0002;
	/// <summary>Section type for comments.</summary>
	public const uint SECTION_COMMENTS = 0x0003;
	/// <summary>Section type for memory regions.</summary>
	public const uint SECTION_MEMORY_REGIONS = 0x0004;
	/// <summary>Section type for data types.</summary>
	public const uint SECTION_DATA_TYPES = 0x0005;
	/// <summary>Section type for cross-references.</summary>
	public const uint SECTION_CROSS_REFS = 0x0006;
	/// <summary>Section type for source map.</summary>
	public const uint SECTION_SOURCE_MAP = 0x0007;
	/// <summary>Section type for metadata.</summary>
	public const uint SECTION_METADATA = 0x0008;
	#endregion

	#region ByteFlags
	/// <summary>
	/// Per-byte classification flags for code/data map.
	/// </summary>
	[Flags]
	public enum ByteFlags : byte {
		/// <summary>No classification.</summary>
		None = 0,
		/// <summary>Byte is code.</summary>
		Code = 0x01,
		/// <summary>Byte is data.</summary>
		Data = 0x02,
		/// <summary>Byte is a jump target.</summary>
		JumpTarget = 0x04,
		/// <summary>Byte is a subroutine entry point.</summary>
		SubEntry = 0x08,
		/// <summary>Byte is an opcode (not operand).</summary>
		Opcode = 0x10,
		/// <summary>Byte was drawn (graphics).</summary>
		Drawn = 0x20,
		/// <summary>Byte was read.</summary>
		Read = 0x40,
		/// <summary>Byte was accessed indirectly.</summary>
		Indirect = 0x80,
	}
	#endregion

	#region Symbol Types
	/// <summary>
	/// Types of symbol entries.
	/// </summary>
	public enum SymbolEntryType : byte {
		/// <summary>Code or data label.</summary>
		Label = 1,
		/// <summary>Named constant value.</summary>
		Constant = 2,
		/// <summary>Enumeration member.</summary>
		Enum = 3,
		/// <summary>Structure definition.</summary>
		Struct = 4,
		/// <summary>Macro definition.</summary>
		Macro = 5,
		/// <summary>Local label (within scope).</summary>
		Local = 6,
		/// <summary>Anonymous label (+/-).</summary>
		Anonymous = 7,
	}
	#endregion

	#region Cross-Reference Types
	/// <summary>
	/// Types of cross-references.
	/// </summary>
	public enum CrossRefType : byte {
		/// <summary>Subroutine call (JSR/JSL/CALL).</summary>
		Jsr = 1,
		/// <summary>Jump (JMP/JML).</summary>
		Jmp = 2,
		/// <summary>Branch (BRA/BEQ/etc).</summary>
		Branch = 3,
		/// <summary>Read access.</summary>
		Read = 4,
		/// <summary>Write access.</summary>
		Write = 5,
	}
	#endregion

	// Internal section data structure
	private class SectionData {
		public uint Type { get; set; }
		public byte[] Data { get; set; } = [];
	}

	// Track cross-references
	private readonly List<(uint From, uint To, CrossRefType Type)> _crossRefs = [];

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
	public void RegisterCrossRef(uint fromAddress, uint toAddress, CrossRefType type) {
		_crossRefs.Add((fromAddress, toAddress, type));
	}

	/// <summary>
	/// Generates Pansy file data.
	/// </summary>
	/// <param name="romSize">The total ROM size in bytes.</param>
	/// <param name="romCrc32">CRC32 of the ROM (optional).</param>
	/// <param name="compress">Whether to compress section data.</param>
	/// <returns>The complete Pansy file bytes.</returns>
	public byte[] Generate(int romSize, uint romCrc32 = 0, bool compress = true) {
		var sections = new List<SectionData>();

		// Generate code/data map section
		var codeDataMap = GenerateCodeDataMap(romSize);
		sections.Add(new SectionData { Type = SECTION_CODE_DATA_MAP, Data = codeDataMap });

		// Generate symbols section
		var symbols = GenerateSymbols();
		if (symbols.Length > 0) {
			sections.Add(new SectionData { Type = SECTION_SYMBOLS, Data = symbols });
		}

		// Generate memory regions section
		var regions = GenerateMemoryRegions();
		if (regions.Length > 0) {
			sections.Add(new SectionData { Type = SECTION_MEMORY_REGIONS, Data = regions });
		}

		// Generate cross-references section
		var crossRefs = GenerateCrossRefs();
		if (crossRefs.Length > 0) {
			sections.Add(new SectionData { Type = SECTION_CROSS_REFS, Data = crossRefs });
		}

		// Generate source map section if listing is available
		if (_listing is not null) {
			var sourceMap = GenerateSourceMap();
			if (sourceMap.Length > 0) {
				sections.Add(new SectionData { Type = SECTION_SOURCE_MAP, Data = sourceMap });
			}
		}

		// Generate metadata section
		var metadata = GenerateMetadata();
		sections.Add(new SectionData { Type = SECTION_METADATA, Data = metadata });

		// Calculate flags
		var flags = PansyFlags.None;
		if (compress) flags |= PansyFlags.Compressed;
		if (_listing is not null) flags |= PansyFlags.HasSourceMap;
		if (_crossRefs.Count > 0) flags |= PansyFlags.HasCrossRefs;
		if (_cdlGenerator is not null) flags |= PansyFlags.DetailedCdl;

		// Build file
		return BuildFile(sections, romSize, romCrc32, flags, compress);
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
	/// Generates the code/data map section (per-byte flags).
	/// </summary>
	private byte[] GenerateCodeDataMap(int romSize) {
		var map = new byte[romSize];

		// Mark all code segments
		foreach (var segment in _segments) {
			var startAddr = (int)segment.StartAddress;
			var segmentLength = segment.Data.Count;

			for (int i = 0; i < segmentLength && (startAddr + i) < romSize; i++) {
				var romOffset = CpuToRomAddress(startAddr + i);
				if (romOffset >= 0 && romOffset < romSize) {
					map[romOffset] = (byte)ByteFlags.Code;
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
				map[romOffset] = (byte)(ByteFlags.Code | ByteFlags.Opcode);

				// Remaining bytes are operands
				for (int i = 1; i < entry.Bytes.Length; i++) {
					var operandOffset = romOffset + i;
					if (operandOffset >= 0 && operandOffset < romSize) {
						map[operandOffset] = (byte)ByteFlags.Code;
					}
				}
			}
		}

		// Mark symbols
		foreach (var symbol in _symbolTable.Symbols.Values) {
			if (!symbol.IsDefined || !symbol.Value.HasValue) continue;

			var romOffset = CpuToRomAddress((int)symbol.Value.Value);
			if (romOffset < 0 || romOffset >= romSize) continue;

			var flags = (ByteFlags)map[romOffset];

			if (symbol.Type == SymbolType.Label) {
				var name = symbol.Name.ToLowerInvariant();
				if (name.StartsWith("sub_") || name.StartsWith("fn_") || name.StartsWith("func_")) {
					flags |= ByteFlags.SubEntry;
				}
				flags |= ByteFlags.JumpTarget;
			}

			map[romOffset] = (byte)flags;
		}

		// Apply CDL generator data if available
		if (_cdlGenerator is not null) {
			// Mark additional sub entry points and jump targets tracked during code generation
			// These are already transferred via CopyTargetsFrom in the CLI
		}

		return map;
	}

	/// <summary>
	/// Generates the symbols section.
	/// </summary>
	private byte[] GenerateSymbols() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

		foreach (var symbol in _symbolTable.Symbols.Values) {
			if (!symbol.IsDefined || !symbol.Value.HasValue) continue;

			var address = (uint)symbol.Value.Value;
			var bank = (byte)((address >> 16) & 0xff);
			var addr24 = (address & 0xffffff) | ((uint)bank << 24);

			writer.Write(addr24);

			// Determine symbol type
			SymbolEntryType entryType = symbol.Type switch {
				SymbolType.Label => SymbolEntryType.Label,
				SymbolType.Constant => SymbolEntryType.Constant,
				SymbolType.Macro => SymbolEntryType.Macro,
				_ => SymbolEntryType.Label,
			};
			writer.Write((byte)entryType);

			// Flags (unused for now)
			writer.Write((byte)0);

			// Name
			var nameBytes = Encoding.UTF8.GetBytes(symbol.Name);
			writer.Write((ushort)nameBytes.Length);
			writer.Write(nameBytes);

			// Value (for constants)
			if (entryType == SymbolEntryType.Constant) {
				writer.Write((ushort)8); // Value length
				writer.Write(symbol.Value.Value);
			} else {
				writer.Write((ushort)0);
			}
		}

		return ms.ToArray();
	}

	/// <summary>
	/// Generates the memory regions section.
	/// </summary>
	private byte[] GenerateMemoryRegions() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

		int segmentIndex = 0;
		foreach (var segment in _segments) {
			writer.Write((uint)segment.StartAddress);
			writer.Write((uint)(segment.StartAddress + segment.Data.Count - 1));
			writer.Write((byte)1); // Type: code
			writer.Write((byte)((segment.StartAddress >> 16) & 0xff)); // Bank
			writer.Write((ushort)0); // Flags

			// Name the segment by its index and start address
			var segmentName = $"seg_{segmentIndex:d2}_${segment.StartAddress:x6}";
			var nameBytes = Encoding.UTF8.GetBytes(segmentName);
			writer.Write((ushort)nameBytes.Length);
			writer.Write(nameBytes);
			segmentIndex++;
		}

		return ms.ToArray();
	}

	/// <summary>
	/// Generates the cross-references section.
	/// </summary>
	private byte[] GenerateCrossRefs() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

		foreach (var (from, to, type) in _crossRefs) {
			writer.Write(from);
			writer.Write(to);
			writer.Write((byte)type);
		}

		return ms.ToArray();
	}

	/// <summary>
	/// Generates the source map section.
	/// </summary>
	private byte[] GenerateSourceMap() {
		if (_listing is null) return [];

		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

		// Track unique file paths
		var filePaths = new List<string>();
		var fileIndex = new Dictionary<string, ushort>();

		// Gather all unique file paths first
		foreach (var entry in _listing.Entries) {
			var filePath = entry.Location.FilePath;
			if (!string.IsNullOrEmpty(filePath) && !fileIndex.ContainsKey(filePath)) {
				fileIndex[filePath] = (ushort)filePaths.Count;
				filePaths.Add(filePath);
			}
		}

		// Write file count and paths
		writer.Write((ushort)filePaths.Count);
		foreach (var path in filePaths) {
			var pathBytes = Encoding.UTF8.GetBytes(path);
			writer.Write((ushort)pathBytes.Length);
			writer.Write(pathBytes);
		}

		// Write source map entries
		foreach (var entry in _listing.Entries) {
			if (entry.Bytes.Length == 0) continue;
			var filePath = entry.Location.FilePath;
			if (string.IsNullOrEmpty(filePath)) continue;

			var romOffset = CpuToRomAddress((int)entry.Address);
			if (romOffset < 0) continue;

			writer.Write((uint)romOffset);
			writer.Write(fileIndex[filePath]);
			writer.Write((ushort)entry.Location.Line);
			writer.Write((ushort)entry.Location.Column); // Now we have column info
		}

		return ms.ToArray();
	}

	/// <summary>
	/// Generates the metadata section.
	/// </summary>
	private byte[] GenerateMetadata() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

		// Project name
		var nameBytes = Encoding.UTF8.GetBytes(ProjectName);
		writer.Write((ushort)nameBytes.Length);
		writer.Write(nameBytes);

		// Author
		var authorBytes = Encoding.UTF8.GetBytes(Author);
		writer.Write((ushort)authorBytes.Length);
		writer.Write(authorBytes);

		// Version
		var versionBytes = Encoding.UTF8.GetBytes(Version);
		writer.Write((ushort)versionBytes.Length);
		writer.Write(versionBytes);

		// Timestamps
		writer.Write(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // Created
		writer.Write(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // Modified

		return ms.ToArray();
	}

	/// <summary>
	/// Builds the complete Pansy file.
	/// </summary>
	private byte[] BuildFile(List<SectionData> sections, int romSize, uint romCrc32, PansyFlags flags, bool compress) {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

		// Write header
		writer.Write(Magic);
		writer.Write(FormatVersion);
		writer.Write((ushort)flags);
		writer.Write(GetPlatformId());
		writer.Write((byte)0); // Reserved
		writer.Write((byte)0); // Reserved
		writer.Write((byte)0); // Reserved
		writer.Write((uint)romSize);
		writer.Write(romCrc32);
		writer.Write((uint)sections.Count);
		writer.Write((uint)0); // Reserved

		// Calculate section table offset
		var sectionTableOffset = ms.Position;

		// Write placeholder section table
		foreach (var section in sections) {
			writer.Write((uint)0); // Type (placeholder)
			writer.Write((uint)0); // Offset (placeholder)
			writer.Write((uint)0); // Compressed size (placeholder)
			writer.Write((uint)0); // Uncompressed size (placeholder)
		}

		// Write sections and update table
		var sectionOffsets = new List<(long Offset, uint CompressedSize, uint UncompressedSize)>();

		foreach (var section in sections) {
			var offset = ms.Position;
			var uncompressedSize = (uint)section.Data.Length;

			byte[] outputData;
			if (compress && section.Data.Length > 64) {
				// Use DeflateStream for compression (zstd would require external library)
				using var compressedMs = new MemoryStream();
				using (var deflate = new DeflateStream(compressedMs, CompressionLevel.Optimal, leaveOpen: true)) {
					deflate.Write(section.Data, 0, section.Data.Length);
				}
				outputData = compressedMs.ToArray();

				// Only use compressed if smaller
				if (outputData.Length >= section.Data.Length) {
					outputData = section.Data;
				}
			} else {
				outputData = section.Data;
			}

			writer.Write(outputData);
			sectionOffsets.Add((offset, (uint)outputData.Length, uncompressedSize));
		}

		// Go back and fill in section table
		var currentPos = ms.Position;
		ms.Position = sectionTableOffset;

		for (int i = 0; i < sections.Count; i++) {
			writer.Write(sections[i].Type);
			writer.Write((uint)sectionOffsets[i].Offset);
			writer.Write(sectionOffsets[i].CompressedSize);
			writer.Write(sectionOffsets[i].UncompressedSize);
		}

		ms.Position = currentPos;
		return ms.ToArray();
	}

	/// <summary>
	/// Gets the platform ID for the target architecture.
	/// </summary>
	private byte GetPlatformId() {
		return _target switch {
			TargetArchitecture.MOS6502 => PLATFORM_NES,
			TargetArchitecture.WDC65816 => PLATFORM_SNES,
			TargetArchitecture.SM83 => PLATFORM_GB,
			TargetArchitecture.Z80 => PLATFORM_SMS, // Or GB/SMS based on context
			TargetArchitecture.M68000 => PLATFORM_GENESIS,
			TargetArchitecture.SPC700 => PLATFORM_SPC700,
			_ => 0, // Unknown
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
