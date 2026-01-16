// InstructionSetHuC6280.cs
// HuC6280 instruction set implementation for TurboGrafx-16 / PC Engine
// Based on 65C02 with extensions for TG16 hardware

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Poppy.Core.CodeGen;

/// <summary>
/// HuC6280 instruction set implementation for TurboGrafx-16 / PC Engine assembly.
/// The HuC6280 is a modified 65C02 with additional instructions for:
/// - Block transfer operations (TAI, TDD, TIA, TII, TIN)
/// - Memory mapping (TAM, TMA)
/// - Set/clear memory bits (SMB, RMB, BBR, BBS)
/// - Speed control (CSL, CSH)
/// - Timer and interrupt handling
/// </summary>
public static class InstructionSetHuC6280 {
	/// <summary>
	/// Addressing mode types for HuC6280
	/// </summary>
	public enum AddressingMode {
		/// <summary>No operand (CLC)</summary>
		Implied,
		/// <summary>Accumulator operand (ASL A)</summary>
		Accumulator,
		/// <summary>Immediate value (LDA #$ff)</summary>
		Immediate,
		/// <summary>Zero page address (LDA $00)</summary>
		ZeroPage,
		/// <summary>Zero page indexed by X (LDA $00,X)</summary>
		ZeroPageX,
		/// <summary>Zero page indexed by Y (LDX $00,Y)</summary>
		ZeroPageY,
		/// <summary>Absolute address (LDA $1234)</summary>
		Absolute,
		/// <summary>Absolute indexed by X (LDA $1234,X)</summary>
		AbsoluteX,
		/// <summary>Absolute indexed by Y (LDA $1234,Y)</summary>
		AbsoluteY,
		/// <summary>Indirect address (JMP ($1234))</summary>
		Indirect,
		/// <summary>Indexed indirect (LDA ($00,X))</summary>
		IndirectX,
		/// <summary>Indirect indexed (LDA ($00),Y)</summary>
		IndirectY,
		/// <summary>Zero page indirect - 65C02 extension (LDA ($00))</summary>
		ZeroPageIndirect,
		/// <summary>Absolute indirect indexed - 65C02 extension (JMP ($1234,X))</summary>
		AbsoluteIndirectX,
		/// <summary>Relative branch (BEQ label)</summary>
		Relative,
		/// <summary>Block transfer (TII $1234,$5678,$9abc)</summary>
		BlockTransfer,
		/// <summary>Zero page bit operation (SMB0 $00, RMB0 $00)</summary>
		ZeroPageBit,
		/// <summary>Zero page with relative branch (BBR0 $00,label / BBS0 $00,label)</summary>
		ZeroPageRelative
	}

	/// <summary>
	/// HuC6280 opcode definitions with base encoding
	/// </summary>
	private static readonly Dictionary<string, Dictionary<AddressingMode, byte>> Opcodes = new(StringComparer.OrdinalIgnoreCase) {
		// Load/Store operations
		["lda"] = new() {
			[AddressingMode.Immediate] = 0xa9,
			[AddressingMode.ZeroPage] = 0xa5,
			[AddressingMode.ZeroPageX] = 0xb5,
			[AddressingMode.Absolute] = 0xad,
			[AddressingMode.AbsoluteX] = 0xbd,
			[AddressingMode.AbsoluteY] = 0xb9,
			[AddressingMode.IndirectX] = 0xa1,
			[AddressingMode.IndirectY] = 0xb1,
			[AddressingMode.ZeroPageIndirect] = 0xb2
		},
		["ldx"] = new() {
			[AddressingMode.Immediate] = 0xa2,
			[AddressingMode.ZeroPage] = 0xa6,
			[AddressingMode.ZeroPageY] = 0xb6,
			[AddressingMode.Absolute] = 0xae,
			[AddressingMode.AbsoluteY] = 0xbe
		},
		["ldy"] = new() {
			[AddressingMode.Immediate] = 0xa0,
			[AddressingMode.ZeroPage] = 0xa4,
			[AddressingMode.ZeroPageX] = 0xb4,
			[AddressingMode.Absolute] = 0xac,
			[AddressingMode.AbsoluteX] = 0xbc
		},
		["sta"] = new() {
			[AddressingMode.ZeroPage] = 0x85,
			[AddressingMode.ZeroPageX] = 0x95,
			[AddressingMode.Absolute] = 0x8d,
			[AddressingMode.AbsoluteX] = 0x9d,
			[AddressingMode.AbsoluteY] = 0x99,
			[AddressingMode.IndirectX] = 0x81,
			[AddressingMode.IndirectY] = 0x91,
			[AddressingMode.ZeroPageIndirect] = 0x92
		},
		["stx"] = new() {
			[AddressingMode.ZeroPage] = 0x86,
			[AddressingMode.ZeroPageY] = 0x96,
			[AddressingMode.Absolute] = 0x8e
		},
		["sty"] = new() {
			[AddressingMode.ZeroPage] = 0x84,
			[AddressingMode.ZeroPageX] = 0x94,
			[AddressingMode.Absolute] = 0x8c
		},
		["stz"] = new() {  // 65C02 extension
			[AddressingMode.ZeroPage] = 0x64,
			[AddressingMode.ZeroPageX] = 0x74,
			[AddressingMode.Absolute] = 0x9c,
			[AddressingMode.AbsoluteX] = 0x9e
		},

		// Arithmetic operations
		["adc"] = new() {
			[AddressingMode.Immediate] = 0x69,
			[AddressingMode.ZeroPage] = 0x65,
			[AddressingMode.ZeroPageX] = 0x75,
			[AddressingMode.Absolute] = 0x6d,
			[AddressingMode.AbsoluteX] = 0x7d,
			[AddressingMode.AbsoluteY] = 0x79,
			[AddressingMode.IndirectX] = 0x61,
			[AddressingMode.IndirectY] = 0x71,
			[AddressingMode.ZeroPageIndirect] = 0x72
		},
		["sbc"] = new() {
			[AddressingMode.Immediate] = 0xe9,
			[AddressingMode.ZeroPage] = 0xe5,
			[AddressingMode.ZeroPageX] = 0xf5,
			[AddressingMode.Absolute] = 0xed,
			[AddressingMode.AbsoluteX] = 0xfd,
			[AddressingMode.AbsoluteY] = 0xf9,
			[AddressingMode.IndirectX] = 0xe1,
			[AddressingMode.IndirectY] = 0xf1,
			[AddressingMode.ZeroPageIndirect] = 0xf2
		},

		// Increment/Decrement
		["inc"] = new() {
			[AddressingMode.Accumulator] = 0x1a,  // 65C02 extension: INC A
			[AddressingMode.ZeroPage] = 0xe6,
			[AddressingMode.ZeroPageX] = 0xf6,
			[AddressingMode.Absolute] = 0xee,
			[AddressingMode.AbsoluteX] = 0xfe
		},
		["dec"] = new() {
			[AddressingMode.Accumulator] = 0x3a,  // 65C02 extension: DEC A
			[AddressingMode.ZeroPage] = 0xc6,
			[AddressingMode.ZeroPageX] = 0xd6,
			[AddressingMode.Absolute] = 0xce,
			[AddressingMode.AbsoluteX] = 0xde
		},
		["inx"] = new() { [AddressingMode.Implied] = 0xe8 },
		["iny"] = new() { [AddressingMode.Implied] = 0xc8 },
		["dex"] = new() { [AddressingMode.Implied] = 0xca },
		["dey"] = new() { [AddressingMode.Implied] = 0x88 },

		// Logical operations
		["and"] = new() {
			[AddressingMode.Immediate] = 0x29,
			[AddressingMode.ZeroPage] = 0x25,
			[AddressingMode.ZeroPageX] = 0x35,
			[AddressingMode.Absolute] = 0x2d,
			[AddressingMode.AbsoluteX] = 0x3d,
			[AddressingMode.AbsoluteY] = 0x39,
			[AddressingMode.IndirectX] = 0x21,
			[AddressingMode.IndirectY] = 0x31,
			[AddressingMode.ZeroPageIndirect] = 0x32
		},
		["ora"] = new() {
			[AddressingMode.Immediate] = 0x09,
			[AddressingMode.ZeroPage] = 0x05,
			[AddressingMode.ZeroPageX] = 0x15,
			[AddressingMode.Absolute] = 0x0d,
			[AddressingMode.AbsoluteX] = 0x1d,
			[AddressingMode.AbsoluteY] = 0x19,
			[AddressingMode.IndirectX] = 0x01,
			[AddressingMode.IndirectY] = 0x11,
			[AddressingMode.ZeroPageIndirect] = 0x12
		},
		["eor"] = new() {
			[AddressingMode.Immediate] = 0x49,
			[AddressingMode.ZeroPage] = 0x45,
			[AddressingMode.ZeroPageX] = 0x55,
			[AddressingMode.Absolute] = 0x4d,
			[AddressingMode.AbsoluteX] = 0x5d,
			[AddressingMode.AbsoluteY] = 0x59,
			[AddressingMode.IndirectX] = 0x41,
			[AddressingMode.IndirectY] = 0x51,
			[AddressingMode.ZeroPageIndirect] = 0x52
		},

		// Shift/Rotate operations
		["asl"] = new() {
			[AddressingMode.Accumulator] = 0x0a,
			[AddressingMode.ZeroPage] = 0x06,
			[AddressingMode.ZeroPageX] = 0x16,
			[AddressingMode.Absolute] = 0x0e,
			[AddressingMode.AbsoluteX] = 0x1e
		},
		["lsr"] = new() {
			[AddressingMode.Accumulator] = 0x4a,
			[AddressingMode.ZeroPage] = 0x46,
			[AddressingMode.ZeroPageX] = 0x56,
			[AddressingMode.Absolute] = 0x4e,
			[AddressingMode.AbsoluteX] = 0x5e
		},
		["rol"] = new() {
			[AddressingMode.Accumulator] = 0x2a,
			[AddressingMode.ZeroPage] = 0x26,
			[AddressingMode.ZeroPageX] = 0x36,
			[AddressingMode.Absolute] = 0x2e,
			[AddressingMode.AbsoluteX] = 0x3e
		},
		["ror"] = new() {
			[AddressingMode.Accumulator] = 0x6a,
			[AddressingMode.ZeroPage] = 0x66,
			[AddressingMode.ZeroPageX] = 0x76,
			[AddressingMode.Absolute] = 0x6e,
			[AddressingMode.AbsoluteX] = 0x7e
		},

		// Compare/Test operations
		["cmp"] = new() {
			[AddressingMode.Immediate] = 0xc9,
			[AddressingMode.ZeroPage] = 0xc5,
			[AddressingMode.ZeroPageX] = 0xd5,
			[AddressingMode.Absolute] = 0xcd,
			[AddressingMode.AbsoluteX] = 0xdd,
			[AddressingMode.AbsoluteY] = 0xd9,
			[AddressingMode.IndirectX] = 0xc1,
			[AddressingMode.IndirectY] = 0xd1,
			[AddressingMode.ZeroPageIndirect] = 0xd2
		},
		["cpx"] = new() {
			[AddressingMode.Immediate] = 0xe0,
			[AddressingMode.ZeroPage] = 0xe4,
			[AddressingMode.Absolute] = 0xec
		},
		["cpy"] = new() {
			[AddressingMode.Immediate] = 0xc0,
			[AddressingMode.ZeroPage] = 0xc4,
			[AddressingMode.Absolute] = 0xcc
		},
		["bit"] = new() {
			[AddressingMode.Immediate] = 0x89,  // 65C02 extension
			[AddressingMode.ZeroPage] = 0x24,
			[AddressingMode.ZeroPageX] = 0x34,  // 65C02 extension
			[AddressingMode.Absolute] = 0x2c,
			[AddressingMode.AbsoluteX] = 0x3c   // 65C02 extension
		},

		// Branch instructions
		["bcc"] = new() { [AddressingMode.Relative] = 0x90 },
		["bcs"] = new() { [AddressingMode.Relative] = 0xb0 },
		["beq"] = new() { [AddressingMode.Relative] = 0xf0 },
		["bmi"] = new() { [AddressingMode.Relative] = 0x30 },
		["bne"] = new() { [AddressingMode.Relative] = 0xd0 },
		["bpl"] = new() { [AddressingMode.Relative] = 0x10 },
		["bvc"] = new() { [AddressingMode.Relative] = 0x50 },
		["bvs"] = new() { [AddressingMode.Relative] = 0x70 },
		["bra"] = new() { [AddressingMode.Relative] = 0x80 },  // 65C02 extension: unconditional branch

		// Jump/Call/Return
		["jmp"] = new() {
			[AddressingMode.Absolute] = 0x4c,
			[AddressingMode.Indirect] = 0x6c,
			[AddressingMode.AbsoluteIndirectX] = 0x7c  // 65C02 extension
		},
		["jsr"] = new() { [AddressingMode.Absolute] = 0x20 },
		["rts"] = new() { [AddressingMode.Implied] = 0x60 },
		["rti"] = new() { [AddressingMode.Implied] = 0x40 },

		// Stack operations
		["pha"] = new() { [AddressingMode.Implied] = 0x48 },
		["php"] = new() { [AddressingMode.Implied] = 0x08 },
		["pla"] = new() { [AddressingMode.Implied] = 0x68 },
		["plp"] = new() { [AddressingMode.Implied] = 0x28 },
		["phx"] = new() { [AddressingMode.Implied] = 0xda },  // 65C02 extension
		["phy"] = new() { [AddressingMode.Implied] = 0x5a },  // 65C02 extension
		["plx"] = new() { [AddressingMode.Implied] = 0xfa },  // 65C02 extension
		["ply"] = new() { [AddressingMode.Implied] = 0x7a },  // 65C02 extension

		// Transfer operations
		["tax"] = new() { [AddressingMode.Implied] = 0xaa },
		["tay"] = new() { [AddressingMode.Implied] = 0xa8 },
		["txa"] = new() { [AddressingMode.Implied] = 0x8a },
		["tya"] = new() { [AddressingMode.Implied] = 0x98 },
		["tsx"] = new() { [AddressingMode.Implied] = 0xba },
		["txs"] = new() { [AddressingMode.Implied] = 0x9a },

		// Flag operations
		["clc"] = new() { [AddressingMode.Implied] = 0x18 },
		["cld"] = new() { [AddressingMode.Implied] = 0xd8 },
		["cli"] = new() { [AddressingMode.Implied] = 0x58 },
		["clv"] = new() { [AddressingMode.Implied] = 0xb8 },
		["sec"] = new() { [AddressingMode.Implied] = 0x38 },
		["sed"] = new() { [AddressingMode.Implied] = 0xf8 },
		["sei"] = new() { [AddressingMode.Implied] = 0x78 },

		// Miscellaneous
		["nop"] = new() { [AddressingMode.Implied] = 0xea },
		["brk"] = new() { [AddressingMode.Implied] = 0x00 },

		// 65C02 extensions
		["trb"] = new() {  // Test and Reset Bits
			[AddressingMode.ZeroPage] = 0x14,
			[AddressingMode.Absolute] = 0x1c
		},
		["tsb"] = new() {  // Test and Set Bits
			[AddressingMode.ZeroPage] = 0x04,
			[AddressingMode.Absolute] = 0x0c
		},

		// HuC6280-specific block transfer instructions
		["tii"] = new() { [AddressingMode.BlockTransfer] = 0x73 },  // Transfer Increment Increment
		["tdd"] = new() { [AddressingMode.BlockTransfer] = 0xc3 },  // Transfer Decrement Decrement
		["tin"] = new() { [AddressingMode.BlockTransfer] = 0xd3 },  // Transfer Increment None
		["tia"] = new() { [AddressingMode.BlockTransfer] = 0xe3 },  // Transfer Increment Alternate
		["tai"] = new() { [AddressingMode.BlockTransfer] = 0xf3 },  // Transfer Alternate Increment

		// HuC6280-specific memory mapping
		["tam"] = new() { [AddressingMode.Immediate] = 0x53 },  // Transfer A to MPR (memory page register)
		["tma"] = new() { [AddressingMode.Immediate] = 0x43 },  // Transfer MPR to A

		// HuC6280-specific CPU speed control
		["csl"] = new() { [AddressingMode.Implied] = 0x54 },  // Clock Speed Low (1.79 MHz)
		["csh"] = new() { [AddressingMode.Implied] = 0xd4 },  // Clock Speed High (7.16 MHz)

		// HuC6280-specific: Set processor status flag T
		["set"] = new() { [AddressingMode.Implied] = 0xf4 },  // Set T flag

		// HuC6280-specific: Store to memory, then A
		["st0"] = new() { [AddressingMode.Immediate] = 0x03 },  // Store to VDC address register ($0000)
		["st1"] = new() { [AddressingMode.Immediate] = 0x13 },  // Store to VDC data register low ($0002)
		["st2"] = new() { [AddressingMode.Immediate] = 0x23 },  // Store to VDC data register high ($0003)

		// HuC6280-specific: Swap A with register
		["sax"] = new() { [AddressingMode.Implied] = 0x22 },  // Swap A and X
		["say"] = new() { [AddressingMode.Implied] = 0x42 },  // Swap A and Y
		["sxy"] = new() { [AddressingMode.Implied] = 0x02 },  // Swap X and Y

		// HuC6280-specific: Test and branch
		["tst"] = new() {  // Test bits - AND immediate with memory
			[AddressingMode.ZeroPage] = 0x83,    // TST #imm,zp
			[AddressingMode.Absolute] = 0x93,    // TST #imm,abs
			[AddressingMode.ZeroPageX] = 0xa3,   // TST #imm,zp,X
			[AddressingMode.AbsoluteX] = 0xb3    // TST #imm,abs,X
		}
	};

	/// <summary>
	/// 65C02/HuC6280 bit manipulation instructions (SMB, RMB, BBR, BBS)
	/// These use opcode + bit number encoding
	/// </summary>
	private static readonly Dictionary<string, byte> BitInstructionBase = new(StringComparer.OrdinalIgnoreCase) {
		["rmb"] = 0x07,  // Reset Memory Bit: RMB0-RMB7 = $07, $17, $27, ... $77
		["smb"] = 0x87,  // Set Memory Bit:   SMB0-SMB7 = $87, $97, $a7, ... $f7
		["bbr"] = 0x0f,  // Branch on Bit Reset: BBR0-BBR7 = $0f, $1f, $2f, ... $7f
		["bbs"] = 0x8f   // Branch on Bit Set:   BBS0-BBS7 = $8f, $9f, $af, ... $ff
	};

	/// <summary>
	/// Checks if a mnemonic is valid
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic to check</param>
	/// <returns>True if valid HuC6280 instruction</returns>
	public static bool IsValidMnemonic(string mnemonic) {
		if (string.IsNullOrEmpty(mnemonic)) {
			return false;
		}

		var lower = mnemonic.ToLowerInvariant();

		// Check standard opcodes
		if (Opcodes.ContainsKey(lower)) {
			return true;
		}

		// Check bit manipulation instructions (SMB0-7, RMB0-7, BBR0-7, BBS0-7)
		if (lower.Length == 4 && char.IsDigit(lower[3])) {
			var baseMnemonic = lower[..3];
			var bitNum = lower[3] - '0';
			if (bitNum >= 0 && bitNum <= 7 && BitInstructionBase.ContainsKey(baseMnemonic)) {
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Tries to get the base opcode for an instruction and addressing mode
	/// </summary>
	/// <param name="mnemonic">Instruction mnemonic</param>
	/// <param name="mode">Addressing mode</param>
	/// <param name="opcode">Output: opcode byte</param>
	/// <returns>True if valid combination</returns>
	public static bool TryGetOpcode(string mnemonic, AddressingMode mode, out byte opcode) {
		opcode = 0;
		if (string.IsNullOrEmpty(mnemonic)) {
			return false;
		}

		var lower = mnemonic.ToLowerInvariant();

		// Check standard opcodes
		if (Opcodes.TryGetValue(lower, out var modes)) {
			return modes.TryGetValue(mode, out opcode);
		}

		// Check bit manipulation instructions
		if (lower.Length == 4 && char.IsDigit(lower[3])) {
			var baseMnemonic = lower[..3];
			var bitNum = lower[3] - '0';

			if (bitNum >= 0 && bitNum <= 7 && BitInstructionBase.TryGetValue(baseMnemonic, out var baseOpcode)) {
				// RMB/SMB use ZeroPageBit mode
				if ((baseMnemonic == "rmb" || baseMnemonic == "smb") && mode == AddressingMode.ZeroPageBit) {
					opcode = (byte)(baseOpcode + (bitNum << 4));
					return true;
				}

				// BBR/BBS use ZeroPageRelative mode
				if ((baseMnemonic == "bbr" || baseMnemonic == "bbs") && mode == AddressingMode.ZeroPageRelative) {
					opcode = (byte)(baseOpcode + (bitNum << 4));
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
	public static int GetInstructionSize(AddressingMode mode) {
		return mode switch {
			AddressingMode.Implied => 1,
			AddressingMode.Accumulator => 1,
			AddressingMode.Immediate => 2,
			AddressingMode.ZeroPage => 2,
			AddressingMode.ZeroPageX => 2,
			AddressingMode.ZeroPageY => 2,
			AddressingMode.Relative => 2,
			AddressingMode.ZeroPageIndirect => 2,
			AddressingMode.IndirectX => 2,
			AddressingMode.IndirectY => 2,
			AddressingMode.ZeroPageBit => 2,
			AddressingMode.Absolute => 3,
			AddressingMode.AbsoluteX => 3,
			AddressingMode.AbsoluteY => 3,
			AddressingMode.Indirect => 3,
			AddressingMode.AbsoluteIndirectX => 3,
			AddressingMode.ZeroPageRelative => 3,
			AddressingMode.BlockTransfer => 7,  // opcode + src(2) + dst(2) + len(2)
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

		// Standard branches
		if (lower is "bcc" or "bcs" or "beq" or "bmi" or "bne" or "bpl" or "bvc" or "bvs" or "bra") {
			return true;
		}

		// BBR/BBS bit-test branches
		if (lower.Length == 4 && char.IsDigit(lower[3])) {
			var baseMnemonic = lower[..3];
			return baseMnemonic is "bbr" or "bbs";
		}

		return false;
	}

	/// <summary>
	/// Checks if an instruction is a block transfer instruction
	/// </summary>
	/// <param name="mnemonic">Instruction mnemonic</param>
	/// <returns>True if block transfer instruction</returns>
	public static bool IsBlockTransfer(string mnemonic) {
		if (string.IsNullOrEmpty(mnemonic)) {
			return false;
		}

		var lower = mnemonic.ToLowerInvariant();
		return lower is "tii" or "tdd" or "tin" or "tia" or "tai";
	}

	/// <summary>
	/// Encodes an instruction with zero page addressing
	/// </summary>
	/// <param name="opcode">Opcode byte</param>
	/// <param name="address">Zero page address (0-255)</param>
	/// <returns>2-byte instruction</returns>
	public static byte[] EncodeZeroPage(byte opcode, byte address) {
		return [opcode, address];
	}

	/// <summary>
	/// Encodes an instruction with immediate addressing
	/// </summary>
	/// <param name="opcode">Opcode byte</param>
	/// <param name="value">Immediate value (0-255)</param>
	/// <returns>2-byte instruction</returns>
	public static byte[] EncodeImmediate(byte opcode, byte value) {
		return [opcode, value];
	}

	/// <summary>
	/// Encodes an instruction with absolute addressing
	/// </summary>
	/// <param name="opcode">Opcode byte</param>
	/// <param name="address">16-bit address</param>
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
	/// Encodes a block transfer instruction (TII, TDD, TIN, TIA, TAI)
	/// </summary>
	/// <param name="opcode">Opcode byte</param>
	/// <param name="source">Source address</param>
	/// <param name="destination">Destination address</param>
	/// <param name="length">Transfer length</param>
	/// <returns>7-byte instruction</returns>
	public static byte[] EncodeBlockTransfer(byte opcode, ushort source, ushort destination, ushort length) {
		return [
			opcode,
			(byte)(source & 0xff),
			(byte)((source >> 8) & 0xff),
			(byte)(destination & 0xff),
			(byte)((destination >> 8) & 0xff),
			(byte)(length & 0xff),
			(byte)((length >> 8) & 0xff)
		];
	}

	/// <summary>
	/// Encodes a BBR/BBS instruction (zero page relative)
	/// </summary>
	/// <param name="opcode">Opcode byte (includes bit number)</param>
	/// <param name="zeroPageAddr">Zero page address to test</param>
	/// <param name="offset">Relative branch offset</param>
	/// <returns>3-byte instruction</returns>
	public static byte[] EncodeZeroPageRelative(byte opcode, byte zeroPageAddr, sbyte offset) {
		return [opcode, zeroPageAddr, (byte)offset];
	}

	/// <summary>
	/// Memory Page Register (MPR) constants for TG16
	/// </summary>
	public static class MemoryPages {
		/// <summary>MPR0 - maps $0000-$1fff</summary>
		public const byte MPR0 = 0x01;
		/// <summary>MPR1 - maps $2000-$3fff</summary>
		public const byte MPR1 = 0x02;
		/// <summary>MPR2 - maps $4000-$5fff</summary>
		public const byte MPR2 = 0x04;
		/// <summary>MPR3 - maps $6000-$7fff</summary>
		public const byte MPR3 = 0x08;
		/// <summary>MPR4 - maps $8000-$9fff</summary>
		public const byte MPR4 = 0x10;
		/// <summary>MPR5 - maps $a000-$bfff</summary>
		public const byte MPR5 = 0x20;
		/// <summary>MPR6 - maps $c000-$dfff</summary>
		public const byte MPR6 = 0x40;
		/// <summary>MPR7 - maps $e000-$ffff</summary>
		public const byte MPR7 = 0x80;
	}

	/// <summary>
	/// VDC (Video Display Controller) register indices
	/// </summary>
	public static class VdcRegisters {
		/// <summary>Memory address write register</summary>
		public const byte MAWR = 0x00;
		/// <summary>Memory address read register</summary>
		public const byte MARR = 0x01;
		/// <summary>VRAM data register</summary>
		public const byte VRR = 0x02;
		/// <summary>Control register</summary>
		public const byte CR = 0x05;
		/// <summary>Raster counter register</summary>
		public const byte RCR = 0x06;
		/// <summary>Background X scroll</summary>
		public const byte BXR = 0x07;
		/// <summary>Background Y scroll</summary>
		public const byte BYR = 0x08;
		/// <summary>Memory access width</summary>
		public const byte MWR = 0x09;
		/// <summary>Horizontal sync register</summary>
		public const byte HSR = 0x0a;
		/// <summary>Horizontal display register</summary>
		public const byte HDR = 0x0b;
		/// <summary>Vertical sync register</summary>
		public const byte VPR = 0x0c;
		/// <summary>Vertical display register</summary>
		public const byte VDW = 0x0d;
		/// <summary>Vertical display end</summary>
		public const byte VCR = 0x0e;
		/// <summary>DMA control register</summary>
		public const byte DCR = 0x0f;
		/// <summary>DMA source address</summary>
		public const byte SOUR = 0x10;
		/// <summary>DMA destination address</summary>
		public const byte DESR = 0x11;
		/// <summary>DMA length</summary>
		public const byte LENR = 0x12;
		/// <summary>Sprite attribute table address</summary>
		public const byte SATB = 0x13;
	}
}
