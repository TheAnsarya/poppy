# 🌸 Poppy Compiler

> **Smart multi-system assembly compiler for retro gaming projects**

[![License: Unlicense](https://img.shields.io/badge/License-Unlicense-blue.svg)](https://unlicense.org)
[![Version](https://img.shields.io/badge/version-1.0.0-green.svg)](https://github.com/TheAnsarya/poppy/releases/tag/v1.0.0)
[![VS Code](https://img.shields.io/badge/VS%20Code-Extension-blue.svg)](https://marketplace.visualstudio.com/items?itemName=TheAnsarya.poppy-assembly)
[![Tests](https://img.shields.io/badge/tests-942%20passing-brightgreen.svg)](https://github.com/TheAnsarya/poppy)

---

## 🎉 v1.0.0 Released!

**Poppy v1.0.0** is now available with complete support for three retro gaming platforms! [Download the release →](https://github.com/TheAnsarya/poppy/releases/tag/v1.0.0)

---

## 🎯 Overview

**Poppy** is a production-ready multi-system assembly compiler targeting classic gaming platforms:

- **NES** (6502 processor) ✅ Complete
- **SNES** (65816 processor) ✅ Complete
- **Game Boy** (SM83 processor) ✅ Complete

The compiler supports real-world game development with comprehensive tooling, including a VS Code extension with IntelliSense, formatting, and build integration.

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

**Target Systems:**

- 🎮 Full NES/Famicom support (6502)
- 🎨 Full SNES/Super Famicom support (65816)
- 🕹️ Full Game Boy/Color support (SM83)
- 🏛️ Multiple memory mapping modes (LoROM, HiROM, ExHiROM)
- 📋 All iNES mapper configurations

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

**SNES ROM Generation:**

- 🎨 SNES header at correct ROM offset ($7fc0 LoROM, $ffc0 HiROM)
- 📋 11 SNES header directives (`.snes_name`, `.snes_map_mode`, etc.)
- 🗺️ LoROM, HiROM, and ExHiROM support
- ✅ Automatic checksum calculation
- 🔢 ROM speed, type, and region configuration

**Game Boy ROM Generation:**

- 🕹️ Game Boy header at $0100-$014f
- 📋 7 GB header directives (`.gb_title`, `.gb_mbc`, `.gb_cgb`, etc.)
- 🎮 MBC support (MBC1, MBC3, MBC5, etc.)
- 🌈 CGB (Color Game Boy) mode flags
- ✅ Automatic Nintendo logo and checksums
- 🔋 RAM size and battery configuration

**NES ROM Generation:**

- 🎮 iNES 1.0 and iNES 2.0 header generation
- 📋 12 iNES header directives (`.ines_prg`, `.ines_chr`, `.ines_mapper`, etc.)
- 🗺️ Support for mappers 0-4095, submappers 0-15
- 🔋 Battery backup, trainer, mirroring configuration
- 🌍 NTSC/PAL timing selection

**Macro System:**

- 🔧 Macro definitions with parameters (`.macro`/`.endmacro`)
- 🎯 Macro parameter substitution and default values
- 🏷️ Local labels within macros
- 🔁 Nested macro invocations

**Conditional Assembly:**

- ❓ Conditional compilation (`.if`/`.else`/`.endif`)
- 🔍 Symbol existence checks (`.ifdef`/`.ifndef`)
- 🔢 Expression-based conditionals (`.ifeq`, `.ifne`, `.ifgt`, etc.)

**Code Generation:**

- 🔁 Repeat blocks (`.rept`/`.endr`) for code generation
- 🔢 Enumeration blocks (`.enum`/`.ende`) for sequential constants
- 📊 Listing file generation with symbol tables

**Developer Tools:**

- 🎨 [VS Code Extension](https://marketplace.visualstudio.com/items?itemName=TheAnsarya.poppy-assembly)
	- Syntax highlighting for all platforms
	- IntelliSense with opcode documentation
	- Code formatting with column alignment
	- 40+ code snippets
	- Build task integration
	- Go-to-definition and hover info
- 📊 Comprehensive error messages with context
- 🧮 Advanced expression evaluation
- 📋 Multiple output formats (ROM, symbols, listings, memory maps)

---

## 🚀 Quick Start

### Installation

#### From GitHub Releases

Download the latest release from [GitHub Releases](https://github.com/TheAnsarya/poppy/releases/latest).

#### From Source

```bash
# Clone the repository
git clone https://github.com/TheAnsarya/poppy.git
cd poppy

# Build the compiler
cd src
dotnet build -c Release

# The compiler will be at: src/Poppy.CLI/bin/Release/net9.0/poppy.exe
```

#### VS Code Extension

Install the "Poppy Assembly" extension from the [Visual Studio Code Marketplace](https://marketplace.visualstudio.com/items?itemName=TheAnsarya.poppy-assembly) for the best development experience.

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

### Examples

Check out the example projects in the `examples/` directory:

- **[nes-hello-world](examples/nes-hello-world/)** - Minimal NES ROM with screen initialization
- **[snes-hello-world](examples/snes-hello-world/)** - SNES ROM with native mode setup
- **[gb-hello-world](examples/gb-hello-world/)** - Game Boy ROM displaying "HELLO" text

---

## 📖 Documentation

### User Guides

| Document | Description |
|----------|-------------|
| [User Manual](docs/user-manual.md) | Complete usage guide with examples |
| [SNES Development Guide](docs/snes-guide.md) | Comprehensive SNES/65816 guide |
| [Game Boy Development Guide](docs/gameboy-guide.md) | Complete GB/GBC guide with SM83 |
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
| [Roadmap](~docs/roadmap.md) | Development roadmap and milestones (v1.0 complete!) |
| [v1.x Roadmap](~plans/v1.x-roadmap.md) | Plans for v1.1-v1.3 (project system, assets, advanced features) |
| [v2.0 Roadmap](~plans/v2.0-roadmap.md) | Platform expansion (GBA, Genesis, SPC700, LSP, WASM) |
| [v1.0.0 Release Report](~plans/v1.0.0-final-release-report.md) | Complete v1.0.0 release summary |

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

**Current Version:** v1.0.0 (Released January 15, 2026)

**Completed:**
- ✅ Full NES support (6502, iNES 2.0, 942 tests passing)
- ✅ Full SNES support (65816, LoROM/HiROM/ExHiROM)
- ✅ Full Game Boy support (SM83, MBC1/3/5, CGB modes)
- ✅ Complete macro system with parameters
- ✅ Conditional assembly (.if, .ifdef, .ifndef)
- ✅ Include system (.include, .incbin)
- ✅ Debug symbol export (.sym, .nl, .mlb)
- ✅ VS Code extension (published to marketplace)
- ✅ Comprehensive documentation (10 guides, 5,800+ lines)
- ✅ Example projects for all platforms

**Next Version:** v1.1.0 (Q1 2026)
- Project file system (poppy.json)
- Multi-file compilation with dependency tracking
- Watch mode for auto-rebuild
- Enhanced expression evaluation
- VS Code workspace symbols

**Future:** v2.0.0 (Q4 2026)
- Platform expansion (GBA, Genesis, Atari 2600, TG16, etc.)
- Language Server Protocol (LSP)
- Web-based compiler (WASM)
- Plugin system

See [v1.x Roadmap](~plans/v1.x-roadmap.md) and [v2.0 Roadmap](~plans/v2.0-roadmap.md) for details.

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
is free and unencumbered software released into the public domain.

See the [LICENSE](LICENSE) file for details (Unlicense)
---

## 🔗 References

Inspired by and learning from:

- [ASAR](https://github.com/RPGHacker/asar) - SNES patching assembler
- [XKAS](https://github.com/hex-usr/xkas) - SNES assembler
- [Ophis](https://github.com/michaelcmartin/Ophis) - 6502 assembler
- [ca65](https://cc65.github.io/doc/ca65.html) - Part of cc65 suite

---

_🌸 Poppy - Making retro game development bloom_

