import * as vscode from 'vscode';

// Target architecture type
type TargetArch = 'nes' | 'snes' | 'gb' | 'gba' | 'genesis' | 'sms' | 'tg16' | 'a2600' | 'lynx' | 'ws' | 'spc700' | 'channelf';

// Opcode entry
interface OpcodeEntry {
	opcode: string;
	description: string;
	detail: string;
}

// ============================================================================
// 6502 (NES, base for 65C02/65816)
// ============================================================================
const OPCODES_6502: OpcodeEntry[] = [
	// Load/Store
	{ opcode: 'lda', description: 'Load Accumulator', detail: 'A ← M' },
	{ opcode: 'ldx', description: 'Load X Register', detail: 'X ← M' },
	{ opcode: 'ldy', description: 'Load Y Register', detail: 'Y ← M' },
	{ opcode: 'sta', description: 'Store Accumulator', detail: 'M ← A' },
	{ opcode: 'stx', description: 'Store X Register', detail: 'M ← X' },
	{ opcode: 'sty', description: 'Store Y Register', detail: 'M ← Y' },
	// Transfer
	{ opcode: 'tax', description: 'Transfer A to X', detail: 'X ← A' },
	{ opcode: 'tay', description: 'Transfer A to Y', detail: 'Y ← A' },
	{ opcode: 'txa', description: 'Transfer X to A', detail: 'A ← X' },
	{ opcode: 'tya', description: 'Transfer Y to A', detail: 'A ← Y' },
	{ opcode: 'tsx', description: 'Transfer SP to X', detail: 'X ← SP' },
	{ opcode: 'txs', description: 'Transfer X to SP', detail: 'SP ← X' },
	// Stack
	{ opcode: 'pha', description: 'Push Accumulator', detail: 'push A' },
	{ opcode: 'php', description: 'Push Processor Status', detail: 'push P' },
	{ opcode: 'pla', description: 'Pull Accumulator', detail: 'A ← pull' },
	{ opcode: 'plp', description: 'Pull Processor Status', detail: 'P ← pull' },
	// Arithmetic
	{ opcode: 'adc', description: 'Add with Carry', detail: 'A ← A + M + C' },
	{ opcode: 'sbc', description: 'Subtract with Carry', detail: 'A ← A - M - !C' },
	{ opcode: 'inc', description: 'Increment Memory', detail: 'M ← M + 1' },
	{ opcode: 'dec', description: 'Decrement Memory', detail: 'M ← M - 1' },
	{ opcode: 'inx', description: 'Increment X', detail: 'X ← X + 1' },
	{ opcode: 'iny', description: 'Increment Y', detail: 'Y ← Y + 1' },
	{ opcode: 'dex', description: 'Decrement X', detail: 'X ← X - 1' },
	{ opcode: 'dey', description: 'Decrement Y', detail: 'Y ← Y - 1' },
	// Logic
	{ opcode: 'and', description: 'Logical AND', detail: 'A ← A & M' },
	{ opcode: 'ora', description: 'Logical OR', detail: 'A ← A | M' },
	{ opcode: 'eor', description: 'Exclusive OR', detail: 'A ← A ^ M' },
	{ opcode: 'bit', description: 'Bit Test', detail: 'N ← M7, V ← M6, Z ← A & M' },
	{ opcode: 'cmp', description: 'Compare Accumulator', detail: 'Compare A with M' },
	{ opcode: 'cpx', description: 'Compare X', detail: 'Compare X with M' },
	{ opcode: 'cpy', description: 'Compare Y', detail: 'Compare Y with M' },
	// Shift/Rotate
	{ opcode: 'asl', description: 'Arithmetic Shift Left', detail: 'C ← A7, A ← A << 1' },
	{ opcode: 'lsr', description: 'Logical Shift Right', detail: 'C ← A0, A ← A >> 1' },
	{ opcode: 'rol', description: 'Rotate Left', detail: 'C ← A7, A ← (A << 1) | C' },
	{ opcode: 'ror', description: 'Rotate Right', detail: 'C ← A0, A ← (A >> 1) | (C << 7)' },
	// Branch
	{ opcode: 'bcc', description: 'Branch if Carry Clear', detail: 'Branch if C = 0' },
	{ opcode: 'bcs', description: 'Branch if Carry Set', detail: 'Branch if C = 1' },
	{ opcode: 'beq', description: 'Branch if Equal', detail: 'Branch if Z = 1' },
	{ opcode: 'bne', description: 'Branch if Not Equal', detail: 'Branch if Z = 0' },
	{ opcode: 'bmi', description: 'Branch if Minus', detail: 'Branch if N = 1' },
	{ opcode: 'bpl', description: 'Branch if Plus', detail: 'Branch if N = 0' },
	{ opcode: 'bvc', description: 'Branch if Overflow Clear', detail: 'Branch if V = 0' },
	{ opcode: 'bvs', description: 'Branch if Overflow Set', detail: 'Branch if V = 1' },
	// Jump/Call
	{ opcode: 'jmp', description: 'Jump', detail: 'PC ← address' },
	{ opcode: 'jsr', description: 'Jump to Subroutine', detail: 'push PC+2, PC ← address' },
	{ opcode: 'rts', description: 'Return from Subroutine', detail: 'PC ← pull + 1' },
	{ opcode: 'rti', description: 'Return from Interrupt', detail: 'P ← pull, PC ← pull' },
	{ opcode: 'brk', description: 'Break', detail: 'Software interrupt' },
	// Flags
	{ opcode: 'clc', description: 'Clear Carry', detail: 'C ← 0' },
	{ opcode: 'cld', description: 'Clear Decimal', detail: 'D ← 0' },
	{ opcode: 'cli', description: 'Clear Interrupt Disable', detail: 'I ← 0' },
	{ opcode: 'clv', description: 'Clear Overflow', detail: 'V ← 0' },
	{ opcode: 'sec', description: 'Set Carry', detail: 'C ← 1' },
	{ opcode: 'sed', description: 'Set Decimal', detail: 'D ← 1' },
	{ opcode: 'sei', description: 'Set Interrupt Disable', detail: 'I ← 1' },
	// Misc
	{ opcode: 'nop', description: 'No Operation', detail: 'Do nothing' },
];

// ============================================================================
// 65SC02 (Atari Lynx) — extends 6502
// ============================================================================
const OPCODES_65SC02: OpcodeEntry[] = [
	...OPCODES_6502,
	{ opcode: 'bra', description: 'Branch Always', detail: 'Always branch (relative)' },
	{ opcode: 'phx', description: 'Push X', detail: 'push X' },
	{ opcode: 'phy', description: 'Push Y', detail: 'push Y' },
	{ opcode: 'plx', description: 'Pull X', detail: 'X ← pull' },
	{ opcode: 'ply', description: 'Pull Y', detail: 'Y ← pull' },
	{ opcode: 'stz', description: 'Store Zero', detail: 'M ← 0' },
	{ opcode: 'trb', description: 'Test and Reset Bits', detail: 'Z ← A & M, M ← M & ~A' },
	{ opcode: 'tsb', description: 'Test and Set Bits', detail: 'Z ← A & M, M ← M | A' },
];

// ============================================================================
// 65816 (SNES) — extends 6502
// ============================================================================
const OPCODES_65816: OpcodeEntry[] = [
	...OPCODES_6502,
	{ opcode: 'stz', description: 'Store Zero', detail: 'M ← 0' },
	{ opcode: 'tcd', description: 'Transfer C to DP', detail: 'DP ← C' },
	{ opcode: 'tcs', description: 'Transfer C to SP', detail: 'SP ← C' },
	{ opcode: 'tdc', description: 'Transfer DP to C', detail: 'C ← DP' },
	{ opcode: 'tsc', description: 'Transfer SP to C', detail: 'C ← SP' },
	{ opcode: 'txy', description: 'Transfer X to Y', detail: 'Y ← X' },
	{ opcode: 'tyx', description: 'Transfer Y to X', detail: 'X ← Y' },
	{ opcode: 'phx', description: 'Push X', detail: 'push X' },
	{ opcode: 'phy', description: 'Push Y', detail: 'push Y' },
	{ opcode: 'plx', description: 'Pull X', detail: 'X ← pull' },
	{ opcode: 'ply', description: 'Pull Y', detail: 'Y ← pull' },
	{ opcode: 'phb', description: 'Push Data Bank', detail: 'push DB' },
	{ opcode: 'phd', description: 'Push Direct Page', detail: 'push DP' },
	{ opcode: 'phk', description: 'Push Program Bank', detail: 'push PB' },
	{ opcode: 'plb', description: 'Pull Data Bank', detail: 'DB ← pull' },
	{ opcode: 'pld', description: 'Pull Direct Page', detail: 'DP ← pull' },
	{ opcode: 'pea', description: 'Push Effective Address', detail: 'push address' },
	{ opcode: 'pei', description: 'Push Effective Indirect', detail: 'push (dp)' },
	{ opcode: 'per', description: 'Push PC Relative', detail: 'push PC+offset' },
	{ opcode: 'bra', description: 'Branch Always', detail: 'Always branch' },
	{ opcode: 'brl', description: 'Branch Long', detail: '16-bit branch' },
	{ opcode: 'jml', description: 'Jump Long', detail: 'PC:PB ← 24-bit address' },
	{ opcode: 'jsl', description: 'Jump Subroutine Long', detail: 'push PB:PC+3, PC:PB ← address' },
	{ opcode: 'rtl', description: 'Return from Subroutine Long', detail: 'PC:PB ← pull + 1' },
	{ opcode: 'cop', description: 'Coprocessor', detail: 'Coprocessor interrupt' },
	{ opcode: 'rep', description: 'Reset Processor Status', detail: 'P ← P & ~M' },
	{ opcode: 'sep', description: 'Set Processor Status', detail: 'P ← P | M' },
	{ opcode: 'xce', description: 'Exchange Carry/Emulation', detail: 'C ↔ E' },
	{ opcode: 'wai', description: 'Wait for Interrupt', detail: 'Wait until IRQ or NMI' },
	{ opcode: 'stp', description: 'Stop Processor', detail: 'Stop until reset' },
	{ opcode: 'wdm', description: 'Reserved', detail: 'Reserved for future use' },
	{ opcode: 'xba', description: 'Exchange B and A', detail: 'A ↔ B (swap bytes)' },
	{ opcode: 'mvn', description: 'Move Negative', detail: 'Block move decrement' },
	{ opcode: 'mvp', description: 'Move Positive', detail: 'Block move increment' },
	{ opcode: 'trb', description: 'Test and Reset Bits', detail: 'Z ← A & M, M ← M & ~A' },
	{ opcode: 'tsb', description: 'Test and Set Bits', detail: 'Z ← A & M, M ← M | A' },
];

// ============================================================================
// SM83 (Game Boy)
// ============================================================================
const OPCODES_SM83: OpcodeEntry[] = [
	// 8-bit Load
	{ opcode: 'ld', description: 'Load', detail: 'Load value into register/memory' },
	{ opcode: 'ldh', description: 'Load High', detail: 'Load to/from $ff00+n' },
	{ opcode: 'ldi', description: 'Load and Increment', detail: 'ld [hl], a; inc hl' },
	{ opcode: 'ldd', description: 'Load and Decrement', detail: 'ld [hl], a; dec hl' },
	// Stack
	{ opcode: 'push', description: 'Push', detail: 'Push 16-bit register to stack' },
	{ opcode: 'pop', description: 'Pop', detail: 'Pop 16-bit value from stack' },
	// Arithmetic
	{ opcode: 'add', description: 'Add', detail: 'Add value to register' },
	{ opcode: 'adc', description: 'Add with Carry', detail: 'Add with carry flag' },
	{ opcode: 'sub', description: 'Subtract', detail: 'Subtract value from A' },
	{ opcode: 'sbc', description: 'Subtract with Carry', detail: 'Subtract with carry' },
	{ opcode: 'inc', description: 'Increment', detail: 'Increment register/memory' },
	{ opcode: 'dec', description: 'Decrement', detail: 'Decrement register/memory' },
	{ opcode: 'daa', description: 'Decimal Adjust', detail: 'Adjust A for BCD' },
	{ opcode: 'cpl', description: 'Complement', detail: 'A ← ~A' },
	// Logic
	{ opcode: 'and', description: 'Logical AND', detail: 'A ← A & operand' },
	{ opcode: 'or', description: 'Logical OR', detail: 'A ← A | operand' },
	{ opcode: 'xor', description: 'Logical XOR', detail: 'A ← A ^ operand' },
	{ opcode: 'cp', description: 'Compare', detail: 'Compare A with operand' },
	{ opcode: 'ccf', description: 'Complement Carry', detail: 'C ← ~C' },
	{ opcode: 'scf', description: 'Set Carry Flag', detail: 'C ← 1' },
	// Rotate/Shift
	{ opcode: 'rlca', description: 'Rotate Left Circular A', detail: 'Rotate A left' },
	{ opcode: 'rla', description: 'Rotate Left A', detail: 'Rotate A left through carry' },
	{ opcode: 'rrca', description: 'Rotate Right Circular A', detail: 'Rotate A right' },
	{ opcode: 'rra', description: 'Rotate Right A', detail: 'Rotate A right through carry' },
	{ opcode: 'rlc', description: 'Rotate Left Circular', detail: 'Rotate left' },
	{ opcode: 'rl', description: 'Rotate Left', detail: 'Rotate left through carry' },
	{ opcode: 'rrc', description: 'Rotate Right Circular', detail: 'Rotate right' },
	{ opcode: 'rr', description: 'Rotate Right', detail: 'Rotate right through carry' },
	{ opcode: 'sla', description: 'Shift Left Arithmetic', detail: 'Shift left, LSB=0' },
	{ opcode: 'sra', description: 'Shift Right Arithmetic', detail: 'Shift right, MSB unchanged' },
	{ opcode: 'srl', description: 'Shift Right Logical', detail: 'Shift right, MSB=0' },
	{ opcode: 'swap', description: 'Swap Nibbles', detail: 'Swap upper/lower 4 bits' },
	// Bit Operations
	{ opcode: 'bit', description: 'Test Bit', detail: 'Test bit in register' },
	{ opcode: 'set', description: 'Set Bit', detail: 'Set bit to 1' },
	{ opcode: 'res', description: 'Reset Bit', detail: 'Reset bit to 0' },
	// Jump/Call
	{ opcode: 'jp', description: 'Jump', detail: 'Jump to address' },
	{ opcode: 'jr', description: 'Jump Relative', detail: 'Jump relative (signed offset)' },
	{ opcode: 'call', description: 'Call', detail: 'Call subroutine' },
	{ opcode: 'ret', description: 'Return', detail: 'Return from subroutine' },
	{ opcode: 'reti', description: 'Return from Interrupt', detail: 'Return and enable interrupts' },
	{ opcode: 'rst', description: 'Restart', detail: 'Call fixed address ($00-$38)' },
	// Control
	{ opcode: 'halt', description: 'Halt', detail: 'Wait for interrupt' },
	{ opcode: 'stop', description: 'Stop', detail: 'Stop CPU and LCD' },
	{ opcode: 'di', description: 'Disable Interrupts', detail: 'Disable interrupt handling' },
	{ opcode: 'ei', description: 'Enable Interrupts', detail: 'Enable interrupt handling' },
	{ opcode: 'nop', description: 'No Operation', detail: 'Do nothing' },
];

// ============================================================================
// Z80 (Sega Master System / Game Gear)
// ============================================================================
const OPCODES_Z80: OpcodeEntry[] = [
	// Load
	{ opcode: 'ld', description: 'Load', detail: 'Load value into register/memory' },
	{ opcode: 'push', description: 'Push', detail: 'Push 16-bit register to stack' },
	{ opcode: 'pop', description: 'Pop', detail: 'Pop 16-bit value from stack' },
	{ opcode: 'ex', description: 'Exchange', detail: 'Exchange register pairs' },
	{ opcode: 'exx', description: 'Exchange All', detail: 'Exchange BC/DE/HL with shadows' },
	// Arithmetic
	{ opcode: 'add', description: 'Add', detail: 'Add to accumulator or HL' },
	{ opcode: 'adc', description: 'Add with Carry', detail: 'Add with carry' },
	{ opcode: 'sub', description: 'Subtract', detail: 'Subtract from accumulator' },
	{ opcode: 'sbc', description: 'Subtract with Carry', detail: 'Subtract with carry' },
	{ opcode: 'inc', description: 'Increment', detail: 'Increment register/memory' },
	{ opcode: 'dec', description: 'Decrement', detail: 'Decrement register/memory' },
	{ opcode: 'daa', description: 'Decimal Adjust', detail: 'Adjust A for BCD' },
	{ opcode: 'neg', description: 'Negate', detail: 'A ← 0 - A' },
	{ opcode: 'cpl', description: 'Complement', detail: 'A ← ~A' },
	// Logic
	{ opcode: 'and', description: 'Logical AND', detail: 'A ← A & operand' },
	{ opcode: 'or', description: 'Logical OR', detail: 'A ← A | operand' },
	{ opcode: 'xor', description: 'Logical XOR', detail: 'A ← A ^ operand' },
	{ opcode: 'cp', description: 'Compare', detail: 'Compare A with operand' },
	{ opcode: 'ccf', description: 'Complement Carry', detail: 'C ← ~C' },
	{ opcode: 'scf', description: 'Set Carry Flag', detail: 'C ← 1' },
	// Rotate/Shift
	{ opcode: 'rlca', description: 'Rotate Left Circular A', detail: 'Rotate A left' },
	{ opcode: 'rla', description: 'Rotate Left A', detail: 'Rotate A left through carry' },
	{ opcode: 'rrca', description: 'Rotate Right Circular A', detail: 'Rotate A right' },
	{ opcode: 'rra', description: 'Rotate Right A', detail: 'Rotate A right through carry' },
	{ opcode: 'rlc', description: 'Rotate Left Circular', detail: 'Rotate left' },
	{ opcode: 'rl', description: 'Rotate Left', detail: 'Rotate left through carry' },
	{ opcode: 'rrc', description: 'Rotate Right Circular', detail: 'Rotate right' },
	{ opcode: 'rr', description: 'Rotate Right', detail: 'Rotate right through carry' },
	{ opcode: 'sla', description: 'Shift Left Arithmetic', detail: 'Shift left, LSB=0' },
	{ opcode: 'sra', description: 'Shift Right Arithmetic', detail: 'Shift right, MSB unchanged' },
	{ opcode: 'srl', description: 'Shift Right Logical', detail: 'Shift right, MSB=0' },
	{ opcode: 'rld', description: 'Rotate Left Digit', detail: 'BCD rotate left through A and (HL)' },
	{ opcode: 'rrd', description: 'Rotate Right Digit', detail: 'BCD rotate right through A and (HL)' },
	// Bit Operations
	{ opcode: 'bit', description: 'Test Bit', detail: 'Test bit in register' },
	{ opcode: 'set', description: 'Set Bit', detail: 'Set bit to 1' },
	{ opcode: 'res', description: 'Reset Bit', detail: 'Reset bit to 0' },
	// Jump/Call
	{ opcode: 'jp', description: 'Jump', detail: 'Jump to address' },
	{ opcode: 'jr', description: 'Jump Relative', detail: 'Jump relative (signed offset)' },
	{ opcode: 'call', description: 'Call', detail: 'Call subroutine' },
	{ opcode: 'ret', description: 'Return', detail: 'Return from subroutine' },
	{ opcode: 'reti', description: 'Return from Interrupt', detail: 'Return from maskable interrupt' },
	{ opcode: 'retn', description: 'Return from NMI', detail: 'Return from non-maskable interrupt' },
	{ opcode: 'rst', description: 'Restart', detail: 'Call fixed address ($00-$38)' },
	{ opcode: 'djnz', description: 'Decrement and Jump if Not Zero', detail: 'B--; if B!=0 branch' },
	// Block Transfer/Search
	{ opcode: 'ldi', description: 'Load and Increment', detail: '(DE)<-(HL); DE++; HL++; BC--' },
	{ opcode: 'ldir', description: 'Load, Increment, Repeat', detail: 'Repeat LDI until BC=0' },
	{ opcode: 'ldd', description: 'Load and Decrement', detail: '(DE)<-(HL); DE--; HL--; BC--' },
	{ opcode: 'lddr', description: 'Load, Decrement, Repeat', detail: 'Repeat LDD until BC=0' },
	{ opcode: 'cpi', description: 'Compare and Increment', detail: 'Compare A-(HL); HL++; BC--' },
	{ opcode: 'cpir', description: 'Compare, Increment, Repeat', detail: 'Repeat CPI until match/BC=0' },
	{ opcode: 'cpd', description: 'Compare and Decrement', detail: 'Compare A-(HL); HL--; BC--' },
	{ opcode: 'cpdr', description: 'Compare, Decrement, Repeat', detail: 'Repeat CPD until match/BC=0' },
	// I/O
	{ opcode: 'in', description: 'Input', detail: 'Read from I/O port' },
	{ opcode: 'out', description: 'Output', detail: 'Write to I/O port' },
	{ opcode: 'ini', description: 'Input and Increment', detail: '(HL)<-IN(C); HL++; B--' },
	{ opcode: 'inir', description: 'Input, Increment, Repeat', detail: 'Repeat INI until B=0' },
	{ opcode: 'ind', description: 'Input and Decrement', detail: '(HL)<-IN(C); HL--; B--' },
	{ opcode: 'indr', description: 'Input, Decrement, Repeat', detail: 'Repeat IND until B=0' },
	{ opcode: 'outi', description: 'Output and Increment', detail: 'OUT(C)<-(HL); HL++; B--' },
	{ opcode: 'otir', description: 'Output, Increment, Repeat', detail: 'Repeat OUTI until B=0' },
	{ opcode: 'outd', description: 'Output and Decrement', detail: 'OUT(C)<-(HL); HL--; B--' },
	{ opcode: 'otdr', description: 'Output, Decrement, Repeat', detail: 'Repeat OUTD until B=0' },
	// Control
	{ opcode: 'halt', description: 'Halt', detail: 'Wait for interrupt' },
	{ opcode: 'di', description: 'Disable Interrupts', detail: 'Disable maskable interrupts' },
	{ opcode: 'ei', description: 'Enable Interrupts', detail: 'Enable maskable interrupts' },
	{ opcode: 'im', description: 'Interrupt Mode', detail: 'Set interrupt mode (0/1/2)' },
	{ opcode: 'nop', description: 'No Operation', detail: 'Do nothing' },
];

// ============================================================================
// M68000 (Sega Genesis / Mega Drive)
// ============================================================================
const OPCODES_M68000: OpcodeEntry[] = [
	// Data Movement
	{ opcode: 'move', description: 'Move Data', detail: 'dest <- src' },
	{ opcode: 'movea', description: 'Move to Address Register', detail: 'An <- src' },
	{ opcode: 'moveq', description: 'Move Quick', detail: 'Dn <- sign-extended 8-bit' },
	{ opcode: 'movem', description: 'Move Multiple', detail: 'Move multiple registers to/from memory' },
	{ opcode: 'movep', description: 'Move Peripheral', detail: 'Move data between register and peripheral' },
	{ opcode: 'lea', description: 'Load Effective Address', detail: 'An <- effective address' },
	{ opcode: 'pea', description: 'Push Effective Address', detail: 'Push effective address onto stack' },
	{ opcode: 'exg', description: 'Exchange Registers', detail: 'Rx <-> Ry' },
	{ opcode: 'swap', description: 'Swap Halves', detail: 'Swap upper/lower words of Dn' },
	{ opcode: 'link', description: 'Link', detail: 'Link and allocate stack frame' },
	{ opcode: 'unlk', description: 'Unlink', detail: 'Unlink stack frame' },
	// Arithmetic
	{ opcode: 'add', description: 'Add', detail: 'dest <- dest + src' },
	{ opcode: 'adda', description: 'Add to Address Register', detail: 'An <- An + src' },
	{ opcode: 'addi', description: 'Add Immediate', detail: 'dest <- dest + imm' },
	{ opcode: 'addq', description: 'Add Quick', detail: 'dest <- dest + 1..8' },
	{ opcode: 'addx', description: 'Add with Extend', detail: 'dest <- dest + src + X' },
	{ opcode: 'sub', description: 'Subtract', detail: 'dest <- dest - src' },
	{ opcode: 'suba', description: 'Subtract from Address', detail: 'An <- An - src' },
	{ opcode: 'subi', description: 'Subtract Immediate', detail: 'dest <- dest - imm' },
	{ opcode: 'subq', description: 'Subtract Quick', detail: 'dest <- dest - 1..8' },
	{ opcode: 'subx', description: 'Subtract with Extend', detail: 'dest <- dest - src - X' },
	{ opcode: 'mulu', description: 'Unsigned Multiply', detail: 'Dn <- Dn.w * src.w (unsigned)' },
	{ opcode: 'muls', description: 'Signed Multiply', detail: 'Dn <- Dn.w * src.w (signed)' },
	{ opcode: 'divu', description: 'Unsigned Divide', detail: 'Dn <- Dn / src (unsigned)' },
	{ opcode: 'divs', description: 'Signed Divide', detail: 'Dn <- Dn / src (signed)' },
	{ opcode: 'neg', description: 'Negate', detail: 'dest <- 0 - dest' },
	{ opcode: 'negx', description: 'Negate with Extend', detail: 'dest <- 0 - dest - X' },
	{ opcode: 'clr', description: 'Clear', detail: 'dest <- 0' },
	{ opcode: 'ext', description: 'Sign Extend', detail: 'Extend sign of Dn' },
	// Logic
	{ opcode: 'and', description: 'Logical AND', detail: 'dest <- dest & src' },
	{ opcode: 'andi', description: 'AND Immediate', detail: 'dest <- dest & imm' },
	{ opcode: 'or', description: 'Logical OR', detail: 'dest <- dest | src' },
	{ opcode: 'ori', description: 'OR Immediate', detail: 'dest <- dest | imm' },
	{ opcode: 'eor', description: 'Exclusive OR', detail: 'dest <- dest ^ src' },
	{ opcode: 'eori', description: 'EOR Immediate', detail: 'dest <- dest ^ imm' },
	{ opcode: 'not', description: 'Logical NOT', detail: 'dest <- ~dest' },
	{ opcode: 'tst', description: 'Test', detail: 'Set flags based on operand' },
	{ opcode: 'cmp', description: 'Compare', detail: 'Compare dest - src' },
	{ opcode: 'cmpa', description: 'Compare Address', detail: 'Compare An - src' },
	{ opcode: 'cmpi', description: 'Compare Immediate', detail: 'Compare dest - imm' },
	{ opcode: 'cmpm', description: 'Compare Memory', detail: 'Compare (Ay)+ - (Ax)+' },
	// Shift/Rotate
	{ opcode: 'asl', description: 'Arithmetic Shift Left', detail: 'Shift left, LSB=0' },
	{ opcode: 'asr', description: 'Arithmetic Shift Right', detail: 'Shift right, MSB preserved' },
	{ opcode: 'lsl', description: 'Logical Shift Left', detail: 'Shift left, LSB=0' },
	{ opcode: 'lsr', description: 'Logical Shift Right', detail: 'Shift right, MSB=0' },
	{ opcode: 'rol', description: 'Rotate Left', detail: 'Rotate left' },
	{ opcode: 'ror', description: 'Rotate Right', detail: 'Rotate right' },
	{ opcode: 'roxl', description: 'Rotate Left with Extend', detail: 'Rotate left through X' },
	{ opcode: 'roxr', description: 'Rotate Right with Extend', detail: 'Rotate right through X' },
	// Bit Operations
	{ opcode: 'btst', description: 'Bit Test', detail: 'Test bit (set Z flag)' },
	{ opcode: 'bset', description: 'Bit Set', detail: 'Test and set bit to 1' },
	{ opcode: 'bclr', description: 'Bit Clear', detail: 'Test and clear bit to 0' },
	{ opcode: 'bchg', description: 'Bit Change', detail: 'Test and toggle bit' },
	// Branch
	{ opcode: 'bra', description: 'Branch Always', detail: 'Unconditional branch' },
	{ opcode: 'bsr', description: 'Branch to Subroutine', detail: 'Push PC, branch to subroutine' },
	{ opcode: 'beq', description: 'Branch if Equal', detail: 'Branch if Z = 1' },
	{ opcode: 'bne', description: 'Branch if Not Equal', detail: 'Branch if Z = 0' },
	{ opcode: 'bgt', description: 'Branch if Greater Than', detail: 'Signed: branch if > 0' },
	{ opcode: 'bge', description: 'Branch if Greater/Equal', detail: 'Signed: branch if >= 0' },
	{ opcode: 'blt', description: 'Branch if Less Than', detail: 'Signed: branch if < 0' },
	{ opcode: 'ble', description: 'Branch if Less/Equal', detail: 'Signed: branch if <= 0' },
	{ opcode: 'bhi', description: 'Branch if Higher', detail: 'Unsigned: branch if > (C=0 & Z=0)' },
	{ opcode: 'bls', description: 'Branch if Lower/Same', detail: 'Unsigned: branch if <= (C=1 | Z=1)' },
	{ opcode: 'bcc', description: 'Branch if Carry Clear', detail: 'Branch if C = 0' },
	{ opcode: 'bcs', description: 'Branch if Carry Set', detail: 'Branch if C = 1' },
	{ opcode: 'bpl', description: 'Branch if Plus', detail: 'Branch if N = 0' },
	{ opcode: 'bmi', description: 'Branch if Minus', detail: 'Branch if N = 1' },
	{ opcode: 'bvc', description: 'Branch if Overflow Clear', detail: 'Branch if V = 0' },
	{ opcode: 'bvs', description: 'Branch if Overflow Set', detail: 'Branch if V = 1' },
	// Set Condition
	{ opcode: 'scc', description: 'Set on Condition', detail: 'Set byte if condition true' },
	{ opcode: 'dbcc', description: 'Decrement and Branch', detail: 'Dn--; branch if condition false & Dn!=-1' },
	// Jump
	{ opcode: 'jmp', description: 'Jump', detail: 'PC <- effective address' },
	{ opcode: 'jsr', description: 'Jump to Subroutine', detail: 'Push PC, jump to subroutine' },
	{ opcode: 'rts', description: 'Return from Subroutine', detail: 'PC <- pull' },
	{ opcode: 'rte', description: 'Return from Exception', detail: 'Restore SR and PC from stack' },
	{ opcode: 'rtr', description: 'Return and Restore', detail: 'Restore CCR and PC' },
	{ opcode: 'trap', description: 'Trap', detail: 'Software exception' },
	{ opcode: 'nop', description: 'No Operation', detail: 'Do nothing' },
	{ opcode: 'stop', description: 'Stop', detail: 'Load SR and stop' },
	{ opcode: 'reset', description: 'Reset', detail: 'Assert RESET line' },
	// BCD
	{ opcode: 'abcd', description: 'Add BCD', detail: 'dest <- dest + src + X (BCD)' },
	{ opcode: 'sbcd', description: 'Subtract BCD', detail: 'dest <- dest - src - X (BCD)' },
	{ opcode: 'nbcd', description: 'Negate BCD', detail: 'dest <- 0 - dest - X (BCD)' },
	{ opcode: 'tas', description: 'Test and Set', detail: 'Test operand, set bit 7' },
];

// ============================================================================
// ARM7TDMI (Game Boy Advance) — ARM mode core instructions
// ============================================================================
const OPCODES_ARM7TDMI: OpcodeEntry[] = [
	// Data Processing
	{ opcode: 'mov', description: 'Move', detail: 'Rd <- operand2' },
	{ opcode: 'mvn', description: 'Move NOT', detail: 'Rd <- ~operand2' },
	{ opcode: 'add', description: 'Add', detail: 'Rd <- Rn + operand2' },
	{ opcode: 'adc', description: 'Add with Carry', detail: 'Rd <- Rn + operand2 + C' },
	{ opcode: 'sub', description: 'Subtract', detail: 'Rd <- Rn - operand2' },
	{ opcode: 'sbc', description: 'Subtract with Carry', detail: 'Rd <- Rn - operand2 - !C' },
	{ opcode: 'rsb', description: 'Reverse Subtract', detail: 'Rd <- operand2 - Rn' },
	{ opcode: 'rsc', description: 'Reverse Subtract with Carry', detail: 'Rd <- operand2 - Rn - !C' },
	{ opcode: 'mul', description: 'Multiply', detail: 'Rd <- Rm * Rs' },
	{ opcode: 'mla', description: 'Multiply-Accumulate', detail: 'Rd <- Rm * Rs + Rn' },
	{ opcode: 'umull', description: 'Unsigned Multiply Long', detail: 'RdHi:RdLo <- Rm * Rs' },
	{ opcode: 'umlal', description: 'Unsigned Multiply-Accumulate Long', detail: 'RdHi:RdLo += Rm * Rs' },
	{ opcode: 'smull', description: 'Signed Multiply Long', detail: 'RdHi:RdLo <- Rm * Rs (signed)' },
	{ opcode: 'smlal', description: 'Signed Multiply-Accumulate Long', detail: 'RdHi:RdLo += Rm * Rs (signed)' },
	// Logic
	{ opcode: 'and', description: 'Logical AND', detail: 'Rd <- Rn & operand2' },
	{ opcode: 'orr', description: 'Logical OR', detail: 'Rd <- Rn | operand2' },
	{ opcode: 'eor', description: 'Exclusive OR', detail: 'Rd <- Rn ^ operand2' },
	{ opcode: 'bic', description: 'Bit Clear', detail: 'Rd <- Rn & ~operand2' },
	// Compare/Test
	{ opcode: 'cmp', description: 'Compare', detail: 'Set flags for Rn - operand2' },
	{ opcode: 'cmn', description: 'Compare Negative', detail: 'Set flags for Rn + operand2' },
	{ opcode: 'tst', description: 'Test', detail: 'Set flags for Rn & operand2' },
	{ opcode: 'teq', description: 'Test Equivalence', detail: 'Set flags for Rn ^ operand2' },
	// Shift
	{ opcode: 'lsl', description: 'Logical Shift Left', detail: 'Rd <- Rm << n' },
	{ opcode: 'lsr', description: 'Logical Shift Right', detail: 'Rd <- Rm >> n (unsigned)' },
	{ opcode: 'asr', description: 'Arithmetic Shift Right', detail: 'Rd <- Rm >> n (signed)' },
	{ opcode: 'ror', description: 'Rotate Right', detail: 'Rd <- Rm rotated right by n' },
	// Load/Store
	{ opcode: 'ldr', description: 'Load Register', detail: 'Rd <- [address]' },
	{ opcode: 'ldrb', description: 'Load Register Byte', detail: 'Rd <- [address] (byte)' },
	{ opcode: 'ldrh', description: 'Load Register Halfword', detail: 'Rd <- [address] (16-bit)' },
	{ opcode: 'ldrsb', description: 'Load Signed Byte', detail: 'Rd <- sign-extend([address])' },
	{ opcode: 'ldrsh', description: 'Load Signed Halfword', detail: 'Rd <- sign-extend([address] 16-bit)' },
	{ opcode: 'str', description: 'Store Register', detail: '[address] <- Rd' },
	{ opcode: 'strb', description: 'Store Register Byte', detail: '[address] <- Rd (byte)' },
	{ opcode: 'strh', description: 'Store Register Halfword', detail: '[address] <- Rd (16-bit)' },
	{ opcode: 'ldm', description: 'Load Multiple', detail: 'Load multiple registers from memory' },
	{ opcode: 'stm', description: 'Store Multiple', detail: 'Store multiple registers to memory' },
	// Branch
	{ opcode: 'b', description: 'Branch', detail: 'PC <- address' },
	{ opcode: 'bl', description: 'Branch with Link', detail: 'LR <- PC+4, PC <- address' },
	{ opcode: 'bx', description: 'Branch and Exchange', detail: 'Branch and switch ARM/Thumb' },
	// Status Register
	{ opcode: 'mrs', description: 'Move PSR to Register', detail: 'Rd <- CPSR/SPSR' },
	{ opcode: 'msr', description: 'Move Register to PSR', detail: 'CPSR/SPSR <- Rm' },
	// Swap
	{ opcode: 'swp', description: 'Swap', detail: 'Rd <- [Rn], [Rn] <- Rm' },
	{ opcode: 'swpb', description: 'Swap Byte', detail: 'Byte swap at [Rn]' },
	// Misc
	{ opcode: 'swi', description: 'Software Interrupt', detail: 'Supervisor call (BIOS function)' },
	{ opcode: 'nop', description: 'No Operation', detail: 'Do nothing' },
	{ opcode: 'push', description: 'Push (Thumb)', detail: 'Push registers to stack' },
	{ opcode: 'pop', description: 'Pop (Thumb)', detail: 'Pop registers from stack' },
	{ opcode: 'neg', description: 'Negate (Thumb)', detail: 'Rd <- 0 - Rm' },
];

// ============================================================================
// HuC6280 (TurboGrafx-16 / PC Engine) — extends 65SC02
// ============================================================================
const OPCODES_HUC6280: OpcodeEntry[] = [
	...OPCODES_65SC02,
	{ opcode: 'bsr', description: 'Branch to Subroutine', detail: 'Relative subroutine call' },
	{ opcode: 'cla', description: 'Clear Accumulator', detail: 'A <- 0' },
	{ opcode: 'clx', description: 'Clear X', detail: 'X <- 0' },
	{ opcode: 'cly', description: 'Clear Y', detail: 'Y <- 0' },
	{ opcode: 'csh', description: 'Change Speed High', detail: 'Switch to 7.16 MHz' },
	{ opcode: 'csl', description: 'Change Speed Low', detail: 'Switch to 1.79 MHz' },
	{ opcode: 'say', description: 'Swap A and Y', detail: 'A <-> Y' },
	{ opcode: 'sxy', description: 'Swap X and Y', detail: 'X <-> Y' },
	{ opcode: 'sax', description: 'Swap A and X', detail: 'A <-> X' },
	{ opcode: 'set', description: 'Set T Flag', detail: 'T <- 1' },
	{ opcode: 'st0', description: 'Store to VDC Register 0', detail: 'VDC addr register <- imm' },
	{ opcode: 'st1', description: 'Store to VDC Register 1', detail: 'VDC data low <- imm' },
	{ opcode: 'st2', description: 'Store to VDC Register 2', detail: 'VDC data high <- imm' },
	{ opcode: 'tam', description: 'Transfer A to MPR', detail: 'MPR(mask) <- A' },
	{ opcode: 'tma', description: 'Transfer MPR to A', detail: 'A <- MPR(mask)' },
	{ opcode: 'tii', description: 'Transfer Increment-Increment', detail: 'Block move (src++, dst++)' },
	{ opcode: 'tdd', description: 'Transfer Decrement-Decrement', detail: 'Block move (src--, dst--)' },
	{ opcode: 'tin', description: 'Transfer Increment-None', detail: 'Block move (src++, dst fixed)' },
	{ opcode: 'tia', description: 'Transfer Increment-Alternate', detail: 'Block move (src++, dst alt)' },
	{ opcode: 'tai', description: 'Transfer Alternate-Increment', detail: 'Block move (src alt, dst++)' },
	// BB instructions (test and branch)
	{ opcode: 'bbr0', description: 'Branch on Bit 0 Reset', detail: 'Branch if ZP bit 0 = 0' },
	{ opcode: 'bbr1', description: 'Branch on Bit 1 Reset', detail: 'Branch if ZP bit 1 = 0' },
	{ opcode: 'bbr2', description: 'Branch on Bit 2 Reset', detail: 'Branch if ZP bit 2 = 0' },
	{ opcode: 'bbr3', description: 'Branch on Bit 3 Reset', detail: 'Branch if ZP bit 3 = 0' },
	{ opcode: 'bbr4', description: 'Branch on Bit 4 Reset', detail: 'Branch if ZP bit 4 = 0' },
	{ opcode: 'bbr5', description: 'Branch on Bit 5 Reset', detail: 'Branch if ZP bit 5 = 0' },
	{ opcode: 'bbr6', description: 'Branch on Bit 6 Reset', detail: 'Branch if ZP bit 6 = 0' },
	{ opcode: 'bbr7', description: 'Branch on Bit 7 Reset', detail: 'Branch if ZP bit 7 = 0' },
	{ opcode: 'bbs0', description: 'Branch on Bit 0 Set', detail: 'Branch if ZP bit 0 = 1' },
	{ opcode: 'bbs1', description: 'Branch on Bit 1 Set', detail: 'Branch if ZP bit 1 = 1' },
	{ opcode: 'bbs2', description: 'Branch on Bit 2 Set', detail: 'Branch if ZP bit 2 = 1' },
	{ opcode: 'bbs3', description: 'Branch on Bit 3 Set', detail: 'Branch if ZP bit 3 = 1' },
	{ opcode: 'bbs4', description: 'Branch on Bit 4 Set', detail: 'Branch if ZP bit 4 = 1' },
	{ opcode: 'bbs5', description: 'Branch on Bit 5 Set', detail: 'Branch if ZP bit 5 = 1' },
	{ opcode: 'bbs6', description: 'Branch on Bit 6 Set', detail: 'Branch if ZP bit 6 = 1' },
	{ opcode: 'bbs7', description: 'Branch on Bit 7 Set', detail: 'Branch if ZP bit 7 = 1' },
];

// ============================================================================
// V30MZ (WonderSwan) — x86/NEC V-series subset
// ============================================================================
const OPCODES_V30MZ: OpcodeEntry[] = [
	// Data Movement
	{ opcode: 'mov', description: 'Move', detail: 'dest <- src' },
	{ opcode: 'push', description: 'Push', detail: 'Push onto stack' },
	{ opcode: 'pop', description: 'Pop', detail: 'Pop from stack' },
	{ opcode: 'pusha', description: 'Push All', detail: 'Push all general registers' },
	{ opcode: 'popa', description: 'Pop All', detail: 'Pop all general registers' },
	{ opcode: 'pushf', description: 'Push Flags', detail: 'Push flags register' },
	{ opcode: 'popf', description: 'Pop Flags', detail: 'Pop flags register' },
	{ opcode: 'xchg', description: 'Exchange', detail: 'Swap two operands' },
	{ opcode: 'xlat', description: 'Translate', detail: 'AL <- [BX + AL]' },
	{ opcode: 'lea', description: 'Load Effective Address', detail: 'reg <- effective address' },
	// Arithmetic
	{ opcode: 'add', description: 'Add', detail: 'dest <- dest + src' },
	{ opcode: 'adc', description: 'Add with Carry', detail: 'dest <- dest + src + CF' },
	{ opcode: 'sub', description: 'Subtract', detail: 'dest <- dest - src' },
	{ opcode: 'sbb', description: 'Subtract with Borrow', detail: 'dest <- dest - src - CF' },
	{ opcode: 'inc', description: 'Increment', detail: 'dest <- dest + 1' },
	{ opcode: 'dec', description: 'Decrement', detail: 'dest <- dest - 1' },
	{ opcode: 'mul', description: 'Unsigned Multiply', detail: 'AX <- AL * src' },
	{ opcode: 'imul', description: 'Signed Multiply', detail: 'AX <- AL * src (signed)' },
	{ opcode: 'div', description: 'Unsigned Divide', detail: 'AL <- AX / src' },
	{ opcode: 'idiv', description: 'Signed Divide', detail: 'AL <- AX / src (signed)' },
	{ opcode: 'neg', description: 'Negate', detail: 'dest <- 0 - dest' },
	{ opcode: 'cmp', description: 'Compare', detail: 'Set flags for dest - src' },
	// Logic
	{ opcode: 'and', description: 'Logical AND', detail: 'dest <- dest & src' },
	{ opcode: 'or', description: 'Logical OR', detail: 'dest <- dest | src' },
	{ opcode: 'xor', description: 'Logical XOR', detail: 'dest <- dest ^ src' },
	{ opcode: 'not', description: 'Logical NOT', detail: 'dest <- ~dest' },
	{ opcode: 'test', description: 'Test', detail: 'Set flags for dest & src' },
	// Shift/Rotate
	{ opcode: 'shl', description: 'Shift Left', detail: 'Shift left, LSB=0' },
	{ opcode: 'shr', description: 'Shift Right', detail: 'Shift right, MSB=0' },
	{ opcode: 'sar', description: 'Shift Arithmetic Right', detail: 'Shift right, MSB preserved' },
	{ opcode: 'rol', description: 'Rotate Left', detail: 'Rotate left' },
	{ opcode: 'ror', description: 'Rotate Right', detail: 'Rotate right' },
	{ opcode: 'rcl', description: 'Rotate Left through Carry', detail: 'Rotate left through CF' },
	{ opcode: 'rcr', description: 'Rotate Right through Carry', detail: 'Rotate right through CF' },
	// String Operations
	{ opcode: 'movsb', description: 'Move String Byte', detail: '[ES:DI] <- [DS:SI]; SI+-1; DI+-1' },
	{ opcode: 'movsw', description: 'Move String Word', detail: '[ES:DI] <- [DS:SI]; SI+-2; DI+-2' },
	{ opcode: 'stosb', description: 'Store String Byte', detail: '[ES:DI] <- AL; DI+-1' },
	{ opcode: 'stosw', description: 'Store String Word', detail: '[ES:DI] <- AX; DI+-2' },
	{ opcode: 'lodsb', description: 'Load String Byte', detail: 'AL <- [DS:SI]; SI+-1' },
	{ opcode: 'lodsw', description: 'Load String Word', detail: 'AX <- [DS:SI]; SI+-2' },
	{ opcode: 'rep', description: 'Repeat', detail: 'Repeat string op CX times' },
	// Branch/Jump
	{ opcode: 'jmp', description: 'Jump', detail: 'Unconditional jump' },
	{ opcode: 'call', description: 'Call', detail: 'Call subroutine' },
	{ opcode: 'ret', description: 'Return', detail: 'Return from subroutine' },
	{ opcode: 'iret', description: 'Interrupt Return', detail: 'Return from interrupt' },
	{ opcode: 'int', description: 'Interrupt', detail: 'Software interrupt' },
	// Conditional Jumps
	{ opcode: 'je', description: 'Jump if Equal', detail: 'Jump if ZF=1' },
	{ opcode: 'jne', description: 'Jump if Not Equal', detail: 'Jump if ZF=0' },
	{ opcode: 'jb', description: 'Jump if Below', detail: 'Jump if CF=1' },
	{ opcode: 'jnb', description: 'Jump if Not Below', detail: 'Jump if CF=0' },
	{ opcode: 'ja', description: 'Jump if Above', detail: 'Jump if CF=0 and ZF=0' },
	{ opcode: 'jl', description: 'Jump if Less', detail: 'Jump if SF!=OF' },
	{ opcode: 'jg', description: 'Jump if Greater', detail: 'Jump if ZF=0 and SF=OF' },
	{ opcode: 'js', description: 'Jump if Sign', detail: 'Jump if SF=1' },
	{ opcode: 'jns', description: 'Jump if Not Sign', detail: 'Jump if SF=0' },
	{ opcode: 'jcxz', description: 'Jump if CX Zero', detail: 'Jump if CX=0' },
	{ opcode: 'loop', description: 'Loop', detail: 'CX--; jump if CX!=0' },
	// Flags
	{ opcode: 'clc', description: 'Clear Carry', detail: 'CF <- 0' },
	{ opcode: 'stc', description: 'Set Carry', detail: 'CF <- 1' },
	{ opcode: 'cld', description: 'Clear Direction', detail: 'DF <- 0' },
	{ opcode: 'std', description: 'Set Direction', detail: 'DF <- 1' },
	{ opcode: 'cli', description: 'Clear Interrupt', detail: 'IF <- 0' },
	{ opcode: 'sti', description: 'Set Interrupt', detail: 'IF <- 1' },
	// I/O
	{ opcode: 'in', description: 'Input', detail: 'Read from I/O port' },
	{ opcode: 'out', description: 'Output', detail: 'Write to I/O port' },
	// Misc
	{ opcode: 'nop', description: 'No Operation', detail: 'Do nothing' },
	{ opcode: 'hlt', description: 'Halt', detail: 'Halt until interrupt' },
];

// ============================================================================
// SPC700 (SNES Audio)
// ============================================================================
const OPCODES_SPC700: OpcodeEntry[] = [
	// Data Movement
	{ opcode: 'mov', description: 'Move', detail: 'dest <- src' },
	{ opcode: 'movw', description: 'Move Word', detail: 'YA <- dp or dp <- YA' },
	{ opcode: 'push', description: 'Push', detail: 'Push register to stack' },
	{ opcode: 'pop', description: 'Pop', detail: 'Pop register from stack' },
	// Arithmetic
	{ opcode: 'adc', description: 'Add with Carry', detail: 'A <- A + M + C' },
	{ opcode: 'sbc', description: 'Subtract with Carry', detail: 'A <- A - M - !C' },
	{ opcode: 'addw', description: 'Add Word', detail: 'YA <- YA + dp.w' },
	{ opcode: 'subw', description: 'Subtract Word', detail: 'YA <- YA - dp.w' },
	{ opcode: 'inc', description: 'Increment', detail: 'operand <- operand + 1' },
	{ opcode: 'dec', description: 'Decrement', detail: 'operand <- operand - 1' },
	{ opcode: 'incw', description: 'Increment Word', detail: 'dp.w <- dp.w + 1' },
	{ opcode: 'decw', description: 'Decrement Word', detail: 'dp.w <- dp.w - 1' },
	{ opcode: 'mul', description: 'Multiply', detail: 'YA <- Y * A' },
	{ opcode: 'div', description: 'Divide', detail: 'A <- YA / X, Y <- remainder' },
	// Logic
	{ opcode: 'and', description: 'Logical AND', detail: 'A <- A & M' },
	{ opcode: 'or', description: 'Logical OR', detail: 'A <- A | M' },
	{ opcode: 'eor', description: 'Exclusive OR', detail: 'A <- A ^ M' },
	{ opcode: 'cmp', description: 'Compare', detail: 'Compare A with operand' },
	{ opcode: 'cmpw', description: 'Compare Word', detail: 'Compare YA with dp.w' },
	// Shift/Rotate
	{ opcode: 'asl', description: 'Arithmetic Shift Left', detail: 'Shift left, bit 7 -> C' },
	{ opcode: 'lsr', description: 'Logical Shift Right', detail: 'Shift right, bit 0 -> C' },
	{ opcode: 'rol', description: 'Rotate Left', detail: 'Rotate left through carry' },
	{ opcode: 'ror', description: 'Rotate Right', detail: 'Rotate right through carry' },
	{ opcode: 'xcn', description: 'Exchange Nibbles', detail: 'Swap nibbles of A' },
	// Bit Operations
	{ opcode: 'set1', description: 'Set Bit', detail: 'Set bit in direct page' },
	{ opcode: 'clr1', description: 'Clear Bit', detail: 'Clear bit in direct page' },
	{ opcode: 'tset1', description: 'Test and Set', detail: 'Test bits and set' },
	{ opcode: 'tclr1', description: 'Test and Clear', detail: 'Test bits and clear' },
	{ opcode: 'and1', description: 'AND1 Carry', detail: 'C <- C & bit' },
	{ opcode: 'or1', description: 'OR1 Carry', detail: 'C <- C | bit' },
	{ opcode: 'eor1', description: 'EOR1 Carry', detail: 'C <- C ^ bit' },
	{ opcode: 'not1', description: 'NOT1 Bit', detail: 'Complement memory bit' },
	{ opcode: 'mov1', description: 'Move Bit', detail: 'Move bit to/from carry' },
	// Branch
	{ opcode: 'bra', description: 'Branch Always', detail: 'Unconditional branch' },
	{ opcode: 'beq', description: 'Branch if Equal', detail: 'Branch if Z = 1' },
	{ opcode: 'bne', description: 'Branch if Not Equal', detail: 'Branch if Z = 0' },
	{ opcode: 'bcs', description: 'Branch if Carry Set', detail: 'Branch if C = 1' },
	{ opcode: 'bcc', description: 'Branch if Carry Clear', detail: 'Branch if C = 0' },
	{ opcode: 'bmi', description: 'Branch if Minus', detail: 'Branch if N = 1' },
	{ opcode: 'bpl', description: 'Branch if Plus', detail: 'Branch if N = 0' },
	{ opcode: 'bbs', description: 'Branch if Bit Set', detail: 'Branch if direct page bit is set' },
	{ opcode: 'bbc', description: 'Branch if Bit Clear', detail: 'Branch if direct page bit is clear' },
	{ opcode: 'cbne', description: 'Compare and Branch if Not Equal', detail: 'Compare dp, branch if != A' },
	{ opcode: 'dbnz', description: 'Decrement and Branch if Not Zero', detail: 'Decrement, branch if != 0' },
	// Call/Return
	{ opcode: 'call', description: 'Call', detail: 'Call subroutine' },
	{ opcode: 'pcall', description: 'Page Call', detail: 'Call $ff00+n' },
	{ opcode: 'tcall', description: 'Table Call', detail: 'Call through vector table' },
	{ opcode: 'ret', description: 'Return', detail: 'Return from subroutine' },
	{ opcode: 'reti', description: 'Return from Interrupt', detail: 'Return from interrupt' },
	{ opcode: 'brk', description: 'Break', detail: 'Software break' },
	// Flags
	{ opcode: 'clrc', description: 'Clear Carry', detail: 'C <- 0' },
	{ opcode: 'setc', description: 'Set Carry', detail: 'C <- 1' },
	{ opcode: 'notc', description: 'Complement Carry', detail: 'C <- ~C' },
	{ opcode: 'clrv', description: 'Clear V and H', detail: 'V <- 0, H <- 0' },
	{ opcode: 'clrp', description: 'Clear Direct Page', detail: 'P <- 0 (select page 0)' },
	{ opcode: 'setp', description: 'Set Direct Page', detail: 'P <- 1 (select page 1)' },
	{ opcode: 'ei', description: 'Enable Interrupts', detail: 'I <- 1' },
	{ opcode: 'di', description: 'Disable Interrupts', detail: 'I <- 0' },
	// Misc
	{ opcode: 'nop', description: 'No Operation', detail: 'Do nothing' },
	{ opcode: 'sleep', description: 'Sleep', detail: 'Wait for interrupt' },
	{ opcode: 'stop', description: 'Stop', detail: 'Stop processor' },
];

// ============================================================================
// Directive definitions (shared across all platforms)
// ============================================================================
const DIRECTIVES: OpcodeEntry[] = [
	// Origin/Address
	{ opcode: '.org', description: 'Set Origin', detail: 'Set program counter address' },
	{ opcode: '.base', description: 'Set Base Address', detail: 'Set base address for labels' },
	// Data
	{ opcode: '.db', description: 'Define Byte', detail: 'Define 8-bit data' },
	{ opcode: '.byte', description: 'Define Byte', detail: 'Alias for .db' },
	{ opcode: '.dw', description: 'Define Word', detail: 'Define 16-bit data' },
	{ opcode: '.word', description: 'Define Word', detail: 'Alias for .dw' },
	{ opcode: '.dl', description: 'Define Long', detail: 'Define 24-bit data (65816)' },
	{ opcode: '.dd', description: 'Define Dword', detail: 'Define 32-bit data' },
	{ opcode: '.ds', description: 'Define Space', detail: 'Reserve bytes' },
	{ opcode: '.fill', description: 'Fill Space', detail: 'Fill with a byte value' },
	{ opcode: '.res', description: 'Reserve Space', detail: 'Reserve uninitialized bytes' },
	{ opcode: '.ascii', description: 'ASCII String', detail: 'Define ASCII string' },
	{ opcode: '.asciiz', description: 'ASCII Zero-Terminated', detail: 'Define null-terminated ASCII string' },
	// Include
	{ opcode: '.include', description: 'Include Source', detail: 'Include .pasm source file' },
	{ opcode: '.incbin', description: 'Include Binary', detail: 'Include binary file' },
	// Macro/Conditional
	{ opcode: '.macro', description: 'Define Macro', detail: 'Begin macro definition' },
	{ opcode: '.endmacro', description: 'End Macro', detail: 'End macro definition' },
	{ opcode: '.endm', description: 'End Macro', detail: 'Alias for .endmacro' },
	{ opcode: '.if', description: 'If Conditional', detail: 'Conditional assembly' },
	{ opcode: '.ifdef', description: 'If Defined', detail: 'Assemble if symbol defined' },
	{ opcode: '.ifndef', description: 'If Not Defined', detail: 'Assemble if symbol not defined' },
	{ opcode: '.ifeq', description: 'If Equal', detail: 'Assemble if expression = 0' },
	{ opcode: '.ifne', description: 'If Not Equal', detail: 'Assemble if expression != 0' },
	{ opcode: '.ifgt', description: 'If Greater Than', detail: 'Assemble if expression > 0' },
	{ opcode: '.iflt', description: 'If Less Than', detail: 'Assemble if expression < 0' },
	{ opcode: '.ifge', description: 'If Greater/Equal', detail: 'Assemble if expression >= 0' },
	{ opcode: '.ifle', description: 'If Less/Equal', detail: 'Assemble if expression <= 0' },
	{ opcode: '.else', description: 'Else Clause', detail: 'Else branch of conditional' },
	{ opcode: '.elseif', description: 'Else If', detail: 'Else-if branch of conditional' },
	{ opcode: '.endif', description: 'End If', detail: 'End conditional block' },
	// Repeat
	{ opcode: '.repeat', description: 'Repeat Block', detail: 'Repeat assembly block N times' },
	{ opcode: '.rept', description: 'Repeat Block', detail: 'Alias for .repeat' },
	{ opcode: '.endrep', description: 'End Repeat', detail: 'End repeat block' },
	{ opcode: '.endr', description: 'End Repeat', detail: 'Alias for .endrep' },
	// Scope
	{ opcode: '.scope', description: 'Begin Scope', detail: 'Begin named scope' },
	{ opcode: '.endscope', description: 'End Scope', detail: 'End scope' },
	// Symbol
	{ opcode: '.define', description: 'Define Symbol', detail: 'Define preprocessor symbol' },
	{ opcode: '.undef', description: 'Undefine Symbol', detail: 'Undefine symbol' },
	{ opcode: '.equ', description: 'Define Constant', detail: 'Define a constant value' },
	// Enum/Struct
	{ opcode: '.enum', description: 'Begin Enum', detail: 'Begin enumeration block' },
	{ opcode: '.endenum', description: 'End Enum', detail: 'End enumeration block' },
	{ opcode: '.ende', description: 'End Enum', detail: 'Alias for .endenum' },
	// Segment/Bank
	{ opcode: '.segment', description: 'Define Segment', detail: 'Define named memory segment' },
	{ opcode: '.bank', description: 'Set Bank', detail: 'Switch to ROM bank' },
	{ opcode: '.banksize', description: 'Set Bank Size', detail: 'Set size of each bank' },
	// Alignment/Padding
	{ opcode: '.align', description: 'Align', detail: 'Align to boundary' },
	{ opcode: '.pad', description: 'Pad', detail: 'Pad to address' },
	// Target
	{ opcode: '.target', description: 'Set Target', detail: 'Set target architecture' },
	// Diagnostics
	{ opcode: '.assert', description: 'Assert', detail: 'Compile-time assertion' },
	{ opcode: '.error', description: 'Error', detail: 'Emit error message' },
	{ opcode: '.warning', description: 'Warning', detail: 'Emit warning message' },
	// Platform Shorthands
	{ opcode: '.ines', description: 'iNES Header', detail: 'Enable iNES ROM header (NES)' },
	{ opcode: '.ines2', description: 'iNES 2.0', detail: 'Enable iNES 2.0 format' },
	{ opcode: '.snes', description: 'SNES Header', detail: 'Enable SNES ROM header' },
	{ opcode: '.gb', description: 'Game Boy Header', detail: 'Enable Game Boy ROM header' },
	// NES directives
	{ opcode: '.ines_prg', description: 'iNES PRG', detail: 'Set PRG ROM size' },
	{ opcode: '.ines_chr', description: 'iNES CHR', detail: 'Set CHR ROM size' },
	{ opcode: '.ines_mapper', description: 'iNES Mapper', detail: 'Set mapper number' },
	{ opcode: '.ines_mirroring', description: 'iNES Mirroring', detail: 'Set mirroring (0=H, 1=V)' },
	{ opcode: '.ines_battery', description: 'iNES Battery', detail: 'Set battery-backed flag' },
	{ opcode: '.mapper', description: 'NES Mapper', detail: 'Set NES mapper number' },
	// SNES directives
	{ opcode: '.snes_title', description: 'SNES Title', detail: 'Set ROM title (21 chars max)' },
	{ opcode: '.snes_rom_size', description: 'SNES ROM Size', detail: 'Set ROM size in KB' },
	{ opcode: '.snes_region', description: 'SNES Region', detail: 'Set region code' },
	{ opcode: '.snes_fastrom', description: 'SNES FastROM', detail: 'Enable FastROM mode' },
	{ opcode: '.lorom', description: 'LoROM', detail: 'Set LoROM memory mapping' },
	{ opcode: '.hirom', description: 'HiROM', detail: 'Set HiROM memory mapping' },
	{ opcode: '.exhirom', description: 'ExHiROM', detail: 'Set ExHiROM memory mapping' },
	// 65816 Mode
	{ opcode: '.a8', description: 'A 8-bit Mode', detail: 'Set accumulator to 8-bit (65816)' },
	{ opcode: '.a16', description: 'A 16-bit Mode', detail: 'Set accumulator to 16-bit (65816)' },
	{ opcode: '.i8', description: 'Index 8-bit Mode', detail: 'Set index registers to 8-bit (65816)' },
	{ opcode: '.i16', description: 'Index 16-bit Mode', detail: 'Set index registers to 16-bit (65816)' },
	{ opcode: '.smart', description: 'Smart Mode', detail: 'Auto-track M/X from REP/SEP' },
	// GB directives
	{ opcode: '.gb_title', description: 'GB Title', detail: 'Set Game Boy title' },
	{ opcode: '.gb_cartridge_type', description: 'GB Cartridge Type', detail: 'Set cartridge type (MBC)' },
	{ opcode: '.gb_cgb', description: 'GB CGB Mode', detail: 'Set CGB compatibility mode' },
	{ opcode: '.gb_sgb', description: 'GB SGB Mode', detail: 'Set SGB support flag' },
	// GBA directives
	{ opcode: '.gba_title', description: 'GBA Title', detail: 'Set GBA ROM title' },
	{ opcode: '.arm', description: 'ARM Mode', detail: 'Switch to ARM instruction mode' },
	{ opcode: '.thumb', description: 'Thumb Mode', detail: 'Switch to Thumb instruction mode' },
	// Lynx directives
	{ opcode: '.lynx_name', description: 'Lynx Name', detail: 'Set Lynx game name' },
	{ opcode: '.lynx_manufacturer', description: 'Lynx Manufacturer', detail: 'Set manufacturer name' },
	{ opcode: '.lynxboot', description: 'Lynx Boot', detail: 'Generate boot code' },
	{ opcode: '.lynxentry', description: 'Lynx Entry', detail: 'Set entry point address' },
];

// Platform-specific register sets
const REGISTERS: Record<TargetArch, string[]> = {
	nes: ['a', 'x', 'y', 'sp'],
	snes: ['a', 'x', 'y', 'sp', 'dp', 'db', 'pb'],
	gb: ['a', 'b', 'c', 'd', 'e', 'h', 'l', 'af', 'bc', 'de', 'hl', 'sp', 'pc'],
	gba: ['r0', 'r1', 'r2', 'r3', 'r4', 'r5', 'r6', 'r7', 'r8', 'r9', 'r10', 'r11', 'r12', 'r13', 'r14', 'r15', 'sp', 'lr', 'pc', 'cpsr', 'spsr'],
	genesis: ['d0', 'd1', 'd2', 'd3', 'd4', 'd5', 'd6', 'd7', 'a0', 'a1', 'a2', 'a3', 'a4', 'a5', 'a6', 'a7', 'sp', 'sr', 'pc', 'usp', 'ccr'],
	sms: ['a', 'b', 'c', 'd', 'e', 'h', 'l', 'af', 'bc', 'de', 'hl', 'ix', 'iy', 'sp', 'pc', 'i', 'r'],
	tg16: ['a', 'x', 'y', 'sp'],
	a2600: ['a', 'x', 'y', 'sp'],
	lynx: ['a', 'x', 'y', 'sp'],
	ws: ['ax', 'bx', 'cx', 'dx', 'sp', 'bp', 'si', 'di', 'ds', 'es', 'ss', 'cs', 'al', 'ah', 'bl', 'bh', 'cl', 'ch', 'dl', 'dh'],
	spc700: ['a', 'x', 'y', 'sp', 'psw', 'ya'],
	channelf: ['a', 'r0', 'r1', 'r2', 'r3', 'r4', 'r5', 'r6', 'r7', 'r8', 'r9', 'r10', 'r11', 'isar', 'w', 'j'],
};

// Map target to opcode array
function getOpcodes(target: TargetArch): OpcodeEntry[] {
	switch (target) {
		case 'snes': return OPCODES_65816;
		case 'gb': return OPCODES_SM83;
		case 'gba': return OPCODES_ARM7TDMI;
		case 'genesis': return OPCODES_M68000;
		case 'sms': return OPCODES_Z80;
		case 'tg16': return OPCODES_HUC6280;
		case 'a2600': return OPCODES_6502; // 6507 is a pin-limited 6502
		case 'lynx': return OPCODES_65SC02;
		case 'ws': return OPCODES_V30MZ;
		case 'spc700': return OPCODES_SPC700;
		case 'channelf': return OPCODES_6502; // Placeholder
		case 'nes':
		default: return OPCODES_6502;
	}
}

// Target alias map
const TARGET_ALIASES: Record<string, TargetArch> = {
	'nes': 'nes', '6502': 'nes',
	'snes': 'snes', '65816': 'snes',
	'gb': 'gb', 'gameboy': 'gb', 'sm83': 'gb', 'gbc': 'gb',
	'gba': 'gba', 'gameboyadvance': 'gba', 'arm': 'gba', 'arm7tdmi': 'gba', 'arm7': 'gba',
	'genesis': 'genesis', 'megadrive': 'genesis', 'md': 'genesis', '68000': 'genesis', 'm68k': 'genesis', 'm68000': 'genesis',
	'sms': 'sms', 'mastersystem': 'sms', 'z80': 'sms', 'gg': 'sms',
	'tg16': 'tg16', 'turbografx16': 'tg16', 'pcengine': 'tg16', 'pce': 'tg16', 'huc6280': 'tg16',
	'atari2600': 'a2600', '2600': 'a2600', '6507': 'a2600',
	'atarilynx': 'lynx', 'lynx': 'lynx', '65sc02': 'lynx',
	'wonderswan': 'ws', 'ws': 'ws', 'wsc': 'ws', 'v30mz': 'ws',
	'spc700': 'spc700', 'spc': 'spc700',
	'channelf': 'channelf', 'channel-f': 'channelf', 'channel_f': 'channelf', 'f8': 'channelf',
};

export class PoppyCompletionProvider implements vscode.CompletionItemProvider {
	provideCompletionItems(
		document: vscode.TextDocument,
		position: vscode.Position,
		token: vscode.CancellationToken,
		context: vscode.CompletionContext
	): vscode.ProviderResult<vscode.CompletionItem[] | vscode.CompletionList> {
		const linePrefix = document.lineAt(position).text.substring(0, position.character);
		const items: vscode.CompletionItem[] = [];

		// Detect target architecture from file content
		const fileContent = document.getText();
		const target = this.detectTarget(fileContent);

		// Directive completion (starts with .)
		if (linePrefix.match(/\s*\.[\w_]*$/)) {
			for (const dir of DIRECTIVES) {
				const item = new vscode.CompletionItem(dir.opcode, vscode.CompletionItemKind.Keyword);
				item.detail = dir.detail;
				item.documentation = new vscode.MarkdownString(dir.description);
				items.push(item);
			}
			return items;
		}

		// Opcode completion (at start of instruction, after label, or after whitespace)
		if (linePrefix.match(/(?:^\s*|:\s*|\t\s*)[\w]*$/)) {
			const opcodes = getOpcodes(target);

			for (const op of opcodes) {
				const item = new vscode.CompletionItem(op.opcode, vscode.CompletionItemKind.Function);
				item.detail = op.detail;
				item.documentation = new vscode.MarkdownString(`**${op.description}**\n\n${op.detail}`);
				items.push(item);
			}
		}

		// Register completion (context-specific)
		if (linePrefix.match(/,\s*[\w]*$/) || linePrefix.match(/\[\s*[\w]*$/)) {
			const registers = REGISTERS[target] || REGISTERS['nes'];
			for (const reg of registers) {
				const item = new vscode.CompletionItem(reg, vscode.CompletionItemKind.Variable);
				item.detail = 'Register';
				items.push(item);
			}
		}

		return items;
	}

	private detectTarget(content: string): TargetArch {
		// Check .target directive first
		const targetMatch = content.match(/\.target\s+([\w-]+)/i);
		if (targetMatch) {
			const alias = targetMatch[1].toLowerCase();
			if (alias in TARGET_ALIASES) {
				return TARGET_ALIASES[alias];
			}
		}

		// Detect from platform-specific directives (most specific first)
		if (content.match(/\.gba[_\s]|\.arm\b|\.thumb\b/i)) return 'gba';
		if (content.match(/\.genesis[_\s]|\.sega\b/i)) return 'genesis';
		if (content.match(/\.sms[_\s]/i)) return 'sms';
		if (content.match(/\.tg16[_\s]|\.pce[_\s]|\.pce_region\b/i)) return 'tg16';
		if (content.match(/\.a2600[_\s]|\.tia\b/i)) return 'a2600';
		if (content.match(/\.lynx[_\s]|\.lynxboot\b|\.lynxentry\b/i)) return 'lynx';
		if (content.match(/\.ws[_\s]|\.wonderswan\b/i)) return 'ws';
		if (content.match(/\.spc700\b/i)) return 'spc700';
		if (content.match(/\.channelf\b|\.channel-f\b|\.f8\b/i)) return 'channelf';
		if (content.match(/\.gb[_\s]|\.gameboy\b/i)) return 'gb';
		if (content.match(/\.snes[_\s]|\.lorom\b|\.hirom\b|\.exhirom\b/i)) return 'snes';
		if (content.match(/\.ines[_\s]|\.ines\b|\.nes\b/i)) return 'nes';

		return 'nes'; // Default
	}
}
