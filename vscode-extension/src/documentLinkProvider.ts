// ============================================================================
// documentLinkProvider.ts - Poppy Assembly Document Link Provider
// Makes .include and .incbin paths clickable in the editor
// ============================================================================

import * as vscode from 'vscode';
import * as path from 'path';

/**
 * Provides clickable links for .include and .incbin directives.
 */
export class PoppyDocumentLinkProvider implements vscode.DocumentLinkProvider {
	/**
	 * Finds all .include and .incbin directives and creates clickable links.
	 */
	provideDocumentLinks(
		document: vscode.TextDocument,
		_token: vscode.CancellationToken
	): vscode.DocumentLink[] {
		const links: vscode.DocumentLink[] = [];
		const text = document.getText();
		const lines = text.split('\n');

		// Pattern matches .include "path" and .incbin "path"
		const includePattern = /\.(include|incbin)\s+"([^"]+)"/gi;

		for (let i = 0; i < lines.length; i++) {
			const line = lines[i];

			// Skip comment-only lines
			const trimmed = line.trim();
			if (trimmed.startsWith(';')) {
				continue;
			}

			// Strip end-of-line comments
			const commentIdx = line.indexOf(';');
			const codePart = commentIdx >= 0 ? line.substring(0, commentIdx) : line;

			let match;
			includePattern.lastIndex = 0;
			while ((match = includePattern.exec(codePart)) !== null) {
				const filePath = match[2];

				// Calculate the range of the file path (inside quotes)
				const quoteStart = codePart.indexOf('"', match.index) + 1;
				const quoteEnd = quoteStart + filePath.length;

				const range = new vscode.Range(i, quoteStart, i, quoteEnd);

				// Resolve the path relative to the current document
				const docDir = path.dirname(document.uri.fsPath);
				const resolvedPath = path.resolve(docDir, filePath);
				const targetUri = vscode.Uri.file(resolvedPath);

				const link = new vscode.DocumentLink(range, targetUri);
				link.tooltip = `Open ${filePath}`;
				links.push(link);
			}
		}

		return links;
	}
}
