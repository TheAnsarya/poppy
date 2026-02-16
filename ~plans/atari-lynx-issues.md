# Atari Lynx Platform - GitHub Issues

**Created:** February 16, 2026

## Epic Issue

### [Epic] Atari Lynx Platform Support

Complete Atari Lynx (65SC02) platform support for Poppy Compiler.

**Labels:** `epic`, `platform`, `lynx`, `65sc02`

**Description:**
Implement full Atari Lynx platform support including:
- 65SC02 instruction set (✅ complete)
- Lynx ROM builder (✅ complete)
- Platform directive and configuration
- Hardware register definitions
- Sprite and audio system macros
- Documentation and examples

---

## Sub-Issues

### Issue 1: Platform Directive Support

**Title:** `[Lynx] Add .platform "lynx" directive support`

**Labels:** `enhancement`, `lynx`, `parser`

**Description:**
Add support for the `.platform "lynx"` directive to automatically configure:
- 65SC02 instruction set
- Lynx memory map defaults
- Default entry point at $0200
- ROM builder selection

**Acceptance Criteria:**
- [ ] Parser recognizes `lynx` platform
- [ ] Instruction set switches to 65SC02
- [ ] Memory map is configured
- [ ] Tests added

---

### Issue 2: Memory Map Configuration

**Title:** `[Lynx] Configure Lynx memory map segments`

**Labels:** `enhancement`, `lynx`, `codegen`

**Description:**
Define memory segments for Lynx:
- Zero Page: $0000-$00ff
- Stack: $0100-$01ff
- Work RAM: $0200-$fbff
- Suzy: $fc00-$fcff (I/O)
- Mikey: $fd00-$fdff (I/O)
- Boot ROM: $fe00-$ffff

**Acceptance Criteria:**
- [ ] Memory segments defined in CodeGenerator
- [ ] Address validation for ROM placement
- [ ] Warnings for boot ROM conflicts
- [ ] Tests added

---

### Issue 3: Hardware Register Include File

**Title:** `[Lynx] Create lynx.inc hardware definitions`

**Labels:** `enhancement`, `lynx`, `includes`

**Description:**
Create standard include file with:
- All Suzy register constants ($fc00-$fcff)
- All Mikey register constants ($fd00-$fdff)
- Timer registers
- Audio channel registers
- Palette registers
- Joystick/switch registers
- Documented constants

**Acceptance Criteria:**
- [ ] `includes/lynx.inc` created
- [ ] All registers documented
- [ ] Constants use lowercase hex
- [ ] Tests verify inclusion

---

### Issue 4: Sprite System Macros

**Title:** `[Lynx] Implement sprite system macros`

**Labels:** `enhancement`, `lynx`, `macros`

**Description:**
Create macros for Lynx sprite handling:
- SCB structure definition macro
- Sprite data encoding
- Sprite initialization helpers
- Collision setup

**Acceptance Criteria:**
- [ ] SCB macro generates correct structure
- [ ] Sprite data encoding works
- [ ] Examples in documentation
- [ ] Tests added

---

### Issue 5: Audio System Macros

**Title:** `[Lynx] Implement audio system macros`

**Labels:** `enhancement`, `lynx`, `macros`

**Description:**
Create macros for Lynx audio:
- Timer configuration for audio
- Channel initialization
- LFSR tap configuration
- Stereo setup

**Acceptance Criteria:**
- [ ] Timer macros for audio frequencies
- [ ] Channel setup helpers
- [ ] Examples in documentation
- [ ] Tests added

---

### Issue 6: Documentation and Examples

**Title:** `[Lynx] Complete Lynx documentation and examples`

**Labels:** `documentation`, `lynx`

**Description:**
Create comprehensive documentation:
- Lynx assembly guide (✅ created)
- Example programs
- Memory map reference
- Register reference
- Sprite system tutorial
- Audio system tutorial

**Acceptance Criteria:**
- [ ] Guide linked from README
- [x] atari-lynx-guide.md created
- [ ] Example programs compile
- [ ] Hardware register reference

---

## Priority Order

1. **Issue 3** - Include file (foundation for other work)
2. **Issue 1** - Platform directive
3. **Issue 2** - Memory map
4. **Issue 4** - Sprite macros
5. **Issue 5** - Audio macros
6. **Issue 6** - Documentation completion

