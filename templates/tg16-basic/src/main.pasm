; =============================================================================
; TurboGrafx-16 / PC Engine Basic Template
; =============================================================================

.system:turbografx16
.cpu "huc6280"

; =============================================================================
; Hardware Constants
; =============================================================================

; VDC (Video Display Controller)
VDC_STATUS	= $0000		; VDC status (read)
VDC_ADDR	= $0000		; VDC address register (write)
VDC_DATA_L	= $0002		; VDC data low
VDC_DATA_H	= $0003		; VDC data high

; VCE (Video Color Encoder)
VCE_CTRL	= $0400		; VCE control
VCE_ADDR_L	= $0402		; VCE address low
VCE_ADDR_H	= $0403		; VCE address high
VCE_DATA_L	= $0404		; VCE data low
VCE_DATA_H	= $0405		; VCE data high

; PSG
PSG_CH		= $0800		; Channel select
PSG_VOL		= $0801		; Global volume
PSG_FREQ_L	= $0802		; Frequency low
PSG_FREQ_H	= $0803		; Frequency high
PSG_CTRL	= $0804		; Channel control
PSG_CHVOL	= $0805		; Channel volume
PSG_DATA	= $0806		; Waveform data
PSG_NOISE	= $0807		; Noise control
PSG_LFO_F	= $0808		; LFO frequency
PSG_LFO_C	= $0809		; LFO control

; Timer
TIMER_DATA	= $0c00		; Timer counter
TIMER_CTRL	= $0c01		; Timer control

; I/O
JOYPAD		= $1000		; Joypad port

; IRQ Control
IRQ_DISABLE	= $1402		; IRQ disable
IRQ_STATUS	= $1403		; IRQ status

; =============================================================================
; Memory Map
; =============================================================================

; MPRs (Memory Paging Registers)
; MPR0: $0000-$1fff (I/O)
; MPR1: $2000-$3fff (RAM)
; MPR2-6: $4000-$dfff (ROM banks)
; MPR7: $e000-$ffff (ROM, vectors)

; =============================================================================
; Reset Vector
; =============================================================================

.org $fff6

	.dw irq2_handler	; IRQ2 (external)
	.dw irq1_handler	; IRQ1 (VDC)
	.dw timer_handler	; Timer
	.dw nmi_handler		; NMI
	.dw start			; Reset

; =============================================================================
; Main Code
; =============================================================================

.org $e000

start:
	sei
	csh					; High-speed mode (7.16 MHz)
	cld

	; Set up memory mapping
	lda #$ff			; I/O page
	tam #$01			; MPR0 = I/O
	lda #$f8			; RAM page
	tam #$02			; MPR1 = RAM

	; Initialize stack
	ldx #$ff
	txs

	; Disable interrupts
	lda #$07
	sta IRQ_DISABLE

	; Silence PSG
	stz PSG_VOL			; Master volume off
	ldx #$05
@silence_psg:
	stx PSG_CH			; Select channel
	stz PSG_CTRL		; Channel off
	dex
	bpl @silence_psg

	; Initialize VDC
	jsr init_vdc

	; Clear VRAM
	jsr clear_vram

	; Clear palette
	jsr clear_palette

	; Enable VBlank interrupt
	lda #$05			; IRQ2 disabled, Timer disabled
	sta IRQ_DISABLE

	cli

main_loop:
	; Wait for VBlank
	lda vblank_flag
	beq main_loop
	stz vblank_flag

	jsr read_joypad
	jsr update_game

	bra main_loop

; =============================================================================
; Initialize VDC
; =============================================================================

init_vdc:
	; Memory width (128x32 or 64x32)
	lda #$09			; MWR
	sta VDC_ADDR
	lda #$00			; 32 columns
	sta VDC_DATA_L
	stz VDC_DATA_H

	; Horizontal sync
	lda #$0a			; HSR
	sta VDC_ADDR
	lda #$02
	sta VDC_DATA_L
	lda #$02
	sta VDC_DATA_H

	; Horizontal display
	lda #$0b			; HDR
	sta VDC_ADDR
	lda #$1f			; 32 cells
	sta VDC_DATA_L
	lda #$04
	sta VDC_DATA_H

	; Vertical sync
	lda #$0c			; VPR
	sta VDC_ADDR
	lda #$02
	sta VDC_DATA_L
	lda #$17
	sta VDC_DATA_H

	; Vertical display
	lda #$0d			; VDW
	sta VDC_ADDR
	lda #$df			; 224 lines
	sta VDC_DATA_L
	stz VDC_DATA_H

	; Vertical end
	lda #$0e			; VCR
	sta VDC_ADDR
	lda #$0c
	sta VDC_DATA_L
	stz VDC_DATA_H

	; Control register (BG on, sprites off, VBlank IRQ)
	lda #$05			; CR
	sta VDC_ADDR
	lda #$cc			; BG on, VBlank IRQ
	sta VDC_DATA_L
	stz VDC_DATA_H

	rts

; =============================================================================
; Clear VRAM
; =============================================================================

clear_vram:
	; Set VRAM address to 0
	lda #$00			; MAWR
	sta VDC_ADDR
	stz VDC_DATA_L
	stz VDC_DATA_H

	; Write to VRAM
	lda #$02			; VWR
	sta VDC_ADDR

	; Clear 32KB
	ldx #$00
	ldy #$80
@loop:
	stz VDC_DATA_L
	stz VDC_DATA_H
	dex
	bne @loop
	dey
	bne @loop

	rts

; =============================================================================
; Clear Palette
; =============================================================================

clear_palette:
	stz VCE_ADDR_L
	stz VCE_ADDR_H

	ldx #$00
	ldy #$02			; 512 colors
@loop:
	stz VCE_DATA_L
	stz VCE_DATA_H
	dex
	bne @loop
	dey
	bne @loop

	rts

; =============================================================================
; Read Joypad
; =============================================================================

read_joypad:
	lda #$01			; Select
	sta JOYPAD
	lda #$03			; Select + CLR
	sta JOYPAD
	nop
	nop

	lda JOYPAD			; D-pad
	asl a
	asl a
	asl a
	asl a
	sta joypad_state

	lda #$01			; Next nibble
	sta JOYPAD
	nop
	nop

	lda JOYPAD			; Buttons
	and #$0f
	ora joypad_state
	eor #$ff			; Invert
	sta joypad_state

	rts

; =============================================================================
; Update Game
; =============================================================================

update_game:
	; TODO: Add game logic
	rts

; =============================================================================
; Interrupt Handlers
; =============================================================================

irq1_handler:
	pha

	; Check VDC status
	lda VDC_STATUS
	bpl @not_vblank

	; VBlank occurred
	lda #$01
	sta vblank_flag

@not_vblank:
	pla
	rti

irq2_handler:
timer_handler:
nmi_handler:
	rti

; =============================================================================
; Variables (RAM at $2000)
; =============================================================================

.org $2000

vblank_flag:
	.ds 1

joypad_state:
	.ds 1
