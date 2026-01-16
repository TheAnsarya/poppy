// ============================================================================
// InstructionSetM68000.cs - Motorola 68000 Instruction Encoding
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================
// The M68000 is a 16/32-bit CISC processor used in the Sega Genesis/Mega Drive.
// It features BIG-ENDIAN byte order, eight data registers (D0-D7) and eight
// address registers (A0-A7, where A7 is the stack pointer).
// ============================================================================

using Poppy.Core.Parser;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Provides instruction encoding for the Motorola 68000 processor (Sega Genesis).
/// The M68000 is a 16/32-bit big-endian processor with a rich instruction set.
/// </summary>
public static class InstructionSetM68000 {
	/// <summary>
	/// Instruction encoding information for M68000.
	/// </summary>
	/// <param name="BaseOpcode">The base opcode word (16-bit).</param>
	/// <param name="BaseSize">Base instruction size in bytes (always even).</param>
	/// <param name="HasExtension">Whether instruction uses extension words.</param>
	public readonly record struct InstructionEncoding(ushort BaseOpcode, int BaseSize, bool HasExtension = false);

	/// <summary>
	/// M68000 addressing modes.
	/// </summary>
	public enum M68kAddressingMode {
		/// <summary>Data register direct: Dn</summary>
		DataRegisterDirect = 0,

		/// <summary>Address register direct: An</summary>
		AddressRegisterDirect = 1,

		/// <summary>Address register indirect: (An)</summary>
		AddressRegisterIndirect = 2,

		/// <summary>Address register indirect with postincrement: (An)+</summary>
		AddressRegisterIndirectPostIncrement = 3,

		/// <summary>Address register indirect with predecrement: -(An)</summary>
		AddressRegisterIndirectPreDecrement = 4,

		/// <summary>Address register indirect with displacement: d16(An)</summary>
		AddressRegisterIndirectDisplacement = 5,

		/// <summary>Address register indirect with index: d8(An,Xn)</summary>
		AddressRegisterIndirectIndex = 6,

		/// <summary>Absolute short: (xxx).W</summary>
		AbsoluteShort = 7,

		/// <summary>Absolute long: (xxx).L</summary>
		AbsoluteLong = 8,

		/// <summary>Program counter with displacement: d16(PC)</summary>
		PcDisplacement = 9,

		/// <summary>Program counter with index: d8(PC,Xn)</summary>
		PcIndex = 10,

		/// <summary>Immediate: #xxx</summary>
		Immediate = 11,

		/// <summary>Status register</summary>
		StatusRegister = 12,

		/// <summary>Condition code register</summary>
		ConditionCodeRegister = 13,

		/// <summary>User stack pointer</summary>
		UserStackPointer = 14,

		/// <summary>Register list (for MOVEM)</summary>
		RegisterList = 15,

		/// <summary>Quick immediate (3-bit, 1-8 encoded as 0-7)</summary>
		QuickImmediate = 16,

		/// <summary>Implied (no operand)</summary>
		Implied = 17,
	}

	/// <summary>
	/// Operation size for M68000 instructions.
	/// </summary>
	public enum OperationSize {
		/// <summary>Byte operation (.b)</summary>
		Byte = 0,

		/// <summary>Word operation (.w) - default</summary>
		Word = 1,

		/// <summary>Long operation (.l)</summary>
		Long = 2,
	}

	/// <summary>
	/// Condition codes for Bcc, Scc, and DBcc instructions.
	/// </summary>
	public static class ConditionCodes {
		/// <summary>True (always) - T</summary>
		public const int True = 0;
		/// <summary>False (never) - F</summary>
		public const int False = 1;
		/// <summary>Higher (unsigned) - HI</summary>
		public const int Higher = 2;
		/// <summary>Lower or Same (unsigned) - LS</summary>
		public const int LowerOrSame = 3;
		/// <summary>Carry Clear - CC/HS</summary>
		public const int CarryClear = 4;
		/// <summary>Carry Set - CS/LO</summary>
		public const int CarrySet = 5;
		/// <summary>Not Equal - NE</summary>
		public const int NotEqual = 6;
		/// <summary>Equal - EQ</summary>
		public const int Equal = 7;
		/// <summary>Overflow Clear - VC</summary>
		public const int OverflowClear = 8;
		/// <summary>Overflow Set - VS</summary>
		public const int OverflowSet = 9;
		/// <summary>Plus (positive) - PL</summary>
		public const int Plus = 10;
		/// <summary>Minus (negative) - MI</summary>
		public const int Minus = 11;
		/// <summary>Greater or Equal (signed) - GE</summary>
		public const int GreaterOrEqual = 12;
		/// <summary>Less Than (signed) - LT</summary>
		public const int LessThan = 13;
		/// <summary>Greater Than (signed) - GT</summary>
		public const int Greater = 14;
		/// <summary>Less or Equal (signed) - LE</summary>
		public const int LessOrEqual = 15;
	}

	/// <summary>
	/// Data register encoding values (D0-D7).
	/// </summary>
	public static class DataRegisters {
		/// <summary>D0 data register</summary>
		public const int D0 = 0;
		/// <summary>D1 data register</summary>
		public const int D1 = 1;
		/// <summary>D2 data register</summary>
		public const int D2 = 2;
		/// <summary>D3 data register</summary>
		public const int D3 = 3;
		/// <summary>D4 data register</summary>
		public const int D4 = 4;
		/// <summary>D5 data register</summary>
		public const int D5 = 5;
		/// <summary>D6 data register</summary>
		public const int D6 = 6;
		/// <summary>D7 data register</summary>
		public const int D7 = 7;
	}

	/// <summary>
	/// Address register encoding values (A0-A7).
	/// </summary>
	public static class AddressRegisters {
		/// <summary>A0 address register</summary>
		public const int A0 = 0;
		/// <summary>A1 address register</summary>
		public const int A1 = 1;
		/// <summary>A2 address register</summary>
		public const int A2 = 2;
		/// <summary>A3 address register</summary>
		public const int A3 = 3;
		/// <summary>A4 address register</summary>
		public const int A4 = 4;
		/// <summary>A5 address register</summary>
		public const int A5 = 5;
		/// <summary>A6 address register (frame pointer)</summary>
		public const int A6 = 6;
		/// <summary>A7 address register (stack pointer, also SP)</summary>
		public const int A7 = 7;
	}

	/// <summary>
	/// Maps register names to their encoding values and types.
	/// </summary>
	public static readonly Dictionary<string, (int Register, bool IsAddress)> RegisterMap = new(StringComparer.OrdinalIgnoreCase) {
		// Data registers
		{ "d0", (DataRegisters.D0, false) },
		{ "d1", (DataRegisters.D1, false) },
		{ "d2", (DataRegisters.D2, false) },
		{ "d3", (DataRegisters.D3, false) },
		{ "d4", (DataRegisters.D4, false) },
		{ "d5", (DataRegisters.D5, false) },
		{ "d6", (DataRegisters.D6, false) },
		{ "d7", (DataRegisters.D7, false) },

		// Address registers
		{ "a0", (AddressRegisters.A0, true) },
		{ "a1", (AddressRegisters.A1, true) },
		{ "a2", (AddressRegisters.A2, true) },
		{ "a3", (AddressRegisters.A3, true) },
		{ "a4", (AddressRegisters.A4, true) },
		{ "a5", (AddressRegisters.A5, true) },
		{ "a6", (AddressRegisters.A6, true) },
		{ "a7", (AddressRegisters.A7, true) },
		{ "sp", (AddressRegisters.A7, true) },  // SP is an alias for A7

		// Special registers
		{ "pc", (-1, false) },   // Program counter
		{ "sr", (-2, false) },   // Status register
		{ "ccr", (-3, false) },  // Condition code register
		{ "usp", (-4, false) },  // User stack pointer
	};

	/// <summary>
	/// Maps condition code names to their encoding values.
	/// </summary>
	public static readonly Dictionary<string, int> ConditionCodeMap = new(StringComparer.OrdinalIgnoreCase) {
		{ "t", ConditionCodes.True },
		{ "f", ConditionCodes.False },
		{ "hi", ConditionCodes.Higher },
		{ "ls", ConditionCodes.LowerOrSame },
		{ "cc", ConditionCodes.CarryClear },
		{ "hs", ConditionCodes.CarryClear },  // Alias for CC
		{ "cs", ConditionCodes.CarrySet },
		{ "lo", ConditionCodes.CarrySet },    // Alias for CS
		{ "ne", ConditionCodes.NotEqual },
		{ "eq", ConditionCodes.Equal },
		{ "vc", ConditionCodes.OverflowClear },
		{ "vs", ConditionCodes.OverflowSet },
		{ "pl", ConditionCodes.Plus },
		{ "mi", ConditionCodes.Minus },
		{ "ge", ConditionCodes.GreaterOrEqual },
		{ "lt", ConditionCodes.LessThan },
		{ "gt", ConditionCodes.Greater },
		{ "le", ConditionCodes.LessOrEqual },
	};

	/// <summary>
	/// Custom comparer for case-insensitive mnemonic lookup.
	/// </summary>
	private sealed class MnemonicComparer : IEqualityComparer<string> {
		public static readonly MnemonicComparer Instance = new();

		public bool Equals(string? x, string? y) {
			return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
		}

		public int GetHashCode(string obj) {
			return obj.ToLowerInvariant().GetHashCode();
		}
	}

	/// <summary>
	/// Base opcode lookup table. Actual encoding varies by operand types.
	/// This maps mnemonics to their base opcode patterns.
	/// </summary>
	/// <remarks>
	/// M68000 instruction encoding is complex because the opcode encodes:
	/// - Operation (bits 15-12 or 15-8)
	/// - Size (often bits 7-6)
	/// - Effective address mode (bits 5-3)
	/// - Effective address register (bits 2-0)
	/// - For two-operand instructions, destination may be encoded in bits 11-9
	/// </remarks>
	private static readonly Dictionary<string, ushort> _baseOpcodes = new(MnemonicComparer.Instance) {
		// =========================================================================
		// Data Movement Instructions
		// =========================================================================

		// MOVE - Move data ($0xxx for byte, $3xxx for word, $2xxx for long)
		{ "move", 0x0000 },      // Base, actual encoding varies by size
		{ "move.b", 0x1000 },    // Move byte
		{ "move.w", 0x3000 },    // Move word
		{ "move.l", 0x2000 },    // Move long
		{ "movea", 0x0040 },     // Move address (word/long only)
		{ "movea.w", 0x3040 },   // Move address word
		{ "movea.l", 0x2040 },   // Move address long
		{ "moveq", 0x7000 },     // Move quick (sign-extended byte to long)
		{ "movem", 0x4880 },     // Move multiple registers
		{ "movem.w", 0x4880 },   // Move multiple word
		{ "movem.l", 0x48c0 },   // Move multiple long
		{ "movep", 0x0108 },     // Move peripheral
		{ "lea", 0x41c0 },       // Load effective address
		{ "pea", 0x4840 },       // Push effective address
		{ "exg", 0xc100 },       // Exchange registers
		{ "swap", 0x4840 },      // Swap register halves
		{ "link", 0x4e50 },      // Link and allocate
		{ "unlk", 0x4e58 },      // Unlink
		{ "clr", 0x4200 },       // Clear
		{ "clr.b", 0x4200 },     // Clear byte
		{ "clr.w", 0x4240 },     // Clear word
		{ "clr.l", 0x4280 },     // Clear long

		// =========================================================================
		// Arithmetic Instructions
		// =========================================================================

		// ADD - Add binary
		{ "add", 0xd000 },       // Add
		{ "add.b", 0xd000 },     // Add byte
		{ "add.w", 0xd040 },     // Add word
		{ "add.l", 0xd080 },     // Add long
		{ "adda", 0xd0c0 },      // Add address
		{ "adda.w", 0xd0c0 },    // Add address word
		{ "adda.l", 0xd1c0 },    // Add address long
		{ "addi", 0x0600 },      // Add immediate
		{ "addi.b", 0x0600 },    // Add immediate byte
		{ "addi.w", 0x0640 },    // Add immediate word
		{ "addi.l", 0x0680 },    // Add immediate long
		{ "addq", 0x5000 },      // Add quick
		{ "addq.b", 0x5000 },    // Add quick byte
		{ "addq.w", 0x5040 },    // Add quick word
		{ "addq.l", 0x5080 },    // Add quick long
		{ "addx", 0xd100 },      // Add extended

		// SUB - Subtract binary
		{ "sub", 0x9000 },       // Subtract
		{ "sub.b", 0x9000 },     // Subtract byte
		{ "sub.w", 0x9040 },     // Subtract word
		{ "sub.l", 0x9080 },     // Subtract long
		{ "suba", 0x90c0 },      // Subtract address
		{ "suba.w", 0x90c0 },    // Subtract address word
		{ "suba.l", 0x91c0 },    // Subtract address long
		{ "subi", 0x0400 },      // Subtract immediate
		{ "subi.b", 0x0400 },    // Subtract immediate byte
		{ "subi.w", 0x0440 },    // Subtract immediate word
		{ "subi.l", 0x0480 },    // Subtract immediate long
		{ "subq", 0x5100 },      // Subtract quick
		{ "subq.b", 0x5100 },    // Subtract quick byte
		{ "subq.w", 0x5140 },    // Subtract quick word
		{ "subq.l", 0x5180 },    // Subtract quick long
		{ "subx", 0x9100 },      // Subtract extended

		// MUL/DIV - Multiply and Divide
		{ "muls", 0xc1c0 },      // Signed multiply
		{ "muls.w", 0xc1c0 },    // Signed multiply word
		{ "mulu", 0xc0c0 },      // Unsigned multiply
		{ "mulu.w", 0xc0c0 },    // Unsigned multiply word
		{ "divs", 0x81c0 },      // Signed divide
		{ "divs.w", 0x81c0 },    // Signed divide word
		{ "divu", 0x80c0 },      // Unsigned divide
		{ "divu.w", 0x80c0 },    // Unsigned divide word

		// NEG - Negate
		{ "neg", 0x4400 },       // Negate
		{ "neg.b", 0x4400 },     // Negate byte
		{ "neg.w", 0x4440 },     // Negate word
		{ "neg.l", 0x4480 },     // Negate long
		{ "negx", 0x4000 },      // Negate with extend
		{ "negx.b", 0x4000 },    // Negate extended byte
		{ "negx.w", 0x4040 },    // Negate extended word
		{ "negx.l", 0x4080 },    // Negate extended long

		// CMP - Compare
		{ "cmp", 0xb000 },       // Compare
		{ "cmp.b", 0xb000 },     // Compare byte
		{ "cmp.w", 0xb040 },     // Compare word
		{ "cmp.l", 0xb080 },     // Compare long
		{ "cmpa", 0xb0c0 },      // Compare address
		{ "cmpa.w", 0xb0c0 },    // Compare address word
		{ "cmpa.l", 0xb1c0 },    // Compare address long
		{ "cmpi", 0x0c00 },      // Compare immediate
		{ "cmpi.b", 0x0c00 },    // Compare immediate byte
		{ "cmpi.w", 0x0c40 },    // Compare immediate word
		{ "cmpi.l", 0x0c80 },    // Compare immediate long
		{ "cmpm", 0xb108 },      // Compare memory
		{ "cmpm.b", 0xb108 },    // Compare memory byte
		{ "cmpm.w", 0xb148 },    // Compare memory word
		{ "cmpm.l", 0xb188 },    // Compare memory long

		// EXT - Sign extend
		{ "ext", 0x4880 },       // Extend
		{ "ext.w", 0x4880 },     // Extend byte to word
		{ "ext.l", 0x48c0 },     // Extend word to long

		// TST - Test
		{ "tst", 0x4a00 },       // Test
		{ "tst.b", 0x4a00 },     // Test byte
		{ "tst.w", 0x4a40 },     // Test word
		{ "tst.l", 0x4a80 },     // Test long

		// =========================================================================
		// Logical Instructions
		// =========================================================================

		// AND - Logical AND
		{ "and", 0xc000 },       // AND
		{ "and.b", 0xc000 },     // AND byte
		{ "and.w", 0xc040 },     // AND word
		{ "and.l", 0xc080 },     // AND long
		{ "andi", 0x0200 },      // AND immediate
		{ "andi.b", 0x0200 },    // AND immediate byte
		{ "andi.w", 0x0240 },    // AND immediate word
		{ "andi.l", 0x0280 },    // AND immediate long

		// OR - Logical OR
		{ "or", 0x8000 },        // OR
		{ "or.b", 0x8000 },      // OR byte
		{ "or.w", 0x8040 },      // OR word
		{ "or.l", 0x8080 },      // OR long
		{ "ori", 0x0000 },       // OR immediate
		{ "ori.b", 0x0000 },     // OR immediate byte
		{ "ori.w", 0x0040 },     // OR immediate word
		{ "ori.l", 0x0080 },     // OR immediate long

		// EOR - Exclusive OR
		{ "eor", 0xb100 },       // EOR
		{ "eor.b", 0xb100 },     // EOR byte
		{ "eor.w", 0xb140 },     // EOR word
		{ "eor.l", 0xb180 },     // EOR long
		{ "eori", 0x0a00 },      // EOR immediate
		{ "eori.b", 0x0a00 },    // EOR immediate byte
		{ "eori.w", 0x0a40 },    // EOR immediate word
		{ "eori.l", 0x0a80 },    // EOR immediate long

		// NOT - Logical complement
		{ "not", 0x4600 },       // NOT
		{ "not.b", 0x4600 },     // NOT byte
		{ "not.w", 0x4640 },     // NOT word
		{ "not.l", 0x4680 },     // NOT long

		// =========================================================================
		// Shift and Rotate Instructions
		// =========================================================================

		// ASL/ASR - Arithmetic shift
		{ "asl", 0xe100 },       // Arithmetic shift left
		{ "asl.b", 0xe100 },     // ASL byte
		{ "asl.w", 0xe140 },     // ASL word
		{ "asl.l", 0xe180 },     // ASL long
		{ "asr", 0xe000 },       // Arithmetic shift right
		{ "asr.b", 0xe000 },     // ASR byte
		{ "asr.w", 0xe040 },     // ASR word
		{ "asr.l", 0xe080 },     // ASR long

		// LSL/LSR - Logical shift
		{ "lsl", 0xe108 },       // Logical shift left
		{ "lsl.b", 0xe108 },     // LSL byte
		{ "lsl.w", 0xe148 },     // LSL word
		{ "lsl.l", 0xe188 },     // LSL long
		{ "lsr", 0xe008 },       // Logical shift right
		{ "lsr.b", 0xe008 },     // LSR byte
		{ "lsr.w", 0xe048 },     // LSR word
		{ "lsr.l", 0xe088 },     // LSR long

		// ROL/ROR - Rotate
		{ "rol", 0xe118 },       // Rotate left
		{ "rol.b", 0xe118 },     // ROL byte
		{ "rol.w", 0xe158 },     // ROL word
		{ "rol.l", 0xe198 },     // ROL long
		{ "ror", 0xe018 },       // Rotate right
		{ "ror.b", 0xe018 },     // ROR byte
		{ "ror.w", 0xe058 },     // ROR word
		{ "ror.l", 0xe098 },     // ROR long

		// ROXL/ROXR - Rotate with extend
		{ "roxl", 0xe110 },      // Rotate left with extend
		{ "roxl.b", 0xe110 },    // ROXL byte
		{ "roxl.w", 0xe150 },    // ROXL word
		{ "roxl.l", 0xe190 },    // ROXL long
		{ "roxr", 0xe010 },      // Rotate right with extend
		{ "roxr.b", 0xe010 },    // ROXR byte
		{ "roxr.w", 0xe050 },    // ROXR word
		{ "roxr.l", 0xe090 },    // ROXR long

		// =========================================================================
		// Bit Manipulation Instructions
		// =========================================================================

		{ "btst", 0x0100 },      // Bit test
		{ "bset", 0x01c0 },      // Bit set
		{ "bclr", 0x0180 },      // Bit clear
		{ "bchg", 0x0140 },      // Bit change

		// =========================================================================
		// Branch Instructions
		// =========================================================================

		{ "bra", 0x6000 },       // Branch always
		{ "bra.s", 0x6000 },     // Branch always short
		{ "bra.w", 0x6000 },     // Branch always word
		{ "bsr", 0x6100 },       // Branch to subroutine
		{ "bsr.s", 0x6100 },     // Branch to subroutine short
		{ "bsr.w", 0x6100 },     // Branch to subroutine word

		// Conditional branches (Bcc) - condition encoded in bits 11-8
		{ "bhi", 0x6200 },       // Branch if higher
		{ "bls", 0x6300 },       // Branch if lower or same
		{ "bcc", 0x6400 },       // Branch if carry clear
		{ "bhs", 0x6400 },       // Alias for BCC
		{ "bcs", 0x6500 },       // Branch if carry set
		{ "blo", 0x6500 },       // Alias for BCS
		{ "bne", 0x6600 },       // Branch if not equal
		{ "beq", 0x6700 },       // Branch if equal
		{ "bvc", 0x6800 },       // Branch if overflow clear
		{ "bvs", 0x6900 },       // Branch if overflow set
		{ "bpl", 0x6a00 },       // Branch if plus
		{ "bmi", 0x6b00 },       // Branch if minus
		{ "bge", 0x6c00 },       // Branch if greater or equal
		{ "blt", 0x6d00 },       // Branch if less than
		{ "bgt", 0x6e00 },       // Branch if greater than
		{ "ble", 0x6f00 },       // Branch if less or equal

		// DBcc - Decrement and branch
		{ "dbra", 0x51c8 },      // Decrement and branch always (alias for DBF)
		{ "dbf", 0x51c8 },       // Decrement and branch if false
		{ "dbt", 0x50c8 },       // Decrement and branch if true
		{ "dbhi", 0x52c8 },      // Decrement and branch if higher
		{ "dbls", 0x53c8 },      // Decrement and branch if lower or same
		{ "dbcc", 0x54c8 },      // Decrement and branch if carry clear
		{ "dbcs", 0x55c8 },      // Decrement and branch if carry set
		{ "dbne", 0x56c8 },      // Decrement and branch if not equal
		{ "dbeq", 0x57c8 },      // Decrement and branch if equal
		{ "dbvc", 0x58c8 },      // Decrement and branch if overflow clear
		{ "dbvs", 0x59c8 },      // Decrement and branch if overflow set
		{ "dbpl", 0x5ac8 },      // Decrement and branch if plus
		{ "dbmi", 0x5bc8 },      // Decrement and branch if minus
		{ "dbge", 0x5cc8 },      // Decrement and branch if greater or equal
		{ "dblt", 0x5dc8 },      // Decrement and branch if less than
		{ "dbgt", 0x5ec8 },      // Decrement and branch if greater than
		{ "dble", 0x5fc8 },      // Decrement and branch if less or equal

		// =========================================================================
		// Jump and Subroutine Instructions
		// =========================================================================

		{ "jmp", 0x4ec0 },       // Jump
		{ "jsr", 0x4e80 },       // Jump to subroutine
		{ "rts", 0x4e75 },       // Return from subroutine
		{ "rte", 0x4e73 },       // Return from exception
		{ "rtr", 0x4e77 },       // Return and restore CCR

		// =========================================================================
		// Set Condition Instructions (Scc)
		// =========================================================================

		{ "st", 0x50c0 },        // Set if true
		{ "sf", 0x51c0 },        // Set if false
		{ "shi", 0x52c0 },       // Set if higher
		{ "sls", 0x53c0 },       // Set if lower or same
		{ "scc", 0x54c0 },       // Set if carry clear
		{ "scs", 0x55c0 },       // Set if carry set
		{ "sne", 0x56c0 },       // Set if not equal
		{ "seq", 0x57c0 },       // Set if equal
		{ "svc", 0x58c0 },       // Set if overflow clear
		{ "svs", 0x59c0 },       // Set if overflow set
		{ "spl", 0x5ac0 },       // Set if plus
		{ "smi", 0x5bc0 },       // Set if minus
		{ "sge", 0x5cc0 },       // Set if greater or equal
		{ "slt", 0x5dc0 },       // Set if less than
		{ "sgt", 0x5ec0 },       // Set if greater than
		{ "sle", 0x5fc0 },       // Set if less or equal

		// =========================================================================
		// System Control Instructions
		// =========================================================================

		{ "nop", 0x4e71 },       // No operation
		{ "reset", 0x4e70 },     // Reset external devices
		{ "stop", 0x4e72 },      // Stop processor
		{ "trap", 0x4e40 },      // Trap
		{ "trapv", 0x4e76 },     // Trap on overflow
		{ "chk", 0x4180 },       // Check register against bounds
		{ "chk.w", 0x4180 },     // Check word
		{ "illegal", 0x4afc },   // Illegal instruction (for debugging)

		// =========================================================================
		// CCR/SR Manipulation
		// =========================================================================

		{ "andi.b sr", 0x023c }, // AND immediate to CCR
		{ "andi.w sr", 0x027c }, // AND immediate to SR
		{ "eori.b sr", 0x0a3c }, // EOR immediate to CCR
		{ "eori.w sr", 0x0a7c }, // EOR immediate to SR
		{ "ori.b sr", 0x003c },  // OR immediate to CCR
		{ "ori.w sr", 0x007c },  // OR immediate to SR
		{ "move sr", 0x40c0 },   // Move from SR
		{ "move ccr", 0x42c0 },  // Move from CCR
		{ "move to sr", 0x46c0 },  // Move to SR
		{ "move to ccr", 0x44c0 }, // Move to CCR
		{ "move usp", 0x4e60 },  // Move USP

		// =========================================================================
		// BCD Instructions
		// =========================================================================

		{ "abcd", 0xc100 },      // Add BCD
		{ "sbcd", 0x8100 },      // Subtract BCD
		{ "nbcd", 0x4800 },      // Negate BCD

		// =========================================================================
		// Miscellaneous
		// =========================================================================

		{ "tas", 0x4ac0 },       // Test and set
	};

	/// <summary>
	/// Checks if a mnemonic is a valid M68000 instruction.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <returns>True if valid, false otherwise.</returns>
	public static bool IsValidMnemonic(string mnemonic) {
		// Strip size suffix if present
		var baseMnemonic = StripSizeSuffix(mnemonic);
		return _baseOpcodes.ContainsKey(mnemonic) || _baseOpcodes.ContainsKey(baseMnemonic);
	}

	/// <summary>
	/// Strips the size suffix (.b, .w, .l, .s) from a mnemonic.
	/// </summary>
	/// <param name="mnemonic">The mnemonic to strip.</param>
	/// <returns>The base mnemonic without size suffix.</returns>
	public static string StripSizeSuffix(string mnemonic) {
		if (mnemonic.Length > 2 &&
			(mnemonic.EndsWith(".b", StringComparison.OrdinalIgnoreCase) ||
			 mnemonic.EndsWith(".w", StringComparison.OrdinalIgnoreCase) ||
			 mnemonic.EndsWith(".l", StringComparison.OrdinalIgnoreCase) ||
			 mnemonic.EndsWith(".s", StringComparison.OrdinalIgnoreCase))) {
			return mnemonic[..^2];
		}
		return mnemonic;
	}

	/// <summary>
	/// Gets the operation size from a mnemonic's size suffix.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <returns>The operation size.</returns>
	public static OperationSize GetSizeFromMnemonic(string mnemonic) {
		if (mnemonic.EndsWith(".b", StringComparison.OrdinalIgnoreCase)) return OperationSize.Byte;
		if (mnemonic.EndsWith(".l", StringComparison.OrdinalIgnoreCase)) return OperationSize.Long;
		return OperationSize.Word; // Default to word
	}

	/// <summary>
	/// Gets the base opcode for a mnemonic.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <param name="baseOpcode">The base opcode word.</param>
	/// <returns>True if found, false otherwise.</returns>
	public static bool TryGetBaseOpcode(string mnemonic, out ushort baseOpcode) {
		if (_baseOpcodes.TryGetValue(mnemonic, out baseOpcode)) {
			return true;
		}

		// Try without size suffix
		var baseMnemonic = StripSizeSuffix(mnemonic);
		return _baseOpcodes.TryGetValue(baseMnemonic, out baseOpcode);
	}

	/// <summary>
	/// Checks if an instruction is a branch instruction.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <returns>True if it's a branch instruction.</returns>
	public static bool IsBranchInstruction(string mnemonic) {
		var lower = mnemonic.ToLowerInvariant();
		var baseMnemonic = StripSizeSuffix(lower);

		return baseMnemonic switch {
			"bra" or "bsr" or "bhi" or "bls" or "bcc" or "bhs" or "bcs" or "blo" or
			"bne" or "beq" or "bvc" or "bvs" or "bpl" or "bmi" or "bge" or "blt" or
			"bgt" or "ble" => true,
			_ => false
		};
	}

	/// <summary>
	/// Checks if an instruction is a DBcc (decrement and branch) instruction.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <returns>True if it's a DBcc instruction.</returns>
	public static bool IsDbccInstruction(string mnemonic) {
		var lower = mnemonic.ToLowerInvariant();
		return lower.StartsWith("db", StringComparison.Ordinal) &&
			   _baseOpcodes.ContainsKey(lower);
	}

	/// <summary>
	/// Checks if an instruction is an Scc (set on condition) instruction.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <returns>True if it's an Scc instruction.</returns>
	public static bool IsSccInstruction(string mnemonic) {
		var lower = mnemonic.ToLowerInvariant();
		return lower.Length == 2 && lower[0] == 's' &&
			   _baseOpcodes.ContainsKey(lower);
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
	/// <param name="isAddress">Whether it's an address register.</param>
	/// <returns>True if found, false otherwise.</returns>
	public static bool TryGetRegister(string name, out int encoding, out bool isAddress) {
		if (RegisterMap.TryGetValue(name, out var info)) {
			encoding = info.Register;
			isAddress = info.IsAddress;
			return true;
		}
		encoding = 0;
		isAddress = false;
		return false;
	}

	/// <summary>
	/// Encodes an effective address field (mode and register).
	/// </summary>
	/// <param name="mode">The addressing mode.</param>
	/// <param name="register">The register number.</param>
	/// <returns>The 6-bit effective address field.</returns>
	public static int EncodeEffectiveAddress(M68kAddressingMode mode, int register) {
		int eaMode = mode switch {
			M68kAddressingMode.DataRegisterDirect => 0,
			M68kAddressingMode.AddressRegisterDirect => 1,
			M68kAddressingMode.AddressRegisterIndirect => 2,
			M68kAddressingMode.AddressRegisterIndirectPostIncrement => 3,
			M68kAddressingMode.AddressRegisterIndirectPreDecrement => 4,
			M68kAddressingMode.AddressRegisterIndirectDisplacement => 5,
			M68kAddressingMode.AddressRegisterIndirectIndex => 6,
			M68kAddressingMode.AbsoluteShort => 7,
			M68kAddressingMode.AbsoluteLong => 7,
			M68kAddressingMode.PcDisplacement => 7,
			M68kAddressingMode.PcIndex => 7,
			M68kAddressingMode.Immediate => 7,
			_ => 0
		};

		int eaReg = mode switch {
			M68kAddressingMode.AbsoluteShort => 0,
			M68kAddressingMode.AbsoluteLong => 1,
			M68kAddressingMode.PcDisplacement => 2,
			M68kAddressingMode.PcIndex => 3,
			M68kAddressingMode.Immediate => 4,
			_ => register & 7
		};

		return (eaMode << 3) | eaReg;
	}

	/// <summary>
	/// Encodes the size field for instructions that use bits 7-6 for size.
	/// </summary>
	/// <param name="size">The operation size.</param>
	/// <returns>The 2-bit size encoding.</returns>
	public static int EncodeSizeField(OperationSize size) {
		return size switch {
			OperationSize.Byte => 0,
			OperationSize.Word => 1,
			OperationSize.Long => 2,
			_ => 1
		};
	}

	/// <summary>
	/// Gets the byte size for an operation size.
	/// </summary>
	/// <param name="size">The operation size.</param>
	/// <returns>Number of bytes.</returns>
	public static int GetByteCount(OperationSize size) {
		return size switch {
			OperationSize.Byte => 1,
			OperationSize.Word => 2,
			OperationSize.Long => 4,
			_ => 2
		};
	}

	/// <summary>
	/// Writes a 16-bit word in big-endian format.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns>The bytes in big-endian order.</returns>
	public static byte[] WriteWord(int value) {
		return [(byte)((value >> 8) & 0xff), (byte)(value & 0xff)];
	}

	/// <summary>
	/// Writes a 32-bit long in big-endian format.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns>The bytes in big-endian order.</returns>
	public static byte[] WriteLong(long value) {
		return [
			(byte)((value >> 24) & 0xff),
			(byte)((value >> 16) & 0xff),
			(byte)((value >> 8) & 0xff),
			(byte)(value & 0xff)
		];
	}
}

