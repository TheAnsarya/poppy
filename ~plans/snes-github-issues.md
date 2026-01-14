# 📋 SNES GitHub Issues

This document contains the GitHub issue content for SNES/65816 implementation.

---

## Epic #75: Complete SNES/65816 Support

**Labels:** `epic`, `enhancement`, `snes`, `65816`
**Milestone:** v0.3.0 - SNES Support

### Description

Complete the SNES/65816 implementation in Poppy to support real-world SNES projects like Final Fantasy Mystic Quest and Dragon Quest 3 Remake.

### Current Status (~70% Complete)

#### ✅ Implemented
- 65816 instruction set encoding (InstructionSet65816.cs)
- SNES header builder (SnesHeaderBuilder.cs)
- Target architecture enum
- Basic directives (.snes, .lorom, .hirom, .exhirom)
- M/X flag tracking with REP/SEP
- Basic header directives

#### ❌ Missing
- M/X flag-aware instruction sizing
- Bank:Address notation parsing
- Proper ROM layout generation
- Complete header directives
- Memory mapper utilities
- Comprehensive documentation

### Sub-Issues

**Core (P0):**
- [ ] #76 - M/X flag-aware instruction sizing
- [ ] #77 - Bank:Address notation parsing
- [ ] #78 - Proper SNES ROM layout generation
- [ ] #79 - SNES memory mapper implementation

**Headers (P1):**
- [ ] #80 - Complete SNES header directives
- [ ] #81 - SNES vector directives
- [ ] #82 - ExLoROM/ExHiROM support

**Docs & Tests (P2):**
- [ ] #83 - SNES integration tests
- [ ] #84 - SNES example project
- [ ] #85 - SNES user documentation
- [ ] #86 - SNES migration guide (bass/xkas)

### Acceptance Criteria

- [ ] All 65816 instructions encode correctly with M/X awareness
- [ ] Bank:Address notation works in all contexts
- [ ] LoROM and HiROM ROMs generate correctly
- [ ] Headers at correct offsets ($7fc0/$ffc0)
- [ ] Checksums calculate correctly
- [ ] Example runs in bsnes/Snes9x
- [ ] 50+ SNES tests pass

---

## Issue #76: M/X Flag-Aware Instruction Sizing

**Labels:** `enhancement`, `snes`, `65816`, `bug`
**Parent:** Epic #75

### Problem

65816 immediate mode instruction sizes depend on the M (accumulator) and X (index) flags:

| Instruction | M=1 (8-bit) | M=0 (16-bit) |
|-------------|-------------|--------------|
| `lda #$ff` | 2 bytes | 3 bytes |
| `adc #$1234` | 2 bytes | 3 bytes |

| Instruction | X=1 (8-bit) | X=0 (16-bit) |
|-------------|-------------|--------------|
| `ldx #$ff` | 2 bytes | 3 bytes |
| `ldy #$1234` | 2 bytes | 3 bytes |

Currently, Poppy uses fixed sizes regardless of flag state.

### Solution

1. Update `InstructionSet65816.GetInstructionSize()` to accept M/X state
2. Track current M/X state in `SemanticAnalyzer`
3. Update `CodeGenerator` to use state-aware sizing
4. Respect `.a8`, `.a16`, `.i8`, `.i16` directives

### Affected Files

- `src/Poppy.Core/CodeGen/InstructionSet65816.cs`
- `src/Poppy.Core/Semantics/SemanticAnalyzer.cs`
- `src/Poppy.Core/CodeGen/CodeGenerator.cs`

### Acceptance Criteria

- [ ] `lda #$ff` = 2 bytes after `.a8` or `sep #$20`
- [ ] `lda #$1234` = 3 bytes after `.a16` or `rep #$20`
- [ ] `ldx #$ff` = 2 bytes after `.i8` or `sep #$10`
- [ ] `ldx #$1234` = 3 bytes after `.i16` or `rep #$10`
- [ ] Tests verify sizing for all flag combinations

---

## Issue #77: Bank:Address Notation Parsing

**Labels:** `enhancement`, `snes`, `parser`
**Parent:** Epic #75

### Problem

SNES uses 24-bit addressing with bank bytes. Standard notation: `$bb:aaaa`

```asm
lda $7e:1234        ; Load from bank $7e, address $1234
jml $c0:8000        ; Long jump to bank $c0
```

Currently not supported.

### Solution

1. Update Lexer to recognize `:` in hex addresses
2. Add `BankAddressNode` to AST
3. Parse as `(bank << 16) | address`
4. Support in label references: `lda my_label >> 16` for bank

### Syntax Support

```asm
; Direct bank:address
lda $7e:0000
sta $00:2100

; With addressing modes
lda.l $c0:8000,x

; Bank extraction operators
lda #^my_label      ; Bank byte of label
lda #>my_label      ; High byte of address
lda #<my_label      ; Low byte of address
```

### Acceptance Criteria

- [ ] Parser recognizes `$bb:aaaa` syntax
- [ ] Evaluates to correct 24-bit value
- [ ] Works with all long addressing modes
- [ ] Bank extraction operators work
- [ ] Tests cover edge cases

---

## Issue #78: Proper SNES ROM Layout Generation

**Labels:** `enhancement`, `snes`, `output`
**Parent:** Epic #75

### Problem

SNES internal headers must be at specific locations:
- **LoROM:** $007fc0-$007fff
- **HiROM:** $00ffc0-$00ffff

Currently, Poppy prepends the header instead of placing it correctly.

### Solution

Create `SnesRomBuilder` class:

```csharp
public class SnesRomBuilder {
    public byte[] Build(byte[] code, SnesHeaderBuilder header, SnesMapMode mode);
    
    private int GetHeaderOffset(SnesMapMode mode);
    private void PlaceHeader(byte[] rom, int offset, byte[] header);
    private void CalculateChecksum(byte[] rom, SnesMapMode mode);
}
```

### ROM Layout (LoROM 256KB)

```
Offset   | Content
---------|------------------
$000000  | Code/Data (bank $00)
...      | ...
$007fc0  | Internal Header (64 bytes)
$008000  | Code/Data (bank $01)
...      | ...
$03ffff  | End of ROM
```

### Acceptance Criteria

- [ ] LoROM header at $007fc0
- [ ] HiROM header at $00ffc0
- [ ] ROM padded to correct size
- [ ] Checksum calculated correctly
- [ ] ROMs run in bsnes, Snes9x, Mesen-S

---

## Issue #79: SNES Memory Mapper Implementation

**Labels:** `enhancement`, `snes`, `utility`
**Parent:** Epic #75

### Problem

SNES has complex memory mapping. Address translation needed for:
- PC addresses ↔ ROM file offsets
- Mirror detection
- Bank calculations

### Solution

Create `SnesMemoryMapper` class:

```csharp
public static class SnesMemoryMapper {
    // LoROM: Banks $00-$7d, addresses $8000-$ffff map to ROM
    // HiROM: Banks $c0-$ff, addresses $0000-$ffff map to ROM
    
    public static int PcToFileOffset(int pc, SnesMapMode mode);
    public static int FileOffsetToPc(int offset, SnesMapMode mode);
    public static int GetBank(int pc, SnesMapMode mode);
    public static bool IsRomAddress(int address, SnesMapMode mode);
    public static bool IsMirror(int address, SnesMapMode mode);
    public static int UnmirrorAddress(int address, SnesMapMode mode);
}
```

### Address Mapping Examples

**LoROM:**
```
PC $008000 → File $000000
PC $018000 → File $008000
PC $7e0000 → WRAM (not ROM)
```

**HiROM:**
```
PC $c00000 → File $000000
PC $c10000 → File $010000
PC $400000 → Mirror of $c00000
```

### Acceptance Criteria

- [ ] LoROM translation correct
- [ ] HiROM translation correct
- [ ] Mirror addresses detected
- [ ] Invalid addresses rejected
- [ ] Unit tests for all modes

---

## Issue #80: Complete SNES Header Directives

**Labels:** `enhancement`, `snes`, `directives`
**Parent:** Epic #75

### Current Directives

- `.snes_title <string>`
- `.snes_region <code>`
- `.snes_version <num>`
- `.snes_rom_size <kb>`
- `.snes_ram_size <kb>`
- `.snes_fastrom`

### New Directives

```asm
.snes_cartridge_type rom           ; ROM only (default)
.snes_cartridge_type rom_ram       ; ROM + RAM
.snes_cartridge_type rom_ram_sram  ; ROM + RAM + Battery
.snes_cartridge_type superfx       ; Super FX chip
.snes_cartridge_type sa1           ; SA-1 chip
.snes_cartridge_type dsp1          ; DSP-1 chip

.snes_maker_code "01"              ; 2-char maker code
.snes_game_code "SMWE"             ; 4-char game code
.snes_expansion_ram 0              ; Expansion RAM (0, 16, 64, 256 KB)
.snes_special_version 0            ; Special version byte
```

### Acceptance Criteria

- [ ] All directives parsed
- [ ] Header builder updated
- [ ] Invalid values report errors
- [ ] Documentation complete

---

## Issue #81: SNES Vector Directives

**Labels:** `enhancement`, `snes`, `directives`
**Parent:** Epic #75

### Problem

Setting interrupt vectors requires knowing exact memory offsets.

### Solution

Add directives for vector assignment:

```asm
; Native mode vectors (65816 mode)
.snes_native_cop    handler_cop     ; $ffe4
.snes_native_brk    handler_brk     ; $ffe6
.snes_native_abort  handler_abort   ; $ffe8
.snes_native_nmi    handler_nmi     ; $ffea
.snes_native_irq    handler_irq     ; $ffee

; Emulation mode vectors (6502 mode)
.snes_emu_cop       handler_cop     ; $fff4
.snes_emu_abort     handler_abort   ; $fff8
.snes_emu_nmi       handler_nmi     ; $fffa
.snes_emu_reset     reset           ; $fffc (required!)
.snes_emu_irq       handler_irq     ; $fffe
```

### Acceptance Criteria

- [ ] All vector directives work
- [ ] Vectors placed in header
- [ ] Labels resolved at link time
- [ ] Missing reset vector = error

---

## Issue #82: ExLoROM/ExHiROM Support

**Labels:** `enhancement`, `snes`, `mapping`
**Parent:** Epic #75

### Problem

Extended mapping modes for ROMs > 4MB:
- ExLoROM: Up to 8MB
- ExHiROM: Up to 8MB

### Directives

```asm
.exlorom    ; Extended LoROM mapping
.exhirom    ; Extended HiROM mapping (already exists)
```

### Acceptance Criteria

- [ ] `.exlorom` directive works
- [ ] Address mapping correct
- [ ] Large ROM generation
- [ ] Header at correct offset

---

## Issue #83: SNES Integration Tests

**Labels:** `testing`, `snes`
**Parent:** Epic #75

### Test Categories

1. **Basic ROM Generation**
   - LoROM hello world
   - HiROM hello world
   - Header at correct offset

2. **M/X Flag Tests**
   - 8-bit mode operations
   - 16-bit mode operations
   - Mode switching

3. **Address Tests**
   - Bank:address parsing
   - Long addressing modes
   - Memory mapping

4. **Header Tests**
   - All directives
   - Checksum calculation
   - Vector placement

### Acceptance Criteria

- [ ] 50+ SNES-specific tests
- [ ] All tests pass
- [ ] Coverage for all features

---

## Issue #84: SNES Example Project

**Labels:** `documentation`, `snes`, `example`
**Parent:** Epic #75

### Structure

```
examples/snes-hello-world/
├── main.pasm           # Main source
├── poppy.json          # Project config
├── README.md           # Instructions
└── include/
    ├── snes.inc        # Hardware constants
    └── registers.inc   # Register definitions
```

### Features Demonstrated

- SNES header setup
- PPU initialization
- Mode switching (REP/SEP)
- DMA transfers
- V-blank interrupt
- "HELLO WORLD" display

### Acceptance Criteria

- [ ] Compiles with `poppy`
- [ ] Runs in emulators
- [ ] Displays message
- [ ] Well-commented
- [ ] README complete

---

## Issue #85: SNES User Documentation

**Labels:** `documentation`, `snes`
**Parent:** Epic #75

### Document: `docs/snes-assembly.md`

### Outline

1. **Introduction to SNES Development**
2. **65816 Architecture**
   - Registers
   - Addressing modes
   - M/X flags
3. **Memory Mapping**
   - LoROM explained
   - HiROM explained
   - Bank switching
4. **SNES Header Format**
   - All fields explained
   - Checksum algorithm
5. **PPU Programming**
   - Video modes
   - Tilemaps
   - Sprites
6. **DMA Programming**
7. **Interrupt Handling**
8. **Poppy Directives Reference**
9. **Complete Example**

### Acceptance Criteria

- [ ] All sections written
- [ ] Code examples work
- [ ] Technical accuracy
- [ ] Linked from main docs

---

## Issue #86: SNES Migration Guide

**Labels:** `documentation`, `snes`, `migration`
**Parent:** Epic #75

### Document: `docs/migration-from-bass.md`

### Cover Assemblers

- **bass** (byuu's assembler)
- **xkas** (older SNES assembler)
- **asar** (SMW hacking)
- **wla-dx** (multi-platform)

### Content

1. **Syntax Comparison Table**
2. **Directive Mapping**
3. **Label Differences**
4. **Macro Conversion**
5. **Common Gotchas**
6. **Conversion Examples**

### Acceptance Criteria

- [ ] All assemblers covered
- [ ] Side-by-side examples
- [ ] Tested conversions
- [ ] Linked from docs

---

_Issues created: January 14, 2026_

