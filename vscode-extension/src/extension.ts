// ============================================================================
// extension.ts - Poppy Assembly VS Code Extension Entry Point
// ============================================================================

import * as vscode from 'vscode';

/**
 * Extension activation - called when the extension is first activated.
 */
export function activate(context: vscode.ExtensionContext) {
	console.log('Poppy Assembly extension is now active');

	// Register commands (placeholder for future functionality)
	const buildCommand = vscode.commands.registerCommand('poppy.build', () => {
		const editor = vscode.window.activeTextEditor;
		if (editor && editor.document.languageId === 'pasm') {
			vscode.window.showInformationMessage('Building: ' + editor.document.fileName);
			// TODO: Implement actual build functionality
		}
	});

	context.subscriptions.push(buildCommand);
}

/**
 * Extension deactivation - called when the extension is deactivated.
 */
export function deactivate() {
	console.log('Poppy Assembly extension deactivated');
}
