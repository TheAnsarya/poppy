; =============================================================================
; RIOT (6532) Hardware Registers
; Atari 2600 Standard Include File
; =============================================================================
; Include this file to use RIOT registers:
;   .include "riot.pasm"
; =============================================================================

; =============================================================================
; Port Registers
; =============================================================================

SWCHA	= $280		; Port A data (joysticks)
SWACNT	= $281		; Port A DDR (data direction)
SWCHB	= $282		; Port B data (console switches)
SWBCNT	= $283		; Port B DDR (data direction)

; =============================================================================
; Timer Registers
; =============================================================================

; Timer Read (active: all bits)
INTIM	= $284		; Read timer output
TIMINT	= $285		; Read timer interrupt flag (D7)

; Timer Set (interval values)
TIM1T	= $294		; Set 1 clock interval (838 ns)
TIM8T	= $295		; Set 8 clock interval (6.7 μs)
TIM64T	= $296		; Set 64 clock interval (53.6 μs)
T1024T	= $297		; Set 1024 clock interval (858.2 μs)

; =============================================================================
; Console Switch Bit Masks (for SWCHB)
; =============================================================================

SWITCH_RESET	= $01		; Reset switch (active low)
SWITCH_SELECT	= $02		; Select switch (active low)
SWITCH_BW		= $08		; B/W-Color switch (0=B/W, 1=Color)
SWITCH_P0_DIFF	= $40		; P0 difficulty (0=B, 1=A)
SWITCH_P1_DIFF	= $80		; P1 difficulty (0=B, 1=A)

; =============================================================================
; Joystick Bit Masks (for SWCHA)
; =============================================================================

JOY0_UP		= $10		; P0 joystick up (active low)
JOY0_DOWN	= $20		; P0 joystick down (active low)
JOY0_LEFT	= $40		; P0 joystick left (active low)
JOY0_RIGHT	= $80		; P0 joystick right (active low)
JOY1_UP		= $01		; P1 joystick up (active low)
JOY1_DOWN	= $02		; P1 joystick down (active low)
JOY1_LEFT	= $04		; P1 joystick left (active low)
JOY1_RIGHT	= $08		; P1 joystick right (active low)

; =============================================================================
; Timer Prescaler Reference
; =============================================================================
; Register  | Prescaler | Interval  | Max Time
; ----------|-----------|-----------|----------
; TIM1T     | 1         | 838 ns    | 213.7 μs
; TIM8T     | 8         | 6.7 μs    | 1.7 ms
; TIM64T    | 64        | 53.6 μs   | 13.7 ms
; T1024T    | 1024      | 858.2 μs  | 218.8 ms
; =============================================================================
