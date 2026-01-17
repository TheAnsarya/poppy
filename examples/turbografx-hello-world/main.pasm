; TurboGrafx-16 / PC Engine Hello World Example
; Demonstrates basic HuC6280 assembly with VDC initialization
;
; The HuC6280 is a modified 65C02 with additional instructions:
; - Block transfer: tii, tdd, tin, tia, tai
; - Memory mapping: tam, tma
; - CPU speed control: csl, csh
; - I/O control: st0, st1, st2

.system:turbografx16
.cpu "huc6280"

; =============================================================================
; Hardware Constants
; =============================================================================

; VDC (Video Display Controller) ports
VDC_STATUS	= $0000		; VDC status/address register
VDC_DATA_L	= $0002		; VDC data low byte
VDC_DATA_H	= $0003		; VDC data high byte

; VCE (Video Color Encoder) ports
VCE_CONTROL	= $0400		; VCE control register
VCE_ADDR_L	= $0402		; VCE address low
VCE_ADDR_H	= $0403		; VCE address high
VCE_DATA_L	= $0404		; VCE data low
VCE_DATA_H	= $0405		; VCE data high

; Timer ports
TIMER_DATA	= $0c00		; Timer data
TIMER_CTRL	= $0c01		; Timer control

; I/O port
JOYPAD		= $1000		; Joypad input

; IRQ control
IRQ_DISABLE	= $1402		; IRQ disable register
IRQ_STATUS	= $1403		; IRQ status register

; =============================================================================
; Zero Page Variables
; =============================================================================

.org $2000
frame_count:	.ds 1		; Frame counter
temp:		.ds 2		; Temporary storage

; =============================================================================
; Program Code
; =============================================================================

.org $e000

reset:
	sei			; Disable interrupts
	csh			; Set high-speed mode (7.16 MHz)
	cld			; Clear decimal mode
	ldx #$ff
	txs			; Initialize stack

	; Initialize memory mapping
	lda #$ff
	tam #$00		; Map I/O to bank 0
	lda #$f8
	tam #$01		; Map RAM to bank 1

	; Disable all interrupts
	lda #$07
	sta IRQ_DISABLE

	; Initialize VDC
	jsr init_vdc

	; Initialize VCE (set up palette)
	jsr init_vce

	; Clear VRAM
	jsr clear_vram

	; Enable interrupts
	lda #$00
	sta IRQ_DISABLE
	cli

main_loop:
	; Wait for vblank
	jsr wait_vblank

	; Increment frame counter
	inc frame_count

	; Main game logic would go here

	jmp main_loop

; -----------------------------------------------------------------------------
; Initialize VDC
; -----------------------------------------------------------------------------
init_vdc:
	; Set up screen mode (256x224)
	st0 #$09		; Select MWR register
	st1 #$00		; Memory width = 32 words
	st2 #$00

	; Set up display area
	st0 #$0a		; HSR register
	st1 #$02
	st2 #$02

	st0 #$0b		; HDR register
	st1 #$1f
	st2 #$04

	st0 #$0c		; VPR register
	st1 #$02
	st2 #$0d

	st0 #$0d		; VDW register
	st1 #$df
	st2 #$00

	st0 #$0e		; VCR register
	st1 #$03
	st2 #$00

	; Enable display
	st0 #$05		; CR register
	st1 #$c8		; Enable BG, enable vblank IRQ
	st2 #$00

	rts

; -----------------------------------------------------------------------------
; Initialize VCE (palette)
; -----------------------------------------------------------------------------
init_vce:
	; Set VCE to normal speed
	stz VCE_CONTROL

	; Set palette address to 0
	stz VCE_ADDR_L
	stz VCE_ADDR_H

	; Set background color (dark blue)
	lda #$01		; Blue
	sta VCE_DATA_L
	lda #$00
	sta VCE_DATA_H

	; Set color 1 (white for text)
	lda #$ff
	sta VCE_DATA_L
	lda #$01
	sta VCE_DATA_H

	rts

; -----------------------------------------------------------------------------
; Clear VRAM
; -----------------------------------------------------------------------------
clear_vram:
	; Set VRAM write address to 0
	st0 #$00		; MAWR register
	st1 #$00
	st2 #$00

	; Set auto-increment
	st0 #$05		; CR register
	st1 #$c8
	st2 #$00

	; Write zeros to VRAM
	st0 #$02		; VWR register
	ldx #$00
	ldy #$80		; $8000 words = 32KB
@clear_loop:
	st1 #$00
	st2 #$00
	dex
	bne @clear_loop
	dey
	bne @clear_loop

	rts

; -----------------------------------------------------------------------------
; Wait for VBlank
; -----------------------------------------------------------------------------
wait_vblank:
@wait:
	lda IRQ_STATUS
	and #$20		; Check vblank flag
	beq @wait
	lda #$20
	sta IRQ_STATUS		; Clear vblank flag
	rts

; =============================================================================
; Interrupt Vectors
; =============================================================================

.org $fff6
	.dw irq2_handler	; IRQ2 (BRK)
	.dw irq1_handler	; IRQ1 (VDC)
	.dw timer_handler	; Timer
	.dw nmi_handler		; NMI (directly from pin)
	.dw reset		; Reset

irq1_handler:
irq2_handler:
timer_handler:
nmi_handler:
	rti
