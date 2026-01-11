## Description
Generate iNES 2.0 format headers for NES ROM output.

## iNES 2.0 Header Structure
```
Bytes 0-3: "NES" + $1A (magic number)
Byte 4: PRG ROM size (low byte)
Byte 5: CHR ROM size (low byte)
Byte 6: Flags 6 (mapper low, mirroring, battery, trainer)
Byte 7: Flags 7 (mapper mid, NES 2.0 identifier)
Byte 8: Mapper high + submapper
Byte 9: PRG/CHR ROM size (high nibbles)
Byte 10: PRG RAM size
Byte 11: CHR RAM size
Byte 12: CPU/PPU timing
Byte 13: VS System / Extended console type
Byte 14: Misc ROMs
Byte 15: Default expansion device
```

## Syntax
```asm
.ines_prg 2             ; 32KB PRG ROM
.ines_chr 1             ; 8KB CHR ROM
.ines_mapper 0          ; NROM
.ines_mirror vertical   ; or horizontal, four_screen
.ines_battery           ; Has battery-backed RAM
```

## Acceptance Criteria
- [ ] Generate valid iNES 2.0 header
- [ ] Extended mapper support (> 255)
- [ ] Submapper field support
- [ ] PRG/CHR RAM size fields
- [ ] Timing field (NTSC/PAL/Multi)
- [ ] VS System support
- [ ] Unit tests with header verification

## Implementation Notes
- Parse iNES directives during assembly
- Generate 16-byte header before PRG data
- Validate mapper/size combinations
- Default to iNES 1.0 compatible if not using extended features

## Related
Part of #13 (Output Formats Epic)
