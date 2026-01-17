---
name: Feature - Bank Switching Directives
about: Add bank switching directives for multi-bank Atari 2600 ROMs
title: 'Implement .f8, .f6, .f4, .fe, .e0 bank switching directives'
labels: feature, atari2600, bank-switching, high-priority
assignees: ''
milestone: 'Milestone 1: Basic Roundtrip'
---

## Overview

Add support for Atari 2600 bank switching schemes to enable assembly of multi-bank ROMs (8KB+).

## Bank Switching Schemes

### F8 (8KB, 2 banks)

```pasm
.system:atari2600
.f8  ; 8KB ROM, 2x 4KB banks

.bank 0
.org $F000
	; Bank 0 code
	sta $FFF9  ; Switch to bank 1
	
.bank 1
.org $F000
	; Bank 1 code
	sta $FFF8  ; Switch to bank 0
```

**Hotspots**: `$FFF8` (bank 0), `$FFF9` (bank 1)

### F6 (16KB, 4 banks)

```pasm
.system:atari2600
.f6  ; 16KB ROM, 4x 4KB banks

.bank 0
.org $F000
	sta $FFF6  ; Switch to bank 0
	sta $FFF7  ; Switch to bank 1
	sta $FFF8  ; Switch to bank 2
	sta $FFF9  ; Switch to bank 3
```

**Hotspots**: `$FFF6-$FFF9` (banks 0-3)

### F4 (32KB, 8 banks)

```pasm
.system:atari2600
.f4  ; 32KB ROM, 8x 4KB banks

.bank 0
.org $F000
	; Bank switch hotspots: $FFF4-$FFFB
```

**Hotspots**: `$FFF4-$FFFB` (banks 0-7)

### FE (8KB Activision)

```pasm
.system:atari2600
.fe  ; 8KB Activision mapper

.bank 0
.org $F000
	jsr $01FE  ; Switch to bank 0
	jsr $11FE  ; Switch to bank 1
```

**Hotspots**: Special JSR instruction triggers, address bit 12 determines bank

### E0 (8KB Parker Bros)

```pasm
.system:atari2600
.e0  ; 8KB Parker Bros mapper

.slice 0  ; 1KB slices
.org $F000
	; Complex 4-slice banking
```

**Hotspots**: `$FE0-$FE7` control 4 independent 1KB slices

## Implementation

### 1. Bank Directive Parser

```typescript
class BankDirective {
	scheme: 'f8' | 'f6' | 'f4' | 'fe' | 'e0';
	currentBank: number;
	banks: Map<number, Buffer>;
	
	switchBank(num: number): void;
	assemble(): Buffer;
}
```

### 2. Bank Assembly

- Track current bank during assembly
- Allow multiple `.bank N` directives
- Each bank has independent address space
- Merge banks into final ROM

### 3. Cross-Bank Labels

```pasm
.bank 0
reset:
	jmp bank1_routine  ; Cross-bank reference
	sta $FFF9          ; Switch to bank 1
bank1_routine = $F100 + $FFF9  ; Pseudo-label for bank 1 address

.bank 1
.org $F100
bank1_routine:
	; Code here
```

### 4. Vector Table Handling

Only last bank's vectors are used (mirrored across all banks on hardware):

```pasm
.bank 1  ; Last bank in F8
.org $FFFC
.word reset
.word reset
```

## Test Cases

### Test 1: F8 Basic

```pasm
.system:atari2600
.f8

.bank 0
.org $F000
reset:
	lda #$01
	sta $FFF9  ; Jump to bank 1

.bank 1
.org $F000
	lda #$02
	sta $FFF8  ; Back to bank 0

.org $FFFC
.word reset
.word reset
```

**Expected**: 8192-byte .a26 file, both banks present

### Test 2: F6 Multi-Bank

```pasm
.system:atari2600
.f6

.bank 0, 1, 2, 3
.org $F000
	; Repeat pattern in each bank
```

**Expected**: 16384-byte .a26 file

### Test 3: Ms. Pac-Man Roundtrip

```bash
peony disasm "Ms. Pac-Man.a26" -o mspacman.pasm --all-banks
poppy build mspacman.pasm -o rebuilt.a26
peony verify "Ms. Pac-Man.a26" --reassembled rebuilt.a26
```

**Expected**: 100% match

## Acceptance Criteria

- [ ] `.f8`, `.f6`, `.f4`, `.fe`, `.e0` directives recognized
- [ ] `.bank N` switches active bank
- [ ] Cross-bank labels resolve correctly
- [ ] Output ROM size matches scheme (8/16/32KB)
- [ ] Vector table only in final bank
- [ ] Hotspot addresses reserved/documented
- [ ] Multi-bank test ROMs assemble correctly
- [ ] Documentation with examples for each scheme

## Related

- Issue #4: Atari 2600 Code Generation (depends on)
- Peony Issue #2: Multi-Bank Traversal (complementary)
- GameInfo: `docs/Atari-2600/Bank-Switching.md` (reference)

## Priority

**High** - Required for most commercial games (Combat, Ms. Pac-Man, Donkey Kong, etc.)

## Implementation Notes

- Start with F8 (simplest, 2 banks)
- Then F6 (4 banks, similar to F8)
- FE and E0 are complex, can be later
- Use existing bank switching logic from NES mappers as reference
