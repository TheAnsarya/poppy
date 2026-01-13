// ============================================================================
// diagnostics.ts - Poppy Assembly Diagnostics Provider
// Provides real-time error and warning diagnostics in VS Code
// ============================================================================

import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { spawn } from 'child_process';

/**
 * Manages diagnostics for Poppy Assembly files.
 * Provides real-time syntax and semantic error highlighting.
 */
export class PoppyDiagnosticsProvider {
	private diagnosticCollection: vscode.DiagnosticCollection;
	private outputChannel: vscode.OutputChannel;
	private debounceTimers: Map<string, NodeJS.Timeout> = new Map();

	constructor(outputChannel: vscode.OutputChannel) {
		this.diagnosticCollection = vscode.languages.createDiagnosticCollection('poppy');
		this.outputChannel = outputChannel;
	}

	/**
	 * Gets the diagnostic collection for disposal.
	 */
	get collection(): vscode.DiagnosticCollection {
		return this.diagnosticCollection;
	}

	/**
	 * Validates a document and updates diagnostics.
	 * @param document The document to validate
	 */
	async validateDocument(document: vscode.TextDocument): Promise<void> {
		// Only validate Poppy Assembly files
		if (document.languageId !== 'pasm') {
			return;
		}

		// Debounce validation to avoid excessive calls while typing
		const key = document.uri.toString();
		const existingTimer = this.debounceTimers.get(key);
		if (existingTimer) {
			clearTimeout(existingTimer);
		}

		const timer = setTimeout(() => {
			this.debounceTimers.delete(key);
			this.doValidate(document);
		}, 500); // 500ms debounce

		this.debounceTimers.set(key, timer);
	}

	/**
	 * Performs the actual validation.
	 */
	private async doValidate(document: vscode.TextDocument): Promise<void> {
		const config = vscode.workspace.getConfiguration('poppy.diagnostics');
		const enabled = config.get<boolean>('enabled', true);

		if (!enabled) {
			this.diagnosticCollection.delete(document.uri);
			return;
		}

		const diagnostics: vscode.Diagnostic[] = [];

		// Perform quick syntax validation locally
		const syntaxDiagnostics = this.quickSyntaxCheck(document);
		diagnostics.push(...syntaxDiagnostics);

		// Try to use the compiler for more comprehensive checking
		const compilerDiagnostics = await this.compilerCheck(document);
		if (compilerDiagnostics !== null) {
			// Replace syntax diagnostics with compiler diagnostics if available
			diagnostics.length = 0;
			diagnostics.push(...compilerDiagnostics);
		}

		this.diagnosticCollection.set(document.uri, diagnostics);
	}

	/**
	 * Performs quick local syntax checking without the compiler.
	 */
	private quickSyntaxCheck(document: vscode.TextDocument): vscode.Diagnostic[] {
		const diagnostics: vscode.Diagnostic[] = [];
		const text = document.getText();
		const lines = text.split('\n');

		for (let i = 0; i < lines.length; i++) {
			const line = lines[i];
			const trimmed = line.trim();

			// Skip empty lines and comments
			if (!trimmed || trimmed.startsWith(';')) {
				continue;
			}

			// Check for common syntax issues

			// Unterminated string
			const stringMatch = line.match(/^[^;]*"([^"]*$)/);
			if (stringMatch) {
				const startCol = line.indexOf('"');
				const range = new vscode.Range(i, startCol, i, line.length);
				diagnostics.push(new vscode.Diagnostic(
					range,
					'Unterminated string literal',
					vscode.DiagnosticSeverity.Error
				));
			}

			// Invalid hex number (non-hex characters after $)
			const hexMatch = line.match(/\$([^0-9a-fA-F\s,;)}\]]+)/);
			if (hexMatch) {
				const startCol = line.indexOf('$' + hexMatch[1]);
				const range = new vscode.Range(i, startCol, i, startCol + hexMatch[0].length);
				diagnostics.push(new vscode.Diagnostic(
					range,
					`Invalid hex number: ${hexMatch[0]}`,
					vscode.DiagnosticSeverity.Error
				));
			}

			// Invalid binary number (non-binary characters after %)
			const binMatch = line.match(/%([^01\s,;)}\]]+)/);
			if (binMatch) {
				const startCol = line.indexOf('%' + binMatch[1]);
				const range = new vscode.Range(i, startCol, i, startCol + binMatch[0].length);
				diagnostics.push(new vscode.Diagnostic(
					range,
					`Invalid binary number: ${binMatch[0]}`,
					vscode.DiagnosticSeverity.Error
				));
			}

			// Directive without argument (basic check)
			const directiveMatch = trimmed.match(/^\.(\w+)\s*$/);
			if (directiveMatch) {
				const directive = directiveMatch[1].toLowerCase();
				const requiresArg = ['org', 'byte', 'word', 'include', 'incbin', 'segment', 'bank', 'db', 'dw'];
				if (requiresArg.includes(directive)) {
					const startCol = line.indexOf('.');
					const range = new vscode.Range(i, startCol, i, line.length);
					diagnostics.push(new vscode.Diagnostic(
						range,
						`Directive .${directive} requires an argument`,
						vscode.DiagnosticSeverity.Error
					));
				}
			}

			// Mismatched parentheses
			const openParens = (line.match(/\(/g) || []).length;
			const closeParens = (line.match(/\)/g) || []).length;
			if (openParens !== closeParens) {
				const range = new vscode.Range(i, 0, i, line.length);
				diagnostics.push(new vscode.Diagnostic(
					range,
					'Mismatched parentheses',
					vscode.DiagnosticSeverity.Warning
				));
			}
		}

		return diagnostics;
	}

	/**
	 * Uses the Poppy compiler for comprehensive checking.
	 */
	private async compilerCheck(document: vscode.TextDocument): Promise<vscode.Diagnostic[] | null> {
		const config = vscode.workspace.getConfiguration('poppy.compiler');
		const compilerPath = config.get<string>('path');

		// If no compiler path configured, skip compiler checking
		if (!compilerPath || !fs.existsSync(compilerPath)) {
			return null;
		}

		const target = config.get<string>('target') || 'nes';

		return new Promise((resolve) => {
			const diagnostics: vscode.Diagnostic[] = [];

			// Run compiler in check mode (--check flag for syntax only)
			const args = ['--check', '--target', target, document.fileName];
			const process = spawn(compilerPath, args, {
				cwd: path.dirname(document.fileName)
			});

			let stderr = '';
			process.stderr.on('data', (data) => {
				stderr += data.toString();
			});

			process.on('close', (code) => {
				if (code !== 0 && stderr) {
					// Parse compiler error output
					const errorLines = stderr.split('\n');
					for (const errorLine of errorLines) {
						const diagnostic = this.parseCompilerError(errorLine, document);
						if (diagnostic) {
							diagnostics.push(diagnostic);
						}
					}
				}

				resolve(diagnostics);
			});

			process.on('error', () => {
				// Compiler not found or failed to run
				resolve(null);
			});

			// Timeout after 5 seconds
			setTimeout(() => {
				process.kill();
				resolve(null);
			}, 5000);
		});
	}

	/**
	 * Parses a compiler error line into a diagnostic.
	 * Expected format: file(line,col): error: message
	 * Or: file:line:col: error: message
	 */
	private parseCompilerError(line: string, document: vscode.TextDocument): vscode.Diagnostic | null {
		// Try format: file(line,col): error|warning: message
		let match = line.match(/^(.+?)\((\d+)(?:,(\d+))?\):\s*(error|warning):\s*(.+)$/);

		// Try format: file:line:col: error|warning: message
		if (!match) {
			match = line.match(/^(.+?):(\d+):(\d+):\s*(error|warning):\s*(.+)$/);
		}

		// Try format: file:line: error|warning: message
		if (!match) {
			match = line.match(/^(.+?):(\d+):\s*(error|warning):\s*(.+)$/);
			if (match) {
				// Add column as match[3] and shift severity/message
				match = [match[0], match[1], match[2], '1', match[3], match[4]];
			}
		}

		if (!match) {
			return null;
		}

		const lineNum = parseInt(match[2], 10) - 1; // Convert to 0-based
		const colNum = parseInt(match[3] || '1', 10) - 1;
		const severity = match[4] === 'error'
			? vscode.DiagnosticSeverity.Error
			: vscode.DiagnosticSeverity.Warning;
		const message = match[5];

		// Get the range for the error
		let range: vscode.Range;
		if (lineNum >= 0 && lineNum < document.lineCount) {
			const docLine = document.lineAt(lineNum);
			// Highlight from the column to end of meaningful content
			const startCol = Math.min(colNum, docLine.text.length);
			const endCol = docLine.text.trimEnd().length || startCol + 1;
			range = new vscode.Range(lineNum, startCol, lineNum, Math.max(startCol + 1, endCol));
		} else {
			range = new vscode.Range(0, 0, 0, 1);
		}

		const diagnostic = new vscode.Diagnostic(range, message, severity);
		diagnostic.source = 'Poppy';
		return diagnostic;
	}

	/**
	 * Clears diagnostics for a document.
	 */
	clearDiagnostics(uri: vscode.Uri): void {
		this.diagnosticCollection.delete(uri);
		const timer = this.debounceTimers.get(uri.toString());
		if (timer) {
			clearTimeout(timer);
			this.debounceTimers.delete(uri.toString());
		}
	}

	/**
	 * Clears all diagnostics.
	 */
	clearAllDiagnostics(): void {
		this.diagnosticCollection.clear();
		for (const timer of this.debounceTimers.values()) {
			clearTimeout(timer);
		}

		this.debounceTimers.clear();
	}

	/**
	 * Disposes of resources.
	 */
	dispose(): void {
		this.clearAllDiagnostics();
		this.diagnosticCollection.dispose();
	}
}

