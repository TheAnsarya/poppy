﻿namespace Poppy.Arch.MOS6502;

using System.Collections.Frozen;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
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

	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new Mos65sc02RomBuilderAdapter();

	/// <inheritdoc />
	public void ValidateMemoryAddress(string mnemonic, long address, SourceLocation location,
		Action<string, SourceLocation> reportError, Action<string, SourceLocation> reportWarning) {
		// Check if this is a memory-writing instruction
		var isStoreInstruction = mnemonic.ToLowerInvariant() is
			"sta" or "stx" or "sty" or "stz" or
			"inc" or "dec" or "asl" or "lsr" or "rol" or "ror" or
			"tsb" or "trb" or
			"rmb0" or "rmb1" or "rmb2" or "rmb3" or
			"rmb4" or "rmb5" or "rmb6" or "rmb7" or
			"smb0" or "smb1" or "smb2" or "smb3" or
			"smb4" or "smb5" or "smb6" or "smb7";

		if (!isStoreInstruction) return;

		// Lynx memory map validation
		// $0000-$fbff: RAM (64KB - 1KB reserved)
		// $fc00-$fcff: Suzy hardware registers
		// $fd00-$fdff: Mikey hardware registers
		// $fe00-$ffff: Boot ROM (512 bytes)
		if (address is >= 0xfe00 and <= 0xffff) {
			// Boot ROM - cannot write to ROM
			reportError($"Cannot write to Lynx Boot ROM at ${address:x4}", location);
		} else if (address is >= 0xfd00 and <= 0xfdff) {
			// Mikey hardware registers
			reportWarning($"Writing to Lynx Mikey hardware register at ${address:x4}", location);
		} else if (address is >= 0xfc00 and <= 0xfcff) {
			// Suzy hardware registers
			reportWarning($"Writing to Lynx Suzy hardware register at ${address:x4}", location);
		}
	}

	private sealed class Mos65sc02RomBuilderAdapter : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			var romBuilder = new AtariLynxRomBuilder(
				bank0Size: 131072,
				bank1Size: 0,
				gameName: "Poppy Game");
			foreach (var segment in segments) {
				romBuilder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}
			return romBuilder.Build();
		}
	}

	private sealed class Mos65sc02Encoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSet65SC02.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

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
