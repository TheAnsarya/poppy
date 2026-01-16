# SNES Basic Template

A minimal SNES ROM skeleton with 65816 native mode setup.

## Features

- ✅ Native 65816 mode initialization
- ✅ VRAM clearing
- ✅ Basic video mode setup
- ✅ NMI handler
- ✅ Joypad reading
- ✅ LoROM memory mapping

## Building

```bash
poppy build
```

Output: `game.sfc`

## Customization

### Memory Mapping

Edit `poppy.json` for HiROM:
```json
"header": {
    "mapping": "hirom"
}
```

### Adding Graphics

```asm
; Load tiles via DMA
lda #$01
sta DMAP0           ; Word increment, A->B
lda #$18
sta BBAD0           ; To VRAM
ldx #.loword(tiles)
stx A1T0L
lda #^tiles
sta A1B0
ldx #tiles_size
stx DAS0L
lda #$01
sta MDMAEN
```

## Resources

- [Anomie's SNES Docs](https://www.romhacking.net/documents/226/)
- [Poppy SNES Guide](../../docs/snes-guide.md)
