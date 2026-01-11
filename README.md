# 🌸 Poppy Compiler

> **Smart multi-system assembly compiler for retro gaming projects**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

---

## 🎯 Overview

**Poppy** is a multi-system assembly compiler targeting classic gaming platforms:

- **NES** (6502 processor)
- **SNES** (65816 processor)
- **Game Boy** (Z80-like processor)

The compiler aims to support compilation of retro game projects including Dragon Warrior, Final Fantasy Mystic Quest, and Dragon Quest remakes.

---

## ✨ Features

### Implemented ✅
- 📝 Clean, lowercase assembly syntax
- 🔢 `$` prefix for hexadecimal values (e.g., `$40df`)
- 🏷️ Labels and constants
- 📍 `.org` directive for address setting
- 📊 Data directives (`.byte`, `.word`, `.long`, `.fill`, `.ds`)
- 🔀 All 6502 addressing modes
- 📈 Automatic zero-page optimization
- 📋 Symbol table listing output
- 🖥️ Command-line interface

### Coming Soon 🚧
- 📦 Multi-file project support with `.include`
- 🛠️ Macro and conditional assembly
- 🎯 65816 instruction set (SNES)
- 🎮 SM83 instruction set (Game Boy)
- 🎨 Asset conversion pipeline
- 📊 Enhanced error reporting with context

---

## 🚀 Quick Start

### Installation

```bash
# Clone the repository
git clone https://github.com/yourusername/poppy.git
cd poppy

# Build the compiler
dotnet build src/

# Run the compiler
dotnet run --project src/Poppy.CLI -- --help
```

### Usage

```bash
# Basic assembly
poppy game.asm                     # Output: game.bin

# Specify output file
poppy -o rom.nes game.asm          # Output: rom.nes

# Generate listing file
poppy -l game.lst game.asm         # Creates symbol table listing

# Verbose output
poppy -V game.asm                  # Shows compilation progress

# Target different architectures
poppy -t 6502 game.asm             # NES (default)
poppy -t 65816 game.asm            # SNES
poppy -t sm83 game.asm             # Game Boy
```

### Example Assembly (NES/6502)

```asm
; Example Poppy assembly (NES/6502)
.org $8000

; Constants
PPU_CTRL = $2000
PPU_MASK = $2001

reset:
    sei
    cld
    ldx #$ff
    txs

    lda #$00
    sta PPU_CTRL
    sta PPU_MASK

loop:
    jmp loop

; Interrupt handlers
nmi:
irq:
    rti

; Vectors
.org $fffa
.word nmi        ; NMI vector
.word reset      ; Reset vector  
.word irq        ; IRQ vector
```

---

## 📖 Documentation

| Document | Description |
|----------|-------------|
| [Syntax Specification](docs/syntax-spec.md) | Assembly language syntax guide |
| [Architecture](docs/architecture.md) | Compiler design and structure |
| [File Formats](docs/file-formats.md) | ROM and patch format reference |
| [Resources](docs/resources.md) | External links and research |

### Planning Documents

| Document | Description |
|----------|-------------|
| [Roadmap](~docs/roadmap.md) | Development roadmap and milestones |
| [Short-Term Plan](~plans/short-term-plan.md) | 4-week immediate goals |
| [Long-Term Plan](~plans/long-term-plan.md) | Quarterly milestones |

### Internal Documentation

| Document | Description |
|----------|-------------|
| [Chat Logs](~docs/chat-logs/) | AI conversation archives |
| [Session Logs](~docs/session-logs/) | Work session summaries |

---

## 🎮 Target Projects

Priority compilation targets:

1. **Dragon Warrior 1** (NES) - Simple NES game
2. **Final Fantasy Mystic Quest** (SNES) - Simple SNES game
3. **Dragon Warrior 4** (NES) - Complex NES game
4. **Dragon Quest 3 Remake** (SNES) - Complex SNES project

---

## 📐 Syntax Highlights

### Hexadecimal Notation
```asm
lda #$40        ; immediate value
sta $2000       ; absolute address
lda $10,x       ; zero page indexed
```

### Labels and References
```asm
start:
lda #$01
jsr subroutine
jmp start

subroutine:
inc $10
rts
```

### Include Directives
```asm
.include "constants.inc"
.include "macros.inc"

; Asset with convertor (planned)
.asset "graphics.png" png2chr
```

---

## 🏗️ Project Status

**Current Phase:** Foundation

- ✅ Project structure established
- ✅ Documentation framework in place
- ✅ Coding standards defined
- 🔄 Architecture design in progress
- ⬜ Core compiler implementation

---

## 📋 Coding Standards

This project follows strict formatting guidelines:

- **Indentation:** TABS only (never spaces)
- **Brace Style:** K&R (opening brace on same line)
- **Hexadecimal:** Always lowercase with `$` prefix
- **Assembly:** Lowercase opcodes (`lda`, `sta`, `jsr`)
- **Encoding:** UTF-8 with BOM
- **Line Endings:** CRLF

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for complete guidelines.

---

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🔗 References

Inspired by and learning from:

- [ASAR](https://github.com/RPGHacker/asar) - SNES patching assembler
- [XKAS](https://github.com/hex-usr/xkas) - SNES assembler
- [Ophis](https://github.com/michaelcmartin/Ophis) - 6502 assembler
- [ca65](https://cc65.github.io/doc/ca65.html) - Part of cc65 suite

---

*🌸 Poppy - Making retro game development bloom*

