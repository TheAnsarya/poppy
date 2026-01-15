# Change Log

All notable changes to the Poppy Assembly extension will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
