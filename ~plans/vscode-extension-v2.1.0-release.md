# 🌸 Poppy VS Code Extension v2.1.0 Release Plan

## Overview

The Poppy Assembly VS Code extension is at v2.0.0 with excellent syntax highlighting coverage across all 11 platforms, but IntelliSense (completions + hover) only covers 3 of 11 platforms (NES, SNES, GB). This release brings completions and hover up to full multi-platform parity, fixes stale artifacts, modernizes tooling, and addresses grammar gaps.

## Current State Assessment

| Feature | Coverage | Status |
|---------|----------|--------|
| TextMate Grammar (syntax highlighting) | 11/11 platforms | ✅ Excellent |
| Snippets (project templates) | 11/11 platforms | ✅ Excellent |
| Completions (opcodes) | 3/11 platforms | ❌ NES, SNES, GB only |
| Completions (registers) | 2/11 platforms | ❌ 6502, SM83 only |
| Hover (instruction docs) | 2/11 platforms | ❌ 6502/65816 only |
| Target detection | 3/11 platforms | ❌ NES, SNES, GB only |
| Diagnostics | Platform-agnostic | ✅ Complete |
| Symbol provider | Platform-agnostic | ✅ Complete |
| Formatting | Platform-agnostic | ✅ Complete |
| Task provider | Platform-agnostic | ✅ Complete |

## Release Goals

### v2.1.0 Scope

1. **Multi-platform completion parity** — Add opcodes for all 8 missing platforms
2. **Multi-platform hover parity** — Add instruction documentation for all missing platforms
3. **Multi-platform register completion** — Add registers for all platforms
4. **Target detection** — Detect all 11 platform targets in source files
5. **Grammar fixes** — Add missing directives (`.freespace`, `.freedata`, built-in functions, `.equ`)
6. **File extension updates** — Register `.asm` and `.src` as pasm language (done)
7. **Editor defaults** — Tab size 4, no spaces for pasm language (done)
8. **Tooling modernization** — Upgrade ESLint, TypeScript-ESLint, cleanup dual lock files
9. **Documentation updates** — Status, guide, README, CHANGELOG
10. **Test expansion** — Add tests for hover, symbol, diagnostics providers

## Platform Completion Data Needed

### Opcodes by Architecture

| Architecture | Platform(s) | Opcode Count (approx) | Source Reference |
|-------------|-------------|----------------------|-----------------|
| M68000 | Genesis | ~70 | `InstructionSetM68000.cs` |
| Z80 | SMS/GG | ~80 | (standard Z80 set) |
| ARM7TDMI | GBA | ~40 (ARM) + ~20 (Thumb) | `InstructionSetARM7TDMI.cs` |
| HuC6280 | PCE/TG16 | 6502 + ~20 extensions | `InstructionSetHuC6280.cs` |
| V30MZ | WonderSwan | ~50 (x86 subset) | `InstructionSetV30MZ.cs` |
| SPC700 | SNES APU | ~50 | (standard SPC700 set) |
| 6507/65SC02 | Atari 2600/Lynx | 6502 + ~10 extensions | `InstructionSet65SC02.cs` |

### Registers by Architecture

| Architecture | Registers |
|-------------|-----------|
| 6502/65SC02/6507 | A, X, Y (already done) |
| 65816 | A, X, Y, S, DB, PB, D |
| SM83 | A, B, C, D, E, H, L, AF, BC, DE, HL, SP, PC (already done) |
| M68000 | D0-D7, A0-A7, SR, CCR, USP, SSP, PC |
| Z80 | A, B, C, D, E, H, L, F, AF, BC, DE, HL, IX, IY, SP, PC, I, R, AF' |
| ARM7TDMI | R0-R15, SP(R13), LR(R14), PC(R15), CPSR, SPSR |
| HuC6280 | A, X, Y (same as 6502) |
| V30MZ | AX, BX, CX, DX, SI, DI, SP, BP, AL, AH, BL, BH, CL, CH, DL, DH, CS, DS, ES, SS |
| SPC700 | A, X, Y, SP, PSW, YA |

## Cleanup Tasks

1. Delete `package-lock.json` (use only `yarn.lock`)
2. Delete or gitignore `poppy-assembly-1.0.0.vsix` (stale artifact)
3. Update `EXTENSION-GUIDE.md` from v1.0.0 to v2.1.0
4. Update `STATUS.md` with current coverage
5. Update `README.md` install to use `yarn` not `npm/npx`

## DevDependency Upgrades

| Package | Current | Target |
|---------|---------|--------|
| `eslint` | `^8.0.0` | `^9.0.0` |
| `@typescript-eslint/eslint-plugin` | `^6.0.0` | `^8.0.0` |
| `@typescript-eslint/parser` | `^6.0.0` | `^8.0.0` |
| `@types/node` | `^20.0.0` | `^22.0.0` |
| `@types/glob` | `^8.1.0` | Remove (use `vscode.workspace.findFiles`) |
| `mocha` | `^10.0.0` | `^11.0.0` |

## Issue Breakdown

### Epic: Poppy VS Code Extension v2.1.0 Release

1. **Expand completion provider to all platforms** — Add M68000, Z80, ARM7TDMI, HuC6280, V30MZ, SPC700, 65SC02/6507 opcodes and registers
2. **Expand hover provider to all platforms** — Add instruction docs for all architectures
3. **Fix target detection** — Detect all 11 platform targets from `.target` directives
4. **Grammar: add missing directives** — `.freespace`, `.freedata`, `.sizeof()`, `.bankof()`, `.strlen()`, `.defined()`, `.equ`
5. **Cleanup stale artifacts** — Remove package-lock.json, stale .vsix, fix npm→yarn references
6. **Upgrade devDependencies** — ESLint 9, TypeScript-ESLint 8, etc.
7. **Update documentation** — STATUS.md, EXTENSION-GUIDE.md, CHANGELOG.md, README.md
8. **Expand test suite** — Tests for hover, diagnostics, symbol providers
9. **Publish v2.1.0** — Package and publish to VS Code Marketplace

## Success Criteria

- All 11 platforms have opcode completions
- All 11 platforms have register completions
- All 11 platforms have hover documentation
- Target detection works for all `.target` aliases
- Grammar highlights all Poppy directives
- All existing + new tests pass
- `yarn package` produces clean `.vsix`
- README accurately describes features
- CHANGELOG documents all changes
