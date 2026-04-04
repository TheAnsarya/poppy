namespace Poppy.Core.Arch.Profiles;

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
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => null; // TODO: Phase 2

	private sealed class Huc6280Encoder : IInstructionEncoder {
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
