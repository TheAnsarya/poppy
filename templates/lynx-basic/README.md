# Atari Lynx Basic Template

A minimal Atari Lynx ROM skeleton demonstrating 65SC02 initialization.

## Files

- **poppy.json** - Project configuration targeting Lynx
- **src/main.pasm** - Main source with hardware init and input

## Features

- Mikey register definitions (audio, timers, I/O)
- Suzy register definitions (graphics, math)
- Display buffer setup
- Screen clearing
- Input reading
- Basic main loop

## Building

```bash
poppy build
```

## Output

Creates `game.lnx` - a valid Atari Lynx ROM.

## Customization

1. **Header**: Modify cart name and manufacturer in poppy.json
2. **Game Logic**: Add your code in `update_game`
3. **Graphics**: Use Suzy sprite system for rendering
4. **Audio**: Program Mikey's 4 audio channels

## Hardware Notes

- **65SC02**: Enhanced 6502 at 4 MHz
- **Mikey**: Audio, timers, UART, I/O
- **Suzy**: Graphics coprocessor, math, input
- **Screen**: 160×102, 4096 colors (16 simultaneous)
- **RAM**: 64KB

## Joystick Bits ($fcb0)

| Bit | Button |
|-----|--------|
| 0 | Right |
| 1 | Left |
| 2 | Down |
| 3 | Up |
| 4 | Option 1 |
| 5 | Option 2 |
| 6 | B (Inside) |
| 7 | A (Outside) |

## Memory Map

| Address | Size | Description |
|---------|------|-------------|
| $0000 | 256B | Zero page |
| $0100 | 256B | Stack |
| $0200 | ~64KB | RAM |
| $fc00 | 256B | Suzy |
| $fd00 | 256B | Mikey |
| $fe00 | - | ROM vectors |

## Related

- [Lynx Reference](../../docs/resources.md)
