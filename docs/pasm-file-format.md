# 📄 Poppy Assembly (.pasm) File Format

## Overview

Poppy uses the `.pasm` file extension for assembly source files to distinguish them from standard assembler formats. This extension indicates that the file uses Poppy's enhanced assembly syntax with support for:

- Modern macro system with parameters
- Conditional assembly directives  
- Multi-architecture support (6502, 65816, SM83)
- Platform-specific directives (.ines, .target, etc.)
- Unicode and emoji support in comments
- Flexible formatting options

## Why `.pasm`?

The `.pasm` extension:

✅ **Distinguishes Poppy files** from standard `.asm` files  
✅ **Indicates enhanced features** not found in traditional assemblers  
✅ **Enables proper syntax highlighting** in editors with Poppy support  
✅ **Avoids conflicts** with other assembler file associations  
✅ **Makes file purpose clear** in mixed-assembler projects

## File Structure

### Basic Format

```asm
; comment - Standard semicolon comments
// comment - Alternative C-style comments
# comment  - Alternative hash comments

; Labels
label_name:
	instruction operand

; Local labels (start with @)
global_label:
@local:
	instruction operand
```

### Character Encoding

All `.pasm` files must be:
- **UTF-8 encoded with BOM**
- **CRLF line endings** (Windows style)
- Support Unicode characters in:
	- Comments
	- String literals
	- Symbol names (letters, digits, underscore)

### Indentation

Poppy uses **tabs for indentation** (never spaces):
- Tab width: 4 spaces (8 for pure assembly sections)
- Labels at column 0 (no indent)
- Instructions indented with 1 tab
- Nested blocks indented further

## Syntax Features

### Case Sensitivity

- **Opcodes:** Case-insensitive (`LDA`, `lda`, `Lda` all valid)
- **Labels:** Case-sensitive (`Reset` ≠ `reset`)
- **Directives:** Case-insensitive (`.BYTE`, `.byte` same)
- **Reserved words:** Case-insensitive validation

### Number Formats

```asm
; Hexadecimal (always lowercase)
.byte $ff, $0a, $1234

; Decimal
.byte 255, 10

; Binary
.byte %11111111, %00001010

; Character
.byte 'A', 'B'
```

**Important:** Hexadecimal values must use lowercase letters (`$ff` not `$FF`)

### String Literals

```asm
; Standard strings
.byte "Hello, World!", 0

; Unicode support
.byte "你好世界", 0  ; Chinese
.byte "🎮 Game", 0   ; Emojis

; Escape sequences
.byte "Line 1\nLine 2", 0
.byte "Tab\there", 0
```

### Comments

```asm
; Semicolon comments - Traditional style
lda #$00  ; End-of-line comment

// C-style comments - Alternative
lda #$00  // Also end-of-line

# Hash comments - Alternative
lda #$00  # Yet another style
```

## Directives

### Platform Directives

```asm
; NES-specific
.ines mapper 0
.ines prg_banks 2
.ines chr_banks 1
.ines mirroring horizontal

; SNES-specific  
.target snes
.lorom
.org $8000

; Multi-platform
.target 6502    ; NES
.target 65816   ; SNES
.target sm83    ; Game Boy
```

### Data Directives

```asm
; Byte data
.byte $00, $01, $02
.db $00, $01, $02    ; Alias

; Word data (16-bit, little-endian)
.word $1234, $5678
.dw $1234, $5678     ; Alias

; Long data (24-bit)
.long $123456
.dl $123456          ; Alias

; ASCII/String
.byte "Text", 0

; Fill/Reserve space
.fill 256, $ff       ; Fill 256 bytes with $ff
.res 128             ; Reserve 128 bytes (uninitialized)
```

### Assembly Directives

```asm
; Origin - Set assembly address
.org $8000

; Alignment
.align 256           ; Align to 256-byte boundary

; Constants
.equ PPUCTRL, $2000
.define SCREEN_WIDTH, 256

; Include files
.include "macros.pasm"
.include "constants.pasm"
```

## Macros

Macros are reusable code templates. Macro invocations require the `@` prefix to distinguish them from instructions.

### Macro Definition

```asm
; Simple macro
.macro wait_vblank
	:
		bit $2002
		bpl :-
.endmacro

; Macro with parameters (space-separated)
.macro set_ppu_addr address
	bit $2002
	lda #>address
	sta $2006
	lda #<address
	sta $2006
.endmacro

; Macro with parameters (comma-separated)
.macro sprite_dma, addr, count
	lda #>addr
	sta $2003
	lda #<addr
	sta $2004
	ldx count
.endmacro
```

### Macro Invocation

```asm
; Call macro without parameters
@wait_vblank

; Call macro with parameters
@set_ppu_addr $2000
@sprite_dma sprite_data, 64
```

### Reserved Names

Macros cannot use reserved names:
- 6502/65816 opcodes (`lda`, `sta`, `jmp`, etc.)
- Directives (`org`, `byte`, `include`, etc.)
- Macro keywords (`macro`, `endmacro`, `if`, `endif`, etc.)

## Conditional Assembly

### Basic Conditionals

```asm
; .if/.else/.endif - General conditional
.if DEBUG
	.byte "Debug Build", 0
.else
	.byte "Release Build", 0
.endif

; Symbol existence checks
.ifdef FEATURE_SOUND
	jsr init_sound
.endif

.ifndef NO_GRAPHICS
	jsr init_ppu
.endif
```

### Comparison Conditionals

Comparison conditionals evaluate two expressions and assemble code based on the result.

#### Equality Comparisons

```asm
; .ifeq left, right - Assemble if left == right
.ifeq MAPPER, 0
	.byte "NROM mapper", 0
.endif

; .ifne left, right - Assemble if left != right
.ifne TARGET, NES
	.error "This code is NES-only"
.endif
```

#### Relational Comparisons

```asm
; .ifgt left, right - Assemble if left > right
.ifgt PRG_SIZE, $8000
	.warning "Large PRG-ROM detected"
.endif

; .iflt left, right - Assemble if left < right
.iflt RAM_SIZE, 2048
	.error "Insufficient RAM"
.endif

; .ifge left, right - Assemble if left >= right
.ifge VERSION, 2
	jsr new_feature
.endif

; .ifle left, right - Assemble if left <= right
.ifle BUFFER_SIZE, 256
	.byte "Small buffer", 0
.endif
```

#### With Expressions

```asm
; Comparison operands can be complex expressions
SIZE = 128
.ifgt SIZE * 2, 200
	.byte $01  ; SIZE * 2 > 200 (256 > 200)
.else
	.byte $02
.endif

; Using defined symbols
MAPPER_MMC1 = 1
MAPPER_MMC3 = 4

.ifeq MAPPER, MAPPER_MMC3
	jsr init_mmc3
.endif
```

#### Nested Conditionals

```asm
; Comparison conditionals can be nested
DEBUG = 1
LEVEL = 2

.ifeq DEBUG, 1
	.ifgt LEVEL, 1
		.byte "Debug mode, level > 1", 0
	.endif
.endif

; With .else blocks
TARGET = 65816

.ifeq TARGET, 6502
	.byte "6502 code"
.else
	.byte "65816 code"
.endif
```

### Supported Comparison Operators

| Directive | Operator | Meaning |
|-----------|----------|---------|
| `.ifeq`   | `==`     | Equal to |
| `.ifne`   | `!=`     | Not equal to |
| `.ifgt`   | `>`      | Greater than |
| `.iflt`   | `<`      | Less than |
| `.ifge`   | `>=`     | Greater or equal |
| `.ifle`   | `<=`     | Less or equal |

### Common Use Cases

```asm
; Platform selection
.ifeq TARGET_PLATFORM, PLATFORM_NES
	.include "nes_init.pasm"
.else
	.include "snes_init.pasm"
.endif

; Feature gating by version
.ifge VERSION, 3
	jsr enhanced_graphics
.else
	jsr basic_graphics
.endif

; Size-based optimization
.iflt CODE_SIZE, 256
	; Use zero-page addressing
	lda $00
.else
	; Use absolute addressing
	lda $0200
.endif

; Mapper-specific code
.ifeq MAPPER, 1  ; MMC1
	jsr mmc1_bank_switch
.endif

.ifeq MAPPER, 4  ; MMC3
	jsr mmc3_bank_switch
.endif
```

## Repeat Blocks

Repeat blocks allow you to generate repeated code or data with a single directive.

### Basic Syntax

```asm
; .rept count
;   ...body...
; .endr

; Fill buffer with zeros
.rept 256
	.byte $00
.endr

; Unrolled loop
.rept 8
	asl a
	rol $00
.endr
```

### With Expressions

```asm
; Count can be any constant expression
BUFFER_SIZE = 128

.rept BUFFER_SIZE / 2
	.word $0000
.endr

; Nested repeats
.rept 4
	.rept 8
		.byte $ff
	.endr
.endr
```

## Enumeration Blocks

Enumeration blocks define a sequence of consecutive constants with auto-increment.

### Basic Syntax

```asm
; .enum start_value
;   SYMBOL1
;   SYMBOL2
;   ...
; .ende

; Zero page variables
.enum $00
	player_x
	player_y
	player_state
	enemy_count
.ende
; player_x = $00, player_y = $01, player_state = $02, enemy_count = $03
```

### Explicit Values

```asm
; Override auto-increment with explicit values
.enum $2000
	PPUCTRL
	PPUMASK
	PPUSTATUS
	OAMADDR = $2003  ; Skip to $2003
	OAMDATA          ; Auto-continues from $2004
	PPUSCROLL
	PPUADDR
	PPUDATA
.ende
```

### Size Modifiers

```asm
; Use .db, .dw, .dl to control increment size
.enum $0200
	sprite_x    .db  ; +1 byte (default)
	sprite_y    .db  ; +1 byte
	sprite_ptr  .dw  ; +2 bytes (word)
	sprite_bank .db  ; +1 byte
	position    .dl  ; +3 bytes (65816 long address)
.ende
; sprite_x=$0200, sprite_y=$0201, sprite_ptr=$0202, sprite_bank=$0204, position=$0205
```

### Use Cases

```asm
; RAM layout definition
.enum $0000
	temp1
	temp2
	temp3
	pointer  .dw
	counter
.ende

; Memory-mapped I/O
.enum $4000
	SQ1_VOL = $4000
	SQ1_SWEEP
	SQ1_LO
	SQ1_HI
	SQ2_VOL
.ende

; Bit flags
.enum 0
	FLAG_CARRY
	FLAG_ZERO
	FLAG_INTERRUPT
	FLAG_DECIMAL
.ende
; FLAG_CARRY=0, FLAG_ZERO=1, FLAG_INTERRUPT=2, FLAG_DECIMAL=3
```

## Labels

### Global Labels

```asm
reset:              ; Global label
	lda #$00
	sta $2000

nmi:                ; Another global
	rti
```

### Local Labels

```asm
reset:
	ldx #$00
@clear_loop:        ; Local to 'reset'
	sta $0000, x
	inx
	bne @clear_loop
	rts

nmi:
@wait:              ; Different @wait (local to 'nmi')
	bit $2002
	bpl @wait
	rti
```

### Anonymous Labels

```asm
reset:
	ldx #$00
:                   ; Anonymous forward label
	sta $0000, x
	inx
	bne :-          ; Branch to previous ':'
	
:                   ; New anonymous label
	lda #$01
	bne :+          ; Branch to next ':'
	
:                   ; Target of :+
	rts
```

## Expressions

### Arithmetic

```asm
.byte 10 + 5        ; Addition
.byte 10 - 5        ; Subtraction
.byte 10 * 5        ; Multiplication  
.byte 10 / 5        ; Division
.byte 10 % 3        ; Modulo
```

### Bitwise

```asm
.byte $0f & $f0     ; AND
.byte $0f | $f0     ; OR
.byte $0f ^ $f0     ; XOR
.byte ~$0f          ; NOT
.byte $01 << 4      ; Left shift
.byte $10 >> 2      ; Right shift
```

### Address Operators

```asm
.byte <$1234        ; Low byte ($34)
.byte >$1234        ; High byte ($12)
.word ^$123456      ; Bank byte ($12 for 24-bit address)
```

## Complete Example

```asm
; game.pasm - Simple NES game
;
; A complete example showing Poppy assembly syntax

; ============================================================================
; iNES Header
; ============================================================================

.ines mapper 0
.ines prg_banks 1
.ines chr_banks 1
.ines mirroring vertical

; ============================================================================
; Constants
; ============================================================================

.equ PPUCTRL,   $2000
.equ PPUMASK,   $2001
.equ PPUSTATUS, $2002
.equ PPUADDR,   $2006
.equ PPUDATA,   $2007

; ============================================================================
; Macros
; ============================================================================

.macro wait_vblank
:
	bit PPUSTATUS
	bpl :-
.endmacro

.macro ppu_addr, address
	bit PPUSTATUS
	lda #>address
	sta PPUADDR
	lda #<address
	sta PPUADDR
.endmacro

; ============================================================================
; PRG-ROM
; ============================================================================

.org $8000

reset:
	sei
	cld
	
	; Wait for PPU warmup
	wait_vblank
	wait_vblank
	
	; Initialize PPU
	lda #$00
	sta PPUCTRL
	sta PPUMASK
	
	; Clear palette
	@ppu_addr $3f00
	ldx #$20
@clear_palette:
	sta PPUDATA
	dex
	bne @clear_palette
	
	; Enable rendering
	lda #%00011110
	sta PPUMASK
	
	; Main loop
@main_loop:
	wait_vblank
	jmp @main_loop

nmi:
	rti

; ============================================================================
; Vectors
; ============================================================================

.org $fffa
.word nmi
.word reset
.word 0

; ============================================================================
; CHR-ROM
; ============================================================================

.org $0000
.incbin "graphics.chr"
```

## Best Practices

### Style Guidelines

1. **Use tabs for indentation** - Never spaces
2. **Lowercase hex values** - `$ff` not `$FF`
3. **Meaningful label names** - `player_init` not `p1`
4. **Comment thoroughly** - Explain complex logic
5. **Organize with sections** - Use comment banners
6. **One statement per line** - For clarity

### File Organization

```
project/
├── src/
│   ├── main.pasm          # Entry point
│   ├── graphics.pasm      # Graphics routines
│   ├── sound.pasm         # Sound routines
│   ├── macros.pasm        # Shared macros
│   └── constants.pasm     # Shared constants
├── data/
│   ├── graphics.chr       # CHR data
│   └── music.bin          # Music data
└── build/
    └── game.nes           # Output ROM
```

### Portability

For maximum portability:
- Avoid platform-specific directives when possible
- Use `.target` to explicitly declare architecture
- Separate platform-specific code with conditionals
- Document architecture requirements in comments

## File Extensions

- `.pasm` - Poppy assembly source
- `.chr` - CHR-ROM graphics data (binary)
- `.bin` - Generic binary data
- `.inc` - Legacy include files (use `.pasm` instead)

## Editor Support

Configure your editor to:
- Recognize `.pasm` as assembly language
- Use tabs (not spaces) for indentation
- Set tab width to 4 (or 8 for pure assembly)
- Save as UTF-8 with BOM
- Use CRLF line endings

## Migration from .asm

To convert existing `.asm` files:

1. Rename files: `*.asm` → `*.pasm`
2. Update include directives: `.include "file.asm"` → `.include "file.pasm"`
3. Verify hex values are lowercase
4. Ensure UTF-8 with BOM encoding
5. Test with Poppy compiler

---

**See Also:**
- [User Manual](user-manual.md) - Complete language reference
- [README](../README.md) - Quick start guide
- [Examples](../~manual-testing/) - Sample `.pasm` files
