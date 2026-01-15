# 🌸 Poppy Assembly VS Code Extension - User Guide

**Version:** 1.0.0
**Publisher:** TheAnsarya

---

## 📥 Installation

### From VS Code Marketplace

1. Open VS Code
2. Press `Ctrl+Shift+X` (or `Cmd+Shift+X` on macOS)
3. Search for "Poppy Assembly"
4. Click **Install**

### From VSIX File

```bash
code --install-extension poppy-assembly-1.0.0.vsix
```

---

## 🚀 Getting Started

### 1. Create Your First Assembly File

Create a file with `.pasm` extension:

```asm
; hello.pasm - Simple NES program
.target nes

.org $8000
reset:
	sei
	cld
	ldx #$ff
	txs
	jmp reset

.org $fffa
	.word 0        ; NMI
	.word reset    ; RESET
	.word 0        ; IRQ
```

### 2. Enable Syntax Highlighting

Syntax highlighting activates automatically for `.pasm` files. The extension recognizes:

- All 6502, 65816, and SM83 instructions
- Assembler directives (`.org`, `.db`, `.macro`, etc.)
- Labels (global, local, anonymous)
- Comments and literals

### 3. Use Code Completion

Press `Ctrl+Space` to trigger IntelliSense:

- **Opcodes** - All instructions for current target architecture
- **Directives** - Assembler directives with documentation
- **Labels** - All defined labels in the current file
- **Registers** - Valid registers (a, x, y, sp, etc.)

**Example:**
```asm
.target nes
.org $8000
	ld█  ; Press Ctrl+Space → shows LDA, LDX, LDY options
```

### 4. Format Your Code

Press `Shift+Alt+F` (or `Shift+Option+F` on macOS) to format:

**Before:**
```asm
reset:
lda #$00
sta $2000
loop:
dex
bne loop
```

**After:**
```asm
reset:
	lda	#$00
	sta	$2000
loop:
	dex
	bne	loop
```

### 5. Build Your Project

#### Using Commands

1. Press `Ctrl+Shift+P` (Cmd+Shift+P on macOS)
2. Type "Poppy: Build Current File"
3. Errors appear in the Problems panel

#### Using Tasks

1. Press `Ctrl+Shift+B` (Cmd+Shift+B on macOS)
2. Select "Poppy: Build Current File" or "Poppy: Build Project"
3. View build output in the Terminal

**Prerequisites:** Poppy compiler must be installed and in PATH.

---

## 💡 Features

### Syntax Highlighting

The extension provides comprehensive syntax highlighting:

| Element | Color Scheme |
|---------|--------------|
| **Instructions** | Keywords (blue) |
| **Directives** | Keywords (purple) |
| **Labels** | Entity names (yellow) |
| **Comments** | Comments (green) |
| **Numbers** | Constants (light blue) |
| **Strings** | Strings (orange) |
| **Macro Parameters** | Variables (cyan) |

**Supported Instruction Sets:**
- 6502 (NES) - 56 opcodes
- 65816 (SNES) - 256 opcodes
- SM83 (Game Boy) - 512 opcodes (including CB-prefixed)

### IntelliSense Completion

Context-aware code completion with documentation:

#### Opcode Completion

```asm
.target nes
	l█  ; Shows: LDA, LDX, LDY, LSR with descriptions
```

**Details:**
- Architecture-specific (detects `.target nes/snes/gb`)
- Shows valid addressing modes
- Includes opcode description

#### Directive Completion

```asm
.█  ; Shows: .org, .db, .dw, .macro, .include, etc.
```

**Details:**
- All assembler directives
- Parameter hints
- Usage examples

#### Label Completion

```asm
reset:
	jmp r█  ; Shows: reset
```

**Details:**
- All labels in current file
- Includes global and local labels
- Shows label address (if available)

### Code Formatting

Professional column-based alignment:

**Configuration:**
```json
{
	"pasm.formatting.opcodeColumn": 8,
	"pasm.formatting.operandColumn": 16,
	"pasm.formatting.commentColumn": 40
}
```

**Features:**
- Labels aligned at column 0
- Opcodes aligned at column 8 (configurable)
- Operands aligned at column 16 (configurable)
- Comments aligned at column 40 (configurable)
- Smart indentation for nested scopes
- Preserves blank lines and structure

### Navigation

#### Go to Definition

Press `F12` on a label to jump to its definition:

```asm
reset:
	jsr init    ; F12 on 'init' → jumps to definition

init:
	lda #$00
	rts
```

#### Document Symbols

Press `Ctrl+Shift+O` (Cmd+Shift+O on macOS) to view outline:

- All labels listed
- Organized by section
- Quick navigation

#### Hover Information

Hover over an instruction to see documentation:

```asm
	lda #$00    ; Hover shows: "LDA - Load Accumulator"
```

**Includes:**
- Instruction description
- Supported addressing modes
- Cycle counts
- Flags affected

### Build Integration

#### Task Provider

The extension provides two build tasks:

1. **Poppy: Build Current File**
	- Compiles the currently open file
	- Keyboard shortcut: `Ctrl+Shift+B`

2. **Poppy: Build Project**
	- Compiles entire project (poppy.json)
	- Uses project configuration

#### Problem Matcher

Compiler errors appear in VS Code's Problems panel:

```
ERROR: Undefined symbol 'my_label'
  --> hello.pasm:10:5
```

**Features:**
- Click to jump to error location
- Inline error highlighting
- Real-time diagnostics

### Code Snippets

40+ snippets for common patterns. Type the trigger and press `Tab`:

#### NES Snippets

| Trigger | Description |
|---------|-------------|
| `nes-basic` | Basic NES program structure |
| `nes-header` | Complete iNES header |
| `nes-vectors` | Interrupt vectors |
| `wait-vblank` | VBlank wait routine |
| `ppu-addr` | Set PPU address |
| `dma-copy` | OAM DMA transfer |

#### SNES Snippets

| Trigger | Description |
|---------|-------------|
| `snes-basic` | Basic SNES program structure |
| `snes-header` | SNES internal header |
| `snes-vectors` | SNES interrupt vectors |
| `rep-sep` | REP/SEP register size control |

#### Game Boy Snippets

| Trigger | Description |
|---------|-------------|
| `gb-basic` | Basic Game Boy program |
| `gb-header` | Game Boy ROM header |
| `lcd-off` | Turn off LCD |
| `wait-vblank-gb` | VBlank wait (Game Boy) |

#### General Snippets

| Trigger | Description |
|---------|-------------|
| `macro` | Define a macro |
| `if` | Conditional assembly |
| `table` | Data table |
| `for` | For loop pattern |
| `while` | While loop pattern |

---

## ⚙️ Configuration

### Extension Settings

Access via `File > Preferences > Settings` → Search "Poppy"

#### `pasm.compilerPath`
Path to Poppy compiler executable.

**Default:** `"poppy"`  
**Example:** `"C:\\tools\\poppy.exe"`

#### `pasm.defaultTarget`
Default target architecture for new files.

**Options:** `"nes"`, `"snes"`, `"gameboy"`  
**Default:** `"nes"`

#### `pasm.diagnostics.enable`
Enable/disable real-time diagnostics.

**Default:** `true`

#### `pasm.formatting.opcodeColumn`
Column position for opcodes.

**Default:** `8`

#### `pasm.formatting.operandColumn`
Column position for operands.

**Default:** `16`

#### `pasm.formatting.commentColumn`
Column position for comments.

**Default:** `40`

### Example Configuration

```json
{
	"pasm.compilerPath": "/usr/local/bin/poppy",
	"pasm.defaultTarget": "snes",
	"pasm.formatting.opcodeColumn": 12,
	"pasm.formatting.operandColumn": 20,
	"pasm.formatting.commentColumn": 50
}
```

---

## 🎯 Workflow Tips

### Project Organization

```
my-game/
├── poppy.json          # Project configuration
├── src/
│   ├── main.pasm      # Main entry point
│   ├── engine/
│   │   ├── init.pasm
│   │   └── update.pasm
│   └── data/
│       ├── sprites.bin
│       └── music.bin
└── build/             # Output directory
```

### Using Build Tasks

Create `.vscode/tasks.json`:

```json
{
	"version": "2.0.0",
	"tasks": [
		{
			"label": "Build Game",
			"type": "poppy",
			"buildType": "project",
			"problemMatcher": ["$poppy"]
		}
	]
}
```

### Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Build | `Ctrl+Shift+B` |
| Format | `Shift+Alt+F` |
| Go to Definition | `F12` |
| IntelliSense | `Ctrl+Space` |
| Symbol Search | `Ctrl+Shift+O` |
| Command Palette | `Ctrl+Shift+P` |

---

## 🐛 Troubleshooting

### Syntax Highlighting Not Working

**Problem:** File shows as plain text  
**Solution:** Ensure file has `.pasm` extension

**Problem:** Some instructions not highlighted  
**Solution:** Check if using correct target directive (`.target nes/snes/gb`)

### Build Tasks Not Appearing

**Problem:** No Poppy tasks in task list  
**Solution:** Ensure Poppy compiler is installed and in PATH

**Verify:**
```bash
poppy --version
```

### IntelliSense Not Working

**Problem:** No completion suggestions  
**Solution:**
1. Verify `.pasm` file extension
2. Try restarting VS Code
3. Check for conflicting extensions

### Formatting Issues

**Problem:** Code not aligning correctly  
**Solution:** Check formatting settings in VS Code preferences

**Reset to defaults:**
```json
{
	"pasm.formatting.opcodeColumn": 8,
	"pasm.formatting.operandColumn": 16,
	"pasm.formatting.commentColumn": 40
}
```

---

## 📚 Additional Resources

- [Poppy Compiler Documentation](https://github.com/TheAnsarya/poppy/tree/main/docs)
- [User Manual](https://github.com/TheAnsarya/poppy/blob/main/docs/user-manual.md)
- [Example Projects](https://github.com/TheAnsarya/poppy/tree/main/examples)
- [GitHub Issues](https://github.com/TheAnsarya/poppy/issues)

---

## 🤝 Contributing

Found a bug or have a feature request?

1. Check existing [issues](https://github.com/TheAnsarya/poppy/issues)
2. Create a new issue with details
3. Submit a pull request

---

## 📄 License

**Unlicense** - This is free and unencumbered software released into the public domain.

See [LICENSE](https://github.com/TheAnsarya/poppy/blob/main/LICENSE) for details.

---

**🌸 Happy Coding! 🌸**
