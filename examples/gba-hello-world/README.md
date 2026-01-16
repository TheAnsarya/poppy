# 🌸 Game Boy Advance Hello World

A simple Poppy compiler example for the Game Boy Advance.

## Description

This demo initializes the GBA in Mode 3 (240x160 bitmap) and fills the screen with a color gradient. It demonstrates:

- Proper GBA ROM header structure
- ARM/Thumb mode switching
- Display control register configuration
- Direct VRAM manipulation
- VBlank synchronization

## Building

```bash
poppy --project .
```

Or with verbose output:

```bash
poppy --project . -V
```

## Output

- `hello.gba` - GBA ROM image

## Running

Use a GBA emulator like:
- mGBA
- VBA-M
- NO$GBA

## Technical Notes

The GBA uses an ARM7TDMI processor which supports both ARM (32-bit) and Thumb (16-bit) instruction sets. This example uses:

- **ARM mode** for the entry point (required)
- **Thumb mode** available for space-efficient code

Mode 3 provides a simple 240x160 framebuffer with 15-bit color (5 bits per channel).

## Resources

- [GBATEK Documentation](https://problemkaputt.de/gbatek.htm)
- [Tonc GBA Programming](https://www.coranac.com/tonc/text/)
- [ARM7TDMI Reference](https://developer.arm.com/documentation/ddi0210/c/)
