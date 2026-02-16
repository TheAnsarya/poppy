# Atari Lynx Platform Implementation Plan

**Created:** February 16, 2026
**Status:** In Progress

## Overview

This plan documents the implementation of complete Atari Lynx support in Poppy Compiler.

## Current Status

### Already Implemented ✅

1. **InstructionSet65SC02.cs** - 65SC02 instruction encoding
   - All 65C02-specific instructions (BRA, PHX, PHY, PLX, PLY, STZ, TRB, TSB)
   - Zero Page Indirect addressing mode
   - INC A / DEC A accumulator mode
   - Falls back to 6502 instruction set for compatibility

2. **AtariLynxRomBuilder.cs** - ROM generation
   - 64-byte Lynx header generation
   - Magic "LYNX" signature
   - Game name and manufacturer fields
   - Load/start address configuration
   - Page size calculation
   - Rotation flag support

### To Be Implemented 🔄

#### Phase 1: Core Assembly Support

1. **Platform Directive**
   - Add `.platform "lynx"` support
   - Auto-select 65SC02 instruction set
   - Configure memory map defaults

2. **Memory Map Configuration**
   - Define Suzy region ($fc00-$fcff)
   - Define Mikey region ($fd00-$fdff)
   - Boot ROM region ($fe00-$ffff)
   - Work RAM ($0000-$fbff)

3. **Address Validation**
   - Validate code placement
   - Check for reserved regions
   - Warn on Boot ROM conflicts

#### Phase 2: Hardware Register Support

1. **Suzy Registers**
   - Named constants for all Suzy registers
   - Sprite control block macros
   - Math hardware helpers

2. **Mikey Registers**
   - Timer register constants
   - Audio channel registers
   - Palette registers
   - Interrupt registers

3. **Include File**
   - Create `lynx.inc` with all hardware definitions
   - Document each register

#### Phase 3: Advanced Features

1. **Sprite System Helpers**
   - SCB structure macros
   - Sprite data encoding directives
   - Palette setup macros

2. **Audio Support**
   - Timer configuration macros
   - Audio channel setup
   - LFSR tap values

3. **Build Targets**
   - LNX file format
   - LYX file format (headerless)
   - COM file format (encrypted)

## Architecture Reference

### CPU: WDC 65SC02

- **Clock:** 4 MHz (16 MHz master / 4)
- **Registers:** A, X, Y, SP, PC, PS
- **New Instructions:** BRA, PHX, PHY, PLX, PLY, STZ, TRB, TSB, INC A, DEC A
- **New Addressing:** (zp) zero page indirect, (abs,X) indexed indirect

### Memory Map

```
$0000-$00ff   Zero Page
$0100-$01ff   Stack
$0200-$fbff   Work RAM / Program / Display
$fc00-$fcff   Suzy Registers
$fd00-$fdff   Mikey Registers
$fe00-$ffff   Boot ROM (mappable)
```

### Custom Chips

**Mikey ($fd00-$fdff):**
- 8 timers (cascadable)
- 4 audio channels (12-bit LFSR)
- Display DMA
- Interrupt controller
- UART
- Palette (16 colors from 4096)

**Suzy ($fc00-$fcff):**
- Sprite engine (SCB-based)
- Hardware math (16×16 multiply, divide)
- 16-entry collision buffer
- Joystick input

## GitHub Issues

See [GitHub Issues](https://github.com/TheAnsarya/poppy/issues) for tracking.

### Epic: Atari Lynx Platform Support

Sub-issues:
1. [x] 65SC02 instruction encoding
2. [x] Lynx ROM builder
3. [ ] Platform directive support
4. [ ] Memory map configuration
5. [ ] Hardware register include file
6. [ ] Sprite system macros
7. [ ] Audio system macros
8. [ ] Documentation and examples

## External References

- [Atari Lynx Dev](https://www.monlynx.de/lynx/index.html)
- [Lynx Programming Guide](https://atarilynxdeveloper.wordpress.com/)
- [cc65 Lynx Docs](https://cc65.github.io/doc/lynx.html)
- [65C02 Datasheet (WDC)](https://www.westerndesigncenter.com/wdc/documentation/w65c02s.pdf)
- [Nexen Emulator](https://github.com/TheAnsarya/Nexen) - Reference implementation

