import * as vscode from 'vscode';

/**
 * Formatting provider for Poppy Assembly files.
 * Provides column-based alignment for clean, readable code.
 */
export class PoppyFormattingProvider implements vscode.DocumentFormattingEditProvider {
	/**
	 * Format the entire document with proper column alignment.
	 */
	provideDocumentFormattingEdits(
		document: vscode.TextDocument,
		options: vscode.FormattingOptions,
		token: vscode.CancellationToken
	): vscode.ProviderResult<vscode.TextEdit[]> {
		const edits: vscode.TextEdit[] = [];
		const useSpaces = options.insertSpaces;
		const tabSize = options.tabSize;

		// Get configuration for column positions
		const config = vscode.workspace.getConfiguration('poppy.formatting');
		const opcodeColumn = config.get<number>('opcodeColumn', 8);
		const operandColumn = config.get<number>('operandColumn', 16);
		const commentColumn = config.get<number>('commentColumn', 40);

		let indentLevel = 0;

		for (let i = 0; i < document.lineCount; i++) {
			const line = document.lineAt(i);
			const text = line.text;

			// Skip empty lines
			if (text.trim().length === 0) {
				continue;
			}

			// Parse the line
			const formatted = this.formatLine(text, indentLevel, {
				useSpaces,
				tabSize,
				opcodeColumn,
				operandColumn,
				commentColumn
			});

			// Update indent level for scope tracking
			indentLevel = this.updateIndentLevel(text, indentLevel);

			// Create edit if line needs formatting
			if (formatted !== text) {
				const range = new vscode.Range(
					line.range.start,
					line.range.end
				);
				edits.push(vscode.TextEdit.replace(range, formatted));
			}
		}

		return edits;
	}

	/**
	 * Format a single line with proper column alignment.
	 */
	private formatLine(
		text: string,
		indentLevel: number,
		options: {
			useSpaces: boolean;
			tabSize: number;
			opcodeColumn: number;
			operandColumn: number;
			commentColumn: number;
		}
	): string {
		// Extract components
		const match = text.match(/^(\s*)([^;\s]*)(\s*)([^;]*)(\s*)(;.*)?$/);
		if (!match) {
			return text;
		}

		const [, , first, , rest, , comment] = match;

		// Create indent string
		const indent = this.createIndent(indentLevel, options.useSpaces, options.tabSize);

		// Check if this is a label (ends with :)
		if (first.endsWith(':')) {
			// Labels go at column 0
			if (comment) {
				const commentPadding = this.padToColumn(first, options.commentColumn, options.useSpaces, options.tabSize);
				return `${first}${commentPadding}${comment.trimStart()}`;
			}
			return first;
		}

		// Check if this is a directive (starts with .)
		if (first.startsWith('.')) {
			// Directives use indent but no extra column alignment
			const restTrimmed = rest.trim();
			if (comment) {
				const combined = `${indent}${first}${restTrimmed ? ' ' + restTrimmed : ''}`;
				const commentPadding = this.padToColumn(combined, options.commentColumn, options.useSpaces, options.tabSize);
				return `${combined}${commentPadding}${comment.trimStart()}`;
			}
			return `${indent}${first}${restTrimmed ? ' ' + restTrimmed : ''}`;
		}

		// This is an instruction
		if (first.length > 0) {
			const opcode = first.toLowerCase();
			const operand = rest.trim();

			// Align opcode to opcodeColumn
			const opcodeText = `${indent}${opcode}`;
			
			if (operand.length > 0) {
				// Align operand to operandColumn
				const operandPadding = this.padToColumn(opcodeText, options.operandColumn, options.useSpaces, options.tabSize);
				const combined = `${opcodeText}${operandPadding}${operand}`;
				
				if (comment) {
					const commentPadding = this.padToColumn(combined, options.commentColumn, options.useSpaces, options.tabSize);
					return `${combined}${commentPadding}${comment.trimStart()}`;
				}
				return combined;
			} else {
				// No operand, just opcode
				if (comment) {
					const commentPadding = this.padToColumn(opcodeText, options.commentColumn, options.useSpaces, options.tabSize);
					return `${opcodeText}${commentPadding}${comment.trimStart()}`;
				}
				return opcodeText;
			}
		}

		// Comment-only line
		if (comment) {
			return `${indent}${comment.trimStart()}`;
		}

		return text;
	}

	/**
	 * Create indentation string based on level and settings.
	 */
	private createIndent(level: number, useSpaces: boolean, tabSize: number): string {
		if (level === 0) {
			return '';
		}
		if (useSpaces) {
			return ' '.repeat(level * tabSize);
		}
		return '\t'.repeat(level);
	}

	/**
	 * Pad from current text to target column.
	 */
	private padToColumn(text: string, targetColumn: number, useSpaces: boolean, tabSize: number): string {
		const currentLength = this.visualLength(text, tabSize);
		if (currentLength >= targetColumn) {
			return useSpaces ? ' ' : '\t';
		}
		
		const needed = targetColumn - currentLength;
		if (useSpaces) {
			return ' '.repeat(needed);
		}
		
		// Use tabs for alignment
		const tabs = Math.ceil(needed / tabSize);
		return '\t'.repeat(tabs);
	}

	/**
	 * Calculate visual length of text (tabs count as tabSize spaces).
	 */
	private visualLength(text: string, tabSize: number): number {
		let length = 0;
		for (const char of text) {
			if (char === '\t') {
				length += tabSize - (length % tabSize);
			} else {
				length += 1;
			}
		}
		return length;
	}

	/**
	 * Update indentation level based on scope directives.
	 */
	private updateIndentLevel(text: string, currentLevel: number): number {
		const trimmed = text.trim().toLowerCase();
		
		// Scope/block opening
		if (trimmed.startsWith('.scope') || 
			trimmed.startsWith('.macro') || 
			trimmed.startsWith('.repeat') ||
			trimmed.startsWith('.if') ||
			trimmed.startsWith('.ifdef') ||
			trimmed.startsWith('.ifndef')) {
			return currentLevel + 1;
		}
		
		// Scope/block closing
		if (trimmed.startsWith('.endscope') || 
			trimmed.startsWith('.endmacro') || 
			trimmed.startsWith('.endm') ||
			trimmed.startsWith('.endrep') ||
			trimmed.startsWith('.endif')) {
			return Math.max(0, currentLevel - 1);
		}
		
		// .else stays at current level but doesn't change it
		
		return currentLevel;
	}
}
