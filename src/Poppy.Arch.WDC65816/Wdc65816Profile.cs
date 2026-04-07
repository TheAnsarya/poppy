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
