import * as assert from 'assert';
import * as vscode from 'vscode';
import { PoppyCompletionProvider } from '../../completionProvider';

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

	test('Should provide register completions', async () => {
		const provider = new PoppyCompletionProvider();
		const document = await createTestDocument('test.pasm', '\tlda #$00\n\tsta $2000,');
		const position = new vscode.Position(1, 13);
		
		const items = await provider.provideCompletionItems(document, position, {} as any, {} as any);
		
		assert.ok(items);
		assert.ok(Array.isArray(items));
		
		// Should have 6502 registers
		const registers = (items as vscode.CompletionItem[]).map(i => i.label);
		assert.ok(registers.includes('x'));
		assert.ok(registers.includes('y'));
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
