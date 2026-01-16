# WonderSwan Basic Template

A minimal WonderSwan ROM skeleton demonstrating V30MZ (x86) initialization.

## Files

- **poppy.json** - Project configuration targeting WonderSwan
- **src/main.pasm** - Main source with display init and input

## Features

- Complete I/O port definitions
- Display initialization
- Palette setup (monochrome)
- VRAM clearing
- VBlank interrupt handling
- Key input reading (Y and X button groups)

## Building

```bash
poppy build
```

## Output

Creates `game.ws` - a valid WonderSwan ROM.

## Customization

1. **Header**: Modify publisher ID, game ID in poppy.json
2. **Color**: Set `color: true` for WonderSwan Color
3. **Game Logic**: Add your code in `update_game`
4. **Graphics**: Load tiles to VRAM after clearing

## Hardware Notes

- **V30MZ**: NEC 80186-compatible at 3.072 MHz
- **Screen**: 224×144 (horizontal or vertical)
- **VRAM**: 16KB for tiles, tilemaps, sprites
- **Palette**: 16 shades mono / 4096 colors (WSC)
- **RAM**: 16KB internal + 64KB expansion

## Key Bits

### Y Keys (D-pad)
| Bit | Button |
|-----|--------|
| 0 | Y1 (Up) |
| 1 | Y2 (Right) |
| 2 | Y3 (Down) |
| 3 | Y4 (Left) |

### X Keys (Buttons)
| Bit | Button |
|-----|--------|
| 0 | X1 |
| 1 | X2 |
| 2 | X3 |
| 3 | X4 |

Also: A button, B button, Start (via bit 2/3 of separate nibble)

## Memory Map

| Address | Size | Description |
|---------|------|-------------|
| $0000 | 16KB | Internal RAM |
| $2000 | 16KB | VRAM |
| $FE00 | - | I/O ports |

## Related

- [WonderSwan Reference](../../docs/resources.md)
