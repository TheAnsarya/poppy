; ============================================================================
; test.pasm - Simple NES test program
; ============================================================================

.org $8000

; Constants
PPU_CTRL = $2000
PPU_MASK = $2001
PPU_STATUS = $2002

; Reset vector handler
reset:
	sei              ; Disable interrupts
	cld              ; Clear decimal mode
	ldx #$ff
	txs              ; Initialize stack

	; Wait for PPU
	lda PPU_STATUS
	bpl reset

	; Clear RAM
	lda #$00
	ldx #$00
clear_loop:
	sta $00,x
	inx
	bne clear_loop

	; Enable rendering
	lda #$1e
	sta PPU_MASK

forever:
	jmp forever

; NMI handler
nmi:
	rti

; IRQ handler
irq:
	rti

; Vectors
.org $fffa
.word nmi        ; NMI vector
.word reset      ; Reset vector
.word irq        ; IRQ vector


