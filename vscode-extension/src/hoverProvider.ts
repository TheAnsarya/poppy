// ============================================================================
// hoverProvider.ts - Poppy Assembly Hover Information Provider
// Provides hover tooltips for assembly constructs
// ============================================================================

import * as vscode from 'vscode';

/**
 * Instruction documentation for 6502/65816 mnemonics.
 */
interface InstructionInfo {
	mnemonic: string;
	name: string;
	description: string;
	flags?: string;
	cycles?: string;
	bytes?: string;
}

/**
 * Directive documentation.
 */
interface DirectiveInfo {
	name: string;
	description: string;
	syntax: string;
	example?: string;
}

/**
 * Provides hover information for Poppy Assembly files.
 */
export class PoppyHoverProvider implements vscode.HoverProvider {
	private static readonly instructions: Map<string, InstructionInfo> = new Map([
		// Load/Store
		['lda', { mnemonic: 'lda', name: 'Load Accumulator', description: 'Loads a byte into the accumulator', flags: 'N, Z', cycles: '2-5', bytes: '2-3' }],
		['ldx', { mnemonic: 'ldx', name: 'Load X Register', description: 'Loads a byte into the X register', flags: 'N, Z', cycles: '2-5', bytes: '2-3' }],
		['ldy', { mnemonic: 'ldy', name: 'Load Y Register', description: 'Loads a byte into the Y register', flags: 'N, Z', cycles: '2-5', bytes: '2-3' }],
		['sta', { mnemonic: 'sta', name: 'Store Accumulator', description: 'Stores the accumulator into memory', flags: '-', cycles: '3-5', bytes: '2-3' }],
		['stx', { mnemonic: 'stx', name: 'Store X Register', description: 'Stores the X register into memory', flags: '-', cycles: '3-4', bytes: '2-3' }],
		['sty', { mnemonic: 'sty', name: 'Store Y Register', description: 'Stores the Y register into memory', flags: '-', cycles: '3-4', bytes: '2-3' }],
		['stz', { mnemonic: 'stz', name: 'Store Zero', description: 'Stores zero into memory (65C02/65816)', flags: '-', cycles: '3-5', bytes: '2-3' }],

		// Arithmetic
		['adc', { mnemonic: 'adc', name: 'Add with Carry', description: 'Adds operand and carry flag to accumulator', flags: 'N, V, Z, C', cycles: '2-6', bytes: '2-3' }],
		['sbc', { mnemonic: 'sbc', name: 'Subtract with Carry', description: 'Subtracts operand and inverse carry from accumulator', flags: 'N, V, Z, C', cycles: '2-6', bytes: '2-3' }],
		['inc', { mnemonic: 'inc', name: 'Increment', description: 'Increments a memory location or accumulator by 1', flags: 'N, Z', cycles: '2-7', bytes: '1-3' }],
		['dec', { mnemonic: 'dec', name: 'Decrement', description: 'Decrements a memory location or accumulator by 1', flags: 'N, Z', cycles: '2-7', bytes: '1-3' }],
		['inx', { mnemonic: 'inx', name: 'Increment X', description: 'Increments the X register by 1', flags: 'N, Z', cycles: '2', bytes: '1' }],
		['iny', { mnemonic: 'iny', name: 'Increment Y', description: 'Increments the Y register by 1', flags: 'N, Z', cycles: '2', bytes: '1' }],
		['dex', { mnemonic: 'dex', name: 'Decrement X', description: 'Decrements the X register by 1', flags: 'N, Z', cycles: '2', bytes: '1' }],
		['dey', { mnemonic: 'dey', name: 'Decrement Y', description: 'Decrements the Y register by 1', flags: 'N, Z', cycles: '2', bytes: '1' }],

		// Logical
		['and', { mnemonic: 'and', name: 'Logical AND', description: 'Performs bitwise AND with accumulator', flags: 'N, Z', cycles: '2-6', bytes: '2-3' }],
		['ora', { mnemonic: 'ora', name: 'Logical OR', description: 'Performs bitwise OR with accumulator', flags: 'N, Z', cycles: '2-6', bytes: '2-3' }],
		['eor', { mnemonic: 'eor', name: 'Exclusive OR', description: 'Performs bitwise XOR with accumulator', flags: 'N, Z', cycles: '2-6', bytes: '2-3' }],

		// Shifts and Rotates
		['asl', { mnemonic: 'asl', name: 'Arithmetic Shift Left', description: 'Shifts all bits left, bit 7 goes to carry', flags: 'N, Z, C', cycles: '2-7', bytes: '1-3' }],
		['lsr', { mnemonic: 'lsr', name: 'Logical Shift Right', description: 'Shifts all bits right, bit 0 goes to carry', flags: 'N, Z, C', cycles: '2-7', bytes: '1-3' }],
		['rol', { mnemonic: 'rol', name: 'Rotate Left', description: 'Rotates bits left through carry', flags: 'N, Z, C', cycles: '2-7', bytes: '1-3' }],
		['ror', { mnemonic: 'ror', name: 'Rotate Right', description: 'Rotates bits right through carry', flags: 'N, Z, C', cycles: '2-7', bytes: '1-3' }],

		// Branches
		['bcc', { mnemonic: 'bcc', name: 'Branch if Carry Clear', description: 'Branches if C = 0', flags: '-', cycles: '2-4', bytes: '2' }],
		['bcs', { mnemonic: 'bcs', name: 'Branch if Carry Set', description: 'Branches if C = 1', flags: '-', cycles: '2-4', bytes: '2' }],
		['beq', { mnemonic: 'beq', name: 'Branch if Equal', description: 'Branches if Z = 1', flags: '-', cycles: '2-4', bytes: '2' }],
		['bne', { mnemonic: 'bne', name: 'Branch if Not Equal', description: 'Branches if Z = 0', flags: '-', cycles: '2-4', bytes: '2' }],
		['bmi', { mnemonic: 'bmi', name: 'Branch if Minus', description: 'Branches if N = 1', flags: '-', cycles: '2-4', bytes: '2' }],
		['bpl', { mnemonic: 'bpl', name: 'Branch if Plus', description: 'Branches if N = 0', flags: '-', cycles: '2-4', bytes: '2' }],
		['bvc', { mnemonic: 'bvc', name: 'Branch if Overflow Clear', description: 'Branches if V = 0', flags: '-', cycles: '2-4', bytes: '2' }],
		['bvs', { mnemonic: 'bvs', name: 'Branch if Overflow Set', description: 'Branches if V = 1', flags: '-', cycles: '2-4', bytes: '2' }],
		['bra', { mnemonic: 'bra', name: 'Branch Always', description: 'Unconditional relative branch (65C02/65816)', flags: '-', cycles: '3-4', bytes: '2' }],
		['brl', { mnemonic: 'brl', name: 'Branch Long', description: 'Long unconditional branch (65816)', flags: '-', cycles: '4', bytes: '3' }],

		// Jumps and Calls
		['jmp', { mnemonic: 'jmp', name: 'Jump', description: 'Jumps to a new location', flags: '-', cycles: '3-6', bytes: '3' }],
		['jsr', { mnemonic: 'jsr', name: 'Jump to Subroutine', description: 'Jumps to subroutine, pushes return address', flags: '-', cycles: '6', bytes: '3' }],
		['jsl', { mnemonic: 'jsl', name: 'Jump to Subroutine Long', description: 'Long subroutine call (65816)', flags: '-', cycles: '8', bytes: '4' }],
		['rts', { mnemonic: 'rts', name: 'Return from Subroutine', description: 'Returns from subroutine', flags: '-', cycles: '6', bytes: '1' }],
		['rtl', { mnemonic: 'rtl', name: 'Return from Subroutine Long', description: 'Long return (65816)', flags: '-', cycles: '6', bytes: '1' }],
		['rti', { mnemonic: 'rti', name: 'Return from Interrupt', description: 'Returns from interrupt handler', flags: 'all', cycles: '6-7', bytes: '1' }],

		// Stack
		['pha', { mnemonic: 'pha', name: 'Push Accumulator', description: 'Pushes accumulator onto stack', flags: '-', cycles: '3-4', bytes: '1' }],
		['php', { mnemonic: 'php', name: 'Push Processor Status', description: 'Pushes status flags onto stack', flags: '-', cycles: '3', bytes: '1' }],
		['phx', { mnemonic: 'phx', name: 'Push X Register', description: 'Pushes X register onto stack (65C02/65816)', flags: '-', cycles: '3-4', bytes: '1' }],
		['phy', { mnemonic: 'phy', name: 'Push Y Register', description: 'Pushes Y register onto stack (65C02/65816)', flags: '-', cycles: '3-4', bytes: '1' }],
		['pla', { mnemonic: 'pla', name: 'Pull Accumulator', description: 'Pulls accumulator from stack', flags: 'N, Z', cycles: '4-5', bytes: '1' }],
		['plp', { mnemonic: 'plp', name: 'Pull Processor Status', description: 'Pulls status flags from stack', flags: 'all', cycles: '4', bytes: '1' }],
		['plx', { mnemonic: 'plx', name: 'Pull X Register', description: 'Pulls X register from stack (65C02/65816)', flags: 'N, Z', cycles: '4-5', bytes: '1' }],
		['ply', { mnemonic: 'ply', name: 'Pull Y Register', description: 'Pulls Y register from stack (65C02/65816)', flags: 'N, Z', cycles: '4-5', bytes: '1' }],

		// Transfers
		['tax', { mnemonic: 'tax', name: 'Transfer A to X', description: 'Copies accumulator to X register', flags: 'N, Z', cycles: '2', bytes: '1' }],
		['tay', { mnemonic: 'tay', name: 'Transfer A to Y', description: 'Copies accumulator to Y register', flags: 'N, Z', cycles: '2', bytes: '1' }],
		['txa', { mnemonic: 'txa', name: 'Transfer X to A', description: 'Copies X register to accumulator', flags: 'N, Z', cycles: '2', bytes: '1' }],
		['tya', { mnemonic: 'tya', name: 'Transfer Y to A', description: 'Copies Y register to accumulator', flags: 'N, Z', cycles: '2', bytes: '1' }],
		['tsx', { mnemonic: 'tsx', name: 'Transfer SP to X', description: 'Copies stack pointer to X register', flags: 'N, Z', cycles: '2', bytes: '1' }],
		['txs', { mnemonic: 'txs', name: 'Transfer X to SP', description: 'Copies X register to stack pointer', flags: '-', cycles: '2', bytes: '1' }],

		// Comparisons
		['cmp', { mnemonic: 'cmp', name: 'Compare Accumulator', description: 'Compares accumulator with memory', flags: 'N, Z, C', cycles: '2-6', bytes: '2-3' }],
		['cpx', { mnemonic: 'cpx', name: 'Compare X Register', description: 'Compares X register with memory', flags: 'N, Z, C', cycles: '2-4', bytes: '2-3' }],
		['cpy', { mnemonic: 'cpy', name: 'Compare Y Register', description: 'Compares Y register with memory', flags: 'N, Z, C', cycles: '2-4', bytes: '2-3' }],
		['bit', { mnemonic: 'bit', name: 'Bit Test', description: 'Tests bits in memory against accumulator', flags: 'N, V, Z', cycles: '3-5', bytes: '2-3' }],

		// Flags
		['clc', { mnemonic: 'clc', name: 'Clear Carry', description: 'Clears the carry flag', flags: 'C = 0', cycles: '2', bytes: '1' }],
		['sec', { mnemonic: 'sec', name: 'Set Carry', description: 'Sets the carry flag', flags: 'C = 1', cycles: '2', bytes: '1' }],
		['cld', { mnemonic: 'cld', name: 'Clear Decimal', description: 'Clears the decimal mode flag', flags: 'D = 0', cycles: '2', bytes: '1' }],
		['sed', { mnemonic: 'sed', name: 'Set Decimal', description: 'Sets the decimal mode flag', flags: 'D = 1', cycles: '2', bytes: '1' }],
		['cli', { mnemonic: 'cli', name: 'Clear Interrupt', description: 'Clears the interrupt disable flag', flags: 'I = 0', cycles: '2', bytes: '1' }],
		['sei', { mnemonic: 'sei', name: 'Set Interrupt', description: 'Sets the interrupt disable flag', flags: 'I = 1', cycles: '2', bytes: '1' }],
		['clv', { mnemonic: 'clv', name: 'Clear Overflow', description: 'Clears the overflow flag', flags: 'V = 0', cycles: '2', bytes: '1' }],

		// Misc
		['nop', { mnemonic: 'nop', name: 'No Operation', description: 'Does nothing for one cycle', flags: '-', cycles: '2', bytes: '1' }],
		['brk', { mnemonic: 'brk', name: 'Break', description: 'Forces a software interrupt', flags: 'B = 1', cycles: '7', bytes: '1' }],

		// 65816 specific
		['rep', { mnemonic: 'rep', name: 'Reset Processor Bits', description: 'Clears specified bits in status register (65816)', flags: 'varies', cycles: '3', bytes: '2' }],
		['sep', { mnemonic: 'sep', name: 'Set Processor Bits', description: 'Sets specified bits in status register (65816)', flags: 'varies', cycles: '3', bytes: '2' }],
		['xce', { mnemonic: 'xce', name: 'Exchange Carry and Emulation', description: 'Swaps carry and emulation bits (65816)', flags: 'C, E', cycles: '2', bytes: '1' }],
		['wai', { mnemonic: 'wai', name: 'Wait for Interrupt', description: 'Halts CPU until interrupt (65816)', flags: '-', cycles: '3', bytes: '1' }],
		['stp', { mnemonic: 'stp', name: 'Stop Processor', description: 'Halts CPU until reset (65816)', flags: '-', cycles: '3', bytes: '1' }],
		['phb', { mnemonic: 'phb', name: 'Push Data Bank', description: 'Pushes data bank register (65816)', flags: '-', cycles: '3', bytes: '1' }],
		['phd', { mnemonic: 'phd', name: 'Push Direct Page', description: 'Pushes direct page register (65816)', flags: '-', cycles: '4', bytes: '1' }],
		['phk', { mnemonic: 'phk', name: 'Push Program Bank', description: 'Pushes program bank register (65816)', flags: '-', cycles: '3', bytes: '1' }],
		['plb', { mnemonic: 'plb', name: 'Pull Data Bank', description: 'Pulls data bank register (65816)', flags: 'N, Z', cycles: '4', bytes: '1' }],
		['pld', { mnemonic: 'pld', name: 'Pull Direct Page', description: 'Pulls direct page register (65816)', flags: 'N, Z', cycles: '5', bytes: '1' }],
		['tcd', { mnemonic: 'tcd', name: 'Transfer A to Direct Page', description: 'Copies 16-bit A to direct page (65816)', flags: 'N, Z', cycles: '2', bytes: '1' }],
		['tdc', { mnemonic: 'tdc', name: 'Transfer Direct Page to A', description: 'Copies direct page to 16-bit A (65816)', flags: 'N, Z', cycles: '2', bytes: '1' }],
		['tcs', { mnemonic: 'tcs', name: 'Transfer A to Stack Pointer', description: 'Copies 16-bit A to stack pointer (65816)', flags: '-', cycles: '2', bytes: '1' }],
		['tsc', { mnemonic: 'tsc', name: 'Transfer Stack Pointer to A', description: 'Copies stack pointer to 16-bit A (65816)', flags: 'N, Z', cycles: '2', bytes: '1' }],
		['xba', { mnemonic: 'xba', name: 'Exchange B and A', description: 'Swaps the high and low bytes of the accumulator (65816)', flags: 'N, Z', cycles: '3', bytes: '1' }],
		['mvn', { mnemonic: 'mvn', name: 'Block Move Negative', description: 'Block memory move with decrementing addresses (65816)', flags: '-', cycles: '7/byte', bytes: '3' }],
		['mvp', { mnemonic: 'mvp', name: 'Block Move Positive', description: 'Block memory move with incrementing addresses (65816)', flags: '-', cycles: '7/byte', bytes: '3' }],
		['pea', { mnemonic: 'pea', name: 'Push Effective Address', description: 'Pushes a 16-bit address onto the stack (65816)', flags: '-', cycles: '5', bytes: '3' }],
		['pei', { mnemonic: 'pei', name: 'Push Effective Indirect', description: 'Pushes indirect address from direct page (65816)', flags: '-', cycles: '6', bytes: '2' }],
		['per', { mnemonic: 'per', name: 'Push PC Relative', description: 'Pushes PC + signed offset onto stack (65816)', flags: '-', cycles: '6', bytes: '3' }],
		['cop', { mnemonic: 'cop', name: 'Coprocessor', description: 'Generates a coprocessor interrupt (65816)', flags: '-', cycles: '7', bytes: '2' }],
		['trb', { mnemonic: 'trb', name: 'Test and Reset Bits', description: 'Tests bits and resets them in memory (65C02/65816)', flags: 'Z', cycles: '5-7', bytes: '2-3' }],
		['tsb', { mnemonic: 'tsb', name: 'Test and Set Bits', description: 'Tests bits and sets them in memory (65C02/65816)', flags: 'Z', cycles: '5-7', bytes: '2-3' }],
		['jml', { mnemonic: 'jml', name: 'Jump Long', description: 'Jumps to a 24-bit address (65816)', flags: '-', cycles: '4', bytes: '4' }],
		['wdm', { mnemonic: 'wdm', name: 'Reserved (WDM)', description: 'Reserved for future expansion (65816)', flags: '-', cycles: '2', bytes: '2' }],

		// SM83 (Game Boy) specific
		['ld', { mnemonic: 'ld', name: 'Load', description: 'Loads a value into a register or memory (SM83/Z80)', flags: 'varies', cycles: '1-5', bytes: '1-3' }],
		['ldh', { mnemonic: 'ldh', name: 'Load High', description: 'Load to/from $ff00+n (Game Boy)', flags: '-', cycles: '3', bytes: '2' }],
		['push', { mnemonic: 'push', name: 'Push', description: 'Push 16-bit register pair onto stack (SM83/Z80)', flags: '-', cycles: '4', bytes: '1' }],
		['pop', { mnemonic: 'pop', name: 'Pop', description: 'Pop 16-bit value from stack into register pair (SM83/Z80)', flags: 'varies', cycles: '3', bytes: '1' }],
		['add', { mnemonic: 'add', name: 'Add', description: 'Adds operand to accumulator or register (SM83/Z80/M68000/ARM)', flags: 'varies', cycles: 'varies', bytes: 'varies' }],
		['sub', { mnemonic: 'sub', name: 'Subtract', description: 'Subtracts operand from accumulator (SM83/Z80/M68000/ARM)', flags: 'varies', cycles: 'varies', bytes: 'varies' }],
		['cp', { mnemonic: 'cp', name: 'Compare', description: 'Compares A with operand without storing result (SM83/Z80)', flags: 'Z, N, H, C', cycles: '1-2', bytes: '1-2' }],
		['xor', { mnemonic: 'xor', name: 'Exclusive OR', description: 'XOR with accumulator (SM83/Z80)', flags: 'Z, N=0, H=0, C=0', cycles: '1-2', bytes: '1-2' }],
		['or', { mnemonic: 'or', name: 'Logical OR', description: 'OR with accumulator (SM83/Z80)', flags: 'Z, N=0, H=0, C=0', cycles: '1-2', bytes: '1-2' }],
		['swap', { mnemonic: 'swap', name: 'Swap Nibbles', description: 'Swaps upper and lower nibbles of a register (SM83)', flags: 'Z', cycles: '2', bytes: '2' }],
		['jp', { mnemonic: 'jp', name: 'Jump', description: 'Jumps to an absolute address (SM83/Z80)', flags: '-', cycles: '3-4', bytes: '3' }],
		['jr', { mnemonic: 'jr', name: 'Jump Relative', description: 'Relative jump by signed offset (SM83/Z80)', flags: '-', cycles: '2-3', bytes: '2' }],
		['call', { mnemonic: 'call', name: 'Call Subroutine', description: 'Pushes PC and jumps to address (SM83/Z80/V30MZ)', flags: '-', cycles: '3-6', bytes: '3' }],
		['ret', { mnemonic: 'ret', name: 'Return', description: 'Returns from subroutine (SM83/Z80/V30MZ/SPC700)', flags: '-', cycles: '2-5', bytes: '1' }],
		['reti', { mnemonic: 'reti', name: 'Return from Interrupt', description: 'Returns from interrupt handler and re-enables interrupts', flags: 'varies', cycles: '4', bytes: '1' }],
		['rst', { mnemonic: 'rst', name: 'Restart', description: 'Calls a fixed address vector (SM83/Z80)', flags: '-', cycles: '4', bytes: '1' }],
		['halt', { mnemonic: 'halt', name: 'Halt', description: 'Halts CPU until an interrupt occurs (SM83/Z80)', flags: '-', cycles: '1', bytes: '1' }],
		['stop', { mnemonic: 'stop', name: 'Stop', description: 'Stops CPU and LCD (Game Boy) or halts processor', flags: '-', cycles: '1', bytes: '1' }],
		['di', { mnemonic: 'di', name: 'Disable Interrupts', description: 'Disables interrupt handling (SM83/Z80)', flags: '-', cycles: '1', bytes: '1' }],
		['ei', { mnemonic: 'ei', name: 'Enable Interrupts', description: 'Enables interrupt handling (SM83/Z80)', flags: '-', cycles: '1', bytes: '1' }],
		['daa', { mnemonic: 'daa', name: 'Decimal Adjust', description: 'Adjusts accumulator for BCD arithmetic', flags: 'Z, H, C', cycles: '1', bytes: '1' }],
		['cpl', { mnemonic: 'cpl', name: 'Complement', description: 'Complements (bitwise NOT) the accumulator', flags: 'N, H', cycles: '1', bytes: '1' }],
		['ccf', { mnemonic: 'ccf', name: 'Complement Carry Flag', description: 'Inverts the carry flag', flags: 'C', cycles: '1', bytes: '1' }],
		['scf', { mnemonic: 'scf', name: 'Set Carry Flag', description: 'Sets the carry flag to 1', flags: 'C = 1', cycles: '1', bytes: '1' }],
		['rlca', { mnemonic: 'rlca', name: 'Rotate Left Circular A', description: 'Rotates accumulator left, bit 7 to carry and bit 0', flags: 'C', cycles: '1', bytes: '1' }],
		['rla', { mnemonic: 'rla', name: 'Rotate Left through Carry A', description: 'Rotates accumulator left through carry', flags: 'C', cycles: '1', bytes: '1' }],
		['rrca', { mnemonic: 'rrca', name: 'Rotate Right Circular A', description: 'Rotates accumulator right, bit 0 to carry and bit 7', flags: 'C', cycles: '1', bytes: '1' }],
		['rra', { mnemonic: 'rra', name: 'Rotate Right through Carry A', description: 'Rotates accumulator right through carry', flags: 'C', cycles: '1', bytes: '1' }],
		['sla', { mnemonic: 'sla', name: 'Shift Left Arithmetic', description: 'Shifts register/memory left, bit 7 to carry (SM83/Z80)', flags: 'Z, C', cycles: '2', bytes: '2' }],
		['sra', { mnemonic: 'sra', name: 'Shift Right Arithmetic', description: 'Shifts right preserving sign bit (SM83/Z80)', flags: 'Z, C', cycles: '2', bytes: '2' }],
		['srl', { mnemonic: 'srl', name: 'Shift Right Logical', description: 'Shifts right, MSB becomes 0 (SM83/Z80)', flags: 'Z, C', cycles: '2', bytes: '2' }],
		['res', { mnemonic: 'res', name: 'Reset Bit', description: 'Resets (clears) a specific bit to 0 (SM83/Z80)', flags: '-', cycles: '2', bytes: '2' }],
		['set', { mnemonic: 'set', name: 'Set Bit', description: 'Sets a specific bit to 1 (SM83/Z80)', flags: '-', cycles: '2', bytes: '2' }],

		// Z80 (SMS/GG) specific
		['djnz', { mnemonic: 'djnz', name: 'Decrement and Jump if Not Zero', description: 'Decrements B and branches if B is not zero (Z80)', flags: '-', cycles: '3-4', bytes: '2' }],
		['ex', { mnemonic: 'ex', name: 'Exchange', description: 'Exchanges register pairs (Z80)', flags: '-', cycles: '1-5', bytes: '1-2' }],
		['exx', { mnemonic: 'exx', name: 'Exchange All', description: 'Exchanges BC/DE/HL with shadow registers (Z80)', flags: '-', cycles: '1', bytes: '1' }],
		['im', { mnemonic: 'im', name: 'Interrupt Mode', description: 'Sets interrupt mode 0, 1, or 2 (Z80)', flags: '-', cycles: '2', bytes: '2' }],
		['neg', { mnemonic: 'neg', name: 'Negate', description: 'Twos complement negation: A = 0 - A (Z80) or dest = 0 - dest (M68000)', flags: 'varies', cycles: '2', bytes: '2' }],
		['retn', { mnemonic: 'retn', name: 'Return from NMI', description: 'Returns from non-maskable interrupt (Z80)', flags: '-', cycles: '4', bytes: '2' }],
		['ldir', { mnemonic: 'ldir', name: 'Load, Increment, Repeat', description: 'Block copy: repeats LDI until BC=0 (Z80)', flags: 'P/V=0', cycles: '5-6', bytes: '2' }],
		['lddr', { mnemonic: 'lddr', name: 'Load, Decrement, Repeat', description: 'Block copy decrementing: repeats LDD until BC=0 (Z80)', flags: 'P/V=0', cycles: '5-6', bytes: '2' }],
		['cpir', { mnemonic: 'cpir', name: 'Compare, Increment, Repeat', description: 'Block search: repeats CPI until match or BC=0 (Z80)', flags: 'Z, P/V', cycles: '5-6', bytes: '2' }],
		['in', { mnemonic: 'in', name: 'Input from Port', description: 'Reads from an I/O port (Z80/V30MZ)', flags: 'varies', cycles: '3-4', bytes: '2' }],
		['out', { mnemonic: 'out', name: 'Output to Port', description: 'Writes to an I/O port (Z80/V30MZ)', flags: '-', cycles: '3-4', bytes: '2' }],

		// M68000 (Genesis) specific
		['move', { mnemonic: 'move', name: 'Move', description: 'Moves data between registers and memory (M68000)', flags: 'N, Z', cycles: '4-14', bytes: '2-6' }],
		['movea', { mnemonic: 'movea', name: 'Move to Address Register', description: 'Moves data to an address register (M68000)', flags: '-', cycles: '4-12', bytes: '2-6' }],
		['moveq', { mnemonic: 'moveq', name: 'Move Quick', description: 'Loads a sign-extended 8-bit immediate to data register (M68000)', flags: 'N, Z', cycles: '4', bytes: '2' }],
		['movem', { mnemonic: 'movem', name: 'Move Multiple', description: 'Saves/restores multiple registers to/from memory (M68000)', flags: '-', cycles: '12+', bytes: '4-6' }],
		['lea', { mnemonic: 'lea', name: 'Load Effective Address', description: 'Loads an effective address into an address register (M68000/V30MZ)', flags: '-', cycles: '4-12', bytes: '2-6' }],
		['exg', { mnemonic: 'exg', name: 'Exchange Registers', description: 'Exchanges the contents of two registers (M68000)', flags: '-', cycles: '6', bytes: '2' }],
		['link', { mnemonic: 'link', name: 'Link', description: 'Creates a stack frame for subroutine local storage (M68000)', flags: '-', cycles: '16', bytes: '4' }],
		['unlk', { mnemonic: 'unlk', name: 'Unlink', description: 'Restores stack after LINK (M68000)', flags: '-', cycles: '12', bytes: '2' }],
		['mulu', { mnemonic: 'mulu', name: 'Unsigned Multiply', description: 'Multiplies two unsigned 16-bit values (M68000)', flags: 'N, Z', cycles: '38-70', bytes: '2-4' }],
		['muls', { mnemonic: 'muls', name: 'Signed Multiply', description: 'Multiplies two signed 16-bit values (M68000)', flags: 'N, Z', cycles: '38-70', bytes: '2-4' }],
		['divu', { mnemonic: 'divu', name: 'Unsigned Divide', description: 'Unsigned 32÷16 division (M68000)', flags: 'N, Z, V', cycles: '76-140', bytes: '2-4' }],
		['divs', { mnemonic: 'divs', name: 'Signed Divide', description: 'Signed 32÷16 division (M68000)', flags: 'N, Z, V', cycles: '120-158', bytes: '2-4' }],
		['clr', { mnemonic: 'clr', name: 'Clear', description: 'Clears operand to zero (M68000)', flags: 'N=0, Z=1', cycles: '4-12', bytes: '2-4' }],
		['tst', { mnemonic: 'tst', name: 'Test', description: 'Tests an operand and sets condition codes (M68000)', flags: 'N, Z', cycles: '4-8', bytes: '2-4' }],
		['ext', { mnemonic: 'ext', name: 'Sign Extend', description: 'Sign-extends a data register (M68000)', flags: 'N, Z', cycles: '4', bytes: '2' }],
		['btst', { mnemonic: 'btst', name: 'Bit Test', description: 'Tests a bit in an operand (M68000)', flags: 'Z', cycles: '4-8', bytes: '2-4' }],
		['bset', { mnemonic: 'bset', name: 'Bit Set', description: 'Tests and sets a bit (M68000)', flags: 'Z', cycles: '6-12', bytes: '2-4' }],
		['bclr', { mnemonic: 'bclr', name: 'Bit Clear', description: 'Tests and clears a bit (M68000)', flags: 'Z', cycles: '6-12', bytes: '2-4' }],
		['bsr', { mnemonic: 'bsr', name: 'Branch to Subroutine', description: 'Relative subroutine call (M68000/HuC6280)', flags: '-', cycles: '10-18', bytes: '2-4' }],
		['trap', { mnemonic: 'trap', name: 'Trap', description: 'Generates a software exception (M68000)', flags: '-', cycles: '34', bytes: '2' }],
		['rte', { mnemonic: 'rte', name: 'Return from Exception', description: 'Returns from exception handler (M68000)', flags: 'all', cycles: '20', bytes: '2' }],
		['dbcc', { mnemonic: 'dbcc', name: 'Decrement and Branch', description: 'Decrements register and branches if condition false and reg ≠ -1 (M68000)', flags: '-', cycles: '10-14', bytes: '4' }],

		// ARM7TDMI (GBA) specific
		['mov', { mnemonic: 'mov', name: 'Move', description: 'Copies a value into a register (ARM/V30MZ/SPC700)', flags: 'varies', cycles: '1', bytes: '2-4' }],
		['ldr', { mnemonic: 'ldr', name: 'Load Register', description: 'Loads a word from memory into a register (ARM)', flags: '-', cycles: '3', bytes: '4' }],
		['str', { mnemonic: 'str', name: 'Store Register', description: 'Stores a register value to memory (ARM)', flags: '-', cycles: '2', bytes: '4' }],
		['b', { mnemonic: 'b', name: 'Branch', description: 'Unconditional branch to address (ARM)', flags: '-', cycles: '3', bytes: '4' }],
		['bl', { mnemonic: 'bl', name: 'Branch with Link', description: 'Branch and save return address in LR (ARM)', flags: '-', cycles: '3', bytes: '4' }],
		['bx', { mnemonic: 'bx', name: 'Branch and Exchange', description: 'Branch and switch between ARM/Thumb mode (ARM)', flags: '-', cycles: '3', bytes: '4' }],
		['mul', { mnemonic: 'mul', name: 'Multiply', description: 'Multiplies two registers (ARM/V30MZ/SPC700)', flags: 'varies', cycles: '1-4', bytes: '2-4' }],
		['orr', { mnemonic: 'orr', name: 'Logical OR', description: 'Performs bitwise OR on two registers (ARM)', flags: 'N, Z, C', cycles: '1', bytes: '4' }],
		['bic', { mnemonic: 'bic', name: 'Bit Clear', description: 'AND with complement: Rd = Rn & ~Op2 (ARM)', flags: 'N, Z, C', cycles: '1', bytes: '4' }],
		['swi', { mnemonic: 'swi', name: 'Software Interrupt', description: 'Triggers a BIOS system call (ARM)', flags: '-', cycles: 'varies', bytes: '4' }],
		['mvn', { mnemonic: 'mvn', name: 'Move NOT', description: 'Loads bitwise NOT of operand into register (ARM)', flags: 'N, Z, C', cycles: '1', bytes: '4' }],
		['cmn', { mnemonic: 'cmn', name: 'Compare Negative', description: 'Sets flags as if adding Rn + Op2 (ARM)', flags: 'N, Z, C, V', cycles: '1', bytes: '4' }],
		['tst', { mnemonic: 'tst', name: 'Test', description: 'Sets flags for Rn AND Op2 without storing result (ARM)', flags: 'N, Z, C', cycles: '1', bytes: '4' }],
		['teq', { mnemonic: 'teq', name: 'Test Equivalence', description: 'Sets flags for Rn XOR Op2 without storing result (ARM)', flags: 'N, Z, C', cycles: '1', bytes: '4' }],
		['ldm', { mnemonic: 'ldm', name: 'Load Multiple', description: 'Loads multiple registers from consecutive memory (ARM)', flags: 'varies', cycles: '2+n', bytes: '4' }],
		['stm', { mnemonic: 'stm', name: 'Store Multiple', description: 'Stores multiple registers to consecutive memory (ARM)', flags: '-', cycles: '2+n', bytes: '4' }],

		// HuC6280 (TG-16) specific
		['tam', { mnemonic: 'tam', name: 'Transfer A to MPR', description: 'Sets memory mapping page register from accumulator (HuC6280)', flags: '-', cycles: '5', bytes: '2' }],
		['tma', { mnemonic: 'tma', name: 'Transfer MPR to A', description: 'Reads memory mapping page register into accumulator (HuC6280)', flags: 'N, Z', cycles: '4', bytes: '2' }],
		['csh', { mnemonic: 'csh', name: 'Change Speed High', description: 'Switches CPU to high speed 7.16 MHz (HuC6280)', flags: '-', cycles: '3', bytes: '1' }],
		['csl', { mnemonic: 'csl', name: 'Change Speed Low', description: 'Switches CPU to low speed 1.79 MHz (HuC6280)', flags: '-', cycles: '3', bytes: '1' }],
		['st0', { mnemonic: 'st0', name: 'Store VDC Register 0', description: 'Writes to VDC address register (HuC6280)', flags: '-', cycles: '5', bytes: '2' }],
		['st1', { mnemonic: 'st1', name: 'Store VDC Register 1', description: 'Writes to VDC data low (HuC6280)', flags: '-', cycles: '5', bytes: '2' }],
		['st2', { mnemonic: 'st2', name: 'Store VDC Register 2', description: 'Writes to VDC data high (HuC6280)', flags: '-', cycles: '5', bytes: '2' }],
		['tii', { mnemonic: 'tii', name: 'Transfer Increment-Increment', description: 'Block move: src++, dest++ (HuC6280)', flags: '-', cycles: '17+6n', bytes: '7' }],
		['tdd', { mnemonic: 'tdd', name: 'Transfer Decrement-Decrement', description: 'Block move: src--, dest-- (HuC6280)', flags: '-', cycles: '17+6n', bytes: '7' }],
		['cla', { mnemonic: 'cla', name: 'Clear Accumulator', description: 'Sets accumulator to zero (HuC6280)', flags: '-', cycles: '2', bytes: '1' }],
		['clx', { mnemonic: 'clx', name: 'Clear X', description: 'Sets X register to zero (HuC6280)', flags: '-', cycles: '2', bytes: '1' }],
		['cly', { mnemonic: 'cly', name: 'Clear Y', description: 'Sets Y register to zero (HuC6280)', flags: '-', cycles: '2', bytes: '1' }],
		['say', { mnemonic: 'say', name: 'Swap A and Y', description: 'Exchanges A and Y registers (HuC6280)', flags: '-', cycles: '3', bytes: '1' }],
		['sxy', { mnemonic: 'sxy', name: 'Swap X and Y', description: 'Exchanges X and Y registers (HuC6280)', flags: '-', cycles: '3', bytes: '1' }],
		['sax', { mnemonic: 'sax', name: 'Swap A and X', description: 'Exchanges A and X registers (HuC6280)', flags: '-', cycles: '3', bytes: '1' }],

		// V30MZ (WonderSwan) specific
		['xchg', { mnemonic: 'xchg', name: 'Exchange', description: 'Exchanges two operands (V30MZ)', flags: '-', cycles: '3-5', bytes: '1-2' }],
		['loop', { mnemonic: 'loop', name: 'Loop', description: 'Decrements CX and jumps if CX ≠ 0 (V30MZ)', flags: '-', cycles: '5-6', bytes: '2' }],
		['int', { mnemonic: 'int', name: 'Software Interrupt', description: 'Triggers a software interrupt (V30MZ)', flags: '-', cycles: '51', bytes: '2' }],
		['iret', { mnemonic: 'iret', name: 'Interrupt Return', description: 'Returns from interrupt handler (V30MZ)', flags: 'all', cycles: '24', bytes: '1' }],
		['hlt', { mnemonic: 'hlt', name: 'Halt', description: 'Halts processor until interrupt (V30MZ)', flags: '-', cycles: '2', bytes: '1' }],
		['shl', { mnemonic: 'shl', name: 'Shift Left', description: 'Logical shift left (V30MZ)', flags: 'O, S, Z, P, C', cycles: '2-8', bytes: '2-3' }],
		['shr', { mnemonic: 'shr', name: 'Shift Right', description: 'Logical shift right (V30MZ)', flags: 'O, S, Z, P, C', cycles: '2-8', bytes: '2-3' }],
		['test', { mnemonic: 'test', name: 'Test', description: 'Performs AND and sets flags without storing result (V30MZ)', flags: 'S, Z, P', cycles: '3-7', bytes: '2-4' }],

		// SPC700 (SNES Audio) specific
		['movw', { mnemonic: 'movw', name: 'Move Word', description: 'Moves 16-bit value between YA and direct page (SPC700)', flags: 'N, Z', cycles: '5', bytes: '2' }],
		['addw', { mnemonic: 'addw', name: 'Add Word', description: 'Adds 16-bit value: YA += dp.w (SPC700)', flags: 'N, V, H, Z, C', cycles: '5', bytes: '2' }],
		['subw', { mnemonic: 'subw', name: 'Subtract Word', description: 'Subtracts 16-bit value: YA -= dp.w (SPC700)', flags: 'N, V, H, Z, C', cycles: '5', bytes: '2' }],
		['xcn', { mnemonic: 'xcn', name: 'Exchange Nibbles', description: 'Swaps the upper and lower nibbles of A (SPC700)', flags: 'N, Z', cycles: '5', bytes: '1' }],
		['set1', { mnemonic: 'set1', name: 'Set Bit in Direct Page', description: 'Sets a specific bit in a direct page byte (SPC700)', flags: '-', cycles: '4', bytes: '2' }],
		['clr1', { mnemonic: 'clr1', name: 'Clear Bit in Direct Page', description: 'Clears a specific bit in a direct page byte (SPC700)', flags: '-', cycles: '4', bytes: '2' }],
		['dbnz', { mnemonic: 'dbnz', name: 'Decrement and Branch if Not Zero', description: 'Decrements operand and branches if non-zero (SPC700)', flags: '-', cycles: '5-6', bytes: '3' }],
		['tcall', { mnemonic: 'tcall', name: 'Table Call', description: 'Calls subroutine through vector table (SPC700)', flags: '-', cycles: '8', bytes: '1' }],
		['pcall', { mnemonic: 'pcall', name: 'Page Call', description: 'Calls subroutine at $ff00+n (SPC700)', flags: '-', cycles: '6', bytes: '2' }],
		['cbne', { mnemonic: 'cbne', name: 'Compare and Branch if Not Equal', description: 'Compares dp with A, branches if not equal (SPC700)', flags: '-', cycles: '5-7', bytes: '3' }],
		['sleep', { mnemonic: 'sleep', name: 'Sleep', description: 'Enters sleep state waiting for interrupt (SPC700)', flags: '-', cycles: '3', bytes: '1' }],
	]);

	private static readonly directives: Map<string, DirectiveInfo> = new Map([
		['.org', { name: '.org', description: 'Sets the current assembly origin address', syntax: '.org <address>', example: '.org $8000' }],
		['.byte', { name: '.byte', description: 'Defines one or more bytes of data', syntax: '.byte <value>[, <value>...]', example: '.byte $00, $01, $02' }],
		['.db', { name: '.db', description: 'Alias for .byte - defines bytes of data', syntax: '.db <value>[, <value>...]', example: '.db "Hello", 0' }],
		['.word', { name: '.word', description: 'Defines one or more 16-bit words (little-endian)', syntax: '.word <value>[, <value>...]', example: '.word $1234, label' }],
		['.dw', { name: '.dw', description: 'Alias for .word - defines 16-bit words', syntax: '.dw <value>[, <value>...]', example: '.dw $1234' }],
		['.long', { name: '.long', description: 'Defines one or more 24-bit values (65816) or 32-bit values', syntax: '.long <value>[, <value>...]', example: '.long $123456' }],
		['.ds', { name: '.ds', description: 'Reserves space (define space)', syntax: '.ds <count>[, <fill>]', example: '.ds 16, $ff' }],
		['.fill', { name: '.fill', description: 'Fills space with a value', syntax: '.fill <count>[, <value>]', example: '.fill 256, $00' }],
		['.res', { name: '.res', description: 'Reserves uninitialized space', syntax: '.res <count>', example: '.res 100' }],
		['.align', { name: '.align', description: 'Aligns to a boundary', syntax: '.align <boundary>[, <fill>]', example: '.align 256' }],
		['.pad', { name: '.pad', description: 'Pads to a specific address', syntax: '.pad <address>[, <fill>]', example: '.pad $c000' }],
		['.include', { name: '.include', description: 'Includes another source file', syntax: '.include "<filename>"', example: '.include "macros.inc"' }],
		['.incbin', { name: '.incbin', description: 'Includes a binary file', syntax: '.incbin "<filename>"[, <start>, <length>]', example: '.incbin "graphics.chr"' }],
		['.macro', { name: '.macro', description: 'Defines a macro', syntax: '.macro <name> [params...]', example: '.macro add16 dest, src' }],
		['.endmacro', { name: '.endmacro', description: 'Ends a macro definition', syntax: '.endmacro', example: '.endmacro' }],
		['.if', { name: '.if', description: 'Conditional assembly', syntax: '.if <expression>', example: '.if DEBUG' }],
		['.else', { name: '.else', description: 'Else branch of conditional', syntax: '.else', example: '.else' }],
		['.elseif', { name: '.elseif', description: 'Else-if branch of conditional', syntax: '.elseif <expression>', example: '.elseif VERSION > 1' }],
		['.endif', { name: '.endif', description: 'Ends conditional assembly', syntax: '.endif', example: '.endif' }],
		['.ifdef', { name: '.ifdef', description: 'Conditional if symbol is defined', syntax: '.ifdef <symbol>', example: '.ifdef DEBUG' }],
		['.ifndef', { name: '.ifndef', description: 'Conditional if symbol is not defined', syntax: '.ifndef <symbol>', example: '.ifndef RELEASE' }],
		['.segment', { name: '.segment', description: 'Defines a named memory segment', syntax: '.segment "<name>", <start>, <size>', example: '.segment "CODE", $8000, $4000' }],
		['.bank', { name: '.bank', description: 'Switches to a ROM bank', syntax: '.bank <number>', example: '.bank 0' }],
		['.equ', { name: '.equ', description: 'Defines a constant value', syntax: '<name> .equ <value>', example: 'SCREEN_WIDTH .equ 256' }],
		['.target', { name: '.target', description: 'Sets the target architecture', syntax: '.target <arch>', example: '.target nes' }],
		['.scope', { name: '.scope', description: 'Begins a scoped block for local labels', syntax: '.scope [name]', example: '.scope update_player' }],
		['.endscope', { name: '.endscope', description: 'Ends a scoped block', syntax: '.endscope', example: '.endscope' }],
		['.enum', { name: '.enum', description: 'Begins an enumeration block', syntax: '.enum [start_value]', example: '.enum $0000' }],
		['.endenum', { name: '.endenum', description: 'Ends an enumeration block', syntax: '.endenum', example: '.endenum' }],
		['.repeat', { name: '.repeat', description: 'Repeats a block of code', syntax: '.repeat <count>', example: '.repeat 8' }],
		['.endrepeat', { name: '.endrepeat', description: 'Ends a repeat block', syntax: '.endrepeat', example: '.endrepeat' }],
		['.rept', { name: '.rept', description: 'Alias for .repeat', syntax: '.rept <count>', example: '.rept 4' }],
		['.endr', { name: '.endr', description: 'Alias for .endrepeat', syntax: '.endr', example: '.endr' }],
		['.define', { name: '.define', description: 'Defines a preprocessor symbol', syntax: '.define <name> [value]', example: '.define DEBUG 1' }],
		['.undef', { name: '.undef', description: 'Undefines a previously defined symbol', syntax: '.undef <name>', example: '.undef DEBUG' }],
		['.ende', { name: '.ende', description: 'Alias for .endenum', syntax: '.ende', example: '.ende' }],
		['.endm', { name: '.endm', description: 'Alias for .endmacro', syntax: '.endm', example: '.endm' }],
		['.banksize', { name: '.banksize', description: 'Sets the size of each ROM bank', syntax: '.banksize <size>', example: '.banksize $4000' }],
		['.mapper', { name: '.mapper', description: 'Sets the NES mapper number', syntax: '.mapper <number>', example: '.mapper 1' }],
		['.assert', { name: '.assert', description: 'Compile-time assertion; errors if expression is false', syntax: '.assert <expression>[, "message"]', example: '.assert * <= $c000, "Code overflow"' }],
		['.ifeq', { name: '.ifeq', description: 'Assembles block if expression equals zero', syntax: '.ifeq <expression>', example: '.ifeq VERSION' }],
		['.ifne', { name: '.ifne', description: 'Assembles block if expression is non-zero', syntax: '.ifne <expression>', example: '.ifne DEBUG' }],
		['.ifgt', { name: '.ifgt', description: 'Assembles block if expression is greater than zero', syntax: '.ifgt <expression>', example: '.ifgt BANKS - 1' }],
		['.iflt', { name: '.iflt', description: 'Assembles block if expression is less than zero', syntax: '.iflt <expression>', example: '.iflt OFFSET' }],
		['.ifge', { name: '.ifge', description: 'Assembles block if expression is >= zero', syntax: '.ifge <expression>', example: '.ifge COUNT' }],
		['.ifle', { name: '.ifle', description: 'Assembles block if expression is <= zero', syntax: '.ifle <expression>', example: '.ifle REMAINING' }],
		['.error', { name: '.error', description: 'Emits an error message and stops assembly', syntax: '.error "<message>"', example: '.error "Unsupported platform"' }],
		['.warning', { name: '.warning', description: 'Emits a warning message during assembly', syntax: '.warning "<message>"', example: '.warning "Deprecated feature"' }],
		// Platform header shorthands
		['.ines', { name: '.ines', description: 'Enables iNES ROM header generation for NES', syntax: '.ines', example: '.ines' }],
		['.ines2', { name: '.ines2', description: 'Enables iNES 2.0 header format', syntax: '.ines2', example: '.ines2' }],
		['.snes', { name: '.snes', description: 'Enables SNES internal ROM header', syntax: '.snes', example: '.snes' }],
		['.gb', { name: '.gb', description: 'Enables Game Boy ROM header generation', syntax: '.gb', example: '.gb' }],
		['.gba', { name: '.gba', description: 'Enables GBA ROM header generation', syntax: '.gba', example: '.gba' }],
		['.genesis', { name: '.genesis', description: 'Enables Sega Genesis/Mega Drive header', syntax: '.genesis', example: '.genesis' }],
		['.sms', { name: '.sms', description: 'Enables Sega Master System header', syntax: '.sms', example: '.sms' }],
		['.lynx', { name: '.lynx', description: 'Enables Atari Lynx ROM header generation', syntax: '.lynx', example: '.lynx' }],
		['.a2600', { name: '.a2600', description: 'Enables Atari 2600 cartridge format', syntax: '.a2600', example: '.a2600' }],
		['.ws', { name: '.ws', description: 'Enables WonderSwan ROM header', syntax: '.ws', example: '.ws' }],
		['.tg16', { name: '.tg16', description: 'Enables TurboGrafx-16 ROM format', syntax: '.tg16', example: '.tg16' }],
		['.pce', { name: '.pce', description: 'Enables PC Engine ROM format (alias for .tg16)', syntax: '.pce', example: '.pce' }],
		['.channelf', { name: '.channelf', description: 'Enables Channel F cartridge format', syntax: '.channelf', example: '.channelf' }],
		// Mode directives
		['.lorom', { name: '.lorom', description: 'Sets SNES LoROM memory mapping', syntax: '.lorom', example: '.lorom' }],
		['.hirom', { name: '.hirom', description: 'Sets SNES HiROM memory mapping', syntax: '.hirom', example: '.hirom' }],
		['.exhirom', { name: '.exhirom', description: 'Sets SNES ExHiROM memory mapping', syntax: '.exhirom', example: '.exhirom' }],
		['.a8', { name: '.a8', description: 'Sets accumulator to 8-bit mode (65816)', syntax: '.a8', example: '.a8' }],
		['.a16', { name: '.a16', description: 'Sets accumulator to 16-bit mode (65816)', syntax: '.a16', example: '.a16' }],
		['.i8', { name: '.i8', description: 'Sets index registers to 8-bit mode (65816)', syntax: '.i8', example: '.i8' }],
		['.i16', { name: '.i16', description: 'Sets index registers to 16-bit mode (65816)', syntax: '.i16', example: '.i16' }],
		['.smart', { name: '.smart', description: 'Auto-tracks M/X flags from REP/SEP instructions (65816)', syntax: '.smart', example: '.smart' }],
		['.arm', { name: '.arm', description: 'Switches to ARM instruction mode (32-bit, GBA)', syntax: '.arm', example: '.arm' }],
		['.thumb', { name: '.thumb', description: 'Switches to Thumb instruction mode (16-bit, GBA)', syntax: '.thumb', example: '.thumb' }],
		// Lynx-specific
		['.lynx_name', { name: '.lynx_name', description: 'Sets the Lynx game name in ROM header', syntax: '.lynx_name "<name>"', example: '.lynx_name "MY GAME"' }],
		['.lynxboot', { name: '.lynxboot', description: 'Generates Lynx boot code', syntax: '.lynxboot', example: '.lynxboot' }],
		['.lynxentry', { name: '.lynxentry', description: 'Sets the Lynx program entry point', syntax: '.lynxentry <address>', example: '.lynxentry $0200' }],
		// SNES-specific
		['.snes_fastrom', { name: '.snes_fastrom', description: 'Enables SNES FastROM mode (3.58 MHz)', syntax: '.snes_fastrom', example: '.snes_fastrom' }],
	]);

	/**
	 * Provides hover information for a position in a document.
	 */
	async provideHover(
		document: vscode.TextDocument,
		position: vscode.Position,
		_token: vscode.CancellationToken
	): Promise<vscode.Hover | undefined> {
		const wordRange = document.getWordRangeAtPosition(position, /[.%]?[a-zA-Z_@][a-zA-Z0-9_]*/);
		if (!wordRange) {
			return undefined;
		}

		const word = document.getText(wordRange).toLowerCase();

		// Check if it's a directive
		if (word.startsWith('.')) {
			const directive = PoppyHoverProvider.directives.get(word);
			if (directive) {
				return this.createDirectiveHover(directive);
			}
		}

		// Check if it's an instruction
		const instruction = PoppyHoverProvider.instructions.get(word);
		if (instruction) {
			return this.createInstructionHover(instruction);
		}

		// Check for symbol definition in current document first
		const symbolHover = this.findSymbolHover(document, word);
		if (symbolHover) {
			return symbolHover;
		}

		// Fall back to workspace-wide search
		return this.findWorkspaceSymbolHover(document, word);
	}

	/**
	 * Creates hover content for an instruction.
	 */
	private createInstructionHover(info: InstructionInfo): vscode.Hover {
		const md = new vscode.MarkdownString();
		md.appendMarkdown(`## ${info.mnemonic.toUpperCase()} - ${info.name}\n\n`);
		md.appendMarkdown(`${info.description}\n\n`);

		if (info.flags) {
			md.appendMarkdown(`**Flags:** ${info.flags}\n\n`);
		}

		if (info.cycles) {
			md.appendMarkdown(`**Cycles:** ${info.cycles}\n\n`);
		}

		if (info.bytes) {
			md.appendMarkdown(`**Bytes:** ${info.bytes}\n`);
		}

		return new vscode.Hover(md);
	}

	/**
	 * Creates hover content for a directive.
	 */
	private createDirectiveHover(info: DirectiveInfo): vscode.Hover {
		const md = new vscode.MarkdownString();
		md.appendMarkdown(`## ${info.name}\n\n`);
		md.appendMarkdown(`${info.description}\n\n`);
		md.appendMarkdown(`**Syntax:** \`${info.syntax}\`\n\n`);

		if (info.example) {
			md.appendMarkdown(`**Example:**\n\`\`\`pasm\n${info.example}\n\`\`\`\n`);
		}

		return new vscode.Hover(md);
	}

	/**
	 * Finds and creates hover content for a symbol in the given document.
	 */
	private findSymbolInDocument(document: vscode.TextDocument, symbolName: string): { hover: vscode.Hover; file: string } | undefined {
		const text = document.getText();
		const lines = text.split('\n');
		const fileName = vscode.workspace.asRelativePath(document.uri);
		const escapedName = symbolName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

		for (let i = 0; i < lines.length; i++) {
			const line = lines[i];

			// Check for label definition
			const labelMatch = line.match(new RegExp(`^(${escapedName}):`, 'i'));
			if (labelMatch) {
				const md = new vscode.MarkdownString();
				md.appendMarkdown(`## Label: ${symbolName}\n\n`);
				md.appendMarkdown(`**File:** ${fileName}\n\n`);
				md.appendMarkdown(`**Line:** ${i + 1}\n`);
				return { hover: new vscode.Hover(md), file: fileName };
			}

			// Check for constant definition
			const constMatch = line.match(new RegExp(`^(${escapedName})\\s*(?:=|\\.equ)\\s*(.+)`, 'i'));
			if (constMatch) {
				const value = constMatch[2].split(';')[0].trim();
				const md = new vscode.MarkdownString();
				md.appendMarkdown(`## Constant: ${symbolName}\n\n`);
				md.appendMarkdown(`**Value:** \`${value}\`\n\n`);
				md.appendMarkdown(`**File:** ${fileName}\n\n`);
				md.appendMarkdown(`**Line:** ${i + 1}\n`);
				return { hover: new vscode.Hover(md), file: fileName };
			}

			// Check for macro definition
			const macroMatch = line.match(new RegExp(`^\\.macro\\s+(${escapedName})(.*)`, 'i'));
			if (macroMatch) {
				const params = macroMatch[2].trim();
				const md = new vscode.MarkdownString();
				md.appendMarkdown(`## Macro: ${symbolName}\n\n`);
				if (params) {
					md.appendMarkdown(`**Parameters:** ${params}\n\n`);
				}

				md.appendMarkdown(`**File:** ${fileName}\n\n`);
				md.appendMarkdown(`**Line:** ${i + 1}\n`);
				return { hover: new vscode.Hover(md), file: fileName };
			}
		}

		return undefined;
	}

	/**
	 * Finds symbol hover in current document first, then searches workspace.
	 */
	private findSymbolHover(document: vscode.TextDocument, symbolName: string): vscode.Hover | undefined {
		// Search current document first
		const localResult = this.findSymbolInDocument(document, symbolName);
		if (localResult) {
			return localResult.hover;
		}

		return undefined;
	}

	/**
	 * Searches workspace for a symbol hover asynchronously.
	 */
	private async findWorkspaceSymbolHover(document: vscode.TextDocument, symbolName: string): Promise<vscode.Hover | undefined> {
		const files = await vscode.workspace.findFiles('**/*.{pasm,inc}', '**/node_modules/**', 500);

		for (const file of files) {
			if (file.toString() === document.uri.toString()) {
				continue; // Already searched current document
			}

			try {
				const doc = await vscode.workspace.openTextDocument(file);
				const result = this.findSymbolInDocument(doc, symbolName);
				if (result) {
					return result.hover;
				}
			} catch {
				continue;
			}
		}

		return undefined;
	}
}

