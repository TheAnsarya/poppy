# 📦 ROM File Formats Reference

> Reference Document v0.1 - January 11, 2026

This document describes the ROM file formats that Poppy Compiler will support.

---

## 🎮 NES Formats

### iNES Header (16 bytes)

The standard NES ROM format with 16-byte header.

| Offset | Size | Description |
|--------|------|-------------|
| $00-$03 | 4 | Magic number: "NES" + $1a |
| $04 | 1 | PRG ROM size in 16KB units |
| $05 | 1 | CHR ROM size in 8KB units (0 = CHR RAM) |
| $06 | 1 | Flags 6 (mapper low, mirroring, battery, trainer) |
| $07 | 1 | Flags 7 (mapper high, VS/Playchoice, NES 2.0) |
| $08 | 1 | PRG RAM size in 8KB units (0 = 8KB for compatibility) |
| $09 | 1 | Flags 9 (TV system) |
| $0a | 1 | Flags 10 (TV system, PRG RAM presence - unofficial) |
| $0b-$0f | 5 | Padding (should be zero) |

#### Flags 6 Breakdown

```
7       0
---------
NNNN FTBM

N: Lower nybble of mapper number
F: Four-screen VRAM
T: Trainer present (512 bytes at $7000-$71ff)
B: Battery-backed PRG RAM
M: Mirroring (0=horizontal, 1=vertical)
```

#### Flags 7 Breakdown

```
7       0
---------
NNNN xxPV

N: Upper nybble of mapper number
P: Playchoice-10
V: VS Unisystem
xx: If both set, NES 2.0 format
```

### NES 2.0 Header

Extended header format with additional metadata.

| Offset | Size | Description |
|--------|------|-------------|
| $00-$07 | 8 | Same as iNES |
| $08 | 1 | Mapper high nybble / Submapper |
| $09 | 1 | PRG/CHR ROM size MSB |
| $0a | 1 | PRG RAM size (shift count) |
| $0b | 1 | CHR RAM size (shift count) |
| $0c | 1 | CPU/PPU timing mode |
| $0d | 1 | VS System info / Extended console type |
| $0e | 1 | Misc ROMs |
| $0f | 1 | Default expansion device |

### Example iNES Header

```asm
; iNES header for simple NROM game
.db "NES", $1a      ; Magic number
.db 2               ; 2 x 16KB PRG ROM = 32KB
.db 1               ; 1 x 8KB CHR ROM = 8KB
.db %00000001       ; Vertical mirroring, mapper 0
.db %00000000       ; Mapper 0 (NROM)
.db 0, 0, 0, 0, 0, 0, 0, 0  ; Padding
```

---

## 🌟 SNES Formats

### SNES ROM Header

The SNES header is located at different offsets depending on mapping mode.

| Mapping | Header Location | Mode Byte |
|---------|-----------------|-----------|
| LoROM | $007fc0 (or $00ffc0) | $20 |
| HiROM | $00ffc0 | $21 |
| ExLoROM | $407fc0 | $30 |
| ExHiROM | $40ffc0 | $35 |

### Internal Header (64 bytes at $xxffc0)

| Offset | Size | Description |
|--------|------|-------------|
| $00-$14 | 21 | Game title (ASCII, space-padded) |
| $15 | 1 | ROM makeup (mapping mode + speed) |
| $16 | 1 | ROM type (chipset) |
| $17 | 1 | ROM size (2^n KB) |
| $18 | 1 | SRAM size (2^n KB, 0 = no SRAM) |
| $19 | 1 | Destination code (region) |
| $1a | 1 | Fixed value ($33 for extended header) |
| $1b | 1 | Version number |
| $1c-$1d | 2 | Checksum complement |
| $1e-$1f | 2 | Checksum |
| $20-$3f | 32 | Interrupt vectors |

### ROM Makeup Byte ($ffd5)

```
7       0
---------
F--SSMMM

F: FastROM (0=SlowROM 2.68MHz, 1=FastROM 3.58MHz)
S: Speed (usually same as F)
M: Map mode:
   000 = LoROM
   001 = HiROM
   010 = LoROM + S-DD1
   011 = LoROM + SA-1
   101 = ExHiROM
```

### ROM Type Byte ($ffd6)

| Value | Type |
|-------|------|
| $00 | ROM only |
| $01 | ROM + RAM |
| $02 | ROM + RAM + Battery |
| $03 | ROM + DSP1 |
| $04 | ROM + DSP1 + RAM |
| $05 | ROM + DSP1 + RAM + Battery |
| $13 | ROM + SuperFX |
| $14 | ROM + SuperFX + RAM |
| $15 | ROM + SuperFX + RAM + Battery |
| $34 | ROM + SA-1 |
| $35 | ROM + SA-1 + RAM |
| $36 | ROM + SA-1 + RAM + Battery |

### Destination Codes

| Value | Region |
|-------|--------|
| $00 | Japan |
| $01 | USA |
| $02 | Europe (PAL) |
| $03 | Sweden |
| $04 | Finland |
| $05 | Denmark |
| $06 | France |
| $07 | Netherlands |
| $08 | Spain |
| $09 | Germany |
| $0a | Italy |
| $0b | China |
| $0c | Indonesia |
| $0d | Korea |
| $0e | Global |
| $0f | Canada |
| $10 | Brazil |
| $11 | Australia |

### Interrupt Vectors (Native Mode)

| Offset | Vector |
|--------|--------|
| $ffe4 | COP |
| $ffe6 | BRK |
| $ffe8 | ABORT |
| $ffea | NMI |
| $ffec | (unused) |
| $ffee | IRQ |

### Interrupt Vectors (Emulation Mode)

| Offset | Vector |
|--------|--------|
| $fff4 | COP |
| $fff6 | (unused) |
| $fff8 | ABORT |
| $fffa | NMI |
| $fffc | RESET |
| $fffe | IRQ/BRK |

### Example SNES Header (LoROM)

```asm
.org $00ffc0

; Internal header
.db "GAME TITLE          "  ; 21 bytes, space padded
.db $20                     ; LoROM, SlowROM
.db $00                     ; ROM only
.db $09                     ; 512KB (2^9 = 512)
.db $00                     ; No SRAM
.db $01                     ; USA
.db $33                     ; Extended header marker
.db $00                     ; Version 1.0

; Checksums (calculated by assembler)
.dw $ffff                   ; Checksum complement
.dw $0000                   ; Checksum

; Native mode vectors (unused in most games)
.dw $0000                   ; COP
.dw $0000                   ; BRK
.dw $0000                   ; ABORT
.dw NMI                     ; NMI
.dw $0000                   ; Unused
.dw $0000                   ; IRQ

; Emulation mode vectors
.dw $0000                   ; COP
.dw $0000                   ; Unused
.dw $0000                   ; ABORT
.dw NMI                     ; NMI
.dw Reset                   ; RESET
.dw $0000                   ; IRQ/BRK
```

### SMC Header (Optional)

Some SNES ROMs have a 512-byte header prepended (copier header).

```
Total ROM size % 1024 == 512 → Has SMC header
Total ROM size % 1024 == 0   → No header
```

---

## 🎮 Game Boy Formats

### Game Boy ROM Header ($0100-$014f)

| Offset | Size | Description |
|--------|------|-------------|
| $0100-$0103 | 4 | Entry point (usually nop; jp $0150) |
| $0104-$0133 | 48 | Nintendo logo (required for boot) |
| $0134-$0143 | 16 | Title (uppercase ASCII) |
| $013f-$0142 | 4 | Manufacturer code (newer games) |
| $0143 | 1 | CGB flag |
| $0144-$0145 | 2 | New licensee code |
| $0146 | 1 | SGB flag |
| $0147 | 1 | Cartridge type (mapper + features) |
| $0148 | 1 | ROM size |
| $0149 | 1 | RAM size |
| $014a | 1 | Destination code |
| $014b | 1 | Old licensee code ($33 = use new) |
| $014c | 1 | ROM version |
| $014d | 1 | Header checksum |
| $014e-$014f | 2 | Global checksum |

### CGB Flag ($0143)

| Value | Description |
|-------|-------------|
| $00 | DMG (original Game Boy) only |
| $80 | CGB enhanced (works on both) |
| $c0 | CGB only |

### Cartridge Type ($0147)

| Value | Type |
|-------|------|
| $00 | ROM only |
| $01 | MBC1 |
| $02 | MBC1 + RAM |
| $03 | MBC1 + RAM + Battery |
| $05 | MBC2 |
| $06 | MBC2 + Battery |
| $08 | ROM + RAM |
| $09 | ROM + RAM + Battery |
| $0f | MBC3 + Timer + Battery |
| $10 | MBC3 + Timer + RAM + Battery |
| $11 | MBC3 |
| $12 | MBC3 + RAM |
| $13 | MBC3 + RAM + Battery |
| $19 | MBC5 |
| $1a | MBC5 + RAM |
| $1b | MBC5 + RAM + Battery |
| $1c | MBC5 + Rumble |
| $1d | MBC5 + Rumble + RAM |
| $1e | MBC5 + Rumble + RAM + Battery |

### ROM Size ($0148)

| Value | Size | Banks |
|-------|------|-------|
| $00 | 32 KB | 2 (no banking) |
| $01 | 64 KB | 4 |
| $02 | 128 KB | 8 |
| $03 | 256 KB | 16 |
| $04 | 512 KB | 32 |
| $05 | 1 MB | 64 |
| $06 | 2 MB | 128 |
| $07 | 4 MB | 256 |
| $08 | 8 MB | 512 |

### RAM Size ($0149)

| Value | Size |
|-------|------|
| $00 | None |
| $01 | 2 KB (unused) |
| $02 | 8 KB |
| $03 | 32 KB (4 banks) |
| $04 | 128 KB (16 banks) |
| $05 | 64 KB (8 banks) |

### Nintendo Logo (Required)

```asm
; These exact bytes must appear at $0104-$0133
; or the Game Boy won't boot the game
.db $ce, $ed, $66, $66, $cc, $0d, $00, $0b
.db $03, $73, $00, $83, $00, $0c, $00, $0d
.db $00, $08, $11, $1f, $88, $89, $00, $0e
.db $dc, $cc, $6e, $e6, $dd, $dd, $d9, $99
.db $bb, $bb, $67, $63, $6e, $0e, $ec, $cc
.db $dd, $dc, $99, $9f, $bb, $b9, $33, $3e
```

### Header Checksum Calculation

```
x = 0
for i = $0134 to $014c:
    x = x - ROM[i] - 1
```

---

## 🔧 Patch Formats

### IPS Format

Simple patching format for small changes.

```
Header:  "PATCH" (5 bytes)
Records: [offset:3][size:2][data:size] ...
         If size == 0: [rle_size:2][rle_byte:1]
EOF:     "EOF" (3 bytes)
```

### BPS Format

More sophisticated format with checksums.

```
Header:     "BPS1" (4 bytes)
Source size: variable-length integer
Target size: variable-length integer
Metadata:   variable-length integer + data
Actions:    encoded patch data
Checksums:  source CRC32, target CRC32, patch CRC32
```

---

## 📋 Memory Maps

### NES Memory Map

| Range | Description |
|-------|-------------|
| $0000-$07ff | 2KB Internal RAM |
| $0800-$1fff | Mirrors of RAM |
| $2000-$2007 | PPU Registers |
| $2008-$3fff | Mirrors of PPU |
| $4000-$4017 | APU & I/O Registers |
| $4018-$401f | Disabled APU/IO |
| $4020-$ffff | Cartridge space |

### SNES LoROM Map

| Bank | Address | Description |
|------|---------|-------------|
| $00-$3f | $0000-$1fff | LowRAM (mirror) |
| $00-$3f | $2000-$5fff | Hardware registers |
| $00-$3f | $6000-$7fff | Expansion |
| $00-$3f | $8000-$ffff | ROM (lower 32KB) |
| $40-$6f | $0000-$ffff | ROM |
| $70-$7d | $0000-$7fff | SRAM |
| $7e-$7f | $0000-$ffff | WRAM (128KB) |

### SNES HiROM Map

| Bank | Address | Description |
|------|---------|-------------|
| $00-$3f | $0000-$1fff | LowRAM (mirror) |
| $00-$3f | $2000-$5fff | Hardware registers |
| $00-$3f | $6000-$7fff | Expansion |
| $00-$3f | $8000-$ffff | ROM (upper mirror) |
| $40-$7d | $0000-$ffff | ROM |
| $7e-$7f | $0000-$ffff | WRAM (128KB) |
| $c0-$ff | $0000-$ffff | ROM |

### Game Boy Memory Map

| Range | Description |
|-------|-------------|
| $0000-$3fff | ROM Bank 0 (fixed) |
| $4000-$7fff | ROM Bank N (switchable) |
| $8000-$9fff | VRAM (8KB) |
| $a000-$bfff | External RAM |
| $c000-$cfff | Work RAM Bank 0 |
| $d000-$dfff | Work RAM Bank 1-7 (CGB) |
| $e000-$fdff | Echo RAM (mirror of $c000-$ddff) |
| $fe00-$fe9f | OAM (sprite attributes) |
| $fea0-$feff | Unusable |
| $ff00-$ff7f | I/O Registers |
| $ff80-$fffe | High RAM |
| $ffff | Interrupt Enable |

---

## 📝 Notes

- All multi-byte values are little-endian unless noted
- Checksums should be calculated automatically by assembler
- Test output against known-good ROMs
- Some emulators are picky about header validity

---

