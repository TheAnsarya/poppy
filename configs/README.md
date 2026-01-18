# Game-Specific Project Configurations

This directory contains pre-built project configurations for specific games, providing
a starting point for disassembly projects or ROM hacking.

## Available Configurations

### NES

| Game | Config File | Mapper | Notes |
|------|-------------|--------|-------|
| Dragon Warrior | `dragon-warrior-1.json` | MMC1 (1) | Complete disassembly reference |
| Dragon Warrior IV | `dragon-warrior-4.json` | MMC3 (4) | In progress |

### SNES

| Game | Config File | Mapper | Notes |
|------|-------------|--------|-------|
| Final Fantasy Mystic Quest | `ffmq.json` | LoROM | Complete disassembly |

## Configuration Schema

Each configuration file follows the standard Poppy project schema with additional
game-specific metadata:

```json
{
    "name": "game-name",
    "version": "1.0.0",
    "description": "Game description",
    "target": "nes|snes|gb|gba",
    "cpu": "6502|65816|z80|arm7tdmi",
    "output": { ... },
    "header": { ... },
    "memory": { ... },
    "sources": [ ... ],
    "include": [ ... ],
    "assets": { ... },
    "symbols": { ... },
    "reference": {
        "repo": "related-repo-name",
        "wiki": "wiki-url",
        "notes": "additional notes"
    }
}
```

## Usage

1. Copy the configuration to your project directory
2. Rename to `poppy.json`
3. Adjust paths as needed
4. Run `poppy build` to assemble

## Contributing

To add a new game configuration:
1. Create a new JSON file following the schema
2. Include accurate mapper and memory layout information
3. Add reference information for the source disassembly
4. Update this README with the new entry
