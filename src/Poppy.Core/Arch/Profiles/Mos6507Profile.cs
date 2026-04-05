namespace Poppy.Core.Arch.Profiles;

using System.Collections.Frozen;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// MOS 6507 target profile (Atari 2600).
/// </summary>
internal sealed class Mos6507Profile : ITargetProfile {
	public static readonly Mos6507Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.MOS6507;
	public IInstructionEncoder Encoder { get; } = new Mos6507Encoder();
	public int DefaultBankSize => 0x1000; // 4KB Atari 2600 bank
	public long GetBankCpuBase(int bank) => 0xf000;
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => null; // TODO: Phase 2

	private sealed class Mos6507Encoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSet6507.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSet6507.TryGetEncoding(mnemonic, mode, out var enc)) {
				encoding = new EncodedInstruction(enc.Opcode, enc.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		// 6507 uses same branches as 6502
		public bool IsBranchInstruction(string mnemonic) =>
			Mos6502Profile.Instance.Encoder.IsBranchInstruction(mnemonic);
	}
}
