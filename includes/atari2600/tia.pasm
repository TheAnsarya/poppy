; =============================================================================
; TIA (Television Interface Adaptor) Hardware Registers
; Atari 2600 Standard Include File
; =============================================================================
; Include this file to use TIA registers:
;   .include "tia.pasm"
; =============================================================================

; =============================================================================
; TIA Write Registers ($00-$2C)
; =============================================================================

; Sync & Timing
VSYNC	= $00		; Vertical sync set-clear
VBLANK	= $01		; Vertical blank set-clear
WSYNC	= $02		; Wait for horizontal blank
RSYNC	= $03		; Reset horizontal sync counter

; Player/Missile Size and Number
NUSIZ0	= $04		; Number-size player-missile 0
NUSIZ1	= $05		; Number-size player-missile 1

; Color Registers
COLUP0	= $06		; Color-luminance player 0
COLUP1	= $07		; Color-luminance player 1
COLUPF	= $08		; Color-luminance playfield
COLUBK	= $09		; Color-luminance background

; Playfield Control
CTRLPF	= $0a		; Control playfield, ball size, collisions
REFP0	= $0b		; Reflection player 0
REFP1	= $0c		; Reflection player 1

; Playfield Graphics
PF0		= $0d		; Playfield register byte 0
PF1		= $0e		; Playfield register byte 1
PF2		= $0f		; Playfield register byte 2

; Position Reset (Strobe)
RESP0	= $10		; Reset player 0
RESP1	= $11		; Reset player 1
RESM0	= $12		; Reset missile 0
RESM1	= $13		; Reset missile 1
RESBL	= $14		; Reset ball

; Audio Registers
AUDC0	= $15		; Audio control 0
AUDC1	= $16		; Audio control 1
AUDF0	= $17		; Audio frequency 0
AUDF1	= $18		; Audio frequency 1
AUDV0	= $19		; Audio volume 0
AUDV1	= $1a		; Audio volume 1

; Player Graphics
GRP0	= $1b		; Graphics player 0
GRP1	= $1c		; Graphics player 1

; Object Enable
ENAM0	= $1d		; Enable missile 0
ENAM1	= $1e		; Enable missile 1
ENABL	= $1f		; Enable ball

; Horizontal Motion
HMP0	= $20		; Horizontal motion player 0
HMP1	= $21		; Horizontal motion player 1
HMM0	= $22		; Horizontal motion missile 0
HMM1	= $23		; Horizontal motion missile 1
HMBL	= $24		; Horizontal motion ball

; Vertical Delay
VDELP0	= $25		; Vertical delay player 0
VDELP1	= $26		; Vertical delay player 1
VDELBL	= $27		; Vertical delay ball

; Missile/Player Graphics Reset
RESMP0	= $28		; Reset missile 0 to player 0
RESMP1	= $29		; Reset missile 1 to player 1

; Horizontal Motion Apply (Strobe)
HMOVE	= $2a		; Apply horizontal motion
HMCLR	= $2b		; Clear horizontal motion registers
CXCLR	= $2c		; Clear collision latches

; =============================================================================
; TIA Read Registers ($00-$0D, active bits in parentheses)
; =============================================================================

; Collision Detection (active: D7-D6)
CXM0P	= $00		; Read collision M0-P1, M0-P0
CXM1P	= $01		; Read collision M1-P0, M1-P1
CXP0FB	= $02		; Read collision P0-PF, P0-BL
CXP1FB	= $03		; Read collision P1-PF, P1-BL
CXM0FB	= $04		; Read collision M0-PF, M0-BL
CXM1FB	= $05		; Read collision M1-PF, M1-BL
CXBLPF	= $06		; Read collision BL-PF (D7 only)
CXPPMM	= $07		; Read collision P0-P1, M0-M1

; Input Ports (active: D7 or D6)
INPT0	= $08		; Read pot port 0 (D7)
INPT1	= $09		; Read pot port 1 (D7)
INPT2	= $0a		; Read pot port 2 (D7)
INPT3	= $0b		; Read pot port 3 (D7)
INPT4	= $0c		; Read input (trigger) 0 (D7)
INPT5	= $0d		; Read input (trigger) 1 (D7)

; =============================================================================
; RIOT registers are now in a separate include file:
;   .include "riot.pasm"
; =============================================================================

; =============================================================================
; Common Constants
; =============================================================================

; VSYNC/VBLANK bits
VSYNC_ON	= $02		; Start vertical sync
VSYNC_OFF	= $00		; End vertical sync
VBLANK_ON	= $02		; Start vertical blank
VBLANK_OFF	= $00		; End vertical blank

; NTSC timing (60 Hz)
NTSC_VBLANK_LINES	= 37	; Vertical blank lines
NTSC_KERNEL_LINES	= 192	; Visible scanlines
NTSC_OVERSCAN_LINES	= 30	; Overscan lines
NTSC_TOTAL_LINES	= 262	; Total scanlines per frame

; PAL timing (50 Hz)
PAL_VBLANK_LINES	= 45	; Vertical blank lines
PAL_KERNEL_LINES	= 228	; Visible scanlines
PAL_OVERSCAN_LINES	= 36	; Overscan lines
PAL_TOTAL_LINES		= 312	; Total scanlines per frame

; Timer values for TIM64T
VBLANK_TIME	= 43		; VBLANK timer (43 * 64 = 2752 cycles)
OVERSCAN_TIME	= 35		; Overscan timer (35 * 64 = 2240 cycles)

; CTRLPF bits
CTRLPF_REF	= $01		; Reflect playfield
CTRLPF_SCORE	= $02		; Score mode (left half P0 color, right half P1)
CTRLPF_PFP	= $04		; Playfield priority (PF/BL on top of P0/P1)
CTRLPF_BALL1	= $00		; Ball size: 1 clock
CTRLPF_BALL2	= $10		; Ball size: 2 clocks
CTRLPF_BALL4	= $20		; Ball size: 4 clocks
CTRLPF_BALL8	= $30		; Ball size: 8 clocks

; NUSIZ bits
NUSIZ_ONE		= $00		; One copy
NUSIZ_TWO_CLOSE	= $01		; Two copies, close spacing
NUSIZ_TWO_MED	= $02		; Two copies, medium spacing
NUSIZ_THREE_CLOSE	= $03	; Three copies, close spacing
NUSIZ_TWO_WIDE	= $04		; Two copies, wide spacing
NUSIZ_DOUBLE	= $05		; Double-width player
NUSIZ_THREE_MED	= $06		; Three copies, medium spacing
NUSIZ_QUAD		= $07		; Quad-width player
NUSIZ_MSL1		= $00		; Missile size: 1 clock
NUSIZ_MSL2		= $10		; Missile size: 2 clocks
NUSIZ_MSL4		= $20		; Missile size: 4 clocks
NUSIZ_MSL8		= $30		; Missile size: 8 clocks

; Console switch and joystick masks are in riot.pasm

; Color constants (NTSC)
BLACK		= $00
WHITE		= $0e
GRAY		= $06
RED			= $30
ORANGE		= $20
YELLOW		= $10
GREEN		= $c0
CYAN		= $a0
BLUE		= $80
PURPLE		= $50
