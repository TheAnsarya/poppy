# 🌸 Sega Genesis Hello World

A simple Poppy compiler example for the Sega Genesis / Mega Drive.

## Description

This demo initializes the Genesis VDP and clears the screen. It demonstrates:

- Proper ROM header structure
- TMSS (Trademark Security System) handshake
- VDP register initialization
- VRAM clearing
- Interrupt vector setup

## Building

```bash
poppy --project .
```

Or with verbose output:

```bash
poppy --project . -V
```

## Output

- `hello.bin` - Genesis ROM image

## Running

Use a Sega Genesis emulator like:
- BlastEm
- Gens
- Fusion
- Exodus

## Resources

- [Genesis Software Manual](https://segaretro.org/Sega_Mega_Drive/Technical_specifications)
- [VDP Documentation](https://wiki.megadrive.org/index.php?title=VDP)
- [68000 Reference](http://www.tigernt.com/onlineDoc/68000.pdf)
