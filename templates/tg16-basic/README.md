# TurboGrafx-16 / PC Engine Basic Template

A minimal TG16/PCE ROM skeleton demonstrating HuC6280 initialization and a main loop.

## Files

- **poppy.json** - Project configuration targeting TG16
- **src/main.pasm** - Main source with VDC init, joypad, and interrupts

## Features

- Memory mapping setup (I/O, RAM, ROM)
- High-speed CPU mode (7.16 MHz)
- VDC initialization with timing registers
- Memory clearing (VRAM, palette)
- PSG silence
- VBlank interrupt handling
- Joypad reading (2-nibble protocol)

## Building

```bash
poppy build
```

## Output

Creates `game.pce` - a valid PC Engine ROM.

## Customization

1. **Game Logic**: Add your code in `update_game`
2. **Graphics**: Load tiles/sprites after clearing VRAM
3. **Display**: Modify VDC registers for different resolutions
4. **Banking**: Use TAM/TMA for bank switching

## Hardware Notes

- **HuC6280**: Enhanced 65C02 at 7.16 MHz
- **Screen**: 256×224 (up to 512×224)
- **VRAM**: 64KB for tiles, tilemaps, sprites
- **Palette**: 512 colors from 512 total
- **RAM**: 8KB (32KB with CD)

## Memory Mapping

| MPR | Address | Typical Use |
|-----|---------|-------------|
| 0 | $0000-$1FFF | I/O ($FF) |
| 1 | $2000-$3FFF | RAM ($F8) |
| 2-6 | $4000-$DFFF | ROM banks |
| 7 | $E000-$FFFF | ROM (vectors) |

## Joypad Bits

| Bit | Button |
|-----|--------|
| 0 | I |
| 1 | II |
| 2 | Select |
| 3 | Run |
| 4 | Up |
| 5 | Right |
| 6 | Down |
| 7 | Left |

## Related

- [TG16 Reference](../../docs/resources.md)
