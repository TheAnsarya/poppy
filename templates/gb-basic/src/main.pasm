; =============================================================================
; Game Boy Basic Template
; =============================================================================

.system:gameboy
.cpu "sm83"

; =============================================================================
; Hardware Constants
; =============================================================================

; LCD
LCDC	= $40	; LCD control
STAT	= $41	; LCD status
SCY		= $42	; Scroll Y
SCX		= $43	; Scroll X
LY		= $44	; Current scanline
LYC		= $45	; LY compare
DMA		= $46	; OAM DMA
BGP		= $47	; BG palette
OBP0	= $48	; Object palette 0
OBP1	= $49	; Object palette 1
WY		= $4a	; Window Y
WX		= $4b	; Window X

; Joypad
P1		= $00	; Joypad

; Interrupt
IF		= $0f	; Interrupt flag
IE		= $ff	; Interrupt enable

; Audio
NR10	= $10	; Channel 1 sweep
NR11	= $11	; Channel 1 duty/length
NR12	= $12	; Channel 1 volume
NR13	= $13	; Channel 1 frequency low
NR14	= $14	; Channel 1 frequency high
NR50	= $24	; Master volume
NR51	= $25	; Panning
NR52	= $26	; Sound enable

; VRAM addresses
VRAM_TILES0	= $8000
VRAM_TILES1	= $8800
VRAM_BG		= $9800
VRAM_WIN	= $9c00

; =============================================================================
; Header and Entry Point
; =============================================================================

.org $0000
	; RST vectors
	.ds $40, $00

.org $0040
	; VBlank interrupt
	jp vblank_handler

.org $0048
	; STAT interrupt
	reti

.org $0050
	; Timer interrupt
	reti

.org $0058
	; Serial interrupt
	reti

.org $0060
	; Joypad interrupt
	reti

.org $0100
	; Entry point
	nop
	jp start

.org $0104
	; Nintendo logo (required)
	.db $ce, $ed, $66, $66, $cc, $0d, $00, $0b
	.db $03, $73, $00, $83, $00, $0c, $00, $0d
	.db $00, $08, $11, $1f, $88, $89, $00, $0e
	.db $dc, $cc, $6e, $e6, $dd, $dd, $d9, $99
	.db $bb, $bb, $67, $63, $6e, $0e, $ec, $cc
	.db $dd, $dc, $99, $9f, $bb, $b9, $33, $3e

.org $0134
	; Title (11 bytes)
	.db "MYGAME", 0, 0, 0, 0, 0

.org $0143
	; CGB flag
	.db $80			; CGB compatible

.org $0144
	; Licensee code
	.db $00, $00

.org $0146
	; SGB flag
	.db $00

.org $0147
	; Cartridge type (ROM only)
	.db $00

.org $0148
	; ROM size (32KB)
	.db $00

.org $0149
	; RAM size (none)
	.db $00

.org $014a
	; Destination
	.db $01			; Non-Japanese

.org $014b
	; Old licensee
	.db $33

.org $014c
	; Version
	.db $00

.org $014d
	; Header checksum (filled by Poppy)
	.db $00

.org $014e
	; Global checksum (filled by Poppy)
	.dw $0000

; =============================================================================
; Main Code
; =============================================================================

.org $0150

start:
	; Disable interrupts
	di

	; Wait for VBlank
@wait_vblank:
	ldh a, [LY]
	cp 144
	jr c, @wait_vblank

	; Disable LCD
	xor a
	ldh [LCDC], a

	; Initialize stack
	ld sp, $fffe

	; Clear WRAM
	ld hl, $c000
	ld bc, $2000
@clear_wram:
	xor a
	ld [hl+], a
	dec bc
	ld a, b
	or c
	jr nz, @clear_wram

	; Clear VRAM
	ld hl, $8000
	ld bc, $2000
@clear_vram:
	xor a
	ld [hl+], a
	dec bc
	ld a, b
	or c
	jr nz, @clear_vram

	; Clear OAM
	ld hl, $fe00
	ld b, 160
@clear_oam:
	xor a
	ld [hl+], a
	dec b
	jr nz, @clear_oam

	; Set palettes
	ld a, %11100100		; 3-2-1-0
	ldh [BGP], a
	ldh [OBP0], a
	ldh [OBP1], a

	; Enable LCD
	ld a, %10000001		; LCD on, BG on
	ldh [LCDC], a

	; Enable VBlank interrupt
	ld a, $01
	ldh [IE], a
	ei

main_loop:
	halt
	nop

	call read_joypad
	call update_game

	jr main_loop

; =============================================================================
; VBlank Handler
; =============================================================================

vblank_handler:
	push af
	push bc
	push de
	push hl

	; TODO: DMA, scroll updates

	pop hl
	pop de
	pop bc
	pop af
	reti

; =============================================================================
; Read Joypad
; =============================================================================

read_joypad:
	; Select D-pad
	ld a, $20
	ldh [P1], a
	ldh a, [P1]
	ldh a, [P1]		; Read twice for stability
	cpl
	and $0f
	swap a
	ld b, a

	; Select buttons
	ld a, $10
	ldh [P1], a
	ldh a, [P1]
	ldh a, [P1]
	cpl
	and $0f
	or b

	; Reset joypad
	ld b, a
	ld a, $30
	ldh [P1], a

	; Store result
	ld a, b
	ld [$c000], a	; joypad_state

	ret

; =============================================================================
; Update Game
; =============================================================================

update_game:
	; TODO: Add game logic
	ret
