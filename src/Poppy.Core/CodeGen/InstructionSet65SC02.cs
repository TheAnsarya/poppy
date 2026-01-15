// ============================================================================
// InstructionSet65SC02.cs - 65SC02 Instruction Encoding (Atari Lynx)
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Parser;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Provides instruction encoding for the WDC 65SC02 processor (Atari Lynx).
/// The 65SC02 is an enhanced version of the 6502 with additional instructions,
/// addressing modes, and bug fixes (JMP indirect bug fixed).
/// </summary>
public static class InstructionSet65SC02 {
	/// <summary>
	/// Instruction encoding information.
	/// </summary>
	/// <param name="Opcode">The opcode byte.</param>
	/// <param name="Size">The total instruction size in bytes.</param>
	public readonly record struct InstructionEncoding(byte Opcode, int Size);

	/// <summary>
	/// Custom comparer for case-insensitive mnemonic lookup.
	/// </summary>
	private sealed class MnemonicComparer : IEqualityComparer<(string Mnemonic, AddressingMode Mode)> {
		public static readonly MnemonicComparer Instance = new();

		public bool Equals((string Mnemonic, AddressingMode Mode) x, (string Mnemonic, AddressingMode Mode) y) {
			return string.Equals(x.Mnemonic, y.Mnemonic, StringComparison.OrdinalIgnoreCase) && x.Mode == y.Mode;
		}

		public int GetHashCode((string Mnemonic, AddressingMode Mode) obj) {
			return HashCode.Combine(obj.Mnemonic.ToLowerInvariant(), obj.Mode);
		}
	}

	/// <summary>
	/// Lookup table for 65SC02-specific instructions and addressing modes.
	/// This dictionary contains only the NEW instructions and modes added in the 65SC02.
	/// For 6502-compatible instructions, we fall back to InstructionSet6502.
	/// </summary>
	private static readonly Dictionary<(string Mnemonic, AddressingMode Mode), InstructionEncoding> _opcodes = new(MnemonicComparer.Instance) {
		// BRA - Branch Always (new in 65C02)
		{ ("bra", AddressingMode.Relative), new(0x80, 2) },

		// PHX - Push X Register (new in 65C02)
		{ ("phx", AddressingMode.Implied), new(0xda, 1) },

		// PHY - Push Y Register (new in 65C02)
		{ ("phy", AddressingMode.Implied), new(0x5a, 1) },

		// PLX - Pull X Register (new in 65C02)
		{ ("plx", AddressingMode.Implied), new(0xfa, 1) },

		// PLY - Pull Y Register (new in 65C02)
		{ ("ply", AddressingMode.Implied), new(0x7a, 1) },

		// STZ - Store Zero (new in 65C02)
		{ ("stz", AddressingMode.ZeroPage), new(0x64, 2) },
		{ ("stz", AddressingMode.ZeroPageX), new(0x74, 2) },
		{ ("stz", AddressingMode.Absolute), new(0x9c, 3) },
		{ ("stz", AddressingMode.AbsoluteX), new(0x9e, 3) },

		// TRB - Test and Reset Bits (new in 65C02)
		{ ("trb", AddressingMode.ZeroPage), new(0x14, 2) },
		{ ("trb", AddressingMode.Absolute), new(0x1c, 3) },

		// TSB - Test and Set Bits (new in 65C02)
		{ ("tsb", AddressingMode.ZeroPage), new(0x04, 2) },
		{ ("tsb", AddressingMode.Absolute), new(0x0c, 3) },

		// BIT - Bit Test with immediate mode (new in 65C02)
		{ ("bit", AddressingMode.Immediate), new(0x89, 2) },
		{ ("bit", AddressingMode.ZeroPageX), new(0x34, 2) },
		{ ("bit", AddressingMode.AbsoluteX), new(0x3c, 3) },

		// JMP - Absolute Indexed Indirect (new in 65C02, fixes JMP bug)
		{ ("jmp", AddressingMode.AbsoluteIndexedIndirect), new(0x7c, 3) },

		// ADC - with ZeroPage Indirect (new in 65C02)
		{ ("adc", AddressingMode.ZeroPageIndirect), new(0x72, 2) },

		// AND - with ZeroPage Indirect (new in 65C02)
		{ ("and", AddressingMode.ZeroPageIndirect), new(0x32, 2) },

		// CMP - with ZeroPage Indirect (new in 65C02)
		{ ("cmp", AddressingMode.ZeroPageIndirect), new(0xd2, 2) },

		// EOR - with ZeroPage Indirect (new in 65C02)
		{ ("eor", AddressingMode.ZeroPageIndirect), new(0x52, 2) },

		// LDA - with ZeroPage Indirect (new in 65C02)
		{ ("lda", AddressingMode.ZeroPageIndirect), new(0xb2, 2) },

		// ORA - with ZeroPage Indirect (new in 65C02)
		{ ("ora", AddressingMode.ZeroPageIndirect), new(0x12, 2) },

		// SBC - with ZeroPage Indirect (new in 65C02)
		{ ("sbc", AddressingMode.ZeroPageIndirect), new(0xf2, 2) },

		// STA - with ZeroPage Indirect (new in 65C02)
		{ ("sta", AddressingMode.ZeroPageIndirect), new(0x92, 2) },

		// INC/DEC A - Accumulator mode (new in 65C02)
		{ ("inc", AddressingMode.Accumulator), new(0x1a, 1) },
		{ ("dec", AddressingMode.Accumulator), new(0x3a, 1) },
	};

	/// <summary>
	/// Attempts to get the instruction encoding for the given mnemonic and addressing mode.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic (e.g., "lda", "sta").</param>
	/// <param name="mode">The addressing mode.</param>
	/// <param name="encoding">The instruction encoding, if found.</param>
	/// <returns>True if the instruction was found, false otherwise.</returns>
	public static bool TryGetEncoding(string mnemonic, AddressingMode mode, out InstructionEncoding encoding) {
		// First check for 65SC02-specific instructions/modes
		if (_opcodes.TryGetValue((mnemonic, mode), out encoding)) {
			return true;
		}

		// Fall back to 6502 instruction set for compatibility
		if (InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding6502)) {
			encoding = new InstructionEncoding(encoding6502.Opcode, encoding6502.Size);
			return true;
		}

		encoding = default;
		return false;
	}

	/// <summary>
	/// Gets all supported mnemonics for the 65SC02.
	/// </summary>
	/// <returns>A collection of all supported instruction mnemonics.</returns>
	public static IEnumerable<string> GetAllMnemonics() {
		// Combine 65SC02-specific and 6502 mnemonics
		var sc02Mnemonics = _opcodes.Keys.Select(k => k.Mnemonic);
		var c02Mnemonics = InstructionSet6502.GetAllMnemonics();
		return sc02Mnemonics.Concat(c02Mnemonics).Distinct(StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Gets all supported addressing modes for a given mnemonic.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <returns>A collection of supported addressing modes for the mnemonic.</returns>
	public static IEnumerable<AddressingMode> GetSupportedModes(string mnemonic) {
		// Combine 65SC02-specific and 6502 addressing modes
		var sc02Modes = _opcodes.Keys
			.Where(k => k.Mnemonic.Equals(mnemonic, StringComparison.OrdinalIgnoreCase))
			.Select(k => k.Mode);
		var c02Modes = InstructionSet6502.GetSupportedModes(mnemonic);
		return sc02Modes.Concat(c02Modes).Distinct();
	}

	/// <summary>
	/// Checks if an instruction is a branch instruction.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <returns>True if the instruction is a branch.</returns>
	public static bool IsBranchInstruction(string mnemonic) {
		var lower = mnemonic.ToLowerInvariant();
		return lower switch {
			"bcc" or "bcs" or "beq" or "bmi" or "bne" or "bpl" or "bvc" or "bvs" or "bra" => true,
			_ => false
		};
	}
}
