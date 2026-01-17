; 🌸 Atari 2600 Hello World - Poppy Compiler Example
; Displays colored bars on screen using TIA

	.system:atari2600
	.org $f000

; ═══════════════════════════════════════════════════════════════════════════
; Constants
; ═══════════════════════════════════════════════════════════════════════════

; TIA Registers
VSYNC	= $00		; Vertical sync
VBLANK	= $01		; Vertical blank
WSYNC	= $02		; Wait for horizontal sync
COLUBK	= $09		; Background color
COLUPF	= $08		; Playfield color
PF0	= $0d		; Playfield 0
PF1	= $0e		; Playfield 1
PF2	= $0f		; Playfield 2

; RIOT Registers
INTIM	= $284		; Timer value
TIM64T	= $296		; Set 64-cycle timer

; Frame timing
VBLANK_TIME	= 43	; VBLANK timer value (43 * 64 = 2752 cycles)
OVERSCAN_TIME	= 35	; Overscan timer value

; ═══════════════════════════════════════════════════════════════════════════
; Entry Point
; ═══════════════════════════════════════════════════════════════════════════

Reset:
	sei			; Disable interrupts
	cld			; Clear decimal mode
	ldx #$ff
	txs			; Initialize stack

	; Clear TIA and RAM
	lda #0
	ldx #$ff
.clearLoop:
	sta $00,x
	dex
	bne .clearLoop

; ═══════════════════════════════════════════════════════════════════════════
; Main Loop
; ═══════════════════════════════════════════════════════════════════════════

MainLoop:
	; === Vertical Sync (3 scanlines) ===
	lda #2
	sta VSYNC
	sta WSYNC
	sta WSYNC
	sta WSYNC
	lda #0
	sta VSYNC

	; === Vertical Blank (37 scanlines) ===
	lda #VBLANK_TIME
	sta TIM64T		; Start timer
	lda #2
	sta VBLANK		; Enable VBLANK

.waitVblank:
	lda INTIM
	bne .waitVblank		; Wait for timer
	sta WSYNC
	sta VBLANK		; Disable VBLANK (A=0)

	; === Visible Scanlines (192 lines) ===
	ldx #192
	ldy #0			; Color counter

.scanlineLoop:
	sta WSYNC		; Wait for scanline start
	sty COLUBK		; Set background color
	iny			; Next color
	dex
	bne .scanlineLoop

	; === Overscan (30 scanlines) ===
	lda #2
	sta VBLANK		; Enable VBLANK
	lda #OVERSCAN_TIME
	sta TIM64T		; Start timer

.waitOverscan:
	lda INTIM
	bne .waitOverscan	; Wait for timer

	jmp MainLoop		; Next frame

; ═══════════════════════════════════════════════════════════════════════════
; Vectors
; ═══════════════════════════════════════════════════════════════════════════

	.org $fffa
	.dw Reset		; NMI (not used on 2600)
	.dw Reset		; Reset
	.dw Reset		; IRQ (not used on 2600)
