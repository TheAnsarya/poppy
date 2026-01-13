# Poppy Assembly VS Code Extension

Language support for Poppy Assembly (.pasm) files targeting NES, SNES, and Game Boy platforms.

## Features

- **Syntax Highlighting** - Full support for:
  - 6502/65816 instruction mnemonics
  - Game Boy (SM83) instructions
  - Directives (.org, .db, .macro, etc.)
  - Labels (global, local, anonymous)
  - Macro invocations and parameters
  - Comments (;, //, /* */)
  - Numeric literals ($hex, %binary, decimal)
  - String literals

- **Language Configuration**:
  - Auto-closing brackets and quotes
  - Code folding for macros, scopes, and conditionals
  - Smart indentation

## Installation

### From VSIX (Local)

1. Build the extension: `npm run compile`
2. Package: `npx vsce package`
3. Install: `code --install-extension poppy-assembly-0.1.0.vsix`

### Development

1. Open this folder in VS Code
2. Run `npm install`
3. Press F5 to launch Extension Development Host

### Editing the TextMate Grammar

For authoring and debugging the TextMate grammar (`syntaxes/pasm.tmLanguage.json`), we recommend installing the [TmLanguage Syntax Highlighter](https://marketplace.visualstudio.com/items?itemName=RedCMD.tmlanguage-syntax-highlighter) extension by RedCMD. It provides:

- Syntax highlighting for `.tmLanguage.json` files
- Scope inspection tools
- Real-time grammar validation
- Helpful diagnostics for pattern issues

## Supported File Extensions

- `.pasm` - Poppy Assembly source files
- `.inc` - Include files

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `poppy.compiler.path` | `""` | Path to Poppy compiler |
| `poppy.compiler.target` | `"nes"` | Default target (nes/snes/gb) |
| `poppy.diagnostics.enabled` | `true` | Enable real-time diagnostics |

## Syntax Examples

```asm
; NES Hello World
.ines {"mapper": 0, "prg": 1, "chr": 1}

.org $8000

Reset:
    sei             ; Disable interrupts
    cld             ; Clear decimal mode
    ldx #$ff
    txs             ; Initialize stack

    lda #$00
    sta $2000       ; Disable NMI
    sta $2001       ; Disable rendering

.loop:
    jmp .loop       ; Infinite loop

; Vectors
.org $fffa
    .dw 0           ; NMI
    .dw Reset       ; RESET
    .dw 0           ; IRQ
```

## License

MIT - See LICENSE file in repository root.
