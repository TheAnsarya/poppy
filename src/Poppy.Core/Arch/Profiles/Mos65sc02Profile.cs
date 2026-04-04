namespace Poppy.Core.Arch.Profiles;

using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// MOS 65SC02 target profile (Atari Lynx).
/// </summary>
internal sealed class Mos65sc02Profile : ITargetProfile {
	public static readonly Mos65sc02Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.MOS65SC02;
	public IInstructionEncoder Encoder { get; } = new Mos65sc02Encoder();
	public int DefaultBankSize => 0x4000; // 16KB default for Lynx

	public long GetBankCpuBase(int bank) => -1; // Lynx doesn't have standard banking

	public AddressingMode? AdjustAddressingMode(string mnemonic, AddressingMode mode) {
		// 65SC02 INC/DEC with implied → Accumulator
		if (mode == AddressingMode.Implied &&
			(mnemonic.Equals("inc", StringComparison.OrdinalIgnoreCase) ||
			 mnemonic.Equals("dec", StringComparison.OrdinalIgnoreCase))) {
			return AddressingMode.Accumulator;
		}
		return null;
	}

	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => null; // TODO: Phase 2

	private sealed class Mos65sc02Encoder : IInstructionEncoder {
		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSet65SC02.TryGetEncoding(mnemonic, mode, out var enc)) {
				encoding = new EncodedInstruction(enc.Opcode, enc.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSet65SC02.IsBranchInstruction(mnemonic);
	}
}
