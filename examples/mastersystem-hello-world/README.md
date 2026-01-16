# Sega Master System Hello World

A minimal Sega Master System example demonstrating Z80 assembly.

## Platform Details

- **CPU**: Z80 @ 3.58 MHz
- **RAM**: 8KB work RAM
- **VRAM**: 16KB video RAM
- **Resolution**: 256×192 pixels (Mode 4)
- **Colors**: 64 colors total, 32 on screen

## Hardware

The Master System uses:
- **VDP**: Custom video chip (TMS9918 derivative)
- **PSG**: SN76489 sound chip (4 channels: 3 square + 1 noise)

## Building

```bash
poppy build
```

## Memory Map

| Address | Description |
|---------|-------------|
| `$0000-$03FF` | ROM (first 1KB, always mapped) |
| `$0400-$3FFF` | ROM Slot 0 (bankable) |
| `$4000-$7FFF` | ROM Slot 1 (bankable) |
| `$8000-$BFFF` | ROM Slot 2 (bankable) / Card RAM |
| `$C000-$DFFF` | Work RAM (8KB) |
| `$E000-$FFFF` | RAM Mirror |

## I/O Ports

| Port | Description |
|------|-------------|
| `$7E` | V counter (read) / PSG (write) |
| `$7F` | H counter (read) / PSG (write) |
| `$BE` | VDP data port |
| `$BF` | VDP control port |
| `$DC` | I/O port A (joypad 1) |
| `$DD` | I/O port B (joypad 2) |
| `$F0` | FM chip detect |
| `$F2` | FM chip register/data |

## VDP Registers

| Register | Description |
|----------|-------------|
| 0 | Mode control 1 |
| 1 | Mode control 2 |
| 2 | Name table base address |
| 3-4 | (Unused in Mode 4) |
| 5 | Sprite attribute table |
| 6 | Sprite pattern table |
| 7 | Border/background color |
| 8 | X scroll |
| 9 | Y scroll |
| 10 | Line counter |

## Resources

- [SMS Power! Development](https://www.smspower.org/Development/)
- [SMS VDP Documentation](https://www.smspower.org/Development/VDPRegisters)
