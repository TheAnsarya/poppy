# 🌸 WonderSwan Hello World

A simple Poppy compiler example for the WonderSwan.

## Description

This demo initializes the WonderSwan hardware and displays a colored background. It demonstrates:

- V30MZ (8086-compatible) assembly for WonderSwan
- I/O port-based hardware register access
- Basic display controller setup
- Interrupt handling structure
- VBlank synchronization

## Building

```bash
poppy --project .
```

Or with verbose output:

```bash
poppy --project . -V
```

## Output

- `hello.ws` - WonderSwan ROM image

## Running

Use a WonderSwan emulator like:
- Mednafen
- Oswan
- WonderWitch

## Technical Notes

The WonderSwan uses:
- **NEC V30MZ** CPU at 3.072MHz (Intel 8086/80186 compatible)
- **Segment:Offset** addressing (20-bit address space, 1MB)
- **224×144** resolution (WonderSwan) or 224×144 with color (WonderSwan Color)
- **I/O port mapped** hardware registers ($00-$FF)

### Memory Map
```
$0000-$3FFF  Internal RAM (16KB)
$4000-$BFFF  Cartridge SRAM (optional)
$C000-$FFFF  ROM Bank (mapped from cartridge)
```

### CPU Registers
- **AX, BX, CX, DX** - General purpose (16-bit, split into H/L bytes)
- **SI, DI** - Index registers
- **BP, SP** - Stack registers
- **CS, DS, ES, SS** - Segment registers

## Resources

- [WonderSwan Dev Wiki](https://wsdev.romhack.net/)
- [NEC V30MZ Documentation](https://wiki.ws-dev.net/)
- [WStech Documentation](https://www.devrs.com/wswan/)
