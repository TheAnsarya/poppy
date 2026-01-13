// ============================================================================
// InstructionSet65816.cs - WDC 65816 Instruction Encoding
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Parser;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Provides instruction encoding for the WDC 65816 processor (SNES).
/// </summary>
/// <remarks>
/// The 65816 is a 16-bit extension of the 6502, adding:
/// - 16-bit accumulator and index registers (controllable via M/X flags)
/// - 24-bit addressing (16MB address space)
/// - New addressing modes (stack relative, direct page indirect long, etc.)
/// - Block move instructions (MVP/MVN)
/// - Long jumps and calls (JML, JSL)
/// </remarks>
public static class InstructionSet65816 {
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
	/// Includes all 6502 instructions plus 65816 extensions.
	/// </summary>
	private static readonly Dictionary<(string Mnemonic, AddressingMode Mode), InstructionEncoding> _opcodes = new(MnemonicComparer.Instance) {
		// ========================================================================
		// ADC - Add with Carry
		// ========================================================================
		{ ("adc", AddressingMode.Immediate), new(0x69, 2) }, // Size depends on M flag
		{ ("adc", AddressingMode.ZeroPage), new(0x65, 2) },
		{ ("adc", AddressingMode.ZeroPageX), new(0x75, 2) },
		{ ("adc", AddressingMode.Absolute), new(0x6d, 3) },
		{ ("adc", AddressingMode.AbsoluteX), new(0x7d, 3) },
		{ ("adc", AddressingMode.AbsoluteY), new(0x79, 3) },
		{ ("adc", AddressingMode.IndexedIndirect), new(0x61, 2) },
		{ ("adc", AddressingMode.IndirectIndexed), new(0x71, 2) },
		// 65816 additions
		{ ("adc", AddressingMode.AbsoluteLong), new(0x6f, 4) },
		{ ("adc", AddressingMode.AbsoluteLongX), new(0x7f, 4) },
		{ ("adc", AddressingMode.StackRelative), new(0x63, 2) },
		{ ("adc", AddressingMode.StackRelativeIndirectIndexed), new(0x73, 2) },
		{ ("adc", AddressingMode.DirectPageIndirectLong), new(0x67, 2) },
		{ ("adc", AddressingMode.DirectPageIndirectLongY), new(0x77, 2) },

		// ========================================================================
		// AND - Logical AND
		// ========================================================================
		{ ("and", AddressingMode.Immediate), new(0x29, 2) },
		{ ("and", AddressingMode.ZeroPage), new(0x25, 2) },
		{ ("and", AddressingMode.ZeroPageX), new(0x35, 2) },
		{ ("and", AddressingMode.Absolute), new(0x2d, 3) },
		{ ("and", AddressingMode.AbsoluteX), new(0x3d, 3) },
		{ ("and", AddressingMode.AbsoluteY), new(0x39, 3) },
		{ ("and", AddressingMode.IndexedIndirect), new(0x21, 2) },
		{ ("and", AddressingMode.IndirectIndexed), new(0x31, 2) },
		// 65816 additions
		{ ("and", AddressingMode.AbsoluteLong), new(0x2f, 4) },
		{ ("and", AddressingMode.AbsoluteLongX), new(0x3f, 4) },
		{ ("and", AddressingMode.StackRelative), new(0x23, 2) },
		{ ("and", AddressingMode.StackRelativeIndirectIndexed), new(0x33, 2) },
		{ ("and", AddressingMode.DirectPageIndirectLong), new(0x27, 2) },
		{ ("and", AddressingMode.DirectPageIndirectLongY), new(0x37, 2) },

		// ========================================================================
		// ASL - Arithmetic Shift Left
		// ========================================================================
		{ ("asl", AddressingMode.Accumulator), new(0x0a, 1) },
		{ ("asl", AddressingMode.Implied), new(0x0a, 1) },
		{ ("asl", AddressingMode.ZeroPage), new(0x06, 2) },
		{ ("asl", AddressingMode.ZeroPageX), new(0x16, 2) },
		{ ("asl", AddressingMode.Absolute), new(0x0e, 3) },
		{ ("asl", AddressingMode.AbsoluteX), new(0x1e, 3) },

		// ========================================================================
		// Branch Instructions
		// ========================================================================
		{ ("bcc", AddressingMode.Relative), new(0x90, 2) },
		{ ("bcc", AddressingMode.Absolute), new(0x90, 2) },
		{ ("bcs", AddressingMode.Relative), new(0xb0, 2) },
		{ ("bcs", AddressingMode.Absolute), new(0xb0, 2) },
		{ ("beq", AddressingMode.Relative), new(0xf0, 2) },
		{ ("beq", AddressingMode.Absolute), new(0xf0, 2) },
		{ ("bmi", AddressingMode.Relative), new(0x30, 2) },
		{ ("bmi", AddressingMode.Absolute), new(0x30, 2) },
		{ ("bne", AddressingMode.Relative), new(0xd0, 2) },
		{ ("bne", AddressingMode.Absolute), new(0xd0, 2) },
		{ ("bpl", AddressingMode.Relative), new(0x10, 2) },
		{ ("bpl", AddressingMode.Absolute), new(0x10, 2) },
		{ ("bvc", AddressingMode.Relative), new(0x50, 2) },
		{ ("bvc", AddressingMode.Absolute), new(0x50, 2) },
		{ ("bvs", AddressingMode.Relative), new(0x70, 2) },
		{ ("bvs", AddressingMode.Absolute), new(0x70, 2) },
		// 65816 additions
		{ ("bra", AddressingMode.Relative), new(0x80, 2) },
		{ ("bra", AddressingMode.Absolute), new(0x80, 2) },
		{ ("brl", AddressingMode.Relative), new(0x82, 3) }, // Long relative branch
		{ ("brl", AddressingMode.Absolute), new(0x82, 3) },

		// ========================================================================
		// BIT - Bit Test
		// ========================================================================
		{ ("bit", AddressingMode.ZeroPage), new(0x24, 2) },
		{ ("bit", AddressingMode.Absolute), new(0x2c, 3) },
		// 65816 additions
		{ ("bit", AddressingMode.Immediate), new(0x89, 2) },
		{ ("bit", AddressingMode.ZeroPageX), new(0x34, 2) },
		{ ("bit", AddressingMode.AbsoluteX), new(0x3c, 3) },

		// ========================================================================
		// BRK/COP - Software Interrupts
		// ========================================================================
		{ ("brk", AddressingMode.Implied), new(0x00, 2) }, // 65816 BRK is 2 bytes
		{ ("cop", AddressingMode.Implied), new(0x02, 2) }, // 65816 only

		// ========================================================================
		// Clear/Set Flags
		// ========================================================================
		{ ("clc", AddressingMode.Implied), new(0x18, 1) },
		{ ("cld", AddressingMode.Implied), new(0xd8, 1) },
		{ ("cli", AddressingMode.Implied), new(0x58, 1) },
		{ ("clv", AddressingMode.Implied), new(0xb8, 1) },
		{ ("sec", AddressingMode.Implied), new(0x38, 1) },
		{ ("sed", AddressingMode.Implied), new(0xf8, 1) },
		{ ("sei", AddressingMode.Implied), new(0x78, 1) },

		// ========================================================================
		// CMP - Compare Accumulator
		// ========================================================================
		{ ("cmp", AddressingMode.Immediate), new(0xc9, 2) },
		{ ("cmp", AddressingMode.ZeroPage), new(0xc5, 2) },
		{ ("cmp", AddressingMode.ZeroPageX), new(0xd5, 2) },
		{ ("cmp", AddressingMode.Absolute), new(0xcd, 3) },
		{ ("cmp", AddressingMode.AbsoluteX), new(0xdd, 3) },
		{ ("cmp", AddressingMode.AbsoluteY), new(0xd9, 3) },
		{ ("cmp", AddressingMode.IndexedIndirect), new(0xc1, 2) },
		{ ("cmp", AddressingMode.IndirectIndexed), new(0xd1, 2) },
		// 65816 additions
		{ ("cmp", AddressingMode.AbsoluteLong), new(0xcf, 4) },
		{ ("cmp", AddressingMode.AbsoluteLongX), new(0xdf, 4) },
		{ ("cmp", AddressingMode.StackRelative), new(0xc3, 2) },
		{ ("cmp", AddressingMode.StackRelativeIndirectIndexed), new(0xd3, 2) },
		{ ("cmp", AddressingMode.DirectPageIndirectLong), new(0xc7, 2) },
		{ ("cmp", AddressingMode.DirectPageIndirectLongY), new(0xd7, 2) },

		// ========================================================================
		// CPX - Compare X Register
		// ========================================================================
		{ ("cpx", AddressingMode.Immediate), new(0xe0, 2) },
		{ ("cpx", AddressingMode.ZeroPage), new(0xe4, 2) },
		{ ("cpx", AddressingMode.Absolute), new(0xec, 3) },

		// ========================================================================
		// CPY - Compare Y Register
		// ========================================================================
		{ ("cpy", AddressingMode.Immediate), new(0xc0, 2) },
		{ ("cpy", AddressingMode.ZeroPage), new(0xc4, 2) },
		{ ("cpy", AddressingMode.Absolute), new(0xcc, 3) },

		// ========================================================================
		// DEC - Decrement
		// ========================================================================
		{ ("dec", AddressingMode.Accumulator), new(0x3a, 1) }, // 65816 only
		{ ("dec", AddressingMode.ZeroPage), new(0xc6, 2) },
		{ ("dec", AddressingMode.ZeroPageX), new(0xd6, 2) },
		{ ("dec", AddressingMode.Absolute), new(0xce, 3) },
		{ ("dec", AddressingMode.AbsoluteX), new(0xde, 3) },
		{ ("dea", AddressingMode.Implied), new(0x3a, 1) }, // 65816 alias

		// ========================================================================
		// DEX/DEY - Decrement Index
		// ========================================================================
		{ ("dex", AddressingMode.Implied), new(0xca, 1) },
		{ ("dey", AddressingMode.Implied), new(0x88, 1) },

		// ========================================================================
		// EOR - Exclusive OR
		// ========================================================================
		{ ("eor", AddressingMode.Immediate), new(0x49, 2) },
		{ ("eor", AddressingMode.ZeroPage), new(0x45, 2) },
		{ ("eor", AddressingMode.ZeroPageX), new(0x55, 2) },
		{ ("eor", AddressingMode.Absolute), new(0x4d, 3) },
		{ ("eor", AddressingMode.AbsoluteX), new(0x5d, 3) },
		{ ("eor", AddressingMode.AbsoluteY), new(0x59, 3) },
		{ ("eor", AddressingMode.IndexedIndirect), new(0x41, 2) },
		{ ("eor", AddressingMode.IndirectIndexed), new(0x51, 2) },
		// 65816 additions
		{ ("eor", AddressingMode.AbsoluteLong), new(0x4f, 4) },
		{ ("eor", AddressingMode.AbsoluteLongX), new(0x5f, 4) },
		{ ("eor", AddressingMode.StackRelative), new(0x43, 2) },
		{ ("eor", AddressingMode.StackRelativeIndirectIndexed), new(0x53, 2) },
		{ ("eor", AddressingMode.DirectPageIndirectLong), new(0x47, 2) },
		{ ("eor", AddressingMode.DirectPageIndirectLongY), new(0x57, 2) },

		// ========================================================================
		// INC - Increment
		// ========================================================================
		{ ("inc", AddressingMode.Accumulator), new(0x1a, 1) }, // 65816 only
		{ ("inc", AddressingMode.ZeroPage), new(0xe6, 2) },
		{ ("inc", AddressingMode.ZeroPageX), new(0xf6, 2) },
		{ ("inc", AddressingMode.Absolute), new(0xee, 3) },
		{ ("inc", AddressingMode.AbsoluteX), new(0xfe, 3) },
		{ ("ina", AddressingMode.Implied), new(0x1a, 1) }, // 65816 alias

		// ========================================================================
		// INX/INY - Increment Index
		// ========================================================================
		{ ("inx", AddressingMode.Implied), new(0xe8, 1) },
		{ ("iny", AddressingMode.Implied), new(0xc8, 1) },

		// ========================================================================
		// JMP - Jump
		// ========================================================================
		{ ("jmp", AddressingMode.Absolute), new(0x4c, 3) },
		{ ("jmp", AddressingMode.Indirect), new(0x6c, 3) },
		// 65816 additions
		{ ("jmp", AddressingMode.AbsoluteIndexedIndirect), new(0x7c, 3) },
		{ ("jmp", AddressingMode.AbsoluteLong), new(0x5c, 4) },
		{ ("jmp", AddressingMode.AbsoluteIndirectLong), new(0xdc, 3) },
		{ ("jml", AddressingMode.Absolute), new(0x5c, 4) }, // Alias for JMP long
		{ ("jml", AddressingMode.AbsoluteLong), new(0x5c, 4) },
		{ ("jml", AddressingMode.AbsoluteIndirectLong), new(0xdc, 3) },

		// ========================================================================
		// JSR/JSL - Jump to Subroutine
		// ========================================================================
		{ ("jsr", AddressingMode.Absolute), new(0x20, 3) },
		// 65816 additions
		{ ("jsr", AddressingMode.AbsoluteIndexedIndirect), new(0xfc, 3) },
		{ ("jsr", AddressingMode.AbsoluteLong), new(0x22, 4) },
		{ ("jsl", AddressingMode.Absolute), new(0x22, 4) }, // Alias for JSR long
		{ ("jsl", AddressingMode.AbsoluteLong), new(0x22, 4) },

		// ========================================================================
		// LDA - Load Accumulator
		// ========================================================================
		{ ("lda", AddressingMode.Immediate), new(0xa9, 2) },
		{ ("lda", AddressingMode.ZeroPage), new(0xa5, 2) },
		{ ("lda", AddressingMode.ZeroPageX), new(0xb5, 2) },
		{ ("lda", AddressingMode.Absolute), new(0xad, 3) },
		{ ("lda", AddressingMode.AbsoluteX), new(0xbd, 3) },
		{ ("lda", AddressingMode.AbsoluteY), new(0xb9, 3) },
		{ ("lda", AddressingMode.IndexedIndirect), new(0xa1, 2) },
		{ ("lda", AddressingMode.IndirectIndexed), new(0xb1, 2) },
		// 65816 additions
		{ ("lda", AddressingMode.AbsoluteLong), new(0xaf, 4) },
		{ ("lda", AddressingMode.AbsoluteLongX), new(0xbf, 4) },
		{ ("lda", AddressingMode.StackRelative), new(0xa3, 2) },
		{ ("lda", AddressingMode.StackRelativeIndirectIndexed), new(0xb3, 2) },
		{ ("lda", AddressingMode.DirectPageIndirectLong), new(0xa7, 2) },
		{ ("lda", AddressingMode.DirectPageIndirectLongY), new(0xb7, 2) },

		// ========================================================================
		// LDX - Load X Register
		// ========================================================================
		{ ("ldx", AddressingMode.Immediate), new(0xa2, 2) },
		{ ("ldx", AddressingMode.ZeroPage), new(0xa6, 2) },
		{ ("ldx", AddressingMode.ZeroPageY), new(0xb6, 2) },
		{ ("ldx", AddressingMode.Absolute), new(0xae, 3) },
		{ ("ldx", AddressingMode.AbsoluteY), new(0xbe, 3) },

		// ========================================================================
		// LDY - Load Y Register
		// ========================================================================
		{ ("ldy", AddressingMode.Immediate), new(0xa0, 2) },
		{ ("ldy", AddressingMode.ZeroPage), new(0xa4, 2) },
		{ ("ldy", AddressingMode.ZeroPageX), new(0xb4, 2) },
		{ ("ldy", AddressingMode.Absolute), new(0xac, 3) },
		{ ("ldy", AddressingMode.AbsoluteX), new(0xbc, 3) },

		// ========================================================================
		// LSR - Logical Shift Right
		// ========================================================================
		{ ("lsr", AddressingMode.Accumulator), new(0x4a, 1) },
		{ ("lsr", AddressingMode.Implied), new(0x4a, 1) },
		{ ("lsr", AddressingMode.ZeroPage), new(0x46, 2) },
		{ ("lsr", AddressingMode.ZeroPageX), new(0x56, 2) },
		{ ("lsr", AddressingMode.Absolute), new(0x4e, 3) },
		{ ("lsr", AddressingMode.AbsoluteX), new(0x5e, 3) },

		// ========================================================================
		// MVP/MVN - Block Move
		// ========================================================================
		{ ("mvp", AddressingMode.BlockMove), new(0x44, 3) },
		{ ("mvn", AddressingMode.BlockMove), new(0x54, 3) },

		// ========================================================================
		// NOP - No Operation
		// ========================================================================
		{ ("nop", AddressingMode.Implied), new(0xea, 1) },

		// ========================================================================
		// ORA - Logical OR
		// ========================================================================
		{ ("ora", AddressingMode.Immediate), new(0x09, 2) },
		{ ("ora", AddressingMode.ZeroPage), new(0x05, 2) },
		{ ("ora", AddressingMode.ZeroPageX), new(0x15, 2) },
		{ ("ora", AddressingMode.Absolute), new(0x0d, 3) },
		{ ("ora", AddressingMode.AbsoluteX), new(0x1d, 3) },
		{ ("ora", AddressingMode.AbsoluteY), new(0x19, 3) },
		{ ("ora", AddressingMode.IndexedIndirect), new(0x01, 2) },
		{ ("ora", AddressingMode.IndirectIndexed), new(0x11, 2) },
		// 65816 additions
		{ ("ora", AddressingMode.AbsoluteLong), new(0x0f, 4) },
		{ ("ora", AddressingMode.AbsoluteLongX), new(0x1f, 4) },
		{ ("ora", AddressingMode.StackRelative), new(0x03, 2) },
		{ ("ora", AddressingMode.StackRelativeIndirectIndexed), new(0x13, 2) },
		{ ("ora", AddressingMode.DirectPageIndirectLong), new(0x07, 2) },
		{ ("ora", AddressingMode.DirectPageIndirectLongY), new(0x17, 2) },

		// ========================================================================
		// PEA/PEI/PER - Push Effective Address
		// ========================================================================
		{ ("pea", AddressingMode.Absolute), new(0xf4, 3) },
		{ ("pei", AddressingMode.Indirect), new(0xd4, 2) },
		{ ("pei", AddressingMode.ZeroPage), new(0xd4, 2) },
		{ ("per", AddressingMode.Relative), new(0x62, 3) },
		{ ("per", AddressingMode.Absolute), new(0x62, 3) },

		// ========================================================================
		// Push/Pull Instructions
		// ========================================================================
		{ ("pha", AddressingMode.Implied), new(0x48, 1) },
		{ ("php", AddressingMode.Implied), new(0x08, 1) },
		{ ("pla", AddressingMode.Implied), new(0x68, 1) },
		{ ("plp", AddressingMode.Implied), new(0x28, 1) },
		// 65816 additions
		{ ("phx", AddressingMode.Implied), new(0xda, 1) },
		{ ("phy", AddressingMode.Implied), new(0x5a, 1) },
		{ ("plx", AddressingMode.Implied), new(0xfa, 1) },
		{ ("ply", AddressingMode.Implied), new(0x7a, 1) },
		{ ("phb", AddressingMode.Implied), new(0x8b, 1) },
		{ ("phd", AddressingMode.Implied), new(0x0b, 1) },
		{ ("phk", AddressingMode.Implied), new(0x4b, 1) },
		{ ("plb", AddressingMode.Implied), new(0xab, 1) },
		{ ("pld", AddressingMode.Implied), new(0x2b, 1) },

		// ========================================================================
		// REP/SEP - Reset/Set Processor Status Bits
		// ========================================================================
		{ ("rep", AddressingMode.Immediate), new(0xc2, 2) },
		{ ("sep", AddressingMode.Immediate), new(0xe2, 2) },

		// ========================================================================
		// ROL - Rotate Left
		// ========================================================================
		{ ("rol", AddressingMode.Accumulator), new(0x2a, 1) },
		{ ("rol", AddressingMode.Implied), new(0x2a, 1) },
		{ ("rol", AddressingMode.ZeroPage), new(0x26, 2) },
		{ ("rol", AddressingMode.ZeroPageX), new(0x36, 2) },
		{ ("rol", AddressingMode.Absolute), new(0x2e, 3) },
		{ ("rol", AddressingMode.AbsoluteX), new(0x3e, 3) },

		// ========================================================================
		// ROR - Rotate Right
		// ========================================================================
		{ ("ror", AddressingMode.Accumulator), new(0x6a, 1) },
		{ ("ror", AddressingMode.Implied), new(0x6a, 1) },
		{ ("ror", AddressingMode.ZeroPage), new(0x66, 2) },
		{ ("ror", AddressingMode.ZeroPageX), new(0x76, 2) },
		{ ("ror", AddressingMode.Absolute), new(0x6e, 3) },
		{ ("ror", AddressingMode.AbsoluteX), new(0x7e, 3) },

		// ========================================================================
		// RTI/RTL/RTS - Return Instructions
		// ========================================================================
		{ ("rti", AddressingMode.Implied), new(0x40, 1) },
		{ ("rtl", AddressingMode.Implied), new(0x6b, 1) }, // 65816 only
		{ ("rts", AddressingMode.Implied), new(0x60, 1) },

		// ========================================================================
		// SBC - Subtract with Carry
		// ========================================================================
		{ ("sbc", AddressingMode.Immediate), new(0xe9, 2) },
		{ ("sbc", AddressingMode.ZeroPage), new(0xe5, 2) },
		{ ("sbc", AddressingMode.ZeroPageX), new(0xf5, 2) },
		{ ("sbc", AddressingMode.Absolute), new(0xed, 3) },
		{ ("sbc", AddressingMode.AbsoluteX), new(0xfd, 3) },
		{ ("sbc", AddressingMode.AbsoluteY), new(0xf9, 3) },
		{ ("sbc", AddressingMode.IndexedIndirect), new(0xe1, 2) },
		{ ("sbc", AddressingMode.IndirectIndexed), new(0xf1, 2) },
		// 65816 additions
		{ ("sbc", AddressingMode.AbsoluteLong), new(0xef, 4) },
		{ ("sbc", AddressingMode.AbsoluteLongX), new(0xff, 4) },
		{ ("sbc", AddressingMode.StackRelative), new(0xe3, 2) },
		{ ("sbc", AddressingMode.StackRelativeIndirectIndexed), new(0xf3, 2) },
		{ ("sbc", AddressingMode.DirectPageIndirectLong), new(0xe7, 2) },
		{ ("sbc", AddressingMode.DirectPageIndirectLongY), new(0xf7, 2) },

		// ========================================================================
		// STA - Store Accumulator
		// ========================================================================
		{ ("sta", AddressingMode.ZeroPage), new(0x85, 2) },
		{ ("sta", AddressingMode.ZeroPageX), new(0x95, 2) },
		{ ("sta", AddressingMode.Absolute), new(0x8d, 3) },
		{ ("sta", AddressingMode.AbsoluteX), new(0x9d, 3) },
		{ ("sta", AddressingMode.AbsoluteY), new(0x99, 3) },
		{ ("sta", AddressingMode.IndexedIndirect), new(0x81, 2) },
		{ ("sta", AddressingMode.IndirectIndexed), new(0x91, 2) },
		// 65816 additions
		{ ("sta", AddressingMode.AbsoluteLong), new(0x8f, 4) },
		{ ("sta", AddressingMode.AbsoluteLongX), new(0x9f, 4) },
		{ ("sta", AddressingMode.StackRelative), new(0x83, 2) },
		{ ("sta", AddressingMode.StackRelativeIndirectIndexed), new(0x93, 2) },
		{ ("sta", AddressingMode.DirectPageIndirectLong), new(0x87, 2) },
		{ ("sta", AddressingMode.DirectPageIndirectLongY), new(0x97, 2) },

		// ========================================================================
		// STP/WAI - Stop/Wait
		// ========================================================================
		{ ("stp", AddressingMode.Implied), new(0xdb, 1) }, // 65816 only
		{ ("wai", AddressingMode.Implied), new(0xcb, 1) }, // 65816 only

		// ========================================================================
		// STX - Store X Register
		// ========================================================================
		{ ("stx", AddressingMode.ZeroPage), new(0x86, 2) },
		{ ("stx", AddressingMode.ZeroPageY), new(0x96, 2) },
		{ ("stx", AddressingMode.Absolute), new(0x8e, 3) },

		// ========================================================================
		// STY - Store Y Register
		// ========================================================================
		{ ("sty", AddressingMode.ZeroPage), new(0x84, 2) },
		{ ("sty", AddressingMode.ZeroPageX), new(0x94, 2) },
		{ ("sty", AddressingMode.Absolute), new(0x8c, 3) },

		// ========================================================================
		// STZ - Store Zero
		// ========================================================================
		{ ("stz", AddressingMode.ZeroPage), new(0x64, 2) }, // 65816 only
		{ ("stz", AddressingMode.ZeroPageX), new(0x74, 2) },
		{ ("stz", AddressingMode.Absolute), new(0x9c, 3) },
		{ ("stz", AddressingMode.AbsoluteX), new(0x9e, 3) },

		// ========================================================================
		// Transfer Instructions
		// ========================================================================
		{ ("tax", AddressingMode.Implied), new(0xaa, 1) },
		{ ("tay", AddressingMode.Implied), new(0xa8, 1) },
		{ ("tsx", AddressingMode.Implied), new(0xba, 1) },
		{ ("txa", AddressingMode.Implied), new(0x8a, 1) },
		{ ("txs", AddressingMode.Implied), new(0x9a, 1) },
		{ ("tya", AddressingMode.Implied), new(0x98, 1) },
		// 65816 additions
		{ ("tcd", AddressingMode.Implied), new(0x5b, 1) },
		{ ("tcs", AddressingMode.Implied), new(0x1b, 1) },
		{ ("tdc", AddressingMode.Implied), new(0x7b, 1) },
		{ ("tsc", AddressingMode.Implied), new(0x3b, 1) },
		{ ("txy", AddressingMode.Implied), new(0x9b, 1) },
		{ ("tyx", AddressingMode.Implied), new(0xbb, 1) },

		// ========================================================================
		// TRB/TSB - Test and Reset/Set Bits
		// ========================================================================
		{ ("trb", AddressingMode.ZeroPage), new(0x14, 2) }, // 65816 only
		{ ("trb", AddressingMode.Absolute), new(0x1c, 3) },
		{ ("tsb", AddressingMode.ZeroPage), new(0x04, 2) },
		{ ("tsb", AddressingMode.Absolute), new(0x0c, 3) },

		// ========================================================================
		// WDM - Reserved for future use
		// ========================================================================
		{ ("wdm", AddressingMode.Implied), new(0x42, 2) },
		{ ("wdm", AddressingMode.Immediate), new(0x42, 2) },

		// ========================================================================
		// XBA - Exchange B and A
		// ========================================================================
		{ ("xba", AddressingMode.Implied), new(0xeb, 1) },

		// ========================================================================
		// XCE - Exchange Carry and Emulation
		// ========================================================================
		{ ("xce", AddressingMode.Implied), new(0xfb, 1) },
	};

	/// <summary>
	/// Tries to get the encoding for an instruction.
	/// </summary>
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
	public static IEnumerable<AddressingMode> GetSupportedModes(string mnemonic) {
		return _opcodes.Keys
			.Where(k => k.Mnemonic.Equals(mnemonic, StringComparison.OrdinalIgnoreCase))
			.Select(k => k.Mode);
	}

	/// <summary>
	/// Checks if an instruction is a branch instruction.
	/// </summary>
	public static bool IsBranchInstruction(string mnemonic) {
		return mnemonic.ToLowerInvariant() switch {
			"bcc" or "bcs" or "beq" or "bmi" or "bne" or "bpl" or "bvc" or "bvs" or "bra" or "brl" => true,
			_ => false
		};
	}

	/// <summary>
	/// Checks if an instruction is 65816-specific (not available on 6502).
	/// </summary>
	public static bool Is65816Only(string mnemonic) {
		return mnemonic.ToLowerInvariant() switch {
			"bra" or "brl" or "cop" or "jml" or "jsl" or "mvp" or "mvn" => true,
			"pea" or "pei" or "per" or "phb" or "phd" or "phk" or "phx" or "phy" => true,
			"plb" or "pld" or "plx" or "ply" or "rep" or "rtl" or "sep" => true,
			"stp" or "stz" or "tcd" or "tcs" or "tdc" or "trb" or "tsc" or "tsb" => true,
			"txy" or "tyx" or "wai" or "wdm" or "xba" or "xce" or "dea" or "ina" => true,
			_ => false
		};
	}
}

