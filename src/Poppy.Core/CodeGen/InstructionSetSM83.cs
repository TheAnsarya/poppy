// ============================================================================
// InstructionSetSM83.cs - SM83 (Game Boy) Instruction Encoding
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Parser;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Provides instruction encoding for the Sharp SM83 processor (Game Boy).
/// </summary>
/// <remarks>
/// The SM83 is a custom CPU used in the Game Boy, similar to the Z80 but with
/// differences. It has 8-bit and 16-bit operations, CB-prefixed bit manipulation
/// instructions, and Game Boy specific instructions like STOP.
/// </remarks>
public static class InstructionSetSM83 {
	/// <summary>
	/// Instruction encoding information.
	/// </summary>
	/// <param name="Opcode">The opcode byte (or CB prefix + opcode for CB instructions).</param>
	/// <param name="Size">The total instruction size in bytes.</param>
	/// <param name="IsCBPrefixed">Whether this instruction uses the CB prefix.</param>
	public readonly record struct InstructionEncoding(byte Opcode, int Size, bool IsCBPrefixed = false);

	/// <summary>
	/// SM83 addressing modes (different from 6502).
	/// </summary>
	public enum SM83AddressingMode {
		/// <summary>No operand (nop, halt).</summary>
		Implied,
		/// <summary>Single register operand (inc a, dec b).</summary>
		Register,
		/// <summary>Register pair operand (inc bc, dec hl).</summary>
		RegisterPair,
		/// <summary>8-bit immediate value (ld a, $ff).</summary>
		Immediate8,
		/// <summary>16-bit immediate value (ld bc, $1234).</summary>
		Immediate16,
		/// <summary>16-bit absolute address (ld a, ($1234)).</summary>
		Address16,
		/// <summary>Indirect through HL (ld a, (hl)).</summary>
		AddressHL,
		/// <summary>Indirect through BC (ld a, (bc)).</summary>
		AddressBC,
		/// <summary>Indirect through DE (ld a, (de)).</summary>
		AddressDE,
		/// <summary>High RAM through C register ($ff00+c).</summary>
		AddressC,
		/// <summary>Indirect through HL with post-increment (ld a, (hl+)).</summary>
		AddressHLInc,
		/// <summary>Indirect through HL with post-decrement (ld a, (hl-)).</summary>
		AddressHLDec,
		/// <summary>High RAM page address (ldh a, ($ff00+n)).</summary>
		HighAddress,
		/// <summary>Relative branch offset (jr nz, label).</summary>
		Relative,
		/// <summary>Bit number operand (bit 0, a).</summary>
		Bit,
		/// <summary>RST vector address (rst $38).</summary>
		RST,
	}

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
	/// Uses the parser's AddressingMode for compatibility with the code generator.
	/// </summary>
	private static readonly Dictionary<(string Mnemonic, AddressingMode Mode), InstructionEncoding> _opcodes = new(MnemonicComparer.Instance) {
		// ========================================================================
		// 8-Bit Load Instructions
		// ========================================================================

		// LD r, n - Load immediate to register
		{ ("ld a", AddressingMode.Immediate), new(0x3e, 2) },
		{ ("ld b", AddressingMode.Immediate), new(0x06, 2) },
		{ ("ld c", AddressingMode.Immediate), new(0x0e, 2) },
		{ ("ld d", AddressingMode.Immediate), new(0x16, 2) },
		{ ("ld e", AddressingMode.Immediate), new(0x1e, 2) },
		{ ("ld h", AddressingMode.Immediate), new(0x26, 2) },
		{ ("ld l", AddressingMode.Immediate), new(0x2e, 2) },

		// LD r, r - Register to register (implied mode for simplicity)
		{ ("ld a,a", AddressingMode.Implied), new(0x7f, 1) },
		{ ("ld a,b", AddressingMode.Implied), new(0x78, 1) },
		{ ("ld a,c", AddressingMode.Implied), new(0x79, 1) },
		{ ("ld a,d", AddressingMode.Implied), new(0x7a, 1) },
		{ ("ld a,e", AddressingMode.Implied), new(0x7b, 1) },
		{ ("ld a,h", AddressingMode.Implied), new(0x7c, 1) },
		{ ("ld a,l", AddressingMode.Implied), new(0x7d, 1) },
		{ ("ld b,a", AddressingMode.Implied), new(0x47, 1) },
		{ ("ld b,b", AddressingMode.Implied), new(0x40, 1) },
		{ ("ld b,c", AddressingMode.Implied), new(0x41, 1) },
		{ ("ld b,d", AddressingMode.Implied), new(0x42, 1) },
		{ ("ld b,e", AddressingMode.Implied), new(0x43, 1) },
		{ ("ld b,h", AddressingMode.Implied), new(0x44, 1) },
		{ ("ld b,l", AddressingMode.Implied), new(0x45, 1) },
		{ ("ld c,a", AddressingMode.Implied), new(0x4f, 1) },
		{ ("ld c,b", AddressingMode.Implied), new(0x48, 1) },
		{ ("ld c,c", AddressingMode.Implied), new(0x49, 1) },
		{ ("ld c,d", AddressingMode.Implied), new(0x4a, 1) },
		{ ("ld c,e", AddressingMode.Implied), new(0x4b, 1) },
		{ ("ld c,h", AddressingMode.Implied), new(0x4c, 1) },
		{ ("ld c,l", AddressingMode.Implied), new(0x4d, 1) },
		{ ("ld d,a", AddressingMode.Implied), new(0x57, 1) },
		{ ("ld d,b", AddressingMode.Implied), new(0x50, 1) },
		{ ("ld d,c", AddressingMode.Implied), new(0x51, 1) },
		{ ("ld d,d", AddressingMode.Implied), new(0x52, 1) },
		{ ("ld d,e", AddressingMode.Implied), new(0x53, 1) },
		{ ("ld d,h", AddressingMode.Implied), new(0x54, 1) },
		{ ("ld d,l", AddressingMode.Implied), new(0x55, 1) },
		{ ("ld e,a", AddressingMode.Implied), new(0x5f, 1) },
		{ ("ld e,b", AddressingMode.Implied), new(0x58, 1) },
		{ ("ld e,c", AddressingMode.Implied), new(0x59, 1) },
		{ ("ld e,d", AddressingMode.Implied), new(0x5a, 1) },
		{ ("ld e,e", AddressingMode.Implied), new(0x5b, 1) },
		{ ("ld e,h", AddressingMode.Implied), new(0x5c, 1) },
		{ ("ld e,l", AddressingMode.Implied), new(0x5d, 1) },
		{ ("ld h,a", AddressingMode.Implied), new(0x67, 1) },
		{ ("ld h,b", AddressingMode.Implied), new(0x60, 1) },
		{ ("ld h,c", AddressingMode.Implied), new(0x61, 1) },
		{ ("ld h,d", AddressingMode.Implied), new(0x62, 1) },
		{ ("ld h,e", AddressingMode.Implied), new(0x63, 1) },
		{ ("ld h,h", AddressingMode.Implied), new(0x64, 1) },
		{ ("ld h,l", AddressingMode.Implied), new(0x65, 1) },
		{ ("ld l,a", AddressingMode.Implied), new(0x6f, 1) },
		{ ("ld l,b", AddressingMode.Implied), new(0x68, 1) },
		{ ("ld l,c", AddressingMode.Implied), new(0x69, 1) },
		{ ("ld l,d", AddressingMode.Implied), new(0x6a, 1) },
		{ ("ld l,e", AddressingMode.Implied), new(0x6b, 1) },
		{ ("ld l,h", AddressingMode.Implied), new(0x6c, 1) },
		{ ("ld l,l", AddressingMode.Implied), new(0x6d, 1) },

		// LD r, (HL)
		{ ("ld a,(hl)", AddressingMode.Implied), new(0x7e, 1) },
		{ ("ld b,(hl)", AddressingMode.Implied), new(0x46, 1) },
		{ ("ld c,(hl)", AddressingMode.Implied), new(0x4e, 1) },
		{ ("ld d,(hl)", AddressingMode.Implied), new(0x56, 1) },
		{ ("ld e,(hl)", AddressingMode.Implied), new(0x5e, 1) },
		{ ("ld h,(hl)", AddressingMode.Implied), new(0x66, 1) },
		{ ("ld l,(hl)", AddressingMode.Implied), new(0x6e, 1) },

		// LD (HL), r
		{ ("ld (hl),a", AddressingMode.Implied), new(0x77, 1) },
		{ ("ld (hl),b", AddressingMode.Implied), new(0x70, 1) },
		{ ("ld (hl),c", AddressingMode.Implied), new(0x71, 1) },
		{ ("ld (hl),d", AddressingMode.Implied), new(0x72, 1) },
		{ ("ld (hl),e", AddressingMode.Implied), new(0x73, 1) },
		{ ("ld (hl),h", AddressingMode.Implied), new(0x74, 1) },
		{ ("ld (hl),l", AddressingMode.Implied), new(0x75, 1) },
		{ ("ld (hl)", AddressingMode.Immediate), new(0x36, 2) },

		// LD A, (nn) / LD (nn), A
		{ ("ld a,(bc)", AddressingMode.Implied), new(0x0a, 1) },
		{ ("ld a,(de)", AddressingMode.Implied), new(0x1a, 1) },
		{ ("ld (bc),a", AddressingMode.Implied), new(0x02, 1) },
		{ ("ld (de),a", AddressingMode.Implied), new(0x12, 1) },
		{ ("ld a", AddressingMode.Absolute), new(0xfa, 3) },
		{ ("ld", AddressingMode.Absolute), new(0xea, 3) }, // ld (nn), a

		// LD A, (HL+/-) / LD (HL+/-), A
		{ ("ld a,(hl+)", AddressingMode.Implied), new(0x2a, 1) },
		{ ("ld a,(hli)", AddressingMode.Implied), new(0x2a, 1) },
		{ ("ld a,(hl-)", AddressingMode.Implied), new(0x3a, 1) },
		{ ("ld a,(hld)", AddressingMode.Implied), new(0x3a, 1) },
		{ ("ld (hl+),a", AddressingMode.Implied), new(0x22, 1) },
		{ ("ld (hli),a", AddressingMode.Implied), new(0x22, 1) },
		{ ("ld (hl-),a", AddressingMode.Implied), new(0x32, 1) },
		{ ("ld (hld),a", AddressingMode.Implied), new(0x32, 1) },
		{ ("ldi a,(hl)", AddressingMode.Implied), new(0x2a, 1) },
		{ ("ldi (hl),a", AddressingMode.Implied), new(0x22, 1) },
		{ ("ldd a,(hl)", AddressingMode.Implied), new(0x3a, 1) },
		{ ("ldd (hl),a", AddressingMode.Implied), new(0x32, 1) },

		// LDH - High RAM access ($ff00+n)
		{ ("ldh a", AddressingMode.Immediate), new(0xf0, 2) },
		{ ("ldh", AddressingMode.Immediate), new(0xe0, 2) }, // ldh (n), a
		{ ("ld a,(c)", AddressingMode.Implied), new(0xf2, 1) },
		{ ("ldh a,(c)", AddressingMode.Implied), new(0xf2, 1) },
		{ ("ld (c),a", AddressingMode.Implied), new(0xe2, 1) },
		{ ("ldh (c),a", AddressingMode.Implied), new(0xe2, 1) },

		// ========================================================================
		// 16-Bit Load Instructions
		// ========================================================================

		// LD rr, nn
		{ ("ld bc", AddressingMode.Immediate), new(0x01, 3) },
		{ ("ld de", AddressingMode.Immediate), new(0x11, 3) },
		{ ("ld hl", AddressingMode.Immediate), new(0x21, 3) },
		{ ("ld sp", AddressingMode.Immediate), new(0x31, 3) },

		// LD SP, HL
		{ ("ld sp,hl", AddressingMode.Implied), new(0xf9, 1) },

		// LD HL, SP+n
		{ ("ld hl,sp", AddressingMode.Immediate), new(0xf8, 2) },

		// LD (nn), SP
		{ ("ld (nn),sp", AddressingMode.Absolute), new(0x08, 3) },

		// PUSH/POP
		{ ("push bc", AddressingMode.Implied), new(0xc5, 1) },
		{ ("push de", AddressingMode.Implied), new(0xd5, 1) },
		{ ("push hl", AddressingMode.Implied), new(0xe5, 1) },
		{ ("push af", AddressingMode.Implied), new(0xf5, 1) },
		{ ("pop bc", AddressingMode.Implied), new(0xc1, 1) },
		{ ("pop de", AddressingMode.Implied), new(0xd1, 1) },
		{ ("pop hl", AddressingMode.Implied), new(0xe1, 1) },
		{ ("pop af", AddressingMode.Implied), new(0xf1, 1) },

		// ========================================================================
		// 8-Bit Arithmetic/Logic Instructions
		// ========================================================================

		// ADD A, r/n/(HL)
		{ ("add a,a", AddressingMode.Implied), new(0x87, 1) },
		{ ("add a,b", AddressingMode.Implied), new(0x80, 1) },
		{ ("add a,c", AddressingMode.Implied), new(0x81, 1) },
		{ ("add a,d", AddressingMode.Implied), new(0x82, 1) },
		{ ("add a,e", AddressingMode.Implied), new(0x83, 1) },
		{ ("add a,h", AddressingMode.Implied), new(0x84, 1) },
		{ ("add a,l", AddressingMode.Implied), new(0x85, 1) },
		{ ("add a,(hl)", AddressingMode.Implied), new(0x86, 1) },
		{ ("add a", AddressingMode.Immediate), new(0xc6, 2) },
		{ ("add", AddressingMode.Immediate), new(0xc6, 2) },

		// ADC A, r/n/(HL)
		{ ("adc a,a", AddressingMode.Implied), new(0x8f, 1) },
		{ ("adc a,b", AddressingMode.Implied), new(0x88, 1) },
		{ ("adc a,c", AddressingMode.Implied), new(0x89, 1) },
		{ ("adc a,d", AddressingMode.Implied), new(0x8a, 1) },
		{ ("adc a,e", AddressingMode.Implied), new(0x8b, 1) },
		{ ("adc a,h", AddressingMode.Implied), new(0x8c, 1) },
		{ ("adc a,l", AddressingMode.Implied), new(0x8d, 1) },
		{ ("adc a,(hl)", AddressingMode.Implied), new(0x8e, 1) },
		{ ("adc a", AddressingMode.Immediate), new(0xce, 2) },
		{ ("adc", AddressingMode.Immediate), new(0xce, 2) },

		// SUB r/n/(HL)
		{ ("sub a", AddressingMode.Implied), new(0x97, 1) },
		{ ("sub b", AddressingMode.Implied), new(0x90, 1) },
		{ ("sub c", AddressingMode.Implied), new(0x91, 1) },
		{ ("sub d", AddressingMode.Implied), new(0x92, 1) },
		{ ("sub e", AddressingMode.Implied), new(0x93, 1) },
		{ ("sub h", AddressingMode.Implied), new(0x94, 1) },
		{ ("sub l", AddressingMode.Implied), new(0x95, 1) },
		{ ("sub (hl)", AddressingMode.Implied), new(0x96, 1) },
		{ ("sub", AddressingMode.Immediate), new(0xd6, 2) },

		// SBC A, r/n/(HL)
		{ ("sbc a,a", AddressingMode.Implied), new(0x9f, 1) },
		{ ("sbc a,b", AddressingMode.Implied), new(0x98, 1) },
		{ ("sbc a,c", AddressingMode.Implied), new(0x99, 1) },
		{ ("sbc a,d", AddressingMode.Implied), new(0x9a, 1) },
		{ ("sbc a,e", AddressingMode.Implied), new(0x9b, 1) },
		{ ("sbc a,h", AddressingMode.Implied), new(0x9c, 1) },
		{ ("sbc a,l", AddressingMode.Implied), new(0x9d, 1) },
		{ ("sbc a,(hl)", AddressingMode.Implied), new(0x9e, 1) },
		{ ("sbc a", AddressingMode.Immediate), new(0xde, 2) },
		{ ("sbc", AddressingMode.Immediate), new(0xde, 2) },

		// AND r/n/(HL)
		{ ("and a", AddressingMode.Implied), new(0xa7, 1) },
		{ ("and b", AddressingMode.Implied), new(0xa0, 1) },
		{ ("and c", AddressingMode.Implied), new(0xa1, 1) },
		{ ("and d", AddressingMode.Implied), new(0xa2, 1) },
		{ ("and e", AddressingMode.Implied), new(0xa3, 1) },
		{ ("and h", AddressingMode.Implied), new(0xa4, 1) },
		{ ("and l", AddressingMode.Implied), new(0xa5, 1) },
		{ ("and (hl)", AddressingMode.Implied), new(0xa6, 1) },
		{ ("and", AddressingMode.Immediate), new(0xe6, 2) },

		// OR r/n/(HL)
		{ ("or a", AddressingMode.Implied), new(0xb7, 1) },
		{ ("or b", AddressingMode.Implied), new(0xb0, 1) },
		{ ("or c", AddressingMode.Implied), new(0xb1, 1) },
		{ ("or d", AddressingMode.Implied), new(0xb2, 1) },
		{ ("or e", AddressingMode.Implied), new(0xb3, 1) },
		{ ("or h", AddressingMode.Implied), new(0xb4, 1) },
		{ ("or l", AddressingMode.Implied), new(0xb5, 1) },
		{ ("or (hl)", AddressingMode.Implied), new(0xb6, 1) },
		{ ("or", AddressingMode.Immediate), new(0xf6, 2) },

		// XOR r/n/(HL)
		{ ("xor a", AddressingMode.Implied), new(0xaf, 1) },
		{ ("xor b", AddressingMode.Implied), new(0xa8, 1) },
		{ ("xor c", AddressingMode.Implied), new(0xa9, 1) },
		{ ("xor d", AddressingMode.Implied), new(0xaa, 1) },
		{ ("xor e", AddressingMode.Implied), new(0xab, 1) },
		{ ("xor h", AddressingMode.Implied), new(0xac, 1) },
		{ ("xor l", AddressingMode.Implied), new(0xad, 1) },
		{ ("xor (hl)", AddressingMode.Implied), new(0xae, 1) },
		{ ("xor", AddressingMode.Immediate), new(0xee, 2) },

		// CP r/n/(HL)
		{ ("cp a", AddressingMode.Implied), new(0xbf, 1) },
		{ ("cp b", AddressingMode.Implied), new(0xb8, 1) },
		{ ("cp c", AddressingMode.Implied), new(0xb9, 1) },
		{ ("cp d", AddressingMode.Implied), new(0xba, 1) },
		{ ("cp e", AddressingMode.Implied), new(0xbb, 1) },
		{ ("cp h", AddressingMode.Implied), new(0xbc, 1) },
		{ ("cp l", AddressingMode.Implied), new(0xbd, 1) },
		{ ("cp (hl)", AddressingMode.Implied), new(0xbe, 1) },
		{ ("cp", AddressingMode.Immediate), new(0xfe, 2) },

		// INC/DEC r/(HL)
		{ ("inc a", AddressingMode.Implied), new(0x3c, 1) },
		{ ("inc b", AddressingMode.Implied), new(0x04, 1) },
		{ ("inc c", AddressingMode.Implied), new(0x0c, 1) },
		{ ("inc d", AddressingMode.Implied), new(0x14, 1) },
		{ ("inc e", AddressingMode.Implied), new(0x1c, 1) },
		{ ("inc h", AddressingMode.Implied), new(0x24, 1) },
		{ ("inc l", AddressingMode.Implied), new(0x2c, 1) },
		{ ("inc (hl)", AddressingMode.Implied), new(0x34, 1) },
		{ ("dec a", AddressingMode.Implied), new(0x3d, 1) },
		{ ("dec b", AddressingMode.Implied), new(0x05, 1) },
		{ ("dec c", AddressingMode.Implied), new(0x0d, 1) },
		{ ("dec d", AddressingMode.Implied), new(0x15, 1) },
		{ ("dec e", AddressingMode.Implied), new(0x1d, 1) },
		{ ("dec h", AddressingMode.Implied), new(0x25, 1) },
		{ ("dec l", AddressingMode.Implied), new(0x2d, 1) },
		{ ("dec (hl)", AddressingMode.Implied), new(0x35, 1) },

		// ========================================================================
		// 16-Bit Arithmetic
		// ========================================================================

		// ADD HL, rr
		{ ("add hl,bc", AddressingMode.Implied), new(0x09, 1) },
		{ ("add hl,de", AddressingMode.Implied), new(0x19, 1) },
		{ ("add hl,hl", AddressingMode.Implied), new(0x29, 1) },
		{ ("add hl,sp", AddressingMode.Implied), new(0x39, 1) },

		// ADD SP, n
		{ ("add sp", AddressingMode.Immediate), new(0xe8, 2) },

		// INC/DEC rr
		{ ("inc bc", AddressingMode.Implied), new(0x03, 1) },
		{ ("inc de", AddressingMode.Implied), new(0x13, 1) },
		{ ("inc hl", AddressingMode.Implied), new(0x23, 1) },
		{ ("inc sp", AddressingMode.Implied), new(0x33, 1) },
		{ ("dec bc", AddressingMode.Implied), new(0x0b, 1) },
		{ ("dec de", AddressingMode.Implied), new(0x1b, 1) },
		{ ("dec hl", AddressingMode.Implied), new(0x2b, 1) },
		{ ("dec sp", AddressingMode.Implied), new(0x3b, 1) },

		// ========================================================================
		// Rotate/Shift Instructions (non-CB prefixed)
		// ========================================================================

		{ ("rlca", AddressingMode.Implied), new(0x07, 1) },
		{ ("rla", AddressingMode.Implied), new(0x17, 1) },
		{ ("rrca", AddressingMode.Implied), new(0x0f, 1) },
		{ ("rra", AddressingMode.Implied), new(0x1f, 1) },

		// ========================================================================
		// Miscellaneous Instructions
		// ========================================================================

		{ ("nop", AddressingMode.Implied), new(0x00, 1) },
		{ ("halt", AddressingMode.Implied), new(0x76, 1) },
		{ ("stop", AddressingMode.Implied), new(0x10, 2) }, // STOP 0
		{ ("di", AddressingMode.Implied), new(0xf3, 1) },
		{ ("ei", AddressingMode.Implied), new(0xfb, 1) },
		{ ("ccf", AddressingMode.Implied), new(0x3f, 1) },
		{ ("scf", AddressingMode.Implied), new(0x37, 1) },
		{ ("daa", AddressingMode.Implied), new(0x27, 1) },
		{ ("cpl", AddressingMode.Implied), new(0x2f, 1) },

		// ========================================================================
		// Jump Instructions
		// ========================================================================

		// JP nn
		{ ("jp", AddressingMode.Absolute), new(0xc3, 3) },
		{ ("jp (hl)", AddressingMode.Implied), new(0xe9, 1) },

		// JP cc, nn
		{ ("jp nz", AddressingMode.Absolute), new(0xc2, 3) },
		{ ("jp z", AddressingMode.Absolute), new(0xca, 3) },
		{ ("jp nc", AddressingMode.Absolute), new(0xd2, 3) },
		{ ("jp c", AddressingMode.Absolute), new(0xda, 3) },

		// JR n (relative)
		{ ("jr", AddressingMode.Relative), new(0x18, 2) },
		{ ("jr", AddressingMode.Absolute), new(0x18, 2) },

		// JR cc, n
		{ ("jr nz", AddressingMode.Relative), new(0x20, 2) },
		{ ("jr nz", AddressingMode.Absolute), new(0x20, 2) },
		{ ("jr z", AddressingMode.Relative), new(0x28, 2) },
		{ ("jr z", AddressingMode.Absolute), new(0x28, 2) },
		{ ("jr nc", AddressingMode.Relative), new(0x30, 2) },
		{ ("jr nc", AddressingMode.Absolute), new(0x30, 2) },
		{ ("jr c", AddressingMode.Relative), new(0x38, 2) },
		{ ("jr c", AddressingMode.Absolute), new(0x38, 2) },

		// ========================================================================
		// Call/Return Instructions
		// ========================================================================

		// CALL nn
		{ ("call", AddressingMode.Absolute), new(0xcd, 3) },

		// CALL cc, nn
		{ ("call nz", AddressingMode.Absolute), new(0xc4, 3) },
		{ ("call z", AddressingMode.Absolute), new(0xcc, 3) },
		{ ("call nc", AddressingMode.Absolute), new(0xd4, 3) },
		{ ("call c", AddressingMode.Absolute), new(0xdc, 3) },

		// RET
		{ ("ret", AddressingMode.Implied), new(0xc9, 1) },
		{ ("reti", AddressingMode.Implied), new(0xd9, 1) },

		// RET cc
		{ ("ret nz", AddressingMode.Implied), new(0xc0, 1) },
		{ ("ret z", AddressingMode.Implied), new(0xc8, 1) },
		{ ("ret nc", AddressingMode.Implied), new(0xd0, 1) },
		{ ("ret c", AddressingMode.Implied), new(0xd8, 1) },

		// RST n
		{ ("rst $00", AddressingMode.Implied), new(0xc7, 1) },
		{ ("rst $08", AddressingMode.Implied), new(0xcf, 1) },
		{ ("rst $10", AddressingMode.Implied), new(0xd7, 1) },
		{ ("rst $18", AddressingMode.Implied), new(0xdf, 1) },
		{ ("rst $20", AddressingMode.Implied), new(0xe7, 1) },
		{ ("rst $28", AddressingMode.Implied), new(0xef, 1) },
		{ ("rst $30", AddressingMode.Implied), new(0xf7, 1) },
		{ ("rst $38", AddressingMode.Implied), new(0xff, 1) },
	};

	/// <summary>
	/// CB-prefixed instructions (bit manipulation, rotates, shifts).
	/// All are 2 bytes: CB + opcode.
	/// </summary>
	private static readonly Dictionary<string, InstructionEncoding> _cbOpcodes = new(StringComparer.OrdinalIgnoreCase) {
		// RLC r/(HL)
		{ "rlc b", new(0x00, 2, true) },
		{ "rlc c", new(0x01, 2, true) },
		{ "rlc d", new(0x02, 2, true) },
		{ "rlc e", new(0x03, 2, true) },
		{ "rlc h", new(0x04, 2, true) },
		{ "rlc l", new(0x05, 2, true) },
		{ "rlc (hl)", new(0x06, 2, true) },
		{ "rlc a", new(0x07, 2, true) },

		// RRC r/(HL)
		{ "rrc b", new(0x08, 2, true) },
		{ "rrc c", new(0x09, 2, true) },
		{ "rrc d", new(0x0a, 2, true) },
		{ "rrc e", new(0x0b, 2, true) },
		{ "rrc h", new(0x0c, 2, true) },
		{ "rrc l", new(0x0d, 2, true) },
		{ "rrc (hl)", new(0x0e, 2, true) },
		{ "rrc a", new(0x0f, 2, true) },

		// RL r/(HL)
		{ "rl b", new(0x10, 2, true) },
		{ "rl c", new(0x11, 2, true) },
		{ "rl d", new(0x12, 2, true) },
		{ "rl e", new(0x13, 2, true) },
		{ "rl h", new(0x14, 2, true) },
		{ "rl l", new(0x15, 2, true) },
		{ "rl (hl)", new(0x16, 2, true) },
		{ "rl a", new(0x17, 2, true) },

		// RR r/(HL)
		{ "rr b", new(0x18, 2, true) },
		{ "rr c", new(0x19, 2, true) },
		{ "rr d", new(0x1a, 2, true) },
		{ "rr e", new(0x1b, 2, true) },
		{ "rr h", new(0x1c, 2, true) },
		{ "rr l", new(0x1d, 2, true) },
		{ "rr (hl)", new(0x1e, 2, true) },
		{ "rr a", new(0x1f, 2, true) },

		// SLA r/(HL)
		{ "sla b", new(0x20, 2, true) },
		{ "sla c", new(0x21, 2, true) },
		{ "sla d", new(0x22, 2, true) },
		{ "sla e", new(0x23, 2, true) },
		{ "sla h", new(0x24, 2, true) },
		{ "sla l", new(0x25, 2, true) },
		{ "sla (hl)", new(0x26, 2, true) },
		{ "sla a", new(0x27, 2, true) },

		// SRA r/(HL)
		{ "sra b", new(0x28, 2, true) },
		{ "sra c", new(0x29, 2, true) },
		{ "sra d", new(0x2a, 2, true) },
		{ "sra e", new(0x2b, 2, true) },
		{ "sra h", new(0x2c, 2, true) },
		{ "sra l", new(0x2d, 2, true) },
		{ "sra (hl)", new(0x2e, 2, true) },
		{ "sra a", new(0x2f, 2, true) },

		// SWAP r/(HL)
		{ "swap b", new(0x30, 2, true) },
		{ "swap c", new(0x31, 2, true) },
		{ "swap d", new(0x32, 2, true) },
		{ "swap e", new(0x33, 2, true) },
		{ "swap h", new(0x34, 2, true) },
		{ "swap l", new(0x35, 2, true) },
		{ "swap (hl)", new(0x36, 2, true) },
		{ "swap a", new(0x37, 2, true) },

		// SRL r/(HL)
		{ "srl b", new(0x38, 2, true) },
		{ "srl c", new(0x39, 2, true) },
		{ "srl d", new(0x3a, 2, true) },
		{ "srl e", new(0x3b, 2, true) },
		{ "srl h", new(0x3c, 2, true) },
		{ "srl l", new(0x3d, 2, true) },
		{ "srl (hl)", new(0x3e, 2, true) },
		{ "srl a", new(0x3f, 2, true) },

		// BIT 0-7, r/(HL) - generated programmatically below
		// SET 0-7, r/(HL) - generated programmatically below
		// RES 0-7, r/(HL) - generated programmatically below
	};

	/// <summary>
	/// Static constructor to populate BIT/SET/RES instructions.
	/// </summary>
	static InstructionSetSM83() {
		string[] registers = ["b", "c", "d", "e", "h", "l", "(hl)", "a"];

		for (int bit = 0; bit < 8; bit++) {
			for (int reg = 0; reg < 8; reg++) {
				// BIT b, r: opcode = 0x40 + bit*8 + reg
				_cbOpcodes[$"bit {bit},{registers[reg]}"] = new((byte)(0x40 + bit * 8 + reg), 2, true);

				// RES b, r: opcode = 0x80 + bit*8 + reg
				_cbOpcodes[$"res {bit},{registers[reg]}"] = new((byte)(0x80 + bit * 8 + reg), 2, true);

				// SET b, r: opcode = 0xc0 + bit*8 + reg
				_cbOpcodes[$"set {bit},{registers[reg]}"] = new((byte)(0xc0 + bit * 8 + reg), 2, true);
			}
		}
	}

	/// <summary>
	/// Tries to get the encoding for an instruction.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic (may include operands like "ld a,b").</param>
	/// <param name="mode">The addressing mode.</param>
	/// <param name="encoding">The encoding if found.</param>
	/// <returns>True if the encoding was found.</returns>
	public static bool TryGetEncoding(string mnemonic, AddressingMode mode, out InstructionEncoding encoding) {
		// First check CB-prefixed instructions (full mnemonic match)
		if (_cbOpcodes.TryGetValue(mnemonic, out encoding)) {
			return true;
		}

		// Then check regular opcodes
		return _opcodes.TryGetValue((mnemonic, mode), out encoding);
	}

	/// <summary>
	/// Gets all supported mnemonics.
	/// </summary>
	public static IEnumerable<string> GetAllMnemonics() {
		var regular = _opcodes.Keys.Select(k => k.Mnemonic);
		var cb = _cbOpcodes.Keys;
		return regular.Concat(cb).Distinct(StringComparer.OrdinalIgnoreCase);
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

	/// <summary>
	/// Checks if an instruction is a relative branch (JR).
	/// </summary>
	public static bool IsRelativeBranch(string mnemonic) {
		var lower = mnemonic.ToLowerInvariant();
		return lower.StartsWith("jr");
	}
}

