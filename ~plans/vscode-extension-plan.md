# 🎨 VS Code Extension - Implementation Plan

**Epic:** #47 - VS Code Extension  
**Created:** 2026-01-13 05:57 EST  
**Status:** In Progress

---

## 📊 Current Status

### ✅ Already Implemented
- [x] Extension structure and package.json
- [x] Language configuration (comments, brackets)
- [x] Basic TextMate grammar
- [x] Diagnostics provider (error highlighting)
- [x] Hover provider (opcode information)
- [x] Symbol provider (go-to-definition for labels)
- [x] Task provider (build tasks)
- [x] Problem matcher for compiler output

### 🎯 To Be Implemented

#### 1. Enhanced Syntax Highlighting
**Status:** Partial - needs expansion  
**Priority:** High  
**Files:** `syntaxes/pasm.tmLanguage.json`

Current grammar is basic. Needs:
- Target-specific opcode highlighting (6502, SM83, 65816)
- Directive keyword colors
- Macro definition/invocation highlighting
- String literal handling
- Numeric literal formats (hex, binary, decimal)
- Comment types (line, block)
- Label definitions vs references

#### 2. Code Snippets
**Status:** Not implemented  
**Priority:** Medium  
**Files:** New `snippets/pasm.json`

Need snippets for:
- Common macro patterns
- Directive blocks (`.ines`, `.snes`, `.gb`)
- Instruction sequences (wait vblank, DMA transfer)
- Header templates (NES, SNES, GB)
- Loop structures
- Subroutine templates

#### 3. Completion Provider
**Status:** Not implemented  
**Priority:** High  
**Files:** New `src/completionProvider.ts`

Features:
- Opcode completion (context-aware by target)
- Directive completion
- Label completion from current file
- Register name completion
- Addressing mode hints

#### 4. Formatting Provider
**Status:** Not implemented  
**Priority:** Low  
**Files:** New `src/formattingProvider.ts`

Features:
- Indent alignment
- Column alignment (opcode, operands, comments)
- Comment formatting

#### 5. Outline/Breadcrumbs
**Status:** Partial - symbols work  
**Priority:** Medium  
**Enhancement needed:**
- Show macro definitions in outline
- Show segment boundaries
- Show include file structure

#### 6. Extension Testing
**Status:** Minimal  
**Priority:** High  
**Files:** New `src/test/`

Need:
- Unit tests for providers
- Integration tests
- Test fixtures

---

## 📋 Sub-Issues to Create

### Issue #70: Enhanced TextMate Grammar
**Description:** Expand syntax highlighting to support all Poppy features  
**Tasks:**
- Add 6502 instruction keywords
- Add SM83 instruction keywords  
- Add 65816 instruction keywords
- Add all directive keywords
- Add macro syntax patterns
- Add label patterns
- Add numeric literal patterns (with $, %, 0x prefixes)
- Add string escape sequences

### Issue #71: Code Snippets Library
**Description:** Create comprehensive snippet collection  
**Tasks:**
- NES project template snippets
- SNES project template snippets
- GB project template snippets
- Common macro patterns (wait_vblank, ppu_addr, etc.)
- Directive blocks (iNES, SNES, GB headers)
- Loop and branch patterns
- DMA transfer patterns

### Issue #72: IntelliSense Completion Provider
**Description:** Implement context-aware code completion  
**Tasks:**
- Create completion provider class
- Parse opcode lists from compiler
- Implement opcode completion
- Implement directive completion
- Implement label completion
- Implement register completion
- Add documentation strings for completions
- Context-aware filtering by target architecture

### Issue #73: Document Formatting Provider
**Description:** Auto-format assembly code  
**Tasks:**
- Create formatting provider
- Implement column alignment
- Implement indentation rules
- Add configuration options
- Format on save option

### Issue #74: Extension Test Suite
**Description:** Comprehensive testing for extension  
**Tasks:**
- Set up test infrastructure
- Unit tests for diagnostics
- Unit tests for hover provider
- Unit tests for completion provider
- Integration tests
- CI/CD integration

---

## 🎯 Implementation Order

### Phase 1: Core Features (Current Sprint)
1. **Issue #70** - Enhanced syntax highlighting (2 hours)
2. **Issue #71** - Code snippets (1 hour)
3. **Issue #72** - Completion provider (3 hours)

### Phase 2: Quality of Life (Next Sprint)
4. **Issue #73** - Formatting provider (2 hours)
5. **Issue #74** - Test suite (3 hours)

### Phase 3: Polish & Release
6. README documentation
7. Extension icon and branding
8. Marketplace publishing
9. User guide and tutorials

---

## 📝 Technical Design

### Completion Provider Architecture

```typescript
class PoppyCompletionProvider implements vscode.CompletionItemProvider {
	private opcodes: Map<TargetArch, OpcodeInfo[]>;
	private directives: DirectiveInfo[];
	
	provideCompletionItems(
		document: vscode.TextDocument,
		position: vscode.Position,
		token: vscode.CancellationToken
	): vscode.ProviderResult<vscode.CompletionItem[]> {
		const target = this.detectTargetArchitecture(document);
		const linePrefix = document.lineAt(position).text.substr(0, position.character);
		
		// Check context: directive, opcode, or label
		if (linePrefix.trimStart().startsWith('.')) {
			return this.getDirectiveCompletions();
		} else {
			return this.getOpcodeCompletions(target);
		}
	}
}
```

### Snippet Structure

```json
{
	"Wait VBlank Macro": {
		"prefix": "wait_vblank",
		"body": [
			".macro wait_vblank",
			"-:",
			"\tbit ${1:PPU_STATUS}",
			"\tbpl -",
			".endmacro"
		],
		"description": "NES VBlank wait macro"
	}
}
```

---

## 🧪 Testing Strategy

### Unit Tests
- Test each provider in isolation
- Mock VS Code API
- Verify correct completions/hovers/etc.

### Integration Tests
- Test extension activation
- Test with real .pasm files
- Verify multi-file projects

### Manual Tests
- Test in real VS Code instance
- Test with example projects
- Performance testing with large files

---

## 📊 Success Metrics

- **Syntax Highlighting:** All language features have distinct colors
- **Completion:** >90% of common opcodes/directives auto-complete
- **Hover:** Documentation appears for all standard opcodes
- **Diagnostics:** Errors appear within 1 second of edit
- **Performance:** <100ms completion provider response time

---

## 🚀 Marketplace Preparation

### Required for Publishing
- [ ] Extension icon (128x128 PNG)
- [ ] Detailed README with screenshots
- [ ] LICENSE file
- [ ] CHANGELOG.md
- [ ] Version 1.0.0 tag
- [ ] Publisher account setup

### Marketing
- [ ] Demo GIF/video
- [ ] Feature highlights
- [ ] Installation instructions
- [ ] Comparison with other assembly extensions

---

**Next Steps:** Create sub-issues and begin implementation with #70 (syntax highlighting).
