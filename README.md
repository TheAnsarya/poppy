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

**Core Compiler Features:**

- 📝 Clean, lowercase assembly syntax
- 🔢 `$` prefix for hexadecimal values (e.g., `$40df`)
- 🏷️ Labels, local labels, and anonymous labels
- 📍 `.org` directive for address setting
- 📊 Data directives (`.byte`, `.word`, `.long`, `.fill`, `.ds`)
- 🔀 All 6502 addressing modes
- 📈 Automatic zero-page optimization
- 🖥️ Command-line interface

**File System & Organization:**

- 📦 `.include` directive for file inclusion
- 📂 `.incbin` directive for binary data inclusion
- 🔄 Preprocessor with include path resolution
- 🗂️ Multi-file project support

**Label System:**

- 🏷️ Global labels
- 📌 Local labels with `@` prefix and scoping
- ➕ Anonymous forward labels (`+`, `++`, etc.)
- ➖ Anonymous backward labels (`-`, `--`, etc.)

**Directives & Features:**

- 🎯 Target directives (`.nes`, `.snes`, `.gb`)
- 🗺️ Memory mapping (`.lorom`, `.hirom`, `.exhirom`)
- 🔧 Mapper selection (`.mapper`)
- 📐 Alignment directives (`.align`, `.pad`)
- ✅ Compile-time assertions (`.assert`)
- ⚠️ Error and warning directives (`.error`, `.warning`)
- 💬 Multi-line comments (`/* */`)

**NES ROM Generation:**

- 🎮 iNES 1.0 and iNES 2.0 header generation
- 📋 12 iNES header directives (`.ines_prg`, `.ines_chr`, `.ines_mapper`, etc.)
- 🗺️ Support for mappers 0-4095, submappers 0-15
- 🔋 Battery backup, trainer, mirroring configuration
- 🌍 NTSC/PAL timing selection

**Macro System & Advanced Directives:**

- 🔧 Macro definitions with parameters (`.macro`/`.endmacro`)
- 🔄 Macro expansion with parameter substitution
- 📞 Macro invocations with `@` prefix (`@macro_name arg1, arg2`)
- 🏷️ Local label support in macros with automatic renaming
- ❓ Conditional assembly (`.if`/`.elseif`/`.else`/`.endif`)
- 🔍 Symbol existence checks (`.ifdef`/`.ifndef`)
- 🔁 Repeat blocks (`.rept`/`.endr`) for code generation
- 🔢 Enumeration blocks (`.enum`/`.ende`) for sequential constants

**Output Formats:**

- 🎮 NES ROM with iNES 2.0 header
- 🐛 Debug symbol files (FCEUX .nl, Mesen .mlb, generic .sym)
- 📊 Symbol table listing output

### Coming Soon 🚧

- 🎯 65816 instruction set (SNES)
- 🎮 SM83 instruction set (Game Boy)
- 🎨 Asset conversion pipeline
- 📊 Enhanced error reporting with context
- 🧮 More advanced expression evaluation

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
poppy game.pasm                     # Output: game.bin

# Specify output file
poppy -o rom.nes game.pasm          # Output: rom.nes

# Generate debug symbols
poppy -s game.nl game.pasm          # Creates FCEUX .nl symbol file
poppy -s game.mlb game.pasm         # Creates Mesen .mlb symbol file
poppy -s game.sym game.pasm         # Creates generic .sym symbol file

# Generate listing file
poppy -l game.lst game.pasm         # Creates symbol table listing

# Verbose output
poppy -V game.pasm                  # Shows compilation progress

# Target different architectures
poppy -t 6502 game.pasm             # NES (default)
poppy -t 65816 game.pasm            # SNES
poppy -t sm83 game.pasm             # Game Boy
```

### Example Assembly (NES/6502)

```asm
; Example NES ROM with iNES 2.0 header
.nes
.ines_prg 2        ; 32KB PRG ROM
.ines_chr 1        ; 8KB CHR ROM
.ines_mapper 0     ; NROM mapper
.ines_mirroring 1  ; vertical mirroring

; Constants
PPU_CTRL = $2000
PPU_MASK = $2001
PPU_STATUS = $2002

; Reset vector
.org $8000

reset:
    sei
    cld
    ldx #$ff
    txs

    lda #$00
    sta PPU_CTRL
    sta PPU_MASK

@wait_vblank1:
    bit PPU_STATUS
    bpl @wait_vblank1

@wait_vblank2:
    bit PPU_STATUS
    bpl @wait_vblank2

main_loop:
    jmp main_loop

; NMI handler
nmi:
    rti

; IRQ handler  
irq:
    rti

; Interrupt vectors
.org $fffa
.word nmi        ; NMI vector
.word reset      ; Reset vector
.word irq        ; IRQ/BRK vector
```

### Advanced Features

#### Local Labels
```asm
subroutine1:
@local_loop:     ; local to subroutine1
    dex
    bne @local_loop
    rts

subroutine2:
@local_loop:     ; different local scope
    dey
    bne @local_loop
    rts
```

#### Anonymous Labels
```asm
; Forward references (+)
lda #$00
beq +            ; jump to next +
lda #$01
+:
sta $2000

; Backward references (-)
-:
lda ($00),y
sta $2007
iny
bne -            ; jump to previous -
```

#### File Inclusion
```asm
.include "constants.inc"
.include "macros.inc"

; Binary data inclusion
.org $a000
.incbin "graphics.chr"
```

#### Compile-Time Assertions
```asm
.assert * < $8000, "Code exceeds PRG ROM bank"
.error "Not implemented yet"
.warning "TODO: Optimize this section"
```

---

## 📖 Documentation

### User Guides

| Document | Description |
|----------|-------------|
| [User Manual](docs/user-manual.md) | Complete usage guide with examples |
| [SNES Development Guide](docs/snes-guide.md) | Comprehensive SNES/65816 guide |
| [Project File Format](docs/project-file-format.md) | `.poppy` project configuration |
| [Syntax Specification](docs/syntax-spec.md) | Assembly language syntax guide |

### Technical Reference

| Document | Description |
|----------|-------------|
| [Architecture](docs/architecture.md) | Compiler design and structure |
| [File Formats](docs/file-formats.md) | ROM and patch format reference |
| [Resources](docs/resources.md) | External links and research |

### Planning Documents

| Document | Description |
|----------|-------------|
| [Roadmap](~docs/roadmap.md) | Development roadmap and milestones |
| [SNES Implementation Plan](~plans/snes-implementation-plan.md) | SNES/65816 work plan |
| [Short-Term Plan](~plans/short-term-plan.md) | 4-week immediate goals |
| [Long-Term Plan](~plans/long-term-plan.md) | Quarterly milestones |
| [GitHub Issues (Expanded)](~plans/github-issues-expanded.md) | Comprehensive issue templates |

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

**Current Phase:** v0.1.0 - Foundation Complete

- ✅ Project structure established
- ✅ Documentation framework in place
- ✅ Coding standards defined
- ✅ Core compiler implementation
- ✅ NES ROM generation with iNES 2.0
- ✅ Symbol export for debuggers
- ✅ Include system and preprocessor
- ✅ Label system (global, local, anonymous)
- ✅ Comprehensive test suite (375 tests)

**Next Phase:** Macro System & Conditional Assembly

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

_🌸 Poppy - Making retro game development bloom_

