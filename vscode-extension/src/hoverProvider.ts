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
	]);

	/**
	 * Provides hover information for a position in a document.
	 */
	provideHover(
		document: vscode.TextDocument,
		position: vscode.Position,
		_token: vscode.CancellationToken
	): vscode.ProviderResult<vscode.Hover> {
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

		// Check for symbol definition in document
		const symbolHover = this.findSymbolHover(document, word);
		if (symbolHover) {
			return symbolHover;
		}

		return undefined;
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
	 * Finds and creates hover content for a symbol in the document.
	 */
	private findSymbolHover(document: vscode.TextDocument, symbolName: string): vscode.Hover | undefined {
		const text = document.getText();
		const lines = text.split('\n');

		for (let i = 0; i < lines.length; i++) {
			const line = lines[i];

			// Check for label definition
			const labelMatch = line.match(new RegExp(`^(${symbolName}):`, 'i'));
			if (labelMatch) {
				const md = new vscode.MarkdownString();
				md.appendMarkdown(`## Label: ${symbolName}\n\n`);
				md.appendMarkdown(`Defined at line ${i + 1}\n`);
				return new vscode.Hover(md);
			}

			// Check for constant definition
			const constMatch = line.match(new RegExp(`^(${symbolName})\\s*(?:=|\\.equ)\\s*(.+)`, 'i'));
			if (constMatch) {
				const value = constMatch[2].split(';')[0].trim();
				const md = new vscode.MarkdownString();
				md.appendMarkdown(`## Constant: ${symbolName}\n\n`);
				md.appendMarkdown(`**Value:** \`${value}\`\n\n`);
				md.appendMarkdown(`Defined at line ${i + 1}\n`);
				return new vscode.Hover(md);
			}

			// Check for macro definition
			const macroMatch = line.match(new RegExp(`^\\.macro\\s+(${symbolName})(.*)`, 'i'));
			if (macroMatch) {
				const params = macroMatch[2].trim();
				const md = new vscode.MarkdownString();
				md.appendMarkdown(`## Macro: ${symbolName}\n\n`);
				if (params) {
					md.appendMarkdown(`**Parameters:** ${params}\n\n`);
				}

				md.appendMarkdown(`Defined at line ${i + 1}\n`);
				return new vscode.Hover(md);
			}
		}

		return undefined;
	}
}

