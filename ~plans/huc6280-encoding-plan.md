# HuC6280 Instruction Encoding Plan

> Implementation plan for Hudson HuC6280 (TurboGrafx-16/PC Engine) instruction encoding in Poppy.

## Overview

The HuC6280 is a modified 65C02 used in the NEC/Hudson TurboGrafx-16 (PC Engine). It is a **superset** of the WDC 65C02, adding block transfer operations, memory page mapping, VDC I/O, clock speed control, and bit manipulation instructions.

**Parent Issue:** TheAnsarya/poppy#186
**Epic:** TheAnsarya/poppy#184

## Current State

- `InstructionSetHuC6280.cs` — Substantial implementation (~600 lines):
	- `AddressingMode` enum (19 modes including BlockTransfer, ZeroPageBit, ZeroPageRelative)
	- Opcode dictionary with base 65C02 opcodes + HuC6280 extensions
	- Block transfer encoding helpers
	- Bit operation addressing
	- Memory page register constants
- `TurboGrafxRomBuilder.cs` — Fully implemented (ROM size validation, entry point, vectors)
- `CodeGenerator.cs` — HuC6280 case **falls back to 6502** (mostly works for base set but misses extensions)
- `InstructionSetHuC6280Tests.cs` — 350+ lines of tests already exist

## Architecture Details

### Registers (same as 65C02)

| Register | Size | Purpose |
|----------|------|---------|
| A | 8-bit | Accumulator |
| X | 8-bit | Index X |
| Y | 8-bit | Index Y |
| SP | 8-bit | Stack pointer |
| P | 8-bit | Status flags (NVTBDIZC) |
| PC | 16-bit | Program counter |
| MPR0-7 | 8-bit | Memory page registers (HuC6280 specific) |

### HuC6280-Specific Instructions

#### Block Transfers (7 bytes: opcode + src16 + dst16 + len16)

| Mnemonic | Opcode | Transfer Direction |
|----------|--------|--------------------|
| TII | $73 | src++ → dst++ (increment both) |
| TDD | $c3 | src-- → dst-- (decrement both) |
| TIN | $d3 | src++ → dst (increment src, fixed dst) |
| TIA | $e3 | src → dst++ (fixed src, increment dst — alternate) |
| TAI | $f3 | src++ → dst (increment src, alternate dst) |

#### Memory Page Registers

| Mnemonic | Opcode | Operation |
|----------|--------|-----------|
| TAM #n | $53 | Transfer A to MPR (bitmask n selects which MPR) |
| TMA #n | $43 | Transfer MPR to A (bitmask n selects which MPR) |

#### VDC I/O

| Mnemonic | Opcode | Operation |
|----------|--------|-----------|
| ST0 #n | $03 | Write to VDC port $0000 |
| ST1 #n | $13 | Write to VDC port $0002 |
| ST2 #n | $23 | Write to VDC port $0003 |

#### Clock Speed

| Mnemonic | Opcode | Speed |
|----------|--------|-------|
| CSL | $54 | Set CPU to 1.79 MHz |
| CSH | $d4 | Set CPU to 7.16 MHz |

#### Register Swaps

| Mnemonic | Opcode | Operation |
|----------|--------|-----------|
| SAX | $22 | Swap A ↔ X |
| SAY | $42 | Swap A ↔ Y |
| SXY | $02 | Swap X ↔ Y |

#### T-Flag and Test

- `SET` ($f4) — Set T flag for next instruction
- `TST #imm, zp` ($83) — Test: imm AND [zp], set flags
- `TST #imm, abs` ($93) — Test: imm AND [abs], set flags
- `TST #imm, zp,X` ($a3) — Test: imm AND [zp,X], set flags
- `TST #imm, abs,X` ($b3) — Test: imm AND [abs,X], set flags

#### Bit Operations (same as Rockwell 65C02)

- `RMB0-7` — Reset Memory Bit (zero page)
- `SMB0-7` — Set Memory Bit (zero page)
- `BBR0-7` — Branch on Bit Reset (zero page,relative)
- `BBS0-7` — Branch on Bit Set (zero page,relative)

## Implementation Phases

### Phase 1: CodeGenerator Wiring (#211)

Wire HuC6280 into `TryGetInstructionEncoding()`:

- The HuC6280 is a 65C02 superset — can reuse 65SC02/65C02 base encoding
- Add a HuC6280 case that first checks `InstructionSetHuC6280`
- Falls through to 65C02 base for standard opcodes
- Override only for HuC6280-specific instructions

### Phase 2: Block Transfers (#212)

Implement 7-byte block transfer encoding:

```
opcode src_lo src_hi dst_lo dst_hi len_lo len_hi
```

- Parser must handle 3-operand syntax: `tii $2000, $4000, $100`
- CodeGenerator must emit 7 bytes per instruction
- Verify all 5 variants: TII, TDD, TIN, TIA, TAI

### Phase 3: HuC6280-Specific Instructions (#213)

- TAM/TMA — Immediate operand is a bitmask
- ST0/ST1/ST2 — Immediate operand for VDC register
- CSL/CSH — Implied mode
- SAX/SAY/SXY — Implied mode
- SET — Implied mode
- TST — 4 variants with different addressing modes (immediate + address)

### Phase 4: Tests (#214)

Expand existing InstructionSetHuC6280Tests.cs:

- Verify block transfer encoding produces correct 7-byte sequences
- Verify TAM/TMA immediate encoding
- Verify ST0/ST1/ST2 immediate encoding
- Verify bit operations (RMB/SMB/BBR/BBS)
- Verify 65C02 base compatibility

### Phase 5: Integration Test (#216)

Minimal PCE ROM:

```asm
; minimal-pce.pasm
.target "pce"
.org $e000

reset:
	sei
	csh            ; 7.16 MHz mode
	cld
	ldx #$ff
	txs

	; Map page 0 to bank $f8 (I/O)
	lda #$ff
	tam #$01

	; Set VDC register 0
	st0 #$00

loop:
	jmp loop

.org $fff6
	.dw $0000       ; IRQ2/BRK vector
	.dw $0000       ; VDC vector
	.dw $0000       ; Timer vector
	.dw $0000       ; NMI vector
	.dw reset       ; Reset vector
```

## Complexity Assessment

**Effort: Medium** — HuC6280 is a 65C02 superset, which means:

1. **~90% of opcodes are shared with 65C02** — Can reuse existing encoding
2. **Block transfers are the main challenge** — 3-operand 7-byte instructions
3. **TST instruction** — Unusual dual-operand format (immediate + address)
4. **Parser support** — 3-operand syntax not currently handled for non-x86 targets

Key advantage: Most work is incremental on top of proven 65C02 encoding.

## References

- HuC6280 Programming Manual
- PCEdev Wiki: https://pcedev.wordpress.com
- Mednafen PCE source code (reference implementation)
- ArchiveCD: TG16/PCE technical documentation
