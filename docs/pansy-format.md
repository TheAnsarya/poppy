# 🌼 Pansy File Format Specification

> **Pansy** - Program ANalysis SYstem format for comprehensive assembly metadata exchange

## Overview

Pansy (`.pansy`) is a comprehensive binary metadata format designed for roundtrip data exchange between:
- **Poppy** (assembler) - Creates `.pansy` files during compilation
- **Peony** (disassembler) - Consumes `.pansy` files to improve disassembly accuracy

Pansy combines and extends the capabilities of:
- **CDL** (Code/Data Log) - Code vs data byte classification
- **DIZ** (DiztinGUIsh) - Rich symbol and label metadata
- **MLB/NL/SYM** - Debug symbol files
- **MAP** - Memory map information

## Design Goals

1. **Complete roundtrip support** - Preserve all assembly information for perfect reconstruction
2. **Multi-system** - Support NES, SNES, GB, GBA, Genesis, and more
3. **Extensible** - Version-aware with room for future fields
4. **Efficient** - Compressed binary format with fast random access
5. **Interoperable** - Import/export from CDL, DIZ, symbol files

## File Structure

```
┌─────────────────────────────────────────────────────────────────┐
│                     PANSY FILE STRUCTURE                         │
├─────────────────────────────────────────────────────────────────┤
│ Header (32 bytes)                                                │
│   Magic: "PANSY\0\0\0" (8 bytes)                                 │
│   Version: uint16 (major.minor)                                  │
│   Flags: uint16                                                  │
│   Platform: uint8 (NES=1, SNES=2, GB=3, GBA=4, etc.)            │
│   Reserved: uint8[3]                                             │
│   ROM Size: uint32                                               │
│   ROM CRC32: uint32                                              │
│   Section Count: uint32                                          │
│   Reserved: uint32                                               │
├─────────────────────────────────────────────────────────────────┤
│ Section Table (16 bytes per section)                             │
│   Section Type: uint32                                           │
│   Offset: uint32                                                 │
│   Compressed Size: uint32                                        │
│   Uncompressed Size: uint32                                      │
├─────────────────────────────────────────────────────────────────┤
│ Section Data (variable, zstd compressed)                         │
│   [CODE_DATA_MAP]   - Per-byte flags (like CDL)                 │
│   [SYMBOLS]         - Labels, constants, enums                   │
│   [COMMENTS]        - Per-address comments                       │
│   [MEMORY_REGIONS]  - Segment/bank definitions                   │
│   [DATA_TYPES]      - Data structure definitions                 │
│   [CROSS_REFS]      - Jump/call target references                │
│   [SOURCE_MAP]      - Original source file mapping               │
│   [METADATA]        - Project name, author, etc.                 │
└─────────────────────────────────────────────────────────────────┘
```

## Header Format

| Offset | Size | Field | Description |
|--------|------|-------|-------------|
| 0x00 | 8 | Magic | "PANSY\0\0\0" signature |
| 0x08 | 2 | Version | Format version (0x0100 = v1.0) |
| 0x0A | 2 | Flags | Feature flags |
| 0x0C | 1 | Platform | Target platform ID |
| 0x0D | 3 | Reserved | Must be zero |
| 0x10 | 4 | RomSize | Size of target ROM in bytes |
| 0x14 | 4 | RomCrc32 | CRC32 of target ROM |
| 0x18 | 4 | SectionCount | Number of sections |
| 0x1C | 4 | Reserved | Must be zero |

### Platform IDs

| ID | Platform | CPU |
|----|----------|-----|
| 0x01 | NES | MOS 6502 |
| 0x02 | SNES | WDC 65816 |
| 0x03 | Game Boy | Sharp SM83 |
| 0x04 | Game Boy Advance | ARM7TDMI |
| 0x05 | Sega Genesis | Motorola 68000 |
| 0x06 | Sega Master System | Zilog Z80 |
| 0x07 | TurboGrafx-16 | HuC6280 |
| 0x08 | Atari 2600 | MOS 6507 |
| 0x09 | Atari Lynx | WDC 65SC02 |
| 0x0A | WonderSwan | NEC V30MZ |
| 0x0B | Neo Geo | Motorola 68000 |
| 0x0C | SPC700 | Sony SPC700 |

### Flags

| Bit | Flag | Description |
|-----|------|-------------|
| 0 | COMPRESSED | Section data is zstd compressed |
| 1 | HAS_SOURCE_MAP | Contains original source mapping |
| 2 | HAS_CROSS_REFS | Contains cross-reference data |
| 3 | DETAILED_CDL | Has instruction-analyzed CDL data |
| 4-15 | Reserved | Must be zero |

## Section Types

### CODE_DATA_MAP (0x0001)

Per-byte classification flags, similar to CDL but extended.

**Byte flags:**
| Bit | Flag | Description |
|-----|------|-------------|
| 0 | CODE | Byte is code (opcode or operand) |
| 1 | DATA | Byte is data |
| 2 | JUMP_TARGET | Byte is a JMP destination |
| 3 | SUB_ENTRY | Byte is a JSR/CALL destination |
| 4 | OPCODE | Byte is an opcode (not operand) |
| 5 | DRAWN | Byte was rendered (graphics) |
| 6 | READ | Byte was read as data |
| 7 | INDIRECT | Accessed via indirect addressing |

### SYMBOLS (0x0002)

Label and constant definitions.

```
Symbol Entry:
  Address: uint32 (24-bit address + 8-bit bank)
  Type: uint8 (label=1, constant=2, enum=3, struct=4)
  Flags: uint8
  NameLength: uint16
  Name: char[NameLength]
  ValueLength: uint16 (for constants)
  Value: int64 (for constants)
```

**Symbol Types:**
| Value | Type | Description |
|-------|------|-------------|
| 1 | LABEL | Code or data label |
| 2 | CONSTANT | Named constant value |
| 3 | ENUM | Enumeration member |
| 4 | STRUCT | Structure definition |
| 5 | MACRO | Macro definition |
| 6 | LOCAL | Local label (within scope) |
| 7 | ANONYMOUS | Anonymous label (+/-) |

### COMMENTS (0x0003)

Per-address comments.

```
Comment Entry:
  Address: uint32
  Type: uint8 (inline=1, block=2, todo=3)
  Length: uint16
  Text: char[Length]
```

### MEMORY_REGIONS (0x0004)

Memory segment definitions.

```
Region Entry:
  StartAddress: uint32
  EndAddress: uint32
  Type: uint8 (code=1, data=2, bss=3, rodata=4)
  Bank: uint8
  Flags: uint16
  NameLength: uint16
  Name: char[NameLength]
```

### DATA_TYPES (0x0005)

Data structure definitions for tables, arrays, etc.

```
DataType Entry:
  Address: uint32
  Length: uint32
  ElementSize: uint16
  ElementCount: uint16
  Type: uint8 (byte=1, word=2, long=3, ptr=4, string=5)
  NameLength: uint16
  Name: char[NameLength]
```

### CROSS_REFS (0x0006)

Cross-reference data for jump/call targets.

```
CrossRef Entry:
  FromAddress: uint32
  ToAddress: uint32
  Type: uint8 (jsr=1, jmp=2, branch=3, read=4, write=5)
```

### SOURCE_MAP (0x0007)

Maps ROM addresses back to original source files.

```
SourceMap Entry:
  RomAddress: uint32
  FileIndex: uint16
  Line: uint16
  Column: uint16

SourceFile Entry:
  PathLength: uint16
  Path: char[PathLength]
```

### METADATA (0x0008)

Project metadata.

```
Metadata:
  ProjectNameLength: uint16
  ProjectName: char[Length]
  AuthorLength: uint16
  Author: char[Length]
  VersionLength: uint16
  Version: char[Length]
  CreatedTimestamp: int64
  ModifiedTimestamp: int64
```

## Compression

Section data is compressed using **zstd** (Zstandard) with compression level 3 by default.
Uncompressed data is stored if COMPRESSED flag is not set or if compression increases size.

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-19 | Initial specification |

## Comparison with Existing Formats

| Feature | CDL | DIZ | Pansy |
|---------|-----|-----|-------|
| Code/data flags | ✅ | ✅ | ✅ |
| Jump targets | Mesen only | ❌ | ✅ |
| Sub entry points | ✅ | ✅ | ✅ |
| Labels | ❌ | ✅ | ✅ |
| Comments | ❌ | ✅ | ✅ |
| Source mapping | ❌ | ❌ | ✅ |
| Cross-references | ❌ | ❌ | ✅ |
| Data types | ❌ | Limited | ✅ |
| Multi-system | Limited | SNES only | ✅ |
| Compression | ❌ | gzip | zstd |
| Binary format | ✅ | JSON | ✅ |

## File Extension

- Primary: `.pansy`
- Alternative: `.psy`

## Related Tools

- **Poppy** - `--pansy output.pansy` flag for generation
- **Peony** - `peony disasm --pansy input.pansy rom.nes` for consumption
- **GameInfo** - Import/export utilities

## Example Usage

### Poppy (Assembly)

```bash
# Generate ROM with Pansy metadata
poppy game.pasm --output game.nes --pansy game.pansy

# Full build with all outputs
poppy game.pasm \
	--output game.nes \
	--pansy game.pansy \
	--symbols game.sym \
	--listing game.lst
```

### Peony (Disassembly)

```bash
# Disassemble with Pansy metadata
peony disasm game.nes --pansy game.pansy --output game.pasm

# Use Pansy for improved accuracy
peony disasm game.nes \
	--pansy game.pansy \
	--labels game.sym \
	--output src/
```

## Implementation Notes

1. Sections can appear in any order
2. Unknown section types should be preserved but ignored
3. CRC32 uses IEEE polynomial (same as PNG/ZIP)
4. All multi-byte values are little-endian
5. Strings are UTF-8 encoded without null terminator
