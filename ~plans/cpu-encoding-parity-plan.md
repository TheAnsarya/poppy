# Poppy — CPU Instruction Encoding Parity Plan

## Overview

Poppy has ROM builders for all 10 target platforms, but instruction encoding only works
for 6502-family CPUs (NES, SNES, GB, Atari 2600, Atari Lynx) and partially GBA.

Four architectures need instruction encoding implemented in `CodeGenerator.TryGetInstructionEncoding()`:

| Architecture | Platform | ROM Builder | Encoding Status |
|-------------|----------|-------------|-----------------|
| Z80 | SMS | ✅ `MasterSystemRomBuilder` | ❌ Falls to 6502 |
| HuC6280 | PCE/TG16 | ✅ `TurboGrafxRomBuilder` | ❌ Falls to 6502 |
| V30MZ | WonderSwan | ✅ `WonderSwanRomBuilder` | ❌ Falls to 6502 |
| M68000 | Genesis | ✅ `GenesisRomBuilder` | ❌ Falls to 6502 |

## Z80 Instruction Encoding (SMS)

### Scope

- 256 base opcodes
- 4 prefix tables: CB (bit ops), DD (IX-indexed), ED (extended), FD (IY-indexed)
- DD CB and FD CB double-prefix tables
- 8-bit and 16-bit registers (A, B, C, D, E, H, L, AF, BC, DE, HL, SP, IX, IY)
- Multiple addressing modes: register, immediate, direct, indirect, indexed, relative

### Key Opcodes

- Load/store: `ld`, `push`, `pop`
- Arithmetic: `add`, `adc`, `sub`, `sbc`, `inc`, `dec`, `cp`
- Logic: `and`, `or`, `xor`, `cpl`
- Bit: `bit`, `set`, `res`, `rl`, `rr`, `sla`, `sra`, `srl`
- Control: `jp`, `jr`, `call`, `ret`, `rst`, `djnz`
- I/O: `in`, `out`
- Block: `ldi`, `ldir`, `cpd`, `cpdr`

### Estimated Effort: High

## HuC6280 Instruction Encoding (PCE)

### Scope

- 65C02 superset — all standard 65C02 opcodes plus:
- Block transfer: `tai`, `tia`, `tii`, `tdd`, `tin` (3-byte source/dest/length)
- Timer: `set`, `cla`, `clx`, `cly`
- T-flag operations: Memory-to-accumulator via T flag
- I/O: `st0`, `st1`, `st2` (VDC/VCE writes)
- CSL/CSH: Clock speed change
- `tam`/`tma`: Memory mapping register access

### Estimated Effort: Medium

- Can extend existing 65C02 encoding with HuC6280-specific instructions

## V30MZ Instruction Encoding (WonderSwan)

### Scope

- Intel 80186 compatible (8086 + 80186 extensions)
- 256 base opcodes + 0F prefix table
- Segment registers: CS, DS, ES, SS
- General registers: AX, BX, CX, DX, SI, DI, BP, SP (+ 8-bit halves)
- Complex ModR/M byte encoding
- Segment override prefixes, REP prefixes

### Key Opcodes

- `mov`, `push`, `pop`, `xchg`
- `add`, `sub`, `cmp`, `inc`, `dec`, `mul`, `div`
- `and`, `or`, `xor`, `not`, `test`
- `jmp`, `call`, `ret`, `int`, conditional jumps
- `in`, `out` (I/O ports)
- String ops: `movsb`, `stosb`, `lodsb`, `cmpsb`, `scasb`

### Estimated Effort: Very High

- x86-family encoding is significantly more complex than 6502 or Z80

## M68000 Instruction Encoding (Genesis)

### Scope

- 16/32-bit CISC architecture
- 8 data registers (D0-D7), 8 address registers (A0-A7)
- Multiple operation sizes: byte (.b), word (.w), long (.l)
- 14 addressing modes
- Variable-length instructions (2-10 bytes)

### Key Opcodes

- `move`, `movem`, `lea`, `pea`
- `add`, `sub`, `muls`, `mulu`, `divs`, `divu`
- `and`, `or`, `eor`, `not`
- `bra`, `bsr`, `bcc` (conditional branches), `jmp`, `jsr`, `rts`
- `dbcc` (decrement and branch), `scc` (set on condition)
- `cmp`, `tst`, `btst`, `bset`, `bclr`, `bchg`

### Estimated Effort: Very High

- Complex variable-length encoding
- Effective address calculation for 14 addressing modes
- Condition code computation
