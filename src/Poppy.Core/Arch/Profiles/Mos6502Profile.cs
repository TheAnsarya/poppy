namespace Poppy.Core.Arch.Profiles;

using System.Collections.Frozen;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// MOS 6502 target profile (NES).
/// </summary>
internal sealed class Mos6502Profile : ITargetProfile {
	public static readonly Mos6502Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.MOS6502;
	public IInstructionEncoder Encoder { get; } = new Mos6502Encoder();
	public int DefaultBankSize => 0x4000; // 16KB NES PRG bank
	public long GetBankCpuBase(int bank) => 0x8000;
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => null; // TODO: Phase 2

	private sealed class Mos6502Encoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSet6502.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSet6502.TryGetEncoding(mnemonic, mode, out var enc)) {
				encoding = new EncodedInstruction(enc.Opcode, enc.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			mnemonic.Equals("bcc", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("bcs", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("beq", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("bmi", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("bne", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("bpl", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("bvc", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("bvs", StringComparison.OrdinalIgnoreCase);
	}
}
