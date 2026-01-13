# poppy
Smart compiler for retro gaming projects

## Quick Start

Create a new retro game project:

```bash
./new-project.sh my-game
cd my-game
```

This will copy all the template files from `project-base/` to a new directory with your project name, giving you a ready-to-edit project structure.

## Project Structure

The template project includes:
- `src/` - Source code files (assembly)
- `assets/` - Game assets (sprites, sounds, music)
- `build/` - Build output directory
- `Makefile` - Build configuration
- `README.md` - Project documentation

## Creating a New Project

Use the `new-project.sh` script to scaffold a new project:

```bash
# Create in current directory
./new-project.sh my-awesome-game

# Create in specific directory
./new-project.sh my-awesome-game ~/projects/
```

The script will:
1. Copy all files from `project-base/`
2. Set up the directory structure
3. Configure the project name in the build files

## Building Your Game

After creating your project:

```bash
cd your-project-name
make
```

Your compiled game will be in the `build/` directory.
