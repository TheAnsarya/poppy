# 🌸 Atari Lynx Hello World

A simple Poppy compiler example for the Atari Lynx.

## Description

This demo initializes the Lynx hardware (Mikey + Suzy chips). It demonstrates:

- Basic 65C02 assembly for the Lynx
- Mikey display controller setup
- Timer configuration
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

- `hello.lnx` - Lynx ROM image

## Running

Use an Atari Lynx emulator like:
- Mednafen
- Handy
- Felix

## Technical Notes

The Atari Lynx uses:
- **WDC 65C02** CPU at 4MHz (same as Apple IIc)
- **Mikey** - Audio, timers, UART, interrupts
- **Suzy** - Sprite engine, math coprocessor
- **160x102** resolution with 4096 colors

## Resources

- [Lynx Programming Tutorial](https://www.monlynx.de/lynx/lynxprg.html)
- [Atari Lynx Hardware Reference](https://atarilynxdeveloper.wordpress.com/)
