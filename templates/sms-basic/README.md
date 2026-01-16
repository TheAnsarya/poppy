# Sega Master System Basic Template

A minimal SMS ROM skeleton demonstrating Z80 initialization and a main loop.

## Files

- **poppy.json** - Project configuration targeting SMS
- **src/main.pasm** - Main source with init, VDP setup, and joypad

## Features

- Valid SMS header with TMR SEGA string
- VDP initialization and register setup
- PSG silence
- Memory clearing (VRAM, CRAM)
- VBlank interrupt handling
- Joypad reading

## Building

```bash
poppy build
```

## Output

Creates `game.sms` - a valid Master System ROM.

## Customization

1. **Header**: Modify region/size byte for different configurations
2. **Game Logic**: Add your code in `update_game`
3. **Graphics**: Load patterns and tilemaps after VDP init
4. **Banking**: Use mapper registers for ROMs > 48KB

## Hardware Notes

- **Z80**: 8-bit CPU at 3.58 MHz
- **Screen**: 256×192, 32 colors from 64
- **VRAM**: 16KB for tiles and tilemaps
- **CRAM**: 32 colors (16 BG + 16 sprite)
- **RAM**: 8KB at $c000-$dfff

## VDP Modes

| Mode | Description |
|------|-------------|
| Mode 4 | Standard SMS mode (256×192) |
| Mode 2 | TMS9918A compatible |

## Joypad Bits (Port $dc)

| Bit | Button |
|-----|--------|
| 0 | Up |
| 1 | Down |
| 2 | Left |
| 3 | Right |
| 4 | Button 1 |
| 5 | Button 2 |

## Related

- [SMS Reference](../../docs/resources.md)
- [WLA-DX Migration](../../docs/migration-from-wla-dx.md)
