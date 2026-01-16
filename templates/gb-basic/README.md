# Game Boy Basic Template

A minimal Game Boy ROM skeleton demonstrating basic hardware initialization and a main loop.

## Files

- **poppy.json** - Project configuration targeting Game Boy
- **src/main.pasm** - Main source with initialization, VBlank, and joypad reading

## Features

- Hardware register constants (LCD, joypad, audio)
- Complete GB header with Nintendo logo
- LCD initialization and VBlank waiting
- Memory clearing (WRAM, VRAM, OAM)
- Palette setup
- Joypad reading
- Interrupt handlers

## Building

```bash
poppy build
```

## Output

Creates `game.gb` - a valid Game Boy ROM.

## Customization

1. **Title**: Change the title string at `$0134` (max 11 characters)
2. **Game Logic**: Add your code in `update_game`
3. **Graphics**: Load tiles/maps after clearing VRAM
4. **Sprites**: Set up OAM DMA for sprite management

## Hardware Notes

- **SM83 CPU**: Modified Z80 at 4.19 MHz
- **Screen**: 160×144, 4 shades of gray
- **VRAM**: $8000-$9FFF (tiles and tilemaps)
- **OAM**: $FE00-$FE9F (40 sprites)
- **Joypad**: D-pad + A/B/Start/Select

## Related

- [Game Boy Reference](../../docs/resources.md)
- [SM83 CPU Guide](https://gbdev.io/pandocs/)
- [RGBDS Migration Guide](../../docs/migration-from-rgbds.md)
