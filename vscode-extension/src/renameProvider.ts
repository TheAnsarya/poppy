// ============================================================================
// renameProvider.ts - Poppy Assembly Rename Provider
// Provides symbol rename across workspace .pasm and .inc files
// ============================================================================

import * as vscode from 'vscode';

/**
 * Provides symbol rename functionality for Poppy Assembly files.
 * Renames labels, constants, and macros across all workspace files.
 */
export class PoppyRenameProvider implements vscode.RenameProvider {
	/**
	 * Performs the rename operation.
	 */
	async provideRenameEdits(
		document: vscode.TextDocument,
		position: vscode.Position,
		newName: string,
		token: vscode.CancellationToken
	): Promise<vscode.WorkspaceEdit | undefined> {
		const wordRange = document.getWordRangeAtPosition(position, /[a-zA-Z_@.][a-zA-Z0-9_]*/);
		if (!wordRange) {
			return undefined;
		}

		const oldName = document.getText(wordRange);
		if (!oldName || oldName === newName) {
			return undefined;
		}

		// Validate new name
		if (!/^[a-zA-Z_@.][a-zA-Z0-9_]*$/.test(newName)) {
			throw new Error('Invalid symbol name. Must start with a letter, underscore, @, or . and contain only letters, digits, and underscores.');
		}

		const edit = new vscode.WorkspaceEdit();

		// Search current document first
		const currentReplacements = this.findAllOccurrences(document, oldName);
		for (const range of currentReplacements) {
			edit.replace(document.uri, range, newName);
		}

		// Search all workspace files
		const files = await vscode.workspace.findFiles('**/*.{pasm,inc}', '**/node_modules/**', 500);

		for (const file of files) {
			if (token.isCancellationRequested) {
				break;
			}

			// Skip current document (already searched)
			if (file.toString() === document.uri.toString()) {
				continue;
			}

			try {
				const doc = await vscode.workspace.openTextDocument(file);
				const replacements = this.findAllOccurrences(doc, oldName);
				for (const range of replacements) {
					edit.replace(doc.uri, range, newName);
				}
			} catch {
				continue;
			}
		}

		return edit;
	}

	/**
	 * Validates that the position is on a renameable symbol.
	 */
	prepareRename(
		document: vscode.TextDocument,
		position: vscode.Position,
		_token: vscode.CancellationToken
	): vscode.Range | { range: vscode.Range; placeholder: string } {
		const wordRange = document.getWordRangeAtPosition(position, /[a-zA-Z_@.][a-zA-Z0-9_]*/);
		if (!wordRange) {
			throw new Error('No symbol found at cursor position.');
		}

		const word = document.getText(wordRange);

		// Don't allow renaming opcodes or directives
		const opcodes = ['lda', 'ldx', 'ldy', 'sta', 'stx', 'sty', 'adc', 'sbc', 'and', 'ora',
			'eor', 'cmp', 'cpx', 'cpy', 'inc', 'dec', 'inx', 'dex', 'iny', 'dey', 'asl',
			'lsr', 'rol', 'ror', 'jmp', 'jsr', 'rts', 'rti', 'bcc', 'bcs', 'beq', 'bne',
			'bmi', 'bpl', 'bvc', 'bvs', 'clc', 'sec', 'cli', 'sei', 'clv', 'cld', 'sed',
			'pha', 'pla', 'php', 'plp', 'nop', 'brk', 'tax', 'txa', 'tay', 'tya', 'tsx', 'txs',
			'ld', 'push', 'pop', 'call', 'ret', 'halt', 'di', 'ei', 'jr', 'jp'];

		if (opcodes.includes(word.toLowerCase())) {
			throw new Error('Cannot rename CPU instructions.');
		}

		if (word.startsWith('.') && ['org', 'byte', 'word', 'db', 'dw', 'include', 'incbin',
			'macro', 'endmacro', 'segment', 'bank', 'if', 'else', 'endif', 'define',
			'ines', 'snes', 'gb', 'gba', 'genesis'].includes(word.substring(1).toLowerCase())) {
			throw new Error('Cannot rename directives.');
		}

		return { range: wordRange, placeholder: word };
	}

	/**
	 * Finds all occurrences of a symbol in a document (both definitions and references).
	 */
	private findAllOccurrences(document: vscode.TextDocument, symbolName: string): vscode.Range[] {
		const ranges: vscode.Range[] = [];
		const text = document.getText();
		const lines = text.split('\n');

		const escaped = symbolName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
		const pattern = new RegExp(`(?<![a-zA-Z0-9_])${escaped}(?![a-zA-Z0-9_])`, 'g');

		for (let i = 0; i < lines.length; i++) {
			const line = lines[i];

			// Strip comments
			const commentIdx = line.indexOf(';');
			const codePart = commentIdx >= 0 ? line.substring(0, commentIdx) : line;

			if (!codePart.includes(symbolName)) {
				continue;
			}

			let match;
			pattern.lastIndex = 0;
			while ((match = pattern.exec(codePart)) !== null) {
				ranges.push(new vscode.Range(i, match.index, i, match.index + symbolName.length));
			}
		}

		return ranges;
	}
}
