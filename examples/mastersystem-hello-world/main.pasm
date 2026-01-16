; Sega Master System Hello World Example
; Demonstrates basic Z80 assembly with VDP initialization
;
; The SMS uses a Z80 CPU and a VDP derived from the TMS9918

.target "mastersystem"
.cpu "z80"

; =============================================================================
; Hardware Constants
; =============================================================================

; VDP ports
VDP_DATA	= $be		; VDP data port
VDP_CTRL	= $bf		; VDP control port (also status when read)

; PSG port
PSG		= $7f		; Programmable Sound Generator

; I/O ports
IO_A		= $dc		; I/O port A (joypad 1, light gun)
IO_B		= $dd		; I/O port B (joypad 2, etc.)

; Memory mapper ports
MAPPER_CTRL	= $fffc		; Mapper control
MAPPER_SLOT0	= $fffd		; Slot 0 bank ($0000-$3fff)
MAPPER_SLOT1	= $fffe		; Slot 1 bank ($4000-$7fff)
MAPPER_SLOT2	= $ffff		; Slot 2 bank ($8000-$bfff)

; RAM
RAM_START	= $c000		; Start of RAM
RAM_END		= $dfff		; End of RAM

; VDP register values for Mode 4 (SMS mode)
VDP_REG0_VAL	= %00000110	; Mode 4, no sync, no external video
VDP_REG1_VAL	= %10100000	; Display on, vblank IRQ on, 8x8 sprites

; =============================================================================
; RAM Variables (at $c000)
; =============================================================================

.org $c000
frame_count:	.ds 1		; Frame counter
vblank_flag:	.ds 1		; VBlank occurred flag

; =============================================================================
; ROM Start
; =============================================================================

.org $0000

	di			; Disable interrupts
	im 1			; Set interrupt mode 1
	jp init			; Jump to initialization

; =============================================================================
; RST Vectors
; =============================================================================

.org $0008
	ret

.org $0010
	ret

.org $0018
	ret

.org $0020
	ret

.org $0028
	ret

.org $0030
	ret

; =============================================================================
; VBlank Interrupt Handler (called at $0038 in IM 1)
; =============================================================================

.org $0038
	push af
	in a, (VDP_CTRL)	; Read VDP status (acknowledges interrupt)
	ld a, 1
	ld (vblank_flag), a
	pop af
	ei
	reti

; =============================================================================
; NMI Handler (Pause button)
; =============================================================================

.org $0066
	retn

; =============================================================================
; Main Program
; =============================================================================

.org $0100

init:
	; Initialize stack
	ld sp, $dff0

	; Initialize RAM
	call clear_ram

	; Initialize VDP
	call init_vdp

	; Initialize PSG (silence all channels)
	call silence_psg

	; Clear VRAM
	call clear_vram

	; Load palette
	call load_palette

	; Enable interrupts
	ei

main_loop:
	; Wait for vblank
	call wait_vblank

	; Increment frame counter
	ld a, (frame_count)
	inc a
	ld (frame_count), a

	; Main game logic would go here

	jr main_loop

; -----------------------------------------------------------------------------
; Clear RAM
; -----------------------------------------------------------------------------
clear_ram:
	ld hl, RAM_START
	ld de, RAM_START + 1
	ld bc, $1fff		; 8KB - 1
	ld (hl), 0
	ldir
	ret

; -----------------------------------------------------------------------------
; Initialize VDP
; -----------------------------------------------------------------------------
init_vdp:
	; Set up VDP registers
	ld hl, vdp_register_data
	ld b, 11		; 11 registers (0-10)
	ld c, 0			; Starting register
@loop:
	ld a, (hl)
	out (VDP_CTRL), a
	ld a, c
	or $80			; Set high bit for register write
	out (VDP_CTRL), a
	inc hl
	inc c
	djnz @loop
	ret

vdp_register_data:
	.db %00000110		; Reg 0: Mode 4
	.db %10100000		; Reg 1: Display on, VBlank IRQ
	.db $ff			; Reg 2: Name table at $3800
	.db $ff			; Reg 3: (unused in mode 4)
	.db $ff			; Reg 4: (unused in mode 4)
	.db $ff			; Reg 5: Sprite table at $3f00
	.db $ff			; Reg 6: Sprite tiles in second half
	.db $f0			; Reg 7: Border color (palette entry 0)
	.db $00			; Reg 8: X scroll
	.db $00			; Reg 9: Y scroll
	.db $ff			; Reg 10: Line counter (disabled)

; -----------------------------------------------------------------------------
; Silence PSG
; -----------------------------------------------------------------------------
silence_psg:
	ld a, %10011111		; Channel 0 volume = off
	out (PSG), a
	ld a, %10111111		; Channel 1 volume = off
	out (PSG), a
	ld a, %11011111		; Channel 2 volume = off
	out (PSG), a
	ld a, %11111111		; Channel 3 (noise) volume = off
	out (PSG), a
	ret

; -----------------------------------------------------------------------------
; Clear VRAM
; -----------------------------------------------------------------------------
clear_vram:
	; Set VRAM address to 0
	xor a
	out (VDP_CTRL), a
	ld a, $40		; Bit 6 = write mode
	out (VDP_CTRL), a

	; Write zeros
	ld bc, $4000		; 16KB
@loop:
	xor a
	out (VDP_DATA), a
	dec bc
	ld a, b
	or c
	jr nz, @loop
	ret

; -----------------------------------------------------------------------------
; Load Palette
; -----------------------------------------------------------------------------
load_palette:
	; Set CRAM address to 0
	xor a
	out (VDP_CTRL), a
	ld a, $c0		; Bit 7,6 = CRAM write
	out (VDP_CTRL), a

	; Write palette (32 colors)
	ld hl, palette_data
	ld b, 32
@loop:
	ld a, (hl)
	out (VDP_DATA), a
	inc hl
	djnz @loop
	ret

palette_data:
	; Background palette (16 colors)
	.db $00			; Black
	.db $3f			; White
	.db $03			; Red
	.db $0c			; Green
	.db $30			; Blue
	.db $0f			; Yellow
	.db $33			; Cyan
	.db $3c			; Magenta
	.db $15			; Gray
	.db $00, $00, $00, $00, $00, $00, $00

	; Sprite palette (16 colors)
	.db $00			; Transparent
	.db $3f			; White
	.db $03			; Red
	.db $0c			; Green
	.db $30			; Blue
	.db $0f			; Yellow
	.db $33			; Cyan
	.db $3c			; Magenta
	.db $15			; Gray
	.db $00, $00, $00, $00, $00, $00, $00

; -----------------------------------------------------------------------------
; Wait for VBlank
; -----------------------------------------------------------------------------
wait_vblank:
	xor a
	ld (vblank_flag), a
@wait:
	ld a, (vblank_flag)
	or a
	jr z, @wait
	ret

; =============================================================================
; SMS ROM Header (at $7ff0)
; =============================================================================

.org $7ff0
	.ds 8			; Reserved
	.db $4d, $52		; "MR" - ROM checksum marker
	.dw $0000		; Checksum (to be calculated)
	.db $00, $00		; Product code (BCD)
	.db $00			; Version
	.db $4c			; Region/size: Export, 32KB

; =============================================================================
; Padding to 32KB
; =============================================================================

.org $7fff
	.db $00
