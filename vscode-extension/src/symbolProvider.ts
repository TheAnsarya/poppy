// ============================================================================
// symbolProvider.ts - Poppy Assembly Symbol Provider
// Provides symbol information for go-to-definition, hover, and completion
// ============================================================================

import * as vscode from 'vscode';

/**
 * Represents a symbol found in Poppy Assembly code.
 */
interface PoppySymbol {
	name: string;
	kind: SymbolKind;
	location: vscode.Location;
	value?: string;
	documentation?: string;
}

/**
 * Types of symbols in Poppy Assembly.
 */
enum SymbolKind {
	Label,
	LocalLabel,
	Constant,
	Macro
}

/**
 * Provides symbol-related features for Poppy Assembly files.
 * Supports go-to-definition, peek definition, and symbol caching.
 */
export class PoppySymbolProvider implements vscode.DefinitionProvider, vscode.DocumentSymbolProvider {
	private symbolCache: Map<string, PoppySymbol[]> = new Map();

	/**
	 * Provides the definition of a symbol at the given position.
	 */
	async provideDefinition(
		document: vscode.TextDocument,
		position: vscode.Position,
		_token: vscode.CancellationToken
	): Promise<vscode.Definition | undefined> {
		const wordRange = document.getWordRangeAtPosition(position, /[a-zA-Z_@.][a-zA-Z0-9_]*/);
		if (!wordRange) {
			return undefined;
		}

		const word = document.getText(wordRange);

		// Search for the symbol definition in this document and included files
		const definition = await this.findDefinition(word, document);
		return definition;
	}

	/**
	 * Provides document symbols for the outline view.
	 */
	async provideDocumentSymbols(
		document: vscode.TextDocument,
		_token: vscode.CancellationToken
	): Promise<vscode.DocumentSymbol[]> {
		const symbols = await this.parseDocumentSymbols(document);
		return symbols.map(sym => this.toDocumentSymbol(sym, document));
	}

	/**
	 * Finds the definition of a symbol.
	 */
	private async findDefinition(
		symbolName: string,
		document: vscode.TextDocument
	): Promise<vscode.Location | undefined> {
		// Get symbols from current document
		const symbols = await this.getDocumentSymbols(document);

		// Look for exact match
		let symbol = symbols.find(s => s.name === symbolName);

		// For local labels, try with the current scope
		if (!symbol && symbolName.startsWith('.')) {
			// Local labels are scoped to the previous non-local label
			// Try to find it in the current context
			symbol = symbols.find(s => s.name.endsWith(symbolName) && s.kind === SymbolKind.LocalLabel);
		}

		// If not found in current document, search workspace
		if (!symbol) {
			symbol = await this.searchWorkspaceForSymbol(symbolName, document);
		}

		return symbol?.location;
	}

	/**
	 * Gets symbols from a document (with caching).
	 */
	private async getDocumentSymbols(document: vscode.TextDocument): Promise<PoppySymbol[]> {
		const key = document.uri.toString();
		const cached = this.symbolCache.get(key);

		// Return cached if document hasn't changed
		if (cached) {
			return cached;
		}

		const symbols = await this.parseDocumentSymbols(document);
		this.symbolCache.set(key, symbols);
		return symbols;
	}

	/**
	 * Parses a document to find all symbols.
	 */
	private async parseDocumentSymbols(document: vscode.TextDocument): Promise<PoppySymbol[]> {
		const symbols: PoppySymbol[] = [];
		const text = document.getText();
		const lines = text.split('\n');
		let currentScope = '';

		for (let i = 0; i < lines.length; i++) {
			const line = lines[i];
			const trimmed = line.trim();

			// Skip empty lines and full-line comments
			if (!trimmed || trimmed.startsWith(';')) {
				continue;
			}

			// Remove end-of-line comments for parsing
			const codeOnly = line.split(';')[0];

			// Check for labels (ends with colon or starts at column 0)
			const labelMatch = codeOnly.match(/^([a-zA-Z_][a-zA-Z0-9_]*):?\s*$/);
			if (labelMatch) {
				const name = labelMatch[1];
				const col = line.indexOf(name);
				const location = new vscode.Location(
					document.uri,
					new vscode.Position(i, col)
				);

				symbols.push({
					name,
					kind: SymbolKind.Label,
					location
				});

				currentScope = name;
				continue;
			}

			// Check for local labels (.label or @label)
			const localLabelMatch = codeOnly.match(/^([.@][a-zA-Z_][a-zA-Z0-9_]*):?\s*$/);
			if (localLabelMatch) {
				const name = localLabelMatch[1];
				const fullName = currentScope ? `${currentScope}${name}` : name;
				const col = line.indexOf(name);
				const location = new vscode.Location(
					document.uri,
					new vscode.Position(i, col)
				);

				symbols.push({
					name: fullName,
					kind: SymbolKind.LocalLabel,
					location
				});
				continue;
			}

			// Check for label with instruction on same line
			const labelWithCodeMatch = codeOnly.match(/^([a-zA-Z_][a-zA-Z0-9_]*):\s+\S/);
			if (labelWithCodeMatch) {
				const name = labelWithCodeMatch[1];
				const col = line.indexOf(name);
				const location = new vscode.Location(
					document.uri,
					new vscode.Position(i, col)
				);

				symbols.push({
					name,
					kind: SymbolKind.Label,
					location
				});

				currentScope = name;
			}

			// Check for constant definitions (NAME = value or NAME .equ value)
			const constMatch = codeOnly.match(/^([a-zA-Z_][a-zA-Z0-9_]*)\s*(?:=|\.equ)\s*(.+)/i);
			if (constMatch) {
				const name = constMatch[1];
				const value = constMatch[2].trim();
				const col = line.indexOf(name);
				const location = new vscode.Location(
					document.uri,
					new vscode.Position(i, col)
				);

				symbols.push({
					name,
					kind: SymbolKind.Constant,
					location,
					value
				});
				continue;
			}

			// Check for macro definitions (.macro name)
			const macroMatch = codeOnly.match(/^\s*\.macro\s+([a-zA-Z_][a-zA-Z0-9_]*)/i);
			if (macroMatch) {
				const name = macroMatch[1];
				const col = line.indexOf(name);
				const location = new vscode.Location(
					document.uri,
					new vscode.Position(i, col)
				);

				// Try to extract parameter list for documentation
				const paramsMatch = codeOnly.match(/\.macro\s+\w+\s+(.+)/i);
				const params = paramsMatch ? paramsMatch[1].trim() : '';

				symbols.push({
					name,
					kind: SymbolKind.Macro,
					location,
					documentation: params ? `Parameters: ${params}` : undefined
				});
				continue;
			}
		}

		return symbols;
	}

	/**
	 * Searches the workspace for a symbol definition.
	 */
	private async searchWorkspaceForSymbol(
		symbolName: string,
		_currentDocument: vscode.TextDocument
	): Promise<PoppySymbol | undefined> {
		// Search all .pasm and .inc files in the workspace
		const files = await vscode.workspace.findFiles('**/*.{pasm,inc}', '**/node_modules/**', 50);

		for (const file of files) {
			try {
				const document = await vscode.workspace.openTextDocument(file);
				const symbols = await this.getDocumentSymbols(document);
				const symbol = symbols.find(s => s.name === symbolName);
				if (symbol) {
					return symbol;
				}
			} catch {
				// Skip files that can't be opened
				continue;
			}
		}

		return undefined;
	}

	/**
	 * Converts a PoppySymbol to a VS Code DocumentSymbol.
	 */
	private toDocumentSymbol(symbol: PoppySymbol, document: vscode.TextDocument): vscode.DocumentSymbol {
		const vsKind = this.toVsCodeSymbolKind(symbol.kind);
		const line = symbol.location.range.start.line;
		const lineText = document.lineAt(line);
		const range = lineText.range;

		return new vscode.DocumentSymbol(
			symbol.name,
			symbol.value || '',
			vsKind,
			range,
			symbol.location.range
		);
	}

	/**
	 * Converts PoppySymbol kind to VS Code SymbolKind.
	 */
	private toVsCodeSymbolKind(kind: SymbolKind): vscode.SymbolKind {
		switch (kind) {
			case SymbolKind.Label:
				return vscode.SymbolKind.Function;
			case SymbolKind.LocalLabel:
				return vscode.SymbolKind.Method;
			case SymbolKind.Constant:
				return vscode.SymbolKind.Constant;
			case SymbolKind.Macro:
				return vscode.SymbolKind.Module;
		}
	}

	/**
	 * Invalidates the cache for a document.
	 */
	invalidateCache(uri: vscode.Uri): void {
		this.symbolCache.delete(uri.toString());
	}

	/**
	 * Clears the entire symbol cache.
	 */
	clearCache(): void {
		this.symbolCache.clear();
	}
}

