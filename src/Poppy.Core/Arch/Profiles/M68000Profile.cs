﻿namespace Poppy.Core.Arch.Profiles;

using System.Collections.Frozen;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// Motorola 68000 target profile (Sega Genesis / Mega Drive).
/// </summary>
internal sealed class M68000Profile : ITargetProfile {
	public static readonly M68000Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.M68000;
	public IInstructionEncoder Encoder { get; } = new M68000Encoder();
	public int DefaultBankSize => 0x4000; // Default
	public long GetBankCpuBase(int bank) => -1;
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => null; // TODO: Phase 2

	private sealed class M68000Encoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSetM68000.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSetM68000.TryGetEncodingFromShared(mnemonic, mode, out var opcode, out var size)) {
				encoding = new EncodedInstruction(opcode, size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetM68000.IsBranchInstruction(mnemonic);
	}
}
