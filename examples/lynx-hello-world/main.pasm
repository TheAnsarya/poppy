; 🌸 Atari Lynx Hello World - Poppy Compiler Example
; Displays colored screen using Suzy/Mikey hardware

	.system:lynx
	.org $0200		; RAM start

; ═══════════════════════════════════════════════════════════════════════════
; Hardware Registers (Mikey)
; ═══════════════════════════════════════════════════════════════════════════

MIKEY		= $fd00
TIMER0		= MIKEY + $00	; Timer 0 backup
TIM0CTLA	= MIKEY + $04	; Timer 0 control A
TIM0CNT		= MIKEY + $05	; Timer 0 current count
TIM0CTLB	= MIKEY + $06	; Timer 0 control B
INTRST		= MIKEY + $80	; Interrupt reset
INTSET		= MIKEY + $81	; Interrupt set
DISPCTL		= MIKEY + $92	; Display control
PBKUP		= MIKEY + $8c	; P backup

; ═══════════════════════════════════════════════════════════════════════════
; Hardware Registers (Suzy)
; ═══════════════════════════════════════════════════════════════════════════

SUZY		= $fc00
SCBNEXT		= SUZY + $00	; Next SCB pointer
SPRDATAADR	= SUZY + $04	; Sprite data address
SPCTL0		= SUZY + $0a	; Sprite control 0
SPCTL1		= SUZY + $0b	; Sprite control 1
SPRINIT		= SUZY + $92	; Sprite initialization
SUZYHREV	= SUZY + $fc	; Suzy hardware revision

; Screen dimensions
SCREEN_WIDTH	= 160
SCREEN_HEIGHT	= 102

; ═══════════════════════════════════════════════════════════════════════════
; Entry Point
; ═══════════════════════════════════════════════════════════════════════════

	.org $0200
Reset:
	sei			; Disable interrupts
	cld			; Clear decimal mode

	; Initialize hardware
	lda #$98		; Enable Mikey
	sta DISPCTL

	; Set up video timing
	lda #$1f
	sta PBKUP		; P backup value

	; Initialize timers for display
	lda #$00
	sta TIM0CTLA
	lda #$9e		; Timer 0 config
	sta TIM0CTLB

	; Clear interrupts
	lda #$ff
	sta INTRST

MainLoop:
	; Wait for VBlank
.waitVBlank:
	lda INTRST
	and #$04		; Check VBlank bit
	beq .waitVBlank

	; Clear VBlank flag
	lda #$04
	sta INTRST

	jmp MainLoop

; ═══════════════════════════════════════════════════════════════════════════
; ROM Header
; ═══════════════════════════════════════════════════════════════════════════

	.org $fff8
	.db "LYNX"		; Magic bytes

	.org $fffa
	.dw Reset		; NMI vector
	.dw Reset		; Reset vector
	.dw Reset		; IRQ vector
