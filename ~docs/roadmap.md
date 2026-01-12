# 🗺️ Project Roadmap - Poppy Compiler

**Repository:** TheAnsarya/poppy
**Purpose:** Multi-system assembly compiler for retro gaming platforms
**Created:** December 25, 2025
**Updated:** January 11, 2026

---

## 🎯 Vision

Create a comprehensive assembly compiler supporting:
- **NES** (6502) - Primary target
- **SNES** (65816) - Primary target
- **Game Boy** (Z80-like) - Secondary target

Capable of compiling real-world projects like Dragon Warrior, FFMQ, DW4, and DQ3r.

---

## 📅 Milestones

### Phase 1: Foundation ✅ (Complete)
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
- ⬜ Create Kanban board

---

### Phase 2: Research & Architecture ✅ (Complete)
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

### Phase 5: Code Generation - 65816 ⬜ (Not Started)
**Status:** Not Started
**Goal:** Add SNES/65816 support

#### Tasks
- ⬜ Implement 65816 instruction set
- ⬜ Handle 16-bit mode
- ⬜ Bank switching support
- ⬜ SNES header generation
- ⬜ Memory mapping

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
- ✅ Multi-line comments (/* */)

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

### Phase 10: Target Project Compilation ⬜ (Not Started)
**Status:** Not Started
**Goal:** Successfully compile target game projects

#### Tasks
- ⬜ Compile Dragon Warrior 1 (NES)
- ⬜ Compile Final Fantasy Mystic Quest (SNES)
- ⬜ Compile Dragon Warrior 4 (NES)
- ⬜ Compile Dragon Quest 3 Remake (SNES)

---

### Phase 11: 65816 Support ⬜ (Not Started)
**Status:** Not Started
**Goal:** Add SNES/65816 architecture support

#### Tasks
- ⬜ 65816 instruction set implementation
- ⬜ 16-bit mode support
- ⬜ Bank switching
- ⬜ SNES header generation
- ⬜ Memory mapping

**GitHub Issue:** #12 (Epic)

---

### Phase 12: Polish & Documentation ⬜ (Not Started)
**Status:** Not Started
**Goal:** Complete documentation and prepare for release

#### Tasks
- ⬜ Comprehensive user manual
- ⬜ API documentation
- ⬜ Example projects
- ⬜ VS Code extension (syntax highlighting)
- ⬜ Version 1.0 release

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

