# 🔧 Phase 2: Macro System & Conditional Assembly - Implementation Plan

**Phase:** 2
**Timeline:** January 2026
**Focus:** Macro definitions, expansion, and conditional assembly
**Dependencies:** Phase 1 (Complete)

---

## 🎯 Overview

Phase 2 adds macro and conditional assembly capabilities to Poppy, enabling code reuse, parameterization, and conditional compilation. This is essential for real-world game development where repetitive patterns and platform-specific code are common.

---

## ✨ Features to Implement

### 1. Macro System
**Priority:** High
**Complexity:** High

#### 1.1 Macro Definitions
- `.macro` directive to begin macro definition
- `.endmacro` (or `.endm`) to end macro definition
- Macro name validation (no conflicts with opcodes/labels)
- Parameter support with named parameters
- Optional parameter syntax
- Parameter count validation

#### 1.2 Macro Expansion
- Macro invocation by name
- Argument passing and substitution
- Local label generation within macros
- Nested macro expansion
- Recursion detection and limits
- Macro-local symbols

#### 1.3 Advanced Macro Features
- Default parameter values
- Variadic parameters (variable argument count)
- String concatenation in macros
- Macro-time expressions
- `.exitm` directive for early exit

### 2. Conditional Assembly
**Priority:** High
**Complexity:** Medium

#### 2.1 Basic Conditionals
- `.if <expr>` - conditional block
- `.else` - else block
- `.elseif <expr>` - else-if chain
- `.endif` - end conditional block
- Expression evaluation at assembly time

#### 2.2 Symbol Conditionals
- `.ifdef <symbol>` - if symbol defined
- `.ifndef <symbol>` - if symbol not defined
- `.ifexist <symbol>` - alias for ifdef
- Nested conditionals

#### 2.3 Comparison Conditionals
- `.ifeq <val1>, <val2>` - if equal
- `.ifne <val1>, <val2>` - if not equal
- `.ifgt <val1>, <val2>` - if greater than
- `.iflt <val1>, <val2>` - if less than
- `.ifge <val1>, <val2>` - if greater or equal
- `.ifle <val1>, <val2>` - if less or equal

### 3. Repeat Blocks
**Priority:** Medium
**Complexity:** Low

- `.rept <count>` - repeat block N times
- `.endr` - end repeat block
- Counter variable within repeat blocks
- Nested repeats

### 4. Enumeration
**Priority:** Medium
**Complexity:** Low

- `.enum <start>` - begin enumeration
- `.ende` - end enumeration
- Automatic incrementing values
- Custom increment values

### 5. String Operations
**Priority:** Low
**Complexity:** Medium

- `.string` directive for string data
- String concatenation operator
- String length function
- Substring operations

---

## 📊 Implementation Strategy

### Stage 1: Parser & AST Extensions
**Duration:** 1-2 days

1. Add macro-related tokens to lexer
2. Add conditional directive tokens
3. Extend AST with new node types:
   - `MacroDefinitionNode`
   - `MacroInvocationNode`
   - `ConditionalBlockNode`
   - `RepeatBlockNode`
   - `EnumBlockNode`
4. Update parser to handle new syntax

### Stage 2: Macro Definition Processing
**Duration:** 1-2 days

1. Create `MacroTable` class (similar to `SymbolTable`)
2. Parse macro definitions in first pass
3. Store macro name, parameters, body
4. Validate macro names against reserved words
5. Handle nested macro definitions (error)

### Stage 3: Macro Expansion Engine
**Duration:** 2-3 days

1. Create `MacroExpander` class
2. Detect macro invocations
3. Validate argument count
4. Substitute parameters in macro body
5. Generate unique local labels
6. Expand macros recursively
7. Detect recursion (max depth check)
8. Insert expanded code into token stream

### Stage 4: Conditional Assembly
**Duration:** 1-2 days

1. Create `ConditionalEvaluator` class
2. Evaluate conditional expressions
3. Skip/include code blocks based on conditions
4. Handle nested conditionals
5. Validate condition syntax

### Stage 5: Repeat & Enumeration
**Duration:** 1 day

1. Implement `.rept` block expansion
2. Implement `.enum` value auto-assignment
3. Test nested structures

### Stage 6: Testing & Integration
**Duration:** 1-2 days

1. Unit tests for each feature (target: 60+ tests)
2. Integration tests with real-world patterns
3. Performance testing (macro expansion cost)
4. Documentation and examples

---

## 🧪 Test Coverage Goals

### Macro Tests (30 tests)
- Basic macro definition and invocation
- Macros with 0, 1, 3, 5+ parameters
- Macro with default parameters
- Macro with local labels
- Nested macro calls
- Recursive macro detection
- Macro name conflicts
- Empty macros
- Macros with data directives
- Macros with instruction sequences

### Conditional Tests (20 tests)
- Basic if/else/endif
- Nested conditionals (3 levels deep)
- ifdef/ifndef with defined/undefined symbols
- Comparison conditionals (eq, ne, gt, lt, ge, le)
- elseif chains
- Empty conditional blocks
- Conditionals in macros
- Conditionals around includes

### Repeat Tests (5 tests)
- Basic repeat block
- Nested repeats
- Repeat with counter variable
- Zero-count repeat
- Large repeat count

### Enumeration Tests (5 tests)
- Basic enum block
- Enum with custom start
- Enum with gaps
- Nested enum (error)

---

## 📝 Syntax Specification

### Macro Definition
```asm
.macro sprite_dma, addr, count
	lda #<addr
	sta $2002
	lda #>addr
	sta $2003
	ldx count
@loop:
	lda sprite_data,x
	sta $2004
	dex
	bne @loop
.endmacro

; Usage
sprite_dma $0200, #64
```

### Conditional Assembly
```asm
.define DEBUG

.ifdef DEBUG
	.include "debug.inc"
.else
	.include "release.inc"
.endif

.if MAPPER = 0
	; NROM-specific code
.elseif MAPPER = 1
	; MMC1-specific code
.else
	.error "Unsupported mapper"
.endif
```

### Repeat Blocks
```asm
.rept 8
	.byte $00
.endr

; Generates:
; .byte $00
; .byte $00
; ... (8 times)
```

### Enumeration
```asm
.enum $00
STATE_INIT
STATE_RUNNING
STATE_PAUSED
STATE_GAMEOVER
.ende

; Generates:
; STATE_INIT = $00
; STATE_RUNNING = $01
; STATE_PAUSED = $02
; STATE_GAMEOVER = $03
```

---

## 🔀 Integration Points

### Preprocessor Integration
- Macros processed after includes
- Conditional directives processed in preprocessor pass
- Macro expansion happens before semantic analysis

### Symbol Table Integration
- Macro-local labels tracked separately
- Macro parameters shadow global symbols within macro scope
- Enum values added to symbol table

### Error Reporting
- Track macro invocation stack for error messages
- Report macro definition location for errors
- Show expanded code context in errors

---

## 🚀 GitHub Issues to Create

1. **#24:** Implement macro definition parsing (.macro/.endmacro)
2. **#25:** Implement macro expansion engine
3. **#26:** Implement macro parameters and local labels
4. **#27:** Implement conditional assembly (.if/.else/.endif)
5. **#28:** Implement symbol conditionals (.ifdef/.ifndef)
6. **#29:** Implement comparison conditionals (.ifeq, .ifne, etc.)
7. **#30:** Implement repeat blocks (.rept/.endr)
8. **#31:** Implement enumeration blocks (.enum/.ende)
9. **#32:** Implement macro default parameters
10. **#33:** Add comprehensive macro system tests

---

## 📚 Reference Implementation Research

### ASAR (SNES Assembler)
- Macro syntax: `macro name(params)` ... `endmacro`
- Parameter substitution with `<param>`
- Local labels with `?` prefix

### ca65 (6502/65816 Assembler)
- `.macro` and `.endmacro`
- Parameters accessed by position or name
- `.local` for macro-local labels
- `.exitmacro` for early exit

### Ophis (6502 Assembler)
- `.macro` directive
- Simple parameter substitution
- No nested macros

### Poppy Design Decisions
- Use `.macro/.endmacro` (explicit, clear)
- Named parameters (readable, maintainable)
- Auto-generate unique local labels with `@@` prefix
- Maximum recursion depth: 100 levels
- Maximum macro body size: 10,000 tokens

---

## 🎯 Success Criteria

Phase 2 will be complete when:
- ✅ Macro definitions can be parsed
- ✅ Macros can be invoked with arguments
- ✅ Macro parameters are substituted correctly
- ✅ Local labels in macros work correctly
- ✅ Conditional assembly (.if/.else/.endif) works
- ✅ Symbol conditionals (.ifdef/.ifndef) work
- ✅ Repeat blocks (.rept) work
- ✅ Enumeration blocks (.enum) work
- ✅ 60+ tests covering all features
- ✅ Documentation updated with examples
- ✅ Real-world macro patterns compile successfully

---

## 🔗 Related Documents

- [Roadmap](roadmap.md) - Overall project roadmap
- [Short-Term Plan](../~plans/short-term-plan.md) - Immediate goals
- [GitHub Issues](https://github.com/TheAnsarya/poppy/issues) - Issue tracker

---

*Plan created: January 11, 2026*
*Target completion: January 2026*
