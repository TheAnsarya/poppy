# SPC700 Hello World Example

A minimal SNES audio driver demonstrating **SPC700** assembly programming for the Poppy Compiler.

## 🎯 About

The **SPC700** is the Sony audio co-processor in the Super Nintendo. It's a completely separate CPU from the main 65816, running at 1.024 MHz with its own 64KB of RAM. Communication with the main CPU happens through four I/O ports ($f4-$f7).

This example demonstrates:
- DSP register initialization
- BRR-encoded sample playback
- Timer-driven audio updates
- Basic voice control (key on/off)

## 📁 Files

| File | Description |
|------|-------------|
| `poppy.json` | Project configuration |
| `main.pasm` | SPC700 assembly source |
| `README.md` | This documentation |

## 🔧 Building

```bash
poppy build
```

This produces `hello.spc` - a standalone SPC audio file playable in SPC players.

## 🎮 SPC700 Architecture

### Registers

| Register | Size | Description |
|----------|------|-------------|
| A | 8-bit | Accumulator |
| X | 8-bit | Index register X |
| Y | 8-bit | Index register Y |
| SP | 8-bit | Stack pointer |
| PSW | 8-bit | Processor status word |
| PC | 16-bit | Program counter |

### PSW Flags

| Bit | Flag | Description |
|-----|------|-------------|
| 7 | N | Negative |
| 6 | V | Overflow |
| 5 | P | Direct page ($0100 when set) |
| 4 | B | Break |
| 3 | H | Half-carry |
| 2 | I | Interrupt disable |
| 1 | Z | Zero |
| 0 | C | Carry |

## 📋 Memory Map

| Address | Size | Description |
|---------|------|-------------|
| $0000-$00ef | 240 bytes | Direct page (zero page) |
| $00f0-$00ff | 16 bytes | I/O registers |
| $0100-$01ff | 256 bytes | Stack / Direct page 1 |
| $0200-$ffbf | ~64KB | Program/data RAM |
| $ffc0-$ffff | 64 bytes | IPL ROM (initial loader) |

## 🔊 I/O Registers ($00f0-$00ff)

| Address | Name | Description |
|---------|------|-------------|
| $f0 | TEST | Test register (don't touch) |
| $f1 | CONTROL | Timer/IPL control |
| $f2 | DSPADDR | DSP register address |
| $f3 | DSPDATA | DSP register data |
| $f4 | CPUIO0 | Communication port 0 |
| $f5 | CPUIO1 | Communication port 1 |
| $f6 | CPUIO2 | Communication port 2 |
| $f7 | CPUIO3 | Communication port 3 |
| $f8 | AUXIO4 | Auxiliary port 4 |
| $f9 | AUXIO5 | Auxiliary port 5 |
| $fa | T0TARGET | Timer 0 target (8 kHz) |
| $fb | T1TARGET | Timer 1 target (8 kHz) |
| $fc | T2TARGET | Timer 2 target (64 kHz) |
| $fd | T0OUT | Timer 0 counter (read clears) |
| $fe | T1OUT | Timer 1 counter (read clears) |
| $ff | T2OUT | Timer 2 counter (read clears) |

## 🎵 DSP Registers

The DSP (Digital Signal Processor) handles all audio generation. It's accessed indirectly via DSPADDR/DSPDATA.

### Per-Voice Registers (8 voices × $10)

| Offset | Name | Description |
|--------|------|-------------|
| +$00 | VOL_L | Left volume (-128 to +127) |
| +$01 | VOL_R | Right volume (-128 to +127) |
| +$02 | PITCH_L | Pitch low byte |
| +$03 | PITCH_H | Pitch high byte (only bits 0-5) |
| +$04 | SRCN | Sample source number |
| +$05 | ADSR1 | Attack/decay settings |
| +$06 | ADSR2 | Sustain/release settings |
| +$07 | GAIN | Gain control (if ADSR disabled) |
| +$08 | ENVX | Current envelope (read-only) |
| +$09 | OUTX | Current sample (read-only) |

### Global DSP Registers

| Address | Name | Description |
|---------|------|-------------|
| $0c | MVOL_L | Master volume left |
| $1c | MVOL_R | Master volume right |
| $2c | EVOL_L | Echo volume left |
| $3c | EVOL_R | Echo volume right |
| $4c | KON | Key on (start voices) |
| $5c | KOFF | Key off (stop voices) |
| $6c | FLG | Flags (reset/mute/echo/noise) |
| $7c | ENDX | Voice end flags (read-only) |
| $5d | DIR | Sample directory page |
| $6d | ESA | Echo buffer start |
| $7d | EDL | Echo delay |

## 🔈 BRR Sample Format

The SPC700's DSP uses **BRR (Bit Rate Reduction)** compression for samples:

- Each block is 9 bytes: 1 header + 8 bytes of sample data
- 8 bytes = 16 4-bit samples
- Compression ratio: 32:9 (about 28% of original)

### BRR Header Byte

| Bits | Description |
|------|-------------|
| 7-6 | Range (shift amount) |
| 5-4 | Filter (0-3) |
| 1 | Loop flag |
| 0 | End flag |

## 📖 SPC700 Unique Instructions

The SPC700 has several unique instructions not found in other 6502-family CPUs:

| Instruction | Description |
|-------------|-------------|
| `mul ya` | Multiply Y × A → YA |
| `div ya, x` | Divide YA ÷ X → A (rem in Y) |
| `daa` | Decimal adjust A (addition) |
| `das` | Decimal adjust A (subtraction) |
| `xcn a` | Exchange nibbles of A |
| `sleep` | Wait for interrupt |
| `stop` | Halt CPU |
| `set1 dp.b` | Set bit b in direct page |
| `clr1 dp.b` | Clear bit b in direct page |
| `tset1 abs` | Test and set bits |
| `tclr1 abs` | Test and clear bits |
| `cbne dp, rel` | Compare and branch if not equal |
| `dbnz dp, rel` | Decrement and branch if not zero |
| `dbnz y, rel` | Decrement Y and branch if not zero |

## 🔗 Resources

- [SPC700 Reference](https://wiki.superfamicom.org/spc700-reference)
- [DSP Manual](https://wiki.superfamicom.org/snes-spc700-reference)
- [BRR Format](https://wiki.superfamicom.org/bit-rate-reduction-(brr))
- [SPC File Format](https://wiki.superfamicom.org/spc-and-rsn-file-format)

## 📝 Notes

- The SPC file format preserves the full 64KB RAM state
- Most SPC players expect code at $0200
- The I/O ports at $f4-$f7 are bidirectional
- Timer outputs auto-clear when read
- The DSP runs at 32 kHz sample rate
