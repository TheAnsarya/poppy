; ============================================================================
; Game Boy Hello World
; Displays "HELLO" on screen
; ============================================================================

.system:gameboy

; ROM Header Configuration
.gb_title "HELLO"
.gb_licensee "01"              ; Nintendo
.gb_mbc NONE                   ; No MBC needed for 32KB
.gb_romsize 0                  ; 32KB (2 banks)
.gb_ramsize 0                  ; No RAM
.gb_cgb CGB_COMPATIBLE         ; Works on both DMG and CGB

; ============================================================================
; Constants
; ============================================================================

; Hardware registers
LCDC    = $ff40                ; LCD Control
BGP     = $ff47                ; Background Palette
LY      = $ff44                ; LCD Y-Coordinate

; LCDC flags
LCDC_ON     = $80              ; LCD Display Enable
LCDC_BG_ON  = $01              ; Background Enable

; Memory addresses
VRAM_TILES  = $8000            ; Tile data area
VRAM_MAP    = $9800            ; Background map

; ============================================================================
; Program Start (entry point at $0150)
; ============================================================================

.org $0150
start:
	; Disable interrupts during initialization
	di

	; Initialize stack pointer
	ld sp, $fffe

	; Turn off LCD (required before accessing VRAM)
	call wait_vblank
	xor a                      ; A = 0
	ldh (LCDC), a              ; Turn off LCD

	; Clear VRAM
	call clear_vram

	; Load tile data
	call load_tiles

	; Display "HELLO" on background
	call display_hello

	; Set up palette (grayscale)
	ld a, %11100100            ; Palette: 3=black, 2=dark, 1=light, 0=white
	ldh (BGP), a

	; Turn on LCD with background enabled
	ld a, LCDC_ON | LCDC_BG_ON
	ldh (LCDC), a

	; Enable interrupts
	ei

; Main loop
main_loop:
	halt                       ; Wait for interrupt
	jr main_loop

; ============================================================================
; Subroutines
; ============================================================================

; Wait for vertical blank period
wait_vblank:
	ldh a, (LY)                ; Read LCD Y coordinate
	cp 144                     ; Check if in VBlank (line 144+)
	jr nz, wait_vblank
	ret

; Clear all VRAM ($8000-$9fff = 8KB)
clear_vram:
	ld hl, $8000               ; Start of VRAM
	ld bc, $2000               ; 8KB to clear
@loop:
	xor a                      ; A = 0
	ld (hl+), a                ; Write and increment
	dec bc
	ld a, b
	or c                       ; Check if BC = 0
	jr nz, @loop
	ret

; Load tile data into VRAM
load_tiles:
	ld hl, VRAM_TILES          ; Destination: VRAM
	ld de, tile_data           ; Source: tile data
	ld bc, tile_data_end - tile_data  ; Length
@loop:
	ld a, (de)                 ; Read byte
	ld (hl+), a                ; Write to VRAM
	inc de
	dec bc
	ld a, b
	or c
	jr nz, @loop
	ret

; Display "HELLO" on background map
display_hello:
	ld hl, VRAM_MAP + 32 * 8 + 6  ; Row 8, column 6 (centered)

	; H
	ld a, 1
	ld (hl+), a

	; E
	ld a, 2
	ld (hl+), a

	; L
	ld a, 3
	ld (hl+), a

	; L
	ld a, 3
	ld (hl+), a

	; O
	ld a, 4
	ld (hl+), a

	ret

; ============================================================================
; Tile Data
; ============================================================================

; Each tile is 16 bytes (8x8 pixels, 2 bits per pixel)
; Format: 2 bytes per row (low byte, high byte)
; Bits: 00=color 0, 01=color 1, 10=color 2, 11=color 3

tile_data:
	; Tile 0: Blank (space)
	.byte $00, $00, $00, $00, $00, $00, $00, $00
	.byte $00, $00, $00, $00, $00, $00, $00, $00

	; Tile 1: H
	.byte $81, $81, $81, $81, $ff, $ff, $81, $81
	.byte $81, $81, $81, $81, $00, $00, $00, $00

	; Tile 2: E
	.byte $ff, $ff, $80, $80, $fc, $fc, $80, $80
	.byte $ff, $ff, $00, $00, $00, $00, $00, $00

	; Tile 3: L
	.byte $80, $80, $80, $80, $80, $80, $80, $80
	.byte $ff, $ff, $00, $00, $00, $00, $00, $00

	; Tile 4: O
	.byte $7e, $7e, $81, $81, $81, $81, $81, $81
	.byte $7e, $7e, $00, $00, $00, $00, $00, $00

tile_data_end:

; ============================================================================
; Interrupt Vectors (not used in this example, but defined for completeness)
; ============================================================================

; VBlank interrupt ($0040)
.org $0040
	reti

; LCD STAT interrupt ($0048)
.org $0048
	reti

; Timer interrupt ($0050)
.org $0050
	reti

; Serial interrupt ($0058)
.org $0058
	reti

; Joypad interrupt ($0060)
.org $0060
	reti
