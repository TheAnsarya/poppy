# Architecture Separation Plan

## Component Model Decision

### Question: Languages or Systems?

**Answer: Components are organized by CPU architecture (language), NOT by system (platform).**

ARM and Thumb stay together in **one component** (`Poppy.Arch.ARM7TDMI`).

### Rationale

1. **ARM+Thumb share core data** — `ConditionMap`, `DataProcessingOpcodes`, `ShiftMap`, `RegisterMap`, and ~30 overlapping mnemonics. Mode switching is an in-program directive (`.arm`/`.thumb`), not a project-level choice. They are one CPU with two instruction encodings.

2. **6502 family has inheritance** — MOS6507 delegates 100% to MOS6502. MOS65SC02 is a delta table over 6502. These MUST share a component or reference a common base.

3. **Platform artifacts follow the CPU** — NES ROM format is a system concern, but it's always coupled 1:1 with a CPU architecture. ROM builders and header builders belong with their CPU.

4. **SPC700 is a coprocessor** — It targets SNES audio, but it has its own independent instruction set with no overlap with 65816. It's a separate component.

5. **HuC6280 is a 65C02 variant** — Unlike MOS65SC02 which delegates to 6502, HuC6280 has its own opcode table (643 lines) with enough divergence (block transfers, bit ops, hardware I/O) to justify a separate component. However, if code sharing becomes desirable, it could reference `Poppy.Arch.MOS6502`.

## Component Map

| Component | CPUs | Systems | InstructionSets | Profiles | ROM Builders | Header Builders | Other |
|-----------|------|---------|-----------------|----------|--------------|-----------------|-------|
| **Poppy.Arch.MOS6502** | 6502, 6507, 65SC02 | NES, Atari 2600, Lynx | 6502 (268), 6507 (52→delegate), 65SC02 (128→delta) | Mos6502, Mos6507, Mos65sc02 | Atari2600RomBuilder (168), AtariLynxRomBuilder (235) | INesHeaderBuilder (232) | LynxBootCodeGenerator |
| **Poppy.Arch.WDC65816** | 65816 | SNES | 65816 (483) | Wdc65816 | SnesRomBuilder (220) | SnesHeaderBuilder (468) | — |
| **Poppy.Arch.SM83** | SM83 | Game Boy | SM83 (526) | Sm83 | GbRomBuilder (168) | GbHeaderBuilder (274) | — |
| **Poppy.Arch.M68000** | M68000 | Genesis | M68000 (734) | M68000 | GenesisRomBuilder (308) | — | — |
| **Poppy.Arch.Z80** | Z80 | SMS/GG | Z80 (585) | Z80 | MasterSystemRomBuilder (230) | — | — |
| **Poppy.Arch.V30MZ** | V30MZ | WonderSwan | V30MZ (364) | V30mz | WonderSwanRomBuilder (256) | — | — |
| **Poppy.Arch.ARM7TDMI** | ARM + Thumb | GBA | ARM7TDMI (1140, both modes) | Arm7tdmi | GbaRomBuilder (293) | — | — |
| **Poppy.Arch.SPC700** | SPC700 | SNES Audio | SPC700 (810) | Spc700 | SpcFileBuilder | — | — |
| **Poppy.Arch.HuC6280** | HuC6280 | TG-16/PCE | HuC6280 (643) | Huc6280 | TurboGrafxRomBuilder (303) | — | — |

**Total: 9 architecture components**

## Shared Infrastructure (stays in Poppy.Core)

| Component | Purpose |
|-----------|---------|
| `ITargetProfile` | Interface — all arch projects reference this |
| `IInstructionEncoder` | Interface |
| `IRomBuilder` | Interface |
| `TargetResolver` | Alias resolution + profile dispatch |
| `TargetArchitecture` | Enum (must move from SemanticAnalyzer to Arch namespace) |
| `EncodedInstruction` | Shared record struct |
| `MnemonicModeComparer<T>` | Shared comparer for opcode tables |
| `AddressingMode` | Shared addressing mode enum (from Parser) |
| `Lexer`, `Parser`, `SemanticAnalyzer`, `CodeGenerator` | Core pipeline |
| `OutputSegment`, `MemorySegment` | Data models |
| All generators (CDL, Pansy, Listing, Memory Map, Symbol, Diz) | Shared output |
| `TextEncoder`, `ImageToChrConverter`, `JsonToAsmConverter` | Shared utilities |

## Dependency Graph

```
Poppy.Core (shared framework, interfaces, pipeline)
  ├── Poppy.Arch.MOS6502 (references Poppy.Core)
  ├── Poppy.Arch.WDC65816 (references Poppy.Core)
  ├── Poppy.Arch.SM83 (references Poppy.Core)
  ├── Poppy.Arch.M68000 (references Poppy.Core)
  ├── Poppy.Arch.Z80 (references Poppy.Core)
  ├── Poppy.Arch.V30MZ (references Poppy.Core)
  ├── Poppy.Arch.ARM7TDMI (references Poppy.Core)
  ├── Poppy.Arch.SPC700 (references Poppy.Core)
  └── Poppy.Arch.HuC6280 (references Poppy.Core)

Poppy.CLI → Poppy.Core + all Poppy.Arch.* (for registration)
Poppy.Tests → Poppy.Core + all Poppy.Arch.* (InternalsVisibleTo)
Poppy.Benchmarks → Poppy.Core + all Poppy.Arch.*
```

## Migration Strategy

### Phase 1: Prepare Shared Infrastructure (in Poppy.Core)

1. **Move `TargetArchitecture` enum** from SemanticAnalyzer.cs to `Poppy.Core/Arch/TargetArchitecture.cs`
2. **Extract directive handler interfaces** — Each arch component should provide its own directive handler that SemanticAnalyzer can dispatch to
3. **Extract ROM-building interface from CodeGenerator** — CodeGenerator should call `profile.CreateRomBuilder()` instead of its if-chain
4. **Make `MnemonicModeComparer<T>` public** — Arch projects need it for opcode tables
5. **Extract `InstructionEncoding` to shared location** — Each InstructionSet has its own copy; unify into one shared type or let each arch use `EncodedInstruction` directly

### Phase 2: Extract MOS6502 (Proof of Concept) — Issue #247

1. Create `src/Poppy.Arch.MOS6502/` project
2. Move `InstructionSet6502.cs`, `InstructionSet6507.cs`, `InstructionSet65SC02.cs`
3. Move `Mos6502Profile.cs`, `Mos6507Profile.cs`, `Mos65sc02Profile.cs`
4. Move `INesHeaderBuilder.cs`, `Atari2600RomBuilder.cs`, `AtariLynxRomBuilder.cs`, `LynxBootCodeGenerator.cs`
5. Wire up `CreateRomBuilder()` to return actual builders
6. Register profiles via `TargetResolver` extension or discovery
7. Update InternalsVisibleTo
8. **All 3185 tests must pass**

### Phase 3: Extract Remaining Architectures

One component at a time, in order of size/complexity:

1. **SM83** — Clean cut, no dependencies on other archs
2. **WDC65816** — Moderate complexity (LoROM/HiROM, 16-bit modes)
3. **Z80** — Clean cut
4. **V30MZ** — Clean cut (but has CodeGenerator special handler)
5. **M68000** — Clean cut
6. **ARM7TDMI** — Dual-mode complexity, placeholder encoder
7. **SPC700** — Clean cut
8. **HuC6280** — Clean cut

### Phase 4: Eliminate Platform Dispatch from Core

1. Replace `if (_target == TargetArchitecture.XXX)` chains in CodeGenerator with interface dispatch
2. Replace `HandleXxxDirective()` methods in SemanticAnalyzer with arch-provided handlers
3. Replace the giant mnemonic switch in Lexer with `IInstructionEncoder.Mnemonics`

## Discovery/Registration Mechanism

Architecture components register themselves with Poppy.Core via a lightweight mechanism:

```csharp
// In each Poppy.Arch.* project:
public static class Registration {
    public static void Register() {
        TargetResolver.RegisterProfile(Mos6502Profile.Instance);
        TargetResolver.RegisterProfile(Mos6507Profile.Instance);
        TargetResolver.RegisterProfile(Mos65sc02Profile.Instance);
    }
}

// In Poppy.CLI / Poppy.Tests startup:
Poppy.Arch.MOS6502.Registration.Register();
// ... etc
```

Alternatively, use assembly scanning (`[assembly: PoppyArch(...)]` attribute) — but explicit registration is simpler and faster.

## Risk Mitigation

1. **Byte-identical output** — Every extraction phase must produce identical compiler output
2. **Incremental** — One arch at a time, full test suite after each
3. **No behavior changes** — Pure refactoring, no new features during extraction
4. **Backward compatible** — CLI and API contract unchanged
5. **InternalsVisibleTo** — Arch projects grant access to Poppy.Tests for InstructionSet testing

## Line Count Estimates

| What Moves | Lines |
|------------|-------|
| InstructionSet files (11 total) | ~5,752 |
| Profile files (11 total) | ~550 |
| ROM builder files (9 total) | ~2,419 |
| Header builder files (4 total) | ~1,206 |
| LynxBootCodeGenerator | ~200 |
| **Total extracted from Poppy.Core** | **~10,127** |
| **Remaining in Poppy.Core** | **~10,000+** |

## Related Issues

- #238 — [Epic] Architecture Separation
- #245 — Split InstructionSet/RomBuilder per-architecture
- #247 — PoC: Extract MOS6502 backend
- #248 — Organize test suite per-architecture
- #240 — Target-aware lexer
- #246 — Extend parser for multi-operand
