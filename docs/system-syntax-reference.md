# System Syntax Reference

This guide documents PASM target selection and baseline command syntax for each supported system profile.

## Coverage Model

Poppy validates target-system wiring through integration smoke compilation for each profile using:

- `.target` profile selection in source
- Profile-specific opcode parsing/encoding where available
- Segment/address mapping via `.org`
- End-to-end semantic analysis + code generation

These smoke checks are designed to catch target-registry breakage quickly and are complementary to architecture-specific unit tests.

Additional byte-verified integration vectors are maintained for core assembler completeness on:

- NES (MOS6502)
- Atari 2600 (MOS6507)
- Game Boy (SM83)
- SNES (WDC65816)
- Master System (Z80)
- TurboGrafx-16 / PC Engine (HuC6280)
- Atari Lynx (MOS65SC02)
- WonderSwan (V30MZ)
- Genesis / Mega Drive (M68000)

These vectors assert exact emitted bytes for representative canonical instructions so cross-target compile correctness remains deterministic.

## Per-System PASM Reference

| System | CPU Model | PASM `.target` | CLI `--platform` | Baseline Syntax Notes |
|--------|-----------|----------------|------------------|-----------------------|
| NES | MOS 6502 | `nes` | `nes` | 6502 opcodes like `nop`, `lda`, `sta`; use `.org $8000` for PRG entry windows |
| Atari 2600 | MOS 6507 | `a26` | `a26` / `atari2600` | 6507-compatible 6502 subset; common start region `.org $f000` |
| Atari Lynx | MOS 65SC02 | `lynx` | `lynx` | 65SC02 model with PASM 65xx directives and standard label flow |
| SNES | WDC 65816 | `snes` | `snes` | 65816 mnemonics/directives (`rep`, `sep`, banked addressing) with SNES header directives |
| Game Boy | SM83 | `gb` | `gb` / `gbc` | SM83 opcodes like `nop`, `halt`; Game Boy header directives available |
| Genesis | Motorola 68000 | `genesis` | `genesis` | M68000 profile supports deterministic integration-verified operand-bearing forms including `moveq #imm,dn`, `move.l <src>,dn`, `movea.l <src>,an`, `jmp $address`, `jsr (an)`, `lea <ea>,an`, and `pea <ea>` |
| Master System | Z80 | `sms` | `sms` | Z80 profile supports baseline instructions like `nop`, `halt` |
| WonderSwan | NEC V30MZ | `ws` | `ws` / `wonderswan` | V30MZ profile supports baseline control ops like `nop`, `cli` |
| GBA | ARM7TDMI | `gba` | `gba` | ARM mode supports byte-verified core mnemonics: `mov`, `add`, `sub`, `cmp`, `b`, `bl`, `bx`, `swi`, `ldr`/`str`/`ldrb`/`strb`, `mul`/`mla` |
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

## Asset Inclusion and Conversion Directives

PASM supports directives for binary inclusion and asset conversion before assembly output:

- `.incbin "path"[, offset[, length]]`
	- Includes raw binary bytes from a file, with optional byte offset/length slicing.
- `.asset "path", "type"[, option1[, option2[, option3[, option4]]]]`
	- Converts an asset into emitted bytes using a converter type/options.
	- Example: `.asset "sprite.png", "chr", "gba8", 8, 1, 1`
- `.asset_manifest "path/to/assets.json"`
	- Loads a manifest describing multiple asset inputs and conversion rules.
	- Supports mixed sources (for example binary slices and JSON byte arrays) in one pre-assembly step.

These directives are validated in codegen tests, including cross-target `.asset_manifest` byte-consistency checks and PNG conversion coverage.

Genesis / Mega Drive asset pipeline examples:

```asm
.target genesis
.org $000200
.asset_manifest "assets/genesis-assets.json"

; Optional direct conversion/inclusion when needed
.asset "assets/tiles/title.png", "chr", "gba8", 8, 1, 1
moveq #$2a, d0
jmp $00000300
```

```bash
poppy --platform genesis src/main.pasm -o build/game.bin
```

Roundtrip fixture tests also validate editable JSON + PNG inputs emit deterministic binary blob prefixes across representative platform profiles, including Atari 2600 and Genesis.

## Genesis Hardware Programming Notes

For Sega Genesis projects, the 68000-target assembler flow is verified together with hardware-facing register programming patterns for these core subsystems:

- Main CPU: Motorola 68000
- Sound coprocessor: Z80 (typically uploaded/controlled by 68000-side startup code)
- Video: VDP
- FM audio: YM2612
- PSG audio: SN76489-compatible PSG

Common 68000-side register anchors used in startup/runtime code:

- `Z80_BUS_REQ = $a11100`
- `Z80_RESET = $a11200`
- `Z80_RAM = $a00000`
- `VDP_DATA = $c00000`
- `VDP_CTRL = $c00004`
- `PSG_PORT = $c00011`
- `YM2612_A0 = $a04000`
- `YM2612_D0 = $a04001`
- `YM2612_A1 = $a04002`
- `YM2612_D1 = $a04003`

Representative snippet:

```asm
.target genesis
.org $000200

	move.w  #$0100, $a11100   ; request Z80 bus
	move.b  #$80, $c00011     ; PSG write
	move.b  #$22, $a04000     ; YM2612 register (port 0)
	move.b  #$00, $a04001     ; YM2612 value
	move.l  #$40000003, $c00004 ; VDP control long write
```

Use this as a hardware map reference; final initialization ordering and wait-state behavior should follow platform docs linked in `docs/resources.md`.

## ARM7TDMI Coverage Notes

Current ARM mode instruction emission is verified end-to-end for:

- Data processing: `mov`, `add`, `sub`, `cmp` (register and encodable immediate forms)
- Control flow: `b`, `bl`, `bx`
- Supervisor call: `swi`
- Load/store (simple register-base forms): `ldr`, `str`, `ldrb`, `strb`
- Multiply: `mul`, `mla`
- Long multiply: `umull`, `smull`, `umlal`, `smlal`, `umulls`, `smulls`, `umlals`, `smlals`

Conditional-suffix variants are byte-verified for representative forms beyond multiply, including:

- Data processing: `moveq`, `addne`, `cmplt`
- Load/store: `ldreq`, `strne`
- Control flow and supervisor call: `beq`, `blne`, `bxne`, `swige`
- Long multiply set-flags forms with and without conditions: `umulls`, `smulls`, `umlals`, `smlals`, `umullseq`, `smlalsne`

Current operand-shape limits for this slice:

- Load/store accepts both canonical and flattened forms:
	- `mnemonic rd, rn[, #imm]`
	- `mnemonic rd, [rn]`
	- `mnemonic rd, [rn, #imm]`
	- `mnemonic rd, [rn, #-imm]`
	- `mnemonic rd, [rn, #imm]!`
	- `mnemonic rd, [rn], #imm`
	- `mnemonic rd, [rn], rm`
	- `mnemonic rd, [rn, rm]`
	- Shifted register-offset load/store forms with immediate shifts:
		- `mnemonic rd, [rn, rm, lsl #n]`
		- `mnemonic rd, [rn, rm, lsr #n]`
		- `mnemonic rd, [rn, rm, asr #n]`
		- `mnemonic rd, [rn, rm, ror #n]`
		- `mnemonic rd, [rn, rm, rrx]`
	- Register-specified shift amount forms:
		- `mnemonic rd, [rn, rm, lsl rs]`
		- `mnemonic rd, [rn, rm, lsr rs]`
		- `mnemonic rd, [rn, rm, asr rs]`
		- `mnemonic rd, [rn, rm, ror rs]`
	- Subtract register-offset forms:
		- `mnemonic rd, [rn, - rm]`
		- `mnemonic rd, [rn, -rm]`
		- `mnemonic rd, [rn], - rm, <shift> #n`
		- `mnemonic rd, [rn], -rm, <shift> #n`
		- `mnemonic rd, [rn], - rm, <shift> rs`
		- `mnemonic rd, [rn], -rm, <shift> rs`
	- Advanced combinations validated:
		- writeback + shifted register-offset: `mnemonic rd, [rn, rm, <shift> #n]!`
		- post-index + register-specified shift: `mnemonic rd, [rn], rm, <shift> rs`
		- post-index + `rrx`: `mnemonic rd, [rn], rm, rrx`
		- subtract variants across writeback/post-index combinations
- Multiply-long expects register form: `mnemonic rdlo, rdhi, rm, rs`

Tracked follow-up work:

- Broader ARM7TDMI instruction family coverage beyond current slices: issues #344 and #346

For platform-specific headers and extended directives, see:

- `docs/snes-guide.md`
- `docs/gameboy-guide.md`
- `docs/atari-lynx-guide.md`
- `docs/channelf-guide.md`
- `docs/syntax-spec.md`
