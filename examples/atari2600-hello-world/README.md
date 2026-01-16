# 🌸 Atari 2600 Hello World

A simple Poppy compiler example for the Atari 2600.

## Description

This demo displays a colorful gradient by changing the background color on each scanline. It demonstrates:

- Basic 6502 assembly for the 2600
- TIA register manipulation
- Proper frame timing with VSYNC/VBLANK/Overscan
- Timer-based frame synchronization

## Building

```bash
poppy --project .
```

Or with verbose output:

```bash
poppy --project . -V
```

## Output

- `hello.bin` - 4KB ROM image

## Running

Use an Atari 2600 emulator like:
- Stella
- z26
- Javatari

## Resources

- [Atari 2600 Programming Guide](https://alienbill.com/2600/101/)
- [TIA Hardware Manual](https://problemkaputt.de/2k6specs.htm)
