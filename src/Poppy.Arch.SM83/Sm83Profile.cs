namespace Poppy.Arch.SM83;

using System.Collections.Frozen;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// Sharp SM83 target profile (Game Boy / GBC).
/// </summary>
internal sealed class Sm83Profile : ITargetProfile {
	public static readonly Sm83Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.SM83;
	public IInstructionEncoder Encoder { get; } = new Sm83Encoder();
	public int DefaultBankSize => 0x4000; // 16KB Game Boy bank

	public long GetBankCpuBase(int bank) =>
		bank == 0 ? 0x0000 : 0x4000;

	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new Sm83RomBuilderAdapter(analyzer);

	private sealed class Sm83RomBuilderAdapter(SemanticAnalyzer analyzer) : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			var headerBuilder = analyzer.GetGbHeaderBuilder();
			if (headerBuilder is null) {
				return flatBinary;
			}

			var header = headerBuilder.Build();
			var romBuilder = new GbRomBuilder(header);
			foreach (var segment in segments) {
				romBuilder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}
			return romBuilder.Build();
		}
	}

	private sealed class Sm83Encoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSetSM83.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSetSM83.TryGetEncoding(mnemonic, mode, out var enc)) {
				encoding = new EncodedInstruction(enc.Opcode, enc.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetSM83.IsRelativeBranch(mnemonic);
	}
}
