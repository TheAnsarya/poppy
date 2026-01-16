# NES Basic Template

A minimal NES ROM skeleton ready for your game code.

## Features

- ✅ Complete NES initialization
- ✅ VBlank-synchronized main loop
- ✅ Controller reading
- ✅ Sprite DMA
- ✅ Palette loading
- ✅ Hardware constants

## Building

```bash
poppy build
```

Output: `game.nes`

## Project Structure

```
nes-basic/
├── poppy.json          # Project configuration
├── README.md           # This file
└── src/
    ├── main.pasm       # Main entry point
    └── constants.pasm  # NES hardware constants
```

## Customization

### Adding Graphics

1. Create CHR data (8KB pattern table)
2. Add to `src/main.pasm`:
   ```asm
   .org $10000
   .incbin "graphics.chr"
   ```

### Adding Game Logic

Edit `init_game` and `update_game` in `src/main.pasm`:

```asm
init_game:
    ; Your initialization code
    rts

update_game:
    ; Your game logic (called every frame)
    rts
```

### Changing Mapper

Edit `poppy.json`:
```json
"header": {
    "mapper": 1,        // MMC1
    "prgRomSize": 8,    // 128KB PRG
    "chrRomSize": 16    // 128KB CHR
}
```

## Memory Map

| Range | Usage |
|-------|-------|
| $0000-$00ff | Zero page variables |
| $0100-$01ff | Stack |
| $0200-$02ff | OAM buffer |
| $0300-$07ff | General RAM |
| $c000-$ffff | PRG-ROM |

## Resources

- [NES Dev Wiki](https://www.nesdev.org/wiki/)
- [Poppy User Manual](../../docs/user-manual.md)
