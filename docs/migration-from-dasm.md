# Migration Guide: DASM to Poppy

This guide helps users transition from DASM to Poppy Assembly for Atari 2600 and other 6502-based development.

## Overview

DASM is one of the most popular assemblers for Atari 2600 development. Poppy provides equivalent functionality with a consistent syntax that works across all supported 6502 platforms.

## Key Philosophical Differences

| Aspect | DASM | Poppy |
|--------|------|-------|
| Target | Multi-platform (6502 focus) | Multi-platform (11 systems) |
| Output | Direct binary | Direct binary |
| Configuration | Command-line flags | JSON project files |
| Syntax | Processor-specific modes | Target directives |
| Build Process | Single-step | Single-step |

## Directive Equivalents

| DASM | Poppy | Notes |
|------|-------|-------|
| `processor 6502` | `.cpu "6507"` | Use project file instead |
| `org $addr` | `.org $addr` | Same functionality |
| `dc.b`, `.byte` | `.db`, `.byte` | Dot prefix preferred |
| `dc.w`, `.word` | `.dw`, `.word` | Dot prefix preferred |
| `ds n` | `.ds n` | Reserve space |
| `incbin "file"` | `.incbin "file"` | Same functionality |
| `include "file"` | `.include "file"` | Same functionality |
| `if` / `endif` | `.if` / `.endif` | Dot prefix required |
| `else` | `.else` | Dot prefix required |
| `equ` / `=` | `.define` / `=` | Both supported |
| `set` | `.set` | Reassignable |
| `mac` / `endm` | `.macro` / `.endmacro` | Different keywords |
| `repeat` / `repend` | `.rept` / `.endr` | Different keywords |
| `seg` / `seg.u` | `.segment` | Simplified |
| `echo` | `.print` | Renamed |
| `err` | `.error` | Renamed |

## Processor Declaration

### DASM Style
```asm
    processor 6502
    org $f000
```

### Poppy Style
In poppy.json:
```json
{
    "name": "mygame",
    "target": "atari2600",
    "cpu": "6507",
    "output": {
        "format": "a26",
        "filename": "mygame.bin"
    }
}
```

Or with directives:
```asm
.target "atari2600"
.cpu "6507"
.org $f000
```

## Macro Syntax

### DASM Style
```asm
    mac SLEEP
.LOOP
    repeat {1}/2
    nop
    repend
    endm

    SLEEP 10
```

### Poppy Style
```asm
.macro SLEEP, cycles
.loop:
    .rept \cycles / 2
    nop
    .endr
.endmacro

    %SLEEP 10
```

### Key Differences

| Feature | DASM | Poppy |
|---------|------|-------|
| Definition | `mac name` | `.macro name` |
| End | `endm` | `.endmacro` |
| Parameters | `{1}`, `{2}`, etc. | `\param1`, `\param2` |
| Invocation | `MACRO_NAME args` | `%MACRO_NAME args` |
| Local labels | `.label` auto-unique | `@label` scoped |

### Parameter Example

```asm
; DASM
    mac STORE_VALUE
    lda #{1}
    sta {2}
    endm

    STORE_VALUE $10, $80

; Poppy
.macro STORE_VALUE, value, addr
    lda #\value
    sta \addr
.endmacro

    %STORE_VALUE $10, $80
```

## Label Syntax

### Global Labels (Same)
```asm
; DASM
Reset:
    sei
    cld

; Poppy (same)
Reset:
    sei
    cld
```

### Local Labels
```asm
; DASM - uses . prefix for local scope
KernelLoop:
.wait:
    lda INTIM
    bne .wait
    rts

; Poppy - uses @ prefix
KernelLoop:
@wait:
    lda INTIM
    bne @wait
    rts
```

## Segment Management

### DASM Style
```asm
    seg Code
    org $f000

Start:
    sei
    cld

    seg.u Variables
    org $80

PlayerX:    ds 1
PlayerY:    ds 1
```

### Poppy Style
```asm
; Code segment
.org $f000

Start:
    sei
    cld

; Variables (TIA RAM at $80)
.org $80
PlayerX:    .ds 1
PlayerY:    .ds 1
```

Or use `.enum` for RAM definitions:
```asm
.enum $80
    PlayerX:    .ds 1
    PlayerY:    .ds 1
    Score:      .ds 2
.ende
```

## Conditional Assembly

### DASM Style
```asm
NTSC = 1

    if NTSC
SCANLINES = 262
    else
SCANLINES = 312
    endif
```

### Poppy Style
```asm
.define NTSC 1

.if NTSC
    .define SCANLINES 262
.else
    .define SCANLINES 312
.endif
```

## Repeat Blocks

### DASM Style
```asm
    repeat 8
    nop
    repend
```

### Poppy Style
```asm
.rept 8
    nop
.endr
```

## Numeric Literals

| Type | DASM | Poppy |
|------|------|-------|
| Hexadecimal | `$ff` or `0xff` | `$ff` |
| Binary | `%10101010` | `%10101010` |
| Decimal | `255` | `255` |
| Character | `"A"` | `'A'` |

## Expression Differences

### Operators

| Operation | DASM | Poppy |
|-----------|------|-------|
| Addition | `+` | `+` |
| Subtraction | `-` | `-` |
| Multiplication | `*` | `*` |
| Division | `/` | `/` |
| Modulo | `%` | `%` |
| Low byte | `<addr` | `<addr` |
| High byte | `>addr` | `>addr` |
| Bank byte | N/A | `^addr` |

### Special Symbols

| Symbol | DASM | Poppy |
|--------|------|-------|
| Current address | `.` or `*` | `*` |
| Current origin | `.` | `*` |

## Atari 2600 Specific

### TIA Registers

Poppy supports standard TIA register names when targeting Atari 2600:

```asm
; Both assemblers use same register names
    lda #$00
    sta COLUBK    ; Background color
    sta COLUPF    ; Playfield color
    sta COLUP0    ; Player 0 color
    sta COLUP1    ; Player 1 color
```

### Typical 4K ROM Structure

```asm
; DASM
    processor 6502
    org $f000

; ... game code ...

    org $fffc
    dc.w Reset
    dc.w Reset

; Poppy
.target "atari2600"
.cpu "6507"
.org $f000

; ... game code ...

.org $fffc
    .dw Reset
    .dw Reset
```

## Common Migration Tasks

### 1. Update Project Setup
- Create poppy.json with target/cpu settings
- Remove `processor` directive

### 2. Update Directives
- Add dot prefix to all directives
- Change `mac`/`endm` to `.macro`/`.endmacro`
- Change `repeat`/`repend` to `.rept`/`.endr`

### 3. Update Macros
- Change `{1}`, `{2}` to named parameters
- Add `%` prefix to macro invocations

### 4. Update Labels
- Change `.localLabel` to `@localLabel`

### 5. Update Data Definitions
- Change `dc.b` to `.db`
- Change `dc.w` to `.dw`

## Quick Reference Card

```
DASM                     Poppy
────                     ─────
processor 6502           ; Use poppy.json
org $f000                .org $f000
dc.b $ff                 .db $ff
dc.w $1234               .dw $1234
ds 10                    .ds 10
incbin "x.bin"           .incbin "x.bin"
include "x.asm"          .include "x.asm"
name equ 5               .define name 5
name = 5                 name = 5
mac foo                  .macro foo
endm                     .endmacro
foo                      %foo
if cond                  .if cond
else                     .else
endif                    .endif
repeat 4                 .rept 4
repend                   .endr
.localLabel:             @localLabel:
echo "msg"               .print "msg"
```

## Example: Complete Migration

### DASM Original (Minimal 2600 Kernel)
```asm
    processor 6502
    include "vcs.h"

    seg.u Variables
    org $80

FrameCount: ds 1

    seg Code
    org $f000

Reset:
    sei
    cld
    ldx #0
    txa
.clearMem:
    sta 0,x
    inx
    bne .clearMem

MainLoop:
    ; VSYNC
    lda #2
    sta VSYNC
    sta WSYNC
    sta WSYNC
    sta WSYNC
    lda #0
    sta VSYNC

    ; VBLANK (37 lines)
    lda #43
    sta TIM64T

.waitVblank:
    lda INTIM
    bne .waitVblank

    lda #0
    sta VBLANK

    ; Visible scanlines (192)
    ldx #192
.kernel:
    sta WSYNC
    dex
    bne .kernel

    ; Overscan
    lda #2
    sta VBLANK
    lda #35
    sta TIM64T

.waitOverscan:
    lda INTIM
    bne .waitOverscan

    jmp MainLoop

    org $fffc
    dc.w Reset
    dc.w Reset
```

### Poppy Equivalent
```asm
.include "vcs.pasm"

; Variables in TIA RAM
.enum $80
    FrameCount: .ds 1
.ende

.org $f000

Reset:
    sei
    cld
    ldx #0
    txa
@clearMem:
    sta 0, x
    inx
    bne @clearMem

MainLoop:
    ; VSYNC
    lda #2
    sta VSYNC
    sta WSYNC
    sta WSYNC
    sta WSYNC
    lda #0
    sta VSYNC

    ; VBLANK (37 lines)
    lda #43
    sta TIM64T

@waitVblank:
    lda INTIM
    bne @waitVblank

    lda #0
    sta VBLANK

    ; Visible scanlines (192)
    ldx #192
@kernel:
    sta WSYNC
    dex
    bne @kernel

    ; Overscan
    lda #2
    sta VBLANK
    lda #35
    sta TIM64T

@waitOverscan:
    lda INTIM
    bne @waitOverscan

    jmp MainLoop

.org $fffc
    .dw Reset
    .dw Reset
```

### poppy.json
```json
{
    "name": "minimal-kernel",
    "target": "atari2600",
    "cpu": "6507",
    "output": {
        "format": "a26",
        "filename": "kernel.bin"
    },
    "include": ["vcs.pasm"]
}
```

## Resources

- [Poppy User Manual](user-manual.md)
- [PASM Syntax Reference](pasm-file-format.md)
- [Stella Programmer's Guide](https://alienbill.com/2600/101/docs/stella.html)
- [8bitworkshop](https://8bitworkshop.com/) - Online 2600 development
