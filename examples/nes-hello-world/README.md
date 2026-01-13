# NES Hello World Example

A complete example NES project demonstrating Poppy Assembly features.

## Project Structure

```
nes-hello-world/
├── poppy.json          # Project configuration
├── main.pasm           # Main source file
├── include/
│   └── nes.inc         # NES hardware definitions
└── README.md           # This file
```

## Features Demonstrated

### 1. iNES Header Generation
```asm
.ines {"mapper": 0, "prg": 2, "chr": 1, "mirroring": "horizontal"}
```
Automatically generates a valid iNES header with the specified configuration.

### 2. Label Types

**Global Labels** - Accessible throughout the file:
```asm
Reset:
    sei
```

**Local Labels** - Scoped to the previous global label:
```asm
.clear_ram:
    sta $0000, x
```

**Anonymous Labels** - For short loops:
```asm
-:
    bit PPU_STATUS
    bpl -
```

### 3. Macros

**Definition:**
```asm
.macro wait_vblank
-:
    bit PPU_STATUS
    bpl -
.endmacro
```

**Invocation:**
```asm
%wait_vblank
```

**With Parameters:**
```asm
.macro ppu_addr, addr
    lda #>(addr)
    sta PPU_ADDR
    lda #<(addr)
    sta PPU_ADDR
.endmacro

%ppu_addr $21cb
```

### 4. Include Files
```asm
.include "include/nes.inc"
```

### 5. Data Directives
```asm
palette_data:
    .db $0f, $00, $10, $20

hello_text:
    .db "HELLO WORLD!", $00
```

### 6. Memory Organization
```asm
.org $00        ; Zero page
.org $8000      ; PRG-ROM
.org $fffa      ; Vectors
```

## Building

```bash
# From the example directory
poppy build poppy.json

# Or directly
poppy -o hello.nes main.pasm
```

## Output Files

- `hello.nes` - The compiled NES ROM
- `hello.lst` - Assembly listing with addresses
- `hello.sym` - Symbol table for debugging
- `hello.map` - Memory map

## Running

Load `hello.nes` in any NES emulator:
- [Mesen](https://www.mesen.ca/)
- [FCEUX](http://www.fceux.com/)
- [Nestopia](http://nestopia.sourceforge.net/)

## Notes

This example creates a minimal ROM that:
1. Initializes the NES hardware
2. Clears RAM
3. Loads a palette
4. Displays "HELLO WORLD" text
5. Runs an infinite loop

For actual graphics, you would need to create CHR data (pattern tables) with
the character tiles and include them with `.incbin`.

## License

Public domain - Use as a template for your own projects.
