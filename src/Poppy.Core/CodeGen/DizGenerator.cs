// ============================================================================
// DizGenerator.cs - DiztinGUIsh Project File Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Poppy.Core.Semantics;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Generates DiztinGUIsh (.diz) project files for use with the DiztinGUIsh disassembler.
/// This provides rich symbol, label, and code/data information for SNES ROM analysis.
/// </summary>
public sealed class DizGenerator {
	private readonly SymbolTable _symbolTable;
	private readonly TargetArchitecture _target;
	private readonly IReadOnlyList<OutputSegment> _segments;
	private readonly ListingGenerator? _listing;
	private readonly string _projectName;

	/// <summary>
	/// Data types recognized by DiztinGUIsh.
	/// </summary>
	public enum DizDataType {
		/// <summary>Byte has not been accessed during emulation.</summary>
		Unreached = 0,
		/// <summary>Byte is an opcode (instruction start).</summary>
		Opcode = 1,
		/// <summary>Byte is an operand (part of instruction).</summary>
		Operand = 2,
		/// <summary>8-bit data value.</summary>
		Data8 = 3,
		/// <summary>Graphics/tile data.</summary>
		Graphics = 4,
		/// <summary>Music/audio data.</summary>
		Music = 5,
		/// <summary>Empty/filler byte.</summary>
		Empty = 6,
		/// <summary>16-bit data value.</summary>
		Data16 = 7,
		/// <summary>16-bit pointer.</summary>
		Pointer16 = 8,
		/// <summary>24-bit data value.</summary>
		Data24 = 9,
		/// <summary>24-bit pointer.</summary>
		Pointer24 = 10,
		/// <summary>32-bit data value.</summary>
		Data32 = 11,
		/// <summary>32-bit pointer.</summary>
		Pointer32 = 12,
		/// <summary>Text/string data.</summary>
		Text = 13
	}

	/// <summary>
	/// Creates a new DIZ generator.
	/// </summary>
	/// <param name="symbolTable">The symbol table from compilation.</param>
	/// <param name="target">The target architecture.</param>
	/// <param name="segments">The output segments from code generation.</param>
	/// <param name="projectName">The project name for the DIZ file.</param>
	/// <param name="listing">Optional listing generator for detailed tracking.</param>
	public DizGenerator(
		SymbolTable symbolTable,
		TargetArchitecture target,
		IReadOnlyList<OutputSegment> segments,
		string projectName,
		ListingGenerator? listing = null) {
		_symbolTable = symbolTable;
		_target = target;
		_segments = segments;
		_projectName = projectName;
		_listing = listing;
	}

	/// <summary>
	/// Generates DIZ project data.
	/// </summary>
	/// <param name="romData">The ROM binary data.</param>
	/// <returns>The JSON representation of the DIZ project.</returns>
	public string Generate(byte[] romData) {
		var project = new Dictionary<string, object> {
			["ProjectName"] = _projectName,
			["RomMapMode"] = GetMapModeString(),
			["RomSpeed"] = GetRomSpeedString(),
			["Version"] = "3.0",
			["Generator"] = "Poppy Compiler"
		};

		// Generate labels dictionary
		var labels = new Dictionary<string, Dictionary<string, object>>();
		foreach (var symbol in _symbolTable.Symbols.Values) {
			if (!symbol.IsDefined || !symbol.Value.HasValue) continue;
			if (symbol.Type == SymbolType.Macro) continue;  // Skip macros

			var address = symbol.Value.Value;
			var labelData = new Dictionary<string, object> {
				["Name"] = symbol.Name,
				["Comment"] = GetSymbolComment(symbol),
				["DataType"] = (int)GetDataTypeForSymbol(symbol)
			};

			labels[address.ToString()] = labelData;
		}
		project["Labels"] = labels;

		// Generate data type array (per-byte marking)
		var dataTypes = new int[romData.Length];
		Array.Fill(dataTypes, (int)DizDataType.Unreached);

		// Mark code regions from segments
		foreach (var segment in _segments) {
			var startAddr = (int)segment.StartAddress;
			var romOffset = SnesAddressToRomOffset(startAddr);

			for (int i = 0; i < segment.Data.Count; i++) {
				var offset = romOffset + i;
				if (offset >= 0 && offset < dataTypes.Length) {
					// Default to opcode, will be refined below
					dataTypes[offset] = (int)DizDataType.Opcode;
				}
			}
		}

		// Use listing for more precise opcode/operand distinction
		if (_listing is not null) {
			foreach (var entry in _listing.Entries) {
				if (entry.Bytes.Length == 0) continue;

				var address = (int)entry.Address;
				var romOffset = SnesAddressToRomOffset(address);
				if (romOffset < 0 || romOffset >= dataTypes.Length) continue;

				// First byte is opcode
				dataTypes[romOffset] = (int)DizDataType.Opcode;

				// Remaining bytes are operands
				for (int i = 1; i < entry.Bytes.Length; i++) {
					var operandOffset = romOffset + i;
					if (operandOffset >= 0 && operandOffset < dataTypes.Length) {
						dataTypes[operandOffset] = (int)DizDataType.Operand;
					}
				}
			}
		}

		project["DataTypes"] = dataTypes;

		// Generate ROM bytes checksum info
		project["RomChecksum"] = CalculateChecksum(romData);
		project["RomSize"] = romData.Length;

		// Serialize to JSON
		var options = new JsonSerializerOptions {
			WriteIndented = true,
			PropertyNamingPolicy = null  // Keep PascalCase
		};

		return JsonSerializer.Serialize(project, options);
	}

	/// <summary>
	/// Exports to a .diz file (gzip compressed JSON).
	/// </summary>
	/// <param name="path">Output file path.</param>
	/// <param name="romData">The ROM binary data.</param>
	/// <param name="compress">Whether to gzip compress the output (default true).</param>
	public void Export(string path, byte[] romData, bool compress = true) {
		var json = Generate(romData);
		var jsonBytes = Encoding.UTF8.GetBytes(json);

		if (compress) {
			using var output = File.Create(path);
			using var gzip = new GZipStream(output, CompressionLevel.Optimal);
			gzip.Write(jsonBytes, 0, jsonBytes.Length);
		} else {
			File.WriteAllBytes(path, jsonBytes);
		}
	}

	/// <summary>
	/// Gets the SNES map mode string.
	/// </summary>
	private string GetMapModeString() {
		return _target switch {
			TargetArchitecture.WDC65816 => "LoRom",  // Default to LoROM
			_ => "Unknown"
		};
	}

	/// <summary>
	/// Gets the ROM speed string.
	/// </summary>
	private string GetRomSpeedString() {
		return "SlowRom";  // Default, can be made configurable
	}

	/// <summary>
	/// Gets a comment for a symbol.
	/// </summary>
	private static string GetSymbolComment(Symbol symbol) {
		return symbol.Type switch {
			SymbolType.Label => "Label",
			SymbolType.Constant => "Constant",
			SymbolType.External => "External",
			_ => ""
		};
	}

	/// <summary>
	/// Determines the DIZ data type for a symbol.
	/// </summary>
	private static DizDataType GetDataTypeForSymbol(Symbol symbol) {
		// Labels at code locations are opcodes
		if (symbol.Type == SymbolType.Label)
			return DizDataType.Opcode;

		// Constants could be data
		if (symbol.Type == SymbolType.Constant)
			return DizDataType.Data8;

		return DizDataType.Unreached;
	}

	/// <summary>
	/// Converts SNES CPU address to ROM file offset.
	/// </summary>
	private int SnesAddressToRomOffset(int address) {
		// LoROM mapping (simplified)
		var bank = (address >> 16) & 0xff;
		var offset = address & 0xffff;

		if (offset >= 0x8000) {
			return ((bank & 0x7f) * 0x8000) + (offset - 0x8000);
		}

		// For NES/GB, simpler mapping
		if (_target == TargetArchitecture.MOS6502) {
			if (address >= 0x8000 && address < 0x10000) {
				return address - 0x8000;  // No header in DIZ
			}
		}

		if (_target == TargetArchitecture.SM83) {
			if (address >= 0 && address < 0x8000) {
				return address;
			}
		}

		return address;
	}

	/// <summary>
	/// Calculates a simple checksum for the ROM.
	/// </summary>
	private static int CalculateChecksum(byte[] data) {
		int sum = 0;
		foreach (var b in data) {
			sum = (sum + b) & 0xffff;
		}
		return sum;
	}
}
