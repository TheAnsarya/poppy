# SPC700 Basic Template (SNES Audio)

A minimal SPC700 sound program skeleton for SNES audio development.

## Files

- **poppy.json** - Project configuration targeting SPC700
- **src/main.pasm** - Main source with DSP init and command handling

## Features

- Complete DSP register definitions
- Communication port setup (SNES ↔ SPC700)
- Timer-based main loop
- DSP initialization (master volume, mute voices)
- Command processing framework
- Sample directory structure

## Building

```bash
poppy build
```

## Output

Creates `sound.spc` - an SPC700 sound program.

## Customization

1. **Commands**: Add more command handlers in `process_commands`
2. **Sound Engine**: Implement in `update_sound`
3. **Samples**: Add BRR-encoded samples to sample directory
4. **Music**: Implement a tracker or sequence player

## Hardware Notes

- **SPC700**: Sony custom 8-bit CPU at ~2 MHz
- **DSP**: 8 voices, 16-bit stereo
- **Sample Format**: BRR (bit-rate reduction) compression
- **RAM**: 64KB shared with samples
- **Timers**: 3 timers (2 × 8kHz, 1 × 64kHz)

## DSP Voice Registers

Each voice (0-7) has registers at offset `$x0`:

| Offset | Register | Description |
|--------|----------|-------------|
| $00 | VOLL | Left volume |
| $01 | VOLR | Right volume |
| $02 | PITCHL | Pitch low |
| $03 | PITCHH | Pitch high |
| $04 | SRCN | Source number |
| $05 | ADSR1 | Attack/Decay |
| $06 | ADSR2 | Sustain/Release |
| $07 | GAIN | Direct gain |

## Communication Ports

| Port | Address | Description |
|------|---------|-------------|
| PORT0 | $F4 | Commands |
| PORT1 | $F5 | Data 1 |
| PORT2 | $F6 | Data 2 |
| PORT3 | $F7 | Status |

## BRR Sample Format

- 9 bytes per block (1 header + 8 nibbles)
- 4 samples per byte (16 samples per block)
- ~3.5:1 compression ratio

## Related

- [SNES Reference](../../docs/resources.md)
- [SNES Template](../snes-basic/README.md)
