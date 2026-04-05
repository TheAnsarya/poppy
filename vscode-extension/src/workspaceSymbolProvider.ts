// ============================================================================
// workspaceSymbolProvider.ts - Poppy Assembly Workspace Symbol Provider
// Enables Ctrl+T / Cmd+T workspace-wide symbol search
// ============================================================================

import * as vscode from 'vscode';

/**
 * Provides workspace-wide symbol search (Ctrl+T / Cmd+T).
 * Searches labels, constants, and macros across all .pasm/.inc files.
 */
export class PoppyWorkspaceSymbolProvider implements vscode.WorkspaceSymbolProvider {
	/**
	 * Searches workspace symbols matching the query string.
	 */
	async provideWorkspaceSymbols(
		query: string,
		token: vscode.CancellationToken
	): Promise<vscode.SymbolInformation[]> {
		if (!query || query.length < 1) {
			return [];
		}

		const symbols: vscode.SymbolInformation[] = [];
		const queryLower = query.toLowerCase();

		const files = await vscode.workspace.findFiles('**/*.{pasm,inc}', '**/node_modules/**', 500);

		for (const file of files) {
			if (token.isCancellationRequested) {
				break;
			}

			try {
				const doc = await vscode.workspace.openTextDocument(file);
				const fileSymbols = this.parseSymbols(doc, queryLower);
				symbols.push(...fileSymbols);

				// Limit results to avoid performance issues
				if (symbols.length > 200) {
					break;
				}
			} catch {
				continue;
			}
		}

		return symbols;
	}

	/**
	 * Parses symbols from a document that match the query.
	 */
	private parseSymbols(document: vscode.TextDocument, query: string): vscode.SymbolInformation[] {
		const symbols: vscode.SymbolInformation[] = [];
		const text = document.getText();
		const lines = text.split('\n');

		for (let i = 0; i < lines.length; i++) {
			const line = lines[i];
			const trimmed = line.trim();

			if (!trimmed || trimmed.startsWith(';')) {
				continue;
			}

			const codeOnly = line.split(';')[0];

			// Labels
			const labelMatch = codeOnly.match(/^([a-zA-Z_][a-zA-Z0-9_]*):?\s*$/);
			if (labelMatch) {
				const name = labelMatch[1];
				if (name.toLowerCase().includes(query)) {
					symbols.push(new vscode.SymbolInformation(
						name,
						vscode.SymbolKind.Function,
						'',
						new vscode.Location(document.uri, new vscode.Position(i, 0))
					));
				}
				continue;
			}

			// Label with code on same line
			const labelWithCodeMatch = codeOnly.match(/^([a-zA-Z_][a-zA-Z0-9_]*):\s+\S/);
			if (labelWithCodeMatch) {
				const name = labelWithCodeMatch[1];
				if (name.toLowerCase().includes(query)) {
					symbols.push(new vscode.SymbolInformation(
						name,
						vscode.SymbolKind.Function,
						'',
						new vscode.Location(document.uri, new vscode.Position(i, 0))
					));
				}
			}

			// Constants
			const constMatch = codeOnly.match(/^([a-zA-Z_][a-zA-Z0-9_]*)\s*(?:=|\.equ)\s*(.+)/i);
			if (constMatch) {
				const name = constMatch[1];
				if (name.toLowerCase().includes(query)) {
					symbols.push(new vscode.SymbolInformation(
						name,
						vscode.SymbolKind.Constant,
						constMatch[2].trim(),
						new vscode.Location(document.uri, new vscode.Position(i, 0))
					));
				}
				continue;
			}

			// Macros
			const macroMatch = codeOnly.match(/^\s*\.macro\s+([a-zA-Z_][a-zA-Z0-9_]*)/i);
			if (macroMatch) {
				const name = macroMatch[1];
				if (name.toLowerCase().includes(query)) {
					symbols.push(new vscode.SymbolInformation(
						name,
						vscode.SymbolKind.Function,
						'macro',
						new vscode.Location(document.uri, new vscode.Position(i, 0))
					));
				}
				continue;
			}
		}

		return symbols;
	}
}
