﻿namespace Poppy.Core.Arch.Profiles;

using System.Collections.Frozen;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// WDC 65816 target profile (SNES).
/// </summary>
internal sealed class Wdc65816Profile : ITargetProfile {
	public static readonly Wdc65816Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.WDC65816;
	public IInstructionEncoder Encoder { get; } = new Wdc65816Encoder();
	public int LongDirectiveSize => 3;

	// Bank size depends on map mode; default LoROM = 32KB, HiROM = 64KB
	// CodeGenerator overrides this based on .snesheader map mode
	public int DefaultBankSize => 0x8000; // LoROM default

	public long GetBankCpuBase(int bank) => 0x8000;
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => null; // TODO: Phase 2

	private sealed class Wdc65816Encoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSet65816.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSet65816.TryGetEncoding(mnemonic, mode, out var enc)) {
				encoding = new EncodedInstruction(enc.Opcode, enc.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSet65816.IsBranchInstruction(mnemonic);
	}
}
