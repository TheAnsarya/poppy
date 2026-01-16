# Game Boy Advance Basic Template

A minimal GBA ROM skeleton demonstrating ARM initialization and a main loop.

## Files

- **poppy.json** - Project configuration targeting GBA
- **src/main.pasm** - Main source with vectors, init, and IRQ handling

## Features

- Valid GBA header
- CPU mode and stack initialization
- Memory clearing (VRAM, palette, OAM)
- Display initialization
- VBlank interrupt setup with BIOS wait
- Key input reading
- IRQ handler

## Building

```bash
poppy build
```

## Output

Creates `game.gba` - a valid Game Boy Advance ROM.

## Customization

1. **Header**: Modify game title and code in poppy.json
2. **Display Mode**: Change DISPCNT for different video modes
3. **Game Logic**: Add your code in `update_game`
4. **Graphics**: Load tiles/sprites to VRAM after clearing

## Hardware Notes

- **ARM7TDMI**: 32-bit CPU at 16.78 MHz
- **Screen**: 240×160, 15-bit color
- **VRAM**: 96KB for tiles, tilemaps, bitmaps
- **Palette**: 512 colors (256 BG + 256 OBJ)
- **OAM**: 128 sprites

## Video Modes

| Mode | Type | Description |
|------|------|-------------|
| 0 | Tile | 4 regular BG layers |
| 1 | Tile | 2 regular + 1 affine BG |
| 2 | Tile | 2 affine BG layers |
| 3 | Bitmap | 240×160 @ 16bpp |
| 4 | Bitmap | 240×160 @ 8bpp, page flip |
| 5 | Bitmap | 160×128 @ 16bpp, page flip |

## Key Bits

| Bit | Button |
|-----|--------|
| 0 | A |
| 1 | B |
| 2 | Select |
| 3 | Start |
| 4 | Right |
| 5 | Left |
| 6 | Up |
| 7 | Down |
| 8 | R |
| 9 | L |

## Related

- [GBA Reference](../../docs/resources.md)
- [devkitARM Migration](../../docs/migration-from-devkitarm.md)
