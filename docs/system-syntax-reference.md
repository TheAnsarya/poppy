# System Syntax Reference

This guide documents PASM target selection and baseline command syntax for each supported system profile.

## Coverage Model

Poppy validates target-system wiring through integration smoke compilation for each profile using:

- `.target` profile selection in source
- Profile-specific opcode parsing/encoding where available
- Segment/address mapping via `.org`
- End-to-end semantic analysis + code generation

These smoke checks are designed to catch target-registry breakage quickly and are complementary to architecture-specific unit tests.

## Per-System PASM Reference

| System | CPU Model | PASM `.target` | CLI `--platform` | Baseline Syntax Notes |
|--------|-----------|----------------|------------------|-----------------------|
| NES | MOS 6502 | `nes` | `nes` | 6502 opcodes like `nop`, `lda`, `sta`; use `.org $8000` for PRG entry windows |
| Atari 2600 | MOS 6507 | `a26` | `a26` / `atari2600` | 6507-compatible 6502 subset; common start region `.org $f000` |
| Atari Lynx | MOS 65SC02 | `lynx` | `lynx` | 65SC02 model with PASM 65xx directives and standard label flow |
| SNES | WDC 65816 | `snes` | `snes` | 65816 mnemonics/directives (`rep`, `sep`, banked addressing) with SNES header directives |
| Game Boy | SM83 | `gb` | `gb` / `gbc` | SM83 opcodes like `nop`, `halt`; Game Boy header directives available |
| Genesis | Motorola 68000 | `genesis` | `genesis` | M68000 profile supports core opcodes (`nop`) and system-specific evolution is ongoing |
| Master System | Z80 | `sms` | `sms` | Z80 profile supports baseline instructions like `nop`, `halt` |
| WonderSwan | NEC V30MZ | `ws` | `ws` / `wonderswan` | V30MZ profile supports baseline control ops like `nop`, `cli` |
| GBA | ARM7TDMI | `gba` | `gba` | ARM mode supports byte-verified core mnemonics: `mov`, `add`, `sub`, `cmp`, `b`, `bl`, `bx`, `swi` |
| SPC700 | Sony SPC700 | `spc700` | `spc700` | SPC700 profile supports baseline instructions such as `nop`, `clrc` |
| TurboGrafx-16 / PC Engine | HuC6280 | `tg16` | `tg16` / `pce` | HuC6280 profile supports baseline instructions such as `nop`, `inx` |
| Channel F | Fairchild F8 | `channelf` / `f8` | `channelf` / `f8` | Channel F scaffold profile supports baseline fixture syntax (`ldi`, `jmp`, labels) |

## Commands

Compile with target selection from source:

```bash
poppy game.pasm -o game.bin
```

Compile by explicit CLI platform override:

```bash
poppy --platform nes game.pasm -o game.nes
poppy --platform snes game.pasm -o game.sfc
poppy --platform gb game.pasm -o game.gb
poppy --platform gba game.pasm -o game.gba
poppy --platform genesis game.pasm -o game.bin
poppy --platform sms game.pasm -o game.sms
poppy --platform tg16 game.pasm -o game.pce
poppy --platform lynx game.pasm -o game.lnx
poppy --platform a26 game.pasm -o game.a26
poppy --platform ws game.pasm -o game.ws
poppy --platform channelf game.pasm -o game.bin
poppy --platform spc700 game.pasm -o game.spc
```

## Minimal Per-System Source Pattern

```asm
.target nes
.org $8000
start:
	nop
```

Swap `.target` and address window per platform, then use opcodes that are valid for that profile.

## ARM7TDMI Coverage Notes

Current ARM mode instruction emission is verified end-to-end for:

- Data processing: `mov`, `add`, `sub`, `cmp` (register and encodable immediate forms)
- Control flow: `b`, `bl`, `bx`
- Supervisor call: `swi`

Tracked follow-up work:

- Broader ARM7TDMI instruction family coverage (load/store, multiply variants, richer operand forms): issue #343

For platform-specific headers and extended directives, see:

- `docs/snes-guide.md`
- `docs/gameboy-guide.md`
- `docs/atari-lynx-guide.md`
- `docs/channelf-guide.md`
- `docs/syntax-spec.md`
