# 🏗️ Poppy 2.0 - Architecture Specifications

**Document Version:** 1.0  
**Last Updated:** 2026-01-13  
**Status:** Draft

---

## 📐 CPU Architectures - Technical Details

### 1️⃣ Motorola 68000 (Sega Genesis/Mega Drive)

#### Overview

- **Word Size:** 16-bit data, 32-bit address (24-bit physical)
- **Registers:** 8 data (D0-D7), 8 address (A0-A7), PC, SR
- **Endianness:** Big-endian
- **Speed:** 7.67 MHz (NTSC), 7.60 MHz (PAL)

#### Instruction Format
```
┌────────────┬─────────┬──────────┬─────────┐
│ Opcode(16) │ Mode(6) │ Reg(3)   │ EA(...)  │
└────────────┴─────────┴──────────┴─────────┘
Variable length: 1-5 words (2-10 bytes)
```

#### Addressing Modes (14 modes)

1. Data Register Direct - `Dn`
2. Address Register Direct - `An`
3. Address Register Indirect - `(An)`
4. Address Register Indirect Postincrement - `(An)+`
5. Address Register Indirect Predecrement - `-(An)`
6. Address Register Indirect Displacement - `d(An)`
7. Address Register Indirect Indexed - `d(An,Xn)`
8. Absolute Short - `xxx.w`
9. Absolute Long - `xxx.l`
10. PC Relative Displacement - `d(PC)`
11. PC Relative Indexed - `d(PC,Xn)`
12. Immediate - `#xxx`
13. Status Register - `SR`
14. Condition Code Register - `CCR`

#### Key Instructions (56 unique mnemonics)

- **Data Movement:** MOVE, MOVEA, MOVEM, MOVEP, EXG, SWAP, LEA, PEA
- **Arithmetic:** ADD, SUB, MUL, DIV, NEG, CLR, CMP, TST, EXT
- **Logical:** AND, OR, EOR, NOT
- **Shifts:** ASL, ASR, LSL, LSR, ROL, ROR, ROXL, ROXR
- **Bit Manipulation:** BCHG, BCLR, BSET, BTST
- **Branches:** BCC, BCS, BEQ, BNE, BGE, BLT, BGT, BLE, BRA, BSR
- **Jumps:** JMP, JSR, RTS, RTR, RTE
- **Stack:** LINK, UNLK
- **Special:** NOP, TRAP, CHK, DBcc

#### Encoding Complexity
```csharp
// Example: MOVE.L D0,($1000).W
// Opcode: $23C0 (move long, dest = abs.short)
// Extension: $1000 (absolute address)
byte[] encoding = { 0x23, 0xC0, 0x10, 0x00 };
```

---

### 2️⃣ ARM7TDMI (Game Boy Advance)

#### Overview

- **Word Size:** 32-bit
- **Registers:** 16 general (R0-R15), CPSR, SPSR
- **Modes:** ARM (32-bit), Thumb (16-bit)
- **Speed:** 16.78 MHz
- **Endianness:** Little-endian (default)

#### ARM Mode Instructions (32-bit)
```
┌─────┬─────┬─┬─┬─┬─┬────┬────┬────┬────┬────┬────────┐
│ Cond│ Op  │I│S│ │ │ Rn │ Rd │Shift│Shmt│Rot │ Imm/Rm │
└─────┴─────┴─┴─┴─┴─┴────┴────┴─────┴────┴────┴────────┘
 31-28 27-24    20   19-16 15-12 11-8  7-4     3-0
```

#### Thumb Mode Instructions (16-bit)
```
┌────────┬────┬────┬────┐
│ Opcode │ Rd │ Rs │ Rn │
└────────┴────┴────┴────┘
  15-8    7-5  4-3  2-0
```

#### Condition Codes (ARM)

- EQ, NE, CS/HS, CC/LO, MI, PL, VS, VC, HI, LS, GE, LT, GT, LE, AL

#### Key Instructions

- **Data Processing:** MOV, ADD, SUB, MUL, AND, ORR, EOR, BIC
- **Memory:** LDR, STR, LDM, STM (with addressing modes)
- **Branch:** B, BL, BX (mode switching)
- **Special:** SWI, MRS, MSR

#### Thumb-ARM Interworking
```asm
.arm          ; ARM mode
  ldr r0, =data
  ldr r1, =thumb_func
  bx r1       ; Switch to Thumb

.thumb        ; Thumb mode
thumb_func:
  mov r2, #5
  bx lr       ; Return (may switch back)
```

---

### 3️⃣ Zilog Z80 (Sega Master System)

#### Overview

- **Word Size:** 8-bit
- **Registers:** A, B, C, D, E, H, L, F (flags), IX, IY, SP, PC
- **Alternate Set:** A', B', C', D', E', H', L', F'
- **Speed:** 3.58 MHz
- **Endianness:** Little-endian

#### Instruction Format
```
Variable length: 1-4 bytes
┌────────┬───────────┬──────────┬──────────┐
│ Prefix │  Opcode   │  Data1   │  Data2   │
└────────┴───────────┴──────────┴──────────┘
Optional   Required    Optional   Optional
```

#### Prefixes

- `$CB` - Bit operations
- `$DD` - IX register operations
- `$ED` - Extended instructions
- `$FD` - IY register operations

#### Addressing Modes

1. Register - `A, B, C, D, E, H, L`
2. Immediate - `n` or `nn`
3. Indirect - `(HL), (BC), (DE)`
4. Indexed - `(IX+d), (IY+d)`
5. Direct - `(nn)`
6. I/O - `(C)` or `(n)`

#### Key Instructions

- **8-bit Load:** LD r,r' / LD r,n / LD r,(HL)
- **16-bit Load:** LD rr,nn / LD rr,(nn) / PUSH/POP
- **Arithmetic:** ADD, ADC, SUB, SBC, INC, DEC, CP
- **Logical:** AND, OR, XOR, BIT, SET, RES
- **Rotate/Shift:** RLCA, RRCA, RLA, RRA, SLA, SRA
- **Jump:** JP, JR, CALL, RET, RST
- **I/O:** IN, OUT, INI, OUTI, INIR, OTIR

#### I/O Port Access
```asm
; Read from port $BF (VDP data)
in a, ($bf)

; Write to port $BE (VDP control)
ld a, $80
out ($be), a
```

---

### 4️⃣ HuC6280 (TurboGrafx-16)

#### Overview

- **Base:** WDC 65C02 (enhanced 6502)
- **Word Size:** 8-bit
- **Registers:** A, X, Y, S, P, PC
- **Speed:** 1.79 MHz (slow), 7.16 MHz (fast)
- **Extensions:** Block transfer, I/O, MMU

#### New Instructions (vs 6502)

- **Block Transfer:** TAI, TIA, TII, TDD, TIN
- **Bit Operations:** TST, TSB, TRB
- **Stack:** PHX, PHY, PLX, PLY
- **Set Speed:** CSL, CSH (change clock speed)
- **Memory Map:** TAM, TMA (MMU control)
- **Special:** ST0, ST1, ST2 (VDC access)

#### Block Transfer Example
```asm
; TII - Transfer Increment Increment
; Copy $2000 bytes from $4000 to $6000
tii $4000, $6000, $2000
```

#### MMU Registers

- 8 mapping registers (MPR0-MPR7)
- Each maps 8KB window to physical memory

```asm
lda #$80      ; Bank $80
tam #$02      ; Map to MPR2 ($4000-$5fff)
```

---

### 5️⃣ NEC V30MZ (WonderSwan)

#### Overview

- **Base:** Intel 8086 compatible
- **Word Size:** 16-bit
- **Registers:** AX, BX, CX, DX, SI, DI, BP, SP, CS, DS, SS, ES
- **Speed:** 3.072 MHz
- **Endianness:** Little-endian

#### Segment:Offset Addressing
```
Physical Address = (Segment << 4) + Offset
```

#### Instruction Format (8086-style)
```
┌──────┬──────┬────────┬────────┬────────┐
│ Prfx │ Opc  │ ModR/M │  SIB   │  Disp  │
└──────┴──────┴────────┴────────┴────────┘
  Opt   1-2B    Opt      Opt      Opt
```

#### Key Instructions

- **Data Movement:** MOV, PUSH, POP, XCHG, IN, OUT
- **Arithmetic:** ADD, SUB, MUL, DIV, INC, DEC
- **Logical:** AND, OR, XOR, NOT, TEST
- **String:** MOVS, CMPS, SCAS, LODS, STOS
- **Control:** JMP, CALL, RET, INT, IRET
- **Loops:** LOOP, LOOPE, LOOPNE

---

### 6️⃣ WDC 65C02 (Atari Lynx)

#### Overview

- **Base:** MOS 6502 enhanced
- **Word Size:** 8-bit
- **Registers:** A, X, Y, S, P, PC
- **Speed:** 4 MHz
- **Extensions:** New opcodes, BCD fixes

#### New Instructions (vs 6502)

- **Bit Operations:** TSB, TRB, BIT (new modes)
- **Branches:** BRA (unconditional)
- **Stack:** PHX, PHY, PLX, PLY
- **Addressing:** (ZP) indirect, (ZP,X) indexed indirect
- **No-ops:** All illegal opcodes become NOPs

#### Removed "Illegal" Opcodes

- All undocumented 6502 opcodes function as NOP

---

## 🔧 Implementation Priorities

### Phase 1: Foundation (Weeks 1-4)

1. **Backend Interface** - Define `IArchitectureBackend`
2. **M68000 Parser** - Instruction encoding logic
3. **ARM7 Parser** - ARM/Thumb dual-mode
4. **Test Framework** - Per-architecture validation

### Phase 2: Code Generation (Weeks 5-8)

1. **M68000 CodeGen** - All addressing modes
2. **ARM7 CodeGen** - Condition codes, shifts
3. **Z80 CodeGen** - Prefixed instructions
4. **Integration Tests** - Hello World per system

### Phase 3: ROM Building (Weeks 9-12)

1. **Genesis Header** - SEGA string, checksum
2. **GBA Header** - Nintendo logo, CRC
3. **SMS Header** - Region codes
4. **Memory Mappers** - Per-system banking

---

## 📊 Complexity Analysis

| Architecture | Opcodes | Addr Modes | Encoding | Priority |
|-------------|---------|-----------|----------|----------|
| M68000 | ~60 | 14 | Complex | High |
| ARM7TDMI | ~50 (ARM) | 9 | Moderate | High |
| Z80 | ~250 | 12 | Moderate | Medium |
| HuC6280 | ~80 | 13 | Simple | Medium |
| V30MZ | ~200 | Variable | Complex | Low |
| 65C02 | ~115 | 13 | Simple | Low |

**Encoding Complexity:**

- **Simple:** Fixed-width or minimal variation (6502 family)
- **Moderate:** Variable-width with patterns (Z80, ARM Thumb)
- **Complex:** Multi-byte with many edge cases (68K, 8086)

---

## 🧪 Testing Strategy

### Per-Architecture Test Suite

```csharp
[Theory]
[InlineData("MOVE.L D0,D1", new byte[] { 0x22, 0x00 })]
[InlineData("ADD.W #$1234,D0", new byte[] { 0x06, 0x40, 0x12, 0x34 })]
public void M68000_InstructionEncoding(string asm, byte[] expected) {
	var result = Assemble(asm, TargetArchitecture.M68000);
	Assert.Equal(expected, result);
}
```

### Cross-Architecture Validation

```asm
; Same logic, different targets
.ifdef GENESIS
	move.l #$c00000,a0
.endif
.ifdef GBA
	ldr r0,=0x04000000
.endif
```

---

**Next:** Begin M68000 instruction encoder implementation.
