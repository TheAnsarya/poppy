# 🎮 Poppy SNES Development Guide

This guide covers everything you need to know to develop Super Nintendo (SNES)
games and homebrew using the Poppy assembler.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [The WDC 65816 Processor](#the-wdc-65816-processor)
3. [Memory Mapping](#memory-mapping)
4. [Register Size Modes](#register-size-modes)
5. [Addressing Modes](#addressing-modes)
6. [SNES Header Configuration](#snes-header-configuration)
7. [Writing Your First SNES Program](#writing-your-first-snes-program)
8. [Advanced Topics](#advanced-topics)
9. [Common Patterns](#common-patterns)
10. [Troubleshooting](#troubleshooting)

---

## 1. Getting Started {#getting-started}

### Prerequisites

- Poppy compiler installed
- A SNES emulator (bsnes, Mesen-S, or snes9x recommended)
- Basic understanding of 6502 assembly (helpful but not required)

### Quick Start

Create a file called `hello.pasm`:

```asm
; hello.pasm - Minimal SNES program
.snes
.lorom

; SNES header
.snes_title "HELLO WORLD"
.snes_region "USA"
.snes_rom_size 256

; Code starts at $8000 in LoROM
.org $8000
reset:
	sei					; Disable interrupts
	clc					; Clear carry
	xce					; Switch to native mode

	rep #$10			; 16-bit index registers
	sep #$20			; 8-bit accumulator
	.a8
	.i16

	; Set up stack
	ldx #$1fff
	txs

infinite:
	wai					; Wait for interrupt
	jmp infinite

; Empty NMI handler
nmi:
	rti

; Empty IRQ handler
irq:
	rti

; Native mode vectors ($ffe0-$ffff)
.org $ffea
	.word nmi			; NMI
.org $fffc
	.word reset			; RESET
	.word irq			; IRQ
```

Compile it:

```bash
poppy -o hello.sfc hello.pasm
```

---

## 2. The WDC 65816 Processor {#the-wdc-65816-processor}

The SNES uses the WDC 65816, an enhanced version of the 6502 with:

- **16-bit accumulator** (switchable to 8-bit)
- **16-bit index registers** (switchable to 8-bit)
- **24-bit address bus** (16MB address space)
- **Stack relocation** (direct page can be anywhere)
- **New instructions** (block moves, 16-bit operations)

### Processor Modes

The 65816 has two main modes:

1. **Emulation Mode** - Behaves like a 6502 (8-bit, 64KB)
2. **Native Mode** - Full 65816 capabilities (16-bit, 16MB)

The SNES starts in emulation mode. To enter native mode:

```asm
clc					; Clear carry
xce					; Exchange carry and emulation flags
```

### Important Flags

- **E (Emulation)** - 1 = emulation mode, 0 = native mode
- **M (Memory/Accumulator)** - 1 = 8-bit A, 0 = 16-bit A
- **X (Index)** - 1 = 8-bit X/Y, 0 = 16-bit X/Y

---

## 3. Memory Mapping {#memory-mapping}

The SNES uses a complex memory map that depends on the ROM mapping mode.

### LoROM

- ROM appears at $8000-$ffff in each bank
- Each bank holds 32KB of ROM
- Most common for games up to 4MB

```asm
.lorom
```

| Bank     | Address         | Contents         |
|----------|-----------------|------------------|
| $00-$3f  | $8000-$ffff     | ROM              |
| $00-$3f  | $0000-$1fff     | WRAM Mirror      |
| $00-$3f  | $2000-$5fff     | Hardware Regs    |
| $7e-$7f  | $0000-$ffff     | Work RAM (128KB) |

### HiROM

- ROM appears at $0000-$ffff in each bank
- Each bank holds 64KB of ROM
- Used for larger games (up to 4MB)

```asm
.hirom
```

| Bank     | Address         | Contents         |
|----------|-----------------|------------------|
| $c0-$ff  | $0000-$ffff     | ROM              |
| $00-$3f  | $0000-$1fff     | WRAM Mirror      |
| $00-$3f  | $2000-$5fff     | Hardware Regs    |
| $7e-$7f  | $0000-$ffff     | Work RAM (128KB) |

### ExHiROM

- Extended HiROM for ROMs larger than 4MB
- Up to 8MB ROM support

```asm
.exhirom
```

---

## 4. Register Size Modes {#register-size-modes}

### M Flag (Accumulator Size)

Controls the size of the accumulator:

```asm
; 8-bit accumulator (M = 1)
sep #$20			; Set M flag
.a8
lda #$ff			; 8-bit load (2 bytes total)

; 16-bit accumulator (M = 0)
rep #$20			; Reset (clear) M flag
.a16
lda #$ffff			; 16-bit load (3 bytes total)
```

### X Flag (Index Register Size)

Controls the size of X and Y registers:

```asm
; 8-bit index (X flag = 1)
sep #$10			; Set X flag
.i8
ldx #$ff			; 8-bit load

; 16-bit index (X flag = 0)
rep #$10			; Reset (clear) X flag
.i16
ldx #$ffff			; 16-bit load
```

### Common Combinations

```asm
; 16-bit A, 16-bit X/Y (all 16-bit)
rep #$30			; Clear both M and X flags
.a16
.i16

; 8-bit A, 8-bit X/Y (all 8-bit, like 6502)
sep #$30			; Set both M and X flags
.a8
.i8

; 16-bit A, 8-bit X/Y (common for math)
rep #$20			; Clear M flag (16-bit A)
sep #$10			; Set X flag (8-bit X/Y)
.a16
.i8

; 8-bit A, 16-bit X/Y (common for indexing)
sep #$20			; Set M flag (8-bit A)
rep #$10			; Clear X flag (16-bit X/Y)
.a8
.i16
```

### ⚠️ Important: Always Use Directives!

Always use `.a8`/`.a16`/`.i8`/`.i16` directives after REP/SEP instructions.
This tells the assembler the current register sizes so it can emit correct
instruction sizes:

```asm
; WRONG - assembler doesn't know the register sizes
rep #$20
lda #$1234			; May emit wrong number of bytes!

; CORRECT - assembler knows A is 16-bit
rep #$20
.a16
lda #$1234			; Correctly emits 3 bytes
```

---

## 5. Addressing Modes {#addressing-modes}

The 65816 supports all 6502 addressing modes plus several new ones.

### Direct Page (Zero Page)

Like 6502 zero page, but relocatable:

```asm
lda $12				; Load from direct page
lda $12,x			; Direct page indexed X
lda $12,y			; Direct page indexed Y
```

### Absolute (16-bit)

```asm
lda $1234			; Absolute
lda $1234,x			; Absolute indexed X
lda $1234,y			; Absolute indexed Y
```

### Absolute Long (24-bit)

Access any address in the 16MB space:

```asm
lda $7e1234			; Absolute long
lda $7e1234,x		; Absolute long indexed X
jml $c08000			; Jump long
jsl subroutine		; Jump to subroutine long
```

### Indirect

```asm
lda ($12)			; Direct page indirect
lda ($12,x)			; Direct page indexed indirect
lda ($12),y			; Direct page indirect indexed
```

### Indirect Long

```asm
lda [$12]			; Direct page indirect long
lda [$12],y			; Direct page indirect long indexed
jml [$0000]			; Jump indirect long
```

### Stack Relative

Access data relative to the stack pointer:

```asm
lda $03,s			; Stack relative
lda ($03,s),y		; Stack relative indirect indexed
```

### Block Move

Move memory blocks efficiently:

```asm
; Move 256 bytes from $7e0000 to $7f0000
lda #$00ff			; Byte count - 1
ldx #$0000			; Source address
ldy #$0000			; Destination address
mvn $7f, $7e		; Move Next (ascending)

; Or descending
mvp $7f, $7e		; Move Previous (descending)
```

### Size Suffix Hints

Force specific operand sizes:

```asm
lda.b #$12			; Force 8-bit immediate
lda.w #$1234		; Force 16-bit immediate
lda.l $123456		; Force 24-bit (long) address
```

---

## 6. SNES Header Configuration {#snes-header-configuration}

### Required Directives

```asm
.snes					; Target SNES
.lorom					; Or .hirom, .exhirom
```

### Header Information

```asm
.snes_title "GAME NAME"		; Up to 21 characters
.snes_region "USA"			; "Japan", "USA", "Europe"
.snes_version 1				; Version (0-255)
.snes_rom_size 256			; ROM size in KB
.snes_ram_size 8			; SRAM size in KB (0 for none)
```

### Optional Settings

```asm
.fastrom					; Enable 3.58 MHz mode
```

### Header Location

The SNES internal header is located at:

- LoROM: $007fc0-$007fff
- HiROM: $00ffc0-$00ffff

Poppy automatically places the header at the correct location.

---

## 7. Writing Your First SNES Program {#writing-your-first-snes-program}

### Complete Example: Color Bars

```asm
; colorbars.pasm - Display color bars on screen
.snes
.lorom

.snes_title "COLOR BARS"
.snes_region "USA"
.snes_rom_size 256

; PPU registers
.define INIDISP		$2100
.define CGADD		$2121
.define CGDATA		$2122
.define SETINI		$2133

; Code origin
.org $8000
reset:
	sei
	clc
	xce					; Native mode

	rep #$10			; 16-bit index
	sep #$20			; 8-bit accumulator
	.a8
	.i16

	; Set up stack
	ldx #$1fff
	txs

	; Force blank
	lda #$80
	sta INIDISP

	; Set background color (blue)
	stz CGADD			; Color 0
	lda #$1f			; Blue component (5 bits)
	sta CGDATA
	lda #$00
	sta CGDATA

	; Disable interlace
	stz SETINI

	; Enable display (full brightness)
	lda #$0f
	sta INIDISP

infinite:
	wai
	jmp infinite

nmi:
	rti

irq:
	rti

; Vectors
.org $ffea
	.word nmi			; NMI
.org $fffc
	.word reset			; RESET
	.word irq			; IRQ
```

---

## 8. Advanced Topics {#advanced-topics}

### Direct Page Relocation

Move the direct page anywhere in bank 0:

```asm
rep #$20
.a16
lda #$0100			; New direct page address
tcd					; Transfer to direct page register
sep #$20
.a8
```

### Data Bank Register

Set the default bank for absolute addressing:

```asm
lda #$7e			; Bank $7e
pha
plb					; Pull to data bank register
```

### Program Bank

The K register holds the program bank. Use PHK to push it:

```asm
phk					; Push program bank
plb					; Use as data bank
```

### 24-bit Pointers

Store and use 24-bit addresses:

```asm
; Store a 24-bit pointer
.org $00
my_pointer:
	.dl sprite_data		; 3-byte pointer

; Use the pointer
lda [my_pointer]		; Indirect long
lda [my_pointer],y		; Indirect long indexed
```

---

## 9. Common Patterns {#common-patterns}

### Wait for VBlank

```asm
.define RDNMI	$4210

wait_vblank:
	lda RDNMI			; Read NMI flag
	bpl wait_vblank		; Loop if not in VBlank
	rts
```

### DMA Transfer

```asm
.define VMADDL	$2116
.define VMADDH	$2117
.define VMDATAL	$2118
.define DMAP0	$4300
.define BBAD0	$4301
.define A1T0L	$4302
.define A1T0H	$4303
.define A1B0	$4304
.define DAS0L	$4305
.define DAS0H	$4306
.define MDMAEN	$420b

; Transfer tilemap to VRAM
dma_to_vram:
	; Set VRAM address
	ldx #$0000
	stx VMADDL

	; Set DMA parameters
	lda #$01			; Word write, increment
	sta DMAP0
	lda #$18			; Destination: $2118 (VMDATAL)
	sta BBAD0

	; Set source address
	ldx #tilemap_data
	stx A1T0L
	lda #^tilemap_data	; Bank
	sta A1B0

	; Set transfer size
	ldx #$0800			; 2KB
	stx DAS0L

	; Execute DMA
	lda #$01			; Enable DMA channel 0
	sta MDMAEN
	rts
```

### Save to SRAM

```asm
.define SRAM_START	$700000

save_game:
	rep #$20
	.a16
	ldx #$0000
save_loop:
	lda game_data,x
	sta SRAM_START,x
	inx
	inx
	cpx #$0100			; 256 bytes
	bne save_loop
	sep #$20
	.a8
	rts
```

---

## 10. Troubleshooting {#troubleshooting}

### Common Issues

#### Wrong Instruction Sizes

**Problem:** Game crashes or behaves oddly.

**Cause:** Missing `.a8`/`.a16`/`.i8`/`.i16` directives.

**Solution:** Always add size directives after REP/SEP:

```asm
rep #$20
.a16			; Don't forget this!
lda #$1234
```

#### ROM Doesn't Boot

**Problem:** Emulator shows black screen.

**Cause:** Missing or incorrect vectors.

**Solution:** Ensure vectors are at correct addresses:

```asm
; LoROM vectors
.org $ffea
	.word nmi
.org $fffc
	.word reset
	.word irq
```

#### Graphics Not Showing

**Problem:** Screen is blank or wrong colors.

**Cause:** Display disabled or force blank enabled.

**Solution:** Enable display after setup:

```asm
lda #$0f		; Full brightness
sta $2100		; INIDISP
```

### Debugging Tips

1. **Use an emulator with debugging** - bsnes and Mesen-S have excellent debuggers
2. **Check register sizes** - Most bugs come from wrong M/X flags
3. **Verify memory mapping** - LoROM and HiROM have different layouts
4. **Watch the vectors** - Ensure RESET, NMI, and IRQ point to valid code

---

## Resources

- [SNES Development Wiki](https://wiki.superfamicom.org/)
- [65816 Reference](https://wiki.superfamicom.org/65816-reference)
- [SNES Memory Map](https://wiki.superfamicom.org/memory-mapping)
- [SNES Registers](https://wiki.superfamicom.org/snes-registers)

---

*Happy SNES coding with Poppy! 🌸*
