namespace Poppy.Arch.WDC65816;

using System.Collections.Frozen;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// WDC 65816 target profile (SNES).
/// </summary>
internal sealed class Wdc65816Profile : ITargetProfile {
	public static readonly Wdc65816Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.WDC65816;
	public IInstructionEncoder Encoder { get; } = new Wdc65816Encoder();
	public int LongDirectiveSize => 3;

	// Bank size depends on map mode; default LoROM = 32KB, HiROM = 64KB
	// CodeGenerator overrides this based on .snesheader map mode
	public int DefaultBankSize => 0x8000; // LoROM default

	public long GetBankCpuBase(int bank) => 0x8000;

	/// <inheritdoc />
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new Wdc65816RomBuilderAdapter(analyzer);

	/// <inheritdoc />
	public int MapCpuToRomOffset(int cpuAddress) {
		// LoROM mapping (simplified)
		var bank = (cpuAddress >> 16) & 0xff;
		var offset = cpuAddress & 0xffff;
		if (offset >= 0x8000) {
			return ((bank & 0x7f) * 0x8000) + (offset - 0x8000);
		}
		return -1;
	}

	/// <inheritdoc />
	public string GetMemoryRegionName(long address) => address switch {
		< 0x2000 => "WRAM",
		_ => "PRG"
	};

	/// <inheritdoc />
	public int GetBankSize(SemanticAnalyzer analyzer) {
		var mapping = analyzer.MemoryMapping;
		if (mapping is not null && mapping.Equals("hirom", StringComparison.OrdinalIgnoreCase))
			return 0x10000;
		return DefaultBankSize; // 0x8000 for LoROM
	}

	/// <inheritdoc />
	public int GetOperandSize(string mnemonic, AddressingMode mode, int encodingSize,
		bool accumulatorIs16Bit, bool indexIs16Bit) {
		if (mode == AddressingMode.Immediate) {
			var lower = mnemonic.ToLowerInvariant();

			// Index register instructions use X flag
			if (lower is "ldx" or "ldy" or "cpx" or "cpy")
				return indexIs16Bit ? 2 : 1;

			// Accumulator instructions use M flag
			if (lower is "lda" or "adc" or "sbc" or "cmp" or "and" or "ora" or "eor" or "bit")
				return accumulatorIs16Bit ? 2 : 1;

			// REP/SEP are always 8-bit immediate
			if (lower is "rep" or "sep")
				return 1;

			// PEA is always 16-bit immediate
			if (lower is "pea")
				return 2;

			// Default to current accumulator size for other immediate instructions
			return accumulatorIs16Bit ? 2 : 1;
		}

		return encodingSize - 1;
	}

	/// <inheritdoc />
	public (bool AccumulatorIs16Bit, bool IndexIs16Bit) UpdateProcessorFlags(
		string mnemonic, long? operandValue, bool accumulatorIs16Bit, bool indexIs16Bit) {
		if (mnemonic.Equals("rep", StringComparison.OrdinalIgnoreCase) && operandValue.HasValue) {
			// REP clears flags (sets to 16-bit mode)
			if ((operandValue.Value & 0x20) != 0) accumulatorIs16Bit = true;  // M flag
			if ((operandValue.Value & 0x10) != 0) indexIs16Bit = true;        // X flag
		} else if (mnemonic.Equals("sep", StringComparison.OrdinalIgnoreCase) && operandValue.HasValue) {
			// SEP sets flags (sets to 8-bit mode)
			if ((operandValue.Value & 0x20) != 0) accumulatorIs16Bit = false; // M flag
			if ((operandValue.Value & 0x10) != 0) indexIs16Bit = false;       // X flag
		}
		return (accumulatorIs16Bit, indexIs16Bit);
	}

	/// <inheritdoc />
	public IReadOnlyList<(string Name, long StartAddress, long MaxSize, SegmentType Type)> GetDefaultSegments() => [
		("ZEROPAGE", 0x0000, 0x0100, SegmentType.ZeroPage),
		("RAM", 0x7e0000, 0x020000, SegmentType.Ram),
		("CODE", 0x008000, 0x008000, SegmentType.Code),
	];

	/// <inheritdoc />
	public bool TryHandleDirective(DirectiveNode node, SemanticAnalyzer analyzer) {
		var directiveName = node.Name.ToLowerInvariant();

		switch (directiveName) {
			case "snes_title":
			case "snes_region":
			case "snes_version":
			case "snes_rom_size":
			case "snes_ram_size":
			case "snes_fastrom":
				return HandleSnesDirective(node, analyzer, directiveName);

			default:
				return false;
		}
	}

	private static bool HandleSnesDirective(DirectiveNode node, SemanticAnalyzer analyzer, string directiveName) {
		if (analyzer.Pass != 1) return true;

		// Get the value from the first argument (if any)
		long? value = null;
		string? stringValue = null;

		if (node.Arguments.Count > 0) {
			if (node.Arguments[0] is StringLiteralNode stringLit) {
				stringValue = stringLit.Value;
			} else {
				value = analyzer.EvaluateExpression(node.Arguments[0]);
				if (value is null) {
					analyzer.AddError($".{directiveName} directive requires a constant value", node.Location);
					return true;
				}
			}
		}

		switch (directiveName) {
			case "snes_title":
				if (stringValue is null) {
					analyzer.AddError(".snes_title directive requires a string value (up to 21 characters)", node.Location);
					return true;
				}
				if (stringValue.Length > 21) {
					analyzer.AddError($".snes_title is too long ({stringValue.Length} characters, maximum is 21)", node.Location);
					return true;
				}
				analyzer.SnesTitle = stringValue;
				break;

			case "snes_region":
				if (stringValue is null) {
					analyzer.AddError(".snes_region directive requires a region string (e.g., \"Japan\", \"USA\", \"Europe\")", node.Location);
					return true;
				}
				var validRegions = new[] { "Japan", "USA", "Europe", "Scandinavia", "France",
					"Netherlands", "Spain", "Germany", "Italy", "China", "Korea", "Canada", "Brazil", "Australia" };
				if (!validRegions.Contains(stringValue, StringComparer.OrdinalIgnoreCase)) {
					analyzer.AddError($".snes_region \"{stringValue}\" is not valid. Valid regions: {string.Join(", ", validRegions)}", node.Location);
					return true;
				}
				analyzer.SnesRegion = stringValue;
				break;

			case "snes_version":
				if (value is null) {
					analyzer.AddError(".snes_version directive requires a version number (0-255)", node.Location);
					return true;
				}
				if (value < 0 || value > 255) {
					analyzer.AddError($".snes_version must be 0-255 (got {value})", node.Location);
					return true;
				}
				analyzer.SnesVersion = (int)value;
				break;

			case "snes_rom_size":
				if (value is null) {
					analyzer.AddError(".snes_rom_size directive requires a ROM size in KB (power of 2, e.g., 256, 512, 1024)", node.Location);
					return true;
				}
				if (value <= 0 || (value & (value - 1)) != 0) {
					analyzer.AddError($".snes_rom_size must be a power of 2 (got {value})", node.Location);
					return true;
				}
				analyzer.SnesRomSizeKb = (int)value;
				break;

			case "snes_ram_size":
				if (value is null) {
					analyzer.AddError(".snes_ram_size directive requires a RAM size in KB (0, 2, 8, or 32)", node.Location);
					return true;
				}
				var validRamSizes = new[] { 0, 2, 8, 32 };
				if (!validRamSizes.Contains((int)value)) {
					analyzer.AddError($".snes_ram_size must be 0, 2, 8, or 32 KB (got {value})", node.Location);
					return true;
				}
				analyzer.SnesRamSizeKb = (int)value;
				break;

			case "snes_fastrom":
				analyzer.SnesFastRom = value is null || value != 0;
				break;
		}

		return true;
	}

	private sealed class Wdc65816RomBuilderAdapter(SemanticAnalyzer analyzer) : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			var headerBuilder = analyzer.GetSnesHeaderBuilder();
			if (headerBuilder is null) {
				return flatBinary;
			}

			var header = headerBuilder.Build();
			var mapMode = GetMapMode(analyzer.MemoryMapping);

			var romBuilder = new SnesRomBuilder(mapMode, header);
			foreach (var segment in segments) {
				romBuilder.AddSegment(segment.StartAddress, segment.Data.ToArray());
			}
			return romBuilder.Build();
		}

		private static SnesMapMode GetMapMode(string? mapping) {
			if (mapping is null)
				return SnesMapMode.LoRom;
			if (mapping.Equals("lorom", StringComparison.OrdinalIgnoreCase))
				return SnesMapMode.LoRom;
			if (mapping.Equals("hirom", StringComparison.OrdinalIgnoreCase))
				return SnesMapMode.HiRom;
			if (mapping.Equals("exhirom", StringComparison.OrdinalIgnoreCase))
				return SnesMapMode.ExHiRom;
			return SnesMapMode.LoRom;
		}
	}

	private sealed class Wdc65816Encoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSet65816.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSet65816.TryGetEncoding(mnemonic, mode, out var enc)) {
				encoding = new EncodedInstruction(enc.Opcode, enc.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSet65816.IsBranchInstruction(mnemonic);
	}
}
