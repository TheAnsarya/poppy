# V30MZ Instruction Encoding Plan

> Implementation plan for NEC V30MZ (WonderSwan) instruction encoding in Poppy.

## Overview

The V30MZ is an Intel 80186-compatible CPU used in the Bandai WonderSwan and WonderSwan Color. It uses segment:offset addressing with a 20-bit (1MB) address space. The instruction encoding is based on the Intel 8086/80186 ISA, which is significantly more complex than 6502-family encoding due to the ModR/M byte system.

**Parent Issue:** TheAnsarya/poppy#187
**Epic:** TheAnsarya/poppy#184

## Current State

- `InstructionSetV30MZ.cs` — Scaffolding exists with:
	- `V30AddressingMode` enum (14 modes)
	- `InstructionEncoding` record struct (Opcode[], ModRmReg, BaseSize)
	- `TryGetImpliedOpcode()` — Partial, needs completion
	- `TryGetConditionalJump()` — 32 conditional jump types
	- `TryGetLoopInstruction()` — LOOPNZ, LOOPZ, LOOP, JCXZ
	- `EncodeModRM()` — ModR/M byte calculation
	- `TryGetEncodingFromShared()` — Parser addressing mode mapper
	- Register and segment prefix dictionaries
- `WonderSwanRomBuilder.cs` — Fully implemented (10-byte trailer, ROM sizes, save types)
- `CodeGenerator.cs` — V30MZ case **falls back to 6502** (produces garbage)
- **No tests exist** for V30MZ encoding

## Architecture Details

### Registers

| Type | Registers |
|------|-----------|
| 16-bit general | AX, BX, CX, DX, SP, BP, SI, DI |
| 8-bit halves | AL, AH, BL, BH, CL, CH, DL, DH |
| Segment | CS, DS, ES, SS |
| Special | IP (instruction pointer), FLAGS |

### ModR/M Byte Encoding

The ModR/M byte is the core complexity of x86 encoding:

```
  7  6  5  4  3  2  1  0
 [mod ] [  reg  ] [  rm  ]
```

- `mod` (2 bits): addressing mode (00=indirect, 01=disp8, 10=disp16, 11=register)
- `reg` (3 bits): register operand or opcode extension (/0-/7)
- `rm` (3 bits): register/memory operand

### Memory Addressing Modes

| mod | rm  | Effective Address |
|-----|-----|-------------------|
| 00  | 000 | [BX+SI] |
| 00  | 001 | [BX+DI] |
| 00  | 010 | [BP+SI] |
| 00  | 011 | [BP+DI] |
| 00  | 100 | [SI] |
| 00  | 101 | [DI] |
| 00  | 110 | disp16 (direct address) |
| 00  | 111 | [BX] |
| 01  | xxx | [base + disp8] |
| 10  | xxx | [base + disp16] |
| 11  | xxx | register |

### Instruction Encoding Patterns

1. **Implied** (1 byte): `NOP` → `$90`
2. **Register+Immediate** (2-3 bytes): `MOV AL, $12` → `$b0 $12`
3. **ModR/M** (2+ bytes): `ADD AX, BX` → `$01 $d8` (opcode + ModR/M)
4. **ModR/M+Immediate**: `ADD [BX], $12` → `$80 $07 $12`
5. **Direct address**: `MOV AX, [$1234]` → `$a1 $34 $12`
6. **Short jump**: `JZ label` → `$74 rel8`
7. **Near jump**: `JMP label` → `$e9 rel16`
8. **Segment prefix**: `ES: MOV AX, [SI]` → `$26 $8b $04`

## Implementation Phases

### Phase 1: Opcode Tables (#203)

Complete the static opcode dictionaries in InstructionSetV30MZ:

- Implied opcodes (NOP, HLT, CLC, STC, CLI, STI, etc.)
- String operations (MOVSB/W, STOSB/W, LODSB/W, etc.)
- Flag operations (PUSHF, POPF, LAHF, SAHF)
- BCD operations (DAA, DAS, AAA, AAS, AAM, AAD)
- 80186 extensions (PUSHA, POPA, BOUND, ENTER, LEAVE)

### Phase 2: CodeGenerator Integration (#204)

Wire V30MZ encoding into CodeGenerator:

- Add V30MZ dispatch in `TryGetInstructionEncoding()`
- Map parser `AddressingMode` → `V30AddressingMode`
- Generate ModR/M bytes for memory operands
- Handle segment override prefix injection
- Disambiguate 8-bit vs 16-bit operations

### Phase 3: Control Flow (#206)

Implement jumps, calls, returns:

- Short jumps (rel8): `JZ`, `JNZ`, `JC`, etc.
- Near jumps (rel16): `JMP`, `CALL`
- Far jumps (seg:off): `JMP FAR`, `CALL FAR`
- Loop instructions: `LOOP`, `LOOPZ`, `LOOPNZ`, `JCXZ`
- Software interrupts: `INT n`
- Returns: `RET`, `RETF`, `IRET`

### Phase 4: Register/Immediate Operations (#207)

Implement the bulk of arithmetic and data movement:

- MOV (register, memory, immediate — many encoding variants)
- ADD, SUB, CMP, AND, OR, XOR, TEST
- INC, DEC (register short forms + ModR/M forms)
- MUL, DIV, IMUL, IDIV
- XCHG, LEA, LDS, LES
- Shift/rotate: SHL, SHR, SAR, ROL, ROR, RCL, RCR

### Phase 5: Tests (#208)

Create InstructionSetV30MZTests.cs:

- Implied opcode encoding
- ModR/M byte generation for all mod/rm combinations
- Segment prefix injection
- 8-bit vs 16-bit register disambiguation
- Conditional jump encoding
- Immediate operand encoding (byte vs word)

### Phase 6: Integration Test (#210)

Minimal WonderSwan ROM:

```asm
; minimal-ws.pasm
.target "ws"
.org $0000

start:
	cli
	mov ax, $0000
	mov ds, ax
	mov ss, ax
	mov sp, $2000
	sti

loop:
	hlt
	jmp loop

.vectors
	.dw start    ; reset vector at end of ROM
```

## Complexity Assessment

**Effort: Very High** — The x86 ModR/M encoding system is fundamentally different from 6502/65816/SM83. Key challenges:

1. **ModR/M byte generation** — Must handle 256 possible mod/reg/rm combinations
2. **Operand size ambiguity** — `MOV [BX], $12` could be byte or word; needs size hints
3. **Multiple encoding paths** — `MOV AX, $1234` has both short form (`$b8`) and ModR/M form
4. **Segment overrides** — Prefix bytes that change the default segment
5. **Parser integration** — The Poppy parser was designed for 6502-style syntax; x86 syntax (`MOV AX, [BX+SI+$10]`) needs careful mapping

## References

- Intel 8086/8088 User's Manual
- NEC V30MZ Technical Manual
- WSdev Wiki: https://wsdev.org
- x86 opcode reference: http://ref.x86asm.net/coder32.html (8086 subset)
