# Change Log

All notable changes to the Poppy Assembly extension will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.0] - 2026-02-01

### Added

- 🔗 **Reference Provider** - Find All References (Shift+F12) across workspace `.pasm`/`.inc` files
- ✏️ **Rename Provider** - Rename symbols (F2) across all workspace files with validation
- 📄 **Document Link Provider** - Clickable `.include`/`.incbin` paths with Ctrl+Click navigation
- 🔎 **Workspace Symbol Provider** - Search symbols across workspace with Ctrl+T
- 🌐 **Cross-File Hover** - Hover now searches workspace-wide for symbol definitions with file path display

### Fixed

- 🐛 **Hex Diagnostic Regex** - Fixed false positives: identifiers like `$GG` are now flagged while label-like tokens (starting with letter + underscore or length > 4) are correctly skipped
- 🐛 **Binary Diagnostic Regex** - Same heuristic fix applied to binary literal validation
- 🐛 **Parenthesis Check** - Now strips comments and strings before counting parentheses

### Changed

- ⬆️ **Symbol Provider File Limit** - Raised from 50 to 500 workspace files for better large-project support

## [2.1.0] - 2026-01-18

### Added

- 🎮 **Full 12-Platform Completions** - IntelliSense now provides 700+ opcodes across 10 ISAs:
	- 6502 (56 opcodes), 65SC02 (64), 65816 (92), SM83 (47), Z80 (75+)
	- M68000 (90+), ARM7TDMI (55+), HuC6280 (80+), V30MZ (75+), SPC700 (80+)
- 🔍 **Full 12-Platform Hover** - Instruction documentation for all architectures
	- SM83, Z80, M68000, ARM7TDMI, HuC6280, V30MZ, SPC700 instructions added
	- 40+ new directive hover entries
- 🎯 **Smart Target Detection** - Detects all 12 platforms from `.target` directive aliases and platform-specific directives
- 📝 **Grammar Additions** - New syntax highlighting patterns:
	- Conditional: `.ifeq`, `.ifne`, `.ifgt`, `.iflt`, `.ifge`, `.ifle`, `.ifexist`
	- Segment: `.banksize`
	- Output: `.channelf`, `.channel-f`, `.f8`
	- SNES: `.snes_fastrom`
	- Lynx: `.lynx_name`, `.lynx_bank0_size`, `.lynx_bank1_size`, `.lynxentry`, `.lynxboot`
	- iNES: no-underscore aliases (`.ines_fourscreen`, `.ines_prgram`, `.ines_chrram`, `.ines_pal`)
	- Misc: `.equ` constant definition, `.ende` alias for `.endenum`

### Changed

- ⬆️ **ESLint 9 Migration** - Upgraded to ESLint 9 with flat config (`eslint.config.mjs`)
- ⬆️ **typescript-eslint v8** - Unified package replacing separate plugin/parser
- ⬆️ **@types/node ^22** - Updated Node.js type definitions
- ⬆️ **TypeScript ^5.8** - Latest TypeScript compiler

### Removed

- 🧹 **Stale Artifacts** - Removed `package-lock.json` (using Yarn) and old `.vsix` build
- 🧹 **Legacy ESLint** - Removed `@typescript-eslint/eslint-plugin` and `@typescript-eslint/parser` (replaced by unified `typescript-eslint`)

## [2.0.0] - 2026-01-16

### Added

- 🎮 **11 Platform Support** - Full syntax highlighting for all Poppy v2.0 platforms:
    - **M68000** (Sega Genesis/Mega Drive) - Full instruction set
    - **Z80** (Master System/Game Gear) - Complete opcode support
    - **HuC6280** (TurboGrafx-16/PC Engine) - 6502 + extensions
    - **ARM7TDMI** (Game Boy Advance) - ARM and Thumb modes
    - **6507** (Atari 2600) - 6502 variant support
    - **65SC02** (Atari Lynx) - 65C02 instructions
    - **V30MZ** (WonderSwan) - x86-like instruction set
    - **SPC700** (SNES Audio) - Complete DSP opcodes
- 📝 **Platform-Specific Snippets** - Project templates for all 11 platforms:
    - `genesis-project` - Sega Genesis with vector table
    - `gba-project` - GBA with ROM header
    - `sms-project` - Master System with SEGA header
    - `tg16-project` - TurboGrafx-16 with MPR setup
    - `a2600-project` - Atari 2600 with proper timing loop
    - `lynx-project` - Atari Lynx template
    - `ws-project` - WonderSwan template
    - `spc700-project` - SNES audio template
- 🔤 **Register Highlighting** - CPU registers for all architectures:
    - M68000: d0-d7, a0-a7, sp, sr, ccr
    - Z80: a, b, c, d, e, h, l, ix, iy, af, bc, de, hl
    - ARM: r0-r15, sp, lr, pc, cpsr, spsr
    - V30MZ: ax, bx, cx, dx, si, di, bp, sp, cs, ds, es, ss
    - SPC700: a, x, y, sp, ya, psw
- 🏷️ **Platform Directives** - Highlighting for:
    - `.target`, `.cpu`, `.platform`, `.arch`
    - `.genesis_*`, `.gba_*`, `.sms_*`, `.tg16_*`
    - `.a2600_*`, `.lynx_*`, `.ws_*`
    - `.arm`, `.thumb` mode switching

### Changed

- 📦 **Version Bump to 2.0.0** - Major update for multi-platform support
- 🎯 **Target Settings** - Extended to support all 11 platforms
- 📝 **Updated Description** - Now lists all supported platforms

## [1.0.2] - 2026-01-16

### Fixed

- 🌸 **Fixed Extension Icon** - Proper cherry blossom emoji icon for marketplace

## [1.0.1] - 2026-01-16

### Added

- 🌸 **Extension Icon** - Beautiful pink flower icon for marketplace
- 📝 **Enhanced Description** - Updated to include all supported platforms
- 🔗 **Documentation Links** - Added links to GitHub, docs, and marketplace
- 🏷️ **More Keywords** - Added 68000, Z80, ARM, V30MZ, GBA, Genesis, Atari, WonderSwan

### Changed

- Updated platform support table to show current development status
- Improved installation instructions with compiler setup guide

## [1.0.0] - 2026-01-15

### Released

- 🎉 **First Stable Release** - Complete language support for Poppy Assembly
- 📦 **Published to Marketplace** - Available for installation in VS Code

### Added

- ✨ **Syntax Highlighting** - Comprehensive TextMate grammar for NES/SNES/GB assembly
   	- 6502 instruction set (56 opcodes)
   	- 65816 instruction set (80+ opcodes)
   	- SM83/Game Boy instruction set (90+ opcodes)
   	- All assembler directives (`.org`, `.db`, `.macro`, `.ines`, `.snes`, `.gb`, etc.)
   	- Labels (global, local, anonymous)
   	- Comments (single-line and multi-line)
   	- Numeric literals (hex, binary, decimal)
   	- String literals with escape sequences
   	- Macro definitions and invocations

- 💡 **IntelliSense Completion**
   	- Architecture-specific opcode completion (auto-detects target from directives)
   	- Directive completion with parameter hints
   	- Label completion (all defined labels in file)
   	- Register completion per architecture
   	- Addressing mode documentation

- 📐 **Code Formatting**
   	- Column-based alignment (configurable positions)
   	- Smart indentation for nested scopes
   	- Label positioning at column 0
   	- Opcode/operand/comment alignment
   	- Respects user tab/space preferences

- 🎯 **Navigation Features**
   	- Go to Definition for labels
   	- Document Symbol provider for outline view
   	- Hover information for opcodes
   	- Symbol search across files

- 🔧 **Build Integration**
   	- Task provider for building current file
   	- Task provider for building entire project
   	- Problem matcher for compiler errors
   	- Build commands in command palette

- 🐛 **Diagnostics**
   	- Real-time syntax error detection
   	- Compiler integration for validation
   	- Inline error messages with context
   	- Problems panel integration

- 📝 **Code Snippets**
   	- 40+ snippets for common patterns
   	- Project templates (NES, SNES, GB)
   	- Common macros (wait_vblank, ppu_addr, dma_copy)
   	- Control flow patterns (if/while/for/switch)
   	- Data structures (tables, strings, tiles, palettes)
   	- Hardware access patterns

- 🧪 **Testing**
   	- 13 Mocha integration tests
   	- Test infrastructure with @vscode/test-electron
   	- Coverage for completion, formatting, and integration features

- ⚙️ **Configuration**
   	- Compiler path setting
   	- Default target architecture
   	- Diagnostics enable/disable
   	- Formatting column positions (opcodes, operands, comments)

### Technical Details

- Minimum VS Code version: 1.80.0
- Language ID: `pasm`
- Supported file extensions: `.pasm`, `.inc`
- Test framework: Mocha 10.0
- TypeScript compilation target: ES2020

### Documentation

- Complete README with feature overview
- Publishing guide with step-by-step instructions
- Status tracking document
- LICENSE file (Unlicense)

### Known Issues
None - all planned features implemented and tested!

## [Unreleased]

### Planned for v1.1.0

- Extension icon and branding
- Screenshot and GIF demos in README
- Additional code snippets
- Performance optimizations for large files
- More comprehensive hover documentation
- Workspace symbol search

---

**Full Changelog**: <https://github.com/TheAnsarya/poppy/commits/main/vscode-extension>
