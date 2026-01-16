# Migration Guide: devkitARM/GAS to Poppy

This guide helps users transition from devkitARM (GNU Assembler syntax) to Poppy Assembly for Game Boy Advance development.

## Overview

devkitARM is the standard toolchain for GBA homebrew, using GNU Assembler (GAS) syntax. Poppy provides native ARM7TDMI support with a more assembly-focused syntax.

## Key Philosophical Differences

| Aspect | devkitARM/GAS | Poppy |
|--------|---------------|-------|
| Toolchain | Full C/C++ + ASM | Assembly-focused |
| Output | ELF → objcopy → .gba | Direct .gba output |
| Syntax | AT&T-style (GAS) | Intel-style |
| Build | make/cmake complex | Simple JSON config |
| Header | gbafix post-process | Built-in header |

## Directive Equivalents

| GAS | Poppy | Notes |
|-----|-------|-------|
| `.section .text` | `.org $address` | Simplified |
| `.global symbol` | `symbol:` | Auto-exported |
| `.byte`, `.hword`, `.word` | `.db`, `.dw`, `.dl` | Renamed |
| `.ascii`, `.asciz` | `.db "string"` | Use .db |
| `.space n` | `.ds n` | Same concept |
| `.incbin "file"` | `.incbin "file"` | Same |
| `.include "file"` | `.include "file"` | Same |
| `.if` / `.endif` | `.if` / `.endif` | Same |
| `.ifdef` / `.ifndef` | `.ifdef` / `.ifndef` | Same |
| `.equ symbol, value` | `.define symbol value` | Renamed |
| `.set symbol, value` | `.set symbol, value` | Reassignable |
| `.macro` / `.endm` | `.macro` / `.endmacro` | Different end |
| `.rept` / `.endr` | `.rept` / `.endr` | Same |
| `.align n` | `.align n` | Same |
| `.arm` | `.arm` | Same |
| `.thumb` | `.thumb` | Same |
| `.thumb_func` | N/A | Automatic in Poppy |
| `.pool` | `.pool` | Literal pool |
| `.error "msg"` | `.error "msg"` | Same |

## Instruction Syntax

### ARM Mode

| GAS | Poppy | Notes |
|-----|-------|-------|
| `mov r0, #0x10` | `mov r0, #$10` | Hex prefix |
| `ldr r0, [r1]` | `ldr r0, [r1]` | Same |
| `ldr r0, [r1, #4]` | `ldr r0, [r1, #4]` | Same |
| `ldr r0, =label` | `ldr r0, =label` | Pool literal |
| `str r0, [r1], #4` | `str r0, [r1], #4` | Post-index |
| `str r0, [r1, #4]!` | `str r0, [r1, #4]!` | Pre-index |
| `add r0, r1, r2, lsl #2` | `add r0, r1, r2, lsl #2` | Same |
| `bne label` | `bne label` | Same |
| `bl function` | `bl function` | Same |
| `stmfd sp!, {r4-r11, lr}` | `stmfd sp!, {r4-r11, lr}` | Same |
| `ldmfd sp!, {r4-r11, pc}` | `ldmfd sp!, {r4-r11, pc}` | Same |

### Thumb Mode

| GAS | Poppy | Notes |
|-----|-------|-------|
| `mov r0, #0x10` | `mov r0, #$10` | Hex prefix |
| `ldr r0, [r1]` | `ldr r0, [r1]` | Same |
| `ldr r0, [pc, #offset]` | `ldr r0, [pc, #offset]` | Same |
| `push {r4-r7, lr}` | `push {r4-r7, lr}` | Same |
| `pop {r4-r7, pc}` | `pop {r4-r7, pc}` | Same |
| `bx lr` | `bx lr` | Same |

### Key Syntax Differences

1. **Hexadecimal:** `0x` → `$`
2. **Comments:** `@` or `//` → `;`
3. **Immediates:** Same `#` prefix
4. **Register lists:** Same `{r0-r3}` syntax

## Mode Switching

### GAS Style
```asm
    .arm
    .global ArmFunction
ArmFunction:
    mov r0, #0
    bx lr

    .thumb
    .thumb_func
    .global ThumbFunction
ThumbFunction:
    mov r0, #0
    bx lr
```

### Poppy Style
```asm
.arm
ArmFunction:
    mov r0, #0
    bx lr

.thumb
ThumbFunction:
    mov r0, #0
    bx lr
```

## Literal Pools

### GAS Style
```asm
    ldr r0, =0x04000000
    ldr r1, =LargeConstant
    @ ... code ...
    .pool

LargeConstant:
    .word 0x12345678
```

### Poppy Style
```asm
    ldr r0, =$04000000
    ldr r1, =LargeConstant
    ; ... code ...
    .pool

LargeConstant:
    .dl $12345678
```

## Macro Syntax

### GAS Style
```asm
.macro WAIT_VBLANK
1:
    ldr r0, =0x04000004
    ldrh r1, [r0]
    tst r1, #1
    beq 1b
.endm

    WAIT_VBLANK
```

### Poppy Style
```asm
.macro WAIT_VBLANK
@loop:
    ldr r0, =$04000004
    ldrh r1, [r0]
    tst r1, #1
    beq @loop
.endmacro

    %WAIT_VBLANK
```

### Macro Parameters

```asm
; GAS
.macro SET_BG_COLOR reg, color
    ldr \reg, =0x05000000
    ldr r12, =\color
    strh r12, [\reg]
.endm

    SET_BG_COLOR r0, 0x7FFF

; Poppy
.macro SET_BG_COLOR, reg, color
    ldr \reg, =$05000000
    ldr r12, =\color
    strh r12, [\reg]
.endmacro

    %SET_BG_COLOR r0, $7fff
```

## Label Syntax

### Global Labels (Same)
```asm
; GAS
main:
    push {lr}
    bl init

; Poppy (same)
main:
    push {lr}
    bl init
```

### Local Labels
```asm
; GAS uses numbered local labels
1:
    subs r0, #1
    bne 1b         @ backward to 1

; Poppy uses named local labels
@loop:
    subs r0, #1
    bne @loop
```

## GBA Header Configuration

### GAS + gbafix
```bash
# After assembly and linking
arm-none-eabi-objcopy -O binary game.elf game.gba
gbafix game.gba -tMYGAME -cMGME -mAB
```

### Poppy (built-in)
In assembly:
```asm
.gba_title "MYGAME"
.gba_code "MGME"
.gba_maker "AB"
```

Or in poppy.json:
```json
{
    "name": "mygame",
    "target": "gba",
    "cpu": "arm7tdmi",
    "output": {
        "format": "gba",
        "filename": "mygame.gba"
    },
    "header": {
        "title": "MYGAME",
        "gameCode": "MGME",
        "makerCode": "AB"
    }
}
```

## ROM Entry Point

### GAS (with crt0.s)
```asm
    .section .init
    .global _start
_start:
    b main

    .section .text
    .arm
main:
    @ Initialize system
    mov r0, #0x04000000
    @ ...
```

### Poppy
```asm
.target "gba"
.cpu "arm7tdmi"

.org $08000000
.arm

; ROM header (auto-generated by Poppy)
; Entry point at $08000000 after header

main:
    ; Initialize system
    mov r0, #$04000000
    ; ...
```

## Interworking (ARM/Thumb)

### GAS Style
```asm
    .arm
ArmCode:
    @ ... ARM code ...
    adr r0, ThumbCode + 1
    bx r0

    .thumb
    .thumb_func
ThumbCode:
    @ ... Thumb code ...
    bx lr
```

### Poppy Style
```asm
.arm
ArmCode:
    ; ... ARM code ...
    adr r0, ThumbCode + 1
    bx r0

.thumb
ThumbCode:
    ; ... Thumb code ...
    bx lr
```

## IWRAM/EWRAM Sections

### GAS (with linker script)
```asm
    .section .iwram, "ax", %progbits
    .arm
IWRAMCode:
    @ Fast code in IWRAM
    bx lr

    .section .ewram, "ax", %progbits
LargeBuffer:
    .space 0x10000
```

### Poppy
```asm
; IWRAM code section
.org $03000000
.arm
IWRAMCode:
    ; Fast code in IWRAM
    bx lr

; EWRAM data section
.org $02000000
LargeBuffer:
    .ds $10000
```

## Quick Reference Card

```
GAS                      Poppy
───                      ─────
.section .text           .org $addr
.global sym              sym:
.byte $ff                .db $ff
.hword $1234             .dw $1234
.word $12345678          .dl $12345678
.space 10                .ds 10
.ascii "str"             .db "str"
.asciz "str"             .db "str", 0
.incbin "x.bin"          .incbin "x.bin"
.include "x.s"           .include "x.pasm"
.equ sym, 5              .define sym 5
.set sym, 5              .set sym 5
.macro foo               .macro foo
.endm                    .endmacro
foo                      %foo
.if cond                 .if cond
.endif                   .endif
.ifdef                   .ifdef
.rept 4                  .rept 4
.endr                    .endr
.arm                     .arm
.thumb                   .thumb
.align 4                 .align 4
.pool                    .pool
0x1234                   $1234
@ comment                ; comment
1:                       @label:
1b                       @label
```

## Example: Complete Migration

### GAS Original
```asm
    .arm
    .global main
    .section .text

main:
    @ Disable interrupts
    mov r0, #0x04000000
    mov r1, #0
    str r1, [r0, #0x208]

    @ Set video mode 3
    mov r1, #0x403
    strh r1, [r0]

    @ Clear screen to blue
    ldr r0, =0x06000000
    ldr r1, =0x7C00        @ Blue in BGR555
    ldr r2, =240*160

1:
    strh r1, [r0], #2
    subs r2, #1
    bne 1b

    @ Infinite loop
2:
    b 2b

    .pool
```

### Poppy Equivalent
```asm
.target "gba"
.cpu "arm7tdmi"

.arm
.org $08000000

main:
    ; Disable interrupts
    mov r0, #$04000000
    mov r1, #0
    str r1, [r0, #$208]

    ; Set video mode 3
    mov r1, #$403
    strh r1, [r0]

    ; Clear screen to blue
    ldr r0, =$06000000
    ldr r1, =$7c00         ; Blue in BGR555
    ldr r2, =240*160

@clearLoop:
    strh r1, [r0], #2
    subs r2, #1
    bne @clearLoop

    ; Infinite loop
@forever:
    b @forever

    .pool
```

### poppy.json
```json
{
    "name": "blue-screen",
    "target": "gba",
    "cpu": "arm7tdmi",
    "output": {
        "format": "gba",
        "filename": "blue.gba"
    },
    "header": {
        "title": "BLUE",
        "gameCode": "BLUE",
        "makerCode": "PP"
    }
}
```

## Common Migration Tasks

### 1. Update Build System
- Remove Makefile/CMake
- Create poppy.json
- Remove gbafix step

### 2. Update Syntax
- Change `0x` to `$` for hex
- Change `@` comments to `;`
- Remove `.section` directives

### 3. Update Macros
- Change `.endm` to `.endmacro`
- Add `%` prefix to invocations

### 4. Update Labels
- Change numbered labels `1:` to named `@label:`
- Change `1b`/`1f` to `@label`

### 5. Update Data
- Change `.word` to `.dl`
- Change `.hword` to `.dw`
- Change `.byte` to `.db`

## Resources

- [Poppy User Manual](user-manual.md)
- [PASM Syntax Reference](pasm-file-format.md)
- [GBATEK](https://problemkaputt.de/gbatek.htm) - GBA hardware reference
- [Tonc](https://www.coranac.com/tonc/text/toc.htm) - GBA programming tutorial
