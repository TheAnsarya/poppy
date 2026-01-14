# Migration Guide: ca65 to Poppy

This guide helps users transition from ca65 (part of the cc65 suite) to Poppy Assembly.

## Overview

ca65 is a powerful macro assembler commonly used for NES, SNES, and other 6502/65816 development. Poppy aims to provide similar functionality with a more streamlined syntax.

## Key Philosophical Differences

| Aspect | ca65 | Poppy |
|--------|------|-------|
| Output | Object files (linked by ld65) | Direct binary output |
| Configuration | Linker config files (.cfg) | JSON project files |
| Segments | Defined in linker config | Defined with directives |
| Build Process | Multi-step (assemble → link) | Single-step compilation |

## Directive Equivalents

| ca65 | Poppy | Notes |
|------|-------|-------|
| `.org $addr` | `.org $addr` | Same syntax |
| `.byte`, `.db` | `.db`, `.byte` | Both work |
| `.word`, `.dw` | `.dw`, `.word` | Both work |
| `.res n` | `.ds n`, `.res n` | Reserve bytes |
| `.incbin "file"` | `.incbin "file"` | Same syntax |
| `.include "file"` | `.include "file"` | Same syntax |
| `.if` / `.endif` | `.if` / `.endif` | Same syntax |
| `.ifdef` / `.ifndef` | `.ifdef` / `.ifndef` | Same syntax |
| `.define SYMBOL value` | `.define SYMBOL value` | Same syntax |
| `.macro` / `.endmacro` | `.macro` / `.endmacro` | Different parameter syntax |
| `.proc` / `.endproc` | `.scope` / `.endscope` | Renamed |
| `.scope` / `.endscope` | `.scope` / `.endscope` | Same |
| `.enum` / `.endenum` | `.enum` / `.endenum` | Same |
| `.struct` / `.endstruct` | `.struct` / `.endstruct` | Same |
| `.segment "NAME"` | `.segment "NAME", $addr, $size` | Different syntax |
| `.assert` | `.assert` | Same |
| `.error` | `.error` | Same |
| `.warning` | `.warning` | Same |

## Macro Syntax

### ca65 Style
```asm
.macro store_val addr, value
    lda #value
    sta addr
.endmacro

store_val $2000, $10
```

### Poppy Style
```asm
.macro store_val, addr, value
    lda #\value
    sta \addr
.endmacro

%store_val $2000, $10
```

### Key Differences

- Parameters listed after macro name with commas
- Parameter references use `\param` instead of bare name
- Invocation uses `%` prefix instead of calling like a function
- ca65's `.local` for macro-local labels → Poppy uses automatic scoping

### Macro Parameters Comparison

| Feature | ca65 | Poppy |
|---------|------|-------|
| Named parameters | `value` | `\value` |
| Numbered parameters | `:+` / `:-` | Not supported |
| Parameter count | `.paramcount` | `\#` |
| String parameters | `.string` | Native support |
| Token pasting | `::` | `.concat` |

## Label Syntax

### Global Labels (Same)
```asm
; ca65
MyLabel:
    lda #$00

; Poppy (same)
MyLabel:
    lda #$00
```

### Local Labels
```asm
; ca65 uses @
MyProc:
    @loop:
        dex
        bne @loop
    rts

; Poppy uses . prefix
MyProc:
    .loop:
        dex
        bne .loop
    rts
```

### Anonymous Labels
```asm
; ca65 uses : and :+/:- references
:   dex
    bne :-
    
; Poppy uses +/- with colon definition
-:  dex
    bne -
```

### .proc vs .scope
```asm
; ca65 .proc creates a scope AND exports label
.proc MyFunction
    lda #$00
    rts
.endproc

; Poppy .scope just creates scope, label is separate
MyFunction:
.scope
    lda #$00
    rts
.endscope
```

## Segments and Memory Layout

### ca65 Approach (Linker Config)
```text
# memory.cfg
MEMORY {
    ZP:     start = $00,    size = $100, type = rw;
    RAM:    start = $0200,  size = $600, type = rw;
    PRG:    start = $8000,  size = $8000, type = ro, file = %O;
}

SEGMENTS {
    ZEROPAGE: load = ZP,  type = zp;
    BSS:      load = RAM, type = bss;
    CODE:     load = PRG, type = ro;
}
```

```asm
; Assembly file
.segment "ZEROPAGE"
temp: .res 2

.segment "CODE"
Reset:
    lda #$00
```

### Poppy Approach (Direct)
```asm
; Zero page variables
.org $00
temp: .ds 2

; Code section
.org $8000
Reset:
    lda #$00
```

Or with segments:
```asm
.segment "ZEROPAGE", $00, $100
temp: .ds 2

.segment "CODE", $8000, $8000
Reset:
    lda #$00
```

## Conditional Assembly

### ca65
```asm
.if .defined(DEBUG)
    jsr DebugPrint
.elseif .defined(VERBOSE)
    jsr VerbosePrint
.else
    ; Release mode
.endif

.ifdef FEATURE_A
    ; Feature A code
.endif

.ifblank param
    ; Parameter is blank
.endif
```

### Poppy
```asm
.ifdef DEBUG
    jsr DebugPrint
.elseif VERBOSE
    jsr VerbosePrint
.else
    ; Release mode
.endif

.ifdef FEATURE_A
    ; Feature A code
.endif

; Note: .ifblank not currently supported
; Use .if \# > 0 for parameter checks
```

## Expressions and Operators

### Byte Selection
```asm
; ca65
lda #<MyLabel       ; Low byte
lda #>MyLabel       ; High byte
lda #^MyLabel       ; Bank byte

; Poppy (same syntax)
lda #<(MyLabel)     ; Low byte (parentheses recommended)
lda #>(MyLabel)     ; High byte
lda #^(MyLabel)     ; Bank byte
```

### Built-in Functions

| ca65 | Poppy | Notes |
|------|-------|-------|
| `.lobyte(expr)` | `<(expr)` | Low byte |
| `.hibyte(expr)` | `>(expr)` | High byte |
| `.bankbyte(expr)` | `^(expr)` | Bank byte |
| `.loword(expr)` | Expr & $ffff | Mask manually |
| `.hiword(expr)` | Expr >> 16 | Shift manually |
| `.strlen("str")` | Not yet | Planned |
| `.sprintf(...)` | Not yet | Planned |
| `.defined(sym)` | `.ifdef sym` | Use conditional |
| `.const(sym)` | N/A | Not needed |

## Imports and Exports

### ca65 (Multi-file linking)
```asm
; file1.s
.export MyFunction
.proc MyFunction
    rts
.endproc

; file2.s
.import MyFunction
    jsr MyFunction
```

### Poppy (Single compilation unit)
```asm
; main.pasm - all files compiled together
.include "file1.pasm"
.include "file2.pasm"

; Symbols are automatically visible after include
jsr MyFunction
```

For multi-file projects, use `poppy.json`:
```json
{
    "name": "MyProject",
    "sources": ["file1.pasm", "file2.pasm"],
    "main": "main.pasm"
}
```

## iNES Header

### ca65
```asm
.segment "HEADER"
    .byte "NES", $1A        ; iNES magic
    .byte 2                 ; PRG-ROM banks
    .byte 1                 ; CHR-ROM banks
    .byte %00000001         ; Flags 6
    .byte %00000000         ; Flags 7
    .byte 0, 0, 0, 0, 0, 0, 0, 0
```

### Poppy
```asm
; Single directive generates entire header
.ines {"mapper": 0, "prg": 2, "chr": 1, "mirroring": "vertical"}
```

## Repeat Blocks

### ca65
```asm
.repeat 16, i
    .byte i * 2
.endrepeat
```

### Poppy
```asm
.repeat 16, i
    .db i * 2
.endrepeat
```

## Enumerations

### ca65
```asm
.enum
    STATE_IDLE
    STATE_RUNNING
    STATE_PAUSED = 10
    STATE_DONE
.endenum
```

### Poppy
```asm
.enum
    STATE_IDLE
    STATE_RUNNING
    STATE_PAUSED = 10
    STATE_DONE
.endenum
```

(Same syntax)

## Example Conversion

### ca65 Original (with linker config)
```asm
; game.s
.include "nes.inc"

.segment "ZEROPAGE"
player_x: .res 1
player_y: .res 1

.segment "CODE"

.proc reset
    sei
    cld
    ldx #$ff
    txs
    
    lda #$00
    sta player_x
    sta player_y
    
    @loop:
        jmp @loop
.endproc

.proc nmi
    rti
.endproc

.segment "VECTORS"
    .word nmi
    .word reset
    .word 0
```

### Poppy Equivalent
```asm
; game.pasm
.include "nes.inc"

.ines {"mapper": 0, "prg": 1, "chr": 0}

; Zero page
.org $00
player_x: .ds 1
player_y: .ds 1

; Code
.org $8000

reset:
.scope
    sei
    cld
    ldx #$ff
    txs
    
    lda #$00
    sta player_x
    sta player_y
    
.loop:
    jmp .loop
.endscope

nmi:
    rti

; Vectors
.org $fffa
    .dw nmi
    .dw reset
    .dw 0
```

## Feature Comparison

| Feature | ca65 | Poppy |
|---------|------|-------|
| 6502 Support | ✅ | ✅ |
| 65816 Support | ✅ | ✅ |
| Macros | ✅ | ✅ |
| Structs | ✅ | ✅ |
| Enums | ✅ | ✅ |
| Segments | ✅ (via linker) | ✅ (directives) |
| Multi-file | ✅ (linking) | ✅ (includes/project) |
| Object Files | ✅ | ❌ |
| Debug Info | ✅ | ✅ |
| String Functions | ✅ | Partial |
| NES Support | ✅ | ✅ |
| Game Boy Support | ❌ | ✅ |
| VS Code Integration | ❌ | ✅ |

## Tips for Migration

1. **Replace local label `@` prefix** with `.` prefix
2. **Convert macro syntax** - use `\param` references and `%` invocation
3. **Remove segment/linker config** - use `.org` or inline `.segment`
4. **Replace `.proc`** with label + `.scope`
5. **Use `.ines` directive** instead of manual header bytes
6. **Combine source files** with `.include` or project file
7. **Replace `.import`/`.export`** - symbols are global by default
8. **Use `.ifdef`** instead of `.if .defined()`

## Common Gotchas

### 1. Local Label Scope
ca65's `@labels` are scoped to `.proc`; Poppy's `.labels` are scoped to the previous global label.

### 2. Macro Invocation
ca65: `my_macro arg1, arg2`
Poppy: `%my_macro arg1, arg2`

### 3. No Linker
Poppy outputs directly to binary - no intermediate object files or linking step.

### 4. Segment Behavior
ca65 segments can be non-contiguous and interleaved; Poppy segments are more linear.

## Getting Help

- Check the [Poppy documentation](../README.md)
- See [example projects](../examples/)
- File issues on [GitHub](https://github.com/TheAnsarya/poppy/issues)
