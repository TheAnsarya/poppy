# 🎮 Game Boy Hello World Example

A minimal Game Boy ROM that displays "HELLO" on screen.

## Building

```bash
poppy main.pasm -o hello.gb
```

## Running

Test in any Game Boy emulator:
- [BGB](https://bgb.bircd.org/) - Recommended for debugging
- [SameBoy](https://sameboy.github.io/)
- [Emulicious](https://emulicious.net/)

## What It Does

1. Initializes the Game Boy hardware
2. Sets up a simple tileset
3. Displays "HELLO" text on the background layer
4. Enters an infinite loop

## ROM Details

- **Platform**: Game Boy (DMG) / Game Boy Color (CGB Compatible)
- **ROM Size**: 32KB
- **RAM Size**: None
- **MBC**: None (simple ROM)

## File Structure

- `main.pasm` - Main source code
- `poppy.json` - Project configuration (optional)
- `hello.gb` - Output ROM (after building)
