// ============================================================================
// referenceProvider.ts - Poppy Assembly Reference Provider
// Provides Find All References and workspace-wide symbol search
// ============================================================================

import * as vscode from 'vscode';

/**
 * Provides Find All References for Poppy Assembly symbols.
 * Searches across all .pasm and .inc files in the workspace.
 */
export class PoppyReferenceProvider implements vscode.ReferenceProvider {
	/**
	 * Finds all references to the symbol at the given position.
	 */
	async provideReferences(
		document: vscode.TextDocument,
		position: vscode.Position,
		context: vscode.ReferenceContext,
		token: vscode.CancellationToken
	): Promise<vscode.Location[]> {
		const wordRange = document.getWordRangeAtPosition(position, /[a-zA-Z_@.][a-zA-Z0-9_]*/);
		if (!wordRange) {
			return [];
		}

		const word = document.getText(wordRange);
		if (!word) {
			return [];
		}

		const locations: vscode.Location[] = [];

		// Search the current document first
		const currentRefs = this.findReferencesInDocument(document, word, context.includeDeclaration);
		locations.push(...currentRefs);

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
				const refs = this.findReferencesInDocument(doc, word, context.includeDeclaration);
				locations.push(...refs);
			} catch {
				// Skip files that can't be opened
				continue;
			}
		}

		return locations;
	}

	/**
	 * Finds all references to a symbol in a single document.
	 */
	private findReferencesInDocument(
		document: vscode.TextDocument,
		symbolName: string,
		includeDeclaration: boolean
	): vscode.Location[] {
		const locations: vscode.Location[] = [];
		const text = document.getText();
		const lines = text.split('\n');

		// Build a word-boundary regex for the symbol name
		const escaped = symbolName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
		const pattern = new RegExp(`(?<![a-zA-Z0-9_])${escaped}(?![a-zA-Z0-9_])`, 'g');

		for (let i = 0; i < lines.length; i++) {
			const line = lines[i];

			// Strip comments for matching
			const commentIdx = line.indexOf(';');
			const codePart = commentIdx >= 0 ? line.substring(0, commentIdx) : line;

			// Skip if symbol not present in code portion
			if (!codePart.includes(symbolName)) {
				continue;
			}

			// Check if this is a definition line
			if (!includeDeclaration) {
				const trimmed = codePart.trim();
				// Definition patterns: "label:" or "NAME = value" or "NAME .equ value" or ".macro name"
				const isDefinition =
					new RegExp(`^${escaped}:?\\s*$`).test(trimmed) ||
					new RegExp(`^${escaped}:\\s+\\S`).test(trimmed) ||
					new RegExp(`^${escaped}\\s*(?:=|\\.equ)\\s`, 'i').test(trimmed) ||
					new RegExp(`^\\.macro\\s+${escaped}`, 'i').test(trimmed);

				if (isDefinition) {
					continue;
				}
			}

			// Find all occurrences in this line's code portion
			let match;
			pattern.lastIndex = 0;
			while ((match = pattern.exec(codePart)) !== null) {
				const col = match.index;
				const range = new vscode.Range(i, col, i, col + symbolName.length);
				locations.push(new vscode.Location(document.uri, range));
			}
		}

		return locations;
	}
}
