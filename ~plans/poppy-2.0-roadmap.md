# 🎮 Poppy 2.0 - Multi-System Compiler Roadmap

**Last Updated:** 2026-01-13  
**Status:** Planning Phase  
**Target Release:** Q2 2026

---

## 📋 Overview

Poppy 2.0 will expand beyond NES/SNES/GB to support a comprehensive range of retro gaming platforms, establishing Poppy as the premier multi-system assembly compiler for retro game development.

## 🎯 Goals

1. **Universal Assembly Language** - One syntax for all platforms
2. **Smart Code Generation** - Architecture-aware optimization
3. **Cross-Platform Development** - Easy porting between systems
4. **Modern Tooling** - VS Code integration, debugger support
5. **Complete ROM Generation** - Headers, checksums, all formats

---

## 🕹️ Target Systems - Poppy 2.0

### ✅ Currently Supported (v1.0)

- **NES** (MOS 6502) - iNES header, mappers
- **SNES** (WDC 65816) - LoRom/HiRom, FastROM
- **Game Boy** (Sharp SM83) - DMG/CGB modes

### 🎯 Tier 1 - High Priority (Q1-Q2 2026)
Systems with large homebrew communities and well-documented architectures:

#### Sega Genesis / Mega Drive

- **CPU:** Motorola 68000 @ 7.67 MHz
- **Sound:** Yamaha YM2612 (FM), TI SN76489 (PSG)
- **Resolution:** 320×224, 256 colors
- **ROM Format:** SEGA header, checksum validation
- **Features:** DMA, VDP registers, sprite system
- **Complexity:** Medium-High (CISC architecture)

#### Game Boy Advance

- **CPU:** ARM7TDMI @ 16.78 MHz
- **Sound:** 2 PCM channels, 4 GB channels
- **Resolution:** 240×160, 32,768 colors
- **ROM Format:** GBA header, Nintendo logo, checksum
- **Features:** Multiple background modes, hardware sprites
- **Complexity:** Medium (RISC, but complex peripherals)

#### Sega Master System

- **CPU:** Zilog Z80 @ 3.58 MHz
- **Sound:** TI SN76489 PSG
- **Resolution:** 256×192, 32 colors
- **ROM Format:** SMS/GG header, region codes
- **Features:** Sprite system, scrolling
- **Complexity:** Low-Medium (Z80 similar to GB)

#### TurboGrafx-16 / PC Engine

- **CPU:** HuC6280 (enhanced 6502) @ 7.16 MHz
- **Sound:** HuC6280 PSG (6 channels)
- **Resolution:** 256×224, 512 colors
- **ROM Format:** HuCard header
- **Features:** Tile-based graphics, sprite multiplexing
- **Complexity:** Low (6502-based)

### 🎯 Tier 2 - Medium Priority (Q3 2026)

#### Atari 2600 (VCS)

- **CPU:** MOS 6507 @ 1.19 MHz
- **Sound:** TIA chip
- **Resolution:** 160×192, 128 colors
- **ROM Format:** Cartridge types (2K-32K+)
- **Features:** Racing the beam, kernel tricks
- **Complexity:** High (extremely hardware-constrained)

#### Game Boy Color

- **CPU:** Sharp SM83 @ 8 MHz (double-speed)
- **Sound:** 4 channels (same as DMG)
- **Resolution:** 160×144, 32,768 colors (56 on screen)
- **ROM Format:** Enhanced GB header, CGB flag
- **Features:** Color palettes, VRAM banking
- **Complexity:** Low (extends existing GB support)

#### WonderSwan / WonderSwan Color

- **CPU:** NEC V30MZ @ 3.07 MHz
- **Sound:** 4-channel PCM
- **Resolution:** 224×144, 4096 colors (WSC)
- **ROM Format:** WonderSwan header
- **Features:** Tile-based, sprite system
- **Complexity:** Medium (V30 is 8086-like)

### 🎯 Tier 3 - Future Expansion (Q4 2026+)

#### Atari Lynx

- **CPU:** WDC 65C02 @ 4 MHz
- **Sound:** 4-channel 8-bit DAC
- **Resolution:** 160×102, 4096 colors
- **ROM Format:** Lynx header
- **Features:** Hardware scaling/rotation
- **Complexity:** Medium (65C02 + custom chips)

#### Neo Geo Pocket / Color

- **CPU:** Toshiba TLCS-900H @ 6.144 MHz
- **Sound:** T6W28 PSG, DAC
- **Resolution:** 160×152, 4096 colors
- **ROM Format:** NGP header
- **Features:** Tile-based graphics
- **Complexity:** High (unique architecture)

---

## 🏗️ Architecture Design

### Core Compiler Structure (v2.0)

```
┌─────────────────────────────────────┐
│   Poppy Universal Assembly Parser   │
│  (Target-agnostic syntax tree)      │
└──────────────┬──────────────────────┘
               │
       ┌───────┴────────┐
       │  Semantic      │
       │  Analyzer      │
       │  (Multi-pass)  │
       └───────┬────────┘
               │
    ┌──────────┴───────────┐
    │  Architecture        │
    │  Selector            │
    └──────────┬───────────┘
               │
     ┌─────────┴──────────┐
     │                    │
┌────▼────┐        ┌─────▼─────┐
│ 6502    │   ...  │ 68000     │
│ Backend │        │ Backend   │
└────┬────┘        └─────┬─────┘
     │                   │
┌────▼────────────────────▼─────┐
│   Code Generator              │
│   (Target-specific opcodes)   │
└────┬──────────────────────────┘
     │
┌────▼────────────────────────┐
│  ROM Builder                │
│  (Headers, checksums, etc.) │
└─────────────────────────────┘
```

### Backend Plugin System

Each CPU architecture becomes a **backend plugin**:

```csharp
public interface IArchitectureBackend {
	string Name { get; }
	string[] SupportedSystems { get; }
	IInstructionSet InstructionSet { get; }
	IAddressingModeResolver AddressingModes { get; }
	IRomHeaderBuilder HeaderBuilder { get; }
	IMemoryMapper MemoryMapper { get; }
	
	byte[] AssembleInstruction(Instruction instruction);
	bool ValidateInstruction(Instruction instruction, out string error);
}
```

### New Instruction Sets Required

| Architecture | Base | Extensions | Complexity |
|-------------|------|------------|-----------|
| M68000 | CISC, 32-bit registers | Addressing modes (14) | High |
| Z80 | 8-bit, accumulator | I/O ports, IX/IY | Medium |
| ARM7TDMI | RISC, 32-bit | Thumb mode, barrel shifter | Medium |
| HuC6280 | 6502 base | Block transfer, bit ops | Low |
| V30MZ | 8086 compatible | Segment registers | Medium |
| 65C02 | 6502 enhanced | New opcodes, addressing | Low |

---

## 📚 Documentation Requirements

### Per-System Documentation Needed

For each new system, create:

1. **System Overview** (`docs/systems/{system}/overview.md`)
   - CPU specifications
   - Memory map
   - I/O registers
   - Timing constraints

2. **Instruction Reference** (`docs/systems/{system}/instructions.md`)
   - Complete opcode table
   - Addressing modes
   - Cycle timing
   - Flags affected

3. **ROM Format Spec** (`docs/systems/{system}/rom-format.md`)
   - Header structure
   - Checksum algorithms
   - Cartridge types
   - Region codes

4. **Programming Guide** (`docs/systems/{system}/programming.md`)
   - Hello World example
   - Common patterns
   - Best practices
   - Hardware quirks

5. **Directive Reference** (`docs/systems/{system}/directives.md`)
   - System-specific directives
   - Header configuration
   - Memory mapping
   - Assembler features

---

## 🔬 Research Tasks

### High Priority Research

- [ ] **M68000 Instruction Encoding** - Complex addressing modes, word alignment
- [ ] **GBA ROM Structure** - Multiboot, save types, header requirements
- [ ] **Z80 I/O Instructions** - Port addressing, timing
- [ ] **ARM Thumb Mode** - Mixed ARM/Thumb code generation
- [ ] **Atari 2600 Kernel Tricks** - Racing the beam, common patterns

### Reference Materials Needed

#### CPU Datasheets

- Motorola M68000 Programmer's Reference Manual
- ARM7TDMI Technical Reference Manual
- Zilog Z80 CPU User Manual
- NEC V30MZ Datasheet
- WDC 65C02 Datasheet

#### System Documentation

- Sega Genesis/MD Hardware Manual
- GBA Programming Manual (GBATEK)
- SMS VDP Programmer's Guide
- TG16/PCE Hardware Reference
- Atari 2600 Stella Programmer's Guide

#### Existing Assemblers (for reference)

- **asm68k** - Genesis assembly
- **devkitARM** - GBA development
- **WLA-DX** - Multi-system assembler
- **DASM** - Atari 2600 assembler
- **NESASM** - NES assembler (comparison)

---

## 🧪 Testing Strategy

### Test Requirements per System

1. **Instruction Set Tests** (100% coverage)
   - Every opcode, every addressing mode
   - Edge cases, invalid instructions
   - Cycle timing validation

2. **Integration Tests**
   - Hello World ROM generation
   - ROM loads in emulator
   - Header validates correctly
   - Checksum generation

3. **Cross-Platform Tests**
   - Same source for multiple targets (where applicable)
   - Macro/directive portability
   - Symbol table consistency

### Emulator Testing

Target emulators for validation:

- **Genesis:** Gens, Blastem
- **GBA:** mGBA, VBA-M
- **SMS:** Emulicious, Meka
- **TG16:** Mednafen, Magic Engine
- **A2600:** Stella
- **WSC:** Oswan, Mednafen

---

## 📦 Deliverables

### v2.0 Alpha (Q1 2026)

- ✅ Genesis M68000 backend
- ✅ GBA ARM7 backend
- ✅ Basic ROM generation for both
- ✅ Hello World examples

### v2.0 Beta (Q2 2026)

- ✅ SMS Z80 backend
- ✅ TG16 HuC6280 backend
- ✅ Complete header generation
- ✅ VS Code syntax for all systems
- ✅ Comprehensive test suite

### v2.0 Release (Q2 2026)

- ✅ All Tier 1 systems complete
- ✅ Full documentation
- ✅ Example projects for each system
- ✅ Performance optimization
- ✅ Debugger integration

---

## 🚀 Migration Path

### Breaking Changes from v1.0

Minimal - v2.0 maintains backward compatibility:

- Existing NES/SNES/GB projects compile unchanged
- New `.target` directive optional (auto-detected from file extension)
- Directives namespaced per-system (`.nes.mapper`, `.sms.region`)

### New Features Available to v1.0 Projects

- Improved error messages with context
- Faster compilation (optimized backends)
- Better macro system
- Enhanced VS Code integration

---

## 📊 Success Metrics

- **System Coverage:** 10+ platforms supported
- **Community Adoption:** 500+ projects using Poppy 2.0
- **Performance:** <1s compile time for 128KB ROM
- **Quality:** 95%+ test coverage across all backends
- **Documentation:** Complete guide for each system

---

## 🤝 Community Involvement

### Contribution Areas

- **Backend Development** - Implement new architecture support
- **Documentation** - Write system programming guides
- **Testing** - Validate on real hardware
- **Examples** - Create demo projects
- **Tooling** - Enhance VS Code extension

### Support Channels

- GitHub Issues - Bug reports, feature requests
- Discord Server - Real-time help, discussions
- Wiki - Community-contributed guides
- YouTube - Tutorial videos

---

## 📅 Timeline Summary

| Quarter | Focus | Deliverables |
|---------|-------|-------------|
| Q1 2026 | Genesis + GBA | Alpha release, 2 new systems |
| Q2 2026 | SMS + TG16 | Beta release, 4 systems total |
| Q3 2026 | A2600 + GBC + WS | Tier 2 complete |
| Q4 2026 | Polish + Tier 3 | v2.0 stable, 10+ systems |

---

**Next Steps:** Begin M68000 instruction set implementation and Genesis ROM format research.
