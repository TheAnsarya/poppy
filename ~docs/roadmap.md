# 🗺️ Project Roadmap - Poppy Compiler

**Repository:** TheAnsarya/poppy
**Purpose:** Multi-system assembly compiler for retro gaming platforms
**Created:** December 25, 2025
**Updated:** January 15, 2026
**Current Version:** 1.0.0 (Released)

---

## 🎉 Current Status

**Poppy v1.0.0 is RELEASED!** (January 15, 2026)

✅ **Complete NES Support** - Full 6502 with iNES 2.0
✅ **Complete SNES Support** - Full 65816 with LoROM/HiROM
✅ **Complete Game Boy Support** - Full SM83 with MBC/CGB
✅ **VS Code Extension** - Published to marketplace
✅ **942 Tests Passing** - Comprehensive test coverage
✅ **Full Documentation** - 5,800+ lines across 10 guides

[📦 Download v1.0.0](https://github.com/TheAnsarya/poppy/releases/tag/v1.0.0)

---

## 🎯 Vision

Create a comprehensive assembly compiler supporting:

- **NES** (6502) - Primary target
- **SNES** (65816) - Primary target
- **Game Boy** (Z80-like) - Secondary target

Capable of compiling real-world projects like Dragon Warrior, FFMQ, DW4, and DQ3r.

---

## �️ Detailed Roadmaps

For detailed feature planning:

- [v1.x Roadmap](../~plans/v1.x-roadmap.md) - v1.1.0 through v1.3.0 plans
- [v2.0 Roadmap](../~plans/v2.0-roadmap.md) - Platform expansion and advanced features

---

## 📅 Historical Milestones (COMPLETED)

### Phase 1: Foundation ✅ (December 2025)
**Status:** Complete
**Goal:** Establish project structure and documentation

#### Tasks

- ✅ Create `.editorconfig` with comprehensive formatting rules
- ✅ Create `.gitignore` for common development artifacts
- ✅ Set up documentation folder structure
- ✅ Create initial chat and session logs
- ✅ Save manual prompts log
- ✅ Create `.github/copilot-instructions.md`
- ✅ Create roadmap and planning documents
- ✅ Create short-term and long-term plans
- ✅ Update README.md for project
- ✅ Git commit all work
- ✅ Enable GitHub Issues
- ✅ Create Kanban board

### Phase 2: Research & Architecture ✅ (December 2025)
**Status:** Complete
**Goal:** Design compiler architecture and document target systems

#### Tasks

- ✅ Document 6502 instruction set (in InstructionSet6502.cs)
- ⬜ Document 65816 instruction set
- ✅ Analyze ASAR, XKAS, Ophis, ca65 syntax
- ✅ Design compiler architecture
- ✅ Choose implementation language (C# / .NET 10)
- ✅ Define Poppy assembly syntax specification
- ✅ Create architecture documentation

---

### Phase 3: Core Compiler - Lexer & Parser ✅ (Complete)
**Status:** Complete
**Goal:** Implement lexical analysis and parsing

#### Tasks

- ✅ Create source code project structure
- ✅ Define token types (opcodes, operands, labels, directives)
- ✅ Implement lexer for Poppy syntax
- ✅ Implement parser for assembly statements
- ✅ Handle lowercase opcodes
- ✅ Handle `$` hex prefix parsing
- ✅ Create unit test infrastructure (125 Lexer tests, 67 Parser tests)

---

### Phase 4: Code Generation - 6502 ✅ (Complete)
**Status:** Complete
**Goal:** Generate correct binary for NES/6502 assembly
**Completed:** January 11, 2026

#### Tasks

- ✅ Create opcode-to-byte mapping tables (InstructionSet6502)
- ✅ Implement all 6502 addressing modes
- ✅ Label and symbol resolution (SemanticAnalyzer)
- ✅ Generate binary output (CodeGenerator)
- ✅ iNES header generation
- ✅ Comprehensive testing (375 tests passing)

---

### Phase 5: Code Generation - 65816 ✅ (January 2026)
**Status:** Complete
**Goal:** Add SNES/65816 support

#### Tasks

- ✅ Implement 65816 instruction set
- ✅ Handle 16-bit mode
- ✅ Bank switching support
- ✅ SNES header generation
- ✅ Memory mapping (LoROM/HiROM/ExHiROM)

---

### Phase 6: Include System & Label Features ✅ (Complete)
**Status:** Complete
**Goal:** Multi-file support and advanced label features
**Completed:** January 11, 2026

#### Tasks

- ✅ .include directive for file inclusion
- ✅ .incbin directive for binary data inclusion
- ✅ Local labels with @ prefix and scoping
- ✅ Anonymous forward labels (+)
- ✅ Anonymous backward labels (-)
- ✅ Target directives (.nes, .snes, .gb)
- ✅ Mapper selection (.mapper)
- ✅ Alignment directives (.align, .pad)
- ✅ Compile-time assertions (.assert, .error, .warning)
- ✅ Multi-line comments (/**/)

---

### Phase 7: Output Formats ✅ (Complete)
**Status:** Complete
**Goal:** ROM generation and debug symbol export
**Completed:** January 11, 2026

#### Tasks

- ✅ iNES 1.0 and iNES 2.0 header generation
- ✅ 12 iNES header directives (.ines_prg, .ines_chr, etc.)
- ✅ Debug symbol export (FCEUX .nl, Mesen .mlb, generic .sym)
- ✅ CLI integration with -s/--symbols flag
- ✅ Automatic header prepending for NES ROMs

---

### Phase 8: Macro System & Conditional Assembly ✅ (Complete)
**Status:** Complete
**Goal:** Add macros, conditionals, and code reuse features
**Completed:** January 12, 2026

#### Tasks

- ✅ Macro definitions (.macro/.endmacro)
- ✅ Macro expansion engine (MacroExpander)
- ✅ Macro parameters and substitution
- ✅ Macro default parameters (param=value syntax)
- ✅ Macro-local labels
- ✅ Macro invocation with @ prefix
- ✅ Case-insensitive macro names
- ✅ Conditional assembly (.if, .else, .endif)
- ✅ Symbol conditionals (.ifdef, .ifndef)
- ✅ Comparison conditionals (.ifeq, .ifne, .ifgt, .iflt, .ifge, .ifle)
- ✅ Repeat blocks (.rept/.endr)
- ✅ Enumeration blocks (.enum/.ende)
- ✅ Comprehensive macro tests (30+ tests)

**GitHub Issues:** #24-#33
**Tests:** 60+ tests
**Documentation:** pasm-file-format.md updated

---

### Phase 9: Developer Experience ✅ (Complete)
**Status:** Complete
**Goal:** Improve error reporting and developer tools
**Completed:** January 12, 2026

#### Tasks

- ✅ Error messages with source context (ErrorFormatter)
- ✅ Listing file generation (ListingGenerator)
- ✅ Symbol table in listings
- ✅ Multi-file listing support
- ✅ 24+ tests for error/listing features

**GitHub Issues:** #7, #8

---

### Phase 10: Game Boy Support ✅ (January 2026)
**Status:** Complete
**Goal:** Add Game Boy (DMG/CGB) architecture support

#### Tasks

- ✅ SM83 (GB-Z80) instruction set
- ✅ Game Boy header directives  
- ✅ MBC bank switching support
- ✅ CGB (Color) mode features
- ✅ Complete Game Boy guide documentation

**GitHub Issue:** #48 (Epic) - Closed

---

### Phase 11: SNES Support ✅ (January 2026)
**Status:** Complete
**Goal:** Complete SNES/65816 architecture support

#### Tasks

- ✅ 65816 instruction set implementation
- ✅ SNES header builder
- ✅ M/X flag tracking with REP/SEP
- ✅ Register size directives (.a8, .a16, .i8, .i16, .smart)
- ✅ SNES header directives (.snes_title, .snes_region, etc.)
- ✅ LoROM/HiROM/ExHiROM memory mapping
- ✅ SNES example project
- ✅ SNES user documentation

---

### Phase 12: VS Code Extension ✅ (January 2026)
**Status:** Complete
**Goal:** Complete documentation and VS Code extension

#### Tasks

- ✅ Comprehensive user manual (1,373 lines)
- ✅ Architecture documentation
- ✅ Tutorial projects (NES, SNES, GB)
- ✅ VS Code extension (#47) - Published
	- Syntax highlighting
	- IntelliSense completion
	- Code formatting
	- Build integration
	- 40+ code snippets
- ✅ Migration guides (ca65, ASAR)

**GitHub Issue:** #47 (Epic) - Closed

---

### Phase 13: v1.0.0 Release ✅ (January 15, 2026)
**Status:** Complete
**Goal:** First stable release

#### Deliverables

- ✅ Complete compiler for 3 platforms (NES, SNES, GB)
- ✅ 942 tests passing
- ✅ VS Code extension published to marketplace
- ✅ Complete documentation (10 guides, 5,800+ lines)
- ✅ Example projects for all platforms
- ✅ GitHub release with binaries
- ✅ Unlicense (public domain)

---

## 🚀 Future Roadmap

### Planned: Project Build System (Phase 14)
**Target:** v1.1.0 (Q1 2026)
**Goal:** Implement project file support and build pipeline

#### Tasks

- [ ] Define poppy.json project file format
- [ ] Implement project file parser
- [ ] Multi-file compilation support
- [ ] Watch mode for auto-recompilation
- [ ] Build configurations (debug/release)

See [v1.x Roadmap](../~plans/v1.x-roadmap.md) for complete v1.1-1.3 plans.

---

### Planned: Platform Expansion (Phase 15)
**Target:** v2.0.0 (Q4 2026)
**Goal:** Add additional retro platforms

#### Tasks

- [ ] Game Boy Advance (ARM7TDMI)
- [ ] Sega Genesis/Mega Drive (68000)
- [ ] SNES SPC700 (Audio processor)

See [v2.0 Roadmap](../~plans/v2.0-roadmap.md) for complete v2.0 plans.

---

## 📊 Progress Summary

### Overall Completion

| Phase | Status | Completion |
|-------|--------|------------|
| Foundation | ✅ Complete | 100% |
| Research & Architecture | ✅ Complete | 100% |
| Lexer & Parser | ✅ Complete | 100% |
| 6502 Code Generation | ✅ Complete | 100% |
| 65816 Code Generation | ✅ Complete | 100% |
| Include & Labels | ✅ Complete | 100% |
| Output Formats | ✅ Complete | 100% |
| Macros & Conditionals | ✅ Complete | 100% |
| Developer Experience | ✅ Complete | 100% |
| Game Boy Support | ✅ Complete | 100% |
| SNES Support | ✅ Complete | 100% |
| VS Code Extension | ✅ Complete | 100% |
| **v1.0.0 Release** | ✅ **RELEASED** | **100%** |

**v1.0.0:** 13/13 phases complete ✅

---

### Platform Support Status

| Platform | Instruction Set | ROM Generation | Documentation | Examples | Status |
|----------|----------------|----------------|---------------|----------|--------|
| NES | ✅ 6502 | ✅ iNES 2.0 | ✅ Complete | ✅ | **Stable** |
| SNES | ✅ 65816 | ✅ LoROM/HiROM | ✅ Complete | ✅ | **Stable** |
| Game Boy | ✅ SM83 | ✅ MBC/CGB | ✅ Complete | ✅ | **Stable** |
| GBA | - | - | - | - | Planned v2.0 |
| Genesis | - | - | - | - | Planned v2.0 |

---

### Test Coverage

- **Total Tests:** 942
- **Pass Rate:** 100%
- **Coverage Areas:**
	- Lexer (125+ tests)
	- Parser (200+ tests)
	- Code Generation (200+ tests)
	- Semantics (150+ tests)
	- Integration (100+ tests)
	- Macros (60+ tests)
	- Error Handling (30+ tests)
	- VS Code Extension (13 tests)

---

## 🎯 Next Steps

1. **v1.1.0 Development** - Start implementing project system
2. **Community Building** - Gather feedback on v1.0.0
3. **Example Projects** - Create more complete game examples
4. **v2.0 Planning** - Finalize GBA/Genesis architecture

---

## 📝 Related Documents

- [Architecture Guide](architecture.md) - Compiler design details
- [Syntax Specification](syntax-spec.md) - Language reference
- [User Manual](user-manual.md) - Complete usage guide
- [v1.x Roadmap](../~plans/v1.x-roadmap.md) - v1.1-1.3 plans
- [v2.0 Roadmap](../~plans/v2.0-roadmap.md) - Future platform expansion

---

**Last Updated:** January 15, 2026
**Current Version:** v1.0.0 (Released)
**Next Target:** v1.1.0 (Q1 2026)

**GitHub Issues:** #46, #47 (Epics)

---

### Phase 15: Release ⬜ (Not Started)
**Status:** Not Started
**Goal:** Version 1.0 release

#### Tasks

- ⬜ Performance optimization
- ⬜ Final testing with real projects
- ⬜ Package for distribution (NuGet, standalone)
- ⬜ Release notes and changelog
- ⬜ Version 1.0.0 tag

---

## 📋 Coding Standards

### Formatting

- **Indentation:** Tabs (4-space width, 8 for assembly), NEVER spaces
- **Brace Style:** K&R (opening brace on same line)
- **Hexadecimal:** Always lowercase with `$` prefix
- **Encoding:** UTF-8 with BOM
- **Line Endings:** CRLF (Windows standard)
- **File Endings:** Blank line at EOF
- **Trailing Whitespace:** Removed (except markdown line breaks)

### Assembly Code

- Lowercase opcodes: `lda`, `sta`, `jsr`, `inc`, `tya`
- Lowercase hex values: `$40df`, `$ff`, `$0a`
- `$` prefix for all hex values

### Git Workflow

- **Feature Branches:** Create branch for each significant feature
- **Issue Tracking:** All commits reference issues
- **Logical Commits:** Commit in logical, related batches
- **Branch Merging:** Merge feature branches when complete

---

## 📁 Project Structure

```
/
├── .github/               # GitHub configuration
│   └── copilot-instructions.md
├── docs/                  # User documentation
├── src/                   # Source code (planned)
├── ~docs/                 # Project creation docs
│   ├── chat-logs/         # AI conversation logs
│   ├── session-logs/      # Session summaries
│   ├── roadmap.md         # This file
│   └── structure.md       # Structure documentation
├── ~plans/                # Planning documents
│   ├── short-term-plan.md
│   └── long-term-plan.md
├── ~manual-testing/       # Manual test files
├── ~reference-files/      # Reference materials
├── .editorconfig          # Editor configuration
├── .gitignore             # Git exclusions
├── LICENSE                # MIT License
└── README.md              # Project overview
```

---

## 🎮 Reference Compilers

Learning from existing assemblers:

| Compiler | System | Notes |
|----------|--------|-------|
| ASAR | SNES | Patching assembler, good syntax |
| XKAS | SNES | Classic SNES assembler |
| Ophis | 6502 | Clean Python-based assembler |
| ca65 | 6502/65816 | Part of cc65, full-featured |

---

## 📊 Success Criteria

Poppy 1.0 will be complete when:

- ✅ Core configuration and documentation in place
- ✅ Can compile 6502/NES assembly correctly
- ⬜ Can compile 65816/SNES assembly correctly
- ⬜ Successfully compiles DW1, FFMQ, DW4, DQ3r
- ✅ Comprehensive documentation exists
- ✅ Error messages are clear and helpful
- ⬜ Performance is competitive with existing assemblers

---

## 🔗 Related Documents

- [README](../README.md) - Project overview
- [Short-Term Plan](../~plans/short-term-plan.md) - 4-week goals
- [Long-Term Plan](../~plans/long-term-plan.md) - Quarterly milestones
- [Structure](structure.md) - Folder organization

---

