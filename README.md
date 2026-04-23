# 🌸 Poppy Compiler

> **Smart multi-system assembly compiler for retro gaming projects**

[![License: Unlicense](https://img.shields.io/badge/License-Unlicense-blue.svg)](https://unlicense.org)
[![Version](https://img.shields.io/badge/version-2.0.0-green.svg)](https://github.com/TheAnsarya/poppy/releases/tag/v2.0.0)
[![VS Code](https://img.shields.io/badge/VS%20Code-Extension-blue.svg)](https://marketplace.visualstudio.com/items?itemName=TheAnsarya.poppy-assembly)
[![Tests](https://img.shields.io/badge/tests-3185%20passing-brightgreen.svg)](https://github.com/TheAnsarya/poppy)

---

## 🎉 v2.0.0 Released

**Poppy v2.0.0** is now available with Pansy.Core integration, reverse converters, and bank switching! [Download the release →](https://github.com/TheAnsarya/poppy/releases/tag/v2.0.0)

---

## 🎯 Overview

**Poppy** is a production-ready multi-system assembly compiler targeting classic gaming platforms:

| Platform | CPU | Status |
|----------|-----|--------|
| **NES** | MOS 6502 | ✅ Compile-validated |
| **SNES** | WDC 65816 | ✅ Compile-validated |
| **Game Boy** | Sharp SM83 | ✅ Compile-validated |
| **Atari 2600** | MOS 6507 | ✅ Compile-validated |
| **Atari Lynx** | WDC 65C02 | ✅ Compile-validated |
| **Genesis** | Motorola 68000 | ✅ Compile-validated |
| **GBA** | ARM7TDMI | ✅ Compile-validated |
| **WonderSwan** | NEC V30MZ | ✅ Compile-validated |
| **Master System** | Zilog Z80 | ✅ Compile-validated |
| **TurboGrafx-16** | HuC6280 | ✅ Compile-validated |

The compiler supports real-world game development with comprehensive tooling, including a [VS Code extension](https://marketplace.visualstudio.com/items?itemName=TheAnsarya.poppy-assembly) with IntelliSense, formatting, and build integration.

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
- 🧩 `.asset` / `.asset_manifest` directives for binary/JSON/CHR reinsertion
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

**Metadata & Verification:**

- 🌼 Automatic Pansy metadata generation (symbols, CDL, cross-refs)
- 🔄 Roundtrip verification against original ROM (byte-for-byte)
- 📦 Peony project support (`peony.json` from Nexen game packs)
- 🎯 `--pansy`, `--no-pansy`, `--no-verify` CLI flags

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

**Format Export:**

- 🔄 PASM-to-ASAR exporter (`.asm` output)
- 🔄 PASM-to-CA65 exporter (`.s` output with `.`→`@` local label conversion)
- 🔄 PASM-to-XKAS exporter (`;`→`//` comment conversion)
- 🏭 ExporterFactory for extensible format registration
- 📝 Automatic directive translation via reverse mapping tables

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

# The compiler will be at: src/Poppy.CLI/bin/Release/net10.0/poppy.exe
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

# Pansy metadata & roundtrip verification
poppy game.pasm -o game.nes         # Auto-generates game.pansy
poppy game.pasm --no-pansy          # Disable Pansy generation
poppy game.pasm --no-verify         # Disable roundtrip verification
poppy game.pasm --pansy out.pansy   # Custom Pansy output path

# Export to other assembler formats
poppy export game.pasm --to asar    # Export to ASAR format (.asm)
poppy export game.pasm --to ca65    # Export to CA65 format (.s)
poppy export game.pasm --to xkas    # Export to XKAS format (.asm)
poppy export src/ --to asar         # Export entire project directory
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
| [Channel F Development Guide](docs/channelf-guide.md) | Channel F/F8 project layout, syntax, and coding patterns |
| [SNES Development Guide](docs/snes-guide.md) | Comprehensive SNES/65816 guide |
| [Game Boy Development Guide](docs/gameboy-guide.md) | Complete GB/GBC guide with SM83 |
| [Atari Lynx Guide](docs/atari-lynx-guide.md) | Atari Lynx/65C02 assembly guide |
| [System Syntax Reference](docs/system-syntax-reference.md) | Per-system `.target`, CLI platform commands, and baseline syntax |
| [Build from Project](docs/build-from-project.md) | Nexen → Peony → Poppy pipeline |
| [Project File Format](docs/project-file-format.md) | `.poppy` project configuration |
| [Syntax Specification](docs/syntax-spec.md) | Assembly language syntax guide |

### Migration Guides

| Document | Description |
|----------|-------------|
| [Migrating from ASAR](docs/migration-from-asar.md) | ASAR → Poppy for SNES |
| [Migrating from ca65](docs/migration-from-ca65.md) | ca65/cc65 → Poppy for NES/SNES |
| [Migrating from xkas](docs/migration-from-xkas.md) | xkas → Poppy for SNES |
| [Migrating from RGBDS](docs/migration-from-rgbds.md) | RGBDS → Poppy for Game Boy |
| [Migrating from WLA-DX](docs/migration-from-wla-dx.md) | WLA-DX → Poppy for Z80/6502/65816 |
| [Migrating from DASM](docs/migration-from-dasm.md) | DASM → Poppy for Atari 2600 |
| [Migrating from ASM68K](docs/migration-from-asm68k.md) | ASM68K → Poppy for Genesis |
| [Migrating from devkitARM](docs/migration-from-devkitarm.md) | devkitARM/GAS → Poppy for GBA |

### Technical Reference

| Document | Description |
|----------|-------------|
| [Architecture](docs/architecture.md) | Compiler design and structure |
| [Benchmarks](docs/benchmarks.md) | BenchmarkDotNet suite and ARM special-emission benchmark workflows |
| [PASM File Format](docs/pasm-file-format.md) | Poppy Assembly `.pasm` file format specification |
| [CDL/DIZ Workflow](docs/cdl-diz-workflow.md) | Poppy ↔ Peony roundtrip workflow with CDL/DIZ files |
| [File Formats](docs/file-formats.md) | ROM and patch format reference |
| [Resources](docs/resources.md) | External links and research |
| [Release Notes v1.0.0](RELEASE-NOTES-1.0.0.md) | Complete v1.0.0 release summary |

### Planning Documents

| Document | Description |
|----------|-------------|
| [Roadmap](~docs/roadmap.md) | Development roadmap and milestones (v1.0 complete!) |
| [v1.x Roadmap](~plans/v1.x-roadmap.md) | Plans for v1.1-v1.3 (project system, assets, advanced features) |
| [v2.0 Roadmap](~plans/v2.0-roadmap.md) | Platform expansion (GBA, Genesis, SPC700, LSP, WASM) |
| [v1.0.0 Release Report](~plans/v1.0.0-final-release-report.md) | Complete v1.0.0 release summary |
| [All Plans](~plans/) | Architecture plans, encoding specs, issue analysis |

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

**Current Version:** v2.0.0 (Released 2026)

**Completed:**

- ✅ Full NES support (6502, iNES 2.0)
- ✅ Full SNES support (65816, LoROM/HiROM/ExHiROM)
- ✅ Full Game Boy support (SM83, MBC1/3/5, CGB modes)
- ✅ Full Atari 2600 support (6507)
- ✅ Full Atari Lynx support (65C02)
- ✅ Complete macro system with parameters
- ✅ Conditional assembly (.if, .ifdef, .ifndef)
- ✅ Include system (.include, .incbin)
- ✅ Debug symbol export (.sym, .nl, .mlb)
- ✅ Reverse converters (PASM → ASAR/CA65/XKAS)
- ✅ VS Code extension (published to marketplace)
- ✅ Comprehensive documentation (21 guides, 5,800+ lines)
- ✅ Example projects for all platforms
- ✅ Pansy.Core integration for metadata export
- ✅ Bank switching support
- ✅ 3,185 tests passing

**Planned:**

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

## 🌷 Integrated Pipeline

Poppy is the **build** stage of the **Flower Toolchain** — an integrated pipeline for playing, debugging, disassembling, editing, and rebuilding retro games:

| Stage | Tool | Poppy Role |
|-------|------|------------|
| 1. Play & Debug | [Nexen](https://github.com/TheAnsarya/Nexen) | — |
| 2. Disassemble | [Peony](https://github.com/TheAnsarya/peony) | — |
| 3. Edit & Document | Editor + [Pansy](https://github.com/TheAnsarya/pansy) UI | — |
| 4. Build | **Poppy** | Compile `.pasm` → ROM, generate Pansy metadata |
| 5. Verify | [Game Garden](https://github.com/TheAnsarya/game-garden) | Roundtrip byte-identical rebuild |

See the [Integrated Pipeline Master Plan](https://github.com/TheAnsarya/pansy/blob/main/~Plans/integrated-pipeline-master-plan.md) for architecture details.

## 🤝 Related Projects

- **[Nexen](https://github.com/TheAnsarya/Nexen)** - Multi-system emulator & debugger
- **[🌼 Pansy](https://github.com/TheAnsarya/pansy)** - Universal disassembly metadata format
- **[🌺 Peony](https://github.com/TheAnsarya/peony)** - Multi-system disassembler
- **[🌱 Game Garden](https://github.com/TheAnsarya/game-garden)** - Games disassembly & recompilation
- **[GameInfo](https://github.com/TheAnsarya/GameInfo)** - ROM hacking toolkit
- **[BPS-Patch](https://github.com/TheAnsarya/bps-patch)** - Binary patching system

---

## 📜 License

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

