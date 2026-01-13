// ============================================================================
// extension.ts - Poppy Assembly VS Code Extension Entry Point
// ============================================================================

import * as vscode from 'vscode';
import { PoppyTaskProvider, createOutputChannel } from './taskProvider';

// Output channel for Poppy build output
let outputChannel: vscode.OutputChannel;

/**
 * Extension activation - called when the extension is first activated.
 */
export function activate(context: vscode.ExtensionContext) {
	console.log('Poppy Assembly extension is now active');

	// Create output channel for build output
	outputChannel = createOutputChannel();
	context.subscriptions.push(outputChannel);

	// Register the task provider
	const taskProvider = new PoppyTaskProvider(outputChannel);
	const taskProviderDisposable = vscode.tasks.registerTaskProvider(
		PoppyTaskProvider.TaskType,
		taskProvider
	);
	context.subscriptions.push(taskProviderDisposable);

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

