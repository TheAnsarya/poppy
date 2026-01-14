# 📚 Resources - Poppy Compiler

> Collection of URLs, documentation, and reference materials for development

**Last Updated:** January 11, 2026

---

## 🎮 Target System Documentation

### 6502 Processor (NES)

| Resource | URL | Description |
|----------|-----|-------------|
| 6502 Instruction Set | <https://www.masswerk.at/6502/6502_instruction_set.html> | Complete instruction reference |
| 6502.org | <http://www.6502.org/> | Community resources and tutorials |
| Easy 6502 | <https://skilldrick.github.io/easy6502/> | Interactive 6502 tutorial |
| NESdev Wiki - 6502 | <https://www.nesdev.org/wiki/CPU> | NES-specific CPU documentation |
| 6502 Opcodes | <http://www.obelisk.me.uk/6502/reference.html> | Detailed opcode reference |

### 65816 Processor (SNES)

| Resource | URL | Description |
|----------|-----|-------------|
| 65816 Reference | <https://www.westerndesigncenter.com/wdc/documentation/w65c816s.pdf> | Official WDC datasheet |
| SNESdev Wiki - 65816 | <https://snes.nesdev.org/wiki/65816_reference> | SNES-specific CPU reference |
| 65816 Primer | <https://wiki.superfamicom.org/65816-reference> | SuperFamicom wiki reference |
| Programming the 65816 | <https://www.westerndesigncenter.com/wdc/documentation/Programmingthe65816.pdf> | WDC programming guide |

### Game Boy (Z80-like)

| Resource | URL | Description |
|----------|-----|-------------|
| Pan Docs | <https://gbdev.io/pandocs/> | Comprehensive GB documentation |
| GB CPU Manual | <https://gbdev.io/gb-opcodes/optables/> | Opcode tables |
| GBDev Wiki | <https://gbdev.gg8.se/wiki/articles/Main_Page> | Community wiki |

---

## 🔧 Reference Assemblers

### ASAR (SNES)

| Resource | URL | Description |
|----------|-----|-------------|
| GitHub Repository | <https://github.com/RPGHacker/asar> | Source code |
| Documentation | <https://rpghacker.github.io/asar/> | Official manual |
| ASAR Manual | <https://github.com/RPGHacker/asar/blob/master/doc/asar_manual.txt> | Text manual |

### XKAS (SNES)

| Resource | URL | Description |
|----------|-----|-------------|
| GitHub Mirror | <https://github.com/hex-usr/xkas> | Source code |
| Documentation | <https://github.com/hex-usr/xkas/blob/master/readme.txt> | Usage guide |

### ca65 (cc65 Suite)

| Resource | URL | Description |
|----------|-----|-------------|
| GitHub Repository | <https://github.com/cc65/cc65> | Source code |
| ca65 Documentation | <https://cc65.github.io/doc/ca65.html> | Assembler manual |
| cc65 Main Site | <https://cc65.github.io/> | Project homepage |

### Ophis (6502)

| Resource | URL | Description |
|----------|-----|-------------|
| GitHub Repository | <https://github.com/michaelcmartin/Ophis> | Source code |
| Documentation | <http://michaelcmartin.github.io/Ophis/> | User manual |

### Other Assemblers

| Assembler | URL | Description |
|-----------|-----|-------------|
| asm6 | <https://github.com/parasyte/asm6> | Simple 6502 assembler |
| NESASM | <https://github.com/camsaul/nesasm> | NES-focused assembler |
| WLA-DX | <https://github.com/vhelin/wla-dx> | Multi-platform assembler |
| 64tass | <https://sourceforge.net/projects/tass64/> | Turbo Assembler compatible |

---

## 📖 NES Development

| Resource | URL | Description |
|----------|-----|-------------|
| NESdev Wiki | <https://www.nesdev.org/wiki/Nesdev_Wiki> | Primary NES dev resource |
| NES Architecture | <https://www.nesdev.org/wiki/NES_reference_guide> | Hardware reference |
| iNES Header | <https://www.nesdev.org/wiki/INES> | ROM header format |
| NES Memory Map | <https://www.nesdev.org/wiki/CPU_memory_map> | Address space layout |
| PPU Reference | <https://www.nesdev.org/wiki/PPU> | Graphics hardware |

---

## 📖 SNES Development

| Resource | URL | Description |
|----------|-----|-------------|
| SNESdev Wiki | <https://snes.nesdev.org/wiki/Main_Page> | Primary SNES dev resource |
| SNES Architecture | <https://snes.nesdev.org/wiki/SNES_reference_guide> | Hardware reference |
| SFC Development | <https://wiki.superfamicom.org/> | SuperFamicom wiki |
| SNES Header | <https://snes.nesdev.org/wiki/ROM_header> | ROM header format |
| Memory Map | <https://snes.nesdev.org/wiki/Memory_map> | Address space layout |

---

## 🛠️ Compiler Design

| Resource | URL | Description |
|----------|-----|-------------|
| Crafting Interpreters | <https://craftinginterpreters.com/> | Compiler/interpreter design |
| Writing an Assembler | <https://www.codeproject.com/Articles/6950/Writing-an-Assembler> | Tutorial article |
| Assembler Design | <https://www.davespace.co.uk/arm/introduction-to-arm/assembler.html> | General concepts |

---

## 📦 File Formats

### NES Formats

| Format | URL | Description |
|--------|-----|-------------|
| iNES | <https://www.nesdev.org/wiki/INES> | Standard NES ROM format |
| NES 2.0 | <https://www.nesdev.org/wiki/NES_2.0> | Extended header format |
| CHR Format | <https://www.nesdev.org/wiki/CHR_ROM> | Graphics data format |

### SNES Formats

| Format | URL | Description |
|--------|-----|-------------|
| SFC/SMC | <https://snes.nesdev.org/wiki/ROM_file_formats> | SNES ROM formats |
| LoROM/HiROM | <https://snes.nesdev.org/wiki/Memory_map> | Memory mapping modes |
| BPS Patch | <https://github.com/blakesmith/rompatcher> | Patch format |

---

## 🎯 Target Projects

| Project | System | Complexity | Notes |
|---------|--------|------------|-------|
| Dragon Warrior 1 | NES | Simple | First target |
| Final Fantasy Mystic Quest | SNES | Simple | First SNES target |
| Dragon Warrior 4 | NES | Complex | Multi-bank |
| Dragon Quest 3 Remake | SNES | Complex | Large project |

---

## � Syntax Research Summary

> Key patterns discovered from reference assembler analysis (January 11, 2026)

### ASAR Syntax Highlights

| Feature | ASAR Syntax | Notes |
|---------|-------------|-------|
| Hex literals | `$ff`, `$0a40` | Dollar prefix, lowercase |
| Binary literals | `%01111111` | Percent prefix |
| Labels | `Label:`, `.SubLabel:` | Colon optional for sub labels |
| +/- Labels | `+`, `-`, `++`, `--` | Anonymous relative labels |
| Defines | `!define = value` | Bang prefix, spaces around `=` |
| Macros | `macro name(args)...endmacro` | Call with `%name(args)` |
| Include | `incsrc "file.asm"` | Source include |
| Binary include | `incbin "file.bin"` | Binary data include |
| Data | `db`, `dw`, `dl`, `dd` | 8/16/24/32-bit |
| Comments | `; comment` | Semicolon |
| Conditionals | `if/elseif/else/endif` | No dot prefix |
| Loops | `while`, `for`, `rep` | Iteration constructs |
| Org | `org $8000` | Set program counter |
| Mapping | `lorom`, `hirom` | SNES memory mapping |
| Freespace | `freecode`, `freedata` | Auto-find free space |
| Arch switch | `arch 65816`, `arch spc700` | CPU mode selection |
| Length spec | `lda.b #0`, `lda.w #0` | Explicit operand size |

### ca65 Syntax Highlights

| Feature | ca65 Syntax | Notes |
|---------|-------------|-------|
| Hex literals | `$ff`, `$0A40` | Dollar prefix |
| Binary literals | `%01111111` | Percent prefix |
| Labels | `Label:` | Colon required |
| Local labels | `@loop:` | At-sign prefix |
| Unnamed labels | `:` | Reference with `:+`, `:-` |
| Constants | `name = value` | Simple assignment |
| Macros | `.macro name...endmacro` | Dot prefix |
| Include | `.include "file"` | Dot prefix directive |
| Binary include | `.incbin "file"` | Dot prefix directive |
| Data | `.byte`, `.word`, `.dword` | Dot prefix |
| Comments | `; comment` | Semicolon |
| Conditionals | `.if/.elseif/.else/.endif` | Dot prefix |
| Repeat | `.repeat count` | Iteration |
| Org | `.org address` | Set program counter |
| Segments | `.segment "NAME"` | Relocatable sections |
| CPU mode | `.P02`, `.P816` | Processor selection |
| Structs | `.struct/.endstruct` | Data structures |
| Scopes | `.scope/.endscope`, `.proc/.endproc` | Lexical scoping |

### Key Differences & Design Considerations

| Aspect | ASAR | ca65 | Poppy Consideration |
|--------|------|------|---------------------|
| Directive prefix | None (keywords) | Dot prefix | TBD - leaning toward dot prefix |
| Define prefix | `!` | None (`.define`) | TBD |
| Macro call | `%macro()` | `macro` (like mnemonic) | TBD |
| Local labels | `.sub` | `@local` | Support both? |
| Relocatable | No (absolute) | Yes (segments) | Support both modes |
| Freespace | Built-in | N/A | Useful for ROM hacking |

### Common Patterns Across Assemblers

1. **Hex notation**: Universal `$` prefix for hex values
2. **Comments**: Universal `;` for line comments
3. **Labels**: Name followed by colon
4. **Basic data**: Some form of byte/word/long directives
5. **Includes**: Both source and binary file inclusion
6. **Conditionals**: if/else/endif structure
7. **Macros**: Define once, call multiple times

---

## �️ 65816 Instruction Set Summary

> Research completed January 11, 2026 from SuperFamicom Wiki

### Internal Registers

| Register | Name | Description |
|----------|------|-------------|
| A | Accumulator | Math register, 8 or 16-bit |
| X, Y | Index | Index/counter registers, 8 or 16-bit |
| S | Stack Pointer | Points to next available stack location |
| DBR / DB | Data Bank | Default bank for memory transfers |
| D / DP | Direct Page | Direct page addressing base |
| PB / PBR | Program Bank | Bank address of instruction fetches |
| P | Processor Status | Flags and processor states |
| PC | Program Counter | Current instruction address |

### Processor Flags (P Register)

| Flag | Bit | Value | Description |
|------|-----|-------|-------------|
| N | 7 | `$80` | Negative |
| V | 6 | `$40` | Overflow |
| M | 5 | `$20` | Accumulator size (0=16-bit, 1=8-bit) - native mode only |
| X | 4 | `$10` | Index size (0=16-bit, 1=8-bit) - native mode only |
| D | 3 | `$08` | Decimal mode |
| I | 2 | `$04` | IRQ disable |
| Z | 1 | `$02` | Zero |
| C | 0 | `$01` | Carry |
| E | - | - | 6502 emulation mode (hidden, accessed via XCE) |
| B | 4 | `$10` | Break (emulation mode only, shares bit with X) |

### 65816 Addressing Modes (~24 modes)

| Mode | Syntax | Description |
|------|--------|-------------|
| Implied | `pha`, `nop` | No operand |
| Accumulator | `inc a`, `asl a` | Operates on A register |
| Immediate (8-bit) | `sep #$20` | Always 8-bit constant |
| Immediate (mem) | `lda #$ff` | 8 or 16-bit based on M flag |
| Immediate (index) | `ldx #$ff` | 8 or 16-bit based on X flag |
| Direct Page | `lda $10` | Zero/direct page address |
| Direct Page,X | `lda $10,x` | DP indexed by X |
| Direct Page,Y | `lda $10,y` | DP indexed by Y |
| Absolute | `lda $1000` | 16-bit address in current bank |
| Absolute,X | `lda $1000,x` | Absolute indexed by X |
| Absolute,Y | `lda $1000,y` | Absolute indexed by Y |
| Absolute Long | `lda $7e1000` | 24-bit full address |
| Absolute Long,X | `lda $7e1000,x` | Long indexed by X |
| DP Indirect | `lda ($10)` | Indirect through DP |
| DP Indirect,Y | `lda ($10),y` | Indirect indexed |
| DP Indexed Indirect | `lda ($10,x)` | Indexed indirect |
| DP Indirect Long | `lda [$10]` | Long indirect (24-bit pointer) |
| DP Indirect Long,Y | `lda [$10],y` | Long indirect indexed |
| Absolute Indirect | `jmp ($1000)` | Jump indirect |
| Absolute Indirect Long | `jml [$1000]` | Jump indirect long |
| Absolute Indexed Indirect | `jmp ($1000,x)` | Jump indexed indirect |
| Stack Relative | `lda $05,s` | Stack-relative addressing |
| Stack Relative Indirect Indexed | `lda ($05,s),y` | Complex stack mode |
| Relative | `bne label` | 8-bit signed offset |
| Relative Long | `brl label` | 16-bit signed offset |
| Block Move | `mvn $7e,$7f` | Block memory transfer |

### 65816 Instruction Categories

| Category | Instructions | Count |
|----------|-------------|-------|
| Load/Store | `lda`, `ldx`, `ldy`, `sta`, `stx`, `sty`, `stz` | 7 |
| Transfer | `tax`, `tay`, `txa`, `tya`, `txy`, `tyx`, `tcd`, `tcs`, `tdc`, `tsc`, `tsx`, `txs` | 12 |
| Stack | `pha`, `phx`, `phy`, `php`, `phb`, `phd`, `phk`, `pla`, `plx`, `ply`, `plp`, `plb`, `pld`, `pea`, `pei`, `per` | 16 |
| Arithmetic | `adc`, `sbc`, `inc`, `inx`, `iny`, `dec`, `dex`, `dey` | 8 |
| Logic | `and`, `ora`, `eor`, `bit`, `trb`, `tsb` | 6 |
| Shift/Rotate | `asl`, `lsr`, `rol`, `ror` | 4 |
| Compare | `cmp`, `cpx`, `cpy` | 3 |
| Branch | `bcc`/`blt`, `bcs`/`bge`, `beq`, `bne`, `bmi`, `bpl`, `bvc`, `bvs`, `bra`, `brl` | 10 |
| Jump | `jmp`, `jml`, `jsr`, `jsl`, `rts`, `rtl`, `rti` | 7 |
| Flag | `clc`, `cld`, `cli`, `clv`, `sec`, `sed`, `sei`, `rep`, `sep` | 9 |
| Block Move | `mvn`, `mvp` | 2 |
| Misc | `nop`, `wai`, `stp`, `wdm`, `xba`, `xce`, `brk`, `cop` | 8 |
| **Total** | | **~92** |

### 65816 vs 6502 Key Differences

| Feature | 6502 | 65816 |
|---------|------|-------|
| Accumulator | 8-bit only | 8 or 16-bit (M flag) |
| Index registers | 8-bit only | 8 or 16-bit (X flag) |
| Address space | 64KB | 16MB (24-bit) |
| Stack | 256 bytes (page 1) | 64KB (full 16-bit pointer) |
| Direct/Zero page | Fixed at $0000 | Relocatable (D register) |
| Data bank | N/A | DBR for default bank |
| Emulation mode | N/A | Can emulate 6502 |
| New instructions | N/A | 30+ new instructions |

### 65816 Mode Switching

```asm
; Enter native mode (16-bit capable)
clc
xce

; Set 16-bit accumulator and index
rep #$30		; clear M and X flags

; Set 8-bit accumulator, 16-bit index
sep #$20		; set M flag
rep #$10		; clear X flag

; Return to emulation mode
sec
xce
```

---

## 🎮 Game Boy CPU (SM83) Summary

> Research completed January 11, 2026 from Pan Docs and gbdev.io

### Note on CPU Name

The Game Boy CPU is often incorrectly called "Z80" but is actually a custom **Sharp SM83** (or LR35902). It's closer to an Intel 8080 than a Z80, lacking the Z80's IX/IY registers and extended instruction set.

### Registers

| Register | Size | Description |
|----------|------|-------------|
| A | 8-bit | Accumulator |
| F | 8-bit | Flags register |
| B, C | 8-bit | General purpose, can pair as BC (16-bit) |
| D, E | 8-bit | General purpose, can pair as DE (16-bit) |
| H, L | 8-bit | General purpose, can pair as HL (16-bit) |
| SP | 16-bit | Stack Pointer |
| PC | 16-bit | Program Counter |

### Register Pairs

| Pair | Usage |
|------|-------|
| AF | Accumulator + Flags (for push/pop) |
| BC | General purpose 16-bit |
| DE | General purpose 16-bit |
| HL | General purpose, memory pointer |

### Flags (F Register, bits 7-4)

| Bit | Flag | Name | Description |
|-----|------|------|-------------|
| 7 | Z | Zero | Set if result is zero |
| 6 | N | Subtract | Set if last operation was subtraction (BCD) |
| 5 | H | Half Carry | Carry from bit 3 to bit 4 (BCD) |
| 4 | C | Carry | Carry/borrow from bit 7 |
| 3-0 | - | Unused | Always 0 |

### Addressing Modes

| Mode | Syntax | Example |
|------|--------|---------|
| Implied | `nop`, `halt` | No operand |
| Register | `inc b`, `dec a` | Single register |
| Register to Register | `ld b, c` | Reg-to-reg transfer |
| Immediate 8-bit | `ld b, $ff` | Load constant |
| Immediate 16-bit | `ld bc, $1234` | Load 16-bit constant |
| Register Indirect | `ld a, [hl]` | Memory via HL |
| Register Indirect (BC/DE) | `ld a, [bc]` | Memory via BC/DE |
| HL Increment/Decrement | `ld a, [hl+]` | Auto inc/dec HL |
| Direct 16-bit | `ld a, [$ff44]` | Absolute address |
| High RAM | `ldh a, [$ff00+n]` | $ff00-$ffff shortcut |
| High RAM (C) | `ldh a, [c]` | $ff00+C |
| Relative | `jr label` | Signed 8-bit offset |
| SP Relative | `ld hl, sp+n` | Stack pointer offset |

### Instruction Categories

| Category | Instructions | Notes |
|----------|-------------|-------|
| **Load 8-bit** | `ld`, `ldh` | Register/memory loads |
| **Load 16-bit** | `ld`, `push`, `pop` | 16-bit register ops |
| **Arithmetic 8-bit** | `add`, `adc`, `sub`, `sbc`, `inc`, `dec` | A register math |
| **Arithmetic 16-bit** | `add hl`, `inc`, `dec`, `add sp` | 16-bit math |
| **Logic** | `and`, `or`, `xor`, `cp` | Bitwise operations |
| **Rotate/Shift** | `rlca`, `rrca`, `rla`, `rra`, `rlc`, `rrc`, `rl`, `rr`, `sla`, `sra`, `srl`, `swap` | Bit manipulation |
| **Bit Test** | `bit`, `set`, `res` | Single bit operations |
| **Jump** | `jp`, `jr`, `call`, `ret`, `reti`, `rst` | Control flow |
| **Stack** | `push`, `pop` | Stack operations |
| **Misc** | `nop`, `halt`, `stop`, `di`, `ei`, `daa`, `cpl`, `scf`, `ccf` | System control |

### CB-Prefixed Instructions

The `$cb` prefix enables extended bit operations:

| Instruction | Description |
|-------------|-------------|
| `rlc r8` | Rotate left circular |
| `rrc r8` | Rotate right circular |
| `rl r8` | Rotate left through carry |
| `rr r8` | Rotate right through carry |
| `sla r8` | Shift left arithmetic |
| `sra r8` | Shift right arithmetic |
| `swap r8` | Swap nibbles |
| `srl r8` | Shift right logical |
| `bit n, r8` | Test bit n |
| `res n, r8` | Reset bit n |
| `set n, r8` | Set bit n |

### SM83 vs Z80 Key Differences

| Feature | Z80 | SM83 (Game Boy) |
|---------|-----|-----------------|
| IX, IY registers | Yes | No |
| Shadow registers | Yes | No |
| I, R registers | Yes | No |
| Extended instructions (ED prefix) | Yes | No |
| Block transfer (LDIR, etc.) | Yes | No |
| Index addressing | Yes | No |
| IN/OUT instructions | Yes | No (use memory-mapped I/O) |
| `swap` instruction | No | Yes (unique to SM83) |

### Memory Map Context

| Range | Description |
|-------|-------------|
| `$0000-$3fff` | ROM Bank 0 (fixed) |
| `$4000-$7fff` | ROM Bank N (switchable) |
| `$8000-$9fff` | VRAM |
| `$a000-$bfff` | External RAM |
| `$c000-$dfff` | Work RAM |
| `$fe00-$fe9f` | OAM (sprite data) |
| `$ff00-$ff7f` | I/O Registers |
| `$ff80-$fffe` | High RAM (fast) |
| `$ffff` | Interrupt Enable |

---

## 📝 Notes

- Resources are organized by category
- URLs should be verified before use
- Add new resources as discovered during development
- Mark broken links with ❌
- Syntax research from Jan 11, 2026 - ASAR and ca65 analysis complete
- 65816 research from Jan 11, 2026 - instruction set documented
- Game Boy SM83 research from Jan 11, 2026 - registers, flags, instructions documented

---

