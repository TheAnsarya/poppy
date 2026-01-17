; =============================================================================
; Atari 2600 Basic Template
; =============================================================================

.system:atari2600
.cpu "6507"

; =============================================================================
; TIA Hardware Constants
; =============================================================================

; Write registers
VSYNC	= $00		; Vertical sync
VBLANK	= $01		; Vertical blank
WSYNC	= $02		; Wait for horizontal sync
RSYNC	= $03		; Reset horizontal sync
NUSIZ0	= $04		; Number-size player 0
NUSIZ1	= $05		; Number-size player 1
COLUP0	= $06		; Color player 0
COLUP1	= $07		; Color player 1
COLUPF	= $08		; Color playfield
COLUBK	= $09		; Color background
CTRLPF	= $0a		; Control playfield
REFP0	= $0b		; Reflect player 0
REFP1	= $0c		; Reflect player 1
PF0		= $0d		; Playfield 0
PF1		= $0e		; Playfield 1
PF2		= $0f		; Playfield 2
RESP0	= $10		; Reset player 0
RESP1	= $11		; Reset player 1
RESM0	= $12		; Reset missile 0
RESM1	= $13		; Reset missile 1
RESBL	= $14		; Reset ball
AUDC0	= $15		; Audio control 0
AUDC1	= $16		; Audio control 1
AUDF0	= $17		; Audio frequency 0
AUDF1	= $18		; Audio frequency 1
AUDV0	= $19		; Audio volume 0
AUDV1	= $1a		; Audio volume 1
GRP0	= $1b		; Graphics player 0
GRP1	= $1c		; Graphics player 1
ENAM0	= $1d		; Enable missile 0
ENAM1	= $1e		; Enable missile 1
ENABL	= $1f		; Enable ball
HMP0	= $20		; Horizontal motion player 0
HMP1	= $21		; Horizontal motion player 1
HMM0	= $22		; Horizontal motion missile 0
HMM1	= $23		; Horizontal motion missile 1
HMBL	= $24		; Horizontal motion ball
VDELP0	= $25		; Vertical delay player 0
VDELP1	= $26		; Vertical delay player 1
VDELBL	= $27		; Vertical delay ball
RESMP0	= $28		; Reset missile 0 to player 0
RESMP1	= $29		; Reset missile 1 to player 1
HMOVE	= $2a		; Apply horizontal motion
HMCLR	= $2b		; Clear horizontal motion
CXCLR	= $2c		; Clear collision latches

; Read registers
CXM0P	= $30		; Collision M0-P1, M0-P0
CXM1P	= $31		; Collision M1-P0, M1-P1
CXP0FB	= $32		; Collision P0-PF, P0-BL
CXP1FB	= $33		; Collision P1-PF, P1-BL
CXM0FB	= $34		; Collision M0-PF, M0-BL
CXM1FB	= $35		; Collision M1-PF, M1-BL
CXBLPF	= $36		; Collision BL-PF
CXPPMM	= $37		; Collision P0-P1, M0-M1
INPT0	= $38		; Paddle 0
INPT1	= $39		; Paddle 1
INPT2	= $3a		; Paddle 2
INPT3	= $3b		; Paddle 3
INPT4	= $3c		; Joystick 0 trigger
INPT5	= $3d		; Joystick 1 trigger

; RIOT Chip
SWCHA	= $280		; Joystick directions
SWACNT	= $281		; Port A DDR
SWCHB	= $282		; Console switches
SWBCNT	= $283		; Port B DDR
INTIM	= $284		; Timer output
INSTAT	= $285		; Timer status
TIM1T	= $294		; Set 1-cycle timer
TIM8T	= $295		; Set 8-cycle timer
TIM64T	= $296		; Set 64-cycle timer
T1024T	= $297		; Set 1024-cycle timer

; =============================================================================
; Constants
; =============================================================================

SCANLINES_VSYNC		= 3
SCANLINES_VBLANK	= 37
SCANLINES_VISIBLE	= 192
SCANLINES_OVERSCAN	= 30

; =============================================================================
; RAM Variables
; =============================================================================

.org $80

frame_counter:	.ds 1
player_x:		.ds 1
player_y:		.ds 1
joy_state:		.ds 1

; =============================================================================
; ROM Start
; =============================================================================

.org $f000

start:
	; Clean start - clear RAM and TIA
	sei
	cld
	ldx #$00
	txa
@clear:
	sta $00, x
	dex
	bne @clear

	; Initialize stack
	ldx #$ff
	txs

	; Initialize variables
	lda #$50
	sta player_x
	lda #$50
	sta player_y

; =============================================================================
; Main Loop
; =============================================================================

main_loop:
	; ----- VSYNC (3 scanlines) -----
	lda #$02
	sta VSYNC
	sta WSYNC
	sta WSYNC
	sta WSYNC
	lda #$00
	sta VSYNC

	; ----- VBLANK (37 scanlines) -----
	lda #$02
	sta VBLANK

	; Set timer for VBLANK period
	lda #43				; ~2812 cycles (37 * 76)
	sta TIM64T

	; Do game logic during VBLANK
	jsr read_joystick
	jsr update_game

	; Wait for VBLANK timer
@wait_vblank:
	lda INTIM
	bne @wait_vblank

	sta WSYNC
	lda #$00
	sta VBLANK

	; ----- VISIBLE AREA (192 scanlines) -----

	; Set background color
	lda #$84			; Blue
	sta COLUBK

	; Set player color
	lda #$0e			; White
	sta COLUP0

	ldx #SCANLINES_VISIBLE
@scanline:
	sta WSYNC

	; Draw player?
	txa
	cmp player_y
	bne @no_player
	lda #$ff
	sta GRP0
	bne @next
@no_player:
	lda #$00
	sta GRP0
@next:
	dex
	bne @scanline

	; ----- OVERSCAN (30 scanlines) -----
	lda #$02
	sta VBLANK

	ldx #SCANLINES_OVERSCAN
@overscan:
	sta WSYNC
	dex
	bne @overscan

	inc frame_counter

	jmp main_loop

; =============================================================================
; Read Joystick
; =============================================================================

read_joystick:
	lda SWCHA
	sta joy_state
	rts

; =============================================================================
; Update Game
; =============================================================================

update_game:
	; Move player based on joystick
	lda joy_state

	; Up
	lsr a
	bcs @no_up
	dec player_y
@no_up:

	; Down
	lsr a
	bcs @no_down
	inc player_y
@no_down:

	; Left
	lsr a
	bcs @no_left
	dec player_x
@no_left:

	; Right
	lsr a
	bcs @no_right
	inc player_x
@no_right:

	rts

; =============================================================================
; Vectors
; =============================================================================

.org $fffa

	.dw start			; NMI
	.dw start			; Reset
	.dw start			; IRQ
