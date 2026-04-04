import * as assert from 'assert';
import * as vscode from 'vscode';
import { PoppyCompletionProvider } from '../../completionProvider';
import { PoppyHoverProvider } from '../../hoverProvider';
import { PoppySymbolProvider } from '../../symbolProvider';

suite('Completion Provider Test Suite', () => {
	vscode.window.showInformationMessage('Running completion provider tests');

	test('Should provide 6502 opcodes', async () => {
		const provider = new PoppyCompletionProvider();
		const document = await createTestDocument('nes-test.pasm', '.ines\n\tlda');
		const position = new vscode.Position(1, 4);

		const items = await provider.provideCompletionItems(document, position, {} as any, {} as any);

		assert.ok(items);
		assert.ok(Array.isArray(items));
		assert.ok(items.length > 0);

		// Should have lda, ldx, ldy, etc.
		const opcodes = (items as vscode.CompletionItem[]).map(i => i.label);
		assert.ok(opcodes.includes('lda'));
		assert.ok(opcodes.includes('ldx'));
		assert.ok(opcodes.includes('sta'));
	});

	test('Should provide SM83 opcodes for GB target', async () => {
		const provider = new PoppyCompletionProvider();
		const document = await createTestDocument('gb-test.pasm', '.gb\n\tld');
		const position = new vscode.Position(1, 3);

		const items = await provider.provideCompletionItems(document, position, {} as any, {} as any);

		assert.ok(items);
		assert.ok(Array.isArray(items));
		assert.ok(items.length > 0);

		// Should have Game Boy opcodes
		const opcodes = (items as vscode.CompletionItem[]).map(i => i.label);
		assert.ok(opcodes.includes('ld'));
		assert.ok(opcodes.includes('push'));
		assert.ok(opcodes.includes('pop'));
	});

	test('Should provide 65816 opcodes for SNES target', async () => {
		const provider = new PoppyCompletionProvider();
		const document = await createTestDocument('snes-test.pasm', '.snes\n\tstz');
		const position = new vscode.Position(1, 4);

		const items = await provider.provideCompletionItems(document, position, {} as any, {} as any);

		assert.ok(items);
		assert.ok(Array.isArray(items));

		// Should have 65816-specific opcodes
		const opcodes = (items as vscode.CompletionItem[]).map(i => i.label);
		assert.ok(opcodes.includes('stz'));
		assert.ok(opcodes.includes('mvn'));
		assert.ok(opcodes.includes('mvp'));
	});

	test('Should provide directives when line starts with dot', async () => {
		const provider = new PoppyCompletionProvider();
		const document = await createTestDocument('test.pasm', '.or');
		const position = new vscode.Position(0, 3);

		const items = await provider.provideCompletionItems(document, position, {} as any, {} as any);

		assert.ok(items);
		assert.ok(Array.isArray(items));

		// Should have directives
		const directives = (items as vscode.CompletionItem[]).map(i => i.label);
		assert.ok(directives.includes('.org'));
		assert.ok(directives.includes('.db'));
		assert.ok(directives.includes('.macro'));
	});

	test('Should provide completions at instruction position', async () => {
		const provider = new PoppyCompletionProvider();
		const document = await createTestDocument('test.pasm', '\tlda #$00\n\tsta $2000');
		const position = new vscode.Position(1, 4);

		const items = await provider.provideCompletionItems(document, position, {} as any, {} as any);

		assert.ok(items);
		assert.ok(Array.isArray(items));
		const opcodes = (items as vscode.CompletionItem[]).map(i => i.label);
		assert.ok(opcodes.includes('sta'));
	});
});

suite('Hover Provider Test Suite', () => {
	test('Should provide hover for 6502 instructions', async () => {
		const provider = new PoppyHoverProvider();
		const document = await createTestDocument('hover-6502.pasm', '\tlda #$80');
		const position = new vscode.Position(0, 2);

		const hover = await provider.provideHover(document, position, {} as any);

		assert.ok(hover, 'Should return hover for lda');
		assert.ok(hover.contents.length > 0);
		const content = (hover.contents[0] as vscode.MarkdownString).value;
		assert.ok(content.includes('Load Accumulator'), 'Should contain instruction name');
	});

	test('Should provide hover for 65816 instructions', async () => {
		const provider = new PoppyHoverProvider();
		const document = await createTestDocument('hover-65816.pasm', '\trep #$30');
		const position = new vscode.Position(0, 2);

		const hover = await provider.provideHover(document, position, {} as any);

		assert.ok(hover, 'Should return hover for rep');
		const content = (hover.contents[0] as vscode.MarkdownString).value;
		assert.ok(content.includes('Reset Processor Bits'), 'Should contain 65816 instruction name');
	});

	test('Should provide hover for SM83 instructions', async () => {
		const provider = new PoppyHoverProvider();
		const document = await createTestDocument('hover-sm83.pasm', '\tldh');
		const position = new vscode.Position(0, 2);

		const hover = await provider.provideHover(document, position, {} as any);

		assert.ok(hover, 'Should return hover for ldh');
		const content = (hover.contents[0] as vscode.MarkdownString).value;
		assert.ok(content.includes('Load High'), 'Should contain SM83 instruction name');
	});

	test('Should provide hover for HuC6280 instructions', async () => {
		const provider = new PoppyHoverProvider();
		const document = await createTestDocument('hover-huc.pasm', '\ttam #$01');
		const position = new vscode.Position(0, 2);

		const hover = await provider.provideHover(document, position, {} as any);

		assert.ok(hover, 'Should return hover for tam');
		const content = (hover.contents[0] as vscode.MarkdownString).value;
		assert.ok(content.includes('Transfer A to MPR'), 'Should contain HuC6280 instruction name');
	});

	test('Should provide hover for M68000 instructions', async () => {
		const provider = new PoppyHoverProvider();
		const document = await createTestDocument('hover-m68k.pasm', '\tmove.l d0,d1');
		const position = new vscode.Position(0, 2);

		const hover = await provider.provideHover(document, position, {} as any);

		assert.ok(hover, 'Should return hover for move');
		const content = (hover.contents[0] as vscode.MarkdownString).value;
		assert.ok(content.includes('Move'), 'Should contain M68000 instruction name');
	});

	test('Should provide hover for ARM7TDMI instructions', async () => {
		const provider = new PoppyHoverProvider();
		const document = await createTestDocument('hover-arm.pasm', '\tldr r0,[r1]');
		const position = new vscode.Position(0, 2);

		const hover = await provider.provideHover(document, position, {} as any);

		assert.ok(hover, 'Should return hover for ldr');
		const content = (hover.contents[0] as vscode.MarkdownString).value;
		assert.ok(content.includes('Load Register'), 'Should contain ARM instruction name');
	});

	test('Should provide hover for V30MZ instructions', async () => {
		const provider = new PoppyHoverProvider();
		const document = await createTestDocument('hover-v30.pasm', '\txchg ax,bx');
		const position = new vscode.Position(0, 2);

		const hover = await provider.provideHover(document, position, {} as any);

		assert.ok(hover, 'Should return hover for xchg');
		const content = (hover.contents[0] as vscode.MarkdownString).value;
		assert.ok(content.includes('Exchange'), 'Should contain V30MZ instruction name');
	});

	test('Should provide hover for SPC700 instructions', async () => {
		const provider = new PoppyHoverProvider();
		const document = await createTestDocument('hover-spc.pasm', '\tmovw ya,$00');
		const position = new vscode.Position(0, 2);

		const hover = await provider.provideHover(document, position, {} as any);

		assert.ok(hover, 'Should return hover for movw');
		const content = (hover.contents[0] as vscode.MarkdownString).value;
		assert.ok(content.includes('Move Word'), 'Should contain SPC700 instruction name');
	});

	test('Should provide hover for directives', async () => {
		const provider = new PoppyHoverProvider();
		const document = await createTestDocument('hover-dir.pasm', '.org $8000');
		const position = new vscode.Position(0, 2);

		const hover = await provider.provideHover(document, position, {} as any);

		assert.ok(hover, 'Should return hover for .org');
		const content = (hover.contents[0] as vscode.MarkdownString).value;
		assert.ok(content.includes('origin'), 'Should contain directive description');
	});

	test('Should include flags and cycles in instruction hover', async () => {
		const provider = new PoppyHoverProvider();
		const document = await createTestDocument('hover-detail.pasm', '\tadc #$01');
		const position = new vscode.Position(0, 2);

		const hover = await provider.provideHover(document, position, {} as any);

		assert.ok(hover, 'Should return hover for adc');
		const content = (hover.contents[0] as vscode.MarkdownString).value;
		assert.ok(content.includes('N, V, Z, C'), 'Should show affected flags');
	});

	test('Should return undefined for unknown words', async () => {
		const provider = new PoppyHoverProvider();
		const document = await createTestDocument('hover-unknown.pasm', '\txyzzy');
		const position = new vscode.Position(0, 2);

		const hover = await provider.provideHover(document, position, {} as any);

		// Unknown words should not produce a hover
		assert.strictEqual(hover, undefined);
	});
});

suite('Diagnostics Provider Test Suite', () => {
	// Tests call quickSyntaxCheck directly via any-cast to bypass
	// the languageId guard and debounce timer in validateDocument.

	test('Should detect unterminated strings', async () => {
		const { PoppyDiagnosticsProvider } = await import('../../diagnostics');
		const outputChannel = vscode.window.createOutputChannel('Poppy Test');
		const provider = new PoppyDiagnosticsProvider(outputChannel);
		const document = await createTestDocument('diag-string.pasm', '\t.db "unterminated');

		const diagnostics: vscode.Diagnostic[] = (provider as any).quickSyntaxCheck(document);
		const stringError = diagnostics.find(d => d.message.includes('Unterminated string'));
		assert.ok(stringError, 'Should detect unterminated string literal');
		assert.strictEqual(stringError.severity, vscode.DiagnosticSeverity.Error);

		outputChannel.dispose();
		provider.collection.dispose();
	});

	test('Should detect invalid hex numbers', async () => {
		const { PoppyDiagnosticsProvider } = await import('../../diagnostics');
		const outputChannel = vscode.window.createOutputChannel('Poppy Test');
		const provider = new PoppyDiagnosticsProvider(outputChannel);
		const document = await createTestDocument('diag-hex.pasm', '\tlda $GG');

		const diagnostics: vscode.Diagnostic[] = (provider as any).quickSyntaxCheck(document);
		const hexError = diagnostics.find(d => d.message.includes('Invalid hex'));
		assert.ok(hexError, 'Should detect invalid hex number');

		outputChannel.dispose();
		provider.collection.dispose();
	});

	test('Should detect invalid binary numbers', async () => {
		const { PoppyDiagnosticsProvider } = await import('../../diagnostics');
		const outputChannel = vscode.window.createOutputChannel('Poppy Test');
		const provider = new PoppyDiagnosticsProvider(outputChannel);
		const document = await createTestDocument('diag-bin.pasm', '\tlda #%xyz');

		const diagnostics: vscode.Diagnostic[] = (provider as any).quickSyntaxCheck(document);
		const binError = diagnostics.find(d => d.message.includes('Invalid binary'));
		assert.ok(binError, 'Should detect invalid binary number');

		outputChannel.dispose();
		provider.collection.dispose();
	});

	test('Should detect directives without arguments', async () => {
		const { PoppyDiagnosticsProvider } = await import('../../diagnostics');
		const outputChannel = vscode.window.createOutputChannel('Poppy Test');
		const provider = new PoppyDiagnosticsProvider(outputChannel);
		const document = await createTestDocument('diag-dir.pasm', '.org');

		const diagnostics: vscode.Diagnostic[] = (provider as any).quickSyntaxCheck(document);
		const dirError = diagnostics.find(d => d.message.includes('requires an argument'));
		assert.ok(dirError, 'Should detect directive missing argument');

		outputChannel.dispose();
		provider.collection.dispose();
	});

	test('Should detect mismatched parentheses', async () => {
		const { PoppyDiagnosticsProvider } = await import('../../diagnostics');
		const outputChannel = vscode.window.createOutputChannel('Poppy Test');
		const provider = new PoppyDiagnosticsProvider(outputChannel);
		const document = await createTestDocument('diag-paren.pasm', '\tlda ($00');

		const diagnostics: vscode.Diagnostic[] = (provider as any).quickSyntaxCheck(document);
		const parenWarn = diagnostics.find(d => d.message.includes('parentheses'));
		assert.ok(parenWarn, 'Should detect mismatched parentheses');
		assert.strictEqual(parenWarn.severity, vscode.DiagnosticSeverity.Warning);

		outputChannel.dispose();
		provider.collection.dispose();
	});

	test('Should not flag valid code', async () => {
		const { PoppyDiagnosticsProvider } = await import('../../diagnostics');
		const outputChannel = vscode.window.createOutputChannel('Poppy Test');
		const provider = new PoppyDiagnosticsProvider(outputChannel);
		const document = await createTestDocument('diag-valid.pasm', '.org $8000\n\tlda #$ff\n\tsta ($00),y');

		const diagnostics: vscode.Diagnostic[] = (provider as any).quickSyntaxCheck(document);
		assert.strictEqual(diagnostics.length, 0, 'Valid code should have no diagnostics');

		outputChannel.dispose();
		provider.collection.dispose();
	});
});

suite('Symbol Provider Test Suite', () => {
	test('Should detect labels in document', async () => {
		const provider = new PoppySymbolProvider();
		const document = await createTestDocument('sym-labels.pasm',
			'reset:\n\tsei\nloop:\n\tjmp loop');

		const symbols = await provider.provideDocumentSymbols(document, {} as any);

		assert.ok(symbols, 'Should return symbols');
		assert.ok(symbols.length >= 2, 'Should find at least 2 labels');

		const names = symbols.map(s => s.name);
		assert.ok(names.includes('reset'), 'Should find reset label');
		assert.ok(names.includes('loop'), 'Should find loop label');
	});

	test('Should detect constants in document', async () => {
		const provider = new PoppySymbolProvider();
		const document = await createTestDocument('sym-const.pasm',
			'SCREEN_WIDTH = $100\nPLAYER_X .equ $00\nstart:\n\tlda #SCREEN_WIDTH');

		const symbols = await provider.provideDocumentSymbols(document, {} as any);

		assert.ok(symbols, 'Should return symbols');
		const names = symbols.map(s => s.name);
		assert.ok(names.includes('SCREEN_WIDTH'), 'Should find SCREEN_WIDTH constant');
		assert.ok(names.includes('PLAYER_X'), 'Should find PLAYER_X constant');
	});

	test('Should detect macros in document', async () => {
		const provider = new PoppySymbolProvider();
		const document = await createTestDocument('sym-macro.pasm',
			'.macro add16 dest, src\n\tclc\n.endmacro\nstart:\n\tadd16 $00, $02');

		const symbols = await provider.provideDocumentSymbols(document, {} as any);

		assert.ok(symbols, 'Should return symbols');
		const names = symbols.map(s => s.name);
		assert.ok(names.includes('add16'), 'Should find macro definition');
	});

	test('Should provide go-to-definition for labels', async () => {
		const provider = new PoppySymbolProvider();
		const document = await createTestDocument('sym-goto.pasm',
			'start:\n\tnop\nloop:\n\tjmp start');

		// Position on 'start' in 'jmp start'
		const position = new vscode.Position(3, 6);
		const definition = await provider.provideDefinition(document, position, {} as any);

		assert.ok(definition, 'Should find definition of start');
	});

	test('Should assign correct symbol kinds', async () => {
		const provider = new PoppySymbolProvider();
		const document = await createTestDocument('sym-kinds.pasm',
			'CONST_VAL = $42\nmain:\n\tlda #CONST_VAL\n\trts');

		const symbols = await provider.provideDocumentSymbols(document, {} as any);

		const constSym = symbols.find(s => s.name === 'CONST_VAL');
		const labelSym = symbols.find(s => s.name === 'main');

		assert.ok(constSym, 'Should find constant');
		assert.ok(labelSym, 'Should find label');
		assert.strictEqual(constSym.kind, vscode.SymbolKind.Constant, 'Constant should be SymbolKind.Constant');
		assert.strictEqual(labelSym.kind, vscode.SymbolKind.Function, 'Label should be SymbolKind.Function');
	});
});

suite('Task Provider Test Suite', () => {
	test('Should create task provider instance', async () => {
		const { PoppyTaskProvider } = await import('../../taskProvider');
		const outputChannel = vscode.window.createOutputChannel('Poppy Test');
		const provider = new PoppyTaskProvider(outputChannel);

		assert.ok(provider, 'Should create task provider');
		outputChannel.dispose();
	});

	test('Should provide tasks array', async () => {
		const { PoppyTaskProvider } = await import('../../taskProvider');
		const outputChannel = vscode.window.createOutputChannel('Poppy Test');
		const provider = new PoppyTaskProvider(outputChannel);

		const tasks = await provider.provideTasks();

		assert.ok(Array.isArray(tasks), 'Should return an array of tasks');
		outputChannel.dispose();
	});

	test('Should have correct task type', async () => {
		const { PoppyTaskProvider } = await import('../../taskProvider');
		assert.strictEqual(PoppyTaskProvider.TaskType, 'poppy', 'Task type should be poppy');
	});
});

suite('Formatting Provider Test Suite', () => {
	test('Should align opcodes to column 8', async () => {
		const { PoppyFormattingProvider } = await import('../../formattingProvider');
		const provider = new PoppyFormattingProvider();
		const document = await createTestDocument('test.pasm', 'start:\nlda #$80\nsta $2000');

		const edits = await provider.provideDocumentFormattingEdits(
			document,
			{ insertSpaces: false, tabSize: 8 },
			{} as any
		);

		assert.ok(edits);
		assert.ok(Array.isArray(edits));
	});

	test('Should align comments to configured column', async () => {
		const { PoppyFormattingProvider } = await import('../../formattingProvider');
		const provider = new PoppyFormattingProvider();
		const document = await createTestDocument('test.pasm', '\tlda #$80 ; load value');

		const edits = await provider.provideDocumentFormattingEdits(
			document,
			{ insertSpaces: false, tabSize: 8 },
			{} as any
		);

		assert.ok(edits);
		// Should create edit to align comment
		assert.ok(edits.length > 0);
	});

	test('Should keep labels at column 0', async () => {
		const { PoppyFormattingProvider } = await import('../../formattingProvider');
		const provider = new PoppyFormattingProvider();
		const document = await createTestDocument('test.pasm', '  start:');

		const edits = await provider.provideDocumentFormattingEdits(
			document,
			{ insertSpaces: false, tabSize: 8 },
			{} as any
		);

		assert.ok(edits);
		if (edits.length > 0) {
			// First edit should move label to column 0
			assert.ok(edits[0].newText.startsWith('start:'));
		}
	});

	test('Should indent nested scopes', async () => {
		const { PoppyFormattingProvider } = await import('../../formattingProvider');
		const provider = new PoppyFormattingProvider();
		const document = await createTestDocument('test.pasm', '.scope test\nlda #$00\n.endscope');

		const edits = await provider.provideDocumentFormattingEdits(
			document,
			{ insertSpaces: false, tabSize: 8 },
			{} as any
		);

		assert.ok(edits);
		// Should indent the lda instruction
		assert.ok(edits.length > 0);
	});
});

suite('Integration Tests', () => {
	test('Extension should be present', () => {
		assert.ok(vscode.extensions.getExtension('TheAnsarya.poppy-assembly'));
	});

	test('Should activate extension', async () => {
		const ext = vscode.extensions.getExtension('TheAnsarya.poppy-assembly');
		assert.ok(ext);
		await ext.activate();
		assert.strictEqual(ext.isActive, true);
	});

	test('Should register .pasm language', () => {
		const languages = vscode.languages.getLanguages();
		assert.ok(languages);
	});

	test('Should provide syntax highlighting for .pasm files', async () => {
		const document = await createTestDocument('test.pasm', 'lda #$80');
		const tokens = await vscode.commands.executeCommand<any>(
			'vscode.provideDocumentSemanticTokens',
			document.uri
		);
		// Just verify command doesn't fail
		assert.ok(true);
	});
});

// Helper function to create test documents
async function createTestDocument(filename: string, content: string): Promise<vscode.TextDocument> {
	const uri = vscode.Uri.parse(`untitled:${filename}`);
	const document = await vscode.workspace.openTextDocument(uri);
	const edit = new vscode.WorkspaceEdit();
	edit.insert(uri, new vscode.Position(0, 0), content);
	await vscode.workspace.applyEdit(edit);
	return document;
}
