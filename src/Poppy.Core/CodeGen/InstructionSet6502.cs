// ============================================================================
// InstructionSet6502.cs - 6502 Instruction Encoding
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Parser;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Provides instruction encoding for the MOS 6502 processor.
/// </summary>
public static class InstructionSet6502 {
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
	/// Lookup table for instruction opcodes by mnemonic and addressing mode.
	/// </summary>
	private static readonly Dictionary<(string Mnemonic, AddressingMode Mode), InstructionEncoding> _opcodes = new(MnemonicComparer.Instance) {
		// ADC - Add with Carry
		{ ("adc", AddressingMode.Immediate), new(0x69, 2) },
		{ ("adc", AddressingMode.ZeroPage), new(0x65, 2) },
		{ ("adc", AddressingMode.ZeroPageX), new(0x75, 2) },
		{ ("adc", AddressingMode.Absolute), new(0x6d, 3) },
		{ ("adc", AddressingMode.AbsoluteX), new(0x7d, 3) },
		{ ("adc", AddressingMode.AbsoluteY), new(0x79, 3) },
		{ ("adc", AddressingMode.IndexedIndirect), new(0x61, 2) },
		{ ("adc", AddressingMode.IndirectIndexed), new(0x71, 2) },

		// AND - Logical AND
		{ ("and", AddressingMode.Immediate), new(0x29, 2) },
		{ ("and", AddressingMode.ZeroPage), new(0x25, 2) },
		{ ("and", AddressingMode.ZeroPageX), new(0x35, 2) },
		{ ("and", AddressingMode.Absolute), new(0x2d, 3) },
		{ ("and", AddressingMode.AbsoluteX), new(0x3d, 3) },
		{ ("and", AddressingMode.AbsoluteY), new(0x39, 3) },
		{ ("and", AddressingMode.IndexedIndirect), new(0x21, 2) },
		{ ("and", AddressingMode.IndirectIndexed), new(0x31, 2) },

		// ASL - Arithmetic Shift Left
		{ ("asl", AddressingMode.Accumulator), new(0x0a, 1) },
		{ ("asl", AddressingMode.Implied), new(0x0a, 1) }, // ASL A
		{ ("asl", AddressingMode.ZeroPage), new(0x06, 2) },
		{ ("asl", AddressingMode.ZeroPageX), new(0x16, 2) },
		{ ("asl", AddressingMode.Absolute), new(0x0e, 3) },
		{ ("asl", AddressingMode.AbsoluteX), new(0x1e, 3) },

		// BCC - Branch if Carry Clear
		{ ("bcc", AddressingMode.Relative), new(0x90, 2) },
		{ ("bcc", AddressingMode.Absolute), new(0x90, 2) }, // Treat as relative

		// BCS - Branch if Carry Set
		{ ("bcs", AddressingMode.Relative), new(0xb0, 2) },
		{ ("bcs", AddressingMode.Absolute), new(0xb0, 2) },

		// BEQ - Branch if Equal (Zero flag set)
		{ ("beq", AddressingMode.Relative), new(0xf0, 2) },
		{ ("beq", AddressingMode.Absolute), new(0xf0, 2) },

		// BIT - Bit Test
		{ ("bit", AddressingMode.ZeroPage), new(0x24, 2) },
		{ ("bit", AddressingMode.Absolute), new(0x2c, 3) },

		// BMI - Branch if Minus (Negative flag set)
		{ ("bmi", AddressingMode.Relative), new(0x30, 2) },
		{ ("bmi", AddressingMode.Absolute), new(0x30, 2) },

		// BNE - Branch if Not Equal (Zero flag clear)
		{ ("bne", AddressingMode.Relative), new(0xd0, 2) },
		{ ("bne", AddressingMode.Absolute), new(0xd0, 2) },

		// BPL - Branch if Plus (Negative flag clear)
		{ ("bpl", AddressingMode.Relative), new(0x10, 2) },
		{ ("bpl", AddressingMode.Absolute), new(0x10, 2) },

		// BRK - Force Interrupt
		{ ("brk", AddressingMode.Implied), new(0x00, 1) },

		// BVC - Branch if Overflow Clear
		{ ("bvc", AddressingMode.Relative), new(0x50, 2) },
		{ ("bvc", AddressingMode.Absolute), new(0x50, 2) },

		// BVS - Branch if Overflow Set
		{ ("bvs", AddressingMode.Relative), new(0x70, 2) },
		{ ("bvs", AddressingMode.Absolute), new(0x70, 2) },

		// CLC - Clear Carry Flag
		{ ("clc", AddressingMode.Implied), new(0x18, 1) },

		// CLD - Clear Decimal Mode
		{ ("cld", AddressingMode.Implied), new(0xd8, 1) },

		// CLI - Clear Interrupt Disable
		{ ("cli", AddressingMode.Implied), new(0x58, 1) },

		// CLV - Clear Overflow Flag
		{ ("clv", AddressingMode.Implied), new(0xb8, 1) },

		// CMP - Compare Accumulator
		{ ("cmp", AddressingMode.Immediate), new(0xc9, 2) },
		{ ("cmp", AddressingMode.ZeroPage), new(0xc5, 2) },
		{ ("cmp", AddressingMode.ZeroPageX), new(0xd5, 2) },
		{ ("cmp", AddressingMode.Absolute), new(0xcd, 3) },
		{ ("cmp", AddressingMode.AbsoluteX), new(0xdd, 3) },
		{ ("cmp", AddressingMode.AbsoluteY), new(0xd9, 3) },
		{ ("cmp", AddressingMode.IndexedIndirect), new(0xc1, 2) },
		{ ("cmp", AddressingMode.IndirectIndexed), new(0xd1, 2) },

		// CPX - Compare X Register
		{ ("cpx", AddressingMode.Immediate), new(0xe0, 2) },
		{ ("cpx", AddressingMode.ZeroPage), new(0xe4, 2) },
		{ ("cpx", AddressingMode.Absolute), new(0xec, 3) },

		// CPY - Compare Y Register
		{ ("cpy", AddressingMode.Immediate), new(0xc0, 2) },
		{ ("cpy", AddressingMode.ZeroPage), new(0xc4, 2) },
		{ ("cpy", AddressingMode.Absolute), new(0xcc, 3) },

		// DEC - Decrement Memory
		{ ("dec", AddressingMode.ZeroPage), new(0xc6, 2) },
		{ ("dec", AddressingMode.ZeroPageX), new(0xd6, 2) },
		{ ("dec", AddressingMode.Absolute), new(0xce, 3) },
		{ ("dec", AddressingMode.AbsoluteX), new(0xde, 3) },

		// DEX - Decrement X Register
		{ ("dex", AddressingMode.Implied), new(0xca, 1) },

		// DEY - Decrement Y Register
		{ ("dey", AddressingMode.Implied), new(0x88, 1) },

		// EOR - Exclusive OR
		{ ("eor", AddressingMode.Immediate), new(0x49, 2) },
		{ ("eor", AddressingMode.ZeroPage), new(0x45, 2) },
		{ ("eor", AddressingMode.ZeroPageX), new(0x55, 2) },
		{ ("eor", AddressingMode.Absolute), new(0x4d, 3) },
		{ ("eor", AddressingMode.AbsoluteX), new(0x5d, 3) },
		{ ("eor", AddressingMode.AbsoluteY), new(0x59, 3) },
		{ ("eor", AddressingMode.IndexedIndirect), new(0x41, 2) },
		{ ("eor", AddressingMode.IndirectIndexed), new(0x51, 2) },

		// INC - Increment Memory
		{ ("inc", AddressingMode.ZeroPage), new(0xe6, 2) },
		{ ("inc", AddressingMode.ZeroPageX), new(0xf6, 2) },
		{ ("inc", AddressingMode.Absolute), new(0xee, 3) },
		{ ("inc", AddressingMode.AbsoluteX), new(0xfe, 3) },

		// INX - Increment X Register
		{ ("inx", AddressingMode.Implied), new(0xe8, 1) },

		// INY - Increment Y Register
		{ ("iny", AddressingMode.Implied), new(0xc8, 1) },

		// JMP - Jump
		{ ("jmp", AddressingMode.Absolute), new(0x4c, 3) },
		{ ("jmp", AddressingMode.Indirect), new(0x6c, 3) },

		// JSR - Jump to Subroutine
		{ ("jsr", AddressingMode.Absolute), new(0x20, 3) },

		// LDA - Load Accumulator
		{ ("lda", AddressingMode.Immediate), new(0xa9, 2) },
		{ ("lda", AddressingMode.ZeroPage), new(0xa5, 2) },
		{ ("lda", AddressingMode.ZeroPageX), new(0xb5, 2) },
		{ ("lda", AddressingMode.Absolute), new(0xad, 3) },
		{ ("lda", AddressingMode.AbsoluteX), new(0xbd, 3) },
		{ ("lda", AddressingMode.AbsoluteY), new(0xb9, 3) },
		{ ("lda", AddressingMode.IndexedIndirect), new(0xa1, 2) },
		{ ("lda", AddressingMode.IndirectIndexed), new(0xb1, 2) },

		// LDX - Load X Register
		{ ("ldx", AddressingMode.Immediate), new(0xa2, 2) },
		{ ("ldx", AddressingMode.ZeroPage), new(0xa6, 2) },
		{ ("ldx", AddressingMode.ZeroPageY), new(0xb6, 2) },
		{ ("ldx", AddressingMode.Absolute), new(0xae, 3) },
		{ ("ldx", AddressingMode.AbsoluteY), new(0xbe, 3) },

		// LDY - Load Y Register
		{ ("ldy", AddressingMode.Immediate), new(0xa0, 2) },
		{ ("ldy", AddressingMode.ZeroPage), new(0xa4, 2) },
		{ ("ldy", AddressingMode.ZeroPageX), new(0xb4, 2) },
		{ ("ldy", AddressingMode.Absolute), new(0xac, 3) },
		{ ("ldy", AddressingMode.AbsoluteX), new(0xbc, 3) },

		// LSR - Logical Shift Right
		{ ("lsr", AddressingMode.Accumulator), new(0x4a, 1) },
		{ ("lsr", AddressingMode.Implied), new(0x4a, 1) }, // LSR A
		{ ("lsr", AddressingMode.ZeroPage), new(0x46, 2) },
		{ ("lsr", AddressingMode.ZeroPageX), new(0x56, 2) },
		{ ("lsr", AddressingMode.Absolute), new(0x4e, 3) },
		{ ("lsr", AddressingMode.AbsoluteX), new(0x5e, 3) },

		// NOP - No Operation
		{ ("nop", AddressingMode.Implied), new(0xea, 1) },

		// ORA - Logical OR
		{ ("ora", AddressingMode.Immediate), new(0x09, 2) },
		{ ("ora", AddressingMode.ZeroPage), new(0x05, 2) },
		{ ("ora", AddressingMode.ZeroPageX), new(0x15, 2) },
		{ ("ora", AddressingMode.Absolute), new(0x0d, 3) },
		{ ("ora", AddressingMode.AbsoluteX), new(0x1d, 3) },
		{ ("ora", AddressingMode.AbsoluteY), new(0x19, 3) },
		{ ("ora", AddressingMode.IndexedIndirect), new(0x01, 2) },
		{ ("ora", AddressingMode.IndirectIndexed), new(0x11, 2) },

		// PHA - Push Accumulator
		{ ("pha", AddressingMode.Implied), new(0x48, 1) },

		// PHP - Push Processor Status
		{ ("php", AddressingMode.Implied), new(0x08, 1) },

		// PLA - Pull Accumulator
		{ ("pla", AddressingMode.Implied), new(0x68, 1) },

		// PLP - Pull Processor Status
		{ ("plp", AddressingMode.Implied), new(0x28, 1) },

		// ROL - Rotate Left
		{ ("rol", AddressingMode.Accumulator), new(0x2a, 1) },
		{ ("rol", AddressingMode.Implied), new(0x2a, 1) }, // ROL A
		{ ("rol", AddressingMode.ZeroPage), new(0x26, 2) },
		{ ("rol", AddressingMode.ZeroPageX), new(0x36, 2) },
		{ ("rol", AddressingMode.Absolute), new(0x2e, 3) },
		{ ("rol", AddressingMode.AbsoluteX), new(0x3e, 3) },

		// ROR - Rotate Right
		{ ("ror", AddressingMode.Accumulator), new(0x6a, 1) },
		{ ("ror", AddressingMode.Implied), new(0x6a, 1) }, // ROR A
		{ ("ror", AddressingMode.ZeroPage), new(0x66, 2) },
		{ ("ror", AddressingMode.ZeroPageX), new(0x76, 2) },
		{ ("ror", AddressingMode.Absolute), new(0x6e, 3) },
		{ ("ror", AddressingMode.AbsoluteX), new(0x7e, 3) },

		// RTI - Return from Interrupt
		{ ("rti", AddressingMode.Implied), new(0x40, 1) },

		// RTS - Return from Subroutine
		{ ("rts", AddressingMode.Implied), new(0x60, 1) },

		// SBC - Subtract with Carry
		{ ("sbc", AddressingMode.Immediate), new(0xe9, 2) },
		{ ("sbc", AddressingMode.ZeroPage), new(0xe5, 2) },
		{ ("sbc", AddressingMode.ZeroPageX), new(0xf5, 2) },
		{ ("sbc", AddressingMode.Absolute), new(0xed, 3) },
		{ ("sbc", AddressingMode.AbsoluteX), new(0xfd, 3) },
		{ ("sbc", AddressingMode.AbsoluteY), new(0xf9, 3) },
		{ ("sbc", AddressingMode.IndexedIndirect), new(0xe1, 2) },
		{ ("sbc", AddressingMode.IndirectIndexed), new(0xf1, 2) },

		// SEC - Set Carry Flag
		{ ("sec", AddressingMode.Implied), new(0x38, 1) },

		// SED - Set Decimal Flag
		{ ("sed", AddressingMode.Implied), new(0xf8, 1) },

		// SEI - Set Interrupt Disable
		{ ("sei", AddressingMode.Implied), new(0x78, 1) },

		// STA - Store Accumulator
		{ ("sta", AddressingMode.ZeroPage), new(0x85, 2) },
		{ ("sta", AddressingMode.ZeroPageX), new(0x95, 2) },
		{ ("sta", AddressingMode.Absolute), new(0x8d, 3) },
		{ ("sta", AddressingMode.AbsoluteX), new(0x9d, 3) },
		{ ("sta", AddressingMode.AbsoluteY), new(0x99, 3) },
		{ ("sta", AddressingMode.IndexedIndirect), new(0x81, 2) },
		{ ("sta", AddressingMode.IndirectIndexed), new(0x91, 2) },

		// STX - Store X Register
		{ ("stx", AddressingMode.ZeroPage), new(0x86, 2) },
		{ ("stx", AddressingMode.ZeroPageY), new(0x96, 2) },
		{ ("stx", AddressingMode.Absolute), new(0x8e, 3) },

		// STY - Store Y Register
		{ ("sty", AddressingMode.ZeroPage), new(0x84, 2) },
		{ ("sty", AddressingMode.ZeroPageX), new(0x94, 2) },
		{ ("sty", AddressingMode.Absolute), new(0x8c, 3) },

		// TAX - Transfer Accumulator to X
		{ ("tax", AddressingMode.Implied), new(0xaa, 1) },

		// TAY - Transfer Accumulator to Y
		{ ("tay", AddressingMode.Implied), new(0xa8, 1) },

		// TSX - Transfer Stack Pointer to X
		{ ("tsx", AddressingMode.Implied), new(0xba, 1) },

		// TXA - Transfer X to Accumulator
		{ ("txa", AddressingMode.Implied), new(0x8a, 1) },

		// TXS - Transfer X to Stack Pointer
		{ ("txs", AddressingMode.Implied), new(0x9a, 1) },

		// TYA - Transfer Y to Accumulator
		{ ("tya", AddressingMode.Implied), new(0x98, 1) },
	};

	/// <summary>
	/// Tries to get the encoding for an instruction.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <param name="mode">The addressing mode.</param>
	/// <param name="encoding">The encoding if found.</param>
	/// <returns>True if the encoding was found.</returns>
	public static bool TryGetEncoding(string mnemonic, AddressingMode mode, out InstructionEncoding encoding) {
		return _opcodes.TryGetValue((mnemonic, mode), out encoding);
	}

	/// <summary>
	/// Gets all supported mnemonics.
	/// </summary>
	public static IEnumerable<string> GetAllMnemonics() {
		return _opcodes.Keys.Select(k => k.Mnemonic).Distinct(StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Gets all supported modes for a mnemonic.
	/// </summary>
	/// <param name="mnemonic">The mnemonic to check.</param>
	/// <returns>The supported addressing modes.</returns>
	public static IEnumerable<AddressingMode> GetSupportedModes(string mnemonic) {
		return _opcodes.Keys
			.Where(k => k.Mnemonic.Equals(mnemonic, StringComparison.OrdinalIgnoreCase))
			.Select(k => k.Mode);
	}
}

