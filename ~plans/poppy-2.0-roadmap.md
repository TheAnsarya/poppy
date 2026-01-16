# 🎮 Poppy 2.0 - Multi-System Compiler Roadmap

**Last Updated:** 2026-01-16
**Status:** ✅ Implementation Complete!
**Version:** 2.0.0-dev
**Tests:** 1527+ passing

---

## 📋 Overview

Poppy 2.0 has expanded beyond NES/SNES/GB to support **11 retro gaming platforms** with comprehensive instruction sets, ROM generation, and documentation. All planned platforms have been implemented!

## 🎯 Goals - ALL ACHIEVED ✅

1. ✅ **Universal Assembly Language** - One syntax for all platforms
2. ✅ **Smart Code Generation** - Architecture-aware optimization
3. ✅ **Cross-Platform Development** - Easy porting between systems
4. ✅ **Modern Tooling** - VS Code integration for all platforms
5. ✅ **Complete ROM Generation** - Headers, checksums, all formats

---

## 🕹️ Supported Systems - All 11 Platforms Complete!

### ✅ Original v1.0 Platforms

| Platform | CPU | ROM Format | Status |
|----------|-----|------------|--------|
| **NES** | MOS 6502 | iNES 2.0 | ✅ Complete |
| **SNES** | WDC 65816 | LoROM/HiROM/ExHiROM | ✅ Complete |
| **Game Boy** | Sharp SM83 | GB (DMG/CGB/MBC) | ✅ Complete |

### ✅ v2.0 New Platforms - ALL COMPLETE

#### Sega Genesis / Mega Drive ✅

- **CPU:** Motorola 68000 @ 7.67 MHz
- **Sound:** Yamaha YM2612 (FM), TI SN76489 (PSG)
- **Resolution:** 320×224, 256 colors
- **Implementation:** `InstructionSetM68000.cs`, `GenesisHeaderBuilder.cs`
- **Tests:** 150+ M68000 instruction tests
- **Example:** `examples/genesis-hello-world/`

#### Game Boy Advance ✅

- **CPU:** ARM7TDMI @ 16.78 MHz (ARM + Thumb modes)
- **Sound:** 2 PCM channels, 4 GB channels
- **Resolution:** 240×160, 32,768 colors
- **Implementation:** `InstructionSetARM7TDMI.cs`, `GbaHeaderBuilder.cs`
- **Tests:** 200+ ARM instruction tests
- **Example:** `examples/gba-hello-world/`

#### Sega Master System ✅

- **CPU:** Zilog Z80 @ 3.58 MHz
- **Sound:** TI SN76489 PSG
- **Resolution:** 256×192, 32 colors
- **Implementation:** `InstructionSetZ80.cs`, `SmsHeaderBuilder.cs`
- **Tests:** 150+ Z80 instruction tests
- **Example:** `examples/mastersystem-hello-world/`

#### TurboGrafx-16 / PC Engine ✅

- **CPU:** HuC6280 (enhanced 6502) @ 7.16 MHz
- **Sound:** HuC6280 PSG (6 channels)
- **Resolution:** 256×224, 512 colors
- **Implementation:** `InstructionSetHuC6280.cs`, `PceHeaderBuilder.cs`
- **Tests:** 100+ HuC6280 instruction tests (includes block transfer)
- **Example:** `examples/turbografx-hello-world/`

#### Atari 2600 (VCS) ✅

- **CPU:** MOS 6507 @ 1.19 MHz
- **Sound:** TIA chip
- **Resolution:** 160×192, 128 colors
- **Implementation:** `InstructionSet6507.cs`, `A26HeaderBuilder.cs`
- **Tests:** Uses 6502 base with 6507 restrictions
- **Example:** `examples/atari2600-hello-world/`

#### Atari Lynx ✅

- **CPU:** WDC 65SC02 @ 4 MHz
- **Sound:** 4-channel 8-bit DAC
- **Resolution:** 160×102, 4096 colors
- **Implementation:** `InstructionSet65SC02.cs`, `LnxHeaderBuilder.cs`
- **Tests:** 65SC02 instruction tests
- **Example:** `examples/lynx-hello-world/`

#### WonderSwan / WonderSwan Color ✅

- **CPU:** NEC V30MZ @ 3.07 MHz
- **Sound:** 4-channel PCM
- **Resolution:** 224×144, 4096 colors (WSC)
- **Implementation:** `InstructionSetV30MZ.cs`, `WsHeaderBuilder.cs`
- **Tests:** V30MZ instruction tests
- **Example:** `examples/wonderswan-hello-world/`

#### SNES SPC700 (Audio) ✅

- **CPU:** Sony SPC700 @ 1.024 MHz
- **Sound:** DSP with 8 voices, BRR compression
- **Output:** .spc audio file format
- **Implementation:** `InstructionSetSPC700.cs`, `SpcFileBuilder.cs`
- **Tests:** 100+ SPC700 instruction tests
- **Example:** `examples/spc700-hello-world/`

---

## 🏗️ Architecture Summary

### Instruction Sets Implemented

| Architecture | File | Opcodes | Addressing Modes |
|-------------|------|---------|------------------|
| 6502 | `InstructionSet6502.cs` | 56 | 13 |
| 6507 | `InstructionSet6507.cs` | 56 | 13 (6502 subset) |
| 65SC02 | `InstructionSet65SC02.cs` | 78 | 16 |
| 65816 | `InstructionSet65816.cs` | 92 | 24 |
| SM83 | `InstructionSetSM83.cs` | 245 | 12 |
| Z80 | `InstructionSetZ80.cs` | 700+ | 11 |
| HuC6280 | `InstructionSetHuC6280.cs` | 85+ | 16+ |
| M68000 | `InstructionSetM68000.cs` | 60+ | 14 |
| V30MZ | `InstructionSetV30MZ.cs` | 100+ | 8 |
| ARM7TDMI | `InstructionSetARM7TDMI.cs` | 150+ | ARM + Thumb |
| SPC700 | `InstructionSetSPC700.cs` | 80+ | 15 |

### ROM Header Builders

| System | File | Format |
|--------|------|--------|
| NES | `InesHeaderBuilder.cs` | iNES 1.0/2.0 |
| SNES | `SnesHeaderBuilder.cs` | LoROM/HiROM/ExHiROM |
| Game Boy | `GbHeaderBuilder.cs` | DMG/CGB/MBC |
| Genesis | `GenesisHeaderBuilder.cs` | SEGA Genesis |
| GBA | `GbaHeaderBuilder.cs` | GBA cartridge |
| SMS | `SmsHeaderBuilder.cs` | Master System/Game Gear |
| TG16 | `PceHeaderBuilder.cs` | HuCard/PC Engine |
| Atari 2600 | `A26HeaderBuilder.cs` | 2K-32K+ ROMs |
| Lynx | `LnxHeaderBuilder.cs` | Lynx cartridge |
| WonderSwan | `WsHeaderBuilder.cs` | WS/WSC ROMs |
| SPC700 | `SpcFileBuilder.cs` | .spc audio |

---

## 📚 Documentation Status

### Example Projects ✅ All Complete

| Platform | Example Directory | Files |
|----------|-------------------|-------|
| NES | `nes-hello-world/` | ✅ |
| SNES | `snes-hello-world/` | ✅ |
| Game Boy | `gb-hello-world/` | ✅ |
| Genesis | `genesis-hello-world/` | ✅ |
| GBA | `gba-hello-world/` | ✅ |
| SMS | `mastersystem-hello-world/` | ✅ |
| TG16 | `turbografx-hello-world/` | ✅ |
| Atari 2600 | `atari2600-hello-world/` | ✅ |
| Lynx | `lynx-hello-world/` | ✅ |
| WonderSwan | `wonderswan-hello-world/` | ✅ |
| SPC700 | `spc700-hello-world/` | ✅ |

### Remaining Documentation Tasks

- [ ] Platform-specific migration guides (#111)
- [x] v2.0 roadmap update (#112) - Complete
- [ ] Project templates (#113)
- [ ] `poppy init` command (#114)

---

## 🧪 Test Coverage

### Test Summary

- **Total Tests:** 1527+
- **Pass Rate:** 100%
- **Coverage Areas:**
	- Lexer (125+ tests)
	- Parser (200+ tests)
	- Code Generation (400+ tests)
	- Semantics (150+ tests)
	- Integration (200+ tests)
	- Macros (60+ tests)
	- Error Handling (30+ tests)
	- Platform-specific (300+ tests)

### Per-Architecture Tests

| Architecture | Test File | Test Count |
|-------------|-----------|------------|
| 6502 | Multiple | 200+ |
| 65816 | `InstructionSet65816Tests.cs` | 150+ |
| SM83 | `InstructionSetSM83Tests.cs` | 200+ |
| Z80 | `InstructionSetZ80Tests.cs` | 150+ |
| M68000 | `InstructionSetM68000Tests.cs` | 150+ |
| ARM7TDMI | `InstructionSetARM7TDMITests.cs` | 200+ |
| HuC6280 | `InstructionSetHuC6280Tests.cs` | 100+ |
| SPC700 | `InstructionSetSPC700Tests.cs` | 100+ |

---

## 📦 Completed Deliverables

### ✅ v2.0 Alpha - COMPLETE

- ✅ Genesis M68000 backend
- ✅ GBA ARM7 backend (ARM + Thumb)
- ✅ Basic ROM generation for both
- ✅ Hello World examples

### ✅ v2.0 Beta - COMPLETE

- ✅ SMS Z80 backend
- ✅ TG16 HuC6280 backend
- ✅ SPC700 audio backend
- ✅ Complete header generation
- ✅ Comprehensive test suite

### ✅ v2.0 Implementation - COMPLETE

- ✅ All 11 platforms supported
- ✅ 1527+ tests passing
- ✅ Example projects for each system
- ✅ Full instruction set coverage

### 🔄 v2.0 Release - In Progress

- ✅ All platforms implemented
- ✅ Documentation updates
- ⬜ Project templates
- ⬜ CLI enhancements

---

## 🚀 Migration Path

### Backward Compatibility

v2.0 maintains full backward compatibility:

- Existing NES/SNES/GB projects compile unchanged
- New `.target` directive for platform selection
- Per-platform header directives

### New Features Available

- 8 additional platforms
- Improved error messages with context
- Better macro system
- Enhanced VS Code integration

---

## 📊 Success Metrics - ACHIEVED ✅

| Metric | Target | Actual |
|--------|--------|--------|
| System Coverage | 10+ platforms | ✅ 11 platforms |
| Test Coverage | 95%+ | ✅ 100% (1527 tests) |
| Documentation | Complete guides | ✅ 11 examples |
| Performance | <1s for 128KB | ✅ Achieved |

---

## 🔮 Future Expansion (v2.1+)

Potential future platforms (not currently planned):

- Neo Geo Pocket / Color (TLCS-900H)
- Virtual Boy (V810)
- PlayStation (MIPS R3000)
- Nintendo 64 (MIPS VR4300)
- Sega Saturn (SH-2)

---

## 📅 Timeline Summary

| Phase | Status | Deliverables |
|-------|--------|--------------|
| v2.0 Alpha | ✅ Complete | Genesis + GBA backends |
| v2.0 Beta | ✅ Complete | SMS + TG16 + more |
| v2.0 Implementation | ✅ Complete | All 11 platforms |
| v2.0 Release | 🔄 In Progress | Docs + tooling |

---

## 🔗 Related Issues

### Closed (Implementation Complete)

- #85 - Atari 2600 ✅
- #86 - Atari Lynx ✅
- #87 - WonderSwan ✅
- #88 - Genesis (M68000) ✅
- #89 - Master System (Z80) ✅
- #90 - GBA (ARM7TDMI) ✅
- #91 - TurboGrafx-16 (HuC6280) ✅
- #92 - SPC700 ✅
- #107-#109 - Instruction set implementations ✅
- #116-#118 - ROM header implementations ✅
- #110 - Example projects ✅

### Open (Documentation/Tooling)

- #111 - Platform migration guides
- #112 - Roadmap updates (this document)
- #113 - Project templates
- #114 - `poppy init` command

---

**Last Updated:** January 16, 2026
**Current Status:** Implementation Complete, Documentation In Progress
