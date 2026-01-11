# 📚 Resources - Poppy Compiler

> Collection of URLs, documentation, and reference materials for development

**Last Updated:** January 11, 2026

---

## 🎮 Target System Documentation

### 6502 Processor (NES)

| Resource | URL | Description |
|----------|-----|-------------|
| 6502 Instruction Set | https://www.masswerk.at/6502/6502_instruction_set.html | Complete instruction reference |
| 6502.org | http://www.6502.org/ | Community resources and tutorials |
| Easy 6502 | https://skilldrick.github.io/easy6502/ | Interactive 6502 tutorial |
| NESdev Wiki - 6502 | https://www.nesdev.org/wiki/CPU | NES-specific CPU documentation |
| 6502 Opcodes | http://www.obelisk.me.uk/6502/reference.html | Detailed opcode reference |

### 65816 Processor (SNES)

| Resource | URL | Description |
|----------|-----|-------------|
| 65816 Reference | https://www.westerndesigncenter.com/wdc/documentation/w65c816s.pdf | Official WDC datasheet |
| SNESdev Wiki - 65816 | https://snes.nesdev.org/wiki/65816_reference | SNES-specific CPU reference |
| 65816 Primer | https://wiki.superfamicom.org/65816-reference | SuperFamicom wiki reference |
| Programming the 65816 | https://www.westerndesigncenter.com/wdc/documentation/Programmingthe65816.pdf | WDC programming guide |

### Game Boy (Z80-like)

| Resource | URL | Description |
|----------|-----|-------------|
| Pan Docs | https://gbdev.io/pandocs/ | Comprehensive GB documentation |
| GB CPU Manual | https://gbdev.io/gb-opcodes/optables/ | Opcode tables |
| GBDev Wiki | https://gbdev.gg8.se/wiki/articles/Main_Page | Community wiki |

---

## 🔧 Reference Assemblers

### ASAR (SNES)

| Resource | URL | Description |
|----------|-----|-------------|
| GitHub Repository | https://github.com/RPGHacker/asar | Source code |
| Documentation | https://rpghacker.github.io/asar/ | Official manual |
| ASAR Manual | https://github.com/RPGHacker/asar/blob/master/doc/asar_manual.txt | Text manual |

### XKAS (SNES)

| Resource | URL | Description |
|----------|-----|-------------|
| GitHub Mirror | https://github.com/hex-usr/xkas | Source code |
| Documentation | https://github.com/hex-usr/xkas/blob/master/readme.txt | Usage guide |

### ca65 (cc65 Suite)

| Resource | URL | Description |
|----------|-----|-------------|
| GitHub Repository | https://github.com/cc65/cc65 | Source code |
| ca65 Documentation | https://cc65.github.io/doc/ca65.html | Assembler manual |
| cc65 Main Site | https://cc65.github.io/ | Project homepage |

### Ophis (6502)

| Resource | URL | Description |
|----------|-----|-------------|
| GitHub Repository | https://github.com/michaelcmartin/Ophis | Source code |
| Documentation | http://michaelcmartin.github.io/Ophis/ | User manual |

### Other Assemblers

| Assembler | URL | Description |
|-----------|-----|-------------|
| asm6 | https://github.com/parasyte/asm6 | Simple 6502 assembler |
| NESASM | https://github.com/camsaul/nesasm | NES-focused assembler |
| WLA-DX | https://github.com/vhelin/wla-dx | Multi-platform assembler |
| 64tass | https://sourceforge.net/projects/tass64/ | Turbo Assembler compatible |

---

## 📖 NES Development

| Resource | URL | Description |
|----------|-----|-------------|
| NESdev Wiki | https://www.nesdev.org/wiki/Nesdev_Wiki | Primary NES dev resource |
| NES Architecture | https://www.nesdev.org/wiki/NES_reference_guide | Hardware reference |
| iNES Header | https://www.nesdev.org/wiki/INES | ROM header format |
| NES Memory Map | https://www.nesdev.org/wiki/CPU_memory_map | Address space layout |
| PPU Reference | https://www.nesdev.org/wiki/PPU | Graphics hardware |

---

## 📖 SNES Development

| Resource | URL | Description |
|----------|-----|-------------|
| SNESdev Wiki | https://snes.nesdev.org/wiki/Main_Page | Primary SNES dev resource |
| SNES Architecture | https://snes.nesdev.org/wiki/SNES_reference_guide | Hardware reference |
| SFC Development | https://wiki.superfamicom.org/ | SuperFamicom wiki |
| SNES Header | https://snes.nesdev.org/wiki/ROM_header | ROM header format |
| Memory Map | https://snes.nesdev.org/wiki/Memory_map | Address space layout |

---

## 🛠️ Compiler Design

| Resource | URL | Description |
|----------|-----|-------------|
| Crafting Interpreters | https://craftinginterpreters.com/ | Compiler/interpreter design |
| Writing an Assembler | https://www.codeproject.com/Articles/6950/Writing-an-Assembler | Tutorial article |
| Assembler Design | https://www.davespace.co.uk/arm/introduction-to-arm/assembler.html | General concepts |

---

## 📦 File Formats

### NES Formats

| Format | URL | Description |
|--------|-----|-------------|
| iNES | https://www.nesdev.org/wiki/INES | Standard NES ROM format |
| NES 2.0 | https://www.nesdev.org/wiki/NES_2.0 | Extended header format |
| CHR Format | https://www.nesdev.org/wiki/CHR_ROM | Graphics data format |

### SNES Formats

| Format | URL | Description |
|--------|-----|-------------|
| SFC/SMC | https://snes.nesdev.org/wiki/ROM_file_formats | SNES ROM formats |
| LoROM/HiROM | https://snes.nesdev.org/wiki/Memory_map | Memory mapping modes |
| BPS Patch | https://github.com/blakesmith/rompatcher | Patch format |

---

## 🎯 Target Projects

| Project | System | Complexity | Notes |
|---------|--------|------------|-------|
| Dragon Warrior 1 | NES | Simple | First target |
| Final Fantasy Mystic Quest | SNES | Simple | First SNES target |
| Dragon Warrior 4 | NES | Complex | Multi-bank |
| Dragon Quest 3 Remake | SNES | Complex | Large project |

---

## � Syntax Research Summary

> Key patterns discovered from reference assembler analysis (January 11, 2026)

### ASAR Syntax Highlights

| Feature | ASAR Syntax | Notes |
|---------|-------------|-------|
| Hex literals | `$ff`, `$0a40` | Dollar prefix, lowercase |
| Binary literals | `%01111111` | Percent prefix |
| Labels | `Label:`, `.SubLabel:` | Colon optional for sub labels |
| +/- Labels | `+`, `-`, `++`, `--` | Anonymous relative labels |
| Defines | `!define = value` | Bang prefix, spaces around `=` |
| Macros | `macro name(args)...endmacro` | Call with `%name(args)` |
| Include | `incsrc "file.asm"` | Source include |
| Binary include | `incbin "file.bin"` | Binary data include |
| Data | `db`, `dw`, `dl`, `dd` | 8/16/24/32-bit |
| Comments | `; comment` | Semicolon |
| Conditionals | `if/elseif/else/endif` | No dot prefix |
| Loops | `while`, `for`, `rep` | Iteration constructs |
| Org | `org $8000` | Set program counter |
| Mapping | `lorom`, `hirom` | SNES memory mapping |
| Freespace | `freecode`, `freedata` | Auto-find free space |
| Arch switch | `arch 65816`, `arch spc700` | CPU mode selection |
| Length spec | `lda.b #0`, `lda.w #0` | Explicit operand size |

### ca65 Syntax Highlights

| Feature | ca65 Syntax | Notes |
|---------|-------------|-------|
| Hex literals | `$ff`, `$0A40` | Dollar prefix |
| Binary literals | `%01111111` | Percent prefix |
| Labels | `Label:` | Colon required |
| Local labels | `@loop:` | At-sign prefix |
| Unnamed labels | `:` | Reference with `:+`, `:-` |
| Constants | `name = value` | Simple assignment |
| Macros | `.macro name...endmacro` | Dot prefix |
| Include | `.include "file"` | Dot prefix directive |
| Binary include | `.incbin "file"` | Dot prefix directive |
| Data | `.byte`, `.word`, `.dword` | Dot prefix |
| Comments | `; comment` | Semicolon |
| Conditionals | `.if/.elseif/.else/.endif` | Dot prefix |
| Repeat | `.repeat count` | Iteration |
| Org | `.org address` | Set program counter |
| Segments | `.segment "NAME"` | Relocatable sections |
| CPU mode | `.P02`, `.P816` | Processor selection |
| Structs | `.struct/.endstruct` | Data structures |
| Scopes | `.scope/.endscope`, `.proc/.endproc` | Lexical scoping |

### Key Differences & Design Considerations

| Aspect | ASAR | ca65 | Poppy Consideration |
|--------|------|------|---------------------|
| Directive prefix | None (keywords) | Dot prefix | TBD - leaning toward dot prefix |
| Define prefix | `!` | None (`.define`) | TBD |
| Macro call | `%macro()` | `macro` (like mnemonic) | TBD |
| Local labels | `.sub` | `@local` | Support both? |
| Relocatable | No (absolute) | Yes (segments) | Support both modes |
| Freespace | Built-in | N/A | Useful for ROM hacking |

### Common Patterns Across Assemblers

1. **Hex notation**: Universal `$` prefix for hex values
2. **Comments**: Universal `;` for line comments
3. **Labels**: Name followed by colon
4. **Basic data**: Some form of byte/word/long directives
5. **Includes**: Both source and binary file inclusion
6. **Conditionals**: if/else/endif structure
7. **Macros**: Define once, call multiple times

---

## 📝 Notes

- Resources are organized by category
- URLs should be verified before use
- Add new resources as discovered during development
- Mark broken links with ❌
- Syntax research from Jan 11, 2026 - ASAR and ca65 analysis complete

---

