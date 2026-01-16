; =============================================================================
; Sega Master System Basic Template
; =============================================================================

.target "sms"
.cpu "z80"

; =============================================================================
; Hardware Constants
; =============================================================================

; VDP Ports
VDP_DATA	= $be		; VDP data port
VDP_CTRL	= $bf		; VDP control port
VDP_STATUS	= $bf		; VDP status (read)

; I/O Ports
IO_A		= $dc		; Controller port A
IO_B		= $dd		; Controller port B
IO_CTRL		= $3f		; I/O port control

; PSG Port
PSG			= $7f		; Programmable Sound Generator

; Memory Mapper
MAPPER_CTRL	= $fffc		; Mapper control
MAPPER_ROM0	= $fffd		; ROM bank 0 (slot 0)
MAPPER_ROM1	= $fffe		; ROM bank 1 (slot 1)
MAPPER_ROM2	= $ffff		; ROM bank 2 (slot 2)

; =============================================================================
; ROM Header
; =============================================================================

.org $0000

	di
	im 1
	jp start

.org $0038
	; VBlank/IRQ handler
	jp irq_handler

.org $0066
	; NMI handler (Pause button)
	retn

; =============================================================================
; Header (at $7ff0 for 32KB ROMs)
; =============================================================================

.org $7ff0

header:
	.db "TMR SEGA"		; Magic string
	.dw $0000			; Reserved
	.dw $0000			; Checksum (filled by Poppy)
	.db $00, $00, $00	; Product code (BCD)
	.db $40				; Version + region (Export, 32KB)

; =============================================================================
; Main Code
; =============================================================================

.org $0100

start:
	; Initialize stack
	ld sp, $dff0

	; Silence PSG
	ld a, $9f			; Channel 0 volume off
	out (PSG), a
	ld a, $bf			; Channel 1 volume off
	out (PSG), a
	ld a, $df			; Channel 2 volume off
	out (PSG), a
	ld a, $ff			; Noise volume off
	out (PSG), a

	; Initialize VDP
	ld hl, vdp_regs
	ld b, 11			; 11 register writes
	ld c, 0				; Starting register
@init_vdp:
	ld a, (hl)
	out (VDP_CTRL), a
	ld a, c
	or $80
	out (VDP_CTRL), a
	inc hl
	inc c
	djnz @init_vdp

	; Clear VRAM
	xor a
	out (VDP_CTRL), a
	ld a, $40
	out (VDP_CTRL), a
	ld bc, $4000		; 16KB
@clear_vram:
	xor a
	out (VDP_DATA), a
	dec bc
	ld a, b
	or c
	jr nz, @clear_vram

	; Clear palette (CRAM)
	xor a
	out (VDP_CTRL), a
	ld a, $c0
	out (VDP_CTRL), a
	ld b, 32			; 32 colors
@clear_cram:
	xor a
	out (VDP_DATA), a
	djnz @clear_cram

	; Enable display and VBlank
	ld a, %11000000		; Display on, VBlank IRQ on
	out (VDP_CTRL), a
	ld a, $81
	out (VDP_CTRL), a

	ei

main_loop:
	halt				; Wait for VBlank

	call read_joypad
	call update_game

	jr main_loop

; =============================================================================
; VDP Register Initialization
; =============================================================================

vdp_regs:
	.db %00000110		; R0: Mode control 1 (no ext video)
	.db %10100000		; R1: Mode control 2 (display off initially)
	.db $ff				; R2: Name table at $3800
	.db $ff				; R3: Color table (unused Mode 4)
	.db $ff				; R4: Pattern generator (unused Mode 4)
	.db $ff				; R5: Sprite attribute at $3f00
	.db $fb				; R6: Sprite pattern at $0000
	.db $00				; R7: Border color (palette 0)
	.db $00				; R8: Horizontal scroll
	.db $00				; R9: Vertical scroll
	.db $ff				; R10: Line counter (VBlank only)

; =============================================================================
; Read Joypad
; =============================================================================

read_joypad:
	in a, (IO_A)
	cpl					; Invert (active low)
	ld (joypad_state), a
	ret

; =============================================================================
; Update Game
; =============================================================================

update_game:
	; TODO: Add game logic
	ret

; =============================================================================
; IRQ Handler
; =============================================================================

irq_handler:
	push af

	; Read VDP status to acknowledge interrupt
	in a, (VDP_STATUS)

	; TODO: VBlank processing

	pop af
	ei
	reti

; =============================================================================
; Variables (RAM at $c000)
; =============================================================================

.org $c000

joypad_state:
	.ds 1
