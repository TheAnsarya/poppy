# Fairchild Channel F Development Guide

This guide documents the Channel F `.pasm` workflow in Poppy, including project structure, syntax, instruction patterns, and practical coding examples.

## Scope

- System target: `channelf`
- CPU: Fairchild F8
- Cartridge entry: `$0800`
- Include package: `includes/channelf/channelf.inc`

## Quick Start

Use a project file (`poppy.json`) and a main source file:

```json
{
	"project": {
		"name": "channelf-demo",
		"target": "channelf",
		"entry": "main.pasm",
		"output": "build/demo.bin"
	}
}
```

```asm
	.system:channelf
	.include "channelf/channelf.inc"
	.org CART_ROM_START

CartEntry:
	br .halt

.halt:
	br .halt
```

Build:

```bash
poppy --project .
```

## Memory Model

Channel F memory definitions are provided by `channelf.inc`.

- BIOS 1: `$0000-$03ff`
- BIOS 2: `$0400-$07ff`
- Cartridge ROM: `$0800-$17ff`
- System RAM: `$2800-$2fff`
- Video RAM: `$3000-$37ff`
- I/O ports: `$3800-$38ff`

## Source Structure

Recommended layout:

- `main.pasm`: entry and top-level flow
- `src/video.pasm`: VRAM routines
- `src/input.pasm`: controller/front panel reads
- `src/gameplay.pasm`: game state and logic
- `src/data/*.pasm`: constants/tables

Include modules from `main.pasm`:

```asm
	.include "src/video.pasm"
	.include "src/input.pasm"
	.include "src/gameplay.pasm"
```

## Core Syntax

Use lowercase mnemonics and `$`-prefixed hex.

- Labels: `MainLoop:`
- Local labels: `.loop:`, `.done:`
- Constants: `SCREEN_END = $3800`
- Includes: `.include "path/file.pasm"`

Note: Use `.include` in source. If older notes mention `inc`, treat that as shorthand documentation, not the directive form.

## F8 Instruction Families

Commonly used instruction groups for gameplay and rendering:

- Load/transfer: `li`, `lr`, `lm`, `st`
- Arithmetic/logical: `ai`, `ci`, `ni`, `oi`, `xi`, `as`, `asd`, `xs`, `ns`
- Branching: `bt`, `bf`, `br`, `jmp`, `pi`, `pop`
- Port I/O: `in`, `out`, `ins`, `outs`
- Register/scratchpad flow: `lisu`, `lisl`, `lis`, `isar`-indexed register access patterns

## Include and Constants Pattern

```asm
	.include "channelf/channelf.inc"

	.org CART_ROM_START

Start:
	li COLOR_BLUE
	lr 1, a
	br MainLoop
```

## Loop Pattern

```asm
MainLoop:
	; game logic
	br MainLoop
```

## VRAM Fill Pattern

```asm
	li $aa
	lr 1, a

	li $30
	lr 10, a
	li $00
	lr 11, a

FillLoop:
	lr a, 1
	st
	lr a, 10
	ci $38
	bz FillDone
	br FillLoop

FillDone:
	br FillDone
```

## Input Pattern

Use constants from `channelf.inc` and mask bits from controller/console ports.

```asm
	in CH_F_PORT1
	ni CTRL_RIGHT
	bz PlayerMovingRight
```

## Multi-File Project Pattern

```asm
; main.pasm
	.system:channelf
	.include "channelf/channelf.inc"
	.include "src/init.pasm"
	.include "src/game.pasm"
	.org CART_ROM_START

CartEntry:
	br GameLoop
```

## Recommended Workflow

1. Start from `examples/channelf-hello-world/`.
2. Keep hardware constants in `channelf.inc` usage, not duplicated literals.
3. Organize code into include files by subsystem.
4. Keep mnemonics lowercase and addresses explicit.
5. Test ROM behavior in Nexen and compare against expected screen/input behavior.

## Related Files

- Example: `examples/channelf-hello-world/main.pasm`
- Hardware include: `includes/channelf/channelf.inc`
- Template set: `templates/`

