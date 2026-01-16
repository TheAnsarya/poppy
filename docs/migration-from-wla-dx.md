# Migration Guide: WLA-DX to Poppy

This guide helps users transition from WLA-DX to Poppy Assembly for Z80, 6502, and 65816 development (Master System, Game Boy, SNES, etc.).

## Overview

WLA-DX is a popular multi-platform assembler supporting Z80, 6502, 65816, and other CPUs. Poppy provides similar multi-platform support with a more unified syntax.

## Key Philosophical Differences

| Aspect | WLA-DX | Poppy |
|--------|--------|-------|
| Output | Object files (linked by WLALINK) | Direct binary output |
| Configuration | Linker scripts | JSON project files |
| Build Process | Multi-step (assemble → link) | Single-step compilation |
| Syntax | Architecture-specific modes | Unified syntax |

## Directive Equivalents

| WLA-DX | Poppy | Notes |
|--------|-------|-------|
| `.org $addr` | `.org $addr` | Same syntax |
| `.db`, `.byte` | `.db`, `.byte` | Same |
| `.dw`, `.word` | `.dw`, `.word` | Same |
| `.ds n` | `.ds n` | Reserve space |
| `.incbin "file"` | `.incbin "file"` | Same |
| `.include "file"` | `.include "file"` | Same |
| `.if` / `.endif` | `.if` / `.endif` | Same |
| `.ifdef` / `.ifndef` | `.ifdef` / `.ifndef` | Same |
| `.define` / `.equ` | `.define` / `=` | Same concepts |
| `.macro` / `.endm` | `.macro` / `.endmacro` | Different end keyword |
| `.rept` / `.endr` | `.rept` / `.endr` | Same |
| `.enum` / `.ende` | `.enum` / `.ende` | Same |
| `.struct` / `.endst` | `.struct` / `.endstruct` | Different end keyword |
| `.section` | `.segment` | Different keyword |
| `.bank n` | `.bank n` | Same |
| `.slot n` | N/A | Use `.org` instead |
| `.fail` | `.error` | Renamed |
| `.printt` | `.print` | Renamed |
| `.rombanks` / `.rombankmap` | N/A | Use project file |
| `.memorymap` | N/A | Use project file |

## Memory Map Configuration

### WLA-DX Style
```asm
.memorymap
    defaultslot 0
    slotsize $4000
    slot 0 $0000
    slot 1 $4000
    slot 2 $8000
.endme

.rombankmap
    bankstotal 4
    banksize $4000
    banks 4
.endro
```

### Poppy Style (poppy.json)
```json
{
    "name": "mygame",
    "target": "mastersystem",
    "cpu": "z80",
    "output": {
        "format": "sms",
        "filename": "mygame.sms"
    },
    "romSize": 65536
}
```

## Macro Syntax

### WLA-DX Style
```asm
.macro LOAD_TO_VRAM args address, length, dest
    ld hl, \1
    ld de, \3
    ld bc, \2
    call LoadVRAM
.endm

LOAD_TO_VRAM TileData, TileData_End - TileData, $4000
```

### Poppy Style
```asm
.macro LOAD_TO_VRAM, address, length, dest
    ld hl, \address
    ld de, \dest
    ld bc, \length
    call LoadVRAM
.endmacro

%LOAD_TO_VRAM TileData, TileData_End - TileData, $4000
```

### Key Differences

| Feature | WLA-DX | Poppy |
|---------|--------|-------|
| Definition | `.macro name args p1, p2` | `.macro name, p1, p2` |
| End | `.endm` | `.endmacro` |
| Parameters | `\1`, `\2` or `\name` | `\name` only |
| Invocation | `MACRO args` | `%MACRO args` |
| Optional args | `?param` | `param=default` |

## Label Syntax

### Global Labels (Same)
```asm
; WLA-DX
MyLabel:
    ld a, $00

; Poppy (same)
MyLabel:
    ld a, $00
```

### Local Labels
```asm
; WLA-DX uses _ prefix
MyProc:
_loop:
    dec b
    jr nz, _loop
    ret

; Poppy uses @ prefix
MyProc:
@loop:
    dec b
    jr nz, @loop
    ret
```

### Anonymous Labels
```asm
; WLA-DX uses +/- prefixes
-:
    dec b
    jr nz, -
    ret

; Poppy (same syntax)
-:
    dec b
    jr nz, -
    ret
```

## Section/Bank Management

### WLA-DX Style
```asm
.bank 0 slot 0
.org $0000

.section "Header" free
HeaderData:
    .db "GAME"
.ends

.section "Main" free
Start:
    di
    im 1
.ends
```

### Poppy Style
```asm
.bank 0
.org $0000

HeaderData:
    .db "GAME"

Start:
    di
    im 1
```

## Structs and Enums

### WLA-DX Struct
```asm
.struct Player
    X   db
    Y   db
    HP  dw
.endst

.enum $c000
    player1 instanceof Player
    player2 instanceof Player
.ende
```

### Poppy Equivalent
```asm
.struct Player
    X:   .ds 1
    Y:   .ds 1
    HP:  .ds 2
.endstruct

.enum $c000
    player1: .ds Player.size
    player2: .ds Player.size
.ende
```

## Conditional Assembly

### WLA-DX Style
```asm
.ifdef DEBUG
    .printt "Debug mode enabled\n"
.else
    .define RELEASE 1
.endif
```

### Poppy Style
```asm
.ifdef DEBUG
    .print "Debug mode enabled"
.else
    .define RELEASE 1
.endif
```

## Platform-Specific Examples

### Master System (Z80)

#### WLA-DX
```asm
.memorymap
    defaultslot 0
    slotsize $4000
    slot 0 $0000
.endme

.rombankmap
    bankstotal 2
    banksize $4000
    banks 2
.endro

.bank 0 slot 0
.org $0000
    di
    im 1
    jp Start

.org $0038
IRQHandler:
    push af
    in a, ($bf)
    pop af
    ei
    reti

.org $0066
NMIHandler:
    retn

Start:
    ld sp, $dff0
    call InitVDP
    ; ...
```

#### Poppy
```asm
.target "mastersystem"
.cpu "z80"

.org $0000
    di
    im 1
    jp Start

.org $0038
IRQHandler:
    push af
    in a, ($bf)
    pop af
    ei
    reti

.org $0066
NMIHandler:
    retn

Start:
    ld sp, $dff0
    call InitVDP
    ; ...
```

### Genesis (M68000)

#### WLA-DX (using WLA-68K)
```asm
.memorymap
    defaultslot 0
    slotsize $400000
    slot 0 $000000
.endme

.bank 0 slot 0
.org $000000
    dc.l $00ffe000       ; Initial SP
    dc.l Start           ; Initial PC
```

#### Poppy
```asm
.target "genesis"
.cpu "m68000"

.org $000000
    .dl $00ffe000        ; Initial SP
    .dl Start            ; Initial PC
```

## Quick Reference Card

```
WLA-DX                   Poppy
──────                   ─────
.org $1234               .org $1234
.db $ff                  .db $ff
.dw $1234                .dw $1234
.ds 10                   .ds 10
.incbin "x.bin"          .incbin "x.bin"
.include "x.asm"         .include "x.asm"
.define x 5              .define x 5
x .equ 5                 x = 5
.macro foo               .macro foo
.endm                    .endmacro
foo                      %foo
.if cond                 .if cond
.else                    .else
.endif                   .endif
.ifdef                   .ifdef
.rept 4                  .rept 4
.endr                    .endr
.enum $c000              .enum $c000
.ende                    .ende
.struct Foo              .struct Foo
.endst                   .endstruct
_localLabel:             @localLabel:
.fail "msg"              .error "msg"
.printt "msg"            .print "msg"
```

## Common Migration Tasks

### 1. Remove Memory Map
- Delete `.memorymap`/`.endme` blocks
- Delete `.rombankmap`/`.endro` blocks
- Create poppy.json with target settings

### 2. Update Sections
- Convert `.section`/`.ends` to simple `.org`
- Remove `.slot` references

### 3. Update Macros
- Change `.endm` to `.endmacro`
- Convert numbered params `\1` to named params
- Add `%` prefix to invocations

### 4. Update Labels
- Change `_localLabel` to `@localLabel`

### 5. Update Structs
- Change `.endst` to `.endstruct`
- Update `instanceof` to `.ds Struct.size`

## Resources

- [Poppy User Manual](user-manual.md)
- [PASM Syntax Reference](pasm-file-format.md)
- [SMS Power!](https://www.smspower.org/) - Master System resources
- [Sega Retro](https://segaretro.org/) - Genesis resources
