# 📚 Poppy Compiler User Manual

> Complete guide to using the Poppy multi-system assembly compiler

**Version:** 0.1.0
**Updated:** January 11, 2026

---

## 📋 Table of Contents

1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Quick Start](#quick-start)
4. [Command-Line Usage](#command-line-usage)
5. [Assembly Syntax](#assembly-syntax)
6. [Directives Reference](#directives-reference)
7. [Addressing Modes](#addressing-modes)
8. [Expressions](#expressions)
9. [Labels and Symbols](#labels-and-symbols)
10. [Target Architectures](#target-architectures)
11. [Output Formats](#output-formats)
12. [Examples](#examples)
13. [Troubleshooting](#troubleshooting)

---

## 1. Introduction {#introduction}

**Poppy** is a modern multi-system assembly compiler targeting classic gaming platforms:

- **NES** (MOS 6502 processor)
- **SNES** (WDC 65816 processor)
- **Game Boy** (Sharp SM83/LR35902 processor)

### Key Features

- 📝 **Clean syntax** - Lowercase mnemonics and `$` hex prefix
- 🔢 **Modern conventions** - `$40df` for hex, `%10101010` for binary
- 🏷️ **Full symbol support** - Labels, constants, and expressions
- 📊 **Data directives** - `.byte`, `.word`, `.long`, `.fill`
- 📈 **Optimization** - Automatic zero-page optimization
- 🖥️ **CLI tools** - Full command-line interface with options

### Philosophy

Poppy aims to be:
- **Readable** - Assembly that looks clean and modern
- **Consistent** - Same syntax patterns across targets
- **Helpful** - Clear error messages with suggestions
- **Capable** - Support for real-world game projects

---

## 2. Installation {#installation}

### Prerequisites

- .NET 10.0 SDK or later
- Git (for cloning)

### From Source

```bash
# Clone the repository
git clone https://github.com/TheAnsarya/poppy.git
cd poppy

# Build the compiler
dotnet build src/

# Optional: Create a release build
dotnet publish src/Poppy.CLI -c Release -o ./bin
```

### Verify Installation

```bash
# Run with --version
dotnet run --project src/Poppy.CLI -- --version

# Or if using published binary
./bin/poppy --version
```

Expected output:
```
Poppy Compiler v0.1.0
Target architectures: 6502, 65816, SM83
Copyright (c) 2024
```

---

## 3. Quick Start {#quick-start}

### Your First Program

Create a file named `hello.asm`:

```asm
; hello.asm - Simple NES program
.org $8000

reset:
    sei             ; Disable interrupts
    cld             ; Clear decimal mode
    ldx #$ff
    txs             ; Set up stack

    ; Clear PPU control
    lda #$00
    sta $2000
    sta $2001

    ; Infinite loop
loop:
    jmp loop

; Interrupt vectors
.org $fffa
    .word $0000     ; NMI
    .word reset     ; RESET
    .word $0000     ; IRQ
```

### Compile

```bash
# Basic compilation
poppy hello.asm

# With output name
poppy -o hello.bin hello.asm

# Generate listing file
poppy -l hello.lst hello.asm

# Verbose mode
poppy -V hello.asm
```

### Check Output

```bash
# Check file size (should be ~32KB for NES PRG bank)
ls -la hello.bin

# Hexdump first bytes
xxd hello.bin | head -20
```

---

## 4. Command-Line Usage {#command-line-usage}

### Synopsis

```
poppy [options] <input.asm>
```

### Options

| Option | Long Form | Description |
|--------|-----------|-------------|
| `-o FILE` | `--output FILE` | Output file path |
| `-l FILE` | `--listing FILE` | Generate listing file |
| `-t ARCH` | `--target ARCH` | Target architecture (6502, 65816, sm83) |
| `-V` | `--verbose` | Verbose output |
| `-h` | `--help` | Show help |
| `--version` | | Show version |

### Examples

```bash
# Compile to specific output
poppy -o game.nes main.asm

# Target SNES
poppy -t 65816 -o game.sfc main.asm

# Generate all files with verbose output
poppy -V -l game.lst -o game.nes main.asm
```

---

## 5. Assembly Syntax {#assembly-syntax}

### Basic Structure

```asm
; Comments start with semicolon
; or double-slash

; Label definition
label_name:
    instruction operand    ; Inline comment

; Data definition
.byte $00, $01, $02

; Constant definition
CONSTANT = $2000
```

### Case Sensitivity

- **Mnemonics**: Case-insensitive (`LDA`, `lda`, `Lda` all work)
- **Labels**: Case-insensitive
- **Directives**: Case-insensitive (`.BYTE`, `.byte`, `.Byte`)
- **Recommended**: Use lowercase for consistency

### Number Formats

| Format | Prefix | Example |
|--------|--------|---------|
| Hexadecimal | `$` | `$ff`, `$1234` |
| Binary | `%` | `%10101010` |
| Decimal | (none) | `255`, `1234` |

### Strings

```asm
.byte "Hello"           ; ASCII string
.byte "Line", $0a       ; String with newline byte
```

---

## 6. Directives Reference {#directives-reference}

### Origin Directive

```asm
.org address            ; Set program counter

; Example
.org $8000             ; Start code at $8000
```

### Data Directives

```asm
; Single bytes
.byte $01, $02, $03
.db $ff                ; Alias for .byte

; 16-bit words (little-endian)
.word $1234, $5678
.dw label              ; Alias for .word

; 24/32-bit longs
.long $123456
.dl $deadbeef          ; Alias for .long

; Strings
.byte "Text data"
```

### Space/Fill Directives

```asm
; Reserve space (zeros)
.ds 16                 ; 16 zero bytes
.res 32                ; Alias for .ds

; Fill with value
.fill 8, $ff           ; 8 bytes of $ff
.fill 256, $ea         ; 256 NOP instructions
```

### Constant Definition

```asm
; Using = syntax (preferred)
PPU_CTRL = $2000
SCREEN_WIDTH = 256

; Using .equ or .define
.define TILE_SIZE, 8
```

### Coming Soon

```asm
; Include files (not yet implemented)
.include "macros.asm"
.incbin "data.bin"

; Conditional assembly (not yet implemented)
.ifdef DEBUG
    lda #$ff
.endif

; Macros (parsing complete, expansion pending)
.macro PUSH_ALL
    pha
    phx
    phy
.endmacro
```

---

## 7. Addressing Modes {#addressing-modes}

### 6502 Addressing Modes

| Mode | Syntax | Example | Bytes |
|------|--------|---------|-------|
| Implied | `opcode` | `nop` | 1 |
| Accumulator | `opcode a` | `asl a` | 1 |
| Immediate | `opcode #value` | `lda #$42` | 2 |
| Zero Page | `opcode $zp` | `lda $00` | 2 |
| Zero Page,X | `opcode $zp,x` | `lda $00,x` | 2 |
| Zero Page,Y | `opcode $zp,y` | `ldx $00,y` | 2 |
| Absolute | `opcode $addr` | `lda $2000` | 3 |
| Absolute,X | `opcode $addr,x` | `lda $2000,x` | 3 |
| Absolute,Y | `opcode $addr,y` | `lda $2000,y` | 3 |
| Indirect | `opcode ($addr)` | `jmp ($fffc)` | 3 |
| (Indirect,X) | `opcode ($zp,x)` | `lda ($00,x)` | 2 |
| (Indirect),Y | `opcode ($zp),y` | `lda ($00),y` | 2 |
| Relative | `opcode label` | `beq loop` | 2 |

### Automatic Optimization

Poppy automatically optimizes absolute addressing to zero page when possible:

```asm
lda $00         ; Zero page (2 bytes)
lda $0000       ; Still zero page (2 bytes) - optimized!
lda $2000       ; Absolute (3 bytes)
```

### Force Addressing Mode

Use size suffixes to force specific addressing:

```asm
lda.b $00       ; Force 8-bit (zero page)
lda.w $0000     ; Force 16-bit (absolute)
lda.l $000000   ; Force 24-bit (65816 long)
```

---

## 8. Expressions {#expressions}

### Arithmetic Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `+` | Addition | `base + 10` |
| `-` | Subtraction | `end - start` |
| `*` | Multiplication | `count * 2` |
| `/` | Division | `total / 4` |
| `%` | Modulo | `value % 8` |

### Bitwise Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `&` | AND | `value & $0f` |
| `\|` | OR | `flags \| $80` |
| `^` | XOR | `mask ^ $ff` |
| `~` | NOT | `~mask` |
| `<<` | Shift left | `value << 4` |
| `>>` | Shift right | `value >> 4` |

### Byte Extraction

| Operator | Description | Example |
|----------|-------------|---------|
| `<` | Low byte | `<address` → $34 for $1234 |
| `>` | High byte | `>address` → $12 for $1234 |
| `^` | Bank byte | `^address` → $7E for $7E1234 |

### Examples

```asm
; Calculate table offset
BASE = $2000
ENTRY_SIZE = 8
    lda BASE + ENTRY_SIZE * 5    ; $2000 + $28 = $2028

; Extract address bytes
VECTOR = $c123
    .byte <VECTOR               ; $23
    .byte >VECTOR               ; $c1

; Bit manipulation
    lda #(1 << 7)               ; $80 (bit 7 set)
    and #~%00001111             ; Clear low nibble
```

### Special Symbols

| Symbol | Description |
|--------|-------------|
| `*` | Current address |
| `$` | Current address (alias) |

```asm
; Calculate relative offset
.org $8000
start:
    ; * = $8000 here
    .byte end - *               ; Size of this block
    nop
    nop
end:
```

---

## 9. Labels and Symbols {#labels-and-symbols}

### Global Labels

```asm
reset:                  ; Define global label
    jmp reset           ; Reference global label

CONSTANT = $2000        ; Define constant
    sta CONSTANT        ; Use constant
```

### Local Labels (Coming Soon)

```asm
routine1:
    @loop:              ; Local to routine1
        dex
        bne @loop

routine2:
    @loop:              ; Different label, local to routine2
        dey
        bne @loop
```

### Anonymous Labels (Coming Soon)

```asm
    ldx #$10
-                       ; Backward target
    dex
    bne -               ; Branch to previous -

    lda #$00
    beq +               ; Branch to next +
    nop
+                       ; Forward target
```

---

## 10. Target Architectures {#target-architectures}

### 6502 (NES)

Default target. All standard 6502 instructions supported.

```bash
poppy -t 6502 game.asm
```

Supported features:
- All 56 official opcodes
- All 13 addressing modes
- Automatic zero-page optimization

### 65816 (SNES) - Coming Soon

```bash
poppy -t 65816 game.asm
```

Additional features:
- 16-bit accumulator and index modes
- 24-bit long addressing
- Stack relative addressing
- Block move instructions

### SM83 (Game Boy) - Coming Soon

```bash
poppy -t sm83 game.asm
```

Features:
- Game Boy specific instruction set
- Z80-derived syntax
- CB-prefixed extended instructions

---

## 11. Output Formats {#output-formats}

### Raw Binary (Default)

Plain binary output with no headers.

```bash
poppy -o output.bin source.asm
```

### iNES Format (Coming Soon)

NES ROM with iNES header.

```asm
; Configure ROM
.ines_prg 2             ; 32KB PRG
.ines_chr 1             ; 8KB CHR
.ines_mapper 0          ; NROM

; Or use directives
.nes
.mapper 0
```

### SFC Format (Coming Soon)

SNES ROM with internal header.

### Listing File

Generate assembly listing with addresses and machine code:

```bash
poppy -l output.lst source.asm
```

Output format:
```
; Poppy Compiler v0.1.0 Listing
; Generated: 2026-01-11 12:00:00

8000  78        sei
8001  d8        cld
8002  a2 ff     ldx #$ff
8004  9a        txs

; Symbols
; -------
; reset = $8000
; PPU_CTRL = $2000
```

---

## 12. Examples {#examples}

### Simple Loop

```asm
; Count down from 10 to 0
.org $8000

    ldx #10             ; Start at 10
loop:
    dex                 ; Decrement
    bne loop            ; Loop until zero
    rts
```

### Data Tables

```asm
; Sprite data table
.org $8000

sprite_data:
    .byte $80, $00, $00, $80    ; Y, tile, attr, X
    .byte $88, $01, $00, $88
    .byte $90, $02, $00, $90

; Pointer table
pointers:
    .word handler1
    .word handler2
    .word handler3

handler1:
    lda #$01
    rts

handler2:
    lda #$02
    rts

handler3:
    lda #$03
    rts
```

### Using Constants

```asm
; NES PPU registers
PPU_CTRL   = $2000
PPU_MASK   = $2001
PPU_STATUS = $2002
OAM_ADDR   = $2003
OAM_DATA   = $2004
PPU_SCROLL = $2005
PPU_ADDR   = $2006
PPU_DATA   = $2007

; Screen dimensions
SCREEN_W = 256
SCREEN_H = 240
TILE_SIZE = 8
TILES_X = SCREEN_W / TILE_SIZE  ; 32
TILES_Y = SCREEN_H / TILE_SIZE  ; 30

.org $8000

init_ppu:
    ; Wait for VBlank
-   bit PPU_STATUS
    bpl -

    ; Set PPU address to $2000 (nametable)
    lda #>$2000
    sta PPU_ADDR
    lda #<$2000
    sta PPU_ADDR

    rts
```

### NES Skeleton

```asm
; ============================================
; NES Program Template
; ============================================

; Constants
PPU_CTRL   = $2000
PPU_MASK   = $2001
PPU_STATUS = $2002
OAM_DMA    = $4014

; Zero page variables
.org $0000
temp1:     .ds 1
temp2:     .ds 1
frame_cnt: .ds 1

; Program code
.org $8000

reset:
    sei
    cld
    ldx #$ff
    txs

    ; Wait for PPU warm-up
    bit PPU_STATUS
-   bit PPU_STATUS
    bpl -
-   bit PPU_STATUS
    bpl -

    ; Initialize
    jsr init_ppu
    jsr init_vars

    ; Enable rendering
    lda #%10000000          ; Enable NMI
    sta PPU_CTRL
    lda #%00011110          ; Show sprites and background
    sta PPU_MASK

main_loop:
    ; Wait for NMI
-   lda frame_cnt
    beq -

    lda #$00
    sta frame_cnt

    ; Game logic here
    jsr update_game
    jsr update_sprites

    jmp main_loop

nmi:
    pha

    ; Sprite DMA
    lda #$02
    sta OAM_DMA

    ; Increment frame counter
    inc frame_cnt

    pla
    rti

irq:
    rti

init_ppu:
    ; Clear nametables, etc.
    rts

init_vars:
    lda #$00
    sta temp1
    sta temp2
    sta frame_cnt
    rts

update_game:
    rts

update_sprites:
    rts

; Vectors
.org $fffa
    .word nmi       ; NMI
    .word reset     ; RESET
    .word irq       ; IRQ
```

---

## 13. Troubleshooting {#troubleshooting}

### Common Errors

#### "Invalid addressing mode"

```
Error: Invalid addressing mode Immediate for instruction 'sta'
```

**Cause:** STA doesn't support immediate mode.
**Fix:** Store requires a memory address, not an immediate value.

```asm
; Wrong
sta #$00        ; Can't store immediate

; Right
sta $2000       ; Store to address
```

#### "Branch target out of range"

```
Error: Branch target out of range (150 bytes, must be -128 to +127)
```

**Cause:** Branch instructions have limited range.
**Fix:** Use JMP for long jumps or restructure code.

```asm
; Wrong - target too far
    beq far_away

; Right - use intermediate jump
    bne +
    jmp far_away
+
```

#### "Undefined symbol"

```
Error: Undefined symbol 'mylabel'
```

**Cause:** Label not defined or typo in name.
**Fix:** Check label spelling and ensure it's defined.

#### "Cannot evaluate operand"

```
Error: Cannot evaluate operand for instruction 'lda'
```

**Cause:** Expression uses undefined symbol.
**Fix:** Ensure all symbols in expression are defined.

### Debug Tips

1. **Use verbose mode:** `poppy -V source.asm`
2. **Generate listing:** `poppy -l output.lst source.asm`
3. **Check symbol values** in listing file
4. **Verify addresses** match expected locations

---

## 🔗 Resources

- [GitHub Repository](https://github.com/TheAnsarya/poppy)
- [6502 Reference](http://www.obelisk.me.uk/6502/)
- [NES Development Wiki](https://www.nesdev.org/wiki/)

---

**Happy coding! 🌸**

