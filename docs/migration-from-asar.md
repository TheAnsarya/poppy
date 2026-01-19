# Migration Guide: ASAR to Poppy

This guide helps users transition from ASAR (Asar Super-Famicom Assembler) to Poppy Assembly.

## Overview

ASAR and Poppy share many similarities as both target SNES/65816 development. However, there are syntax and feature differences to be aware of.

## Quick Start

### Automated Conversion

Poppy includes a built-in converter that can automatically migrate ASAR projects:

```bash
# Convert a single file
poppy convert game.asm -o game.pasm --from asar

# Convert an entire project directory
poppy convert project/ --convert-project --from asar -o output/

# Auto-detect format (works for most ASAR files)
poppy convert game.asm -o game.pasm --auto
```

## Directive Equivalents

| ASAR | Poppy | Notes |
|------|-------|-------|
| `org $address` | `.org $address` | Same functionality |
| `base $address` | `.base $address` | Same functionality |
| `db`, `dw`, `dl`, `dd` | `.db`, `.dw`, `.dl`, `.dd` | Dot prefix required |
| `incbin "file"` | `.incbin "file"` | Same functionality |
| `incsrc "file"` | `.include "file"` | Renamed directive |
| `print "text"` | `.print "text"` | Same functionality |
| `error "text"` | `.error "text"` | Same functionality |
| `warn "text"` | `.warning "text"` | Slightly different name |
| `assert condition` | `.assert condition` | Same functionality |
| `namespace name` | `.scope name` | Renamed directive |
| `endif` | `.endif` | Dot prefix required |

## Macro Syntax

### ASAR Style
```asm
macro MyMacro(param1, param2)
    lda <param1>
    sta <param2>
endmacro

%MyMacro($10, $20)
```

### Poppy Style
```asm
.macro MyMacro, param1, param2
    lda \param1
    sta \param2
.endmacro

%MyMacro $10, $20
```

### Key Differences

- Use `.macro` and `.endmacro` with dot prefix
- Parameters listed after macro name with commas
- Parameter references use `\param` instead of `<param>`
- Invocation uses `%` prefix (same as ASAR)
- No parentheses around arguments in invocation

## Label Syntax

### Global Labels
```asm
; ASAR
MyLabel:

; Poppy (same)
MyLabel:
```

### Local Labels
```asm
; ASAR
.localLabel:

; Poppy (same)
.localLabel:
```

### Anonymous Labels
```asm
; ASAR
-
    dex
    bne -

; Poppy (same)
-:
    dex
    bne -
```

Note: Poppy requires a colon after anonymous label definitions.

## Math Expressions

### Operators

| Operation | ASAR | Poppy |
|-----------|------|-------|
| Addition | `+` | `+` |
| Subtraction | `-` | `-` |
| Multiplication | `*` | `*` |
| Division | `/` | `/` |
| Modulo | `%` | `%` |
| Bitwise AND | `&` | `&` |
| Bitwise OR | `\|` | `\|` |
| Bitwise XOR | `^` | `^` |
| Shift Left | `<<` | `<<` |
| Shift Right | `>>` | `>>` |
| Low Byte | `<value` | `<(value)` |
| High Byte | `>value` | `>(value)` |
| Bank Byte | `^value` | `^(value)` |

### Expression Examples
```asm
; ASAR
lda #<MyLabel
lda #>MyLabel
lda #^MyLabel

; Poppy
lda #<(MyLabel)
lda #>(MyLabel)
lda #^(MyLabel)
```

## Addressing Modes

### Standard Modes (Same Syntax)
```asm
lda #$10        ; Immediate
lda $10         ; Zero Page / Direct Page
lda $1000       ; Absolute
lda $10,x       ; Zero Page,X
lda $1000,x     ; Absolute,X
lda ($10),y     ; Indirect,Y
lda ($10,x)     ; Indexed Indirect
```

### 65816 Long Addressing
```asm
; ASAR
lda.l $7e1000
jsl $008000

; Poppy (same)
lda.l $7e1000
jsl $008000
```

## Conditional Assembly

### ASAR
```asm
if condition
    ; code
elseif other
    ; code
else
    ; code
endif
```

### Poppy
```asm
.if condition
    ; code
.elseif other
    ; code
.else
    ; code
.endif
```

## SNES-Specific Features

### ROM Header
```asm
; ASAR uses various directives spread throughout
; Poppy uses a single JSON-style header

; ASAR
lorom
org $00FFB0
db "GAME TITLE          "
; ... many more lines

; Poppy
.snes {"title": "GAME TITLE", "mode": "lorom", "speed": "slow"}
```

### LoROM/HiROM
```asm
; ASAR
lorom
; or
hirom

; Poppy (in project file or header directive)
.snes {"mode": "lorom"}
.snes {"mode": "hirom"}
```

### Freespace/Freecode (ASAR-specific)
```asm
; ASAR has freespace finding for ROM hacking
freecode
    ; code here
freespace

; Poppy: Not directly supported - use explicit .org
; This feature is specific to ROM hacking workflows
```

## Repeat Blocks

### ASAR
```asm
rep 16
    nop
```

### Poppy
```asm
.repeat 16
    nop
.endrepeat
```

## Defines/Constants

### ASAR
```asm
!myConstant = $1000
lda !myConstant

; or
define myValue $20
```

### Poppy
```asm
myConstant = $1000
lda myConstant

; or
.define myValue $20
```

Note: Poppy doesn't require the `!` prefix for constants.

## Example Conversion

### ASAR Original
```asm
lorom

org $008000

!PlayerX = $0010
!PlayerY = $0012

macro SetPosition(x, y)
    lda #<x>
    sta !PlayerX
    lda #<y>
    sta !PlayerY
endmacro

Reset:
    sei
    clc
    xce
    rep #$30
    
    %SetPosition($80, $70)
    
-
    wai
    bra -

org $00FFFC
    dw Reset
```

### Poppy Equivalent
```asm
.snes {"mode": "lorom"}

.org $008000

PlayerX = $0010
PlayerY = $0012

.macro SetPosition, x, y
    lda #\x
    sta PlayerX
    lda #\y
    sta PlayerY
.endmacro

Reset:
    sei
    clc
    xce
    rep #$30
    
    %SetPosition $80, $70
    
-:
    wai
    bra -

.org $00fffc
    .dw Reset
```

## Feature Comparison

| Feature | ASAR | Poppy |
|---------|------|-------|
| 6502 Support | ✅ | ✅ |
| 65816 Support | ✅ | ✅ |
| Macros | ✅ | ✅ |
| Conditional Assembly | ✅ | ✅ |
| Structs | ✅ | ✅ |
| ROM Patching/Freespace | ✅ | ❌ |
| NES Support | ❌ | ✅ |
| Game Boy Support | ❌ | ✅ |
| Project Files | ❌ | ✅ |
| VS Code Integration | ❌ | ✅ |

## Tips for Migration

1. **Add dot prefixes** to all directives (`.org`, `.db`, etc.)
2. **Convert macro syntax** - parameters use backslash, not angle brackets
3. **Remove `!` prefix** from constants
4. **Add colons** to anonymous labels
5. **Use parentheses** for byte-selection operators in expressions
6. **Replace `incsrc`** with `.include`
7. **Consider using poppy.json** for project configuration instead of in-file directives

## Getting Help

- Check the [Poppy documentation](../README.md)
- See [example projects](../examples/)
- File issues on [GitHub](https://github.com/TheAnsarya/poppy/issues)
