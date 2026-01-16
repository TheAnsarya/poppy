// InstructionSetARM7TDMI.cs
// ARM7TDMI instruction set implementation for Game Boy Advance (GBA)
// Supports both ARM (32-bit) and Thumb (16-bit) instruction modes

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Poppy.Core.CodeGen;

/// <summary>
/// ARM7TDMI instruction set implementation for GBA assembly.
/// Supports ARM mode (32-bit instructions) and Thumb mode (16-bit instructions).
/// </summary>
public static class InstructionSetARM7TDMI {
	/// <summary>
	/// Condition codes for ARM instructions (bits 28-31)
	/// </summary>
	public static class Conditions {
		/// <summary>Equal (Z=1)</summary>
		public const byte EQ = 0x0;
		/// <summary>Not Equal (Z=0)</summary>
		public const byte NE = 0x1;
		/// <summary>Carry Set / Unsigned Higher or Same (C=1)</summary>
		public const byte CS = 0x2;
		/// <summary>Alias for CS</summary>
		public const byte HS = 0x2;
		/// <summary>Carry Clear / Unsigned Lower (C=0)</summary>
		public const byte CC = 0x3;
		/// <summary>Alias for CC</summary>
		public const byte LO = 0x3;
		/// <summary>Minus / Negative (N=1)</summary>
		public const byte MI = 0x4;
		/// <summary>Plus / Positive (N=0)</summary>
		public const byte PL = 0x5;
		/// <summary>Overflow Set (V=1)</summary>
		public const byte VS = 0x6;
		/// <summary>Overflow Clear (V=0)</summary>
		public const byte VC = 0x7;
		/// <summary>Unsigned Higher (C=1 and Z=0)</summary>
		public const byte HI = 0x8;
		/// <summary>Unsigned Lower or Same (C=0 or Z=1)</summary>
		public const byte LS = 0x9;
		/// <summary>Signed Greater or Equal (N=V)</summary>
		public const byte GE = 0xa;
		/// <summary>Signed Less Than (N!=V)</summary>
		public const byte LT = 0xb;
		/// <summary>Signed Greater Than (Z=0 and N=V)</summary>
		public const byte GT = 0xc;
		/// <summary>Signed Less or Equal (Z=1 or N!=V)</summary>
		public const byte LE = 0xd;
		/// <summary>Always (unconditional)</summary>
		public const byte AL = 0xe;
		/// <summary>Never (unpredictable, don't use)</summary>
		public const byte NV = 0xf;
	}

	/// <summary>
	/// Shift types for data processing instructions
	/// </summary>
	public static class ShiftTypes {
		/// <summary>Logical Shift Left</summary>
		public const byte LSL = 0x0;
		/// <summary>Logical Shift Right</summary>
		public const byte LSR = 0x1;
		/// <summary>Arithmetic Shift Right</summary>
		public const byte ASR = 0x2;
		/// <summary>Rotate Right</summary>
		public const byte ROR = 0x3;
		/// <summary>Rotate Right with Extend (shift amount = 0)</summary>
		public const byte RRX = 0x3;
	}

	/// <summary>
	/// ARM mode data processing opcodes (bits 21-24)
	/// </summary>
	public static class ArmOpcodes {
		/// <summary>AND</summary>
		public const byte AND = 0x0;
		/// <summary>Exclusive OR</summary>
		public const byte EOR = 0x1;
		/// <summary>Subtract</summary>
		public const byte SUB = 0x2;
		/// <summary>Reverse Subtract</summary>
		public const byte RSB = 0x3;
		/// <summary>Add</summary>
		public const byte ADD = 0x4;
		/// <summary>Add with Carry</summary>
		public const byte ADC = 0x5;
		/// <summary>Subtract with Carry</summary>
		public const byte SBC = 0x6;
		/// <summary>Reverse Subtract with Carry</summary>
		public const byte RSC = 0x7;
		/// <summary>Test (AND, no result)</summary>
		public const byte TST = 0x8;
		/// <summary>Test Equivalence (EOR, no result)</summary>
		public const byte TEQ = 0x9;
		/// <summary>Compare (SUB, no result)</summary>
		public const byte CMP = 0xa;
		/// <summary>Compare Negative (ADD, no result)</summary>
		public const byte CMN = 0xb;
		/// <summary>Logical OR</summary>
		public const byte ORR = 0xc;
		/// <summary>Move</summary>
		public const byte MOV = 0xd;
		/// <summary>Bit Clear</summary>
		public const byte BIC = 0xe;
		/// <summary>Move Not</summary>
		public const byte MVN = 0xf;
	}

	/// <summary>
	/// Valid ARM mode mnemonics (base form, without condition suffix)
	/// </summary>
	private static readonly HashSet<string> ArmMnemonics = new(StringComparer.OrdinalIgnoreCase) {
		// Data processing
		"and", "eor", "sub", "rsb", "add", "adc", "sbc", "rsc",
		"tst", "teq", "cmp", "cmn", "orr", "mov", "bic", "mvn",
		// With S suffix (sets flags)
		"ands", "eors", "subs", "rsbs", "adds", "adcs", "sbcs", "rscs",
		"orrs", "movs", "bics", "mvns",
		// Multiply
		"mul", "mla", "umull", "umlal", "smull", "smlal",
		"muls", "mlas", "umulls", "umlals", "smulls", "smlals",
		// Branch
		"b", "bl", "bx", "blx",
		// Load/Store
		"ldr", "str", "ldrb", "strb", "ldrh", "strh",
		"ldrsb", "ldrsh", "ldm", "stm",
		// Load/Store variants
		"ldmia", "ldmib", "ldmda", "ldmdb", "ldmfd", "ldmed", "ldmfa", "ldmea",
		"stmia", "stmib", "stmda", "stmdb", "stmfd", "stmed", "stmfa", "stmea",
		// Swap
		"swp", "swpb",
		// Coprocessor
		"cdp", "mrc", "mcr", "ldc", "stc",
		// Software interrupt
		"swi", "svc",
		// Status register
		"mrs", "msr",
		// Miscellaneous
		"nop", "clz"
	};

	/// <summary>
	/// Valid Thumb mode mnemonics
	/// </summary>
	private static readonly HashSet<string> ThumbMnemonics = new(StringComparer.OrdinalIgnoreCase) {
		// Data processing
		"add", "adc", "and", "asr", "bic", "cmn", "cmp", "eor",
		"lsl", "lsr", "mov", "mul", "mvn", "neg", "orr", "ror",
		"sbc", "sub", "tst",
		// Branch
		"b", "bl", "bx", "blx",
		// Load/Store
		"ldr", "ldrb", "ldrh", "ldrsb", "ldrsh",
		"str", "strb", "strh",
		// Stack operations
		"push", "pop",
		// Software interrupt
		"swi", "svc",
		// Miscellaneous
		"nop"
	};

	/// <summary>
	/// Condition code suffixes map
	/// </summary>
	private static readonly Dictionary<string, byte> ConditionMap = new(StringComparer.OrdinalIgnoreCase) {
		["eq"] = Conditions.EQ,
		["ne"] = Conditions.NE,
		["cs"] = Conditions.CS,
		["hs"] = Conditions.HS,
		["cc"] = Conditions.CC,
		["lo"] = Conditions.LO,
		["mi"] = Conditions.MI,
		["pl"] = Conditions.PL,
		["vs"] = Conditions.VS,
		["vc"] = Conditions.VC,
		["hi"] = Conditions.HI,
		["ls"] = Conditions.LS,
		["ge"] = Conditions.GE,
		["lt"] = Conditions.LT,
		["gt"] = Conditions.GT,
		["le"] = Conditions.LE,
		["al"] = Conditions.AL
	};

	/// <summary>
	/// Data processing opcode map
	/// </summary>
	private static readonly Dictionary<string, byte> DataProcessingOpcodes = new(StringComparer.OrdinalIgnoreCase) {
		["and"] = ArmOpcodes.AND,
		["eor"] = ArmOpcodes.EOR,
		["sub"] = ArmOpcodes.SUB,
		["rsb"] = ArmOpcodes.RSB,
		["add"] = ArmOpcodes.ADD,
		["adc"] = ArmOpcodes.ADC,
		["sbc"] = ArmOpcodes.SBC,
		["rsc"] = ArmOpcodes.RSC,
		["tst"] = ArmOpcodes.TST,
		["teq"] = ArmOpcodes.TEQ,
		["cmp"] = ArmOpcodes.CMP,
		["cmn"] = ArmOpcodes.CMN,
		["orr"] = ArmOpcodes.ORR,
		["mov"] = ArmOpcodes.MOV,
		["bic"] = ArmOpcodes.BIC,
		["mvn"] = ArmOpcodes.MVN
	};

	/// <summary>
	/// Shift type map
	/// </summary>
	private static readonly Dictionary<string, byte> ShiftMap = new(StringComparer.OrdinalIgnoreCase) {
		["lsl"] = ShiftTypes.LSL,
		["lsr"] = ShiftTypes.LSR,
		["asr"] = ShiftTypes.ASR,
		["ror"] = ShiftTypes.ROR,
		["rrx"] = ShiftTypes.RRX
	};

	/// <summary>
	/// Register name to number map (R0-R15, plus aliases)
	/// </summary>
	private static readonly Dictionary<string, int> RegisterMap = new(StringComparer.OrdinalIgnoreCase) {
		["r0"] = 0, ["r1"] = 1, ["r2"] = 2, ["r3"] = 3,
		["r4"] = 4, ["r5"] = 5, ["r6"] = 6, ["r7"] = 7,
		["r8"] = 8, ["r9"] = 9, ["r10"] = 10, ["r11"] = 11,
		["r12"] = 12, ["r13"] = 13, ["r14"] = 14, ["r15"] = 15,
		// Aliases
		["a1"] = 0, ["a2"] = 1, ["a3"] = 2, ["a4"] = 3,     // AAPCS argument registers
		["v1"] = 4, ["v2"] = 5, ["v3"] = 6, ["v4"] = 7,     // AAPCS variable registers
		["v5"] = 8, ["v6"] = 9, ["v7"] = 10, ["v8"] = 11,   // (cont.)
		["ip"] = 12,  // Intra-Procedure scratch
		["sp"] = 13,  // Stack Pointer
		["lr"] = 14,  // Link Register
		["pc"] = 15   // Program Counter
	};

	/// <summary>
	/// Checks if a mnemonic is valid in ARM mode
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic to check</param>
	/// <returns>True if valid ARM instruction</returns>
	public static bool IsValidArmMnemonic(string mnemonic) {
		if (string.IsNullOrEmpty(mnemonic)) {
			return false;
		}

		var lower = mnemonic.ToLowerInvariant();

		// Check direct match first
		if (ArmMnemonics.Contains(lower)) {
			return true;
		}

		// Check for conditional variants (e.g., "addeq", "bne", "ldrgt")
		// Most ARM instructions can have condition suffixes
		foreach (var cond in ConditionMap.Keys) {
			if (lower.EndsWith(cond, StringComparison.OrdinalIgnoreCase) && lower.Length > cond.Length) {
				var baseMnemonic = lower[..^cond.Length];
				if (ArmMnemonics.Contains(baseMnemonic)) {
					return true;
				}

				// Check with S suffix removed (e.g., "addseq" -> "adds" -> "add")
				if (baseMnemonic.EndsWith("s", StringComparison.OrdinalIgnoreCase) && baseMnemonic.Length > 1) {
					var withoutS = baseMnemonic[..^1];
					if (ArmMnemonics.Contains(withoutS + "s") || ArmMnemonics.Contains(withoutS)) {
						return true;
					}
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Checks if a mnemonic is valid in Thumb mode
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic to check</param>
	/// <returns>True if valid Thumb instruction</returns>
	public static bool IsValidThumbMnemonic(string mnemonic) {
		if (string.IsNullOrEmpty(mnemonic)) {
			return false;
		}

		var lower = mnemonic.ToLowerInvariant();

		// Direct match - Thumb has limited conditional support
		if (ThumbMnemonics.Contains(lower)) {
			return true;
		}

		// Thumb conditional branches (only B instruction supports conditions)
		if (lower.Length > 1 && lower.StartsWith("b", StringComparison.OrdinalIgnoreCase)) {
			var suffix = lower[1..];
			if (ConditionMap.ContainsKey(suffix)) {
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Checks if a mnemonic is valid (either ARM or Thumb mode)
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic to check</param>
	/// <returns>True if valid in either mode</returns>
	public static bool IsValidMnemonic(string mnemonic) {
		return IsValidArmMnemonic(mnemonic) || IsValidThumbMnemonic(mnemonic);
	}

	/// <summary>
	/// Parses a condition code suffix from a mnemonic
	/// </summary>
	/// <param name="mnemonic">Full mnemonic (e.g., "addeq")</param>
	/// <param name="baseMnemonic">Output: base mnemonic without condition</param>
	/// <param name="condition">Output: condition code (AL if none)</param>
	/// <returns>True if parsing succeeded</returns>
	public static bool TryParseCondition(string mnemonic, out string baseMnemonic, out byte condition) {
		baseMnemonic = mnemonic;
		condition = Conditions.AL;

		if (string.IsNullOrEmpty(mnemonic) || mnemonic.Length < 3) {
			return true;
		}

		var lower = mnemonic.ToLowerInvariant();

		// Check for 2-character condition suffix
		var suffix = lower[^2..];
		if (ConditionMap.TryGetValue(suffix, out var cond)) {
			baseMnemonic = lower[..^2];
			condition = cond;
			return true;
		}

		return true;
	}

	/// <summary>
	/// Tries to get a register number from a register name
	/// </summary>
	/// <param name="name">Register name (r0-r15, sp, lr, pc, etc.)</param>
	/// <param name="register">Output: register number (0-15)</param>
	/// <returns>True if valid register name</returns>
	public static bool TryGetRegister(string name, out int register) {
		register = 0;
		if (string.IsNullOrEmpty(name)) {
			return false;
		}

		return RegisterMap.TryGetValue(name, out register);
	}

	/// <summary>
	/// Tries to get a shift type from a shift name
	/// </summary>
	/// <param name="name">Shift type name (lsl, lsr, asr, ror, rrx)</param>
	/// <param name="shiftType">Output: shift type code</param>
	/// <returns>True if valid shift type</returns>
	public static bool TryGetShiftType(string name, out byte shiftType) {
		shiftType = 0;
		if (string.IsNullOrEmpty(name)) {
			return false;
		}

		return ShiftMap.TryGetValue(name, out shiftType);
	}

	/// <summary>
	/// Tries to get the data processing opcode for a mnemonic
	/// </summary>
	/// <param name="mnemonic">Instruction mnemonic (and, add, mov, etc.)</param>
	/// <param name="opcode">Output: 4-bit opcode</param>
	/// <returns>True if valid data processing instruction</returns>
	public static bool TryGetDataProcessingOpcode(string mnemonic, out byte opcode) {
		opcode = 0;
		if (string.IsNullOrEmpty(mnemonic)) {
			return false;
		}

		// Strip S suffix if present
		var baseMnemonic = mnemonic.ToLowerInvariant();
		if (baseMnemonic.EndsWith("s", StringComparison.OrdinalIgnoreCase) && baseMnemonic.Length > 1) {
			baseMnemonic = baseMnemonic[..^1];
		}

		return DataProcessingOpcodes.TryGetValue(baseMnemonic, out opcode);
	}

	/// <summary>
	/// Checks if the instruction sets flags (has S suffix)
	/// </summary>
	/// <param name="mnemonic">Instruction mnemonic</param>
	/// <returns>True if sets flags</returns>
	public static bool SetsFlags(string mnemonic) {
		if (string.IsNullOrEmpty(mnemonic)) {
			return false;
		}

		var lower = mnemonic.ToLowerInvariant();

		// Check for S suffix (before condition)
		// e.g., "adds", "movseq"
		if (lower.EndsWith("s", StringComparison.OrdinalIgnoreCase)) {
			return true;
		}

		// Check for S before condition suffix
		foreach (var cond in ConditionMap.Keys) {
			if (lower.EndsWith(cond, StringComparison.OrdinalIgnoreCase) && lower.Length > cond.Length) {
				var basePart = lower[..^cond.Length];
				if (basePart.EndsWith("s", StringComparison.OrdinalIgnoreCase)) {
					return true;
				}
			}
		}

		// TST, TEQ, CMP, CMN always set flags
		var baseMnemonic = lower;
		TryParseCondition(lower, out baseMnemonic, out _);
		return baseMnemonic is "tst" or "teq" or "cmp" or "cmn";
	}

	/// <summary>
	/// Checks if an instruction is a branch instruction
	/// </summary>
	/// <param name="mnemonic">Instruction mnemonic</param>
	/// <returns>True if branch instruction</returns>
	public static bool IsBranchInstruction(string mnemonic) {
		if (string.IsNullOrEmpty(mnemonic)) {
			return false;
		}

		var lower = mnemonic.ToLowerInvariant();

		// Direct branch mnemonics
		if (lower is "b" or "bl" or "bx" or "blx") {
			return true;
		}

		// Conditional branches (beq, bne, etc.)
		if (lower.StartsWith("b", StringComparison.OrdinalIgnoreCase) && lower.Length <= 4) {
			var suffix = lower[1..];

			// Check for bl with condition
			if (suffix.StartsWith("l", StringComparison.OrdinalIgnoreCase)) {
				var condSuffix = suffix[1..];
				if (condSuffix.Length == 0 || ConditionMap.ContainsKey(condSuffix)) {
					return true;
				}
			}

			// Check for condition suffix
			if (ConditionMap.ContainsKey(suffix)) {
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Checks if an instruction is a load/store instruction
	/// </summary>
	/// <param name="mnemonic">Instruction mnemonic</param>
	/// <returns>True if load/store instruction</returns>
	public static bool IsLoadStoreInstruction(string mnemonic) {
		if (string.IsNullOrEmpty(mnemonic)) {
			return false;
		}

		var lower = mnemonic.ToLowerInvariant();
		TryParseCondition(lower, out var baseMnemonic, out _);

		return baseMnemonic.StartsWith("ldr", StringComparison.OrdinalIgnoreCase) ||
			   baseMnemonic.StartsWith("str", StringComparison.OrdinalIgnoreCase) ||
			   baseMnemonic.StartsWith("ldm", StringComparison.OrdinalIgnoreCase) ||
			   baseMnemonic.StartsWith("stm", StringComparison.OrdinalIgnoreCase) ||
			   baseMnemonic is "push" or "pop";
	}

	/// <summary>
	/// Encodes an ARM data processing instruction (immediate)
	/// Format: cccc 00I opcode S Rn Rd shifter_operand
	/// </summary>
	/// <param name="opcode">Data processing opcode (0-15)</param>
	/// <param name="rd">Destination register (0-15)</param>
	/// <param name="rn">First operand register (0-15)</param>
	/// <param name="immediate">Immediate value (0-255)</param>
	/// <param name="rotate">Rotation amount (0-15, actual rotation = rotate * 2)</param>
	/// <param name="setFlags">Whether to set condition flags</param>
	/// <param name="condition">Condition code (default: AL)</param>
	/// <returns>32-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeDataProcessingImmediate(
		byte opcode,
		int rd,
		int rn,
		int immediate,
		int rotate = 0,
		bool setFlags = false,
		byte condition = Conditions.AL) {
		// Validate parameters
		if (rd < 0 || rd > 15) throw new ArgumentOutOfRangeException(nameof(rd));
		if (rn < 0 || rn > 15) throw new ArgumentOutOfRangeException(nameof(rn));
		if (immediate < 0 || immediate > 255) throw new ArgumentOutOfRangeException(nameof(immediate));
		if (rotate < 0 || rotate > 15) throw new ArgumentOutOfRangeException(nameof(rotate));
		if (opcode > 15) throw new ArgumentOutOfRangeException(nameof(opcode));

		uint instruction = 0;

		// Condition (bits 28-31)
		instruction |= (uint)(condition & 0xf) << 28;

		// Immediate bit (bit 25) - set for immediate operand
		instruction |= 1u << 25;

		// Opcode (bits 21-24)
		instruction |= (uint)(opcode & 0xf) << 21;

		// S flag (bit 20)
		if (setFlags) {
			instruction |= 1u << 20;
		}

		// Rn (bits 16-19)
		instruction |= (uint)(rn & 0xf) << 16;

		// Rd (bits 12-15)
		instruction |= (uint)(rd & 0xf) << 12;

		// Rotate (bits 8-11)
		instruction |= (uint)(rotate & 0xf) << 8;

		// Immediate (bits 0-7)
		instruction |= (uint)(immediate & 0xff);

		// Return as little-endian bytes (ARM uses little-endian)
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff),
			(byte)((instruction >> 16) & 0xff),
			(byte)((instruction >> 24) & 0xff)
		];
	}

	/// <summary>
	/// Encodes an ARM data processing instruction (register)
	/// Format: cccc 00I opcode S Rn Rd shifter_operand
	/// </summary>
	/// <param name="opcode">Data processing opcode (0-15)</param>
	/// <param name="rd">Destination register (0-15)</param>
	/// <param name="rn">First operand register (0-15)</param>
	/// <param name="rm">Second operand register (0-15)</param>
	/// <param name="shiftType">Shift type (LSL, LSR, ASR, ROR)</param>
	/// <param name="shiftAmount">Shift amount (0-31)</param>
	/// <param name="setFlags">Whether to set condition flags</param>
	/// <param name="condition">Condition code (default: AL)</param>
	/// <returns>32-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeDataProcessingRegister(
		byte opcode,
		int rd,
		int rn,
		int rm,
		byte shiftType = ShiftTypes.LSL,
		int shiftAmount = 0,
		bool setFlags = false,
		byte condition = Conditions.AL) {
		// Validate parameters
		if (rd < 0 || rd > 15) throw new ArgumentOutOfRangeException(nameof(rd));
		if (rn < 0 || rn > 15) throw new ArgumentOutOfRangeException(nameof(rn));
		if (rm < 0 || rm > 15) throw new ArgumentOutOfRangeException(nameof(rm));
		if (shiftAmount < 0 || shiftAmount > 31) throw new ArgumentOutOfRangeException(nameof(shiftAmount));
		if (opcode > 15) throw new ArgumentOutOfRangeException(nameof(opcode));
		if (shiftType > 3) throw new ArgumentOutOfRangeException(nameof(shiftType));

		uint instruction = 0;

		// Condition (bits 28-31)
		instruction |= (uint)(condition & 0xf) << 28;

		// Immediate bit (bit 25) - clear for register operand
		// instruction |= 0u << 25;

		// Opcode (bits 21-24)
		instruction |= (uint)(opcode & 0xf) << 21;

		// S flag (bit 20)
		if (setFlags) {
			instruction |= 1u << 20;
		}

		// Rn (bits 16-19)
		instruction |= (uint)(rn & 0xf) << 16;

		// Rd (bits 12-15)
		instruction |= (uint)(rd & 0xf) << 12;

		// Shift amount (bits 7-11)
		instruction |= (uint)(shiftAmount & 0x1f) << 7;

		// Shift type (bits 5-6)
		instruction |= (uint)(shiftType & 0x3) << 5;

		// Rm (bits 0-3)
		instruction |= (uint)(rm & 0xf);

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff),
			(byte)((instruction >> 16) & 0xff),
			(byte)((instruction >> 24) & 0xff)
		];
	}

	/// <summary>
	/// Encodes an ARM branch instruction (B or BL)
	/// Format: cccc 101L offset24
	/// </summary>
	/// <param name="offset">Branch offset (will be divided by 4 and masked to 24 bits)</param>
	/// <param name="link">True for BL (branch with link), false for B</param>
	/// <param name="condition">Condition code (default: AL)</param>
	/// <returns>32-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeBranch(int offset, bool link = false, byte condition = Conditions.AL) {
		uint instruction = 0;

		// Condition (bits 28-31)
		instruction |= (uint)(condition & 0xf) << 28;

		// Branch opcode (bits 25-27 = 101)
		instruction |= 0x5u << 25;

		// Link bit (bit 24)
		if (link) {
			instruction |= 1u << 24;
		}

		// Offset (bits 0-23)
		// The offset is PC-relative, and the actual offset = (signed_imm24 << 2)
		// We need to convert the byte offset to a word offset
		int wordOffset = offset >> 2;
		instruction |= (uint)(wordOffset & 0xffffff);

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff),
			(byte)((instruction >> 16) & 0xff),
			(byte)((instruction >> 24) & 0xff)
		];
	}

	/// <summary>
	/// Encodes an ARM BX (branch and exchange) instruction
	/// Format: cccc 0001 0010 1111 1111 1111 0001 Rm
	/// </summary>
	/// <param name="rm">Register containing target address</param>
	/// <param name="condition">Condition code (default: AL)</param>
	/// <returns>32-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeBranchExchange(int rm, byte condition = Conditions.AL) {
		if (rm < 0 || rm > 15) throw new ArgumentOutOfRangeException(nameof(rm));

		uint instruction = 0;

		// Condition (bits 28-31)
		instruction |= (uint)(condition & 0xf) << 28;

		// Fixed bits for BX: 0001 0010 1111 1111 1111 0001
		instruction |= 0x012fff10u;

		// Rm (bits 0-3)
		instruction |= (uint)(rm & 0xf);

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff),
			(byte)((instruction >> 16) & 0xff),
			(byte)((instruction >> 24) & 0xff)
		];
	}

	/// <summary>
	/// Encodes an ARM LDR/STR instruction (immediate offset)
	/// Format: cccc 01IP UBW L Rn Rd offset12
	/// </summary>
	/// <param name="isLoad">True for LDR, false for STR</param>
	/// <param name="rd">Destination/source register</param>
	/// <param name="rn">Base register</param>
	/// <param name="offset">Immediate offset (-4095 to +4095)</param>
	/// <param name="isByte">True for byte access (LDRB/STRB)</param>
	/// <param name="preIndexed">True for pre-indexed, false for post-indexed</param>
	/// <param name="writeBack">True to write back modified address</param>
	/// <param name="condition">Condition code (default: AL)</param>
	/// <returns>32-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeLoadStoreImmediate(
		bool isLoad,
		int rd,
		int rn,
		int offset,
		bool isByte = false,
		bool preIndexed = true,
		bool writeBack = false,
		byte condition = Conditions.AL) {
		if (rd < 0 || rd > 15) throw new ArgumentOutOfRangeException(nameof(rd));
		if (rn < 0 || rn > 15) throw new ArgumentOutOfRangeException(nameof(rn));
		if (offset < -4095 || offset > 4095) throw new ArgumentOutOfRangeException(nameof(offset));

		uint instruction = 0;

		// Condition (bits 28-31)
		instruction |= (uint)(condition & 0xf) << 28;

		// Fixed bits for LDR/STR immediate: 01
		instruction |= 0x1u << 26;

		// I bit (bit 25) - 0 for immediate offset
		// instruction |= 0u << 25;

		// P bit (bit 24) - pre/post indexing
		if (preIndexed) {
			instruction |= 1u << 24;
		}

		// U bit (bit 23) - add/subtract offset
		if (offset >= 0) {
			instruction |= 1u << 23;
		}

		// B bit (bit 22) - byte/word
		if (isByte) {
			instruction |= 1u << 22;
		}

		// W bit (bit 21) - write-back
		if (writeBack) {
			instruction |= 1u << 21;
		}

		// L bit (bit 20) - load/store
		if (isLoad) {
			instruction |= 1u << 20;
		}

		// Rn (bits 16-19)
		instruction |= (uint)(rn & 0xf) << 16;

		// Rd (bits 12-15)
		instruction |= (uint)(rd & 0xf) << 12;

		// Offset (bits 0-11)
		instruction |= (uint)(Math.Abs(offset) & 0xfff);

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff),
			(byte)((instruction >> 16) & 0xff),
			(byte)((instruction >> 24) & 0xff)
		];
	}

	/// <summary>
	/// Encodes an ARM LDM/STM (load/store multiple) instruction
	/// Format: cccc 100P USWL Rn register_list
	/// </summary>
	/// <param name="isLoad">True for LDM, false for STM</param>
	/// <param name="rn">Base register</param>
	/// <param name="registerList">Bit mask of registers to load/store</param>
	/// <param name="increment">True for increment, false for decrement</param>
	/// <param name="before">True for before (IB/DB), false for after (IA/DA)</param>
	/// <param name="writeBack">True to write back modified address</param>
	/// <param name="condition">Condition code (default: AL)</param>
	/// <returns>32-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeLoadStoreMultiple(
		bool isLoad,
		int rn,
		ushort registerList,
		bool increment = true,
		bool before = false,
		bool writeBack = false,
		byte condition = Conditions.AL) {
		if (rn < 0 || rn > 15) throw new ArgumentOutOfRangeException(nameof(rn));

		uint instruction = 0;

		// Condition (bits 28-31)
		instruction |= (uint)(condition & 0xf) << 28;

		// Fixed bits for LDM/STM: 100
		instruction |= 0x4u << 25;

		// P bit (bit 24) - before/after
		if (before) {
			instruction |= 1u << 24;
		}

		// U bit (bit 23) - increment/decrement
		if (increment) {
			instruction |= 1u << 23;
		}

		// S bit (bit 22) - PSR & force user mode (not commonly used)
		// instruction |= 0u << 22;

		// W bit (bit 21) - write-back
		if (writeBack) {
			instruction |= 1u << 21;
		}

		// L bit (bit 20) - load/store
		if (isLoad) {
			instruction |= 1u << 20;
		}

		// Rn (bits 16-19)
		instruction |= (uint)(rn & 0xf) << 16;

		// Register list (bits 0-15)
		instruction |= registerList;

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff),
			(byte)((instruction >> 16) & 0xff),
			(byte)((instruction >> 24) & 0xff)
		];
	}

	/// <summary>
	/// Encodes an ARM SWI (software interrupt) instruction
	/// Format: cccc 1111 imm24
	/// </summary>
	/// <param name="swiNumber">Software interrupt number (0-0xFFFFFF)</param>
	/// <param name="condition">Condition code (default: AL)</param>
	/// <returns>32-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeSoftwareInterrupt(int swiNumber, byte condition = Conditions.AL) {
		if (swiNumber < 0 || swiNumber > 0xffffff) throw new ArgumentOutOfRangeException(nameof(swiNumber));

		uint instruction = 0;

		// Condition (bits 28-31)
		instruction |= (uint)(condition & 0xf) << 28;

		// Fixed bits for SWI: 1111
		instruction |= 0xfu << 24;

		// SWI number (bits 0-23)
		instruction |= (uint)(swiNumber & 0xffffff);

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff),
			(byte)((instruction >> 16) & 0xff),
			(byte)((instruction >> 24) & 0xff)
		];
	}

	/// <summary>
	/// Encodes a Thumb MOV (immediate) instruction
	/// Format: 001 op Rd imm8
	/// </summary>
	/// <param name="rd">Destination register (0-7)</param>
	/// <param name="immediate">Immediate value (0-255)</param>
	/// <returns>16-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeThumbMovImmediate(int rd, int immediate) {
		if (rd < 0 || rd > 7) throw new ArgumentOutOfRangeException(nameof(rd));
		if (immediate < 0 || immediate > 255) throw new ArgumentOutOfRangeException(nameof(immediate));

		// Format: 0010 0 Rd(3) imm8
		ushort instruction = 0x2000;
		instruction |= (ushort)((rd & 0x7) << 8);
		instruction |= (ushort)(immediate & 0xff);

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff)
		];
	}

	/// <summary>
	/// Encodes a Thumb ADD/SUB (register) instruction
	/// Format: 0001 10 op Rm Rn Rd
	/// </summary>
	/// <param name="rd">Destination register (0-7)</param>
	/// <param name="rn">First operand register (0-7)</param>
	/// <param name="rm">Second operand register (0-7)</param>
	/// <param name="isSubtract">True for SUB, false for ADD</param>
	/// <returns>16-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeThumbAddSubRegister(int rd, int rn, int rm, bool isSubtract = false) {
		if (rd < 0 || rd > 7) throw new ArgumentOutOfRangeException(nameof(rd));
		if (rn < 0 || rn > 7) throw new ArgumentOutOfRangeException(nameof(rn));
		if (rm < 0 || rm > 7) throw new ArgumentOutOfRangeException(nameof(rm));

		// Format: 0001 100 M Rm Rn Rd
		ushort instruction = 0x1800;
		if (isSubtract) {
			instruction |= 0x0200;  // Set subtract bit
		}
		instruction |= (ushort)((rm & 0x7) << 6);
		instruction |= (ushort)((rn & 0x7) << 3);
		instruction |= (ushort)(rd & 0x7);

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff)
		];
	}

	/// <summary>
	/// Encodes a Thumb conditional branch instruction
	/// Format: 1101 cond imm8
	/// </summary>
	/// <param name="offset">Branch offset (-256 to +254, must be even)</param>
	/// <param name="condition">Condition code (0-14, not 15)</param>
	/// <returns>16-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeThumbConditionalBranch(int offset, byte condition) {
		if (condition > 14) throw new ArgumentOutOfRangeException(nameof(condition));
		if (offset < -256 || offset > 254) throw new ArgumentOutOfRangeException(nameof(offset));

		// Format: 1101 cond imm8
		ushort instruction = 0xd000;
		instruction |= (ushort)((condition & 0xf) << 8);

		// Offset is PC-relative, stored as (offset >> 1)
		int encodedOffset = offset >> 1;
		instruction |= (ushort)(encodedOffset & 0xff);

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff)
		];
	}

	/// <summary>
	/// Encodes a Thumb unconditional branch instruction
	/// Format: 11100 imm11
	/// </summary>
	/// <param name="offset">Branch offset (-2048 to +2046, must be even)</param>
	/// <returns>16-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeThumbBranch(int offset) {
		if (offset < -2048 || offset > 2046) throw new ArgumentOutOfRangeException(nameof(offset));

		// Format: 11100 imm11
		ushort instruction = 0xe000;

		// Offset is PC-relative, stored as (offset >> 1)
		int encodedOffset = offset >> 1;
		instruction |= (ushort)(encodedOffset & 0x7ff);

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff)
		];
	}

	/// <summary>
	/// Encodes a Thumb BL (branch with link) instruction
	/// This is a 32-bit instruction split into two 16-bit halves
	/// Format: 11110 offset_hi[10:0] + 11111 offset_lo[10:0]
	/// </summary>
	/// <param name="offset">Branch offset (-4194304 to +4194302, must be even)</param>
	/// <returns>32-bit encoded instruction (two 16-bit halves, little-endian)</returns>
	public static byte[] EncodeThumbBranchLink(int offset) {
		if (offset < -4194304 || offset > 4194302) throw new ArgumentOutOfRangeException(nameof(offset));

		// The offset is PC-relative, stored as 22-bit signed value >> 1
		int encodedOffset = offset >> 1;

		// First half: 11110 offset[21:11]
		ushort firstHalf = 0xf000;
		firstHalf |= (ushort)((encodedOffset >> 11) & 0x7ff);

		// Second half: 11111 offset[10:0]
		ushort secondHalf = 0xf800;
		secondHalf |= (ushort)(encodedOffset & 0x7ff);

		// Return as little-endian bytes
		return [
			(byte)(firstHalf & 0xff),
			(byte)((firstHalf >> 8) & 0xff),
			(byte)(secondHalf & 0xff),
			(byte)((secondHalf >> 8) & 0xff)
		];
	}

	/// <summary>
	/// Encodes a Thumb PUSH instruction
	/// Format: 1011 010R register_list
	/// </summary>
	/// <param name="registerList">Bit mask of registers to push (R0-R7)</param>
	/// <param name="pushLr">Also push LR</param>
	/// <returns>16-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeThumbPush(byte registerList, bool pushLr = false) {
		// Format: 1011 010 R list
		ushort instruction = 0xb400;

		if (pushLr) {
			instruction |= 0x0100;  // R bit for LR
		}

		instruction |= registerList;

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff)
		];
	}

	/// <summary>
	/// Encodes a Thumb POP instruction
	/// Format: 1011 110R register_list
	/// </summary>
	/// <param name="registerList">Bit mask of registers to pop (R0-R7)</param>
	/// <param name="popPc">Also pop PC</param>
	/// <returns>16-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeThumbPop(byte registerList, bool popPc = false) {
		// Format: 1011 110 R list
		ushort instruction = 0xbc00;

		if (popPc) {
			instruction |= 0x0100;  // R bit for PC
		}

		instruction |= registerList;

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff)
		];
	}

	/// <summary>
	/// Encodes a Thumb LDR (PC-relative) instruction
	/// Format: 01001 Rd imm8
	/// </summary>
	/// <param name="rd">Destination register (0-7)</param>
	/// <param name="offset">PC-relative offset (0-1020, word aligned)</param>
	/// <returns>16-bit encoded instruction (little-endian bytes)</returns>
	public static byte[] EncodeThumbLdrPcRelative(int rd, int offset) {
		if (rd < 0 || rd > 7) throw new ArgumentOutOfRangeException(nameof(rd));
		if (offset < 0 || offset > 1020 || (offset & 0x3) != 0) {
			throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be 0-1020 and word aligned");
		}

		// Format: 01001 Rd imm8
		ushort instruction = 0x4800;
		instruction |= (ushort)((rd & 0x7) << 8);
		instruction |= (ushort)((offset >> 2) & 0xff);

		// Return as little-endian bytes
		return [
			(byte)(instruction & 0xff),
			(byte)((instruction >> 8) & 0xff)
		];
	}

	/// <summary>
	/// Creates a register list bitmask from an array of register numbers
	/// </summary>
	/// <param name="registers">Array of register numbers (0-15)</param>
	/// <returns>16-bit bitmask</returns>
	public static ushort CreateRegisterList(params int[] registers) {
		ushort mask = 0;
		foreach (var reg in registers) {
			if (reg < 0 || reg > 15) throw new ArgumentOutOfRangeException(nameof(registers));
			mask |= (ushort)(1 << reg);
		}
		return mask;
	}

	/// <summary>
	/// Calculates the immediate encoding for ARM data processing
	/// ARM uses a "rotated 8-bit immediate" encoding: value = imm8 ROR (rot * 2)
	/// </summary>
	/// <param name="value">Value to encode</param>
	/// <param name="immediate">Output: 8-bit immediate</param>
	/// <param name="rotate">Output: 4-bit rotation</param>
	/// <returns>True if the value can be encoded</returns>
	public static bool TryEncodeImmediate(uint value, out byte immediate, out byte rotate) {
		immediate = 0;
		rotate = 0;

		// Try all possible rotations
		for (int r = 0; r < 16; r++) {
			// Rotate value left by r*2 bits
			int rotation = r * 2;
			uint rotated = (value << rotation) | (value >> (32 - rotation));

			// Check if result fits in 8 bits
			if ((rotated & 0xffffff00) == 0) {
				immediate = (byte)rotated;
				rotate = (byte)r;
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Writes a 32-bit value as little-endian bytes (ARM native byte order)
	/// </summary>
	/// <param name="value">Value to write</param>
	/// <returns>4 bytes in little-endian order</returns>
	public static byte[] WriteLong(int value) {
		return [
			(byte)(value & 0xff),
			(byte)((value >> 8) & 0xff),
			(byte)((value >> 16) & 0xff),
			(byte)((value >> 24) & 0xff)
		];
	}

	/// <summary>
	/// Writes a 16-bit value as little-endian bytes (ARM native byte order)
	/// </summary>
	/// <param name="value">Value to write</param>
	/// <returns>2 bytes in little-endian order</returns>
	public static byte[] WriteHalf(int value) {
		return [
			(byte)(value & 0xff),
			(byte)((value >> 8) & 0xff)
		];
	}
}
