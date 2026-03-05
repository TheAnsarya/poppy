# Building from a Peony Project

> Reassemble disassembled ROMs with automatic roundtrip verification

---

## Overview

The **Nexen → Peony → Poppy** pipeline enables a full roundtrip workflow:

1. **Nexen** (emulator) exports a game pack containing ROM, CDL, and Pansy metadata
2. **Peony** (disassembler) imports the pack and produces `.pasm` source files + `peony.json`
3. **Poppy** (assembler) builds the `.pasm` files back into a ROM and verifies byte-for-byte accuracy

```
┌─────────┐     export      ┌─────────┐     import      ┌─────────┐
│  Nexen  │ ──────────────► │  Peony  │ ──────────────► │  Poppy  │
│Emulator │  nexen.pack.zip │Disasm   │  peony.json     │Assembler│
└─────────┘                 └─────────┘  + .pasm files  └─────────┘
                                                              │
                                                              ▼
                                                        ┌─────────┐
                                                        │ Verify  │
                                                        │ROM match│
                                                        └─────────┘
```

---

## Quick Start

```bash
# 1. Peony disassembles a ROM (creates peony.json + .pasm files)
peony import --nexen-pack game.nexen.pack.zip --output ./my-project

# 2. Poppy assembles back to ROM with automatic verification
poppy my-project/main.pasm -o my-project/build/game.nes

# If peony.json exists, Poppy automatically:
#   - Generates a .pansy metadata file alongside the ROM
#   - Compares the assembled ROM against the original
#   - Reports PASS/FAIL with CRC32 checksums
```

---

## The `peony.json` File

When Peony disassembles a ROM, it creates a `peony.json` project file in the output directory. Poppy reads this file to enable roundtrip verification and metadata generation.

### Format Reference

```json
{
    "version": "1.0",
    "platform": "nes",
    "rom": {
        "path": "roms/game.nes",
        "crc32": "a1b2c3d4",
        "size": 262144
    },
    "metadata": {
        "cdl": "metadata/game.cdl",
        "pansy": "metadata/game.pansy"
    },
    "output": {
        "format": "poppy",
        "directory": "src",
        "splitBanks": true
    },
    "source": {
        "nexenPack": "game.nexen.pack.zip",
        "importDate": "2026-01-15T10:30:00Z"
    }
}
```

### Fields

| Section | Field | Required | Description |
|---------|-------|----------|-------------|
| *(root)* | `version` | Yes | Project format version (e.g., `"1.0"`) |
| *(root)* | `platform` | Yes | Target platform: `nes`, `snes`, `gb`, `gba`, `lynx`, `a26` |
| `rom` | `path` | Yes | Relative path to the original ROM file |
| `rom` | `crc32` | No | CRC32 hash (lowercase hex) for integrity checking |
| `rom` | `size` | No | ROM size in bytes |
| `metadata` | `cdl` | No | Relative path to Code/Data Log file |
| `metadata` | `pansy` | No | Relative path to Pansy metadata file |
| `output` | `format` | No | Output format (currently `"poppy"`) |
| `output` | `directory` | No | Directory containing disassembled `.pasm` files |
| `output` | `splitBanks` | No | Whether source is split into per-bank files |
| `source` | `nexenPack` | No | Original Nexen pack filename |
| `source` | `importDate` | No | ISO 8601 timestamp of import |

All paths are relative to the directory containing `peony.json`.

---

## Roundtrip Verification

When Poppy detects a `peony.json` in the project directory, it automatically runs roundtrip verification after a successful build:

1. Loads `peony.json` and resolves the original ROM path
2. Reads the assembled output bytes
3. Compares byte-for-byte against the original ROM
4. Reports results with CRC32 checksums

### Example Output

**Pass:**
```
Assembled main.pasm -> build/game.nes (262144 bytes) + game.pansy
Roundtrip: PASS — assembled ROM matches original (CRC32: a1b2c3d4)
```

**Fail:**
```
Assembled main.pasm -> build/game.nes (262144 bytes) + game.pansy
Roundtrip: FAIL — 3 byte(s) differ
  Offset $001a: expected $4c, got $00
  Offset $001b: expected $80, got $00
  Offset $001c: expected $00, got $ff
```

### CRC32 Validation

If `peony.json` includes a `rom.crc32` field, the verifier also checks that the original ROM file hasn't been modified since the disassembly was created. A CRC32 mismatch produces a verification error rather than a misleading byte comparison.

### Disabling Verification

```bash
# Skip roundtrip verification
poppy main.pasm --no-verify
```

Verification is automatically skipped when no `peony.json` exists in the project directory.

---

## Automatic Pansy Generation

Poppy automatically generates a Pansy metadata file (`.pansy`) alongside the assembled ROM. This file contains:

- **Code/Data Map** — Per-byte flags identifying code vs. data regions
- **Symbols** — Labels, constants, and their addresses
- **Comments** — Inline and block comments from the source
- **Cross-References** — Call, jump, branch, read, and write relationships

### Controlling Pansy Output

```bash
# Default: auto-generates .pansy alongside the ROM
poppy main.pasm -o game.nes
# Creates: game.nes + game.pansy

# Override output path
poppy main.pasm -o game.nes --pansy custom-output.pansy

# Disable Pansy generation
poppy main.pasm -o game.nes --no-pansy
```

---

## CLI Flags Reference

| Flag | Description |
|------|-------------|
| `--pansy <file>` | Override the Pansy output file path |
| `--no-pansy` | Disable automatic Pansy file generation |
| `--no-verify` | Disable automatic roundtrip verification |

---

## Project Directory Structure

A typical Peony project directory looks like:

```
my-project/
├── peony.json              # Project configuration (created by Peony)
├── roms/
│   └── game.nes            # Original ROM file
├── metadata/
│   ├── game.cdl            # Code/Data Log from Nexen
│   └── game.pansy          # Pansy metadata from Nexen
├── src/
│   ├── main.pasm           # Main assembly source
│   ├── bank00.pasm         # Bank-split source files
│   ├── bank01.pasm
│   └── ...
└── build/
    ├── game.nes            # Assembled ROM output
    └── game.pansy          # Pansy metadata from Poppy
```

---

## Troubleshooting

### "Roundtrip: FAIL" with byte mismatches

The assembled ROM differs from the original. Common causes:

- **Missing data regions** — Some bytes were disassembled as code instead of data
- **Unresolved symbols** — Labels or constants that couldn't be resolved
- **Bank boundaries** — Code split incorrectly at bank boundaries

Check the mismatch offsets against the original ROM to identify what's wrong. The CDL file from Nexen helps Peony distinguish code from data accurately.

### "Warning: Could not load peony.json"

The `peony.json` file exists but has invalid JSON or missing required fields. Run `peony.json` through a JSON validator and check that `version`, `platform`, and `rom.path` are present.

### "Roundtrip: skipped (no ROM path)"

The `peony.json` exists but doesn't specify `rom.path`. Verification needs the original ROM to compare against. Add the path to the ROM file in the `rom` section.

### "CRC32 mismatch" error

The original ROM file on disk has a different CRC32 than what `peony.json` recorded during import. This means the ROM was modified after disassembly. Re-import from Nexen or update the CRC32 in `peony.json`.

### No Pansy file generated

Check that `--no-pansy` wasn't passed. Pansy generation requires code to be assembled successfully. If the build produces errors, no Pansy file is created.

---

## Related Documentation

- [CDL/DIZ Workflow](cdl-diz-workflow.md) — CDL roundtrip workflow details
- [Project File Format](project-file-format.md) — Poppy's own `.poppy` project format
- [File Formats](file-formats.md) — ROM and output format reference
- [User Manual](user-manual.md) — Complete Poppy usage guide
