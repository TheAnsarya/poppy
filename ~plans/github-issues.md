# 📋 GitHub Issues - Poppy Compiler

> Issue templates and tracking for project management

**Repository:** TheAnsarya/poppy
**Created:** January 11, 2026
**Last Updated:** January 14, 2026

---

## 📈 Current Status

**Open Issues:** 12
**Closed Issues:** 63
**Epic Issues:** 8 (all open)

### Recently Closed (January 2026)
- ✅ #74 - VS Code extension test suite (Jan 14)
- ✅ #73 - Document formatting provider (Jan 14)
- ✅ #72 - IntelliSense completion provider (Jan 14)
- ✅ #71 - Code snippets library (Jan 14)
- ✅ #70 - Enhanced TextMate grammar (Jan 14)
- ✅ #68 - Add completion provider (duplicate of #72, Jan 14)
- ✅ #67 - Game Boy header directives (Jan 14)
- ✅ #58 - Code generation for conditionals/macros/repeat (Jan 12)
- ✅ #33 - Comprehensive macro system tests (Jan 12)
- ✅ #8 - Listing file output (Jan 12)
- ✅ #7 - Error messages with context (Jan 12)
- ✅ #5 - Include file support (previously closed)

### Currently Open Issues

#### Epic Issues (8)
- #9 - [Epic] Include System (phase-6, milestone: v0.2.0)
- #10 - [Epic] Label System Enhancement (phase-6, milestone: v0.2.0)
- #11 - [Epic] Directive System (phase-6, milestone: v0.2.0)
- #13 - [Epic] Output Formats (phase-7, milestone: v1.0.0)
- #44 - [Epic] GameInfo Repository Integration
- #45 - [Epic] Project Build System
- #46 - [Epic] Documentation and Examples
- #47 - [Epic] VS Code Extension (near complete - packaging/publishing remaining)
- #48 - [Epic] Game Boy Support

#### Core Functionality - 3 issues
- #69 - Add .include directive support
- #52 - Create FFMQ Poppy project configuration
- #51 - Create DW1 Poppy project configuration

---

## 🏷️ Labels

Create these labels in the GitHub repository:

| Label | Color | Description |
|-------|-------|-------------|
| `phase-1` | `#0e8a16` | Foundation phase |
| `phase-2` | `#1d76db` | Research & Architecture |
| `phase-3` | `#5319e7` | Lexer & Parser |
| `phase-4` | `#fbca04` | 6502 Code Generation |
| `phase-5` | `#d93f0b` | 65816 Code Generation |
| `phase-6` | `#c5def5` | Advanced Features |
| `phase-7` | `#bfdadc` | Target Output |
| `phase-8` | `#f9d0c4` | Polish & Release |
| `documentation` | `#0075ca` | Documentation improvements |
| `research` | `#7057ff` | Research tasks |
| `bug` | `#d73a4a` | Bug reports |
| `enhancement` | `#a2eeef` | New features |
| `good first issue` | `#7057ff` | Good for newcomers |

---

## 📝 Issue Templates

### Phase 2: Research & Architecture

#### Issue #1: Define Instruction Tables for 6502
```
Title: Define 6502 instruction encoding tables
Labels: phase-2, enhancement

## Description
Create data structures to define all 6502 instructions with their opcodes, 
addressing modes, and byte sizes.

## Acceptance Criteria
- [ ] All 56 legal 6502 opcodes defined
- [ ] All 13 addressing modes mapped
- [ ] Byte sizes and cycle counts documented
- [ ] Unit tests for opcode lookup

## References
- docs/resources.md (6502 section)
```

#### Issue #2: Define Instruction Tables for 65816
```
Title: Define 65816 instruction encoding tables
Labels: phase-2, enhancement

## Description
Create data structures for 65816 instructions including mode-dependent 
operand sizes.

## Acceptance Criteria
- [ ] All 65816 opcodes defined (~92 instructions)
- [ ] All 24 addressing modes mapped
- [ ] M/X flag dependent sizing handled
- [ ] Long addressing modes supported

## References
- docs/resources.md (65816 section)
```

#### Issue #3: Define Instruction Tables for SM83
```
Title: Define Game Boy SM83 instruction encoding tables
Labels: phase-2, enhancement

## Description
Create data structures for Game Boy SM83 (LR35902) instructions including 
CB-prefixed operations.

## Acceptance Criteria
- [ ] All standard opcodes defined
- [ ] All CB-prefixed opcodes defined
- [ ] Conditional timing variants documented
- [ ] Register encoding patterns mapped

## References
- docs/resources.md (Game Boy section)
```

---

### Phase 3: Lexer & Parser

#### Issue #4: Implement Lexer
```
Title: Implement assembly language lexer
Labels: phase-3, enhancement

## Description
Create a lexer that tokenizes Poppy assembly source code.

## Acceptance Criteria
- [ ] Tokenize numbers (hex $ff, decimal, binary %1010)
- [ ] Tokenize identifiers (labels, instructions)
- [ ] Tokenize strings and characters
- [ ] Tokenize operators and delimiters
- [ ] Handle comments (; and /* */)
- [ ] Track source locations for error reporting
- [ ] Unit tests for all token types

## References
- docs/syntax-spec.md
- docs/architecture.md (Lexer section)
```

#### Issue #5: Implement Parser
```
Title: Implement assembly language parser
Labels: phase-3, enhancement

## Description
Create a parser that builds an AST from the token stream.

## Acceptance Criteria
- [ ] Parse instructions with all addressing modes
- [ ] Parse labels (global, local, anonymous)
- [ ] Parse directives (.org, .db, etc.)
- [ ] Parse expressions with operators
- [ ] Build valid AST nodes
- [ ] Report syntax errors with locations
- [ ] Unit tests for grammar rules

## References
- docs/syntax-spec.md
- docs/architecture.md (Parser section)
```

#### Issue #6: Implement Symbol Table
```
Title: Implement symbol table for label resolution
Labels: phase-3, enhancement

## Description
Create a symbol table to track labels, constants, and their addresses.

## Acceptance Criteria
- [ ] Store global labels
- [ ] Handle local/scoped labels
- [ ] Support forward references
- [ ] Track symbol definition state
- [ ] Support defines and constants
- [ ] Unit tests for lookup operations

## References
- docs/architecture.md (Semantic Analyzer section)
```

---

### Phase 4: 6502 Code Generation

#### Issue #7: Implement 6502 Code Generator
```
Title: Implement 6502 machine code generation
Labels: phase-4, enhancement

## Description
Create a code generator that converts AST to 6502 machine code.

## Acceptance Criteria
- [ ] Encode all 56 instructions
- [ ] Handle all addressing modes
- [ ] Resolve label references
- [ ] Calculate relative branch offsets
- [ ] Validate address ranges
- [ ] Integration tests with known-good output

## References
- docs/resources.md (6502 section)
- docs/architecture.md (Code Generator section)
```

#### Issue #8: Implement Basic Directives
```
Title: Implement core assembler directives
Labels: phase-4, enhancement

## Description
Implement essential directives for basic assembly.

## Acceptance Criteria
- [ ] .org - set program counter
- [ ] .db/.byte - define bytes
- [ ] .dw/.word - define words
- [ ] .ds - define space
- [ ] .include - include source
- [ ] .incbin - include binary

## References
- docs/syntax-spec.md (Directives section)
```

---

### Phase 5: 65816 Code Generation

#### Issue #9: Implement 65816 Code Generator
```
Title: Implement 65816 machine code generation
Labels: phase-5, enhancement

## Description
Create a code generator for 65816 with mode tracking.

## Acceptance Criteria
- [ ] Encode all 65816 instructions
- [ ] Track M/X flag state
- [ ] Handle variable-size immediates
- [ ] Support long addressing
- [ ] Support bank addressing
- [ ] Integration tests

## References
- docs/resources.md (65816 section)
```

#### Issue #10: Implement SNES-Specific Directives
```
Title: Implement SNES memory mapping directives
Labels: phase-5, enhancement

## Description
Add SNES-specific directives for ROM mapping.

## Acceptance Criteria
- [ ] .lorom directive
- [ ] .hirom directive
- [ ] Bank addressing support
- [ ] Header generation helpers

## References
- docs/file-formats.md (SNES section)
```

---

### Phase 6: Advanced Features

#### Issue #11: Implement Macro System
```
Title: Implement macro definition and expansion
Labels: phase-6, enhancement

## Description
Add support for defining and calling macros.

## Acceptance Criteria
- [ ] .macro/.endmacro directives
- [ ] Parameter substitution
- [ ] Local labels in macros
- [ ] Recursive macro expansion
- [ ] Variadic macros

## References
- docs/syntax-spec.md (Macros section)
```

#### Issue #12: Implement Conditional Assembly
```
Title: Implement conditional assembly directives
Labels: phase-6, enhancement

## Description
Add support for conditional compilation.

## Acceptance Criteria
- [ ] .if/.elif/.else/.endif
- [ ] .ifdef/.ifndef
- [ ] Expression evaluation in conditions
- [ ] Nested conditionals

## References
- docs/syntax-spec.md (Conditional Assembly section)
```

#### Issue #13: Implement Expression Evaluator
```
Title: Implement compile-time expression evaluation
Labels: phase-6, enhancement

## Description
Evaluate arithmetic and logical expressions at compile time.

## Acceptance Criteria
- [ ] Arithmetic: + - * / %
- [ ] Bitwise: & | ^ ~ << >>
- [ ] Comparison: == != < > <= >=
- [ ] Unary: - ~ ! < > ^
- [ ] Parentheses for grouping
- [ ] Label arithmetic

## References
- docs/syntax-spec.md (Expressions section)
```

---

### Phase 7: Target Output

#### Issue #14: Implement iNES Output
```
Title: Generate iNES format ROM files
Labels: phase-7, enhancement

## Description
Output assembled code as iNES ROM files.

## Acceptance Criteria
- [ ] Generate valid iNES header
- [ ] Support mapper configuration
- [ ] Calculate checksums
- [ ] Validate ROM size

## References
- docs/file-formats.md (NES section)
```

#### Issue #15: Implement SFC Output
```
Title: Generate SNES ROM files
Labels: phase-7, enhancement

## Description
Output assembled code as SNES ROM files.

## Acceptance Criteria
- [ ] Generate internal header
- [ ] Support LoROM/HiROM
- [ ] Calculate checksums
- [ ] Validate ROM size

## References
- docs/file-formats.md (SNES section)
```

---

### Documentation Issues

#### Issue #16: Create User Manual
```
Title: Write comprehensive user documentation
Labels: documentation

## Description
Create user-facing documentation for using the assembler.

## Acceptance Criteria
- [ ] Installation guide
- [ ] Getting started tutorial
- [ ] Command-line reference
- [ ] Complete directive reference
- [ ] Example programs
```

#### Issue #17: Create Developer Guide
```
Title: Write developer documentation
Labels: documentation

## Description
Document the codebase for contributors.

## Acceptance Criteria
- [ ] Architecture overview
- [ ] Module documentation
- [ ] How to add new instructions
- [ ] How to add new output formats
- [ ] Testing guide
```

---

## 🎯 Milestones

### Milestone: v0.1.0 - Basic 6502 Assembler
**Target:** Q1 2026
- Issues: #4, #5, #6, #7, #8, #14

### Milestone: v0.2.0 - 65816 Support
**Target:** Q2 2026
- Issues: #9, #10, #15

### Milestone: v0.3.0 - Advanced Features
**Target:** Q3 2026
- Issues: #11, #12, #13

### Milestone: v1.0.0 - Production Release
**Target:** Q4 2026
- Issues: #16, #17, plus polish

---

## 📊 Issue Creation Commands

Use these commands with GitHub CLI (`gh`) to create issues:

```bash
# Create labels
gh label create phase-1 --color 0e8a16 --description "Foundation phase"
gh label create phase-2 --color 1d76db --description "Research & Architecture"
gh label create phase-3 --color 5319e7 --description "Lexer & Parser"
gh label create phase-4 --color fbca04 --description "6502 Code Generation"
gh label create phase-5 --color d93f0b --description "65816 Code Generation"
gh label create phase-6 --color c5def5 --description "Advanced Features"
gh label create phase-7 --color bfdadc --description "Target Output"
gh label create documentation --color 0075ca --description "Documentation"
gh label create research --color 7057ff --description "Research tasks"

# Create milestones
gh api repos/TheAnsarya/poppy/milestones -f title="v0.1.0 - Basic 6502 Assembler" -f due_on="2026-03-31T00:00:00Z"
gh api repos/TheAnsarya/poppy/milestones -f title="v0.2.0 - 65816 Support" -f due_on="2026-06-30T00:00:00Z"
gh api repos/TheAnsarya/poppy/milestones -f title="v0.3.0 - Advanced Features" -f due_on="2026-09-30T00:00:00Z"
gh api repos/TheAnsarya/poppy/milestones -f title="v1.0.0 - Production Release" -f due_on="2026-12-31T00:00:00Z"

# Example issue creation
gh issue create --title "Implement assembly language lexer" --label "phase-3,enhancement" --body-file issue-4.md
```

---

## 📝 Notes

- Issues should be created in priority order
- Each issue should be small enough to complete in 1-2 days
- Link related issues using GitHub references
- Update this document as issues are created

---

