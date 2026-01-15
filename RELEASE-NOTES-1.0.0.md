# 🌸 Poppy v1.0.0 Release Notes

**Release Date:** January 15, 2026
**Status:** ✅ STABLE RELEASE

---

## 🎉 Overview

This is the first stable release of Poppy, a multi-system assembly compiler targeting classic gaming platforms. Poppy v1.0.0 provides complete, production-ready support for NES, SNES, and Game Boy development.

---

## ✨ Major Features

### Multi-System Support

- **NES/Famicom** - Full 6502 instruction set with iNES 1.0/2.0 ROM generation
- **SNES/Super Famicom** - Complete 65816 support with LoROM/HiROM/ExHiROM
- **Game Boy/Color** - Full SM83 instruction set with MBC and CGB support

### ROM Generation

- **NES** - iNES 2.0 headers with 12 directives, mappers 0-4095, submappers 0-15
- **SNES** - Automatic header placement ($7fc0 LoROM, $ffc0 HiROM), checksum calculation
- **Game Boy** - Header at $0100-$014f, Nintendo logo, MBC1/3/5, CGB modes

### Assembly Features

- 🏷️ Global, local (@), and anonymous (+/-) labels
- 📦 File inclusion (.include, .incbin)
- 🔧 Macro system with parameters and local labels
- ❓ Conditional assembly (.if/.ifdef/.ifndef)
- 🔁 Repeat blocks (.rept) and enumerations (.enum)
- ✅ Compile-time assertions (.assert)
- 🗺️ Memory mapping (.org, .align, .pad)
- 📊 Data directives (.byte, .word, .long, .fill, .ds)

### Developer Tools

- 🐛 Debug symbol export (FCEUX .nl, Mesen .mlb, generic .sym)
- 📝 Comprehensive error messages with context
- 🎨 VS Code extension with IntelliSense and formatting
- 📖 Complete documentation for all target systems

---

## 📋 System-Specific Features

### NES (6502)

```asm
.target nes
.ines_mapper 1         ; MMC1
.ines_prg 2            ; 32KB PRG
.ines_chr 1            ; 8KB CHR
.ines_mirroring VERTICAL

.org $c000
reset:
    lda #$00
    sta $2000
```

### SNES (65816)

```asm
.target snes
.snes_map_mode LOROM
.snes_name "MY GAME"
.snes_rom_speed FAST

.org $808000
start:
    clc                ; 8-bit accumulator
    xce
    rep #$30           ; 16-bit A and X/Y
    lda #$1234
```

### Game Boy (SM83)

```asm
.target gameboy
.gb_title "HELLO"
.gb_mbc MBC5
.gb_cgb CGB_COMPATIBLE

.org $0150
start:
    ld sp, $fffe
    call init_lcd
```

---

## 📊 Test Coverage

- **942 unit tests** - 100% passing
- **Coverage** - Lexer, parser, code generation, semantics
- **Integration tests** - NES, SNES, and GB ROM generation

---

## 📖 Documentation

### User Guides

- [User Manual](docs/user-manual.md) - Complete usage guide
- [SNES Development Guide](docs/snes-guide.md) - 65816 comprehensive reference
- [Game Boy Development Guide](docs/gameboy-guide.md) - SM83 and hardware guide
- [Syntax Specification](docs/syntax-spec.md) - Language syntax reference

### Technical Reference

- [Architecture](docs/architecture.md) - Compiler design
- [File Formats](docs/file-formats.md) - ROM format specifications
- [Migration from ca65](docs/migration-from-ca65.md) - Porting guide
- [Migration from ASAR](docs/migration-from-asar.md) - SNES transition guide

---

## 🚀 Quick Start

### Installation

```bash
# Clone and build
git clone https://github.com/TheAnsarya/poppy.git
cd poppy/src
dotnet build -c Release

# The compiler is at: src/Poppy.CLI/bin/Release/net10.0/poppy.exe
```

### VS Code Extension

Install "Poppy Assembly" from the Visual Studio Code Marketplace for:
- Syntax highlighting for all three platforms
- IntelliSense with opcode documentation
- Code snippets for common patterns
- Build task integration
- Real-time diagnostics

---

## 🎯 What's Included

### Compiler Features

- ✅ All 6502 opcodes and addressing modes
- ✅ All 65816 opcodes with M/X flag awareness
- ✅ All SM83 opcodes including CB-prefixed
- ✅ Bank:Address notation ($7e:1234)
- ✅ Automatic zero-page optimization
- ✅ Local and anonymous label scoping
- ✅ Macro expansion with local labels
- ✅ Conditional compilation
- ✅ File inclusion system
- ✅ Binary data inclusion
- ✅ Debug symbol generation
- ✅ Comprehensive error reporting

### Platform Support

| Feature | NES | SNES | Game Boy |
|---------|-----|------|----------|
| Instruction Set | ✅ 6502 | ✅ 65816 | ✅ SM83 |
| ROM Generation | ✅ iNES 2.0 | ✅ LoROM/HiROM | ✅ MBC1/3/5 |
| Mapper Support | ✅ 0-4095 | ✅ Mode 20/21 | ✅ All MBCs |
| Header Directives | ✅ 12 | ✅ 11 | ✅ 7 |
| Checksum | ✅ Auto | ✅ Auto | ✅ Auto |
| Bank Switching | ✅ Mappers | ✅ Banks | ✅ MBC |
| Special Features | - | ✅ ExHiROM | ✅ CGB Color |

---

## 📦 Downloads

### Source Code

- [GitHub Repository](https://github.com/TheAnsarya/poppy)
- [v1.0.0 Release](https://github.com/TheAnsarya/poppy/releases/tag/v1.0.0)

### VS Code Extension

- [Visual Studio Code Marketplace](https://marketplace.visualstudio.com/items?itemName=TheAnsarya.poppy-assembly)
- Extension ID: `TheAnsarya.poppy-assembly`

---

## 🔧 Known Limitations

1. **No Project System** - Multi-file projects require manual includes (v1.1.0 planned)
2. **Limited Expression Evaluation** - Complex expressions may need parentheses
3. **No Asset Pipeline** - Binary assets must be pre-converted
4. **Manual Testing Required** - Test your ROMs in emulators

---

## 🛣️ Future Plans

### v1.1.0 (Q1 2026)

- Project file system (poppy.json)
- Multi-file compilation with dependency tracking
- Watch mode for auto-rebuild
- Enhanced expression evaluation

### v1.2.0 (Q2 2026)

- Asset conversion pipeline (PNG to CHR/tilemap)
- Language Server Protocol (LSP) support
- Performance optimizations
- More example projects

### v2.0.0 (Q3 2026)

- SPC700 support (SNES audio processor)
- GBA support (ARM7TDMI)
- Genesis/Mega Drive support (68000)

---

## 🙏 Credits

**Inspired by:**
- [ASAR](https://github.com/RPGHacker/asar) - SNES assembler
- [ca65](https://cc65.github.io/doc/ca65.html) - 6502 assembler
- [RGBDS](https://rgbds.gbdev.io/) - Game Boy assembler
- [Ophis](https://github.com/michaelcmartin/Ophis) - 6502 cross-assembler

**Special Thanks:**
- Pan Docs contributors for GB documentation
- Super Famicom Development Wiki
- NESdev community
- All contributors and testers

---

## 📜 License

This software is released into the public domain under the [Unlicense](https://unlicense.org/).

You are free to use, modify, and distribute this software for any purpose, commercial or non-commercial, without any restrictions.

---

## 🐛 Reporting Issues

Found a bug or have a feature request?

- [GitHub Issues](https://github.com/TheAnsarya/poppy/issues)
- [Discussions](https://github.com/TheAnsarya/poppy/discussions)

---

**🌸 Happy retro game development!**

_Poppy v1.0.0 - Making retro game development bloom_
