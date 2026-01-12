# 📁 Poppy Project File Format

> Specification for `.poppy` project files

**Version:** 1.0
**Format:** YAML

---

## 📋 Overview

A Poppy project file (`.poppy` or `.ppy`) defines everything needed to build a complete ROM:

- 📝 Source files
- 🎨 Asset files (graphics, audio, data)
- ⚙️ Build settings
- 🎮 Target configuration
- 📤 Output options

---

## 🏗️ Basic Structure

```yaml
# project.poppy - Minimal example

project: MyGame
target: nes

sources:
  - main.asm

output:
  file: build/game.nes
```

---

## 📖 Complete Reference

### Project Metadata

```yaml
# Project identification
project: DragonWarrior          # Project name (required)
version: 1.0.0                   # Semantic version
author: Your Name                # Author/team name
description: |                   # Multi-line description
  A complete disassembly and
  enhancement of Dragon Warrior.
license: MIT                     # License identifier
```

### Target Configuration

```yaml
# Target system (required)
target: nes                      # nes, snes, gb

# Architecture override (optional, auto-detected from target)
arch: 6502                       # 6502, 65816, sm83
```

### Source Files

```yaml
# Source file specification
sources:
  # Simple list of files
  - src/main.pasm
  - src/game.pasm
  - src/gfx.pasm

  # Or with glob patterns
  - src/**/*.pasm                # All .pasm in src/
  - include/*.inc                # All .inc in include/

# Source file ordering (when order matters)
source_order:
  - src/header.pasm              # Compiled first
  - src/**/*.pasm                # Then everything else
  - src/vectors.pasm             # Compiled last
```

### Include Paths

```yaml
# Directories to search for .include directives
include_paths:
  - include/
  - lib/
  - shared/macros/
```

### Asset Pipeline

```yaml
# Asset definitions
assets:
  # CHR data (graphics tiles)
  - path: assets/sprites.png
    type: chr
    output: build/sprites.chr
    options:
      tile_size: 8x8             # 8x8 or 8x16
      palette: 2bpp              # 2bpp (NES), 4bpp (SNES)

  # Binary data (include as-is)
  - path: assets/music.bin
    type: binary
    symbol: music_data           # Creates label in assembly

  # Tilemap conversion
  - path: assets/level1.tmx
    type: tilemap
    format: tiled                # Tiled editor format
    output: build/level1.bin
    options:
      compression: rle           # none, rle, lz

  # Palette data
  - path: assets/palette.pal
    type: palette
    symbol: main_palette

  # Raw include (no processing)
  - path: data/tables.bin
    type: raw
    address: $a000               # Load at specific address
```

### Asset Type Reference

| Type | Description | Input Formats | Options |
|------|-------------|---------------|---------|
| `chr` | Graphics tiles | PNG, BMP, indexed | tile_size, palette |
| `tilemap` | Level/screen data | TMX, CSV | compression, format |
| `palette` | Color palette | PAL, PNG, ASE | format |
| `binary` | Raw binary | Any | symbol, address |
| `raw` | Unprocessed | Any | address |
| `audio` | Sound/music | NSF, SPC, VGM | format, engine |

### Memory Map

```yaml
# Define memory segments
memory:
  # PRG ROM banks
  prg:
    - name: bank0
      start: $8000
      end: $bfff
      file: true                 # Include in output

    - name: bank1
      start: $c000
      end: $ffff
      file: true

  # CHR ROM banks
  chr:
    - name: chr0
      start: $0000
      end: $1fff
      file: true

  # RAM (not in output)
  ram:
    - name: zeropage
      start: $0000
      end: $00ff
      file: false

    - name: stack
      start: $0100
      end: $01ff
      file: false

    - name: wram
      start: $0200
      end: $07ff
      file: false
```

### Output Configuration

```yaml
# Output file settings
output:
  # Main ROM file
  file: build/game.nes

  # ROM format
  format: ines2                  # raw, ines, ines2, sfc

  # NES-specific header (iNES/NES 2.0)
  nes:
    mapper: 0                    # Mapper number
    prg_rom: 2                   # 16KB PRG banks
    chr_rom: 1                   # 8KB CHR banks
    mirroring: vertical          # horizontal, vertical, four_screen
    battery: false               # Battery-backed SRAM
    trainer: false               # 512-byte trainer

    # NES 2.0 extended fields
    submapper: 0
    prg_ram: 0                   # PRG RAM size (8KB units)
    prg_nvram: 0                 # PRG NVRAM size
    chr_ram: 0                   # CHR RAM size
    chr_nvram: 0                 # CHR NVRAM size
    timing: ntsc                 # ntsc, pal, multi, dendy

  # SNES-specific header
  snes:
    title: "GAME TITLE"          # 21 characters max
    rom_speed: fast              # slow, fast
    map_mode: lorom              # lorom, hirom, exhirom
    chipset: rom                 # rom, rom_ram, rom_ram_battery
    rom_size: auto               # auto or KB value
    ram_size: 0                  # SRAM in KB
    country: usa                 # japan, usa, europe
    developer_id: $33
    version: 0

# Additional output files
listing: build/game.lst          # Assembly listing
symbols: build/game.sym          # Symbol file
map: build/game.map              # Memory map
debug: build/game.dbg            # Debug info (Mesen format)
```

### Build Options

```yaml
# Compiler settings
build:
  # Optimization level
  optimize: 2                    # 0=none, 1=size, 2=speed

  # Warning settings
  warnings:
    level: all                   # none, default, all, pedantic
    as_errors: false             # Treat warnings as errors
    disabled:                    # Specific warnings to disable
      - W001                     # Unused label
      - W023                     # Implicit zero page

  # Code generation
  codegen:
    auto_zeropage: true          # Optimize to ZP when possible
    branch_optimize: true        # Replace JMP with branch when possible
    dead_code: warn              # none, warn, remove

  # Defines (like -D flag)
  defines:
    DEBUG: 1
    VERSION: $0100

  # Feature flags
  features:
    local_labels: true           # Enable @local labels
    anonymous_labels: true       # Enable +/- labels
    long_branch: true            # Auto long-branch for 65816
```

### Scripts and Hooks

```yaml
# Build hooks
scripts:
  # Run before build
  pre_build:
    - python tools/generate_tables.py
    - make -C tools/

  # Run after successful build
  post_build:
    - python tools/add_checksum.py build/game.nes
    - cp build/game.nes ../emulator/

  # Run after failed build
  on_error:
    - echo "Build failed!"

  # Run assets before assembly
  pre_assets:
    - python tools/export_graphics.py

  # Custom commands
  custom:
    clean:
      - rm -rf build/
    test:
      - python test/run_tests.py
```

---

## 📂 Example Projects

### Simple NES Game

```yaml
# simple-game.poppy

project: SimpleGame
version: 1.0.0
target: nes

sources:
  - src/main.pasm

assets:
  - path: gfx/tiles.png
    type: chr
    options:
      tile_size: 8x8

output:
  file: build/game.nes
  format: ines
  nes:
    mapper: 0
    prg_rom: 1
    chr_rom: 1
    mirroring: vertical
```

### Dragon Warrior Hack

```yaml
# dw1.poppy

project: DragonWarrior
version: 2.0.0
author: TheAnsarya
description: Dragon Warrior enhancement project
target: nes

include_paths:
  - include/
  - macros/

sources:
  - src/header.pasm
  - src/bank_00/*.pasm
  - src/bank_01/*.pasm
  - src/bank_02/*.pasm
  - src/bank_03/*.pasm
  - src/vectors.pasm

assets:
  # Graphics
  - path: gfx/tiles.png
    type: chr
    symbol: chr_tiles

  - path: gfx/sprites.png
    type: chr
    symbol: chr_sprites

  # Text tables
  - path: data/dialogue.txt
    type: text_table
    format: dw_text
    symbol: dialogue_data

  # Map data
  - path: maps/overworld.tmx
    type: tilemap
    compression: rle
    symbol: map_overworld

memory:
  prg:
    - name: prg0
      start: $8000
      end: $9fff
    - name: prg1
      start: $a000
      end: $bfff
    - name: prg2
      start: $c000
      end: $dfff
    - name: prg3
      start: $e000
      end: $ffff

  chr:
    - name: chr0
      start: $0000
      end: $1fff

output:
  file: build/dw1.nes
  format: ines2
  nes:
    mapper: 1
    prg_rom: 4
    chr_rom: 2
    mirroring: vertical
    battery: true

  listing: build/dw1.lst
  symbols: build/dw1.sym
  map: build/dw1.map

build:
  optimize: 2
  warnings:
    level: all
  defines:
    REGION: $00
    DEBUG: 0

scripts:
  pre_build:
    - python tools/extract_text.py
  post_build:
    - python tools/verify_checksum.py build/dw1.nes
```

### SNES Project

```yaml
# snes-game.poppy

project: SNESGame
version: 1.0.0
target: snes

sources:
  - src/header.asm
  - src/main.asm
  - src/graphics.asm
  - src/sound.asm

assets:
  - path: gfx/background.png
    type: chr
    options:
      palette: 4bpp
      tile_size: 8x8

  - path: gfx/sprites.png
    type: chr
    options:
      palette: 4bpp

output:
  file: build/game.sfc
  format: sfc
  snes:
    title: "MY SNES GAME"
    map_mode: lorom
    rom_speed: fast
    chipset: rom
    country: usa

build:
  defines:
    HIROM: 0
```

---

## 🖥️ Command-Line Usage

```bash
# Build project
poppy build project.poppy

# Build with overrides
poppy build project.poppy --define DEBUG=1

# Clean build artifacts
poppy clean project.poppy

# Run custom script
poppy run project.poppy test

# Validate project file
poppy check project.poppy

# Generate project from existing files
poppy init --target nes

# Watch for changes and rebuild
poppy watch project.poppy
```

---

## 📝 File Discovery

When no project file is specified, Poppy searches for:

1. `*.poppy` in current directory
2. `*.ppy` in current directory
3. `project.poppy` in parent directories
4. `poppy.yaml` (alternate name)

---

## 🔗 Related Documents

- [User Manual](user-manual.md)
- [Asset Pipeline](asset-pipeline.md) (coming soon)
- [Memory Maps](memory-maps.md) (coming soon)

