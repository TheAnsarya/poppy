# Atari Lynx Assembly Guide

> Complete reference for programming the Atari Lynx with Poppy

## Overview

The Atari Lynx is the world's first color handheld gaming system, released in 1989. It features:

- **WDC 65SC02 CPU** @ 4 MHz
- **160×102 pixel display** with 16 colors from a 4096 color palette
- **64KB RAM** for work memory and display
- **Mikey** custom chip - audio, timers, display, interrupts
- **Suzy** custom chip - sprite engine, hardware math, collision detection

## CPU Architecture

### 65SC02 Processor

The Lynx uses a WDC 65SC02, an enhanced CMOS version of the 6502 with:

- All original 6502 instructions
- New instructions: `bra`, `phx`, `phy`, `plx`, `ply`, `stz`, `trb`, `tsb`
- New addressing mode: Zero Page Indirect `(zp)` 
- Fixed JMP indirect bug (page boundary crossing)
- Decimal mode sets flags correctly

### Registers

| Register | Size | Description |
|----------|------|-------------|
| A | 8-bit | Accumulator |
| X | 8-bit | Index X |
| Y | 8-bit | Index Y |
| SP | 8-bit | Stack Pointer |
| PC | 16-bit | Program Counter |
| P | 8-bit | Processor Status (flags) |

### Processor Flags

| Bit | Flag | Name |
|-----|------|------|
| 7 | N | Negative |
| 6 | V | Overflow |
| 5 | - | Reserved (always 1) |
| 4 | B | Break |
| 3 | D | Decimal |
| 2 | I | IRQ Disable |
| 1 | Z | Zero |
| 0 | C | Carry |

## Memory Map

```
$0000-$00ff   Zero Page (CPU direct page)
$0100-$01ff   Stack
$0200-$fbff   Work RAM / Program / Display Buffer
$fc00-$fcff   Suzy Registers
$fd00-$fdff   Mikey Registers
$fe00-$ffff   Boot ROM (can be disabled)
```

### Suzy Registers ($fc00-$fcff)

| Address | Width | Name | Description |
|---------|-------|------|-------------|
| $fc00-$fc0f | 1 | PENIDX0-F | Pen index color remap |
| $fc10-$fc11 | 2 | TMPADRH/L | Temporary address |
| $fc12-$fc13 | 2 | TILTACUMH/L | Tilt accumulator |
| $fc14-$fc15 | 2 | HOFFL/H | Horizontal offset |
| $fc16-$fc17 | 2 | VOFFL/H | Vertical offset |
| $fc18-$fc19 | 2 | VIDBASH/L | Video buffer base |
| $fc1a-$fc1b | 2 | COLLBASH/L | Collision buffer base |
| $fc1c-$fc1d | 2 | VIDADRH/L | Video address |
| $fc1e-$fc1f | 2 | COLLADRH/L | Collision address |
| $fc20-$fc21 | 2 | SCBNEXTH/L | Next SCB address |
| $fc22-$fc23 | 2 | SPRDLINEH/L | Sprite data line |
| $fc24-$fc25 | 2 | HPOSSTRTL/H | H position start |
| $fc26-$fc27 | 2 | VPOSSTRTL/H | V position start |
| $fc28-$fc29 | 2 | SPRINITR/L | Sprite init bits |
| $fc2a-$fc2b | 2 | HSIZOFFL/H | H size offset |
| $fc2c-$fc2d | 2 | VSIZOFFL/H | V size offset |
| $fc2e-$fc2f | 2 | SCBADRL/H | Current SCB address |
| $fc30-$fc31 | 2 | PROCADRL/H | Process address |
| $fc52 | 1 | MATHD | Math operand D |
| $fc53 | 1 | MATHC | Math operand C |
| $fc54 | 1 | MATHB | Math operand B |
| $fc55 | 1 | MATHA | Math operand A |
| $fc60 | 1 | MATHK | Math result K (low) |
| $fc6c | 1 | MATHM | Math accumulator M (high) |
| $fc80 | 1 | SPRCTL0 | Sprite control 0 |
| $fc81 | 1 | SPRCTL1 | Sprite control 1 |
| $fc82 | 1 | SPRCOLL | Sprite collision number |
| $fc83 | 1 | SPRINIT | Sprite init (reload SCB) |
| $fc90 | 1 | SUZYHREV | Suzy hardware revision |
| $fc91 | 1 | SUZYSREV | Suzy software revision |
| $fc92 | 1 | SPRSYS | Sprite system control |
| $fca0 | 1 | JOYSTICK | Joystick input |
| $fcb0 | 1 | SWITCHES | System switches |

### Mikey Registers ($fd00-$fdff)

#### Timers ($fd00-$fd1f)

Each timer has 4 registers at offsets 0-3:

| Offset | Name | Description |
|--------|------|-------------|
| 0 | BACKUP | Reload value |
| 1 | CTLA | Control A (clock source, enable) |
| 2 | COUNT | Current count |
| 3 | CTLB | Control B / Status |

Timer addresses:
- Timer 0: $fd00-$fd03 (Horizontal sync)
- Timer 1: $fd04-$fd07 (Audio channel 0)
- Timer 2: $fd08-$fd0b (Vertical sync)
- Timer 3: $fd0c-$fd0f (Audio channel 1)
- Timer 4: $fd10-$fd13 (Audio channel 2)
- Timer 5: $fd14-$fd17 (Audio channel 3)
- Timer 6: $fd18-$fd1b (Serial)
- Timer 7: $fd1c-$fd1f (Linked timer)

#### Audio ($fd20-$fd3f)

| Address | Name | Description |
|---------|------|-------------|
| $fd20-$fd27 | AUDIO0 | Audio channel 0 registers |
| $fd28-$fd2f | AUDIO1 | Audio channel 1 registers |
| $fd30-$fd37 | AUDIO2 | Audio channel 2 registers |
| $fd38-$fd3f | AUDIO3 | Audio channel 3 registers |
| $fd40 | STEREO | Stereo control |
| $fd50 | ATTEN0-F | Attenuation registers |

#### Display & Misc

| Address | Name | Description |
|---------|------|-------------|
| $fd80 | INTRST | Interrupt reset |
| $fd81 | INTSET | Interrupt status |
| $fd84-$fd85 | MAGRDY0-1 | Magic / Ready |
| $fd86-$fd87 | AUDIN | Audio in |
| $fd88 | IODIR | I/O direction |
| $fd89 | IODATA | I/O data |
| $fd8a | SERCTL | Serial control |
| $fd8b | SERDAT | Serial data |
| $fd8c-$fd8f | PALETTP | Palette pointer |
| $fd90 | SDONEACK | Sprite done ACK |
| $fd91 | CPUSLEEP | CPU sleep (WAI) |
| $fd92 | DISPCTL | Display control |
| $fd93 | PBKUP | Param backup |
| $fd94 | DISPADR | Display address |
| $fda0-$fdaf | GREEN | Palette green (16 entries) |
| $fdb0-$fdbf | BLUERED | Palette blue/red (16 entries) |

## Instruction Set

### 65SC02-Specific Instructions

#### Branch Always (BRA)
```asm
	bra label			; unconditional relative branch
```

#### Push/Pull X and Y
```asm
	phx					; push X to stack
	phy					; push Y to stack
	plx					; pull X from stack
	ply					; pull Y from stack
```

#### Store Zero (STZ)
```asm
	stz $10				; store zero to zero page
	stz $10,x			; store zero indexed
	stz $1000			; store zero absolute
	stz $1000,x			; store zero absolute indexed
```

#### Test and Reset Bits (TRB)
```asm
	trb $10				; test and reset bits in zero page
	trb $1000			; test and reset bits absolute
```
Sets Z flag based on AND of A with memory, then clears bits in memory that are set in A.

#### Test and Set Bits (TSB)
```asm
	tsb $10				; test and set bits in zero page
	tsb $1000			; test and set bits absolute
```
Sets Z flag based on AND of A with memory, then sets bits in memory that are set in A.

#### INC/DEC Accumulator
```asm
	inc a				; increment accumulator
	dec a				; decrement accumulator
```

### Zero Page Indirect Addressing

The 65SC02 adds a new addressing mode `(zp)` without X or Y indexing:

```asm
	lda ($10)			; load A from address stored at $10-$11
	sta ($10)			; store A to address at $10-$11
	adc ($10)			; add with carry
	and ($10)			; logical AND
	cmp ($10)			; compare
	eor ($10)			; exclusive OR
	ora ($10)			; logical OR
	sbc ($10)			; subtract with carry
```

### Absolute Indexed Indirect

New JMP mode for jump tables:

```asm
	jmp ($1000,x)		; jump to address at $1000+X (16-bit)
```

## Programming Examples

### Hello World - Display Setup

```asm
.platform "lynx"
.org $200

; Bootstrap - runs after boot ROM
reset:
	; Disable interrupts during setup
	sei
	
	; Initialize stack
	ldx #$ff
	txs
	
	; Set up display buffer at $c000
	lda #$c0
	sta DISPADR+1
	lda #$00
	sta DISPADR
	
	; Enable display
	lda #$09
	sta DISPCTL
	
	; Clear screen
	jsr clear_screen
	
	; Enable interrupts
	cli

main_loop:
	; Wait for VBlank (Timer 2 IRQ)
	wai
	bra main_loop
	
clear_screen:
	; Clear display buffer at $c000
	lda #$00
	ldx #$00
.clear_loop:
	sta $c000,x
	sta $c100,x
	sta $c200,x
	; ... (continue for all pages)
	dex
	bne .clear_loop
	rts

; Interrupt handler
irq_handler:
	pha
	; Check which timer fired
	lda INTSET
	; Handle Timer 2 (VBlank)
	and #$04
	beq .not_vblank
	; Clear interrupt
	sta INTRST
.not_vblank:
	pla
	rti

; Vectors
.org $fffa
.word 0				; NMI (unused on Lynx)
.word reset			; RESET
.word irq_handler	; IRQ

```

### Hardware Math - Multiply

```asm
; Multiply two 16-bit numbers using Suzy math hardware
; Input: $10-$11 = multiplicand, $12-$13 = multiplier
; Output: $14-$17 = 32-bit result
multiply_16x16:
	; Set up operands (write in order: E, D, C, B, A)
	; For unsigned multiply: ABCD = operands, EFGH = result
	lda $10
	sta MATHB
	lda $11
	sta MATHA
	lda $12
	sta MATHD
	lda $13
	sta MATHC			; Writing MATHC triggers multiply
	
	; Wait for math complete (polling SPRSYS)
.wait_math:
	lda SPRSYS
	and #$04			; Math busy bit
	bne .wait_math
	
	; Read result (JKLM contains 32-bit product)
	lda MATHK
	sta $14
	lda MATHK+1
	sta $15
	lda MATHM
	sta $16
	lda MATHM+1
	sta $17
	rts

```

### Sprite Display

```asm
; Display a simple sprite using Suzy sprite engine

; Sprite Control Block (SCB) structure
.org $0200
my_sprite:
	.byte %00010001		; SPRCTL0: 4bpp, normal sprite
	.byte %00010000		; SPRCTL1: literal, no reload
	.byte $00			; Collision number
	.word next_sprite	; Next SCB (or $0000 if last)
	.word sprite_data	; Pointer to sprite data
	.word 80			; H position (center)
	.word 51			; V position (center)
	.word $0100			; H size (1.0 in 8.8 fixed point)
	.word $0100			; V size (1.0)
	.word 0				; Stretch
	.word 0				; Tilt
	; Palette follows for reloaded colors

next_sprite:
	.word $0000			; End of chain

sprite_data:
	; Literal sprite data (line by line)
	.byte $88			; Offset to next line
	.byte $01, $23		; Pixels (4bpp packed)
	; ... sprite data continues

; Start sprite engine
start_sprites:
	; Set SCB address
	lda #<my_sprite
	sta SCBADRL
	lda #>my_sprite
	sta SCBADRH
	
	; Set video buffer base
	lda #$00
	sta VIDBASH
	lda #$c0
	sta VIDBASL
	
	; Start sprite engine
	lda #$01
	sta SPRGO
	
	; Wait for completion
.wait_sprite:
	lda SPRSYS
	and #$01			; Sprite busy
	bne .wait_sprite
	
	rts

```

### Reading Joystick

```asm
; Read joystick input
; Returns buttons in A register
;
; Bit layout:
;   7 6 5 4 3 2 1 0
;   | | | | | | | +-- Up
;   | | | | | | +---- Down
;   | | | | | +------ Left
;   | | | | +-------- Right
;   | | | +---------- Option 1
;   | | +------------ Option 2
;   | +-------------- Inside (B)
;   +---------------- Outside (A)

read_joystick:
	lda JOYSTICK
	eor #$ff			; Invert (buttons are active low)
	rts

; Check if A button pressed
check_a_button:
	lda JOYSTICK
	and #$80
	beq .pressed		; Active low
	clc					; Not pressed
	rts
.pressed:
	sec					; Pressed
	rts

```

## Palette System

### Color Format

The Lynx has a 4096-color palette with 16 entries:

- **Green register**: 4 bits (16 levels)
- **Blue/Red register**: 4 bits blue (high nibble) + 4 bits red (low nibble)

```asm
; Set palette entry 0 to pure red
	lda #$0f			; Blue=0, Red=15
	sta BLUERED
	lda #$00			; Green=0
	sta GREEN

; Set palette entry 1 to white
	lda #$ff			; Blue=15, Red=15
	sta BLUERED+1
	lda #$0f			; Green=15
	sta GREEN+1

```

## Lynx ROM Header

Lynx ROMs have a 64-byte header:

| Offset | Size | Description |
|--------|------|-------------|
| 0-3 | 4 | Magic "LYNX" |
| 4-5 | 2 | Page size (ROM size / 256) |
| 6-7 | 2 | Load address (usually $0200) |
| 8-9 | 2 | Start address (entry point) |
| 10-41 | 32 | Game name (null-terminated) |
| 42-57 | 16 | Manufacturer |
| 58 | 1 | Rotation (0=none, 1=left, 2=right) |
| 59-63 | 5 | Reserved |

Poppy generates this header automatically with the `.platform "lynx"` directive.

## External Resources

### Official Documentation
- [Atari Lynx Developer Documentation](https://atarilynxdeveloper.wordpress.com/) - Comprehensive development resources
- [Lynx Development Kit](http://www.monlynx.de/lynx/) - Tools and documentation

### Technical References
- [Handy Lynx Emulator Source](https://github.com/handy-sdl/handy-sdl) - Reference implementation
- [65C02 Datasheet (WDC)](https://www.westerndesigncenter.com/wdc/documentation/w65c02s.pdf) - Official CPU documentation
- [Atari Lynx Hardware Manual](http://www.monlynx.de/lynx/lynxhw.html) - Hardware specifications

### Community Resources
- [AtariAge Lynx Forum](https://atariage.com/forums/forum/13-atari-lynx/) - Active development community
- [Lynx Programmer's Reference Guide](http://www.monlynx.de/lynx/lynxprgr.html) - Programming reference
- [CC65 Lynx Support](https://cc65.github.io/doc/lynx.html) - C compiler Lynx documentation

### Nexen Emulator
The [Nexen emulator](https://github.com/TheAnsarya/Nexen) contains a cycle-accurate Lynx core with:
- Accurate Mikey timer emulation
- Suzy sprite engine emulation
- Hardware math unit
- Full debugging support

