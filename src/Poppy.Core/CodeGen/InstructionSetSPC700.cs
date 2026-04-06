// InstructionSetSPC700.cs
// SPC700 instruction set implementation for SNES audio coprocessor
// The SPC700 is similar to the 6502 but with unique opcodes and addressing modes

using System.Collections.Frozen;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Addressing mode types for SPC700
/// </summary>
public enum Spc700AddressingMode {
	/// <summary>No operand (NOP)</summary>
	Implied,
	/// <summary>Accumulator operand (ASL A)</summary>
	Accumulator,
	/// <summary>Immediate value (MOV A,#$ff)</summary>
	Immediate,
	/// <summary>Direct page address (MOV A,$00)</summary>
	DirectPage,
	/// <summary>Direct page indexed by X (MOV A,$00+X)</summary>
	DirectPageX,
	/// <summary>Direct page indexed by Y (MOV A,$00+Y)</summary>
	DirectPageY,
	/// <summary>Absolute address (MOV A,!$1234)</summary>
	Absolute,
	/// <summary>Absolute indexed by X (MOV A,!$1234+X)</summary>
	AbsoluteX,
	/// <summary>Absolute indexed by Y (MOV A,!$1234+Y)</summary>
	AbsoluteY,
	/// <summary>Indirect via X register (MOV A,(X))</summary>
	IndirectX,
	/// <summary>Indirect via Y register (MOV A,(Y))</summary>
	IndirectY,
	/// <summary>Indirect via X with post-increment (MOV A,(X)+)</summary>
	IndirectXInc,
	/// <summary>Indirect page indexed by X (MOV A,[$00+X])</summary>
	IndirectPageX,
	/// <summary>Indirect page indexed by Y (MOV A,[$00]+Y)</summary>
	IndirectPageY,
	/// <summary>Direct page bit operation (SET1 $00.0)</summary>
	DirectPageBit,
	/// <summary>Direct page not bit (MOV1 C,$00.0)</summary>
	DirectPageNotBit,
	/// <summary>Absolute bit operation (MOV1 C,!$1234.0)</summary>
	AbsoluteBit,
	/// <summary>Relative branch (BEQ label)</summary>
	Relative,
	/// <summary>Direct page with relative branch (CBNE $00,label)</summary>
	DirectPageRelative,
	/// <summary>Direct page to direct page (MOV $00,$01)</summary>
	DirectPageDirect,
	/// <summary>Immediate to direct page (MOV $00,#$ff)</summary>
	DirectPageImmediate,
	/// <summary>16-bit YA register operations (MOVW YA,$00)</summary>
	YA16,
	/// <summary>Table call (TCALL 0)</summary>
	TableCall,
	/// <summary>Page call (PCALL $ff)</summary>
	PCall,
	/// <summary>Bit direct (SET1 $00.0, CLR1 $00.0)</summary>
	BitDirect,
	/// <summary>Direct page bit with relative branch (BBC $00.0,label)</summary>
	DirectPageBitRelative
}

/// <summary>
/// SPC700 instruction set implementation for SNES audio processor assembly.
/// The SPC700 is used in the Sony S-DSP sound chip in the SNES.
/// It has similarities to the 6502 but with important differences:
/// - Different register names (A, X, Y, SP, PC, PSW, YA)
/// - Different opcode encodings
/// - Additional 16-bit operations
/// - Built-in multiplication and division
/// </summary>
internal static class InstructionSetSPC700 {

	/// <summary>
	/// SPC700 opcode definitions with base encoding
	/// Key is "mnemonic operand_pattern" for complex cases
	/// </summary>
	private static readonly FrozenDictionary<string, Dictionary<Spc700AddressingMode, byte>> Opcodes = new Dictionary<string, Dictionary<Spc700AddressingMode, byte>>(StringComparer.OrdinalIgnoreCase) {
		// Move operations
		["mov a"] = new() {
			[Spc700AddressingMode.Immediate] = 0xe8,
			[Spc700AddressingMode.IndirectX] = 0xe6,
			[Spc700AddressingMode.IndirectXInc] = 0xbf,
			[Spc700AddressingMode.DirectPage] = 0xe4,
			[Spc700AddressingMode.DirectPageX] = 0xf4,
			[Spc700AddressingMode.Absolute] = 0xe5,
			[Spc700AddressingMode.AbsoluteX] = 0xf5,
			[Spc700AddressingMode.AbsoluteY] = 0xf6,
			[Spc700AddressingMode.IndirectPageX] = 0xe7,
			[Spc700AddressingMode.IndirectPageY] = 0xf7
		},
		["mov x"] = new() {
			[Spc700AddressingMode.Immediate] = 0xcd,
			[Spc700AddressingMode.DirectPage] = 0xf8,
			[Spc700AddressingMode.DirectPageY] = 0xf9,
			[Spc700AddressingMode.Absolute] = 0xe9
		},
		["mov y"] = new() {
			[Spc700AddressingMode.Immediate] = 0x8d,
			[Spc700AddressingMode.DirectPage] = 0xeb,
			[Spc700AddressingMode.DirectPageX] = 0xfb,
			[Spc700AddressingMode.Absolute] = 0xec
		},
		["mov (x)"] = new() {
			[Spc700AddressingMode.Accumulator] = 0xc6
		},
		["mov (x)+"] = new() {
			[Spc700AddressingMode.Accumulator] = 0xaf
		},
		["mov dp"] = new() {
			[Spc700AddressingMode.Accumulator] = 0xc4,
			[Spc700AddressingMode.DirectPage] = 0xfa,  // MOV dp,dp
			[Spc700AddressingMode.Immediate] = 0x8f    // MOV dp,#imm
		},
		["mov dp+x"] = new() {
			[Spc700AddressingMode.Accumulator] = 0xd4
		},
		["mov dp+y"] = new() {
			[Spc700AddressingMode.Accumulator] = 0xdb  // MOV dp+Y,A - only for X register dest
		},
		["mov abs"] = new() {
			[Spc700AddressingMode.Accumulator] = 0xc5
		},
		["mov abs+x"] = new() {
			[Spc700AddressingMode.Accumulator] = 0xd5
		},
		["mov abs+y"] = new() {
			[Spc700AddressingMode.Accumulator] = 0xd6
		},
		["mov [dp+x]"] = new() {
			[Spc700AddressingMode.Accumulator] = 0xc7
		},
		["mov [dp]+y"] = new() {
			[Spc700AddressingMode.Accumulator] = 0xd7
		},
		["mov sp"] = new() {
			[Spc700AddressingMode.Implied] = 0xbd  // MOV SP,X
		},
		["mov x,sp"] = new() {
			[Spc700AddressingMode.Implied] = 0x9d
		},
		["mov a,x"] = new() {
			[Spc700AddressingMode.Implied] = 0x7d
		},
		["mov a,y"] = new() {
			[Spc700AddressingMode.Implied] = 0xdd
		},
		["mov x,a"] = new() {
			[Spc700AddressingMode.Implied] = 0x5d
		},
		["mov y,a"] = new() {
			[Spc700AddressingMode.Implied] = 0xfd
		},

		// 16-bit move
		["movw ya"] = new() {
			[Spc700AddressingMode.DirectPage] = 0xba  // MOVW YA,dp
		},
		["movw dp"] = new() {
			[Spc700AddressingMode.YA16] = 0xda  // MOVW dp,YA
		},

		// Arithmetic - ADC
		["adc a"] = new() {
			[Spc700AddressingMode.Immediate] = 0x88,
			[Spc700AddressingMode.IndirectX] = 0x86,
			[Spc700AddressingMode.DirectPage] = 0x84,
			[Spc700AddressingMode.DirectPageX] = 0x94,
			[Spc700AddressingMode.Absolute] = 0x85,
			[Spc700AddressingMode.AbsoluteX] = 0x95,
			[Spc700AddressingMode.AbsoluteY] = 0x96,
			[Spc700AddressingMode.IndirectPageX] = 0x87,
			[Spc700AddressingMode.IndirectPageY] = 0x97
		},
		["adc dp"] = new() {
			[Spc700AddressingMode.DirectPage] = 0x89,   // ADC dp,dp
			[Spc700AddressingMode.Immediate] = 0x98     // ADC dp,#imm
		},
		["adc (x)"] = new() {
			[Spc700AddressingMode.IndirectY] = 0x99  // ADC (X),(Y)
		},

		// Arithmetic - SBC
		["sbc a"] = new() {
			[Spc700AddressingMode.Immediate] = 0xa8,
			[Spc700AddressingMode.IndirectX] = 0xa6,
			[Spc700AddressingMode.DirectPage] = 0xa4,
			[Spc700AddressingMode.DirectPageX] = 0xb4,
			[Spc700AddressingMode.Absolute] = 0xa5,
			[Spc700AddressingMode.AbsoluteX] = 0xb5,
			[Spc700AddressingMode.AbsoluteY] = 0xb6,
			[Spc700AddressingMode.IndirectPageX] = 0xa7,
			[Spc700AddressingMode.IndirectPageY] = 0xb7
		},
		["sbc dp"] = new() {
			[Spc700AddressingMode.DirectPage] = 0xa9,   // SBC dp,dp
			[Spc700AddressingMode.Immediate] = 0xb8     // SBC dp,#imm
		},
		["sbc (x)"] = new() {
			[Spc700AddressingMode.IndirectY] = 0xb9  // SBC (X),(Y)
		},

		// Compare
		["cmp a"] = new() {
			[Spc700AddressingMode.Immediate] = 0x68,
			[Spc700AddressingMode.IndirectX] = 0x66,
			[Spc700AddressingMode.DirectPage] = 0x64,
			[Spc700AddressingMode.DirectPageX] = 0x74,
			[Spc700AddressingMode.Absolute] = 0x65,
			[Spc700AddressingMode.AbsoluteX] = 0x75,
			[Spc700AddressingMode.AbsoluteY] = 0x76,
			[Spc700AddressingMode.IndirectPageX] = 0x67,
			[Spc700AddressingMode.IndirectPageY] = 0x77
		},
		["cmp x"] = new() {
			[Spc700AddressingMode.Immediate] = 0xc8,
			[Spc700AddressingMode.DirectPage] = 0x3e,
			[Spc700AddressingMode.Absolute] = 0x1e
		},
		["cmp y"] = new() {
			[Spc700AddressingMode.Immediate] = 0xad,
			[Spc700AddressingMode.DirectPage] = 0x7e,
			[Spc700AddressingMode.Absolute] = 0x5e
		},
		["cmp dp"] = new() {
			[Spc700AddressingMode.DirectPage] = 0x69,   // CMP dp,dp
			[Spc700AddressingMode.Immediate] = 0x78     // CMP dp,#imm
		},
		["cmp (x)"] = new() {
			[Spc700AddressingMode.IndirectY] = 0x79  // CMP (X),(Y)
		},
		["cmpw ya"] = new() {
			[Spc700AddressingMode.DirectPage] = 0x5a  // CMPW YA,dp
		},

		// Logical - AND
		["and a"] = new() {
			[Spc700AddressingMode.Immediate] = 0x28,
			[Spc700AddressingMode.IndirectX] = 0x26,
			[Spc700AddressingMode.DirectPage] = 0x24,
			[Spc700AddressingMode.DirectPageX] = 0x34,
			[Spc700AddressingMode.Absolute] = 0x25,
			[Spc700AddressingMode.AbsoluteX] = 0x35,
			[Spc700AddressingMode.AbsoluteY] = 0x36,
			[Spc700AddressingMode.IndirectPageX] = 0x27,
			[Spc700AddressingMode.IndirectPageY] = 0x37
		},
		["and dp"] = new() {
			[Spc700AddressingMode.DirectPage] = 0x29,   // AND dp,dp
			[Spc700AddressingMode.Immediate] = 0x38     // AND dp,#imm
		},
		["and (x)"] = new() {
			[Spc700AddressingMode.IndirectY] = 0x39  // AND (X),(Y)
		},

		// Logical - OR
		["or a"] = new() {
			[Spc700AddressingMode.Immediate] = 0x08,
			[Spc700AddressingMode.IndirectX] = 0x06,
			[Spc700AddressingMode.DirectPage] = 0x04,
			[Spc700AddressingMode.DirectPageX] = 0x14,
			[Spc700AddressingMode.Absolute] = 0x05,
			[Spc700AddressingMode.AbsoluteX] = 0x15,
			[Spc700AddressingMode.AbsoluteY] = 0x16,
			[Spc700AddressingMode.IndirectPageX] = 0x07,
			[Spc700AddressingMode.IndirectPageY] = 0x17
		},
		["or dp"] = new() {
			[Spc700AddressingMode.DirectPage] = 0x09,   // OR dp,dp
			[Spc700AddressingMode.Immediate] = 0x18     // OR dp,#imm
		},
		["or (x)"] = new() {
			[Spc700AddressingMode.IndirectY] = 0x19  // OR (X),(Y)
		},

		// Logical - EOR
		["eor a"] = new() {
			[Spc700AddressingMode.Immediate] = 0x48,
			[Spc700AddressingMode.IndirectX] = 0x46,
			[Spc700AddressingMode.DirectPage] = 0x44,
			[Spc700AddressingMode.DirectPageX] = 0x54,
			[Spc700AddressingMode.Absolute] = 0x45,
			[Spc700AddressingMode.AbsoluteX] = 0x55,
			[Spc700AddressingMode.AbsoluteY] = 0x56,
			[Spc700AddressingMode.IndirectPageX] = 0x47,
			[Spc700AddressingMode.IndirectPageY] = 0x57
		},
		["eor dp"] = new() {
			[Spc700AddressingMode.DirectPage] = 0x49,   // EOR dp,dp
			[Spc700AddressingMode.Immediate] = 0x58     // EOR dp,#imm
		},
		["eor (x)"] = new() {
			[Spc700AddressingMode.IndirectY] = 0x59  // EOR (X),(Y)
		},

		// Increment/Decrement
		["inc a"] = new() { [Spc700AddressingMode.Implied] = 0xbc },
		["inc x"] = new() { [Spc700AddressingMode.Implied] = 0x3d },
		["inc y"] = new() { [Spc700AddressingMode.Implied] = 0xfc },
		["inc dp"] = new() {
			[Spc700AddressingMode.DirectPage] = 0xab,
			[Spc700AddressingMode.DirectPageX] = 0xbb
		},
		["inc abs"] = new() { [Spc700AddressingMode.Absolute] = 0xac },
		["incw dp"] = new() { [Spc700AddressingMode.DirectPage] = 0x3a },

		["dec a"] = new() { [Spc700AddressingMode.Implied] = 0x9c },
		["dec x"] = new() { [Spc700AddressingMode.Implied] = 0x1d },
		["dec y"] = new() { [Spc700AddressingMode.Implied] = 0xdc },
		["dec dp"] = new() {
			[Spc700AddressingMode.DirectPage] = 0x8b,
			[Spc700AddressingMode.DirectPageX] = 0x9b
		},
		["dec abs"] = new() { [Spc700AddressingMode.Absolute] = 0x8c },
		["decw dp"] = new() { [Spc700AddressingMode.DirectPage] = 0x1a },

		// Shift/Rotate
		["asl a"] = new() { [Spc700AddressingMode.Implied] = 0x1c },
		["asl dp"] = new() {
			[Spc700AddressingMode.DirectPage] = 0x0b,
			[Spc700AddressingMode.DirectPageX] = 0x1b
		},
		["asl abs"] = new() { [Spc700AddressingMode.Absolute] = 0x0c },

		["lsr a"] = new() { [Spc700AddressingMode.Implied] = 0x5c },
		["lsr dp"] = new() {
			[Spc700AddressingMode.DirectPage] = 0x4b,
			[Spc700AddressingMode.DirectPageX] = 0x5b
		},
		["lsr abs"] = new() { [Spc700AddressingMode.Absolute] = 0x4c },

		["rol a"] = new() { [Spc700AddressingMode.Implied] = 0x3c },
		["rol dp"] = new() {
			[Spc700AddressingMode.DirectPage] = 0x2b,
			[Spc700AddressingMode.DirectPageX] = 0x3b
		},
		["rol abs"] = new() { [Spc700AddressingMode.Absolute] = 0x2c },

		["ror a"] = new() { [Spc700AddressingMode.Implied] = 0x7c },
		["ror dp"] = new() {
			[Spc700AddressingMode.DirectPage] = 0x6b,
			[Spc700AddressingMode.DirectPageX] = 0x7b
		},
		["ror abs"] = new() { [Spc700AddressingMode.Absolute] = 0x6c },

		["xcn a"] = new() { [Spc700AddressingMode.Implied] = 0x9f },  // Exchange nibbles

		// Multiplication/Division
		["mul ya"] = new() { [Spc700AddressingMode.Implied] = 0xcf },  // YA = Y * A
		["div ya,x"] = new() { [Spc700AddressingMode.Implied] = 0x9e }, // A = YA / X, Y = remainder
		["daa a"] = new() { [Spc700AddressingMode.Implied] = 0xdf },   // Decimal adjust for addition
		["das a"] = new() { [Spc700AddressingMode.Implied] = 0xbe },   // Decimal adjust for subtraction

		// 16-bit arithmetic
		["addw ya"] = new() { [Spc700AddressingMode.DirectPage] = 0x7a },  // YA = YA + dp
		["subw ya"] = new() { [Spc700AddressingMode.DirectPage] = 0x9a },  // YA = YA - dp

		// Branches
		["bra"] = new() { [Spc700AddressingMode.Relative] = 0x2f },
		["beq"] = new() { [Spc700AddressingMode.Relative] = 0xf0 },
		["bne"] = new() { [Spc700AddressingMode.Relative] = 0xd0 },
		["bcs"] = new() { [Spc700AddressingMode.Relative] = 0xb0 },
		["bcc"] = new() { [Spc700AddressingMode.Relative] = 0x90 },
		["bvs"] = new() { [Spc700AddressingMode.Relative] = 0x70 },
		["bvc"] = new() { [Spc700AddressingMode.Relative] = 0x50 },
		["bmi"] = new() { [Spc700AddressingMode.Relative] = 0x30 },
		["bpl"] = new() { [Spc700AddressingMode.Relative] = 0x10 },

		// Compare and branch
		["cbne dp"] = new() { [Spc700AddressingMode.DirectPageRelative] = 0x2e },
		["cbne dp+x"] = new() { [Spc700AddressingMode.DirectPageRelative] = 0xde },
		["dbnz dp"] = new() { [Spc700AddressingMode.DirectPageRelative] = 0x6e },
		["dbnz y"] = new() { [Spc700AddressingMode.Relative] = 0xfe },

		// Jumps and Calls
		["jmp abs"] = new() { [Spc700AddressingMode.Absolute] = 0x5f },
		["jmp [abs+x]"] = new() { [Spc700AddressingMode.AbsoluteX] = 0x1f },
		["call abs"] = new() { [Spc700AddressingMode.Absolute] = 0x3f },
		["pcall"] = new() { [Spc700AddressingMode.Immediate] = 0x4f },  // Page call ($ff00+u)
		["tcall"] = new() { [Spc700AddressingMode.TableCall] = 0x01 },  // Table call (n*2 at $ffc0)
		["brk"] = new() { [Spc700AddressingMode.Implied] = 0x0f },
		["ret"] = new() { [Spc700AddressingMode.Implied] = 0x6f },
		["ret1"] = new() { [Spc700AddressingMode.Implied] = 0x7f },      // Return from interrupt

		// Stack operations
		["push a"] = new() { [Spc700AddressingMode.Implied] = 0x2d },
		["push x"] = new() { [Spc700AddressingMode.Implied] = 0x4d },
		["push y"] = new() { [Spc700AddressingMode.Implied] = 0x6d },
		["push psw"] = new() { [Spc700AddressingMode.Implied] = 0x0d },
		["pop a"] = new() { [Spc700AddressingMode.Implied] = 0xae },
		["pop x"] = new() { [Spc700AddressingMode.Implied] = 0xce },
		["pop y"] = new() { [Spc700AddressingMode.Implied] = 0xee },
		["pop psw"] = new() { [Spc700AddressingMode.Implied] = 0x8e },

		// Flag operations
		["clrc"] = new() { [Spc700AddressingMode.Implied] = 0x60 },
		["setc"] = new() { [Spc700AddressingMode.Implied] = 0x80 },
		["notc"] = new() { [Spc700AddressingMode.Implied] = 0xed },
		["clrv"] = new() { [Spc700AddressingMode.Implied] = 0xe0 },
		["clrp"] = new() { [Spc700AddressingMode.Implied] = 0x20 },  // Clear direct page flag
		["setp"] = new() { [Spc700AddressingMode.Implied] = 0x40 },  // Set direct page flag
		["ei"] = new() { [Spc700AddressingMode.Implied] = 0xa0 },    // Enable interrupts
		["di"] = new() { [Spc700AddressingMode.Implied] = 0xc0 },    // Disable interrupts

		// Bit operations
		["set1"] = new() { [Spc700AddressingMode.BitDirect] = 0x02 },   // SET1 dp.n = $02 + n*$20
		["clr1"] = new() { [Spc700AddressingMode.BitDirect] = 0x12 },   // CLR1 dp.n = $12 + n*$20
		["tset1"] = new() { [Spc700AddressingMode.Absolute] = 0x0e },   // Test and set bits
		["tclr1"] = new() { [Spc700AddressingMode.Absolute] = 0x4e },   // Test and clear bits

		// Bit branches
		["bbs"] = new() { [Spc700AddressingMode.DirectPageRelative] = 0x03 },  // BBS dp.n = $03 + n*$20
		["bbc"] = new() { [Spc700AddressingMode.DirectPageRelative] = 0x13 },  // BBC dp.n = $13 + n*$20

		// Carry bit operations
		["and1 c"] = new() {
			[Spc700AddressingMode.AbsoluteBit] = 0x4a,        // AND1 C,abs.bit
			[Spc700AddressingMode.DirectPageNotBit] = 0x6a   // AND1 C,/abs.bit
		},
		["or1 c"] = new() {
			[Spc700AddressingMode.AbsoluteBit] = 0x0a,        // OR1 C,abs.bit
			[Spc700AddressingMode.DirectPageNotBit] = 0x2a   // OR1 C,/abs.bit
		},
		["eor1 c"] = new() {
			[Spc700AddressingMode.AbsoluteBit] = 0x8a         // EOR1 C,abs.bit
		},
		["mov1 c"] = new() {
			[Spc700AddressingMode.AbsoluteBit] = 0xaa         // MOV1 C,abs.bit
		},
		["mov1 abs.bit"] = new() {
			[Spc700AddressingMode.AbsoluteBit] = 0xca         // MOV1 abs.bit,C
		},
		["not1"] = new() {
			[Spc700AddressingMode.AbsoluteBit] = 0xea         // NOT1 abs.bit
		},

		// Miscellaneous
		["nop"] = new() { [Spc700AddressingMode.Implied] = 0x00 },
		["sleep"] = new() { [Spc700AddressingMode.Implied] = 0xef },
		["stop"] = new() { [Spc700AddressingMode.Implied] = 0xff }
	}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Simple opcodes without addressing mode complexity
	/// </summary>
	private static readonly FrozenSet<string> SimpleMnemonics = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
		// Move
		"mov", "movw",
		// Arithmetic
		"adc", "sbc", "cmp", "cmpw", "addw", "subw",
		// Logical
		"and", "or", "eor",
		// Inc/Dec
		"inc", "dec", "incw", "decw",
		// Shift
		"asl", "lsr", "rol", "ror", "xcn",
		// Mul/Div
		"mul", "div", "daa", "das",
		// Branch
		"bra", "beq", "bne", "bcs", "bcc", "bvs", "bvc", "bmi", "bpl",
		"cbne", "dbnz", "bbs", "bbc",
		// Jump/Call
		"jmp", "call", "pcall", "tcall", "brk", "ret", "ret1",
		// Stack
		"push", "pop",
		// Flags
		"clrc", "setc", "notc", "clrv", "clrp", "setp", "ei", "di",
		// Bit
		"set1", "clr1", "tset1", "tclr1", "and1", "or1", "eor1", "mov1", "not1",
		// Misc
		"nop", "sleep", "stop"
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Checks if a mnemonic is valid
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic to check</param>
	/// <returns>True if valid SPC700 instruction</returns>
	public static bool IsValidMnemonic(string mnemonic) {
		if (string.IsNullOrEmpty(mnemonic)) {
			return false;
		}

		var lower = mnemonic.ToLowerInvariant();

		// Check simple mnemonics first
		if (SimpleMnemonics.Contains(lower)) {
			return true;
		}

		// Check if it's a TCALL (tcall0-tcall15)
		if (lower.StartsWith("tcall", StringComparison.OrdinalIgnoreCase) && lower.Length > 5) {
			var numPart = lower[5..];
			if (int.TryParse(numPart, out int n) && n >= 0 && n <= 15) {
				return true;
			}
		}

		// Check if it's a bit instruction with number (set1.0-set1.7, etc.)
		if ((lower.StartsWith("set1", StringComparison.OrdinalIgnoreCase) ||
			 lower.StartsWith("clr1", StringComparison.OrdinalIgnoreCase) ||
			 lower.StartsWith("bbs", StringComparison.OrdinalIgnoreCase) ||
			 lower.StartsWith("bbc", StringComparison.OrdinalIgnoreCase)) &&
			lower.Contains('.')) {
			var dotIndex = lower.IndexOf('.');
			if (dotIndex >= 3 && dotIndex < lower.Length - 1) {
				var bitPart = lower[(dotIndex + 1)..];
				if (int.TryParse(bitPart, out int bit) && bit >= 0 && bit <= 7) {
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Gets the instruction size in bytes for an addressing mode
	/// </summary>
	/// <param name="mode">Addressing mode</param>
	/// <returns>Instruction size in bytes</returns>
	public static int GetInstructionSize(Spc700AddressingMode mode) {
		return mode switch {
			Spc700AddressingMode.Implied => 1,
			Spc700AddressingMode.Accumulator => 1,
			Spc700AddressingMode.Immediate => 2,
			Spc700AddressingMode.DirectPage => 2,
			Spc700AddressingMode.DirectPageX => 2,
			Spc700AddressingMode.DirectPageY => 2,
			Spc700AddressingMode.IndirectX => 1,
			Spc700AddressingMode.IndirectY => 1,
			Spc700AddressingMode.IndirectXInc => 1,
			Spc700AddressingMode.IndirectPageX => 2,
			Spc700AddressingMode.IndirectPageY => 2,
			Spc700AddressingMode.Relative => 2,
			Spc700AddressingMode.DirectPageBit => 2,
			Spc700AddressingMode.BitDirect => 2,
			Spc700AddressingMode.TableCall => 1,
			Spc700AddressingMode.PCall => 2,
			Spc700AddressingMode.Absolute => 3,
			Spc700AddressingMode.AbsoluteX => 3,
			Spc700AddressingMode.AbsoluteY => 3,
			Spc700AddressingMode.AbsoluteBit => 3,
			Spc700AddressingMode.DirectPageNotBit => 3,
			Spc700AddressingMode.DirectPageRelative => 3,
			Spc700AddressingMode.DirectPageDirect => 3,
			Spc700AddressingMode.DirectPageImmediate => 3,
			Spc700AddressingMode.YA16 => 2,
			_ => 1
		};
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
		return lower is "bra" or "beq" or "bne" or "bcs" or "bcc" or
			   "bvs" or "bvc" or "bmi" or "bpl" or "cbne" or "dbnz" ||
			   lower.StartsWith("bbs", StringComparison.OrdinalIgnoreCase) ||
			   lower.StartsWith("bbc", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Encodes a TCALL instruction
	/// </summary>
	/// <param name="n">Table entry (0-15)</param>
	/// <returns>1-byte instruction</returns>
	public static byte[] EncodeTcall(int n) {
		if (n < 0 || n > 15) {
			throw new ArgumentOutOfRangeException(nameof(n), "TCALL number must be 0-15");
		}
		// TCALL n = $n1 (n*16 + 1)
		return [(byte)((n << 4) | 0x01)];
	}

	/// <summary>
	/// Encodes a SET1 or CLR1 instruction
	/// </summary>
	/// <param name="isSet">True for SET1, false for CLR1</param>
	/// <param name="bit">Bit number (0-7)</param>
	/// <param name="dpAddress">Direct page address</param>
	/// <returns>2-byte instruction</returns>
	public static byte[] EncodeBitDirect(bool isSet, int bit, byte dpAddress) {
		if (bit < 0 || bit > 7) {
			throw new ArgumentOutOfRangeException(nameof(bit), "Bit number must be 0-7");
		}
		// SET1 dp.n = $02 + n*$20
		// CLR1 dp.n = $12 + n*$20
		byte baseOpcode = isSet ? (byte)0x02 : (byte)0x12;
		byte opcode = (byte)(baseOpcode + (bit << 5));
		return [opcode, dpAddress];
	}

	/// <summary>
	/// Encodes a BBS or BBC instruction (bit test and branch)
	/// </summary>
	/// <param name="isSet">True for BBS (branch if set), false for BBC (branch if clear)</param>
	/// <param name="bit">Bit number (0-7)</param>
	/// <param name="dpAddress">Direct page address to test</param>
	/// <param name="offset">Relative branch offset</param>
	/// <returns>3-byte instruction</returns>
	public static byte[] EncodeBitBranch(bool isSet, int bit, byte dpAddress, sbyte offset) {
		if (bit < 0 || bit > 7) {
			throw new ArgumentOutOfRangeException(nameof(bit), "Bit number must be 0-7");
		}
		// BBS dp.n = $03 + n*$20
		// BBC dp.n = $13 + n*$20
		byte baseOpcode = isSet ? (byte)0x03 : (byte)0x13;
		byte opcode = (byte)(baseOpcode + (bit << 5));
		return [opcode, dpAddress, (byte)offset];
	}

	/// <summary>
	/// Encodes a simple implied/accumulator instruction
	/// </summary>
	/// <param name="opcode">Opcode byte</param>
	/// <returns>1-byte instruction</returns>
	public static byte[] EncodeImplied(byte opcode) {
		return [opcode];
	}

	/// <summary>
	/// Encodes an instruction with immediate addressing
	/// </summary>
	/// <param name="opcode">Opcode byte</param>
	/// <param name="value">Immediate value</param>
	/// <returns>2-byte instruction</returns>
	public static byte[] EncodeImmediate(byte opcode, byte value) {
		return [opcode, value];
	}

	/// <summary>
	/// Encodes an instruction with direct page addressing
	/// </summary>
	/// <param name="opcode">Opcode byte</param>
	/// <param name="address">Direct page address</param>
	/// <returns>2-byte instruction</returns>
	public static byte[] EncodeDirectPage(byte opcode, byte address) {
		return [opcode, address];
	}

	/// <summary>
	/// Encodes an instruction with absolute addressing
	/// </summary>
	/// <param name="opcode">Opcode byte</param>
	/// <param name="address">16-bit absolute address</param>
	/// <returns>3-byte instruction</returns>
	public static byte[] EncodeAbsolute(byte opcode, ushort address) {
		return [
			opcode,
			(byte)(address & 0xff),
			(byte)((address >> 8) & 0xff)
		];
	}

	/// <summary>
	/// Encodes a relative branch instruction
	/// </summary>
	/// <param name="opcode">Opcode byte</param>
	/// <param name="offset">Signed relative offset (-128 to +127)</param>
	/// <returns>2-byte instruction</returns>
	public static byte[] EncodeRelative(byte opcode, sbyte offset) {
		return [opcode, (byte)offset];
	}

	/// <summary>
	/// Encodes a direct page to direct page instruction (MOV dp,dp)
	/// </summary>
	/// <param name="opcode">Opcode byte</param>
	/// <param name="dest">Destination direct page address</param>
	/// <param name="src">Source direct page address</param>
	/// <returns>3-byte instruction</returns>
	public static byte[] EncodeDirectPageDirect(byte opcode, byte dest, byte src) {
		return [opcode, src, dest];  // Note: Source comes first in encoding
	}

	/// <summary>
	/// Encodes a direct page with relative branch instruction (CBNE, DBNZ)
	/// </summary>
	/// <param name="opcode">Opcode byte</param>
	/// <param name="dpAddress">Direct page address</param>
	/// <param name="offset">Relative branch offset</param>
	/// <returns>3-byte instruction</returns>
	public static byte[] EncodeDirectPageRelative(byte opcode, byte dpAddress, sbyte offset) {
		return [opcode, dpAddress, (byte)offset];
	}

	/// <summary>
	/// SPC700 DSP register addresses (memory-mapped at $00f0-$00ff in direct page $00)
	/// </summary>
	public static class DspRegisters {
		// Per-voice registers (VOL, PITCH, SRCN, ADSR, GAIN, ENVX, OUTX)
		// Voice 0-7, each has base + $10*voice

		/// <summary>Master volume left</summary>
		public const byte MVOLL = 0x0c;
		/// <summary>Master volume right</summary>
		public const byte MVOLR = 0x1c;
		/// <summary>Echo volume left</summary>
		public const byte EVOLL = 0x2c;
		/// <summary>Echo volume right</summary>
		public const byte EVOLR = 0x3c;
		/// <summary>Key on</summary>
		public const byte KON = 0x4c;
		/// <summary>Key off</summary>
		public const byte KOFF = 0x5c;
		/// <summary>Flags (reset, mute, echo, noise clock)</summary>
		public const byte FLG = 0x6c;
		/// <summary>Source end block</summary>
		public const byte ENDX = 0x7c;
		/// <summary>Echo feedback</summary>
		public const byte EFB = 0x0d;
		/// <summary>Pitch modulation enable</summary>
		public const byte PMON = 0x2d;
		/// <summary>Noise enable</summary>
		public const byte NON = 0x3d;
		/// <summary>Echo enable</summary>
		public const byte EON = 0x4d;
		/// <summary>Source directory offset (DIR * $100)</summary>
		public const byte DIR = 0x5d;
		/// <summary>Echo buffer start address</summary>
		public const byte ESA = 0x6d;
		/// <summary>Echo delay (EDL * 16ms)</summary>
		public const byte EDL = 0x7d;
		/// <summary>FIR filter coefficient 0</summary>
		public const byte C0 = 0x0f;
		/// <summary>FIR filter coefficient 1</summary>
		public const byte C1 = 0x1f;
		/// <summary>FIR filter coefficient 2</summary>
		public const byte C2 = 0x2f;
		/// <summary>FIR filter coefficient 3</summary>
		public const byte C3 = 0x3f;
		/// <summary>FIR filter coefficient 4</summary>
		public const byte C4 = 0x4f;
		/// <summary>FIR filter coefficient 5</summary>
		public const byte C5 = 0x5f;
		/// <summary>FIR filter coefficient 6</summary>
		public const byte C6 = 0x6f;
		/// <summary>FIR filter coefficient 7</summary>
		public const byte C7 = 0x7f;

		/// <summary>Gets the volume left register for a voice</summary>
		public static byte VoiceVolL(int voice) => (byte)(0x00 + voice * 0x10);
		/// <summary>Gets the volume right register for a voice</summary>
		public static byte VoiceVolR(int voice) => (byte)(0x01 + voice * 0x10);
		/// <summary>Gets the pitch low register for a voice</summary>
		public static byte VoicePitchL(int voice) => (byte)(0x02 + voice * 0x10);
		/// <summary>Gets the pitch high register for a voice</summary>
		public static byte VoicePitchH(int voice) => (byte)(0x03 + voice * 0x10);
		/// <summary>Gets the source number register for a voice</summary>
		public static byte VoiceSrcn(int voice) => (byte)(0x04 + voice * 0x10);
		/// <summary>Gets the ADSR1 register for a voice</summary>
		public static byte VoiceAdsr1(int voice) => (byte)(0x05 + voice * 0x10);
		/// <summary>Gets the ADSR2 register for a voice</summary>
		public static byte VoiceAdsr2(int voice) => (byte)(0x06 + voice * 0x10);
		/// <summary>Gets the GAIN register for a voice</summary>
		public static byte VoiceGain(int voice) => (byte)(0x07 + voice * 0x10);
		/// <summary>Gets the ENVX register for a voice (read-only)</summary>
		public static byte VoiceEnvx(int voice) => (byte)(0x08 + voice * 0x10);
		/// <summary>Gets the OUTX register for a voice (read-only)</summary>
		public static byte VoiceOutx(int voice) => (byte)(0x09 + voice * 0x10);
	}

	/// <summary>
	/// SPC700 I/O register addresses (memory-mapped)
	/// </summary>
	public static class IoRegisters {
		/// <summary>Test register (normally $0a)</summary>
		public const byte TEST = 0xf0;
		/// <summary>Control register</summary>
		public const byte CONTROL = 0xf1;
		/// <summary>DSP register address</summary>
		public const byte DSPADDR = 0xf2;
		/// <summary>DSP register data</summary>
		public const byte DSPDATA = 0xf3;
		/// <summary>CPU I/O port 0</summary>
		public const byte CPUIO0 = 0xf4;
		/// <summary>CPU I/O port 1</summary>
		public const byte CPUIO1 = 0xf5;
		/// <summary>CPU I/O port 2</summary>
		public const byte CPUIO2 = 0xf6;
		/// <summary>CPU I/O port 3</summary>
		public const byte CPUIO3 = 0xf7;
		/// <summary>Normal timer 0 (8kHz)</summary>
		public const byte T0TARGET = 0xfa;
		/// <summary>Normal timer 1 (8kHz)</summary>
		public const byte T1TARGET = 0xfb;
		/// <summary>Normal timer 2 (64kHz)</summary>
		public const byte T2TARGET = 0xfc;
		/// <summary>Timer 0 counter (read-only)</summary>
		public const byte T0OUT = 0xfd;
		/// <summary>Timer 1 counter (read-only)</summary>
		public const byte T1OUT = 0xfe;
		/// <summary>Timer 2 counter (read-only)</summary>
		public const byte T2OUT = 0xff;
	}

	/// <summary>
	/// Gets all unique mnemonics recognized by the SPC700 instruction set.
	/// </summary>
	public static IEnumerable<string> GetAllMnemonics() {
		return Opcodes.Keys;
	}

	/// <summary>
	/// Tries to get the encoding for a mnemonic and SPC700-local addressing mode.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic (may include operand pattern, e.g., "mov a").</param>
	/// <param name="mode">The SPC700-local addressing mode.</param>
	/// <param name="opcode">The opcode byte if found.</param>
	/// <param name="size">The instruction size in bytes if found.</param>
	/// <returns>True if the encoding was found.</returns>
	public static bool TryGetEncoding(string mnemonic, Spc700AddressingMode mode, out byte opcode, out int size) {
		if (Opcodes.TryGetValue(mnemonic, out var modes) && modes.TryGetValue(mode, out opcode)) {
			size = GetInstructionSize(mode);
			return true;
		}

		opcode = 0;
		size = 0;
		return false;
	}

	/// <summary>
	/// Tries to get the encoding for a mnemonic using the shared parser addressing mode.
	/// Maps the shared mode to the SPC700-local mode before lookup.
	/// </summary>
	public static bool TryGetEncoding(string mnemonic, Parser.AddressingMode sharedMode, out byte opcode, out int size) {
		var localMode = MapAddressingMode(sharedMode);
		if (localMode.HasValue && TryGetEncoding(mnemonic, localMode.Value, out opcode, out size)) {
			return true;
		}
		opcode = 0;
		size = 0;
		return false;
	}

	/// <summary>
	/// Maps the shared parser addressing mode to the SPC700-local addressing mode.
	/// </summary>
	private static Spc700AddressingMode? MapAddressingMode(Parser.AddressingMode mode) {
		return mode switch {
			Parser.AddressingMode.Implied => Spc700AddressingMode.Implied,
			Parser.AddressingMode.Accumulator => Spc700AddressingMode.Accumulator,
			Parser.AddressingMode.Immediate => Spc700AddressingMode.Immediate,
			Parser.AddressingMode.ZeroPage => Spc700AddressingMode.DirectPage,
			Parser.AddressingMode.ZeroPageX => Spc700AddressingMode.DirectPageX,
			Parser.AddressingMode.ZeroPageY => Spc700AddressingMode.DirectPageY,
			Parser.AddressingMode.Absolute => Spc700AddressingMode.Absolute,
			Parser.AddressingMode.AbsoluteX => Spc700AddressingMode.AbsoluteX,
			Parser.AddressingMode.AbsoluteY => Spc700AddressingMode.AbsoluteY,
			Parser.AddressingMode.IndexedIndirect => Spc700AddressingMode.IndirectPageX,
			Parser.AddressingMode.IndirectIndexed => Spc700AddressingMode.IndirectPageY,
			Parser.AddressingMode.Relative => Spc700AddressingMode.Relative,
			_ => null
		};
	}
}