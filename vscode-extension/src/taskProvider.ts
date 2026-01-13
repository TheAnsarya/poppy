// ============================================================================
// taskProvider.ts - Poppy Build Task Provider
// ============================================================================

import * as vscode from 'vscode';
import * as path from 'path';

/**
 * Poppy task definition interface.
 */
interface PoppyTaskDefinition extends vscode.TaskDefinition {
	/** The task type: 'build' or 'build-project' */
	task: string;
	/** The file to build (for 'build' task) */
	file?: string;
	/** Path to poppy.json (for 'build-project' task) */
	projectFile?: string;
	/** Target architecture: 'nes', 'snes', or 'gb' */
	target?: string;
	/** Output file path */
	output?: string;
}

/**
 * Task provider for Poppy build tasks.
 * Provides auto-detected tasks and resolves custom task definitions.
 */
export class PoppyTaskProvider implements vscode.TaskProvider {
	static readonly TaskType = 'poppy';
	private outputChannel: vscode.OutputChannel;

	constructor(outputChannel: vscode.OutputChannel) {
		this.outputChannel = outputChannel;
	}

	/**
	 * Provides default tasks when the user runs "Tasks: Run Task" without a tasks.json.
	 * Auto-detects poppy.json files and .pasm files in the workspace.
	 */
	async provideTasks(): Promise<vscode.Task[]> {
		const tasks: vscode.Task[] = [];
		const workspaceFolders = vscode.workspace.workspaceFolders;

		if (!workspaceFolders) {
			return tasks;
		}

		for (const folder of workspaceFolders) {
			// Look for poppy.json project files
			const projectFiles = await vscode.workspace.findFiles(
				new vscode.RelativePattern(folder, '**/poppy.json'),
				'**/node_modules/**'
			);

			for (const projectFile of projectFiles) {
				const relativePath = path.relative(folder.uri.fsPath, projectFile.fsPath);
				const taskName = `Build Project: ${relativePath}`;

				const taskDef: PoppyTaskDefinition = {
					type: PoppyTaskProvider.TaskType,
					task: 'build-project',
					projectFile: relativePath
				};

				const task = this.createBuildTask(taskDef, taskName, folder);
				tasks.push(task);
			}
		}

		return tasks;
	}

	/**
	 * Resolves a task definition from tasks.json into an executable task.
	 * @param task The task to resolve
	 */
	resolveTask(task: vscode.Task): vscode.Task | undefined {
		const definition = task.definition as PoppyTaskDefinition;

		if (definition.type !== PoppyTaskProvider.TaskType) {
			return undefined;
		}

		// Resolve the task with the proper execution
		return this.createBuildTask(
			definition,
			task.name,
			task.scope as vscode.WorkspaceFolder
		);
	}

	/**
	 * Creates a build task with the proper shell execution and problem matcher.
	 * @param definition The task definition
	 * @param name The task name
	 * @param folder The workspace folder
	 */
	private createBuildTask(
		definition: PoppyTaskDefinition,
		name: string,
		folder: vscode.WorkspaceFolder
	): vscode.Task {
		const config = vscode.workspace.getConfiguration('poppy.compiler');
		const compilerPath = config.get<string>('path') || 'poppy';
		const defaultTarget = config.get<string>('target') || 'nes';

		let args: string[] = [];

		if (definition.task === 'build-project') {
			// Build from project file
			args.push('--project', definition.projectFile || 'poppy.json');
		} else if (definition.task === 'build') {
			// Build single file
			const file = definition.file || '${file}';
			const target = definition.target || defaultTarget;
			args.push('--target', target);
			args.push(file);

			if (definition.output) {
				args.push('--output', definition.output);
			}
		}

		const shellExecution = new vscode.ShellExecution(compilerPath, args, {
			cwd: folder.uri.fsPath
		});

		const task = new vscode.Task(
			definition,
			folder,
			name,
			'Poppy',
			shellExecution,
			'$poppy' // Use the poppy problem matcher
		);

		task.group = vscode.TaskGroup.Build;
		task.presentationOptions = {
			reveal: vscode.TaskRevealKind.Always,
			panel: vscode.TaskPanelKind.Shared,
			clear: true
		};

		return task;
	}
}

/**
 * Creates and registers the Poppy output channel.
 * Used for logging build output and diagnostics.
 */
export function createOutputChannel(): vscode.OutputChannel {
	return vscode.window.createOutputChannel('Poppy');
}

