# 🗺️ Long-Term Plan - Poppy Compiler

**Project:** Poppy Compiler
**Vision:** A comprehensive multi-system assembly compiler for retro gaming projects

---

## 📅 Q1 2026: Foundation

### January: Setup & Architecture
- [x] Project structure and configuration
- [ ] Research existing assemblers
- [ ] Design compiler architecture
- [ ] Basic lexer and parser
- [ ] Simple 6502 instruction encoding

### February: NES Support
- [ ] Complete 6502 instruction set
- [ ] All addressing modes
- [ ] Label and symbol support
- [ ] Basic include directive
- [ ] NES header generation (.ines)
- [ ] Memory mapping basics

### March: Polish & Testing
- [ ] Error messages and diagnostics
- [ ] Comprehensive test suite
- [ ] Documentation for NES assembly
- [ ] Simple project file support
- [ ] Command-line interface

---

## 📅 Q2 2026: SNES Support

### April: 65816 Basics
- [ ] 65816 instruction set
- [ ] 16-bit mode handling
- [ ] Bank switching support
- [ ] SNES header generation

### May: Advanced Features
- [ ] Macros
- [ ] Conditional assembly
- [ ] Math expressions
- [ ] Asset include directive
- [ ] Convertor integration planning

### June: DW1/FFMQ Target
- [ ] Test with Dragon Warrior 1 project
- [ ] Test with Final Fantasy Mystic Quest project
- [ ] Fix compatibility issues
- [ ] Performance optimization

---

## 📅 Q3 2026: Advanced Features

### July: Asset Pipeline
- [ ] Asset convertor framework
- [ ] Graphics conversion support
- [ ] Audio conversion support
- [ ] Custom include with convertor syntax

### August: Project System
- [ ] Project file format
- [ ] Multi-file compilation
- [ ] Dependency tracking
- [ ] Incremental builds

### September: DW4/DQ3r Target
- [ ] Test with Dragon Warrior 4 project
- [ ] Test with Dragon Quest 3 remake project
- [ ] Complex ROM mapping support
- [ ] Bank management

---

## 📅 Q4 2026: Polish & Expansion

### October: Game Boy Support
- [ ] Z80-like instruction set
- [ ] GB header generation
- [ ] GB-specific features

### November: Documentation & Tools
- [ ] Comprehensive user manual
- [ ] API documentation
- [ ] VS Code extension (syntax highlighting)
- [ ] Example projects

### December: Release Preparation
- [ ] Version 1.0 feature freeze
- [ ] Final testing and bug fixes
- [ ] Release documentation
- [ ] Community feedback integration

---

## 🎯 Key Milestones

| Date | Milestone | Description |
|------|-----------|-------------|
| Jan 2026 | M1 | Basic 6502 assembly compilation |
| Mar 2026 | M2 | Complete NES support |
| Jun 2026 | M3 | SNES support, DW1/FFMQ compile |
| Sep 2026 | M4 | DW4/DQ3r compile success |
| Dec 2026 | M5 | Version 1.0 release |

---

## 🏆 Target Projects

Priority order for compilation targets:

1. **Dragon Warrior 1 (NES)** - Simple NES game
2. **Final Fantasy Mystic Quest (SNES)** - Simple SNES game
3. **Dragon Warrior 4 (NES)** - Complex NES game
4. **Dragon Quest 3 Remake (SNES)** - Complex SNES project

---

## 📐 Architecture Goals

### Compiler Features
- Multi-pass assembly
- Strong error reporting with line numbers
- Support for multiple output formats
- Extensible instruction set definitions
- Plugin system for convertors

### Syntax Features
- Lowercase opcodes (standard)
- `$` hex prefix (e.g., `$40df`)
- Include files
- Asset includes with convertors
- Macros and conditionals
- Math expressions
- Named constants and labels

### Quality Goals
- Comprehensive documentation
- Full test coverage
- Performance competitive with existing assemblers
- Clear, maintainable codebase

---

## 📝 Reference Compilers

Study and learn from:
- **ASAR** - SNES patching assembler
- **XKAS** - SNES assembler
- **Ophis** - 6502 assembler
- **ca65** - Part of cc65 suite

---

## 🔗 Related Documents

- [Short-Term Plan](short-term-plan.md)
- [Roadmap](../~docs/roadmap.md)
- [Project Structure](../~docs/structure.md)

