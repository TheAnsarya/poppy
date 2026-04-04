namespace Poppy.Core.Arch.Profiles;

using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// ARM7TDMI target profile (Game Boy Advance).
/// </summary>
internal sealed class Arm7tdmiProfile : ITargetProfile {
	public static readonly Arm7tdmiProfile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.ARM7TDMI;
	public IInstructionEncoder Encoder { get; } = new Arm7tdmiEncoder();
	public int DefaultBankSize => 0x4000; // Default
	public long GetBankCpuBase(int bank) => -1;
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => null; // TODO: Phase 2

	private sealed class Arm7tdmiEncoder : IInstructionEncoder {
		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			// ARM7TDMI uses a different encoding model; TryGetInstructionEncoding
			// is not dispatched through the shared pipeline currently.
			// This is a placeholder for future integration.
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetARM7TDMI.IsBranchInstruction(mnemonic);
	}
}
