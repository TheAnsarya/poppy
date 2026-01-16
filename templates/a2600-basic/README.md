# Atari 2600 Basic Template

A minimal Atari 2600 ROM skeleton demonstrating the classic TV frame structure.

## Files

- **poppy.json** - Project configuration targeting Atari 2600
- **src/main.pasm** - Main source with frame loop and joystick

## Features

- Complete TIA register definitions
- RIOT chip definitions (timers, joystick)
- Proper NTSC frame timing (262 scanlines)
- VSYNC, VBLANK, visible, overscan sections
- Timer-based VBLANK waiting
- Joystick reading
- Simple player movement

## Building

```bash
poppy build
```

## Output

Creates `game.a26` - a 4KB Atari 2600 ROM.

## Customization

1. **Graphics**: Modify GRP0/GRP1 for player graphics
2. **Playfield**: Use PF0/PF1/PF2 for backgrounds
3. **Colors**: Change COLUP0/COLUPF/COLUBK
4. **Timing**: Adjust for PAL (312 scanlines)

## Hardware Notes

- **6507**: 6502 variant with 13-bit address bus
- **TIA**: Custom graphics chip (no framebuffer!)
- **Screen**: 160×192 (NTSC), 160×228 (PAL)
- **RAM**: 128 bytes ($80-$ff)
- **ROM**: 4KB standard (up to 64KB with banking)

## Frame Structure (NTSC)

| Section | Scanlines | Purpose |
|---------|-----------|---------|
| VSYNC | 3 | Vertical sync |
| VBLANK | 37 | Game logic |
| Visible | 192 | Draw graphics |
| Overscan | 30 | More game logic |
| **Total** | **262** | |

## Joystick Bits (SWCHA)

| Bit | Direction (P0) | Direction (P1) |
|-----|----------------|----------------|
| 0-3 | - | R, L, D, U |
| 4-7 | R, L, D, U | - |

## Related

- [2600 Reference](../../docs/resources.md)
- [DASM Migration](../../docs/migration-from-dasm.md)
