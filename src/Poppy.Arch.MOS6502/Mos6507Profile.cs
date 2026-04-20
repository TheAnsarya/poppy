﻿namespace Poppy.Arch.MOS6502;

using System.Collections.Frozen;
using Poppy.Core.Arch;
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

	/// <inheritdoc />
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new Mos6507RomBuilderAdapter();

	private sealed class Mos6507RomBuilderAdapter : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			var romBuilder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.None);
			foreach (var segment in segments) {
				romBuilder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}
			return romBuilder.Build();
		}
	}

	private sealed class Mos6507Encoder : IInstructionEncoder {
		private static readonly FrozenSet<string> s_mnemonics = InstructionSet6507.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);
		public IReadOnlySet<string> Mnemonics => s_mnemonics;

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
