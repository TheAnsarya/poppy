// ============================================================================
// extension.ts - Poppy Assembly VS Code Extension Entry Point
// ============================================================================

import * as vscode from 'vscode';
import { PoppyTaskProvider, createOutputChannel } from './taskProvider';
import { PoppyDiagnosticsProvider } from './diagnostics';
import { PoppySymbolProvider } from './symbolProvider';
import { PoppyHoverProvider } from './hoverProvider';
import { PoppyCompletionProvider } from './completionProvider';
import { PoppyFormattingProvider } from './formattingProvider';

// Document selector for Poppy Assembly files
const PASM_SELECTOR: vscode.DocumentSelector = { language: 'pasm', scheme: 'file' };

// Output channel for Poppy build output
let outputChannel: vscode.OutputChannel;

// Diagnostics provider for real-time error checking
let diagnosticsProvider: PoppyDiagnosticsProvider;

// Symbol provider for go-to-definition
let symbolProvider: PoppySymbolProvider;

/**
 * Extension activation - called when the extension is first activated.
 */
export function activate(context: vscode.ExtensionContext) {
	console.log('Poppy Assembly extension is now active');

	// Create output channel for build output
	outputChannel = createOutputChannel();
	context.subscriptions.push(outputChannel);

	// Create diagnostics provider
	diagnosticsProvider = new PoppyDiagnosticsProvider(outputChannel);
	context.subscriptions.push(diagnosticsProvider.collection);

	// Create symbol provider for go-to-definition
	symbolProvider = new PoppySymbolProvider();
	context.subscriptions.push(
		vscode.languages.registerDefinitionProvider(PASM_SELECTOR, symbolProvider)
	);
	context.subscriptions.push(
		vscode.languages.registerDocumentSymbolProvider(PASM_SELECTOR, symbolProvider)
	);

	// Create hover provider
	const hoverProvider = new PoppyHoverProvider();
	context.subscriptions.push(
		vscode.languages.registerHoverProvider(PASM_SELECTOR, hoverProvider)
	);

	// Create completion provider
	const completionProvider = new PoppyCompletionProvider();
	context.subscriptions.push(
		vscode.languages.registerCompletionItemProvider(
			PASM_SELECTOR,
			completionProvider,
			'.', // Trigger on dot for directives
			'#', // Trigger on # for immediate mode
			'$'  // Trigger on $ for hex values
		)
	);

	// Create formatting provider
	const formattingProvider = new PoppyFormattingProvider();
	context.subscriptions.push(
		vscode.languages.registerDocumentFormattingEditProvider(PASM_SELECTOR, formattingProvider)
	);

	// Register the task provider
	const taskProvider = new PoppyTaskProvider(outputChannel);
	const taskProviderDisposable = vscode.tasks.registerTaskProvider(
		PoppyTaskProvider.TaskType,
		taskProvider
	);
	context.subscriptions.push(taskProviderDisposable);

	// Register document change listeners for diagnostics
	context.subscriptions.push(
		vscode.workspace.onDidOpenTextDocument(doc => {
			diagnosticsProvider.validateDocument(doc);
		})
	);
	context.subscriptions.push(
		vscode.workspace.onDidChangeTextDocument(e => {
			diagnosticsProvider.validateDocument(e.document);
			symbolProvider.invalidateCache(e.document.uri);
		})
	);
	context.subscriptions.push(
		vscode.workspace.onDidSaveTextDocument(doc => {
			diagnosticsProvider.validateDocument(doc);
		})
	);
	context.subscriptions.push(
		vscode.workspace.onDidCloseTextDocument(doc => {
			diagnosticsProvider.clearDiagnostics(doc.uri);
			symbolProvider.invalidateCache(doc.uri);
		})
	);

	// Validate all open documents on activation
	vscode.workspace.textDocuments.forEach(doc => {
		if (doc.languageId === 'pasm') {
			diagnosticsProvider.validateDocument(doc);
		}
	});

	// Register build current file command
	const buildCommand = vscode.commands.registerCommand('poppy.build', async () => {
		const editor = vscode.window.activeTextEditor;
		if (!editor) {
			vscode.window.showWarningMessage('No file is currently open');
			return;
		}

		if (editor.document.languageId !== 'pasm') {
			vscode.window.showWarningMessage('Current file is not a Poppy Assembly file');
			return;
		}

		// Save the file before building
		await editor.document.save();

		// Run the build task
		const workspaceFolder = vscode.workspace.getWorkspaceFolder(editor.document.uri);
		if (!workspaceFolder) {
			vscode.window.showErrorMessage('File is not in a workspace folder');
			return;
		}

		const config = vscode.workspace.getConfiguration('poppy.compiler');
		const target = config.get<string>('target') || 'nes';

		const taskDef: vscode.TaskDefinition = {
			type: PoppyTaskProvider.TaskType,
			task: 'build',
			file: editor.document.fileName,
			target: target
		};

		const task = new vscode.Task(
			taskDef,
			workspaceFolder,
			'Build Current File',
			'Poppy',
			new vscode.ShellExecution(
				config.get<string>('path') || 'poppy',
				['--target', target, editor.document.fileName]
			),
			'$poppy'
		);

		await vscode.tasks.executeTask(task);
	});
	context.subscriptions.push(buildCommand);

	// Register build project command
	const buildProjectCommand = vscode.commands.registerCommand('poppy.buildProject', async () => {
		const workspaceFolders = vscode.workspace.workspaceFolders;
		if (!workspaceFolders || workspaceFolders.length === 0) {
			vscode.window.showErrorMessage('No workspace folder is open');
			return;
		}

		// Find poppy.json files in the workspace
		const projectFiles = await vscode.workspace.findFiles('**/poppy.json', '**/node_modules/**');

		if (projectFiles.length === 0) {
			vscode.window.showWarningMessage('No poppy.json project file found in workspace');
			return;
		}

		let projectFile: vscode.Uri;

		if (projectFiles.length === 1) {
			projectFile = projectFiles[0];
		} else {
			// Let user pick which project to build
			const items = projectFiles.map(f => ({
				label: vscode.workspace.asRelativePath(f),
				uri: f
			}));

			const selected = await vscode.window.showQuickPick(items, {
				placeHolder: 'Select a project file to build'
			});

			if (!selected) {
				return;
			}

			projectFile = selected.uri;
		}

		const workspaceFolder = vscode.workspace.getWorkspaceFolder(projectFile);
		if (!workspaceFolder) {
			vscode.window.showErrorMessage('Project file is not in a workspace folder');
			return;
		}

		const config = vscode.workspace.getConfiguration('poppy.compiler');
		const relativePath = vscode.workspace.asRelativePath(projectFile);

		const taskDef: vscode.TaskDefinition = {
			type: PoppyTaskProvider.TaskType,
			task: 'build-project',
			projectFile: relativePath
		};

		const task = new vscode.Task(
			taskDef,
			workspaceFolder,
			`Build Project: ${relativePath}`,
			'Poppy',
			new vscode.ShellExecution(
				config.get<string>('path') || 'poppy',
				['--project', projectFile.fsPath]
			),
			'$poppy'
		);

		await vscode.tasks.executeTask(task);
	});
	context.subscriptions.push(buildProjectCommand);

	outputChannel.appendLine('Poppy Assembly extension activated');
}

/**
 * Extension deactivation - called when the extension is deactivated.
 */
export function deactivate() {
	console.log('Poppy Assembly extension deactivated');
}

