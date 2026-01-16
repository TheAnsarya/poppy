// ============================================================================
// InstructionSetZ80.cs - Zilog Z80 Instruction Encoding
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================
// The Z80 is an 8-bit processor used in the Sega Master System and Game Gear.
// It features an 8-bit data bus, 16-bit address bus, and many registers
// including the shadow registers (AF', BC', DE', HL').
// ============================================================================

using Poppy.Core.Parser;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Provides instruction encoding for the Zilog Z80 processor (Sega Master System/Game Gear).
/// The Z80 is an enhanced 8080-compatible processor with many additional features.
/// </summary>
public static class InstructionSetZ80 {
	/// <summary>
	/// Instruction encoding information for Z80.
	/// </summary>
	/// <param name="Prefix">Optional prefix byte(s) (CB, DD, ED, FD). Empty if none.</param>
	/// <param name="Opcode">The opcode byte.</param>
	/// <param name="Size">Total instruction size in bytes including prefix and operands.</param>
	public readonly record struct InstructionEncoding(byte[] Prefix, byte Opcode, int Size);

	/// <summary>
	/// Z80-specific addressing modes.
	/// </summary>
	public enum Z80AddressingMode {
		/// <summary>Implied (no operand)</summary>
		Implied,

		/// <summary>8-bit register direct (A, B, C, D, E, H, L)</summary>
		Register8,

		/// <summary>16-bit register direct (BC, DE, HL, SP, AF)</summary>
		Register16,

		/// <summary>Register indirect via HL ((HL))</summary>
		RegisterIndirectHL,

		/// <summary>Register indirect via BC ((BC))</summary>
		RegisterIndirectBC,

		/// <summary>Register indirect via DE ((DE))</summary>
		RegisterIndirectDE,

		/// <summary>Register indirect via SP ((SP))</summary>
		RegisterIndirectSP,

		/// <summary>Index register indirect (IX+d) or (IY+d)</summary>
		IndexedIndirect,

		/// <summary>Immediate 8-bit value</summary>
		Immediate8,

		/// <summary>Immediate 16-bit value</summary>
		Immediate16,

		/// <summary>Extended address (direct 16-bit address)</summary>
		Extended,

		/// <summary>Relative offset (for JR, DJNZ)</summary>
		Relative,

		/// <summary>Bit number (0-7)</summary>
		Bit,

		/// <summary>I/O port (in/out)</summary>
		Port,

		/// <summary>Restart vector (RST)</summary>
		Restart,

		/// <summary>Interrupt mode (0, 1, 2)</summary>
		InterruptMode,

		/// <summary>Condition code (Z, NZ, C, NC, PO, PE, P, M)</summary>
		Condition,
	}

	/// <summary>
	/// Z80 prefix bytes for extended instructions.
	/// </summary>
	public static class Prefixes {
		/// <summary>CB prefix for bit manipulation and rotates.</summary>
		public const byte CB = 0xcb;
		/// <summary>DD prefix for IX-indexed instructions.</summary>
		public const byte DD = 0xdd;
		/// <summary>ED prefix for extended instructions.</summary>
		public const byte ED = 0xed;
		/// <summary>FD prefix for IY-indexed instructions.</summary>
		public const byte FD = 0xfd;
	}

	/// <summary>
	/// 8-bit register encoding (bits 2-0 or 5-3).
	/// </summary>
	public static class Reg8 {
		/// <summary>B register</summary>
		public const int B = 0;
		/// <summary>C register</summary>
		public const int C = 1;
		/// <summary>D register</summary>
		public const int D = 2;
		/// <summary>E register</summary>
		public const int E = 3;
		/// <summary>H register</summary>
		public const int H = 4;
		/// <summary>L register</summary>
		public const int L = 5;
		/// <summary>(HL) memory reference</summary>
		public const int M = 6;  // (HL)
		/// <summary>A register (accumulator)</summary>
		public const int A = 7;
	}

	/// <summary>
	/// 16-bit register pair encoding.
	/// </summary>
	public static class Reg16 {
		/// <summary>BC register pair</summary>
		public const int BC = 0;
		/// <summary>DE register pair</summary>
		public const int DE = 1;
		/// <summary>HL register pair</summary>
		public const int HL = 2;
		/// <summary>SP (stack pointer) or AF depending on context</summary>
		public const int SP = 3;
		/// <summary>AF register pair (for PUSH/POP)</summary>
		public const int AF = 3;
	}

	/// <summary>
	/// Condition code encoding for JP, JR, CALL, RET.
	/// </summary>
	public static class Conditions {
		/// <summary>Not Zero</summary>
		public const int NZ = 0;
		/// <summary>Zero</summary>
		public const int Z = 1;
		/// <summary>No Carry</summary>
		public const int NC = 2;
		/// <summary>Carry</summary>
		public const int C = 3;
		/// <summary>Parity Odd (P/V = 0)</summary>
		public const int PO = 4;
		/// <summary>Parity Even (P/V = 1)</summary>
		public const int PE = 5;
		/// <summary>Sign Positive (S = 0)</summary>
		public const int P = 6;
		/// <summary>Sign Negative (S = 1)</summary>
		public const int M = 7;
	}

	/// <summary>
	/// Maps register names to their encoding values.
	/// </summary>
	public static readonly Dictionary<string, (int Encoding, bool Is16Bit)> RegisterMap = new(StringComparer.OrdinalIgnoreCase) {
		// 8-bit registers
		{ "a", (Reg8.A, false) },
		{ "b", (Reg8.B, false) },
		{ "c", (Reg8.C, false) },
		{ "d", (Reg8.D, false) },
		{ "e", (Reg8.E, false) },
		{ "h", (Reg8.H, false) },
		{ "l", (Reg8.L, false) },

		// 16-bit register pairs
		{ "af", (Reg16.AF, true) },
		{ "bc", (Reg16.BC, true) },
		{ "de", (Reg16.DE, true) },
		{ "hl", (Reg16.HL, true) },
		{ "sp", (Reg16.SP, true) },

		// Index registers (special handling required)
		{ "ix", (-1, true) },
		{ "iy", (-2, true) },
		{ "ixh", (-3, false) },  // Undocumented
		{ "ixl", (-4, false) },  // Undocumented
		{ "iyh", (-5, false) },  // Undocumented
		{ "iyl", (-6, false) },  // Undocumented

		// Special registers
		{ "i", (-7, false) },    // Interrupt vector
		{ "r", (-8, false) },    // Memory refresh

		// Shadow registers (prime notation)
		{ "af'", (-9, true) },
	};

	/// <summary>
	/// Maps condition names to their encoding values.
	/// </summary>
	public static readonly Dictionary<string, int> ConditionMap = new(StringComparer.OrdinalIgnoreCase) {
		{ "nz", Conditions.NZ },
		{ "z", Conditions.Z },
		{ "nc", Conditions.NC },
		{ "c", Conditions.C },
		{ "po", Conditions.PO },
		{ "pe", Conditions.PE },
		{ "p", Conditions.P },
		{ "m", Conditions.M },
	};

	/// <summary>
	/// Custom comparer for case-insensitive lookup.
	/// </summary>
	private sealed class MnemonicComparer : IEqualityComparer<(string Mnemonic, Z80AddressingMode Mode)> {
		public static readonly MnemonicComparer Instance = new();

		public bool Equals((string Mnemonic, Z80AddressingMode Mode) x, (string Mnemonic, Z80AddressingMode Mode) y) {
			return string.Equals(x.Mnemonic, y.Mnemonic, StringComparison.OrdinalIgnoreCase) && x.Mode == y.Mode;
		}

		public int GetHashCode((string Mnemonic, Z80AddressingMode Mode) obj) {
			return HashCode.Combine(obj.Mnemonic.ToLowerInvariant(), obj.Mode);
		}
	}

	/// <summary>
	/// Basic (unprefixed) opcode lookup table.
	/// </summary>
	private static readonly Dictionary<(string, Z80AddressingMode), InstructionEncoding> _basicOpcodes = new(MnemonicComparer.Instance) {
		// =========================================================================
		// 8-Bit Load Instructions
		// =========================================================================

		// LD r, r' - Load register from register (base opcode $40)
		// Actual opcode = $40 | (dst << 3) | src
		{ ("ld", Z80AddressingMode.Register8), new([], 0x40, 1) },

		// LD r, n - Load register with immediate
		// Actual opcode = $06 | (reg << 3)
		{ ("ld", Z80AddressingMode.Immediate8), new([], 0x06, 2) },

		// LD r, (HL) - Load register from (HL)
		// Actual opcode = $46 | (reg << 3)
		{ ("ld", Z80AddressingMode.RegisterIndirectHL), new([], 0x46, 1) },

		// LD (HL), r - Store register to (HL)
		// Opcode = $70 | reg

		// LD (HL), n - Store immediate to (HL)
		{ ("ld (hl)", Z80AddressingMode.Immediate8), new([], 0x36, 2) },

		// LD A, (BC) - Load A from (BC)
		{ ("ld a,(bc)", Z80AddressingMode.Implied), new([], 0x0a, 1) },

		// LD A, (DE) - Load A from (DE)
		{ ("ld a,(de)", Z80AddressingMode.Implied), new([], 0x1a, 1) },

		// LD A, (nn) - Load A from direct address
		{ ("ld a", Z80AddressingMode.Extended), new([], 0x3a, 3) },

		// LD (BC), A - Store A to (BC)
		{ ("ld (bc),a", Z80AddressingMode.Implied), new([], 0x02, 1) },

		// LD (DE), A - Store A to (DE)
		{ ("ld (de),a", Z80AddressingMode.Implied), new([], 0x12, 1) },

		// LD (nn), A - Store A to direct address
		{ ("ld", Z80AddressingMode.Extended), new([], 0x32, 3) },

		// =========================================================================
		// 16-Bit Load Instructions
		// =========================================================================

		// LD rr, nn - Load register pair with immediate
		// Opcode = $01 | (rr << 4)
		{ ("ld", Z80AddressingMode.Immediate16), new([], 0x01, 3) },

		// LD HL, (nn) - Load HL from direct address
		{ ("ld hl", Z80AddressingMode.Extended), new([], 0x2a, 3) },

		// LD (nn), HL - Store HL to direct address
		{ ("ld (nn),hl", Z80AddressingMode.Implied), new([], 0x22, 3) },

		// LD SP, HL
		{ ("ld sp,hl", Z80AddressingMode.Implied), new([], 0xf9, 1) },

		// PUSH/POP
		// PUSH rr - Opcode = $c5 | (rr << 4)
		{ ("push", Z80AddressingMode.Register16), new([], 0xc5, 1) },
		// POP rr - Opcode = $c1 | (rr << 4)
		{ ("pop", Z80AddressingMode.Register16), new([], 0xc1, 1) },

		// =========================================================================
		// Exchange Instructions
		// =========================================================================

		{ ("ex de,hl", Z80AddressingMode.Implied), new([], 0xeb, 1) },
		{ ("ex af,af'", Z80AddressingMode.Implied), new([], 0x08, 1) },
		{ ("exx", Z80AddressingMode.Implied), new([], 0xd9, 1) },
		{ ("ex (sp),hl", Z80AddressingMode.Implied), new([], 0xe3, 1) },

		// =========================================================================
		// 8-Bit Arithmetic/Logic
		// =========================================================================

		// ADD A, r - Opcode = $80 | reg
		{ ("add a", Z80AddressingMode.Register8), new([], 0x80, 1) },
		{ ("add a", Z80AddressingMode.Immediate8), new([], 0xc6, 2) },
		{ ("add a,(hl)", Z80AddressingMode.Implied), new([], 0x86, 1) },

		// ADC A, r - Opcode = $88 | reg
		{ ("adc a", Z80AddressingMode.Register8), new([], 0x88, 1) },
		{ ("adc a", Z80AddressingMode.Immediate8), new([], 0xce, 2) },
		{ ("adc a,(hl)", Z80AddressingMode.Implied), new([], 0x8e, 1) },

		// SUB r - Opcode = $90 | reg
		{ ("sub", Z80AddressingMode.Register8), new([], 0x90, 1) },
		{ ("sub", Z80AddressingMode.Immediate8), new([], 0xd6, 2) },
		{ ("sub (hl)", Z80AddressingMode.Implied), new([], 0x96, 1) },

		// SBC A, r - Opcode = $98 | reg
		{ ("sbc a", Z80AddressingMode.Register8), new([], 0x98, 1) },
		{ ("sbc a", Z80AddressingMode.Immediate8), new([], 0xde, 2) },
		{ ("sbc a,(hl)", Z80AddressingMode.Implied), new([], 0x9e, 1) },

		// AND r - Opcode = $a0 | reg
		{ ("and", Z80AddressingMode.Register8), new([], 0xa0, 1) },
		{ ("and", Z80AddressingMode.Immediate8), new([], 0xe6, 2) },
		{ ("and (hl)", Z80AddressingMode.Implied), new([], 0xa6, 1) },

		// XOR r - Opcode = $a8 | reg
		{ ("xor", Z80AddressingMode.Register8), new([], 0xa8, 1) },
		{ ("xor", Z80AddressingMode.Immediate8), new([], 0xee, 2) },
		{ ("xor (hl)", Z80AddressingMode.Implied), new([], 0xae, 1) },

		// OR r - Opcode = $b0 | reg
		{ ("or", Z80AddressingMode.Register8), new([], 0xb0, 1) },
		{ ("or", Z80AddressingMode.Immediate8), new([], 0xf6, 2) },
		{ ("or (hl)", Z80AddressingMode.Implied), new([], 0xb6, 1) },

		// CP r - Opcode = $b8 | reg
		{ ("cp", Z80AddressingMode.Register8), new([], 0xb8, 1) },
		{ ("cp", Z80AddressingMode.Immediate8), new([], 0xfe, 2) },
		{ ("cp (hl)", Z80AddressingMode.Implied), new([], 0xbe, 1) },

		// INC r - Opcode = $04 | (reg << 3)
		{ ("inc", Z80AddressingMode.Register8), new([], 0x04, 1) },
		{ ("inc (hl)", Z80AddressingMode.Implied), new([], 0x34, 1) },

		// DEC r - Opcode = $05 | (reg << 3)
		{ ("dec", Z80AddressingMode.Register8), new([], 0x05, 1) },
		{ ("dec (hl)", Z80AddressingMode.Implied), new([], 0x35, 1) },

		// =========================================================================
		// 16-Bit Arithmetic
		// =========================================================================

		// ADD HL, rr - Opcode = $09 | (rr << 4)
		{ ("add hl", Z80AddressingMode.Register16), new([], 0x09, 1) },

		// INC rr - Opcode = $03 | (rr << 4)
		{ ("inc", Z80AddressingMode.Register16), new([], 0x03, 1) },

		// DEC rr - Opcode = $0b | (rr << 4)
		{ ("dec", Z80AddressingMode.Register16), new([], 0x0b, 1) },

		// =========================================================================
		// General-Purpose Arithmetic and CPU Control
		// =========================================================================

		{ ("daa", Z80AddressingMode.Implied), new([], 0x27, 1) },
		{ ("cpl", Z80AddressingMode.Implied), new([], 0x2f, 1) },
		{ ("neg", Z80AddressingMode.Implied), new([Prefixes.ED], 0x44, 2) },
		{ ("ccf", Z80AddressingMode.Implied), new([], 0x3f, 1) },
		{ ("scf", Z80AddressingMode.Implied), new([], 0x37, 1) },
		{ ("nop", Z80AddressingMode.Implied), new([], 0x00, 1) },
		{ ("halt", Z80AddressingMode.Implied), new([], 0x76, 1) },
		{ ("di", Z80AddressingMode.Implied), new([], 0xf3, 1) },
		{ ("ei", Z80AddressingMode.Implied), new([], 0xfb, 1) },

		// =========================================================================
		// Rotate and Shift
		// =========================================================================

		// Accumulator rotates (non-CB prefixed)
		{ ("rlca", Z80AddressingMode.Implied), new([], 0x07, 1) },
		{ ("rrca", Z80AddressingMode.Implied), new([], 0x0f, 1) },
		{ ("rla", Z80AddressingMode.Implied), new([], 0x17, 1) },
		{ ("rra", Z80AddressingMode.Implied), new([], 0x1f, 1) },

		// =========================================================================
		// Jump Instructions
		// =========================================================================

		// JP nn - Unconditional jump
		{ ("jp", Z80AddressingMode.Extended), new([], 0xc3, 3) },

		// JP cc, nn - Conditional jump
		// Opcode = $c2 | (cc << 3)
		{ ("jp", Z80AddressingMode.Condition), new([], 0xc2, 3) },

		// JP (HL) - Jump to address in HL
		{ ("jp (hl)", Z80AddressingMode.Implied), new([], 0xe9, 1) },

		// JR e - Relative jump
		{ ("jr", Z80AddressingMode.Relative), new([], 0x18, 2) },

		// JR cc, e - Conditional relative jump (only NZ, Z, NC, C)
		{ ("jr nz", Z80AddressingMode.Relative), new([], 0x20, 2) },
		{ ("jr z", Z80AddressingMode.Relative), new([], 0x28, 2) },
		{ ("jr nc", Z80AddressingMode.Relative), new([], 0x30, 2) },
		{ ("jr c", Z80AddressingMode.Relative), new([], 0x38, 2) },

		// DJNZ e - Decrement B and jump if not zero
		{ ("djnz", Z80AddressingMode.Relative), new([], 0x10, 2) },

		// =========================================================================
		// Call and Return
		// =========================================================================

		// CALL nn
		{ ("call", Z80AddressingMode.Extended), new([], 0xcd, 3) },

		// CALL cc, nn - Opcode = $c4 | (cc << 3)
		{ ("call", Z80AddressingMode.Condition), new([], 0xc4, 3) },

		// RET
		{ ("ret", Z80AddressingMode.Implied), new([], 0xc9, 1) },

		// RET cc - Opcode = $c0 | (cc << 3)
		{ ("ret", Z80AddressingMode.Condition), new([], 0xc0, 1) },

		// RETI - Return from interrupt
		{ ("reti", Z80AddressingMode.Implied), new([Prefixes.ED], 0x4d, 2) },

		// RETN - Return from NMI
		{ ("retn", Z80AddressingMode.Implied), new([Prefixes.ED], 0x45, 2) },

		// RST p - Restart
		// Opcode = $c7 | (p & $38)
		{ ("rst", Z80AddressingMode.Restart), new([], 0xc7, 1) },

		// =========================================================================
		// Input/Output
		// =========================================================================

		// IN A, (n)
		{ ("in a", Z80AddressingMode.Port), new([], 0xdb, 2) },

		// OUT (n), A
		{ ("out", Z80AddressingMode.Port), new([], 0xd3, 2) },

		// =========================================================================
		// Interrupt Mode
		// =========================================================================

		{ ("im 0", Z80AddressingMode.Implied), new([Prefixes.ED], 0x46, 2) },
		{ ("im 1", Z80AddressingMode.Implied), new([Prefixes.ED], 0x56, 2) },
		{ ("im 2", Z80AddressingMode.Implied), new([Prefixes.ED], 0x5e, 2) },
	};

	/// <summary>
	/// CB-prefixed opcodes (bit manipulation, rotates).
	/// </summary>
	private static readonly Dictionary<(string, Z80AddressingMode), InstructionEncoding> _cbOpcodes = new(MnemonicComparer.Instance) {
		// Rotates (CB prefix)
		// RLC r - Opcode = $00 | reg
		{ ("rlc", Z80AddressingMode.Register8), new([Prefixes.CB], 0x00, 2) },
		{ ("rlc (hl)", Z80AddressingMode.Implied), new([Prefixes.CB], 0x06, 2) },

		// RRC r - Opcode = $08 | reg
		{ ("rrc", Z80AddressingMode.Register8), new([Prefixes.CB], 0x08, 2) },
		{ ("rrc (hl)", Z80AddressingMode.Implied), new([Prefixes.CB], 0x0e, 2) },

		// RL r - Opcode = $10 | reg
		{ ("rl", Z80AddressingMode.Register8), new([Prefixes.CB], 0x10, 2) },
		{ ("rl (hl)", Z80AddressingMode.Implied), new([Prefixes.CB], 0x16, 2) },

		// RR r - Opcode = $18 | reg
		{ ("rr", Z80AddressingMode.Register8), new([Prefixes.CB], 0x18, 2) },
		{ ("rr (hl)", Z80AddressingMode.Implied), new([Prefixes.CB], 0x1e, 2) },

		// SLA r - Opcode = $20 | reg
		{ ("sla", Z80AddressingMode.Register8), new([Prefixes.CB], 0x20, 2) },
		{ ("sla (hl)", Z80AddressingMode.Implied), new([Prefixes.CB], 0x26, 2) },

		// SRA r - Opcode = $28 | reg
		{ ("sra", Z80AddressingMode.Register8), new([Prefixes.CB], 0x28, 2) },
		{ ("sra (hl)", Z80AddressingMode.Implied), new([Prefixes.CB], 0x2e, 2) },

		// SRL r - Opcode = $38 | reg
		{ ("srl", Z80AddressingMode.Register8), new([Prefixes.CB], 0x38, 2) },
		{ ("srl (hl)", Z80AddressingMode.Implied), new([Prefixes.CB], 0x3e, 2) },

		// BIT b, r - Opcode = $40 | (b << 3) | reg
		{ ("bit", Z80AddressingMode.Bit), new([Prefixes.CB], 0x40, 2) },

		// RES b, r - Opcode = $80 | (b << 3) | reg
		{ ("res", Z80AddressingMode.Bit), new([Prefixes.CB], 0x80, 2) },

		// SET b, r - Opcode = $c0 | (b << 3) | reg
		{ ("set", Z80AddressingMode.Bit), new([Prefixes.CB], 0xc0, 2) },
	};

	/// <summary>
	/// ED-prefixed opcodes (extended instructions).
	/// </summary>
	private static readonly Dictionary<(string, Z80AddressingMode), InstructionEncoding> _edOpcodes = new(MnemonicComparer.Instance) {
		// Block transfer
		{ ("ldi", Z80AddressingMode.Implied), new([Prefixes.ED], 0xa0, 2) },
		{ ("ldir", Z80AddressingMode.Implied), new([Prefixes.ED], 0xb0, 2) },
		{ ("ldd", Z80AddressingMode.Implied), new([Prefixes.ED], 0xa8, 2) },
		{ ("lddr", Z80AddressingMode.Implied), new([Prefixes.ED], 0xb8, 2) },

		// Block compare
		{ ("cpi", Z80AddressingMode.Implied), new([Prefixes.ED], 0xa1, 2) },
		{ ("cpir", Z80AddressingMode.Implied), new([Prefixes.ED], 0xb1, 2) },
		{ ("cpd", Z80AddressingMode.Implied), new([Prefixes.ED], 0xa9, 2) },
		{ ("cpdr", Z80AddressingMode.Implied), new([Prefixes.ED], 0xb9, 2) },

		// Block I/O
		{ ("ini", Z80AddressingMode.Implied), new([Prefixes.ED], 0xa2, 2) },
		{ ("inir", Z80AddressingMode.Implied), new([Prefixes.ED], 0xb2, 2) },
		{ ("ind", Z80AddressingMode.Implied), new([Prefixes.ED], 0xaa, 2) },
		{ ("indr", Z80AddressingMode.Implied), new([Prefixes.ED], 0xba, 2) },
		{ ("outi", Z80AddressingMode.Implied), new([Prefixes.ED], 0xa3, 2) },
		{ ("otir", Z80AddressingMode.Implied), new([Prefixes.ED], 0xb3, 2) },
		{ ("outd", Z80AddressingMode.Implied), new([Prefixes.ED], 0xab, 2) },
		{ ("otdr", Z80AddressingMode.Implied), new([Prefixes.ED], 0xbb, 2) },

		// 16-bit arithmetic
		// ADC HL, rr - Opcode = $4a | (rr << 4)
		{ ("adc hl", Z80AddressingMode.Register16), new([Prefixes.ED], 0x4a, 2) },

		// SBC HL, rr - Opcode = $42 | (rr << 4)
		{ ("sbc hl", Z80AddressingMode.Register16), new([Prefixes.ED], 0x42, 2) },

		// LD (nn), rr / LD rr, (nn) for BC, DE, SP
		// LD (nn), BC - ED $43
		{ ("ld (nn),bc", Z80AddressingMode.Implied), new([Prefixes.ED], 0x43, 4) },
		{ ("ld (nn),de", Z80AddressingMode.Implied), new([Prefixes.ED], 0x53, 4) },
		{ ("ld (nn),sp", Z80AddressingMode.Implied), new([Prefixes.ED], 0x73, 4) },
		{ ("ld bc,(nn)", Z80AddressingMode.Implied), new([Prefixes.ED], 0x4b, 4) },
		{ ("ld de,(nn)", Z80AddressingMode.Implied), new([Prefixes.ED], 0x5b, 4) },
		{ ("ld sp,(nn)", Z80AddressingMode.Implied), new([Prefixes.ED], 0x7b, 4) },

		// I/O with C
		// IN r, (C) - ED $40 | (r << 3)
		{ ("in", Z80AddressingMode.Register8), new([Prefixes.ED], 0x40, 2) },
		// OUT (C), r - ED $41 | (r << 3)
		{ ("out (c)", Z80AddressingMode.Register8), new([Prefixes.ED], 0x41, 2) },

		// Special
		{ ("ld i,a", Z80AddressingMode.Implied), new([Prefixes.ED], 0x47, 2) },
		{ ("ld r,a", Z80AddressingMode.Implied), new([Prefixes.ED], 0x4f, 2) },
		{ ("ld a,i", Z80AddressingMode.Implied), new([Prefixes.ED], 0x57, 2) },
		{ ("ld a,r", Z80AddressingMode.Implied), new([Prefixes.ED], 0x5f, 2) },

		{ ("rrd", Z80AddressingMode.Implied), new([Prefixes.ED], 0x67, 2) },
		{ ("rld", Z80AddressingMode.Implied), new([Prefixes.ED], 0x6f, 2) },
	};

	/// <summary>
	/// Checks if a mnemonic is a valid Z80 instruction.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <returns>True if valid, false otherwise.</returns>
	public static bool IsValidMnemonic(string mnemonic) {
		var lower = mnemonic.ToLowerInvariant();

		// Check basic opcodes
		foreach (var key in _basicOpcodes.Keys) {
			if (key.Item1.Equals(lower, StringComparison.OrdinalIgnoreCase) ||
				lower.StartsWith(key.Item1, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}
		}

		// Check CB opcodes
		foreach (var key in _cbOpcodes.Keys) {
			if (key.Item1.Equals(lower, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}
		}

		// Check ED opcodes
		foreach (var key in _edOpcodes.Keys) {
			if (key.Item1.Equals(lower, StringComparison.OrdinalIgnoreCase)) {
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Tries to get an encoding for an instruction.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <param name="mode">The addressing mode.</param>
	/// <param name="encoding">The encoding if found.</param>
	/// <returns>True if found, false otherwise.</returns>
	public static bool TryGetEncoding(string mnemonic, Z80AddressingMode mode, out InstructionEncoding encoding) {
		var key = (mnemonic, mode);

		if (_basicOpcodes.TryGetValue(key, out encoding)) {
			return true;
		}

		if (_cbOpcodes.TryGetValue(key, out encoding)) {
			return true;
		}

		if (_edOpcodes.TryGetValue(key, out encoding)) {
			return true;
		}

		encoding = default;
		return false;
	}

	/// <summary>
	/// Checks if a register name is valid.
	/// </summary>
	/// <param name="name">The register name.</param>
	/// <returns>True if valid, false otherwise.</returns>
	public static bool IsValidRegister(string name) {
		return RegisterMap.ContainsKey(name);
	}

	/// <summary>
	/// Gets register information.
	/// </summary>
	/// <param name="name">The register name.</param>
	/// <param name="encoding">The register encoding.</param>
	/// <param name="is16Bit">Whether it's a 16-bit register.</param>
	/// <returns>True if found, false otherwise.</returns>
	public static bool TryGetRegister(string name, out int encoding, out bool is16Bit) {
		if (RegisterMap.TryGetValue(name, out var info)) {
			encoding = info.Encoding;
			is16Bit = info.Is16Bit;
			return true;
		}
		encoding = 0;
		is16Bit = false;
		return false;
	}

	/// <summary>
	/// Gets condition code information.
	/// </summary>
	/// <param name="name">The condition name (nz, z, nc, c, po, pe, p, m).</param>
	/// <param name="encoding">The condition encoding.</param>
	/// <returns>True if found, false otherwise.</returns>
	public static bool TryGetCondition(string name, out int encoding) {
		return ConditionMap.TryGetValue(name, out encoding);
	}

	/// <summary>
	/// Checks if an instruction is a relative branch.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <returns>True if relative branch, false otherwise.</returns>
	public static bool IsRelativeBranch(string mnemonic) {
		var lower = mnemonic.ToLowerInvariant();
		return lower == "jr" || lower == "djnz" ||
			   lower.StartsWith("jr ", StringComparison.Ordinal);
	}

	/// <summary>
	/// Gets the prefix byte for index register operations.
	/// </summary>
	/// <param name="indexReg">The index register (ix or iy).</param>
	/// <returns>The prefix byte ($dd for IX, $fd for IY).</returns>
	public static byte GetIndexPrefix(string indexReg) {
		return indexReg.ToLowerInvariant() switch {
			"ix" or "ixh" or "ixl" => Prefixes.DD,
			"iy" or "iyh" or "iyl" => Prefixes.FD,
			_ => 0
		};
	}
}

