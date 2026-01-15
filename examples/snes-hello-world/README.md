# 🎮 SNES Hello World Example

A minimal SNES ROM that initializes the system and displays a pattern.

## Building

```bash
poppy main.pasm -o hello.sfc
```

## Running

Test in any SNES emulator:
- [bsnes](https://github.com/bsnes-emu/bsnes) - Accuracy-focused
- [Mesen-S](https://github.com/SourMesen/Mesen-S) - Excellent debugger
- [SNES9x](https://www.snes9x.com/)

## What It Does

1. Initializes the SNES hardware (CPU, PPU, registers)
2. Enters native 16-bit mode
3. Sets up minimal background
4. Enters an infinite loop

## ROM Details

- **Platform**: Super Nintendo / Super Famicom
- **Mapping**: LoROM (Mode $20)
- **ROM Size**: 256KB
- **ROM Speed**: SlowROM
- **Region**: North America (NTSC)

## File Structure

- `main.pasm` - Main source code
- `poppy.json` - Project configuration (optional)
- `hello.sfc` - Output ROM (after building)
