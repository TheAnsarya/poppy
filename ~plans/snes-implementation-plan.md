# 🎮 SNES/65816 Implementation Plan

**Project:** Poppy Compiler
**Created:** January 14, 2026
**Status:** ~70% Complete - Finishing Phase

---

## 📊 Current Status Summary

### ✅ Already Implemented

| Component | Status | File |
|-----------|--------|------|
| 65816 Instruction Set | ✅ Complete | `InstructionSet65816.cs` |
| SNES Header Builder | ✅ Complete | `SnesHeaderBuilder.cs` |
| Target Architecture | ✅ Complete | `TargetArchitecture.cs` |
| Basic Directives | ✅ Complete | `.snes`, `.lorom`, `.hirom` |
| M/X Flag Tracking | ✅ Complete | `SemanticAnalyzer.cs` |
| Header Directives | ⚠️ Partial | `.snes_title`, `.snes_region`, etc. |
| ROM Generation | ⚠️ Partial | Header prepended, not placed |

### ❌ Missing Features

| Feature | Priority | Effort |
|---------|----------|--------|
| M/X-aware instruction sizing | High | Medium |
| Bank:Address parsing | High | Medium |
| Proper ROM layout | High | High |
| Complete header directives | Medium | Low |
| Memory mapper helpers | Medium | Medium |
| ExLoROM support | Low | Low |

---

## 🎯 Epic Structure

### Epic #75: Complete SNES/65816 Support

**Goal:** Finish SNES implementation to support real-world SNES projects

**Sub-Issues:**

#### Core Implementation (P0 - Critical)

| Issue | Title | Description | Estimate |
|-------|-------|-------------|----------|
| #76 | M/X flag-aware instruction sizing | Fix immediate mode byte counts based on REP/SEP | 4h |
| #77 | Bank:Address notation parsing | Support `$7e:1234` syntax | 3h |
| #78 | Proper SNES ROM layout generation | Place header at $7fc0/$ffc0, fill gaps | 4h |
| #79 | SNES memory mapper implementation | Address translation for LoROM/HiROM | 4h |

#### Header & Directives (P1 - Important)

| Issue | Title | Description | Estimate |
|-------|-------|-------------|----------|
| #80 | Complete SNES header directives | Add .snes_cartridge_type, .snes_maker_code, etc. | 2h |
| #81 | SNES vector directives | Direct interrupt vector setting | 2h |
| #82 | ExLoROM/ExHiROM support | Extended ROM mapping modes | 2h |

#### Testing & Documentation (P2 - Important)

| Issue | Title | Description | Estimate |
|-------|-------|-------------|----------|
| #83 | SNES integration tests | End-to-end ROM generation tests | 3h |
| #84 | SNES example project | Complete hello-world SNES project | 2h |
| #85 | SNES user documentation | Comprehensive SNES assembly guide | 4h |
| #86 | SNES migration guide | Guide from bass/xkas to Poppy | 2h |

---

## 📋 Detailed Issue Specifications

### Issue #76: M/X Flag-Aware Instruction Sizing

**Parent:** Epic #75

**Problem:**
Currently, 65816 immediate mode instructions have fixed sizes. However, the size depends on the M (memory/accumulator) and X (index) flags:

- `lda #$ff` = 2 bytes when M=1 (8-bit mode)
- `lda #$ff` = 3 bytes when M=0 (16-bit mode)
- `ldx #$ff` = 2 bytes when X=1 (8-bit mode)
- `ldx #$ff` = 3 bytes when X=0 (16-bit mode)

**Solution:**
1. Track M/X flag state in SemanticAnalyzer (already partially done)
2. Update `InstructionSet65816.GetInstructionSize()` to accept M/X state
3. Return correct sizes based on flag state
4. Update CodeGenerator to emit correct byte count

**Acceptance Criteria:**
- [ ] Instruction sizing respects current M/X flag state
- [ ] REP/SEP instructions update flag tracking
- [ ] `.a8`, `.a16`, `.i8`, `.i16` directives work correctly
- [ ] Size calculations match real SNES assemblers
- [ ] Tests verify correct sizing

---

### Issue #77: Bank:Address Notation Parsing

**Parent:** Epic #75

**Problem:**
SNES uses 24-bit addressing with bank bytes. Common notation: `$7e:1234`

**Solution:**
1. Update Lexer to recognize `:` in addresses
2. Add `BankAddress` expression node
3. Parse as (bank << 16) | address
4. Support in all addressing contexts

**Syntax:**
```asm
lda $7e:1234        ; Load from bank $7e, address $1234
jml $c0:8000        ; Long jump to bank $c0
sta.l $00:0000      ; Explicit long store
```

**Acceptance Criteria:**
- [ ] Parser recognizes bank:address notation
- [ ] Evaluates to correct 24-bit value
- [ ] Works with labels (bank byte extraction)
- [ ] Tests cover various bank:address cases

---

### Issue #78: Proper SNES ROM Layout Generation

**Parent:** Epic #75

**Problem:**
SNES ROMs require the internal header at specific locations:
- LoROM: Header at $007fc0-$007fff
- HiROM: Header at $00ffc0-$00ffff

Currently, Poppy prepends the header rather than placing it correctly.

**Solution:**
1. Create `SnesRomBuilder` class
2. Calculate header position based on mapping mode
3. Pad ROM data to reach header location
4. Insert header at correct offset
5. Calculate and insert checksum

**ROM Structure (LoROM 256KB example):**
```
$000000-$007fbf: Code/Data
$007fc0-$007fff: Internal Header (64 bytes)
$008000-$03ffff: More Code/Data
```

**Acceptance Criteria:**
- [ ] Header placed at correct offset for LoROM
- [ ] Header placed at correct offset for HiROM
- [ ] ROM padded appropriately
- [ ] Checksum calculated correctly
- [ ] Output runs in emulators (bsnes, Snes9x)

---

### Issue #79: SNES Memory Mapper Implementation

**Parent:** Epic #75

**Problem:**
SNES has complex memory mapping. Addresses need translation between:
- PC (Program Counter) addresses used in code
- ROM file offsets
- Hardware addresses

**Solution:**
Create `SnesMemoryMapper` class with methods:

```csharp
public class SnesMemoryMapper {
    public int PcToFileOffset(int pc, SnesMapMode mode);
    public int FileOffsetToPc(int offset, SnesMapMode mode);
    public int GetBank(int pc);
    public bool IsValidRomAddress(int address, SnesMapMode mode);
    public bool IsMirror(int address, SnesMapMode mode);
}
```

**Acceptance Criteria:**
- [ ] LoROM address translation correct
- [ ] HiROM address translation correct
- [ ] Bank calculations correct
- [ ] Mirror detection implemented
- [ ] Tests verify address math

---

### Issue #80: Complete SNES Header Directives

**Parent:** Epic #75

**Current directives:**
- `.snes_title`
- `.snes_region`
- `.snes_version`
- `.snes_rom_size`
- `.snes_ram_size`
- `.snes_fastrom`

**New directives to add:**
- `.snes_cartridge_type <type>` - ROM, ROM+RAM, ROM+RAM+SRAM, SuperFX, SA1, etc.
- `.snes_maker_code <code>` - 2-character maker code
- `.snes_game_code <code>` - 4-character game code
- `.snes_expansion_ram <size>` - Expansion RAM size
- `.snes_special_version <ver>` - Special version byte
- `.snes_country <code>` - Country code (alias for region)

**Acceptance Criteria:**
- [ ] All directives parsed correctly
- [ ] Header builder accepts all fields
- [ ] Invalid values report errors
- [ ] Documentation updated

---

### Issue #81: SNES Vector Directives

**Parent:** Epic #75

**Purpose:**
Allow setting interrupt vectors directly without manual address placement.

**New directives:**
```asm
.snes_native_cop    handler_cop
.snes_native_brk    handler_brk
.snes_native_abort  handler_abort
.snes_native_nmi    handler_nmi
.snes_native_irq    handler_irq
.snes_emulation_cop  handler_cop
.snes_emulation_nmi  handler_nmi
.snes_emulation_reset reset
.snes_emulation_irq  handler_irq
```

**Acceptance Criteria:**
- [ ] All vector directives parsed
- [ ] Vectors placed in header correctly
- [ ] Labels resolved properly
- [ ] Tests verify vector placement

---

### Issue #82: ExLoROM/ExHiROM Support

**Parent:** Epic #75

**Problem:**
ExLoROM and ExHiROM are extended mapping modes for ROMs > 4MB.

**Solution:**
1. Add `.exlorom` directive handling
2. Update memory mapper for extended modes
3. Update header generation for extended sizes

**Acceptance Criteria:**
- [ ] `.exlorom` directive works
- [ ] ExLoROM address mapping correct
- [ ] ExHiROM address mapping correct
- [ ] Large ROM generation supported

---

### Issue #83: SNES Integration Tests

**Parent:** Epic #75

**Tests needed:**
1. LoROM hello world compilation
2. HiROM hello world compilation
3. Full header directive test
4. M/X flag mode switching test
5. Bank addressing test
6. Vector placement test
7. ROM checksum verification
8. Memory mapping validation

**Acceptance Criteria:**
- [ ] All integration tests pass
- [ ] Tests cover all SNES features
- [ ] Output verified against known-good ROMs
- [ ] Emulator compatibility confirmed

---

### Issue #84: SNES Example Project

**Parent:** Epic #75

**Create:** `examples/snes-hello-world/`

**Files:**
- `main.pasm` - Main source with full SNES setup
- `poppy.json` - Project configuration
- `README.md` - Build instructions
- `include/snes.inc` - SNES hardware constants
- `include/registers.inc` - PPU/APU register definitions

**Features demonstrated:**
- SNES header configuration
- Mode switching (REP/SEP)
- DMA transfers
- Basic PPU setup
- Interrupt handlers

**Acceptance Criteria:**
- [ ] Example compiles successfully
- [ ] ROM runs in emulators
- [ ] Displays "Hello World"
- [ ] Code well-commented
- [ ] README explains concepts

---

### Issue #85: SNES User Documentation

**Parent:** Epic #75

**Create:** `docs/snes-assembly.md`

**Sections:**
1. Introduction to SNES Development
2. 65816 Architecture Overview
3. Memory Mapping (LoROM vs HiROM)
4. Register Modes (8-bit vs 16-bit)
5. SNES Header Format
6. PPU/APU Basics
7. DMA Programming
8. Interrupt Handling
9. Poppy SNES Directives Reference
10. Complete Example Project Walkthrough

**Acceptance Criteria:**
- [ ] All sections complete
- [ ] Code examples compile
- [ ] Linked from main docs
- [ ] Reviewed for accuracy

---

### Issue #86: SNES Migration Guide

**Parent:** Epic #75

**Create:** `docs/migration-from-bass.md`

**Cover migrations from:**
- bass (byuu's assembler)
- xkas
- asar
- wla-dx

**Sections:**
- Syntax differences table
- Directive mapping
- Label syntax changes
- Macro conversion
- Project structure migration

**Acceptance Criteria:**
- [ ] All major assemblers covered
- [ ] Side-by-side comparisons
- [ ] Conversion examples
- [ ] Common gotchas listed

---

## 📅 Implementation Schedule

### Week 1: Core Implementation
- Day 1-2: Issue #76 (M/X flag sizing)
- Day 3: Issue #77 (Bank:Address parsing)
- Day 4-5: Issue #78 (ROM layout)

### Week 2: Completion
- Day 1-2: Issue #79 (Memory mapper)
- Day 3: Issue #80 (Header directives)
- Day 4: Issue #81-82 (Vectors, ExROM)
- Day 5: Issue #83 (Integration tests)

### Week 3: Documentation
- Day 1-2: Issue #84 (Example project)
- Day 3-4: Issue #85 (User docs)
- Day 5: Issue #86 (Migration guide)

---

## 🔗 Related Issues

- Epic #12: [Epic] 65816 Support (existing)
- Epic #13: [Epic] Output Formats
- Issue #39: Implement 65816 instruction set encoding ✅
- Issue #40: Implement M/X flag mode tracking ⚠️ Partial
- Issue #41: Implement SNES memory mapping directives ⚠️ Partial
- Issue #42: Implement SNES ROM format output ⚠️ Partial

---

## 📊 Success Criteria

SNES support will be complete when:

- [ ] All 65816 instructions encode correctly
- [ ] M/X flag tracking affects instruction sizes
- [ ] Bank:Address notation works
- [ ] LoROM and HiROM ROMs generate correctly
- [ ] Headers placed at correct offsets
- [ ] Checksums calculate correctly
- [ ] Example project runs in bsnes/Snes9x
- [ ] Comprehensive documentation exists
- [ ] All tests pass (target: 50+ SNES tests)

---

_Plan created: January 14, 2026_
_Target completion: February 2026_

