# 📜 Poppy Assembly Syntax Specification

> Draft v0.1 - January 11, 2026

This document defines the assembly language syntax for the Poppy Compiler.

---

## 🎯 Design Goals

1. **Familiar**: Similar to ASAR and ca65 for easy transition
2. **Consistent**: Uniform rules across all target architectures
3. **Flexible**: Support both ROM hacking and homebrew workflows
4. **Clear**: Readable code with minimal ambiguity

---

## 📝 Basic Syntax

### Comments

```asm
; This is a line comment (semicolon)
// This is also a line comment (C-style)

/* This is a
   block comment */
```

### Case Sensitivity

- **Opcodes/Mnemonics**: Case-insensitive, normalized to lowercase
- **Directives**: Case-insensitive, normalized to lowercase
- **Labels**: Case-sensitive
- **Defines/Macros**: Case-sensitive

```asm
LDA #$ff        ; Valid, interpreted as 'lda #$ff'
.ORG $8000      ; Valid, interpreted as '.org $8000'
MyLabel:        ; Different from mylabel:
```

---

## 🔢 Numeric Literals

### Hexadecimal (Primary)

```asm
$ff             ; Hex with $ prefix (preferred)
$0a40           ; Multi-digit hex
$7e:1234        ; Bank:address notation (SNES)
```

### Decimal

```asm
255             ; Decimal (no prefix)
1000            ; Multi-digit decimal
```

### Binary

```asm
%01111111       ; Binary with % prefix
%1010_1010      ; Underscores allowed for readability
```

### Character Literals

```asm
'A'             ; Single ASCII character
"Hello"         ; String (for data directives)
```

---

## 🏷️ Labels

### Standard Labels

```asm
GlobalLabel:            ; Global label (colon required)
    lda #$00

AnotherLabel:           ; Another global label
    rts
```

### Local/Sub Labels

```asm
Main:
    .loop:              ; Local to 'Main' (dot prefix)
        dex
        bne .loop       ; Reference local label
    rts

Other:
    .loop:              ; Different .loop, local to 'Other'
        dey
        bne .loop
    rts
```

### Anonymous Labels

```asm
+                       ; Forward-reference anonymous label
-                       ; Backward-reference anonymous label

    lda #$00
-                       ; Anonymous label
    dex
    bne -               ; Branch to previous '-'
    inx
    bne +               ; Branch to next '+'
+                       ; Anonymous label
    rts
```

### Named Anonymous Labels

```asm
-main_loop              ; Named for clarity
    dex
    bne -main_loop
```

---

## 📋 Directives

All directives use a dot prefix for clarity.

### Origin and Addressing

```asm
.org $8000              ; Set program counter
.base $c000             ; Set base address (different from PC)
.bank 0                 ; Set current bank
```

### Data Definition

```asm
.db $ff, $00, $a5       ; Define bytes (alias: .byte)
.dw $1234, $5678        ; Define words (16-bit, alias: .word)
.dl $123456             ; Define long (24-bit)
.dd $12345678           ; Define double (32-bit)

.ds 16                  ; Define space (16 bytes of $00)
.ds 8, $ff              ; Define space (8 bytes of $ff)

.ascii "Hello"          ; ASCII string (no terminator)
.asciiz "Hello"         ; ASCII string (null terminated)
```

### File Inclusion

```asm
.include "header.asm"   ; Include source file
.incbin "data.bin"      ; Include binary file
.incbin "chr.bin", $10, $100  ; Offset and length
```

### Conditional Assembly

```asm
.if CONDITION
    ; code if true
.elif OTHER_CONDITION
    ; code if other true
.else
    ; code if false
.endif

.ifdef SYMBOL           ; If symbol defined
.ifndef SYMBOL          ; If symbol not defined
```

### Repetition

```asm
.repeat 8
    nop
.endrepeat

.repeat 4, i            ; With counter variable
    .db i * 2           ; 0, 2, 4, 6
.endrepeat
```

### Architecture Selection

```asm
.target 6502            ; Target 6502 (NES)
.target 65816           ; Target 65816 (SNES)
.target sm83            ; Target SM83 (Game Boy)
.target spc700          ; Target SPC700 (SNES audio)
```

### SNES-Specific Directives

```asm
.lorom                  ; LoROM mapping
.hirom                  ; HiROM mapping
.exlorom                ; ExLoROM mapping
.exhirom                ; ExHiROM mapping

.freespace              ; Find free space in ROM
.freedata               ; Find free space for data
```

---

## 🔧 Defines and Constants

### Simple Defines

```asm
.define SCREEN_WIDTH 256
.define PPU_CTRL $2000

    lda #SCREEN_WIDTH
    sta PPU_CTRL
```

### Parameterized Defines

```asm
.define ADD(a, b) ((a) + (b))

    lda #ADD(5, 3)      ; Expands to lda #8
```

### Undefine

```asm
.undef SCREEN_WIDTH     ; Remove definition
```

---

## 🔄 Macros

### Basic Macro

```asm
.macro SetA value
    lda #value
.endmacro

    SetA $ff            ; Expands to: lda #$ff
```

### Multi-Parameter Macro

```asm
.macro CopyByte src, dest
    lda src
    sta dest
.endmacro

    CopyByte $00, $10   ; Copy byte from $00 to $10
```

### Variadic Macro

```asm
.macro WriteBytes ...
    .repeat .paramcount
        .db .param(.index)
    .endrepeat
.endmacro

    WriteBytes $ff, $00, $aa, $55
```

### Local Labels in Macros

```asm
.macro WaitVBlank
    .local wait
wait:
    bit $2002
    bpl wait
.endmacro
```

---

## 🎯 Operand Modifiers

### Size Specifiers (65816)

```asm
    lda.b #$00          ; Force 8-bit immediate
    lda.w #$0000        ; Force 16-bit immediate
    lda.l $7e0000       ; Force 24-bit (long) address
```

### Low/High Byte Extraction

```asm
.define ADDR $1234

    lda #<ADDR          ; Low byte ($34)
    ldx #>ADDR          ; High byte ($12)
    ldy #^ADDR          ; Bank byte (for 24-bit)
```

---

## 📊 Expressions

### Arithmetic

```asm
    lda #5 + 3          ; Addition
    lda #10 - 2         ; Subtraction
    lda #4 * 8          ; Multiplication
    lda #16 / 4         ; Division
    lda #17 % 5         ; Modulo
```

### Bitwise

```asm
    lda #$f0 | $0f      ; OR
    lda #$ff & $0f      ; AND
    lda #$aa ^ $55      ; XOR
    lda #~$ff           ; NOT
    lda #1 << 4         ; Shift left
    lda #$80 >> 4       ; Shift right
```

### Comparison (for conditionals)

```asm
.if VALUE == 0          ; Equal
.if VALUE != 0          ; Not equal
.if VALUE < 10          ; Less than
.if VALUE <= 10         ; Less or equal
.if VALUE > 10          ; Greater than
.if VALUE >= 10         ; Greater or equal
```

### Logical

```asm
.if A && B              ; Logical AND
.if A || B              ; Logical OR
.if !A                  ; Logical NOT
```

---

## 🏗️ Segments (Optional Relocatable Mode)

```asm
.segment "CODE"
    ; Program code here

.segment "DATA"
    ; Data here

.segment "ZEROPAGE"
    ; Zero page variables
```

---

## 🔍 Built-in Functions

```asm
.sizeof(label)          ; Size of data block
.bankof(label)          ; Bank number of label
.strlen("text")         ; String length
.defined(SYMBOL)        ; Check if defined (1 or 0)
```

---

## 📁 Example Program

```asm
; NES Hello World
; Poppy Compiler Example

.target 6502

; Constants
.define PPU_CTRL    $2000
.define PPU_MASK    $2001
.define PPU_STATUS  $2002

.org $c000

; Reset handler
Reset:
    sei                     ; Disable interrupts
    cld                     ; Clear decimal mode
    ldx #$ff
    txs                     ; Initialize stack

.wait_vblank:
    bit PPU_STATUS
    bpl .wait_vblank

    ; Initialize PPU
    lda #%10000000          ; Enable NMI
    sta PPU_CTRL
    lda #%00011110          ; Show sprites and background
    sta PPU_MASK

-main_loop:
    jmp -main_loop          ; Infinite loop

; NMI handler
NMI:
    rti

; IRQ handler
IRQ:
    rti

; Vectors
.org $fffa
    .dw NMI
    .dw Reset
    .dw IRQ
```

---

## 🔄 Compatibility Notes

### ASAR Users

- Replace `!define` with `.define`
- Replace `%macro()` calls with just `Macro` (no percent)
- Use `.` prefix for directives

### ca65 Users

- Syntax is very similar
- `.proc` / `.endproc` supported as alias for scoping
- Use `.segment` for relocatable code

---

## 📋 Reserved Words

The following are reserved and cannot be used as labels or defines:

- All CPU mnemonics (lda, sta, etc.)
- All directive names (.org, .db, etc.)
- Built-in function names

---

## 📝 Notes

- This specification is a living document
- Syntax may evolve based on implementation needs
- Feedback welcome during development phase

---

