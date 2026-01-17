; =============================================================================
; SPC700 Basic Template (SNES Audio)
; =============================================================================

.system:snes
.cpu "spc700"

; =============================================================================
; Hardware Constants
; =============================================================================

; DSP Register Addresses
DSP_ADDR	= $f2		; DSP register address
DSP_DATA	= $f3		; DSP register data

; Communication Ports (SNES <-> SPC700)
PORT0		= $f4		; I/O Port 0
PORT1		= $f5		; I/O Port 1
PORT2		= $f6		; I/O Port 2
PORT3		= $f7		; I/O Port 3

; Timer Registers
TIMER0		= $fa		; Timer 0 (8kHz)
TIMER1		= $fb		; Timer 1 (8kHz)
TIMER2		= $fc		; Timer 2 (64kHz)
COUNTER0	= $fd		; Timer 0 counter
COUNTER1	= $fe		; Timer 1 counter
COUNTER2	= $ff		; Timer 2 counter

; Control Registers
TEST		= $f0		; Test register (don't touch!)
CONTROL		= $f1		; Control register
DSPDATA		= $f3		; DSP data register

; DSP Voice Registers (per voice, add $10 for each voice 0-7)
DSP_VOLL	= $00		; Left volume
DSP_VOLR	= $01		; Right volume
DSP_PITCHL	= $02		; Pitch low
DSP_PITCHH	= $03		; Pitch high
DSP_SRCN	= $04		; Source number
DSP_ADSR1	= $05		; ADSR 1
DSP_ADSR2	= $06		; ADSR 2
DSP_GAIN	= $07		; Gain
DSP_ENVX	= $08		; Envelope value (read)
DSP_OUTX	= $09		; Wave output (read)

; DSP Global Registers
DSP_MVOLL	= $0c		; Master volume left
DSP_MVOLR	= $1c		; Master volume right
DSP_EVOLL	= $2c		; Echo volume left
DSP_EVOLR	= $3c		; Echo volume right
DSP_KON		= $4c		; Key on
DSP_KOFF	= $5c		; Key off
DSP_FLG		= $6c		; Flags
DSP_ENDX	= $7c		; End of sample (read)
DSP_EFB		= $0d		; Echo feedback
DSP_PMON	= $2d		; Pitch modulation
DSP_NON		= $3d		; Noise enable
DSP_EON		= $4d		; Echo enable
DSP_DIR		= $5d		; Sample directory page
DSP_ESA		= $6d		; Echo buffer page
DSP_EDL		= $7d		; Echo delay
DSP_FIR		= $0f		; FIR filter coefficients (8 regs)

; =============================================================================
; Entry Point
; =============================================================================

.org $0200

start:
	; Clear ports
	mov PORT0, #$00
	mov PORT1, #$00
	mov PORT2, #$00
	mov PORT3, #$00

	; Initialize control register
	mov CONTROL, #$80	; Enable IPL ROM readback, clear ports

	; Initialize DSP
	call init_dsp

	; Set up timer 0 for main loop timing (~125 Hz)
	mov TIMER0, #$40	; 8000 / 64 = 125 Hz
	mov CONTROL, #$01	; Enable timer 0

main_loop:
	; Wait for timer
	mov a, COUNTER0
	beq main_loop

	; Read commands from SNES
	call process_commands

	; Update sound engine
	call update_sound

	bra main_loop

; =============================================================================
; Initialize DSP
; =============================================================================

init_dsp:
	; Set master volume
	mov DSP_ADDR, #DSP_MVOLL
	mov DSP_DATA, #$7f
	mov DSP_ADDR, #DSP_MVOLR
	mov DSP_DATA, #$7f

	; Disable echo
	mov DSP_ADDR, #DSP_EVOLL
	mov DSP_DATA, #$00
	mov DSP_ADDR, #DSP_EVOLR
	mov DSP_DATA, #$00
	mov DSP_ADDR, #DSP_EON
	mov DSP_DATA, #$00

	; Set flags (disable mute and echo write)
	mov DSP_ADDR, #DSP_FLG
	mov DSP_DATA, #$20	; Echo write disabled

	; Set sample directory
	mov DSP_ADDR, #DSP_DIR
	mov DSP_DATA, #$04	; Directory at $0400

	; Mute all voices
	mov a, #$00
	mov x, #$00
@mute_loop:
	mov DSP_ADDR, x
	mov DSP_DATA, a		; Left volume = 0
	inc x
	mov DSP_ADDR, x
	mov DSP_DATA, a		; Right volume = 0
	clrc
	adc x, #$0f
	cmp x, #$80
	bne @mute_loop

	; Key off all voices
	mov DSP_ADDR, #DSP_KOFF
	mov DSP_DATA, #$ff

	ret

; =============================================================================
; Process Commands from SNES
; =============================================================================

process_commands:
	; Check for command in PORT0
	mov a, PORT0
	beq @no_command

	; Process command based on value
	cmp a, #$01
	beq @cmd_play_sfx
	cmp a, #$02
	beq @cmd_stop_all

	bra @no_command

@cmd_play_sfx:
	; TODO: Implement SFX playback
	mov PORT0, #$00		; Acknowledge
	bra @no_command

@cmd_stop_all:
	; Key off all voices
	mov DSP_ADDR, #DSP_KOFF
	mov DSP_DATA, #$ff
	mov PORT0, #$00		; Acknowledge

@no_command:
	ret

; =============================================================================
; Update Sound Engine
; =============================================================================

update_sound:
	; TODO: Add music/SFX engine
	ret

; =============================================================================
; Sample Directory (at $0400)
; =============================================================================

.org $0400

sample_directory:
	; Each entry: 4 bytes (start address, loop address)
	; Sample 0
	.dw sample_0_data
	.dw sample_0_loop

; =============================================================================
; Sample Data
; =============================================================================

.org $0500

sample_0_data:
	; BRR-encoded sample data here
	; 9 bytes per block (1 header + 8 sample bytes)
sample_0_loop:
	.db $b0, $00, $00, $00, $00, $00, $00, $00, $00	; Silent, end
