# Migration Guide: xkas to Poppy

This guide helps users transition from xkas to Poppy Assembly (PASM).

## Overview

xkas is a classic SNES/65816 assembler known for its simplicity and straightforward syntax. Poppy maintains a similar simplicity while adding modern features and better tooling.

## Quick Start

### Automated Conversion

Poppy includes a built-in converter that can automatically migrate xkas projects:

```bash
# Convert a single file
poppy convert game.asm -o game.pasm --from xkas

# Convert an entire project directory
poppy convert project/ --convert-project --from xkas -o output/

# Auto-detect format (works for most xkas files)
poppy convert game.asm -o game.pasm --auto
```

## Directive Equivalents

| xkas | Poppy | Notes |
|------|-------|-------|
| `db` | `.db` | Dot prefix required |
| `dw` | `.dw` | Dot prefix required |
| `dl` | `.dl` | Dot prefix required |
| `dd` | `.dd` | Dot prefix required |
| `incbin "file"` | `.incbin "file"` | Same functionality |
| `incsrc "file"` | `.include "file"` | Renamed directive |
| `org $address` | `.org $address` | Same functionality |
| `base $address` | `.base $address` | Same functionality |
| `fill count` | `.fill count` | Same functionality |
| `fillbyte $xx` | `.fillbyte $xx` | Same functionality |
| `table "file"` | `.table "file"` | Same functionality |
| `cleartable` | `.cleartable` | Same functionality |
| `header` | `.header` | Same functionality |
| `lorom` | `.lorom` | Same functionality |
| `hirom` | `.hirom` | Same functionality |
| `arch 65816` | `.arch 65816` | Dot prefix required |

## Syntax Differences

### Dot Prefix Requirement

The most significant difference is that Poppy requires dot prefixes on all directives:

#### xkas
```asm
lorom
org $008000
db $01, $02, $03
incbin "data.bin"
```

#### Poppy
```pasm
.lorom
.org $008000
.db $01, $02, $03
.incbin "data.bin"
```

### Labels

Labels work the same way in both assemblers:

```pasm
main:
    lda #$00
    sta $2100
    jmp main

.data:                  ; Local labels also supported
    .db $12, $34
```

### Comments

Both assemblers use semicolon comments:

```pasm
lda #$00            ; This is a comment
; Full line comment
```

## ROM Layout

### Header Mode

Both xkas and Poppy support the same ROM layout directives:

```pasm
.lorom              ; LoROM mapping ($00:8000-$00:FFFF mirrored)
.hirom              ; HiROM mapping ($C0:0000-$C0:FFFF)
.header             ; Include 512-byte SMC header

.org $008000        ; Set PC (Program Counter)
```

## Data Definitions

### Bytes and Words

```pasm
; Define bytes
.db $01, $02, $03, $04
.db "Hello"                 ; ASCII string

; Define words (16-bit)
.dw $1234, $5678
.dw label_address

; Define long (24-bit)
.dl $123456
.dl far_label

; Define double (32-bit)
.dd $12345678
```

### Fill Operations

```pasm
.fillbyte $ff               ; Set fill byte
.fill 16                    ; Fill 16 bytes with $ff

; Or combine in one line
.fill 16, $00               ; Fill 16 bytes with $00
```

## Includes

### Binary Files

```pasm
; Include binary data
.incbin "graphics.bin"
.incbin "music.bin", $10, $100      ; With offset and size
```

### Source Files

```pasm
; xkas: incsrc "file.asm"
; Poppy:
.include "file.pasm"
```

## Tables for Text

xkas and Poppy handle text tables identically:

```pasm
.table "game.tbl"           ; Load character mappings
.db "Hello World"           ; Converts using table
.cleartable                 ; Reset to ASCII
```

## Migration Checklist

1. **Add dot prefixes** to all directives
2. **Rename `incsrc`** to `.include`
3. **Update file extensions** from `.asm` to `.pasm`
4. **Test assembly** with `poppy --project`

## Unsupported Features

The following xkas features are not directly supported in Poppy:

| xkas Feature | Poppy Alternative |
|--------------|-------------------|
| `rep n` | Use loops or copy-paste |

## Example Migration

### Original xkas Code

```asm
lorom
header

org $008000

main:
    sei
    clc
    xce
    rep #$30
    
    lda #$0000
    sta $2100
    
    jmp main

data:
    db $01, $02, $03
    dw label
    incbin "tiles.bin"
```

### Migrated Poppy Code

```pasm
.lorom
.header

.org $008000

main:
    sei
    clc
    xce
    rep #$30
    
    lda #$0000
    sta $2100
    
    jmp main

data:
    .db $01, $02, $03
    .dw label
    .incbin "tiles.bin"
```

## Getting Help

- Run `poppy --help` for command-line options
- Check the [syntax specification](syntax-spec.md) for full directive reference
- See the [SNES guide](snes-guide.md) for platform-specific information
- Use `poppy convert --from xkas` to automatically migrate code
