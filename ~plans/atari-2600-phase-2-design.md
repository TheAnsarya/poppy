# Atari 2600 Phase 2 Design — Poppy Test Suite & Include Completeness

## Goal

Achieve comprehensive test coverage for Atari 2600 assembler support and improve include file organization.

## Current State

- `InstructionSet6507.cs` delegates to 6502 (functionally identical)
- `Atari2600RomBuilder.cs` supports 8 bankswitching schemes
- `includes/atari2600/tia.pasm` has all TIA + RIOT registers + constants
- 4-5 basic tests in `Atari2600CodeGeneratorTests.cs`
- `atari2600-hello-world` example exists

## Phase 2 Work Items

### 1. Bankswitching Test Suite (#191)

Dedicated tests for each scheme:

```
[Scheme]     [ROM Size]  [Banks]  [Hotspot Range]
None         2K/4K       1        N/A
F8           8K          2        $1ff8-$1ff9
F6           16K         4        $1ff6-$1ff9
F4           32K         8        $1ff4-$1ffb
FE           8K          2        Stack-based
E0           8K          4x2K     $1fe0-$1fef
3F           Up to 512K  N        $003f (TIA range)
E7           16K+2K RAM  8+1      $1fe0-$1fe7
```

Each test should:

1. Create ROM with correct scheme header/hotspots
2. Assemble with Poppy
3. Verify output binary size and layout
4. Verify hotspot bytes at expected addresses
5. Verify reset vector placement

### 2. ROM Size Validation (#192)

- 2K ROM: Verify padding to 4K or standalone 2K
- 4K ROM: Standard single-bank layout
- 8K-32K: Correct bank count and size
- Vector presence at `$fffc`/`$fffd` (reset) and `$fffe`/`$ffff` (IRQ)

### 3. RIOT Include (#193)

Split `tia.pasm` into:

- `tia.pasm` — TIA registers only (VSYNC through CXCLR + read registers)
- `riot.pasm` — RIOT registers (SWCHA, timers, etc.) + console switch masks
- `vcs.pasm` — Convenience include that imports both

## File Changes

- `src/Poppy.Tests/CodeGen/Atari2600RomBuilderTests.cs` — New comprehensive test file
- `includes/atari2600/riot.pasm` — New RIOT include
- `includes/atari2600/vcs.pasm` — New convenience include
