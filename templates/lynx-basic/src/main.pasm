; =============================================================================
; Atari Lynx Basic Template
; =============================================================================

.system:lynx
.cpu "65sc02"

; =============================================================================
; Hardware Constants
; =============================================================================

; Mikey (Audio/Timer/UART)
MIKEY		= $fd00

TIM0BKUP	= $fd00		; Timer 0 backup value
TIM0CTLA	= $fd01		; Timer 0 control A
TIM0CNT		= $fd02		; Timer 0 count
TIM0CTLB	= $fd03		; Timer 0 control B
TIM1BKUP	= $fd04		; Timer 1
TIM1CTLA	= $fd05
TIM1CNT		= $fd06
TIM1CTLB	= $fd07
TIM2BKUP	= $fd08		; Timer 2
TIM2CTLA	= $fd09
TIM2CNT		= $fd0a
TIM2CTLB	= $fd0b
TIM3BKUP	= $fd0c		; Timer 3
TIM3CTLA	= $fd0d
TIM3CNT		= $fd0e
TIM3CTLB	= $fd0f
TIM4BKUP	= $fd10		; Timer 4 (linked to audio 0)
TIM4CTLA	= $fd11
TIM4CNT		= $fd12
TIM4CTLB	= $fd13
TIM5BKUP	= $fd14		; Timer 5 (audio 1)
TIM5CTLA	= $fd15
TIM5CNT		= $fd16
TIM5CTLB	= $fd17
TIM6BKUP	= $fd18		; Timer 6 (audio 2)
TIM6CTLA	= $fd19
TIM6CNT		= $fd1a
TIM6CTLB	= $fd1b
TIM7BKUP	= $fd1c		; Timer 7 (audio 3)
TIM7CTLA	= $fd1d
TIM7CNT		= $fd1e
TIM7CTLB	= $fd1f

AUD0VOL		= $fd20		; Audio channel 0 volume
AUD0SHFTFB	= $fd21		; Audio 0 shift feedback
AUD0OUTVAL	= $fd22		; Audio 0 output value
AUD0L8SHFT	= $fd23		; Audio 0 L8 shift
AUD0TBACK	= $fd24		; Audio 0 timer backup
AUD0CTL		= $fd25		; Audio 0 control
AUD0COUNT	= $fd26		; Audio 0 count
AUD0MISC	= $fd27		; Audio 0 misc

MSTEREO		= $fd50		; Stereo control
INTRST		= $fd80		; Interrupt reset
INTSET		= $fd81		; Interrupt set
MAPTS		= $fd84		; ? mapping
SYSCTL1		= $fd87		; System control 1
MIKEYHREV	= $fd88		; Mikey hardware revision
IODIR		= $fd8a		; I/O direction
IODAT		= $fd8b		; I/O data
SERCTL		= $fd8c		; Serial control
SERDAT		= $fd8d		; Serial data
PBKUP		= $fd90		; Parallel backup
DISPADRL	= $fd94		; Display address low
DISPADRH	= $fd95		; Display address high

; Suzy (Graphics/Math)
SUZY		= $fc00

TMPADRH		= $fc00		; Temp address high
TMPADRL		= $fc01		; Temp address low
TILTACUM	= $fc02		; Tilt accumulator
HOFFL		= $fc04		; Horizontal offset low
HOFFH		= $fc05		; Horizontal offset high
VOFFL		= $fc06		; Vertical offset low
VOFFH		= $fc07		; Vertical offset high
VIDBASL		= $fc08		; Video base low
VIDBASH		= $fc09		; Video base high
COLLADRH	= $fc0a		; Collision address high
COLLADRL	= $fc0b		; Collision address low
SCBNEXTL	= $fc10		; SCB next low
SCBNEXTH	= $fc11		; SCB next high
SPRDLINEL	= $fc12		; Sprite data line low
SPRDLINEH	= $fc13		; Sprite data line high
HPOSSTRTL	= $fc14		; H position start low
HPOSSTRTH	= $fc15		; H position start high
VPOSSTRTL	= $fc16		; V position start low
VPOSSTRTH	= $fc17		; V position start high
SPRHSIZL	= $fc18		; Sprite H size low
SPRHSIZH	= $fc19		; Sprite H size high
SPRVSIZL	= $fc1a		; Sprite V size low
SPRVSIZH	= $fc1b		; Sprite V size high
STRETCHL	= $fc1c		; Stretch low
STRETCHH	= $fc1d		; Stretch high
TILTL		= $fc1e		; Tilt low
TILTH		= $fc1f		; Tilt high

SPRCTL0		= $fc80		; Sprite control 0
SPRCTL1		= $fc81		; Sprite control 1
SPRCOLL		= $fc82		; Sprite collision
SPRINIT		= $fc83		; Sprite init
SUZYHREV	= $fc88		; Suzy hardware revision
SUZYSREV	= $fc89		; Suzy software revision
SUZYBUSEN	= $fc90		; Suzy bus enable
SPRGO		= $fc91		; Sprite process go
SPRSYS		= $fc92		; Sprite system control

JOYSTICK	= $fcb0		; Joystick input
SWITCHES	= $fcb1		; Console switches

; Memory
SCREEN_BUF	= $c000		; Screen buffer (80 bytes per line × 102 lines)

; =============================================================================
; Entry Point
; =============================================================================

.org $0200

start:
	sei
	cld

	; Initialize stack
	ldx #$ff
	txs

	; Enable Suzy
	lda #$01
	sta SUZYBUSEN

	; Set up display
	lda #<SCREEN_BUF
	sta DISPADRL
	lda #>SCREEN_BUF
	sta DISPADRH

	; Clear screen buffer
	jsr clear_screen

	; Enable interrupts
	cli

main_loop:
	; Wait for VBlank (timer 0)
	lda TIM0CTLA
	and #$80
	beq main_loop

	; Clear timer flag
	lda #$00
	sta TIM0CTLA

	jsr read_input
	jsr update_game

	bra main_loop

; =============================================================================
; Clear Screen
; =============================================================================

clear_screen:
	lda #<SCREEN_BUF
	sta $00
	lda #>SCREEN_BUF
	sta $01

	; 80 × 102 = 8160 bytes
	ldx #32				; 32 pages
	ldy #$00
	lda #$00			; Black
@loop:
	sta ($00), y
	iny
	bne @loop
	inc $01
	dex
	bne @loop

	rts

; =============================================================================
; Read Input
; =============================================================================

read_input:
	lda JOYSTICK
	eor #$ff			; Invert (active low)
	sta joy_state
	rts

; =============================================================================
; Update Game
; =============================================================================

update_game:
	; TODO: Add game logic
	rts

; =============================================================================
; Variables
; =============================================================================

.org $0000

joy_state:
	.ds 1

; =============================================================================
; Vectors
; =============================================================================

.org $fffa

	.dw start			; NMI
	.dw start			; Reset
	.dw start			; IRQ
