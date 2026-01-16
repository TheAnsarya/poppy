# Migration Guide: RGBDS to Poppy

This guide helps users transition from RGBDS (Rednex Game Boy Development System) to Poppy Assembly for Game Boy development.

## Overview

RGBDS is the de facto standard assembler for Game Boy homebrew. Poppy provides equivalent functionality with a more unified syntax that works across all supported platforms.

## Key Philosophical Differences

| Aspect | RGBDS | Poppy |
|--------|-------|-------|
| Output | Object files (linked by RGBLINK) | Direct binary output |
| Configuration | Linker scripts | JSON project files |
| Sections | SECTION directive with banking | Segment directives |
| Build Process | Multi-step (assemble → link → fix) | Single-step compilation |
| Header | RGBFIX post-processor | Built-in header generation |

## Directive Equivalents

| RGBDS | Poppy | Notes |
|-------|-------|-------|
| `SECTION "name", ROM0[$addr]` | `.org $addr` | Simpler addressing |
| `SECTION "name", ROMX[$addr], BANK[$n]` | `.bank n` + `.org $addr` | Separate bank/org |
| `DB`, `DW`, `DL` | `.db`, `.dw`, `.dl` | Dot prefix, lowercase |
| `DS n` | `.ds n` | Reserve space |
| `INCBIN "file"` | `.incbin "file"` | Same functionality |
| `INCLUDE "file"` | `.include "file"` | Same functionality |
| `IF` / `ENDC` | `.if` / `.endif` | Different endif keyword |
| `ELIF` | `.elseif` | Renamed |
| `DEF symbol EQU value` | `.define symbol value` | Renamed |
| `SET` (reassignable) | `.set` | Same concept |
| `MACRO` / `ENDM` | `.macro` / `.endmacro` | Different syntax |
| `REPT n` / `ENDR` | `.rept n` / `.endr` | Same functionality |
| `RSRESET` / `RSSET` / `RB` / `RW` | `.enum` / `.ende` | Struct-like approach |
| `ASSERT` | `.assert` | Same functionality |
| `FAIL` / `WARN` | `.error` / `.warning` | Renamed |
| `PRINTLN` | `.print` | Renamed |

## Section/Bank Management

### RGBDS Style
```asm
SECTION "Header", ROM0[$0100]
    nop
    jp Start

SECTION "Main", ROM0[$0150]
Start:
    di
    ld sp, $fffe

SECTION "Level Data", ROMX[$4000], BANK[1]
LevelData:
    INCBIN "level1.bin"
```

### Poppy Style
```asm
.org $0100
    nop
    jp Start

.org $0150
Start:
    di
    ld sp, $fffe

.bank 1
.org $4000
LevelData:
    .incbin "level1.bin"
```

## Macro Syntax

### RGBDS Style
```asm
MACRO wait_vblank
.wait\@:
    ldh a, [$44]
    cp 144
    jr c, .wait\@
ENDM

    wait_vblank
```

### Poppy Style
```asm
.macro wait_vblank
.wait:
    ldh a, [$44]
    cp 144
    jr c, .wait
.endmacro

    %wait_vblank
```

### Key Differences

| Feature | RGBDS | Poppy |
|---------|-------|-------|
| Definition | `MACRO name` | `.macro name` |
| End | `ENDM` | `.endmacro` |
| Parameters | `\1`, `\2`, etc. | `\param1`, `\param2` |
| Unique labels | `.label\@` | `.label` (auto-scoped) |
| Invocation | `macro_name args` | `%macro_name args` |
| Argument count | `_NARG` | `\#` |

### Parameter Example

```asm
; RGBDS
MACRO add_to_reg
    ld a, \1
    add \2
    ld \1, a
ENDM

    add_to_reg b, 5

; Poppy
.macro add_to_reg, reg, value
    ld a, \reg
    add \value
    ld \reg, a
.endmacro

    %add_to_reg b, 5
```

## Label Syntax

### Global Labels (Same)
```asm
; RGBDS
MyLabel:
    ld a, $00

; Poppy (same)
MyLabel:
    ld a, $00
```

### Local Labels
```asm
; RGBDS uses .label syntax
MyProc:
.loop:
    dec b
    jr nz, .loop
    ret

; Poppy uses @label syntax
MyProc:
@loop:
    dec b
    jr nz, @loop
    ret
```

### Anonymous Labels
```asm
; RGBDS uses :+ and :- for anonymous labels
    jr nz, :+
    xor a
:
    ret

; Poppy uses - and +
    jr nz, +
    xor a
+:
    ret
```

## Memory Definitions (RSSET)

### RGBDS Style
```asm
RSRESET
DEF wPlayerX RB 1
DEF wPlayerY RB 1
DEF wPlayerHP RW 1
DEF wPlayerName RB 8
```

### Poppy Style
```asm
.enum $c000
    wPlayerX:    .ds 1
    wPlayerY:    .ds 1
    wPlayerHP:   .ds 2
    wPlayerName: .ds 8
.ende
```

## Conditional Assembly

### RGBDS Style
```asm
IF DEF(DEBUG)
    PRINTLN "Debug build"
ELIF DEF(RELEASE)
    ; Release config
ELSE
    FAIL "Unknown build type"
ENDC
```

### Poppy Style
```asm
.ifdef DEBUG
    .print "Debug build"
.elseif RELEASE
    ; Release config
.else
    .error "Unknown build type"
.endif
```

## Header Configuration

### RGBDS (using RGBFIX post-process)
```bash
rgbfix -v -p 0xff -t "MYGAME" -m 0x00 -r 0x00 game.gb
```

### Poppy (built-in directives)
```asm
.gb_title "MYGAME"
.gb_cartridge_type $00    ; ROM only
.gb_rom_size $00          ; 32KB
.gb_ram_size $00          ; No RAM
.gb_cgb_flag $80          ; CGB compatible
```

Or in poppy.json:
```json
{
    "name": "mygame",
    "target": "gb",
    "cpu": "sm83",
    "output": {
        "format": "gb",
        "filename": "mygame.gb"
    },
    "header": {
        "title": "MYGAME",
        "cartridgeType": 0,
        "romSize": 0,
        "ramSize": 0,
        "cgbFlag": 128
    }
}
```

## Instruction Syntax Differences

Most Game Boy instructions are identical between RGBDS and Poppy:

| Instruction | RGBDS | Poppy | Notes |
|-------------|-------|-------|-------|
| Load | `ld a, $ff` | `ld a, $ff` | Same |
| High RAM | `ldh a, [$ff00+c]` | `ldh a, [c]` | Simplified |
| | `ldh a, [$ff44]` | `ldh a, [$44]` | $ff implicit |
| Bit ops | `bit 7, a` | `bit 7, a` | Same |
| Jumps | `jp hl` | `jp hl` | Same |
| | `jp $1234` | `jp $1234` | Same |

### High RAM Addressing

```asm
; RGBDS - full addresses
ldh a, [$ff44]
ldh [$ff00+c], a

; Poppy - implicit $ff00 base
ldh a, [$44]
ldh [c], a
```

## Numeric Literals

| Type | RGBDS | Poppy |
|------|-------|-------|
| Hexadecimal | `$ff` or `0xff` | `$ff` |
| Binary | `%10101010` | `%10101010` |
| Decimal | `255` | `255` |
| Character | `"A"` | `'A'` or `"A"` |

## String/Character Handling

### RGBDS Style
```asm
DB "Hello", 0
DW "AB"           ; Two bytes: 'A', 'B'
```

### Poppy Style
```asm
.db "Hello", 0
.dw "AB"          ; Same behavior
```

## Common Migration Tasks

### 1. Update Directives
- Add dot prefix to all directives
- Convert `ENDC` to `.endif`
- Convert `ENDM` to `.endmacro`

### 2. Update Macros
- Change `MACRO name` to `.macro name`
- Change `\1`, `\2` to named parameters
- Add `%` prefix to macro invocations

### 3. Update Labels
- Change `.localLabel` to `@localLabel`
- Change `:+`/`:-` to `+`/`-`

### 4. Update Sections
- Convert `SECTION` to `.org` and `.bank`
- Move header info to poppy.json or directives

### 5. Update Build Process
- Remove RGBLINK step
- Remove RGBFIX step (header built-in)
- Create poppy.json project file

## Quick Reference Card

```
RGBDS                    Poppy
─────                    ─────
SECTION "x", ROM0[$100]  .org $100
DB $ff                   .db $ff
DW $1234                 .dw $1234
DS 10                    .ds 10
INCBIN "x.bin"           .incbin "x.bin"
INCLUDE "x.asm"          .include "x.asm"
DEF x EQU 5              .define x 5
MACRO foo                .macro foo
ENDM                     .endmacro
foo                      %foo
IF cond                  .if cond
ELIF cond                .elseif cond
ELSE                     .else
ENDC                     .endif
REPT 4                   .rept 4
ENDR                     .endr
.localLabel:             @localLabel:
:+                       +
:-                       -
```

## Example: Complete Migration

### RGBDS Original
```asm
INCLUDE "hardware.inc"

SECTION "Header", ROM0[$0100]
    nop
    jp Start

SECTION "Main", ROM0[$0150]
Start:
    di
    ld sp, $fffe

    ; Wait for VBlank
.waitVBlank:
    ldh a, [rLY]
    cp 144
    jr c, .waitVBlank

    ; Disable LCD
    xor a
    ldh [rLCDC], a

    ; Main loop
.mainLoop:
    halt
    jr .mainLoop
```

### Poppy Equivalent
```asm
.include "hardware.pasm"

.org $0100
    nop
    jp Start

.org $0150
Start:
    di
    ld sp, $fffe

    ; Wait for VBlank
@waitVBlank:
    ldh a, [LY]
    cp 144
    jr c, @waitVBlank

    ; Disable LCD
    xor a
    ldh [LCDC], a

    ; Main loop
@mainLoop:
    halt
    jr @mainLoop
```

## Resources

- [Poppy User Manual](user-manual.md)
- [Game Boy Guide](gameboy-guide.md)
- [PASM Syntax Reference](pasm-file-format.md)
- [Pan Docs](https://gbdev.io/pandocs/) - Game Boy hardware reference
