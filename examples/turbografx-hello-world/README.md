# TurboGrafx-16 / PC Engine Hello World

A minimal TurboGrafx-16/PC Engine example demonstrating HuC6280 assembly.

## Platform Details

- **CPU**: HuC6280 (Modified 65C02 @ 7.16 MHz / 1.79 MHz)
- **RAM**: 8KB work RAM
- **VRAM**: 64KB video RAM
- **Resolution**: 256×224 to 512×224 pixels
- **Colors**: 512 colors, 16 palettes of 16 colors

## HuC6280 Special Instructions

The HuC6280 extends the 65C02 with:

| Instruction | Description |
|-------------|-------------|
| `tii` | Block transfer, increment both |
| `tdd` | Block transfer, decrement both |
| `tin` | Block transfer, increment source only |
| `tia` | Block transfer, alternate increment |
| `tai` | Block transfer, alternate source |
| `tam` | Transfer A to memory mapping register |
| `tma` | Transfer memory mapping register to A |
| `csl` | Clock select low (1.79 MHz) |
| `csh` | Clock select high (7.16 MHz) |
| `st0` | Store to VDC address port |
| `st1` | Store to VDC data low port |
| `st2` | Store to VDC data high port |

## Building

```bash
poppy build
```

## Memory Map

| Address | Description |
|---------|-------------|
| `$0000-$00FF` | I/O ports (VDC, VCE, PSG, Timer, Joypad) |
| `$1800-$1FFF` | CD-ROM interface (if present) |
| `$2000-$3FFF` | Work RAM (8KB) |
| `$E000-$FFFF` | ROM (can be banked) |

## Resources

- [PCE Dev Wiki](https://wiki.pcedev.net/)
- [Archaic Pixels Documentation](https://archaicpixels.com/Main_Page)
