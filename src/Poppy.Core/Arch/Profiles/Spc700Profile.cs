namespace Poppy.Core.Arch.Profiles;

using System.Collections.Frozen;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// Sony SPC700 target profile (SNES Audio).
/// </summary>
internal sealed class Spc700Profile : ITargetProfile {
	public static readonly Spc700Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.SPC700;
	public IInstructionEncoder Encoder { get; } = new Spc700Encoder();
	public int DefaultBankSize => 0x4000; // Default
	public long GetBankCpuBase(int bank) => -1;
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => null; // TODO: Phase 2

	private sealed class Spc700Encoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSetSPC700.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			// SPC700 is dispatched through InstructionSetSPC700 but isn't in the
			// main TryGetInstructionEncoding chain currently.
			// This is a placeholder for future integration.
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetSPC700.IsBranchInstruction(mnemonic);
	}
}
