import * as vscode from 'vscode';

// Opcode definitions for each architecture
const OPCODES_6502 = [
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

// Additional 65816 opcodes
const OPCODES_65816 = [
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

// Game Boy (SM83) opcodes
const OPCODES_SM83 = [
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

// Directive definitions
const DIRECTIVES = [
	{ directive: '.org', description: 'Set Origin', detail: 'Set program counter address' },
	{ directive: '.base', description: 'Set Base Address', detail: 'Set base address for labels' },
	{ directive: '.db', description: 'Define Byte', detail: 'Define 8-bit data' },
	{ directive: '.dw', description: 'Define Word', detail: 'Define 16-bit data' },
	{ directive: '.dl', description: 'Define Long', detail: 'Define 24-bit data (65816)' },
	{ directive: '.dd', description: 'Define Dword', detail: 'Define 32-bit data' },
	{ directive: '.ds', description: 'Define Space', detail: 'Reserve bytes' },
	{ directive: '.ascii', description: 'ASCII String', detail: 'Define ASCII string' },
	{ directive: '.asciiz', description: 'ASCII String Zero-Terminated', detail: 'Define null-terminated ASCII string' },
	{ directive: '.include', description: 'Include Source', detail: 'Include .pasm source file' },
	{ directive: '.incbin', description: 'Include Binary', detail: 'Include binary file' },
	{ directive: '.macro', description: 'Define Macro', detail: 'Begin macro definition' },
	{ directive: '.endmacro', description: 'End Macro', detail: 'End macro definition' },
	{ directive: '.if', description: 'If Conditional', detail: 'Conditional assembly' },
	{ directive: '.ifdef', description: 'If Defined', detail: 'Assemble if symbol defined' },
	{ directive: '.ifndef', description: 'If Not Defined', detail: 'Assemble if symbol not defined' },
	{ directive: '.else', description: 'Else Clause', detail: 'Else branch of conditional' },
	{ directive: '.endif', description: 'End If', detail: 'End conditional block' },
	{ directive: '.repeat', description: 'Repeat Block', detail: 'Repeat assembly block N times' },
	{ directive: '.endrep', description: 'End Repeat', detail: 'End repeat block' },
	{ directive: '.scope', description: 'Begin Scope', detail: 'Begin named scope' },
	{ directive: '.endscope', description: 'End Scope', detail: 'End scope' },
	{ directive: '.define', description: 'Define Symbol', detail: 'Define preprocessor symbol' },
	{ directive: '.undef', description: 'Undefine Symbol', detail: 'Undefine symbol' },
	{ directive: '.ines', description: 'iNES Header', detail: 'Enable iNES ROM header (NES)' },
	{ directive: '.snes', description: 'SNES Header', detail: 'Enable SNES ROM header' },
	{ directive: '.gb', description: 'Game Boy Header', detail: 'Enable Game Boy ROM header' },
	{ directive: '.ines_prg_banks', description: 'iNES PRG Banks', detail: 'Set number of 16KB PRG-ROM banks' },
	{ directive: '.ines_chr_banks', description: 'iNES CHR Banks', detail: 'Set number of 8KB CHR-ROM banks' },
	{ directive: '.ines_mirroring', description: 'iNES Mirroring', detail: 'Set name table mirroring (0=H, 1=V)' },
	{ directive: '.ines_mapper', description: 'iNES Mapper', detail: 'Set mapper number' },
	{ directive: '.snes_title', description: 'SNES Title', detail: 'Set ROM title (21 chars max)' },
	{ directive: '.snes_rom_size', description: 'SNES ROM Size', detail: 'Set ROM size in KB' },
	{ directive: '.gb_title', description: 'GB Title', detail: 'Set Game Boy title (16 chars max)' },
	{ directive: '.gb_cartridge_type', description: 'GB Cartridge Type', detail: 'Set cartridge type (MBC)' },
	{ directive: '.a8', description: 'A 8-bit Mode', detail: 'Set accumulator to 8-bit (65816)' },
	{ directive: '.a16', description: 'A 16-bit Mode', detail: 'Set accumulator to 16-bit (65816)' },
	{ directive: '.i8', description: 'Index 8-bit Mode', detail: 'Set index registers to 8-bit (65816)' },
	{ directive: '.i16', description: 'Index 16-bit Mode', detail: 'Set index registers to 16-bit (65816)' },
];

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
				const item = new vscode.CompletionItem(dir.directive, vscode.CompletionItemKind.Keyword);
				item.detail = dir.detail;
				item.documentation = new vscode.MarkdownString(dir.description);
				items.push(item);
			}
			return items;
		}

		// Opcode completion (at start of instruction, after label, or after whitespace)
		if (linePrefix.match(/(?:^\s*|:\s*|\t\s*)[\w]*$/)) {
			let opcodes: typeof OPCODES_6502 = [];
			
			if (target === 'gb') {
				opcodes = OPCODES_SM83;
			} else if (target === 'snes') {
				opcodes = OPCODES_65816;
			} else {
				// Default to 6502 (NES)
				opcodes = OPCODES_6502;
			}

			for (const op of opcodes) {
				const item = new vscode.CompletionItem(op.opcode, vscode.CompletionItemKind.Function);
				item.detail = op.detail;
				item.documentation = new vscode.MarkdownString(`**${op.description}**\n\n${op.detail}`);
				items.push(item);
			}
		}

		// Register completion (context-specific)
		if (linePrefix.match(/,\s*[\w]*$/) || linePrefix.match(/\[\s*[\w]*$/)) {
			if (target === 'gb') {
				const registers = ['a', 'b', 'c', 'd', 'e', 'h', 'l', 'af', 'bc', 'de', 'hl', 'sp', 'pc'];
				for (const reg of registers) {
					const item = new vscode.CompletionItem(reg, vscode.CompletionItemKind.Variable);
					item.detail = 'Register';
					items.push(item);
				}
			} else {
				const registers = ['a', 'x', 'y'];
				for (const reg of registers) {
					const item = new vscode.CompletionItem(reg, vscode.CompletionItemKind.Variable);
					item.detail = 'Register';
					items.push(item);
				}
			}
		}

		return items;
	}

	private detectTarget(content: string): 'nes' | 'snes' | 'gb' {
		if (content.includes('.gb') || content.includes('.gameboy')) {
			return 'gb';
		}
		if (content.includes('.snes')) {
			return 'snes';
		}
		return 'nes';
	}
}
