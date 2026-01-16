; =============================================================================
; SNES Basic Template - Main Entry Point
; =============================================================================

.target "snes"
.cpu "65816"

; =============================================================================
; Hardware Constants
; =============================================================================

INIDISP		= $2100		; Display control
OBSEL		= $2101		; Object size/address
OAMADDL		= $2102		; OAM address low
OAMADDH		= $2103		; OAM address high
OAMDATA		= $2104		; OAM data
BGMODE		= $2105		; BG mode/tile size
MOSAIC		= $2106		; Mosaic
BG1SC		= $2107		; BG1 tilemap address
BG2SC		= $2108		; BG2 tilemap address
BG3SC		= $2109		; BG3 tilemap address
BG4SC		= $210a		; BG4 tilemap address
BG12NBA		= $210b		; BG1/2 tile address
BG34NBA		= $210c		; BG3/4 tile address
BG1HOFS		= $210d		; BG1 horizontal scroll
BG1VOFS		= $210e		; BG1 vertical scroll
VMAIN		= $2115		; VRAM address increment
VMADDL		= $2116		; VRAM address low
VMADDH		= $2117		; VRAM address high
VMDATAL		= $2118		; VRAM data low
VMDATAH		= $2119		; VRAM data high
CGADD		= $2121		; CGRAM address
CGDATA		= $2122		; CGRAM data
TM			= $212c		; Main screen designation
TS			= $212d		; Sub screen designation
NMITIMEN	= $4200		; NMI/IRQ enable
RDNMI		= $4210		; NMI flag
HVBJOY		= $4212		; H/V blank and joypad status

; Controller
JOY1L		= $4218		; Joypad 1 low
JOY1H		= $4219		; Joypad 1 high

; DMA
DMAP0		= $4300		; DMA parameters
BBAD0		= $4301		; DMA B-bus address
A1T0L		= $4302		; DMA A-bus address low
A1T0H		= $4303		; DMA A-bus address high
A1B0		= $4304		; DMA A-bus bank
DAS0L		= $4305		; DMA size low
DAS0H		= $4306		; DMA size high
MDMAEN		= $420b		; DMA enable

; WRAM
WMDATA		= $2180		; WRAM data
WMADDL		= $2181		; WRAM address low
WMADDM		= $2182		; WRAM address mid
WMADDH		= $2183		; WRAM address high

; =============================================================================
; Zero Page
; =============================================================================

.enum $00
	frame_count:	.ds 2
	joy1_press:		.ds 2
	joy1_held:		.ds 2
	temp:			.ds 4
.ende

; =============================================================================
; LoROM Bank $00
; =============================================================================

.org $008000
.a8
.i8

reset:
	sei
	clc
	xce					; Switch to native mode

	rep #$30			; 16-bit A/X/Y
	.a16
	.i16

	lda #$0000
	tcd					; Direct page = $0000

	lda #$01ff
	tcs					; Stack = $01ff

	sep #$20			; 8-bit A
	.a8

	; Force blank
	lda #$80
	sta INIDISP

	; Initialize hardware
	jsr init_ppu
	jsr init_dma

	; Enable NMI
	lda #$81
	sta NMITIMEN

	; End force blank, full brightness
	lda #$0f
	sta INIDISP

main_loop:
	wai					; Wait for NMI

	jsr read_joy
	jsr update_game

	bra main_loop

; =============================================================================
; Initialize PPU
; =============================================================================

init_ppu:
	; Clear VRAM
	stz VMAIN
	rep #$20
	.a16
	lda #$0000
	sta VMADDL

	ldx #$8000			; 32K words
@clear_vram:
	stz VMDATAL
	dex
	bne @clear_vram

	sep #$20
	.a8

	; Set video mode 1
	lda #$01
	sta BGMODE

	; BG1 tilemap at $0000
	lda #$00
	sta BG1SC

	; BG1 tiles at $1000
	lda #$01
	sta BG12NBA

	; Enable BG1
	lda #$01
	sta TM

	; Load palette (blue background)
	stz CGADD
	stz CGDATA			; Color 0: Black -> Blue
	lda #$7c
	sta CGDATA

	rts

; =============================================================================
; Initialize DMA
; =============================================================================

init_dma:
	rts

; =============================================================================
; Read Joypad
; =============================================================================

read_joy:
@wait:
	lda HVBJOY
	and #$01
	bne @wait

	rep #$20
	.a16
	lda JOY1L
	sta joy1_held
	sep #$20
	.a8

	rts

; =============================================================================
; Update Game
; =============================================================================

update_game:
	; TODO: Add game logic
	rts

; =============================================================================
; NMI Handler
; =============================================================================

nmi:
	rep #$30
	.a16
	.i16
	pha
	phx
	phy

	sep #$20
	.a8
	lda RDNMI			; Acknowledge NMI

	rep #$20
	.a16
	inc frame_count

	ply
	plx
	pla
	rti

; =============================================================================
; Vectors
; =============================================================================

.org $00ffb0
	; Native mode vectors
	.dw $0000			; COP
	.dw $0000			; BRK
	.dw $0000			; ABORT
	.dw nmi				; NMI
	.dw $0000			; (unused)
	.dw $0000			; IRQ

.org $00ffc0
	; Emulation mode vectors
	.dw $0000			; COP
	.dw $0000			; (unused)
	.dw $0000			; ABORT
	.dw nmi				; NMI
	.dw reset			; RESET
	.dw $0000			; IRQ/BRK
