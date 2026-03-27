# 🌸 Fairchild Channel F Hello World

A simple Poppy compiler example for the Fairchild Channel F.

## Description

This demo fills the screen with a solid blue color by writing directly to Video RAM ($3000-$37ff). It demonstrates:

- Basic F8 assembly for the Channel F
- Using the `channelf.inc` include file for hardware constants
- Direct VRAM writes for screen filling
- Cartridge entry point at $0800 (BIOS handoff)
- Scratchpad register usage for address tracking

## Building

```bash
poppy --project .
```

Or with verbose output:

```bash
poppy --project . -V
```

## Output

- `hello.bin` - Channel F cartridge ROM image

## Running

Load the output ROM in any Channel F emulator (e.g., Nexen, MAME, FreeChaF).

The Channel F BIOS ROMs are required for most emulators:

- SL31253 (BIOS 1)
- SL31254 (BIOS 2)

## Hardware Notes

- **CPU:** Fairchild F8 @ ~1.79 MHz
- **Video:** 128×64 pixels, 2 bits per pixel (4 colors)
- **RAM:** 64-byte scratchpad (CPU registers) + 2KB system RAM
- **VRAM:** 2KB at $3000-$37ff
- **Colors:** Green (bg), Yellow, Blue, Grey
