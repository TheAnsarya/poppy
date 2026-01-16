# Migration Guide: ASM68K to Poppy

This guide helps users transition from ASM68K and similar M68000 assemblers to Poppy Assembly for Sega Genesis development.

## Overview

ASM68K is one of the original assemblers used for Sega Genesis development. Poppy provides M68000 support with a modern, cross-platform approach.

## Key Philosophical Differences

| Aspect | ASM68K | Poppy |
|--------|--------|-------|
| Platform | DOS/Windows | Cross-platform |
| Output | Direct binary | Direct binary |
| Case | Case-insensitive | Case-sensitive |
| Syntax | Motorola standard | Unified Poppy syntax |
| Big-endian | Native | Automatic handling |

## Directive Equivalents

| ASM68K | Poppy | Notes |
|--------|-------|-------|
| `org $addr` | `.org $addr` | Same concept |
| `dc.b`, `dc.w`, `dc.l` | `.db`, `.dw`, `.dl` | Renamed |
| `ds.b n`, `ds.w n` | `.ds n`, `.ds n*2` | Reserve space |
| `dcb.b n, v` | `.fill n, v` | Fill with value |
| `incbin "file"` | `.incbin "file"` | Same |
| `include "file"` | `.include "file"` | Same |
| `if` / `endif` | `.if` / `.endif` | Dot prefix |
| `ifd` / `ifnd` | `.ifdef` / `.ifndef` | Renamed |
| `equ` / `=` | `.define` / `=` | Both supported |
| `set` | `.set` | Reassignable |
| `macro` / `endm` | `.macro` / `.endmacro` | Different end |
| `rept` / `endr` | `.rept` / `.endr` | Same concept |
| `even` | `.align 2` | Alignment |
| `cnop 0, 4` | `.align 4` | Word alignment |
| `fail` | `.error` | Renamed |
| `inform 0` | `.print` | Renamed |

## Instruction Syntax

### Data Movement

| ASM68K | Poppy | Notes |
|--------|-------|-------|
| `move.b d0, d1` | `move.b d0, d1` | Same |
| `move.w #$1234, d0` | `move.w #$1234, d0` | Same |
| `move.l (a0), d0` | `move.l (a0), d0` | Same |
| `move.l (a0)+, d0` | `move.l (a0)+, d0` | Post-increment |
| `move.l -(a0), d0` | `move.l -(a0), d0` | Pre-decrement |
| `move.l 4(a0), d0` | `move.l 4(a0), d0` | Offset |
| `move.l 4(a0, d1.w), d0` | `move.l 4(a0, d1.w), d0` | Indexed |
| `lea label(pc), a0` | `lea label(pc), a0` | PC-relative |
| `movem.l d0-d7/a0-a6, -(sp)` | `movem.l d0-d7/a0-a6, -(sp)` | Multiple regs |

### Arithmetic

| ASM68K | Poppy | Notes |
|--------|-------|-------|
| `add.w d0, d1` | `add.w d0, d1` | Same |
| `addi.w #$10, d0` | `addi.w #$10, d0` | Add immediate |
| `addq.w #4, d0` | `addq.w #4, d0` | Quick add |
| `sub.l d0, d1` | `sub.l d0, d1` | Same |
| `mulu.w d0, d1` | `mulu.w d0, d1` | Unsigned multiply |
| `divu.w d0, d1` | `divu.w d0, d1` | Unsigned divide |

### Control Flow

| ASM68K | Poppy | Notes |
|--------|-------|-------|
| `bra label` | `bra label` | Branch always |
| `bra.s label` | `bra.s label` | Short branch |
| `beq label` | `beq label` | Branch if equal |
| `bne.s label` | `bne.s label` | Short branch |
| `jsr subroutine` | `jsr subroutine` | Jump subroutine |
| `jmp label` | `jmp label` | Jump |
| `rts` | `rts` | Return |
| `dbra d0, label` | `dbra d0, label` | Decrement branch |

## Size Suffixes

Both assemblers use the same size suffixes:

| Suffix | Size | Bytes |
|--------|------|-------|
| `.b` | Byte | 1 |
| `.w` | Word | 2 |
| `.l` | Long | 4 |

## Label Syntax

### Global Labels (Same)
```asm
; ASM68K
Start:
    move.w #$2700, sr

; Poppy (same)
Start:
    move.w #$2700, sr
```

### Local Labels
```asm
; ASM68K uses . prefix
WaitVBlank:
.loop:
    btst #3, (VDP_Control).l
    beq.s .loop
    rts

; Poppy uses @ prefix
WaitVBlank:
@loop:
    btst #3, (VDP_Control).l
    beq.s @loop
    rts
```

## Macro Syntax

### ASM68K Style
```asm
PUSH    macro   reg
    move.l  \reg, -(sp)
    endm

POP     macro   reg
    move.l  (sp)+, \reg
    endm

    PUSH    d0
    POP     d0
```

### Poppy Style
```asm
.macro PUSH, reg
    move.l  \reg, -(sp)
.endmacro

.macro POP, reg
    move.l  (sp)+, \reg
.endmacro

    %PUSH d0
    %POP d0
```

### Multi-Parameter Macros

```asm
; ASM68K
VDP_WRITE   macro   reg, value
    move.w  #((\reg<<8)|$80|\value), (VDP_Control).l
    endm

    VDP_WRITE 0, $04

; Poppy
.macro VDP_WRITE, reg, value
    move.w  #((\reg << 8) | $80 | \value), (VDP_Control).l
.endmacro

    %VDP_WRITE 0, $04
```

## Genesis ROM Structure

### ASM68K Style
```asm
    org $000000

; Vector table
    dc.l $00ffe000      ; Initial SP
    dc.l Start          ; Initial PC
    dc.l BusError       ; Bus error
    dc.l AddressError   ; Address error
    ; ... more vectors ...

; ROM header at $100
    org $000100
    dc.b "SEGA GENESIS    "
    dc.b "(C)SEGA 2026.JAN"
    ; ... rest of header ...

    org $000200
Start:
    move.w #$2700, sr
    ; ...
```

### Poppy Style
```asm
.target "genesis"
.cpu "m68000"

.org $000000

; Vector table
    .dl $00ffe000       ; Initial SP
    .dl Start           ; Initial PC
    .dl BusError        ; Bus error
    .dl AddressError    ; Address error
    ; ... more vectors ...

; Header configured in poppy.json or with directives
.genesis_title_domestic "MY GAME"
.genesis_title_overseas "MY GAME"
.genesis_copyright "(C)SEGA 2026.JAN"

.org $000200
Start:
    move.w #$2700, sr
    ; ...
```

## VDP Access

### ASM68K Style
```asm
VDP_Data    equ $C00000
VDP_Control equ $C00004

; Write to CRAM
    lea VDP_Control, a5
    lea VDP_Data, a6
    move.l #$C0000000, (a5)   ; CRAM write address 0
    move.w #$0EEE, (a6)       ; White color
```

### Poppy Style
```asm
.define VDP_Data    $c00000
.define VDP_Control $c00004

; Write to CRAM
    lea VDP_Control, a5
    lea VDP_Data, a6
    move.l #$c0000000, (a5)   ; CRAM write address 0
    move.w #$0eee, (a6)       ; White color
```

## Conditional Assembly

### ASM68K Style
```asm
DEBUG   equ 1

    if DEBUG
    inform 0, "Debug build"
    endif

    ifd DEBUG
    ; Debug code
    endif
```

### Poppy Style
```asm
.define DEBUG 1

.if DEBUG
    .print "Debug build"
.endif

.ifdef DEBUG
    ; Debug code
.endif
```

## Quick Reference Card

```
ASM68K                   Poppy
──────                   ─────
org $1234                .org $1234
dc.b $ff                 .db $ff
dc.w $1234               .dw $1234
dc.l $12345678           .dl $12345678
ds.b 10                  .ds 10
ds.w 10                  .ds 20
incbin "x.bin"           .incbin "x.bin"
include "x.asm"          .include "x.asm"
sym equ 5                .define sym 5
sym = 5                  sym = 5
macro foo                .macro foo
endm                     .endmacro
foo                      %foo
if cond                  .if cond
else                     .else
endif                    .endif
ifd sym                  .ifdef sym
rept 4                   .rept 4
endr                     .endr
even                     .align 2
.localLabel:             @localLabel:
fail "msg"               .error "msg"
```

## Example: Complete Migration

### ASM68K Original
```asm
    org $000000

; Vectors
    dc.l $00ffe000
    dc.l Start
    dcb.l 62, Exception

    org $000100
; Header
    dc.b "SEGA GENESIS    "
    dc.b "(C)SEGA 2026.JAN"
    dc.b "MY GAME                                         "
    dc.b "MY GAME                                         "
    dc.b "GM 00000000-00"
    dc.w $0000
    dc.b "J               "
    dc.l $00000000
    dc.l $003fffff
    dc.l $00ff0000
    dc.l $00ffffff
    dc.b "            "
    dc.b "            "
    dc.b "                                        "
    dc.b "JUE             "

    org $000200
Start:
    move.w #$2700, sr
    lea $ffe000, sp

    ; Initialize VDP
    lea VDPRegs(pc), a0
    lea $c00004, a1
    moveq #18, d0
.initVDP:
    move.w (a0)+, (a1)
    dbra d0, .initVDP

.mainLoop:
    bra.s .mainLoop

Exception:
    rte

VDPRegs:
    dc.w $8004, $8134, $8230, $8328
    dc.w $8407, $857c, $8600, $8700
    dc.w $8803, $8900, $8a00, $8b00
    dc.w $8c81, $8d3f, $8e00, $8f02
    dc.w $9001, $9100, $9200
```

### Poppy Equivalent
```asm
.target "genesis"
.cpu "m68000"

.org $000000

; Vectors
    .dl $00ffe000
    .dl Start
    .fill 62 * 4, Exception

; Header directives
.genesis_title_domestic "MY GAME"
.genesis_title_overseas "MY GAME"
.genesis_copyright "(C)SEGA 2026.JAN"
.genesis_serial "GM 00000000-00"
.genesis_region "JUE"

.org $000200
Start:
    move.w #$2700, sr
    lea $ffe000, sp

    ; Initialize VDP
    lea VDPRegs(pc), a0
    lea $c00004, a1
    moveq #18, d0
@initVDP:
    move.w (a0)+, (a1)
    dbra d0, @initVDP

@mainLoop:
    bra.s @mainLoop

Exception:
    rte

VDPRegs:
    .dw $8004, $8134, $8230, $8328
    .dw $8407, $857c, $8600, $8700
    .dw $8803, $8900, $8a00, $8b00
    .dw $8c81, $8d3f, $8e00, $8f02
    .dw $9001, $9100, $9200
```

### poppy.json
```json
{
    "name": "mygame",
    "target": "genesis",
    "cpu": "m68000",
    "output": {
        "format": "gen",
        "filename": "mygame.bin"
    },
    "header": {
        "domesticTitle": "MY GAME",
        "overseasTitle": "MY GAME",
        "copyright": "(C)SEGA 2026.JAN",
        "serial": "GM 00000000-00",
        "region": "JUE"
    }
}
```

## Common Migration Tasks

### 1. Update Project Setup
- Create poppy.json
- Move header info to config or directives

### 2. Update Directives
- Add dot prefix to all directives
- Change `dc.` to `.d` (dc.b → .db)
- Change `ds.` to `.ds`

### 3. Update Macros
- Add dot prefix to macro/endm
- Change `endm` to `.endmacro`
- Add `%` prefix to invocations

### 4. Update Labels
- Change `.localLabel` to `@localLabel`

### 5. Lowercase Hex
- Convert `$FFFF` to `$ffff` (Poppy convention)

## Resources

- [Poppy User Manual](user-manual.md)
- [PASM Syntax Reference](pasm-file-format.md)
- [Sega Genesis Manual](https://segaretro.org/Sega_Mega_Drive/Technical_specifications)
- [Plutiedev](https://plutiedev.com/) - Genesis programming tutorials
