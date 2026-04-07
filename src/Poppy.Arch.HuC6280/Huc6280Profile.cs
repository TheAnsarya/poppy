namespace Poppy.Arch.HuC6280;

using System.Collections.Frozen;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// Hudson HuC6280 target profile (TurboGrafx-16 / PC Engine).
/// </summary>
internal sealed class Huc6280Profile : ITargetProfile {
	public static readonly Huc6280Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.HuC6280;
	public IInstructionEncoder Encoder { get; } = new Huc6280Encoder();
	public int DefaultBankSize => 0x4000; // 16KB default
	public long GetBankCpuBase(int bank) => -1;

	/// <inheritdoc />
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new Huc6280RomBuilderAdapter();

	private sealed class Huc6280RomBuilderAdapter : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			var builder = new TurboGrafxRomBuilder();
			foreach (var segment in segments) {
				builder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}
			return builder.Build();
		}
	}

	private sealed class Huc6280Encoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSetHuC6280.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSetHuC6280.TryGetEncoding(mnemonic, mode, out var opcode, out var size)) {
				encoding = new EncodedInstruction(opcode, size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetHuC6280.IsBranchInstruction(mnemonic);
	}
}
