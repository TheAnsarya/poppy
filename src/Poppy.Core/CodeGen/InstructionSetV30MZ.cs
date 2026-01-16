// ============================================================================
// InstructionSetV30MZ.cs - NEC V30MZ (8086-compatible) Instruction Encoding
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================
// The V30MZ is an Intel 8086-compatible processor used in the WonderSwan.
// It uses segment:offset addressing with a 20-bit address space.
// ============================================================================

using Poppy.Core.Parser;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Provides instruction encoding for the NEC V30MZ processor (WonderSwan).
/// The V30MZ is Intel 8086/80186 compatible with some extensions.
/// </summary>
public static class InstructionSetV30MZ {
	/// <summary>
	/// Instruction encoding information for V30MZ.
	/// </summary>
	/// <param name="Opcode">The primary opcode byte(s).</param>
	/// <param name="ModRmReg">The /r value for ModR/M byte (0-7), or -1 if not used.</param>
	/// <param name="BaseSize">Base instruction size before operand bytes.</param>
	public readonly record struct InstructionEncoding(byte[] Opcode, int ModRmReg, int BaseSize);

	/// <summary>
	/// V30MZ-specific addressing modes (8086-style).
	/// </summary>
	public enum V30AddressingMode {
		/// <summary>No operand (e.g., NOP, HLT)</summary>
		Implied,

		/// <summary>Immediate byte (e.g., MOV AL, $12)</summary>
		ImmediateByte,

		/// <summary>Immediate word (e.g., MOV AX, $1234)</summary>
		ImmediateWord,

		/// <summary>Register to register (e.g., MOV AX, BX)</summary>
		Register,

		/// <summary>Direct memory address (e.g., MOV AX, [$1234])</summary>
		Direct,

		/// <summary>Register indirect (e.g., MOV AX, [BX])</summary>
		RegisterIndirect,

		/// <summary>Base + index (e.g., MOV AX, [BX+SI])</summary>
		BasePlusIndex,

		/// <summary>Base + displacement (e.g., MOV AX, [BX+$10])</summary>
		BaseDisplacement,

		/// <summary>Base + index + displacement (e.g., MOV AX, [BX+SI+$10])</summary>
		BaseIndexDisplacement,

		/// <summary>Segment override (e.g., ES:, CS:, SS:, DS:)</summary>
		SegmentOverride,

		/// <summary>Short relative jump (-128 to +127)</summary>
		RelativeShort,

		/// <summary>Near relative jump (16-bit offset)</summary>
		RelativeNear,

		/// <summary>Far pointer (segment:offset)</summary>
		FarPointer,

		/// <summary>Port number for IN/OUT</summary>
		Port,

		/// <summary>String operation (implicit SI/DI)</summary>
		String,
	}

	/// <summary>
	/// 8086 register encoding values.
	/// </summary>
	public static class Registers {
		/// <summary>AX register (accumulator, 16-bit).</summary>
		public const int AX = 0;
		/// <summary>CX register (count, 16-bit).</summary>
		public const int CX = 1;
		/// <summary>DX register (data, 16-bit).</summary>
		public const int DX = 2;
		/// <summary>BX register (base, 16-bit).</summary>
		public const int BX = 3;
		/// <summary>SP register (stack pointer, 16-bit).</summary>
		public const int SP = 4;
		/// <summary>BP register (base pointer, 16-bit).</summary>
		public const int BP = 5;
		/// <summary>SI register (source index, 16-bit).</summary>
		public const int SI = 6;
		/// <summary>DI register (destination index, 16-bit).</summary>
		public const int DI = 7;

		/// <summary>AL register (accumulator low, 8-bit).</summary>
		public const int AL = 0;
		/// <summary>CL register (count low, 8-bit).</summary>
		public const int CL = 1;
		/// <summary>DL register (data low, 8-bit).</summary>
		public const int DL = 2;
		/// <summary>BL register (base low, 8-bit).</summary>
		public const int BL = 3;
		/// <summary>AH register (accumulator high, 8-bit).</summary>
		public const int AH = 4;
		/// <summary>CH register (count high, 8-bit).</summary>
		public const int CH = 5;
		/// <summary>DH register (data high, 8-bit).</summary>
		public const int DH = 6;
		/// <summary>BH register (base high, 8-bit).</summary>
		public const int BH = 7;

		/// <summary>ES segment register (extra segment).</summary>
		public const int ES = 0;
		/// <summary>CS segment register (code segment).</summary>
		public const int CS = 1;
		/// <summary>SS segment register (stack segment).</summary>
		public const int SS = 2;
		/// <summary>DS segment register (data segment).</summary>
		public const int DS = 3;
	}

	/// <summary>
	/// Segment override prefixes.
	/// </summary>
	public static class SegmentPrefixes {
		/// <summary>ES segment override prefix ($26).</summary>
		public const byte ES = 0x26;
		/// <summary>CS segment override prefix ($2e).</summary>
		public const byte CS = 0x2e;
		/// <summary>SS segment override prefix ($36).</summary>
		public const byte SS = 0x36;
		/// <summary>DS segment override prefix ($3e).</summary>
		public const byte DS = 0x3e;
	}

	/// <summary>
	/// Maps register names to their encoding values and sizes.
	/// </summary>
	public static readonly Dictionary<string, (int Encoding, bool IsWord)> RegisterMap = new(StringComparer.OrdinalIgnoreCase) {
		// 16-bit registers
		{ "ax", (Registers.AX, true) },
		{ "cx", (Registers.CX, true) },
		{ "dx", (Registers.DX, true) },
		{ "bx", (Registers.BX, true) },
		{ "sp", (Registers.SP, true) },
		{ "bp", (Registers.BP, true) },
		{ "si", (Registers.SI, true) },
		{ "di", (Registers.DI, true) },
		// 8-bit registers
		{ "al", (Registers.AL, false) },
		{ "cl", (Registers.CL, false) },
		{ "dl", (Registers.DL, false) },
		{ "bl", (Registers.BL, false) },
		{ "ah", (Registers.AH, false) },
		{ "ch", (Registers.CH, false) },
		{ "dh", (Registers.DH, false) },
		{ "bh", (Registers.BH, false) },
		// Segment registers
		{ "es", (Registers.ES, true) },
		{ "cs", (Registers.CS, true) },
		{ "ss", (Registers.SS, true) },
		{ "ds", (Registers.DS, true) },
	};

	/// <summary>
	/// Core 8086 opcodes - single byte instructions.
	/// </summary>
	private static readonly Dictionary<string, byte[]> _impliedOpcodes = new(StringComparer.OrdinalIgnoreCase) {
		// No operand instructions
		{ "nop", new byte[] { 0x90 } },
		{ "hlt", new byte[] { 0xf4 } },
		{ "cmc", new byte[] { 0xf5 } },
		{ "clc", new byte[] { 0xf8 } },
		{ "stc", new byte[] { 0xf9 } },
		{ "cli", new byte[] { 0xfa } },
		{ "sti", new byte[] { 0xfb } },
		{ "cld", new byte[] { 0xfc } },
		{ "std", new byte[] { 0xfd } },
		{ "pushf", new byte[] { 0x9c } },
		{ "popf", new byte[] { 0x9d } },
		{ "pusha", new byte[] { 0x60 } },  // 80186+
		{ "popa", new byte[] { 0x61 } },   // 80186+
		{ "sahf", new byte[] { 0x9e } },
		{ "lahf", new byte[] { 0x9f } },
		{ "cbw", new byte[] { 0x98 } },
		{ "cwd", new byte[] { 0x99 } },
		{ "wait", new byte[] { 0x9b } },
		{ "xlat", new byte[] { 0xd7 } },
		{ "xlatb", new byte[] { 0xd7 } },

		// String operations (with REP prefix)
		{ "movsb", new byte[] { 0xa4 } },
		{ "movsw", new byte[] { 0xa5 } },
		{ "cmpsb", new byte[] { 0xa6 } },
		{ "cmpsw", new byte[] { 0xa7 } },
		{ "stosb", new byte[] { 0xaa } },
		{ "stosw", new byte[] { 0xab } },
		{ "lodsb", new byte[] { 0xac } },
		{ "lodsw", new byte[] { 0xad } },
		{ "scasb", new byte[] { 0xae } },
		{ "scasw", new byte[] { 0xaf } },

		// REP prefixes
		{ "rep", new byte[] { 0xf3 } },
		{ "repe", new byte[] { 0xf3 } },
		{ "repz", new byte[] { 0xf3 } },
		{ "repne", new byte[] { 0xf2 } },
		{ "repnz", new byte[] { 0xf2 } },

		// Returns
		{ "ret", new byte[] { 0xc3 } },
		{ "retf", new byte[] { 0xcb } },
		{ "iret", new byte[] { 0xcf } },

		// AAM, AAD, etc.
		{ "daa", new byte[] { 0x27 } },
		{ "das", new byte[] { 0x2f } },
		{ "aaa", new byte[] { 0x37 } },
		{ "aas", new byte[] { 0x3f } },
		{ "aam", new byte[] { 0xd4, 0x0a } },
		{ "aad", new byte[] { 0xd5, 0x0a } },

		// Interrupt
		{ "int3", new byte[] { 0xcc } },
		{ "into", new byte[] { 0xce } },
	};

	/// <summary>
	/// Conditional jump opcodes (short relative).
	/// </summary>
	private static readonly Dictionary<string, byte> _conditionalJumps = new(StringComparer.OrdinalIgnoreCase) {
		{ "jo", 0x70 },      // Jump if overflow
		{ "jno", 0x71 },     // Jump if not overflow
		{ "jb", 0x72 },      // Jump if below (carry)
		{ "jc", 0x72 },      // Jump if carry
		{ "jnae", 0x72 },    // Jump if not above or equal
		{ "jnb", 0x73 },     // Jump if not below
		{ "jnc", 0x73 },     // Jump if no carry
		{ "jae", 0x73 },     // Jump if above or equal
		{ "jz", 0x74 },      // Jump if zero
		{ "je", 0x74 },      // Jump if equal
		{ "jnz", 0x75 },     // Jump if not zero
		{ "jne", 0x75 },     // Jump if not equal
		{ "jbe", 0x76 },     // Jump if below or equal
		{ "jna", 0x76 },     // Jump if not above
		{ "jnbe", 0x77 },    // Jump if not below or equal
		{ "ja", 0x77 },      // Jump if above
		{ "js", 0x78 },      // Jump if sign (negative)
		{ "jns", 0x79 },     // Jump if not sign
		{ "jp", 0x7a },      // Jump if parity (even)
		{ "jpe", 0x7a },     // Jump if parity even
		{ "jnp", 0x7b },     // Jump if not parity
		{ "jpo", 0x7b },     // Jump if parity odd
		{ "jl", 0x7c },      // Jump if less
		{ "jnge", 0x7c },    // Jump if not greater or equal
		{ "jnl", 0x7d },     // Jump if not less
		{ "jge", 0x7d },     // Jump if greater or equal
		{ "jle", 0x7e },     // Jump if less or equal
		{ "jng", 0x7e },     // Jump if not greater
		{ "jnle", 0x7f },    // Jump if not less or equal
		{ "jg", 0x7f },      // Jump if greater
	};

	/// <summary>
	/// Loop instructions (short relative).
	/// </summary>
	private static readonly Dictionary<string, byte> _loopInstructions = new(StringComparer.OrdinalIgnoreCase) {
		{ "loopnz", 0xe0 },  // Loop while CX!=0 and ZF=0
		{ "loopne", 0xe0 },
		{ "loopz", 0xe1 },   // Loop while CX!=0 and ZF=1
		{ "loope", 0xe1 },
		{ "loop", 0xe2 },    // Loop while CX!=0
		{ "jcxz", 0xe3 },    // Jump if CX=0
	};

	/// <summary>
	/// Gets the opcode for an implied (no operand) instruction.
	/// </summary>
	public static bool TryGetImpliedOpcode(string mnemonic, out byte[] opcode) {
		return _impliedOpcodes.TryGetValue(mnemonic, out opcode!);
	}

	/// <summary>
	/// Gets the opcode for a conditional jump instruction.
	/// </summary>
	public static bool TryGetConditionalJump(string mnemonic, out byte opcode) {
		return _conditionalJumps.TryGetValue(mnemonic, out opcode);
	}

	/// <summary>
	/// Gets the opcode for a loop instruction.
	/// </summary>
	public static bool TryGetLoopInstruction(string mnemonic, out byte opcode) {
		return _loopInstructions.TryGetValue(mnemonic, out opcode);
	}

	/// <summary>
	/// Checks if a string is a valid V30MZ register name.
	/// </summary>
	public static bool IsRegister(string name) {
		return RegisterMap.ContainsKey(name);
	}

	/// <summary>
	/// Checks if a string is a segment register.
	/// </summary>
	public static bool IsSegmentRegister(string name) {
		return name.Equals("es", StringComparison.OrdinalIgnoreCase) ||
			   name.Equals("cs", StringComparison.OrdinalIgnoreCase) ||
			   name.Equals("ss", StringComparison.OrdinalIgnoreCase) ||
			   name.Equals("ds", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Encodes a ModR/M byte.
	/// </summary>
	/// <param name="mod">Mod field (0-3)</param>
	/// <param name="reg">Reg/Opcode field (0-7)</param>
	/// <param name="rm">R/M field (0-7)</param>
	public static byte EncodeModRM(int mod, int reg, int rm) {
		return (byte)((mod << 6) | (reg << 3) | rm);
	}

	/// <summary>
	/// Gets register encoding and determines if it's a word register.
	/// </summary>
	public static bool TryGetRegister(string name, out int encoding, out bool isWord) {
		if (RegisterMap.TryGetValue(name, out var info)) {
			encoding = info.Encoding;
			isWord = info.IsWord;
			return true;
		}

		encoding = 0;
		isWord = false;
		return false;
	}

	/// <summary>
	/// Checks if the instruction is a branch/jump instruction.
	/// </summary>
	public static bool IsBranchInstruction(string mnemonic) {
		var lower = mnemonic.ToLowerInvariant();
		return lower == "jmp" ||
			   lower == "call" ||
			   lower.StartsWith('j') && _conditionalJumps.ContainsKey(lower) ||
			   _loopInstructions.ContainsKey(lower);
	}

	/// <summary>
	/// Gets the list of all supported mnemonics for documentation.
	/// </summary>
	public static IEnumerable<string> GetAllMnemonics() {
		return _impliedOpcodes.Keys
			.Concat(_conditionalJumps.Keys)
			.Concat(_loopInstructions.Keys)
			.Concat(new[] {
				"mov", "add", "sub", "and", "or", "xor", "cmp", "test",
				"push", "pop", "inc", "dec", "neg", "not", "mul", "imul",
				"div", "idiv", "shl", "shr", "sar", "rol", "ror", "rcl", "rcr",
				"in", "out", "int", "jmp", "call", "lea", "les", "lds",
				"xchg", "adc", "sbb"
			})
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(m => m);
	}
}
