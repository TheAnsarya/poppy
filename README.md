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

## ✨ Features (Planned)

- 📝 Clean, lowercase assembly syntax
- 🔢 `$` prefix for hexadecimal values (e.g., `$40df`)
- 📦 Multi-file project support
- 🔗 Include directives for code and assets
- 🎨 Asset conversion pipeline
- 🛠️ Macro and conditional assembly
- 📊 Comprehensive error reporting

---

## 🚀 Quick Start

*Coming soon - compiler is under development*

```asm
; Example Poppy assembly (NES/6502)
.org $8000

reset:
sei
cld
ldx #$ff
txs

lda #$00
sta $2000
sta $2001

loop:
jmp loop

.org $fffa
.dw $0000       ; nmi vector
.dw reset       ; reset vector
.dw $0000       ; irq vector
```

---

## 📖 Documentation

| Document | Description |
|----------|-------------|
| [Roadmap](~docs/roadmap.md) | Development roadmap and milestones |
| [Structure](~docs/structure.md) | Project folder structure |
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

