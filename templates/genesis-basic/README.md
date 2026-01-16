# Sega Genesis/Mega Drive Basic Template

A minimal Genesis ROM skeleton demonstrating basic hardware initialization and a main loop.

## Files

- **poppy.json** - Project configuration targeting Genesis
- **src/main.pasm** - Main source with vectors, header, VDP init, joypad

## Features

- Complete vector table with exception handlers
- Valid Genesis header
- TMSS (Trademark Security System) handling
- Z80 initialization and bus management
- VDP initialization and register setup
- PSG silence
- Controller port setup
- Memory clearing (VRAM, CRAM, VSRAM)
- Joypad reading

## Building

```bash
poppy build
```

## Output

Creates `game.bin` - a valid Genesis ROM.

## Customization

1. **Header**: Modify header section for game name, copyright
2. **Game Logic**: Add your code in `update_game`
3. **Graphics**: Load patterns and tilemaps after VDP init
4. **Sound**: Program FM chip ($a04000) or use Z80 driver

## Hardware Notes

- **M68000**: 16/32-bit CPU at 7.67 MHz
- **Screen**: 320×224 (NTSC), 320×240 (PAL)
- **VRAM**: 64KB for tiles, tilemaps, sprites
- **CRAM**: 64 colors (4 palettes × 16 colors)
- **Controllers**: 3-button or 6-button pads

## Memory Map

| Address | Size | Description |
|---------|------|-------------|
| $000000 | 4MB | Cartridge ROM |
| $a00000 | 8KB | Z80 RAM |
| $c00000 | - | VDP ports |
| $ff0000 | 64KB | Work RAM |

## Related

- [Genesis Reference](../../docs/resources.md)
- [ASM68K Migration](../../docs/migration-from-asm68k.md)
- [WLA-DX Migration](../../docs/migration-from-wla-dx.md)
