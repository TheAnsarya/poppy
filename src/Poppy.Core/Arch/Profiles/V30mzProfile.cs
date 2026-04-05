namespace Poppy.Core.Arch.Profiles;

using System.Collections.Frozen;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// NEC V30MZ target profile (WonderSwan).
/// </summary>
internal sealed class V30mzProfile : ITargetProfile {
	public static readonly V30mzProfile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.V30MZ;
	public IInstructionEncoder Encoder { get; } = new V30mzEncoder();
	public int DefaultBankSize => 0x4000; // Default
	public long GetBankCpuBase(int bank) => -1;
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => null; // TODO: Phase 2

	private sealed class V30mzEncoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSetV30MZ.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSetV30MZ.TryGetEncodingFromShared(mnemonic, mode, out var opcode, out var size)) {
				encoding = new EncodedInstruction(opcode, size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetV30MZ.IsBranchInstruction(mnemonic);
	}
}
