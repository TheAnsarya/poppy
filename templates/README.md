# 🎮 Poppy Project Templates

Ready-to-use project templates for all supported platforms. Each template includes a complete project structure with poppy.json, source files, and documentation.

## 📁 Available Templates

### By Platform

| Template | Platform | Status | Description |
|----------|----------|--------|-------------|
| [nes-basic](nes-basic/) | NES | ✅ | Minimal NES ROM skeleton |
| [snes-basic](snes-basic/) | SNES | ✅ | Minimal SNES ROM skeleton |
| [gb-basic](gb-basic/) | Game Boy | ✅ | Minimal GB ROM skeleton |
| [genesis-basic](genesis-basic/) | Genesis | ✅ | Minimal Genesis ROM skeleton |
| [gba-basic](gba-basic/) | GBA | ✅ | Minimal GBA ROM skeleton |
| [sms-basic](sms-basic/) | SMS | ✅ | Minimal SMS ROM skeleton |
| [tg16-basic](tg16-basic/) | TG16/PCE | ✅ | Minimal TG16 ROM skeleton |
| [a2600-basic](a2600-basic/) | Atari 2600 | ✅ | Minimal 2600 ROM skeleton |
| [lynx-basic](lynx-basic/) | Atari Lynx | ✅ | Minimal Lynx ROM skeleton |
| [ws-basic](ws-basic/) | WonderSwan | ✅ | Minimal WonderSwan ROM skeleton |
| [spc700-basic](spc700-basic/) | SPC700 | ✅ | Minimal SPC audio file |

### Future Templates (Planned)

| Template | Platform | Description |
|----------|----------|-------------|
| nes-platformer | NES | Side-scrolling platformer starter |
| snes-rpg | SNES | RPG game starter with menus |
| gb-puzzle | Game Boy | Puzzle game starter |

## 🚀 Using Templates

### Manual Copy

1. Copy the template folder to your project location
2. Rename the folder to your project name
3. Edit `poppy.json` with your project details
4. Start coding in `src/main.pasm`

### With `poppy init` (Coming Soon)

```bash
poppy init my-game --template nes-platformer
```

## 📋 Template Structure

Each template follows this structure:

```
template-name/
├── poppy.json          # Project configuration
├── README.md           # Template documentation
└── src/
    ├── main.pasm       # Main entry point
    ├── constants.pasm  # Hardware constants
    └── ... (additional files)
```

## 🛠️ Creating Your Own Templates

Templates are just regular Poppy projects. To create a template:

1. Start with a basic template
2. Add your code and assets
3. Document the template in README.md
4. Share with the community!

## 📝 Template Guidelines

Good templates should:

- ✅ Build without errors
- ✅ Include comprehensive comments
- ✅ Document all customization points
- ✅ Follow platform conventions
- ✅ Include a clear README
