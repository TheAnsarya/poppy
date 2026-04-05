namespace Poppy.Core.Arch.Profiles;

using System.Collections.Frozen;
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

	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => null; // TODO: Phase 2

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
