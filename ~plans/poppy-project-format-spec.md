# Poppy Project File Format Specification
# .poppy Archive Format Design Document

**Version:** 1.0  
**Date:** January 15, 2026  
**Status:** Draft  
**GitHub Issue:** #94

## 📋 Overview

The `.poppy` file format is a self-contained project archive that packages all project files, assets, configuration, and metadata into a single distributable file. It uses ZIP compression for portability and is inspired by modern package managers like NPM, Cargo, and the GameInfo custom project format.

## 🎯 Goals

1. **Portability:** Single file contains everything needed to build the project
2. **Version Control Friendly:** Can be unpacked, edited, and repacked
3. **Reproducible Builds:** Captures exact build configuration
4. **Easy Sharing:** Share complete projects without explaining folder structure
5. **Template System:** Create reusable project templates
6. **Asset Management:** Include graphics, music, and other resources

## 📦 File Format

### Container Format
- **Extension:** `.poppy`
- **Type:** ZIP archive (standard ZIP format)
- **Compression:** DEFLATE (standard ZIP compression)
- **Encoding:** UTF-8 for all text files

### Directory Structure
```
my-game.poppy (ZIP archive)
├── poppy.json              # Project manifest (REQUIRED)
├── README.md               # Project documentation (recommended)
├── LICENSE                 # License file (recommended)
├── .gitignore              # Git ignore patterns (optional)
├── src/                    # Source code directory
│   ├── main.pasm          # Entry point
│   ├── sprites.pasm       # Sprite data
│   ├── levels.pasm        # Level data
│   └── ...
├── include/                # Include files (optional)
│   ├── constants.pasm
│   ├── macros.pasm
│   └── ...
├── assets/                 # Game assets (optional)
│   ├── graphics/          # Graphics files
│   │   ├── sprites.chr
│   │   └── tileset.chr
│   ├── music/             # Music files
│   │   └── theme.nsf
│   └── sounds/            # Sound effects
│       └── jump.wav
├── build/                  # Build outputs (optional, usually excluded)
│   └── game.nes
├── tests/                  # Test files (optional)
│   └── test_main.pasm
└── .poppy/                 # Metadata directory
    ├── version.txt        # Format version
    ├── checksums.txt      # File integrity checksums
    └── build-info.json    # Build information
```

## 📄 Manifest Schema (poppy.json)

### Required Fields
```json
{
  "$schema": "https://poppy-compiler.org/schemas/project-v1.json",
  "name": "my-game",
  "version": "1.0.0",
  "platform": "nes"
}
```

### Complete Example
```json
{
  "$schema": "https://poppy-compiler.org/schemas/project-v1.json",
  "name": "my-game",
  "version": "1.0.0",
  "description": "A retro platformer game",
  "author": "Developer Name",
  "license": "MIT",
  
  "platform": "nes",
  "entry": "src/main.pasm",
  "output": "build/game.nes",
  
  "compiler": {
    "version": "1.0.0",
    "target": "nes",
    "options": {
      "optimize": true,
      "debug": false,
      "warnings": "all"
    }
  },
  
  "build": {
    "includePaths": [
      "include",
      "vendor/libs"
    ],
    "defines": {
      "DEBUG": false,
      "VERSION": "1.0.0"
    },
    "scripts": {
      "build": "poppy build",
      "test": "poppy test",
      "clean": "poppy clean"
    }
  },
  
  "assets": {
    "graphics": "assets/graphics",
    "music": "assets/music",
    "sounds": "assets/sounds"
  },
  
  "dependencies": {
    "poppy-stdlib": "^1.0.0"
  },
  
  "metadata": {
    "tags": ["platformer", "nes", "retro"],
    "homepage": "https://github.com/user/my-game",
    "repository": "https://github.com/user/my-game.git",
    "created": "2026-01-15T00:00:00Z",
    "modified": "2026-01-15T12:00:00Z"
  }
}
```

### Field Descriptions

#### Core Fields
- **$schema** (string, optional): JSON schema URL for validation
- **name** (string, required): Project name (lowercase, hyphens allowed)
- **version** (string, required): Semantic version (x.y.z)
- **description** (string, optional): Short project description
- **author** (string, optional): Author name or organization
- **license** (string, optional): SPDX license identifier

#### Platform Fields
- **platform** (string, required): Target platform
  - Valid values: `nes`, `snes`, `gb`, `gbc`, `atari2600`, `lynx`, `genesis`, `sms`, `gba`, `wonderswan`, `tg16`, `spc700`
- **entry** (string, optional): Entry point file path (default: `src/main.pasm`)
- **output** (string, optional): Output file path (default: `build/{name}.{ext}`)

#### Compiler Configuration
- **compiler.version** (string, optional): Minimum Poppy compiler version
- **compiler.target** (string, required): Compilation target (matches platform)
- **compiler.options** (object, optional): Compiler flags

#### Build Configuration
- **build.includePaths** (array, optional): Additional include directories
- **build.defines** (object, optional): Preprocessor defines
- **build.scripts** (object, optional): Build scripts/commands

#### Asset Configuration
- **assets** (object, optional): Asset directory mappings
  - Keys: asset types (graphics, music, sounds, etc.)
  - Values: directory paths

#### Dependencies
- **dependencies** (object, optional): External dependencies
  - Keys: package names
  - Values: version ranges (semver)

#### Metadata
- **metadata** (object, optional): Additional metadata
  - **tags**: Project tags/keywords
  - **homepage**: Project homepage URL
  - **repository**: Git repository URL
  - **created**: ISO 8601 creation timestamp
  - **modified**: ISO 8601 last modified timestamp

## 🔧 Metadata Files (.poppy/)

### version.txt
```
1.0
```
Format version number. Current version is `1.0`.

### checksums.txt
```
SHA256:poppy.json:a1b2c3d4...
SHA256:src/main.pasm:e5f6g7h8...
SHA256:assets/graphics/sprites.chr:i9j0k1l2...
```
Format: `<algorithm>:<filepath>:<checksum>`

### build-info.json
```json
{
  "poppyVersion": "1.0.0",
  "buildDate": "2026-01-15T12:00:00Z",
  "platform": "nes",
  "commit": "a1b2c3d4",
  "builder": "Poppy CLI 1.0.0"
}
```

## 🚀 Operations

### Creating a .poppy Archive

#### CLI Command
```bash
poppy pack [directory] -o output.poppy
```

#### Options
- `-o, --output <file>`: Output file path (default: `{name}.poppy`)
- `--exclude <pattern>`: Exclude files matching pattern
- `--include-build`: Include build directory
- `--compress <level>`: Compression level (0-9, default: 6)

#### Process
1. Validate `poppy.json` exists and is valid
2. Calculate checksums for all files
3. Create `.poppy/` metadata directory
4. Create ZIP archive with all files
5. Optionally sign archive (future feature)

### Extracting a .poppy Archive

#### CLI Command
```bash
poppy unpack archive.poppy [directory]
```

#### Options
- `-d, --directory <dir>`: Extract to directory (default: current)
- `--overwrite`: Overwrite existing files
- `--validate`: Validate checksums after extraction

#### Process
1. Extract ZIP archive
2. Validate `poppy.json` schema
3. Verify checksums (if requested)
4. Restore directory structure
5. Report any issues

### Validating a .poppy Archive

#### CLI Command
```bash
poppy validate archive.poppy
```

#### Checks
- ZIP archive integrity
- `poppy.json` exists and valid schema
- All referenced files exist
- Checksum verification
- Platform compatibility
- Compiler version compatibility

## 🎨 Use Cases

### 1. Project Distribution
```bash
# Developer creates project
poppy pack my-game/ -o my-game-v1.0.0.poppy

# User downloads and extracts
poppy unpack my-game-v1.0.0.poppy
cd my-game
poppy build
```

### 2. Project Templates
```bash
# Create template
poppy pack nes-template/ -o templates/nes-platformer.poppy

# Use template
poppy new my-game --template nes-platformer.poppy
```

### 3. Version Control
```bash
# Unpack for editing
poppy unpack my-game.poppy
cd my-game
git init
git add .
git commit -m "Initial commit"

# Make changes...

# Repack for distribution
poppy pack . -o ../my-game-v1.1.0.poppy
```

### 4. Reproducible Builds
```bash
# Archive includes exact build configuration
poppy unpack my-game.poppy
cd my-game
poppy build  # Uses exact settings from poppy.json
```

## 📏 Constraints

### File Size Limits
- **Maximum archive size:** 2 GB (ZIP64 not supported initially)
- **Recommended size:** < 50 MB for easy sharing
- **Individual file limit:** 4 GB

### Naming Conventions
- **Project name:** Lowercase, alphanumeric, hyphens allowed
  - Valid: `my-game`, `nes-platformer`, `game2024`
  - Invalid: `My Game`, `game_name`, `GAME`
- **File extensions:** Standard extensions for each platform
  - NES: `.nes`
  - SNES: `.sfc`, `.smc`
  - Game Boy: `.gb`, `.gbc`
  - etc.

### Reserved Names
- `.poppy/`: Reserved for metadata
- `poppy.json`: Reserved for manifest
- `node_modules/`: Excluded by default
- `build/`: Excluded by default (unless --include-build)
- `.git/`: Excluded by default

## 🔒 Security Considerations

### Checksum Verification
- SHA-256 checksums for all files
- Prevents tampering and corruption
- Optional signature verification (future)

### Path Traversal Prevention
- All paths must be relative
- Paths cannot escape archive root
- Symbolic links are preserved but validated

### Safe Extraction
- Validate all paths before extraction
- Prevent overwriting system files
- Require --overwrite flag for existing files

## 🔄 Migration Path

### From Existing Projects
```bash
# Create poppy.json
poppy init

# Pack project
poppy pack .
```

### From Other Formats
```bash
# Import from CA65 project
poppy import ca65-project.cfg

# Import from ASAR project
poppy import asar-project/

# Pack imported project
poppy pack imported-project/
```

## 🚧 Future Enhancements

### Phase 2
- Digital signatures for archives
- Dependency resolution and downloading
- Remote template repository
- Archive diff/patch support

### Phase 3
- Encrypted archives
- Multi-platform projects in single archive
- Asset pipeline integration
- Cloud storage integration

## 📚 References

- **ZIP Format:** RFC 1951 (DEFLATE), APPNOTE.TXT (ZIP spec)
- **JSON Schema:** https://json-schema.org/
- **Semver:** https://semver.org/
- **NPM package.json:** https://docs.npmjs.com/cli/v8/configuring-npm/package-json
- **Cargo.toml:** https://doc.rust-lang.org/cargo/reference/manifest.html
- **GameInfo Format:** https://github.com/TheAnsarya/GameInfo

## 📝 Change Log

### Version 1.0 (2026-01-15)
- Initial specification
- Core manifest schema
- Basic pack/unpack operations
- Checksum verification
- Metadata structure

---

**Status:** Draft - Ready for implementation  
**Next Steps:** Implement manifest schema validation (#95)
