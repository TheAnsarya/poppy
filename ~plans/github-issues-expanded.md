# 📋 GitHub Issues - Expanded Issue List

> Comprehensive issue list for Poppy Compiler development

**Repository:** TheAnsarya/poppy
**Created:** January 11, 2026

---

## 🎯 Epic: Include System (#E1)

### Issue: Basic Include Directive
```
Title: Implement .include directive for file inclusion
Labels: phase-6, enhancement

## Description
Implement the .include directive to include other assembly files.

## Acceptance Criteria
- [ ] Parse .include "filename.asm" syntax
- [ ] Recursively process included files
- [ ] Track include paths for error reporting
- [ ] Prevent circular includes
- [ ] Support relative and absolute paths
- [ ] Unit tests for include resolution
```

### Issue: Binary Include Directive
```
Title: Implement .incbin directive for binary data inclusion
Labels: phase-6, enhancement

## Description
Implement the .incbin directive to include binary files directly.

## Acceptance Criteria
- [ ] Parse .incbin "filename.bin" syntax
- [ ] Support optional offset parameter: .incbin "file.bin", $100
- [ ] Support optional length parameter: .incbin "file.bin", $100, $200
- [ ] Support for skipping/range: .incbin "file.bin", skip=$10, read=$100
- [ ] Error handling for missing files
- [ ] Unit tests for binary inclusion
```

### Issue: Asset Include with Conversion
```
Title: Implement asset file inclusion with auto-conversion
Labels: phase-6, enhancement, research

## Description
Include asset files (graphics, maps, etc.) that get converted at compile time.

## Acceptance Criteria
- [ ] Define asset conversion pipeline
- [ ] Support CHR/tile graphics conversion
- [ ] Support map/tilemap data conversion
- [ ] Support palette file conversion
- [ ] Configurable conversion parameters
- [ ] Integration with external tools (optional)
```

---

## 🎯 Epic: Label System Enhancement (#E2)

### Issue: Local Labels
```
Title: Implement local labels with scope
Labels: phase-6, enhancement

## Description
Support local labels that are scoped to their parent global label.

## Acceptance Criteria
- [ ] Support @local or .local syntax for local labels
- [ ] Scope local labels to nearest preceding global label
- [ ] Allow same local label name in different scopes
- [ ] Forward reference to local labels within scope
- [ ] Error for out-of-scope local label references
```

### Issue: Anonymous Labels
```
Title: Implement anonymous labels (+ and -)
Labels: phase-6, enhancement

## Description
Support anonymous forward (+) and backward (-) labels.

## Acceptance Criteria
- [ ] + label for forward reference
- [ ] - label for backward reference
- [ ] Multiple + or - for farther references (++, ---)
- [ ] Support in branch instructions
- [ ] Proper address calculation
```

### Issue: Named Anonymous Labels
```
Title: Implement named anonymous labels
Labels: phase-6, enhancement

## Description
Support named plus/minus labels like +skip, -loop.

## Acceptance Criteria
- [ ] +name creates forward-referenceable point
- [ ] -name creates backward-referenceable point
- [ ] Allow reuse of same name
- [ ] Resolve to nearest matching label
```

### Issue: Auto-Generated Routine Names
```
Title: Automatically name routines from JSR/JSL targets
Labels: phase-6, enhancement, research

## Description
When a JSR/JSL instruction targets an address without a label,
optionally auto-generate a routine name based on address.

## Acceptance Criteria
- [ ] Track all JSR/JSL targets
- [ ] Generate sub_XXXX labels for unlabeled targets
- [ ] Optionally output symbol file with auto names
- [ ] Support custom naming patterns
- [ ] Listing file shows auto-generated names
```

---

## 🎯 Epic: Comment System (#E3)

### Issue: Multi-line Comments
```
Title: Implement multi-line comment syntax
Labels: phase-3, enhancement

## Description
Support C-style /* */ multi-line comments.

## Acceptance Criteria
- [ ] /* ... */ syntax for multi-line
- [ ] Nested comment support (optional)
- [ ] Proper line number tracking across comments
- [ ] Comments in listing output
```

### Issue: Documentation Comments
```
Title: Implement documentation comments
Labels: phase-6, enhancement

## Description
Support special comment syntax for documentation generation.

## Acceptance Criteria
- [ ] ;; or /** */ for doc comments
- [ ] Associate doc comments with labels/routines
- [ ] Export documentation data
- [ ] Support markdown in doc comments
```

---

## 🎯 Epic: Directive System (#E4)

### Issue: Target System Directives
```
Title: Implement target system directives
Labels: phase-6, enhancement

## Description
Directives to specify target system and memory mapping.

## Acceptance Criteria
- [ ] .nes, .snes, .gb directives for target
- [ ] .lorom, .hirom, .exhirom for SNES mapping
- [ ] .mapper N for NES mapper selection
- [ ] Auto-configure settings based on target
```

### Issue: Memory Segment Directives
```
Title: Implement memory segment directives
Labels: phase-6, enhancement

## Description
Define memory segments for code organization.

## Acceptance Criteria
- [ ] .segment "name" directive
- [ ] .bank N directive for bank switching
- [ ] .base address for output relocation
- [ ] Segment configuration (.segment "CODE", $8000)
- [ ] Cross-segment references
```

### Issue: Assertion Directives
```
Title: Implement assembly-time assertions
Labels: phase-6, enhancement

## Description
Add directives for compile-time validation.

## Acceptance Criteria
- [ ] .assert condition, "message"
- [ ] .error "message" - unconditional error
- [ ] .warning "message" - unconditional warning
- [ ] Support expressions in conditions
```

### Issue: Alignment Directives
```
Title: Implement alignment and padding directives
Labels: phase-6, enhancement

## Description
Directives for memory alignment and padding.

## Acceptance Criteria
- [ ] .align N - align to N-byte boundary
- [ ] .pad address - pad to specific address
- [ ] .fill count, value - fill with value
- [ ] .skip count - skip bytes (don't output)
```

### Issue: Repeat Directive
```
Title: Implement repeat/rept directive
Labels: phase-6, enhancement

## Description
Repeat a block of code N times.

## Acceptance Criteria
- [ ] .rept count ... .endr syntax
- [ ] Access to iteration counter
- [ ] Nested repeats
- [ ] Use in data tables
```

---

## 🎯 Epic: Expression System (#E5)

### Issue: String Functions
```
Title: Implement string functions in expressions
Labels: phase-6, enhancement

## Description
Add string manipulation functions for expressions.

## Acceptance Criteria
- [ ] strlen("string") - get string length
- [ ] defined(symbol) - check if symbol exists
- [ ] bank(label) - get bank of label
- [ ] high()/low() - byte extraction
```

### Issue: Math Functions
```
Title: Implement math functions in expressions
Labels: phase-6, enhancement

## Description
Add mathematical functions for expressions.

## Acceptance Criteria
- [ ] min(a, b), max(a, b)
- [ ] abs(x) - absolute value
- [ ] log2(x) - for bit counting
- [ ] pow(base, exp) - power
```

---

## 🎯 Epic: Project File System (#E6)

### Issue: Project File Format Design
```
Title: Design project file format (.ppy or .poppy)
Labels: phase-6, enhancement, research

## Description
Design a project file format that describes a complete build.

## Example Format (YAML-based):
```yaml
project: DragonWarrior
version: 1.0.0
target: nes

# Source files in compilation order
sources:
  - src/main.asm
  - src/graphics.asm
  - src/sound.asm

# Asset files with conversion settings
assets:
  - path: assets/sprites.chr
    type: chr
    output: bin/sprites.bin
  - path: assets/tilemap.tmx
    type: tilemap
    output: bin/maps.bin

# Build settings
output:
  format: ines
  mapper: 0
  prg_size: 32768
  chr_size: 8192
  file: build/game.nes

# Include paths
includes:
  - include/
  - lib/
```

## Acceptance Criteria
- [ ] Define YAML/JSON project file schema
- [ ] Parse project files
- [ ] Build from project file
- [ ] Watch mode for development
- [ ] Dependency tracking
```

### Issue: Asset Pipeline Integration
```
Title: Integrate asset conversion pipeline
Labels: phase-6, enhancement

## Description
Support automatic asset conversion during build.

## Acceptance Criteria
- [ ] Define asset type handlers
- [ ] Support external conversion tools
- [ ] Built-in CHR conversion
- [ ] Caching for faster rebuilds
- [ ] Configurable conversion options
```

---

## 🎯 Epic: 65816 Support (#E7)

### Issue: 65816 Instruction Set
```
Title: Implement 65816 instruction encoding
Labels: phase-5, enhancement

## Description
Add complete 65816 instruction set support.

## Acceptance Criteria
- [ ] All 65816 opcodes
- [ ] All 24 addressing modes
- [ ] Block move (MVN/MVP)
- [ ] Stack relative addressing
- [ ] Direct page indirect long
```

### Issue: 65816 Mode Tracking
```
Title: Implement M/X flag mode tracking
Labels: phase-5, enhancement

## Description
Track processor mode for variable-size operands.

## Acceptance Criteria
- [ ] .a8/.a16 directives for accumulator size
- [ ] .i8/.i16 directives for index size
- [ ] .smart mode for auto-detection
- [ ] SEP/REP instruction handling
- [ ] Warning for mode mismatches
```

### Issue: Long Addressing Support
```
Title: Implement 24-bit long addressing
Labels: phase-5, enhancement

## Description
Support 24-bit bank:address addressing.

## Acceptance Criteria
- [ ] $BBHHLL long address syntax
- [ ] JML, JSL long jumps
- [ ] LDA.l, STA.l long loads/stores
- [ ] Bank byte operators (^)
```

---

## 🎯 Epic: Error Handling (#E8)

### Issue: Enhanced Error Messages
```
Title: Implement contextual error messages
Labels: phase-7, enhancement

## Description
Provide helpful error messages with context and suggestions.

## Acceptance Criteria
- [ ] Show source line with error
- [ ] Caret (^) pointing to error location
- [ ] Suggest fixes for common errors
- [ ] Similar symbol suggestions for typos
- [ ] Include stack for macro expansion errors
```

### Issue: Warning System
```
Title: Implement warning levels and controls
Labels: phase-7, enhancement

## Description
Add configurable warning system.

## Acceptance Criteria
- [ ] Warning levels (0-3)
- [ ] -W flags for specific warnings
- [ ] .nowarn directive to suppress
- [ ] Treat warnings as errors option
```

---

## 🎯 Epic: Output Formats (#E9)

### Issue: iNES 2.0 Support
```
Title: Implement iNES 2.0 header generation
Labels: phase-7, enhancement

## Description
Generate iNES 2.0 format headers.

## Acceptance Criteria
- [ ] iNES 2.0 header structure
- [ ] Extended mapper support (> 255)
- [ ] Submapper field
- [ ] PRG/CHR RAM size fields
- [ ] VS System / PlayChoice fields
```

### Issue: Symbol File Output
```
Title: Generate debug symbol files
Labels: phase-7, enhancement

## Description
Output symbol tables for debuggers.

## Acceptance Criteria
- [ ] FCEUX .nl format
- [ ] Mesen .mlb format
- [ ] Generic .sym format
- [ ] Include all labels and constants
```

### Issue: Map File Output
```
Title: Generate memory map files
Labels: phase-7, enhancement

## Description
Output detailed memory usage information.

## Acceptance Criteria
- [ ] List all segments
- [ ] Show segment sizes and ranges
- [ ] List symbols per segment
- [ ] Calculate free space
```

---

## 📊 Priority Matrix

| Priority | Issue | Phase | Effort |
|----------|-------|-------|--------|
| P0 | Include directive | 6 | Medium |
| P0 | Binary include | 6 | Low |
| P0 | Local labels | 6 | Medium |
| P1 | Anonymous labels | 6 | Medium |
| P1 | Target directives | 6 | Medium |
| P1 | Enhanced errors | 7 | High |
| P2 | Project file format | 6 | High |
| P2 | Asset pipeline | 6 | High |
| P2 | 65816 instructions | 5 | High |
| P3 | Multi-line comments | 3 | Low |
| P3 | Doc comments | 6 | Medium |
| P3 | String functions | 6 | Low |

---

## 🏷️ Additional Labels to Create

| Label | Color | Description |
|-------|-------|-------------|
| `epic` | `#3e4b9e` | Epic/feature group |
| `include-system` | `#c2e0c6` | Include directive features |
| `labels` | `#fef2c0` | Label system features |
| `directives` | `#d4c5f9` | Directive features |
| `output` | `#f9d0c4` | Output format features |
| `65816` | `#fbca04` | 65816/SNES specific |
| `project` | `#0e8a16` | Project file system |

---

