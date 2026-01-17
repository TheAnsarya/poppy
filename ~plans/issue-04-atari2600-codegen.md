---
name: Feature - Atari 2600 Code Generation
about: Implement 6502 code generation for Atari 2600 target platform
title: 'Add Atari 2600 platform support with .a26 output'
labels: feature, atari2600, platform, high-priority
assignees: ''
milestone: 'Milestone 1: Basic Roundtrip'
---

## Overview

Implement Atari 2600 code generation support in Poppy assembler to enable roundtrip testing with Peony disassembler.

## Requirements

### 1. Platform Detection

```pasm
.system:atari2600
```

Recognizes `atari2600` as a valid platform target.

### 2. Output Format

- **File Extension**: `.a26` (NOT `.bin`)
- **ROM Size**: 2KB/4KB/8KB/16KB/32KB (auto-detect or configurable)
- **Byte Order**: Little-endian (6502 standard)
- **Header**: None (raw binary, no iNES-style header)

### 3. Memory Map

```
$0000-$007F   TIA Registers (write)
$0080-$00FF   RAM (128 bytes)
$0280-$0297   RIOT Registers
$F000-$FFFF   ROM (4KB standard, address depends on size)
```

### 4. Vector Table

```pasm
.org $FFFC
.word reset    ; Reset vector
.word irq      ; IRQ vector (unused on 2600)
```

Auto-generate vectors at end of ROM if not specified.

### 5. Address Resolution

- ROM loads at address based on size:
  - 2KB: `$F800-$FFFF`
  - 4KB: `$F000-$FFFF`
  - 8KB: `$E000-$FFFF` (with bank switching)
  - 16KB: `$C000-$FFFF` (with bank switching)
  - 32KB: `$8000-$FFFF` (with bank switching)

## Implementation Steps

1. **TypeScript Platform Class** (`src/platforms/atari2600.ts`)
   ```typescript
   export class Atari2600Platform extends Platform {
       name = 'atari2600';
       cpu = '6502';
       extension = '.a26';
       
       getRomSize(code: Buffer): number { /* ... */ }
       getLoadAddress(size: number): number { /* ... */ }
       generateVectors(symbols: SymbolTable): Buffer { /* ... */ }
   }
   ```

2. **Register Platform** (`src/platforms/index.ts`)
   ```typescript
   import { Atari2600Platform } from './atari2600';
   
   export const platforms = {
       'atari2600': new Atari2600Platform(),
       // ... existing platforms
   };
   ```

3. **Assembler Integration** (`src/assembler.ts`)
   - Detect `.system:atari2600` directive
   - Set ROM base address
   - Generate vectors if missing
   - Pad ROM to valid size (2/4/8/16/32KB)

4. **Output Writer**
   - Write raw binary (no header)
   - Ensure correct size
   - Verify reset vector is set

## Test Cases

### Test 1: Hello World (4KB)

```pasm
.system:atari2600

.org $F000

reset:
	ldx #$FF
	txs
	lda #$00
main_loop:
	sta WSYNC
	jmp main_loop

.org $FFFC
.word reset
.word reset
```

**Expected**: `hello.a26` - 4096 bytes, reset vector at `$FFFC` points to `$F000`

### Test 2: Size Auto-Detection

```pasm
.system:atari2600

.org $F800  ; 2KB ROM
reset:
	jmp reset
```

**Expected**: `test.a26` - 2048 bytes, padded correctly

### Test 3: Peony Roundtrip

```bash
# Disassemble existing ROM
peony disasm original.a26 -o disasm.pasm

# Assemble disassembly
poppy build disasm.pasm -o rebuilt.a26

# Verify
peony verify original.a26 --reassembled rebuilt.a26
```

**Expected**: 100% byte match

## Acceptance Criteria

- [ ] `.system:atari2600` directive recognized
- [ ] Outputs `.a26` files (not `.bin`)
- [ ] Hello World assembles to valid 4KB ROM
- [ ] Reset vector auto-generated if missing
- [ ] ROM size calculated/padded correctly
- [ ] Adventure (PAL) disassembly re-assembles
- [ ] CRC32 verification passes
- [ ] Documentation updated

## Related

- Example: `examples/atari2600-hello-world/main.pasm`
- Peony Issue: Roundtrip testing workflow
- GameInfo: `Atari2600/Adventure (1978) (Atari) [PAL]/source/adventure.pasm`

## Priority

**High** - Blocking roundtrip testing and all Atari 2600 development

## Implementation Notes

- Start with simple 2KB/4KB support
- Bank switching can be separate issue (#5)
- Focus on correctness over features
- Use existing NES platform as reference
