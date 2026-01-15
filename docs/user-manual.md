# 📚 Poppy Compiler User Manual

> Complete guide to using the Poppy multi-system assembly compiler

**Version:** 1.0.0
**Updated:** January 15, 2026

---

## 📋 Table of Contents

1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Quick Start](#quick-start)
4. [Command-Line Usage](#command-line-usage)
5. [Project Files](#project-files)
6. [Assembly Syntax](#assembly-syntax)
7. [Directives Reference](#directives-reference)
8. [Addressing Modes](#addressing-modes)
9. [Expressions](#expressions)
10. [Labels and Symbols](#labels-and-symbols)
11. [Target Architectures](#target-architectures)
12. [Output Formats](#output-formats)
13. [Examples](#examples)
14. [Troubleshooting](#troubleshooting)

---

## 📖 Quick Reference

### All Directives

| Directive | Description | Example |
|-----------|-------------|---------|
| **Data** |||
| `.byte`, `.db` | Define byte(s) | `.byte $01, $02, "text"` |
| `.word`, `.dw` | Define 16-bit word(s) | `.word $1234, label` |
| `.long`, `.dl` | Define 24/32-bit value | `.long $123456` |
| `.ds`, `.res` | Reserve space (zeros) | `.ds 16` |
| `.fill` | Fill with value | `.fill 8, $ff` |
| **Layout** |||
| `.org` | Set program counter | `.org $8000` |
| `.align` | Align to boundary | `.align 256` |
| `.pad` | Pad to address | `.pad $fffa, $ff` |
| **Include** |||
| `.include` | Include source file | `.include "defs.pasm"` |
| `.incbin` | Include binary data | `.incbin "data.bin"` |
| **Macros** |||
| `.macro` | Define macro | `.macro NAME [params]` |
| `.endmacro` | End macro definition | `.endmacro` |
| `.rept` | Repeat block | `.rept 8 [, var]` |
| `.endr` | End repeat | `.endr` |
| **Conditionals** |||
| `.if` | If expression true | `.if DEBUG = 1` |
| `.ifdef` | If symbol defined | `.ifdef FEATURE_X` |
| `.ifndef` | If symbol not defined | `.ifndef RELEASE` |
| `.else` | Alternative block | `.else` |
| `.elseif` | Conditional alternative | `.elseif DEBUG = 2` |
| `.endif` | End conditional | `.endif` |
| **Assertions** |||
| `.assert` | Assert condition | `.assert * < $8000` |
| `.error` | Emit error | `.error "Not supported"` |
| `.warning` | Emit warning | `.warning "Deprecated"` |
| **Target** |||
| `.nes` | Set NES target | `.nes` |
| `.snes` | Set SNES target | `.snes` |
| `.gb`, `.gameboy` | Set Game Boy target | `.gb` |
| **Constants** |||
| `=`, `.equ`, `.define` | Define constant | `VALUE = $2000` |

### Label Types

| Type | Syntax | Scope | Example |
|------|--------|-------|---------|
| **Global** | `name:` | Entire file | `reset:` |
| **Local** | `@name:` | Current routine | `@loop:` |
| **Anonymous** | `-`, `+` | Nearest reference | `-` or `+` |
| **Named Anonymous** | `-name`, `+name` | Nearest reference | `+skip` |

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
```text
Poppy Compiler v0.1.0
Target architectures: 6502, 65816, SM83
Copyright (c) 2024
```

---

## 3. Quick Start {#quick-start}

### Your First Program

Create a file named `hello.pasm`:

```asm
; hello.pasm - Simple NES program
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
poppy hello.pasm

# With output name
poppy -o hello.bin hello.pasm

# Generate listing file
poppy -l hello.lst hello.pasm

# Verbose mode
poppy -V hello.pasm
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

```text
poppy [options] <input.pasm>
poppy --project [path] [options]
```

### Options

| Option | Long Form | Description |
|--------|-----------|-------------|
| `-o FILE` | `--output FILE` | Output file path |
| `-l FILE` | `--listing FILE` | Generate listing file |
| `-s FILE` | `--symbols FILE` | Generate symbol file |
| `-m FILE` | `--map FILE` | Generate memory map file |
| `-t ARCH` | `--target ARCH` | Target architecture (6502, 65816, sm83) |
| `-p [PATH]` | `--project [PATH]` | Build from project file |
| `-c NAME` | `--config NAME` | Build configuration (debug, release, etc.) |
| `-V` | `--verbose` | Verbose output |
| `-w` | `--watch` | Watch mode (auto-recompile on changes) |
| `-h` | `--help` | Show help |
| `--version` | | Show version |

### Examples

```bash
# Compile to specific output
poppy -o game.nes main.pasm

# Target SNES
poppy -t 65816 -o game.sfc main.pasm

# Generate all files with verbose output
poppy -V -l game.lst -o game.nes main.pasm

# Build from project file (current directory)
poppy --project

# Build from specific project directory
poppy --project ./my-game

# Build with release configuration
poppy --project -c release

# Build and watch for changes
poppy --project -w
```

### Project Mode

Project mode (`--project` or `-p`) builds from a `poppy.json` project file instead of a single source file. This enables:

- **Multiple source files** with organized include paths
- **Build configurations** (debug, release, etc.)
- **Consistent settings** across team members
- **Output organization** into build directories

See [Project Files](#project-files) for detailed project configuration.

---

## 5. Project Files {#project-files}

Project files (`poppy.json`) define build settings for multi-file projects.

### Basic Project File

```json
{
    "name": "MyGame",
    "version": "1.0.0",
    "target": "nes",
    "main": "main.pasm",
    "output": "game.nes"
}
```

### Complete Project Structure

```json
{
    "name": "HelloWorld",
    "version": "1.0.0",
    "target": "nes",
    "main": "main.pasm",
    "output": "hello.nes",
    "includes": [
        "include/"
    ],
    "defines": {
        "DEBUG": 0
    },
    "listing": "hello.lst",
    "symbols": "hello.sym",
    "mapfile": "hello.map"
}
```

### Project Properties

| Property | Type | Description |
|----------|------|-------------|
| `name` | string | Project name (required) |
| `version` | string | Project version |
| `target` | string | Target system: `nes`, `snes`, `gb` |
| `main` | string | Main source file |
| `output` | string | Output ROM file |
| `sources` | array | Source file patterns (if no main) |
| `includes` | array | Include search directories |
| `defines` | object | Preprocessor definitions |
| `listing` | string | Listing output file |
| `symbols` | string | Symbol file output |
| `mapfile` | string | Memory map output |
| `autolabels` | boolean | Auto-generate labels for jumps |

### Build Configurations

Configurations allow different build settings for debug vs release builds:

```json
{
    "name": "MyGame",
    "target": "nes",
    "main": "main.pasm",
    "output": "game.nes",
    "defines": {
        "DEBUG": 0
    },
    "configurations": {
        "debug": {
            "output": "bin/debug/game.nes",
            "listing": "bin/debug/game.lst",
            "symbols": "bin/debug/game.sym",
            "defines": {
                "DEBUG": 1
            }
        },
        "release": {
            "output": "bin/release/game.nes",
            "symbols": "bin/release/game.sym"
        }
    },
    "defaultConfiguration": "debug"
}
```

### Configuration Properties

| Property | Description |
|----------|-------------|
| `output` | Output file for this configuration |
| `listing` | Listing file for this configuration |
| `symbols` | Symbol file for this configuration |
| `mapfile` | Map file for this configuration |
| `defines` | Additional defines (merged with base) |

### Building with Configurations

```bash
# Build default configuration (debug)
poppy --project

# Build specific configuration
poppy --project -c release

# Build with verbose output
poppy --project -c debug --verbose
```

Configuration settings override base project settings. Defines are merged, with configuration values taking precedence.

### Cleaning Build Artifacts

The `clean` command removes build outputs from a project:

```bash
# Clean default configuration outputs
poppy clean --project

# Clean all configurations
poppy clean --project --all

# Clean with verbose output (shows deleted files)
poppy clean --project -V --all
```

The clean command removes:

- Output ROM files
- Symbol files
- Listing files
- Map files
- Empty build directories

---

## 6. Assembly Syntax {#assembly-syntax}

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

## 7. Directives Reference {#directives-reference}

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

### Alignment Directives

```asm
; Align to boundary
.align 256             ; Align to 256-byte boundary
.align 2               ; Align to even address

; Example: Align sprite data to page boundary
.align 256
sprite_data:
    .incbin "sprites.bin"

; Pad to specific address
.pad $c000             ; Fill with zeros until $c000
.pad $8000, $ff        ; Fill with $ff until $8000

; Example: Ensure interrupt vectors at end of ROM
.org $8000
code_start:
    ; ... code here ...
.pad $fffa             ; Pad to vector table
nmi_vector:
    .word nmi_handler
reset_vector:
    .word reset
irq_vector:
    .word irq_handler
```

**Features:**
- `.align N` - Align to N-byte boundary
- `.pad ADDR [, VALUE]` - Fill with value (default $00) until address
- Useful for page-aligned data and ROM layouts

### Constant Definition

```asm
; Using = syntax (preferred)
PPU_CTRL = $2000
SCREEN_WIDTH = 256

; Using .equ or .define
.define TILE_SIZE, 8
```

### Include Directives

```asm
; Include another assembly file
.include "macros.pasm"
.include "constants.pasm"

; Include binary data
.incbin "graphics.bin"
.incbin "music.bin" $0, $400    ; Include bytes $0-$400

; Relative paths
.include "../common/defs.pasm"
```

**Features:**
- Recursive includes supported
- Circular include detection
- Relative and absolute paths
- Error messages show correct file/line

### Conditional Assembly

```asm
; Conditional on symbol definition
.ifdef DEBUG
    lda #$ff            ; Debug code
    sta $2001
.endif

; Conditional on symbol NOT defined
.ifndef RELEASE
    jsr debug_routine   ; Only in debug builds
.endif

; With else clause
.ifdef PAL
    lda #50             ; PAL refresh rate
.else
    lda #60             ; NTSC refresh rate
.endif

; Expression-based conditionals
.if SCREEN_WIDTH > 256
    .error "Screen too wide"
.endif

; Nested conditionals
.ifdef SNES
    .ifdef DEBUG
        jsr snes_debug
    .endif
.endif
```

**Supported Directives:**
- `.ifdef SYMBOL` - If symbol defined
- `.ifndef SYMBOL` - If symbol not defined
- `.if EXPR` - If expression is non-zero
- `.else` - Alternative block
- `.elseif EXPR` - Conditional alternative
- `.endif` - End conditional block

### Macro System

```asm
; Define a macro
.macro PUSH_ALL
    pha
    phx
    phy
.endmacro

; Use the macro
    PUSH_ALL

; Macros with parameters
.macro SET_PPU_ADDR addr
    lda #>addr
    sta $2006
    lda #<addr
    sta $2006
.endmacro

; Use with argument
    SET_PPU_ADDR $2400

; Default parameters
.macro WAIT_FRAMES count = 1
    lda #count
    jsr wait_frames
.endmacro

    WAIT_FRAMES         ; Uses default: 1
    WAIT_FRAMES 5       ; Override: 5

; Nested macro calls
.macro INIT_SPRITE x, y
    lda #y
    sta $0200
    lda #x
    sta $0203
.endmacro

.macro SETUP_PLAYER
    INIT_SPRITE 100, 120
.endmacro
```

**Features:**
- Parameter substitution
- Default parameter values
- Nested macro calls
- Local label scoping within macros
- Full recursion support

### Repeat Directive

```asm
; Repeat a block of code
.rept 8
    nop
.endr

; With counter variable
.rept 16, i
    .byte i * 2
.endr

; Result: .byte 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30

; Generate lookup tables
.rept 256, n
    .byte (n * 3) / 2
.endr
```

### Assertion Directives

```asm
; Assert a condition
.assert * < $8000, "Code exceeded bank boundary"

; Static error messages
.error "This configuration is not supported"

; Static warnings
.warning "Using deprecated feature"

; Conditional errors
.if BUFFER_SIZE < 256
    .error "Buffer too small"
.endif
```

---

## 8. Addressing Modes {#addressing-modes}

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

## 9. Expressions {#expressions}

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

## 10. Labels and Symbols {#labels-and-symbols}

### Global Labels

```asm
reset:                  ; Define global label
    jmp reset           ; Reference global label

CONSTANT = $2000        ; Define constant
    sta CONSTANT        ; Use constant
```

### Local Labels

Local labels are scoped to the nearest global label. They start with `@` and are local to their containing routine:

```asm
routine1:
    ldx #10
    @loop:              ; Local to routine1
        dex
        bne @loop
    rts

routine2:
    ldy #5
    @loop:              ; Different label, local to routine2
        dey
        bne @loop       ; References routine2's @loop
    rts

; You can have the same local label name in different routines
init:
    @wait:
        lda $2002
        bpl @wait
    rts
```

**Features:**
- Scoped to nearest global label
- Same name can be reused in different routines
- Cannot be referenced outside their scope
- Cleaner code organization

### Anonymous Labels

Anonymous labels provide quick forward/backward references without naming:

```asm
; Backward references use -
    ldx #$10
-                       ; Anonymous backward label
    dex
    bne -               ; Branch to previous -

; Forward references use +
    lda #$00
    beq +               ; Branch to next +
    nop
    nop
+                       ; Anonymous forward label
    sta $2000

; Multiple levels
    ldx #5
--                      ; Outer loop
    ldy #10
-                       ; Inner loop
    dey
    bne -               ; Branch to nearest -
    dex
    bne --              ; Branch to nearest --

; Named anonymous labels
    lda #0
    beq +skip           ; Branch to next +skip
    inc $00
+skip
    rts
```

**Features:**
- `-` references nearest previous anonymous label
- `+` references nearest next anonymous label
- Multiple `-` or `+` for different nesting levels
- Named variants (`+name`, `-name`) for clarity
- Reduces label clutter for small jumps

---

## 11. Target Architectures {#target-architectures}

### 6502 (NES)

Default target. All standard 6502 instructions supported.

```bash
poppy -t 6502 game.pasm
```

Supported features:

- All 56 official opcodes
- All 13 addressing modes
- Automatic zero-page optimization

### 65816 (SNES)

The WDC 65816 is the processor used in the Super Nintendo Entertainment System (SNES).

```bash
poppy -t 65816 game.pasm
```

#### 65816 Features

- All 6502 instructions plus 65816 extensions
- 16-bit accumulator and index register modes
- 24-bit address space (16MB)
- Stack relative addressing
- Block move instructions (MVN/MVP)
- Direct page relocation

#### Memory Mapping Directives

```asm
.snes                   ; Set target to SNES/65816
.lorom                  ; LoROM mapping (32KB banks, $8000-$ffff)
.hirom                  ; HiROM mapping (64KB banks, $0000-$ffff)
.exhirom                ; ExHiROM mapping (extended, up to 8MB)
```

#### Register Size Directives

The 65816 can operate with 8-bit or 16-bit accumulator and index registers.
Use these directives to inform the assembler of the current register sizes:

```asm
.a8                     ; Accumulator is 8-bit (M flag = 1)
.a16                    ; Accumulator is 16-bit (M flag = 0)
.i8                     ; Index registers are 8-bit (X flag = 1)
.i16                    ; Index registers are 16-bit (X flag = 0)
.smart                  ; Auto-track M/X flags from REP/SEP
```

Example:

```asm
.snes
.lorom

.org $8000
reset:
    sei
    clc
    xce                     ; Switch to native mode

    ; Set 16-bit accumulator, 8-bit index
    rep #$20                ; Clear M flag (16-bit A)
    .a16
    sep #$10                ; Set X flag (8-bit X/Y)
    .i8

    lda #$1234              ; 3-byte instruction (16-bit immediate)
    ldx #$ff                ; 2-byte instruction (8-bit immediate)

    ; Switch to 8-bit accumulator
    sep #$20                ; Set M flag (8-bit A)
    .a8

    lda #$42                ; 2-byte instruction (8-bit immediate)
```

#### SNES Header Directives

Configure the internal SNES ROM header:

```asm
.snes_title "MY GAME"       ; Game title (up to 21 characters)
.snes_region "USA"          ; Region: "Japan", "USA", "Europe"
.snes_version 1             ; Version number (0-255)
.snes_rom_size 256          ; ROM size in KB (128, 256, 512, etc.)
.snes_ram_size 8            ; Save RAM size in KB (0, 2, 8, 32, etc.)
.fastrom                    ; Enable FastROM mode (3.58 MHz)
```

#### 65816-Specific Addressing Modes

```asm
; Direct Page
lda $12                     ; Direct page (like zero page)
lda $12,x                   ; Direct page indexed X
lda $12,y                   ; Direct page indexed Y

; Absolute
lda $1234                   ; Absolute
lda $1234,x                 ; Absolute indexed X
lda $1234,y                 ; Absolute indexed Y

; Long (24-bit)
lda $7e1234                 ; Absolute long
lda $7e1234,x               ; Absolute long indexed X

; Indirect
lda ($12)                   ; Direct page indirect
lda ($12,x)                 ; Direct page indexed indirect
lda ($12),y                 ; Direct page indirect indexed
lda [$12]                   ; Direct page indirect long
lda [$12],y                 ; Direct page indirect long indexed

; Stack Relative
lda $03,s                   ; Stack relative
lda ($03,s),y               ; Stack relative indirect indexed

; Immediate with size hints
lda.b #$12                  ; Force 8-bit immediate
lda.w #$1234                ; Force 16-bit immediate
```

#### Block Move Instructions

```asm
; Move 256 bytes from bank $7e to bank $7f
lda #$00ff                  ; Number of bytes - 1
ldx #$0000                  ; Source offset
ldy #$0000                  ; Destination offset
mvn $7f, $7e                ; Move negative (increment)

; Or use move positive
mvp $7f, $7e                ; Move positive (decrement)
```

#### 65816 Vector Table

```asm
.org $ffe0
; Native mode vectors
    .word 0                 ; Reserved
    .word 0                 ; Reserved
    .word cop_handler       ; COP
    .word brk_handler       ; BRK
    .word abort_handler     ; ABORT
    .word nmi_handler       ; NMI
    .word 0                 ; Reserved
    .word irq_handler       ; IRQ

.org $fff0
; Emulation mode vectors
    .word 0                 ; Reserved
    .word 0                 ; Reserved
    .word cop_handler       ; COP
    .word 0                 ; Reserved
    .word abort_handler     ; ABORT
    .word nmi_handler       ; NMI
    .word reset_handler     ; RESET
    .word irq_handler       ; IRQ/BRK
```

### SM83 (Game Boy) - Coming Soon

```bash
poppy -t sm83 game.pasm
```

Features:

- Game Boy specific instruction set
- Z80-derived syntax
- CB-prefixed extended instructions

---

## 12. Output Formats {#output-formats}

### Raw Binary (Default)

Plain binary output with no headers.

```bash
poppy -o output.bin source.pasm
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

### SFC Format

SNES ROM with internal header. The header is automatically generated when you use
SNES header directives.

```asm
.snes
.lorom
.snes_title "MY SNES GAME"
.snes_region "USA"
.snes_version 1
.snes_rom_size 256          ; 256KB ROM
.snes_ram_size 8            ; 8KB SRAM

.org $8000
reset:
    sei
    clc
    xce
    ; ... game code ...

; Vectors
.org $fffc
    .word reset             ; Reset vector
    .word reset             ; NMI vector (placeholder)
```

The SFC header includes:

- Game title (21 characters)
- Map mode (LoROM/HiROM/ExHiROM)
- ROM type and size
- RAM size
- Region/country code
- Version number
- Checksums (auto-calculated)

### Listing File

Generate assembly listing with addresses and machine code:

```bash
poppy -l output.lst source.pasm
```

Output format:
```text
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

## 13. Examples {#examples}

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

## 14. Troubleshooting {#troubleshooting}

### Common Errors

#### "Invalid addressing mode"

```text
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

```text
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

```text
Error: Undefined symbol 'mylabel'
```

**Cause:** Label not defined or typo in name.
**Fix:** Check label spelling and ensure it's defined.

#### "Cannot evaluate operand"

```text
Error: Cannot evaluate operand for instruction 'lda'
```

**Cause:** Expression uses undefined symbol.
**Fix:** Ensure all symbols in expression are defined.

### Debug Tips

1. **Use verbose mode:** `poppy -V source.pasm`
2. **Generate listing:** `poppy -l output.lst source.pasm`
3. **Check symbol values** in listing file
4. **Verify addresses** match expected locations

---

## 🔗 Resources

- [GitHub Repository](https://github.com/TheAnsarya/poppy)
- [6502 Reference](http://www.obelisk.me.uk/6502/)
- [NES Development Wiki](https://www.nesdev.org/wiki/)

---

**Happy coding! 🌸**

