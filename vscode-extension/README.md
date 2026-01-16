# 🌸 Poppy Assembly VS Code Extension

Professional language support for Poppy Assembly (.pasm) files - a multi-system assembly compiler for retro game development.

## 🎮 Supported Platforms

| Platform | CPU | Status |
|----------|-----|--------|
| **NES** | MOS 6502 | ✅ Full support |
| **SNES** | WDC 65816 | ✅ Full support |
| **Game Boy** | Sharp SM83 | ✅ Full support |
| **Sega Genesis** | Motorola 68000 | ✅ Full support |
| **Game Boy Advance** | ARM7TDMI | ✅ Full support |
| **Master System** | Zilog Z80 | ✅ Full support |
| **TurboGrafx-16** | HuC6280 | ✅ Full support |
| **Atari 2600** | MOS 6507 | ✅ Full support |
| **Atari Lynx** | WDC 65SC02 | ✅ Full support |
| **WonderSwan** | NEC V30MZ | ✅ Full support |
| **SNES Audio** | Sony SPC700 | ✅ Full support |

## 🔗 Links

- **GitHub Repository:** [TheAnsarya/poppy](https://github.com/TheAnsarya/poppy)
- **Compiler Documentation:** [User Manual](https://github.com/TheAnsarya/poppy/blob/main/docs/user-manual.md)
- **Syntax Reference:** [Syntax Spec](https://github.com/TheAnsarya/poppy/blob/main/docs/syntax-spec.md)
- **Issues & Bugs:** [GitHub Issues](https://github.com/TheAnsarya/poppy/issues)
- **VS Code Marketplace:** [Poppy Assembly](https://marketplace.visualstudio.com/items?itemName=TheAnsarya.poppy-assembly)

## ✨ Features

### 🎨 **Syntax Highlighting**
Comprehensive TextMate grammar with full support for 11 CPU architectures:

- **6502/65816 Instructions** - NES/SNES opcodes (ADC, LDA, STA, REP, SEP, etc.)
- **SM83 Instructions** - Game Boy opcodes (LD, PUSH, POP, CB-prefixed, etc.)
- **M68000 Instructions** - Genesis opcodes (MOVE, LEA, DBRA, etc.)
- **Z80 Instructions** - Master System opcodes (LD, DJNZ, LDIR, etc.)
- **ARM7TDMI Instructions** - GBA opcodes (LDR, STR, B, BL, etc.)
- **HuC6280 Instructions** - TurboGrafx-16 (6502 + TAM, TMA, CSH, etc.)
- **V30MZ Instructions** - WonderSwan (MOV, PUSH, POP, REP, etc.)
- **SPC700 Instructions** - SNES audio (MOV, MOVW, TCALL, etc.)
- **Directives** - `.org`, `.db`, `.target`, `.cpu`, `.ines`, `.snes`, `.gb`, `.genesis`, etc.
- **Labels** - Global, local (@), and anonymous (+/-) labels
- **Macro System** - Definitions, invocations, and parameters
- **Comments** - Single-line (`;`, `//`) and multi-line (`/* */`)
- **Literals** - Hex (`$ff`), binary (`%10101010`), decimal, strings
- **Registers** - Architecture-specific register highlighting

### 💡 **IntelliSense Completion**
Smart, context-aware code completion:

- **Architecture-Specific Opcodes** - Automatically detects target from `.target` directive
- **Directive Completion** - All assembler directives with documentation
- **Register Completion** - Valid registers for each architecture
- **Label Completion** - Shows all defined labels in current file
- **Addressing Mode Hints** - Documentation shows valid addressing modes

### 📐 **Code Formatting**
Professional column-based alignment:

- Labels at column 0
- Opcodes at column 8 (configurable)
- Operands at column 16 (configurable)
- Comments at column 40 (configurable)
- Smart indentation for nested scopes
- Respects tab/space preferences

### 🎯 **Navigation**

- **Go to Definition** - Jump to label definitions
- **Document Symbols** - Outline view of labels and sections
- **Hover Information** - Opcode documentation and addressing modes

### 🔧 **Build Integration**

- **Task Provider** - Build current file or entire project
- **Problem Matcher** - Compiler errors appear in Problems panel
- **Build Commands** - `Poppy: Build Current File`, `Poppy: Build Project`

### 🐛 **Real-Time Diagnostics**

- Syntax error detection
- Compiler integration for validation
- Inline error messages with context

### 📝 **Code Snippets**
50+ snippets for common patterns:

- Project templates for all 11 platforms (NES, SNES, GB, Genesis, GBA, SMS, TG16, A2600, Lynx, WonderSwan, SPC700)
- Common macros (wait_vblank, ppu_addr, dma_copy)
- Control flow patterns (if/while/for/switch)
- Data structures (tables, strings, tiles, palettes)
- Hardware access patterns

### 🧪 **Fully Tested**

- 13 integration and unit tests
- Mocha + @vscode/test-electron framework
- 1527+ compiler tests (all passing)

## 🚀 Installation

### From VS Code Marketplace

Search for "**Poppy Assembly**" in VS Code Extensions marketplace, or:

1. Open VS Code
2. Press `Ctrl+Shift+X` (Extensions)
3. Search for `TheAnsarya.poppy-assembly`
4. Click **Install**

Or install from command line:
```bash
code --install-extension TheAnsarya.poppy-assembly
```

### Installing the Poppy Compiler

The extension provides language support. For building, you need the Poppy compiler:

1. **Download** from [GitHub Releases](https://github.com/TheAnsarya/poppy/releases)
2. **Or build from source:**
   ```bash
   git clone https://github.com/TheAnsarya/poppy.git
   cd poppy/src
   dotnet build -c Release
   ```
3. **Configure** the compiler path in VS Code settings:
   ```json
   {
     "poppy.compiler.path": "/path/to/poppy"
   }
   ```

### From VSIX (Local Development)

1. Clone the repository
2. Build the extension:
   ```bash
   cd vscode-extension
   npm install
   npm run compile
   ```
3. Package:
   ```bash
   npx @vscode/vsce package
   ```
4. Install:
   ```bash
   code --install-extension poppy-assembly-0.1.0.vsix
   ```

### Development Mode

1. Open `vscode-extension/` folder in VS Code
2. Run `npm install`
3. Press `F5` to launch Extension Development Host
4. Open a `.pasm` file to test

### Debugging Extension Tests

1. Open `vscode-extension/` in VS Code
2. Select "Extension Tests" from debug dropdown
3. Press `F5` to run tests with debugger attached

## 🛠️ Extension Development

### TextMate Grammar Editing

For authoring and debugging the TextMate grammar ([syntaxes/pasm.tmLanguage.json](syntaxes/pasm.tmLanguage.json)), install the [TmLanguage Syntax Highlighter](https://marketplace.visualstudio.com/items?itemName=RedCMD.tmlanguage-syntax-highlighter) extension by RedCMD. It provides:

- Syntax highlighting for `.tmLanguage.json` files
- Scope inspection tools (hover over code to see scopes)
- Real-time grammar validation
- Helpful diagnostics for pattern issues

### Running Tests

```bash
npm run test        # Run all tests
npm run compile     # Compile TypeScript
npm run watch       # Watch mode for development
```

## ⚙️ Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `poppy.compiler.path` | `""` | Path to Poppy compiler executable |
| `poppy.compiler.target` | `"nes"` | Default target architecture (nes/snes/gb) |
| `poppy.diagnostics.enabled` | `true` | Enable real-time diagnostics |
| `poppy.formatting.opcodeColumn` | `8` | Column position for instruction opcodes |
| `poppy.formatting.operandColumn` | `16` | Column position for operands |
| `poppy.formatting.commentColumn` | `40` | Column position for comments |

## 📋 Supported File Extensions

- `.pasm` - Poppy Assembly source files
- `.inc` - Include files (shared headers, constants, macros)

## 📚 Usage Examples

### NES Project

```asm
; NES Hello World with iNES header
.ines {"mapper": 0, "prg": 1, "chr": 1, "mirroring": "horizontal"}

.org $8000

Reset:
	sei					; Disable interrupts
	cld					; Clear decimal mode
	ldx #$ff
	txs					; Initialize stack

	lda #$00
	sta $2000			; Disable NMI
	sta $2001			; Disable rendering

.loop:
	jmp .loop			; Infinite loop

; Interrupt vectors
.org $fffa
	.dw 0				; NMI
	.dw Reset			; RESET
	.dw 0				; IRQ
```

### SNES Project

```asm
; SNES LoROM Project
.snes {"mapper": "lorom", "rom_size": 1024, "ram_size": 0}

.org $8000

Reset:
	sei					; Disable interrupts
	clc					; Clear carry
	xce					; Switch to native mode
	
	rep #$38			; 16-bit A/X/Y, decimal off
	
	lda #$1fff
	tcs					; Set stack pointer

.loop:
	wai					; Wait for interrupt
	bra .loop

; Vectors
.org $ffe4
	.dw 0, 0, 0, 0		; Native vectors
	.dw 0, 0, Reset, 0
```

### Game Boy Project

```asm
; Game Boy ROM Header
.gb
.gb_title "TETRIS"
.gb_cgb 0
.gb_cartridge_type 0
.gb_rom_size 32
.gb_ram_size 0

.org $150

Start:
	di					; Disable interrupts
	ld sp, $fffe		; Initialize stack
	
	; Wait for VBlank
	ld a, [$ff44]
	cp 144
	jr c, Start

	halt
```

### Using Macros and Completion

```asm
; Define a macro with IntelliSense support
@macro wait_vblank
	:loop
		lda $2002
		bpl :loop
@endm

; Use the macro (type @wa and press Ctrl+Space)
@wait_vblank

; Opcode completion (type 'ld' and press Ctrl+Space for all load instructions)
lda #$80			; Load accumulator
ldx #$00			; Load X register
```

### Code Formatting

Before formatting:
```asm
start:
lda #$40
sta $2000,x ; comment
```

After formatting (`Shift+Alt+F`):
```asm
start:
		lda		#$40				; comment
		sta		$2000,x				; comment
```

## 🎨 Color Theme Recommendations

For best syntax highlighting experience, we recommend these VS Code themes:

- **Dark+** (default) - Good contrast for all token types
- **Monokai** - Excellent for assembly code
- **Dracula** - Clear distinction between opcodes and directives
- **One Dark Pro** - Clean and modern

## 🔥 Features in Action

### IntelliSense

Type `.` to see all directives, or start typing an opcode to see completions with documentation:

![IntelliSense Demo](images/completion-demo.gif) _(placeholder for future screenshot)_

### Go to Definition

`Ctrl+Click` on any label to jump to its definition:

![Go to Definition Demo](images/goto-demo.gif) _(placeholder)_

### Build Integration

Press `Ctrl+Shift+B` to build your project with the integrated task provider:

![Build Demo](images/build-demo.gif) _(placeholder)_

## 🤝 Contributing

Contributions welcome! Areas for improvement:

- Additional code snippets
- More opcode documentation
- Performance optimizations
- Bug fixes and feature requests

See the main [Poppy repository](https://github.com/TheAnsarya/poppy) for contribution guidelines.

## 📝 Changelog

### v0.1.0 (Initial Release)

- ✨ Complete syntax highlighting for NES/SNES/GB
- ✨ IntelliSense completion for opcodes, directives, labels
- ✨ Document formatting with column alignment
- ✨ 40+ code snippets
- ✨ Build task integration
- ✨ Real-time diagnostics
- ✨ Go-to-definition and hover support
- ✅ Full test suite (13 tests)

## License

MIT - See LICENSE file in repository root.
