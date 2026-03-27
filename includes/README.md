# Poppy Standard Include Files

This directory contains standard include files for various target systems.

## Usage

Include these files in your assembly source using the `.include` directive:

```pasm
.include "atari2600/tia.pasm"
```

## Available Includes

### Atari 2600

- `atari2600/tia.pasm` - TIA and RIOT register definitions, common constants

### Atari Lynx

- `lynx/lynx.inc` - Suzy/Mikey register definitions, memory map, hardware constants

### Fairchild Channel F

- `channelf/channelf.inc` - F8 CPU port definitions, memory map, controller/button masks, VRAM constants

### NES (Coming Soon)

- `nes/ppu.pasm` - PPU register definitions
- `nes/apu.pasm` - APU register definitions

### SNES (Coming Soon)

- `snes/ppu.pasm` - PPU register definitions
- `snes/spc.pasm` - SPC700 register definitions

### Game Boy (Coming Soon)

- `gb/hardware.pasm` - Hardware register definitions
