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
