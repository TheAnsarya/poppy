// ============================================================================
// CdlGenerator.cs - Code/Data Log (CDL) File Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text;
using Poppy.Core.Semantics;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Generates CDL (Code/Data Log) files for use with emulators like FCEUX and Mesen.
/// CDL files record which bytes in a ROM are code vs data, improving emulator features
/// like disassembly, and providing roundtrip support with disassemblers like Peony.
/// </summary>
public sealed class CdlGenerator {
	private readonly SymbolTable _symbolTable;
	private readonly TargetArchitecture _target;
	private readonly IReadOnlyList<OutputSegment> _segments;
	private readonly ListingGenerator? _listing;

	// Track jump and call targets from instruction analysis
	private readonly HashSet<long> _jsrTargets = []; // JSR/JSL/CALL targets (subroutine entry points)
	private readonly HashSet<long> _jmpTargets = []; // JMP/JML/BRA/BRL targets (jump targets)

	// CDL Flag definitions (FCEUX format)
	private const byte FCEUX_CODE = 0x01;
	private const byte FCEUX_DATA = 0x02;
	private const byte FCEUX_PCM_AUDIO = 0x40;
	private const byte FCEUX_INDIRECT_CODE = 0x10;   // Sub entry point (accessed indirectly)
	private const byte FCEUX_INDIRECT_DATA = 0x20;   // Indexed data

	// CDL Flag definitions (Mesen format)
	private const byte MESEN_CODE = 0x01;
	private const byte MESEN_DATA = 0x02;
	private const byte MESEN_JUMP_TARGET = 0x04;
	private const byte MESEN_SUB_ENTRY_POINT = 0x08;
	private const byte MESEN_DRAWN = 0x10;
	private const byte MESEN_READ = 0x20;

	/// <summary>
	/// Supported CDL output formats.
	/// </summary>
	public enum CdlFormat {
		/// <summary>FCEUX CDL format (raw bytes, no header)</summary>
		FCEUX,
		/// <summary>Mesen CDL format ("CDL\x01" header + bytes)</summary>
		Mesen
	}

	/// <summary>
	/// Creates a new CDL generator.
	/// </summary>
	/// <param name="symbolTable">The symbol table from compilation.</param>
	/// <param name="target">The target architecture.</param>
	/// <param name="segments">The output segments from code generation.</param>
	/// <param name="listing">Optional listing generator for detailed tracking.</param>
	public CdlGenerator(
		SymbolTable symbolTable,
		TargetArchitecture target,
		IReadOnlyList<OutputSegment> segments,
		ListingGenerator? listing = null) {
		_symbolTable = symbolTable;
		_target = target;
		_segments = segments;
		_listing = listing;
	}

	/// <summary>
	/// Generates CDL data for the ROM.
	/// </summary>
	/// <param name="romSize">The total ROM size in bytes.</param>
	/// <param name="format">The CDL format to generate.</param>
	/// <returns>The CDL file bytes.</returns>
	public byte[] Generate(int romSize, CdlFormat format = CdlFormat.Mesen) {
		var cdl = new byte[romSize];

		// Mark all code segments
		foreach (var segment in _segments) {
			var startAddr = (int)segment.StartAddress;
			var segmentLength = segment.Data.Count;

			for (int i = 0; i < segmentLength && (startAddr + i) < romSize; i++) {
				var romOffset = startAddr + i;
				if (romOffset >= 0 && romOffset < romSize) {
					cdl[romOffset] = format == CdlFormat.Mesen ? MESEN_CODE : FCEUX_CODE;
				}
			}
		}

		// Mark symbols - distinguish between code labels and data labels
		foreach (var symbol in _symbolTable.Symbols.Values) {
			if (!symbol.IsDefined || !symbol.Value.HasValue) continue;

			var address = (int)symbol.Value.Value;
			if (address < 0 || address >= romSize) continue;

			// Map CPU addresses to ROM addresses for NES
			var romOffset = CpuToRomAddress(address);
			if (romOffset < 0 || romOffset >= romSize) continue;

			var flags = cdl[romOffset];

			if (format == CdlFormat.Mesen) {
				// Mark jump targets and sub entry points
				if (symbol.Type == SymbolType.Label) {
					// Labels that start with common subroutine prefixes
					var name = symbol.Name.ToLowerInvariant();
					if (name.StartsWith("sub_") || name.StartsWith("fn_") || name.StartsWith("func_")) {
						flags |= MESEN_SUB_ENTRY_POINT;
					}
					flags |= MESEN_JUMP_TARGET;
				}
			} else {
				// FCEUX format
				if (symbol.Type == SymbolType.Label) {
					var name = symbol.Name.ToLowerInvariant();
					if (name.StartsWith("sub_") || name.StartsWith("fn_") || name.StartsWith("func_")) {
						flags |= FCEUX_INDIRECT_CODE;
					}
				}
			}

			cdl[romOffset] = flags;
		}

		// Mark tracked JSR targets as subroutine entry points
		foreach (var targetAddress in _jsrTargets) {
			var romOffset = CpuToRomAddress((int)targetAddress);
			if (romOffset >= 0 && romOffset < romSize) {
				var flags = cdl[romOffset];
				if (format == CdlFormat.Mesen) {
					flags |= MESEN_SUB_ENTRY_POINT;
				} else {
					flags |= FCEUX_INDIRECT_CODE;
				}
				cdl[romOffset] = flags;
			}
		}

		// Mark tracked JMP targets as jump targets
		foreach (var targetAddress in _jmpTargets) {
			var romOffset = CpuToRomAddress((int)targetAddress);
			if (romOffset >= 0 && romOffset < romSize) {
				var flags = cdl[romOffset];
				if (format == CdlFormat.Mesen) {
					flags |= MESEN_JUMP_TARGET;
				}
				cdl[romOffset] = flags;
			}
		}

		// Use listing data if available for more precise tracking
		if (_listing is not null) {
			foreach (var entry in _listing.Entries) {
				if (entry.Bytes.Length == 0) continue;

				var address = (int)entry.Address;
				var romOffset = CpuToRomAddress(address);
				if (romOffset < 0 || romOffset >= romSize) continue;

				// First byte is the opcode (code)
				cdl[romOffset] = format == CdlFormat.Mesen ? MESEN_CODE : FCEUX_CODE;

				// Remaining bytes are operands (still code, but we could distinguish)
				for (int i = 1; i < entry.Bytes.Length; i++) {
					var operandOffset = romOffset + i;
					if (operandOffset >= 0 && operandOffset < romSize) {
						cdl[operandOffset] = format == CdlFormat.Mesen ? MESEN_CODE : FCEUX_CODE;
					}
				}
			}
		}

		// Build final output based on format
		if (format == CdlFormat.Mesen) {
			// Mesen format: "CDL\x01" header + data
			var header = "CDL\x01"u8.ToArray();
			var output = new byte[header.Length + cdl.Length];
			Array.Copy(header, 0, output, 0, header.Length);
			Array.Copy(cdl, 0, output, header.Length, cdl.Length);
			return output;
		}

		// FCEUX format: raw bytes
		return cdl;
	}

	/// <summary>
	/// Maps CPU address to ROM file offset.
	/// </summary>
	private int CpuToRomAddress(int cpuAddress) {
		// NES: PRG ROM starts at $8000, typically maps to offset 0x10 (after iNES header)
		if (_target == TargetArchitecture.MOS6502) {
			if (cpuAddress >= 0x8000) {
				return cpuAddress - 0x8000 + 0x10;  // 16-byte iNES header
			}
			return -1;  // Not PRG ROM
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

	/// <summary>
	/// Exports CDL to a file.
	/// </summary>
	/// <param name="path">Output file path.</param>
	/// <param name="romSize">Total ROM size in bytes.</param>
	/// <param name="format">CDL format to use (auto-detected from extension if null).</param>
	public void Export(string path, int romSize, CdlFormat? format = null) {
		// Auto-detect format from filename
		var actualFormat = format ?? DetectFormatFromPath(path);
		var cdl = Generate(romSize, actualFormat);
		File.WriteAllBytes(path, cdl);
	}

	/// <summary>
	/// Detects CDL format from file path.
	/// </summary>
	private static CdlFormat DetectFormatFromPath(string path) {
		var filename = Path.GetFileName(path).ToLowerInvariant();

		// Check for emulator-specific naming patterns
		if (filename.Contains("fceux") || filename.Contains("fce"))
			return CdlFormat.FCEUX;

		if (filename.Contains("mesen"))
			return CdlFormat.Mesen;

		// Default to Mesen format (more modern, includes header)
		return CdlFormat.Mesen;
	}

	/// <summary>
	/// Registers a JSR/JSL/CALL target address as a subroutine entry point.
	/// Call this during code generation when emitting JSR-type instructions.
	/// </summary>
	/// <param name="targetAddress">The CPU address being called.</param>
	public void RegisterSubroutineEntry(long targetAddress) {
		_jsrTargets.Add(targetAddress);
	}

	/// <summary>
	/// Registers a JMP/JML/BRA/BRL target address as a jump target.
	/// Call this during code generation when emitting JMP-type instructions.
	/// </summary>
	/// <param name="targetAddress">The CPU address being jumped to.</param>
	public void RegisterJumpTarget(long targetAddress) {
		_jmpTargets.Add(targetAddress);
	}
}
