; SPC700 Hello World Example
; A minimal SNES audio driver demonstrating SPC700 assembly
;
; The SPC700 is the audio CPU in the SNES, similar to 6502 but different.
; It runs at 1.024 MHz and has 64KB of RAM shared with DSP registers.

.system:spc700
.cpu "spc700"

; =============================================================================
; I/O Registers (memory-mapped at $00f0-$00ff)
; =============================================================================

TEST		= $f0		; Test register (don't touch)
CONTROL		= $f1		; Control register
DSPADDR		= $f2		; DSP register address
DSPDATA		= $f3		; DSP register data
CPUIO0		= $f4		; Communication port 0 (to/from 65816)
CPUIO1		= $f5		; Communication port 1
CPUIO2		= $f6		; Communication port 2
CPUIO3		= $f7		; Communication port 3
AUXIO4		= $f8		; Auxiliary I/O port 4
AUXIO5		= $f9		; Auxiliary I/O port 5
T0TARGET	= $fa		; Timer 0 target (8 kHz base)
T1TARGET	= $fb		; Timer 1 target (8 kHz base)
T2TARGET	= $fc		; Timer 2 target (64 kHz base)
T0OUT		= $fd		; Timer 0 output (read clears)
T1OUT		= $fe		; Timer 1 output (read clears)
T2OUT		= $ff		; Timer 2 output (read clears)

; =============================================================================
; DSP Registers (accessed via DSPADDR/DSPDATA)
; =============================================================================

; Per-voice registers (add voice * $10)
DSP_VOL_L	= $00		; Volume left (-128 to +127)
DSP_VOL_R	= $01		; Volume right (-128 to +127)
DSP_PITCH_L	= $02		; Pitch low byte
DSP_PITCH_H	= $03		; Pitch high byte
DSP_SRCN	= $04		; Source number (sample index)
DSP_ADSR1	= $05		; ADSR settings 1
DSP_ADSR2	= $06		; ADSR settings 2
DSP_GAIN	= $07		; Gain (if ADSR disabled)
DSP_ENVX	= $08		; Current envelope value (read-only)
DSP_OUTX	= $09		; Current sample output (read-only)

; Global DSP registers
DSP_MVOL_L	= $0c		; Master volume left
DSP_MVOL_R	= $1c		; Master volume right
DSP_EVOL_L	= $2c		; Echo volume left
DSP_EVOL_R	= $3c		; Echo volume right
DSP_KON		= $4c		; Key on (play voices)
DSP_KOFF	= $5c		; Key off (stop voices)
DSP_FLG		= $6c		; Flags (reset, mute, echo, noise)
DSP_ENDX	= $7c		; Voice end flags (read-only)
DSP_EFB		= $0d		; Echo feedback
DSP_PMON	= $2d		; Pitch modulation enable
DSP_NON		= $3d		; Noise enable
DSP_EON		= $4d		; Echo enable
DSP_DIR		= $5d		; Sample directory page ($xx00)
DSP_ESA		= $6d		; Echo buffer start page
DSP_EDL		= $7d		; Echo delay (in 16ms units)

; FIR filter coefficients
DSP_C0		= $0f
DSP_C1		= $1f
DSP_C2		= $2f
DSP_C3		= $3f
DSP_C4		= $4f
DSP_C5		= $5f
DSP_C6		= $6f
DSP_C7		= $7f

; =============================================================================
; Zero Page Variables
; =============================================================================

.org $0000
temp:		.ds 2		; Temporary storage
tick_count:	.ds 1		; Music tick counter

; =============================================================================
; Main Program (starts at $0200 by convention)
; =============================================================================

.org $0200

reset:
	; Initialize stack
	mov x, #$ef
	mov sp, x

	; Initialize DSP
	call init_dsp

	; Set up sample directory
	call init_samples

	; Enable timers
	mov a, #%00000111	; Enable all 3 timers
	mov CONTROL, a

	; Set timer 0 for 125 Hz (8000 / 64 = 125)
	mov a, #64
	mov T0TARGET, a

main_loop:
	; Wait for timer tick
	call wait_timer

	; Update music/sfx here
	inc tick_count

	; Simple heartbeat: toggle voice 0 every 125 ticks (1 second)
	mov a, tick_count
	and a, #$7f
	bne @skip_toggle

	; Play a short beep on voice 0
	call play_beep

@skip_toggle:
	bra main_loop

; -----------------------------------------------------------------------------
; Initialize DSP
; -----------------------------------------------------------------------------
init_dsp:
	; Soft reset DSP
	mov DSPADDR, #DSP_FLG
	mov a, #%11100000	; Reset, mute, disable echo
	mov DSPDATA, a

	; Set master volume
	mov DSPADDR, #DSP_MVOL_L
	mov DSPDATA, #$7f
	mov DSPADDR, #DSP_MVOL_R
	mov DSPDATA, #$7f

	; Disable echo
	mov DSPADDR, #DSP_EVOL_L
	mov DSPDATA, #$00
	mov DSPADDR, #DSP_EVOL_R
	mov DSPDATA, #$00
	mov DSPADDR, #DSP_EON
	mov DSPDATA, #$00

	; Clear key on/off
	mov DSPADDR, #DSP_KON
	mov DSPDATA, #$00
	mov DSPADDR, #DSP_KOFF
	mov DSPDATA, #$ff	; Key off all voices

	; Set sample directory address
	mov DSPADDR, #DSP_DIR
	mov DSPDATA, #$04	; Directory at $0400

	; Release soft reset
	mov DSPADDR, #DSP_FLG
	mov DSPDATA, #$00

	ret

; -----------------------------------------------------------------------------
; Initialize Sample Directory
; -----------------------------------------------------------------------------
init_samples:
	; Sample directory at $0400
	; Each entry is 4 bytes: start_addr (2), loop_addr (2)

	; Sample 0: Square wave at $0500
	mov a, #$00
	mov $0400, a		; Start low
	mov a, #$05
	mov $0401, a		; Start high
	mov a, #$00
	mov $0402, a		; Loop low
	mov a, #$05
	mov $0403, a		; Loop high

	; Copy square wave sample to $0500
	mov y, #$00
@copy_loop:
	mov a, sample_square+y
	mov $0500+y, a
	inc y
	cmp y, #18		; 18 bytes (9 BRR blocks = 1 sample)
	bne @copy_loop

	ret

; Square wave sample (BRR encoded)
; Each BRR block is 9 bytes: 1 header + 8 sample nibbles
sample_square:
	.db $b0			; Header: loop, no filter, range 0
	.db $77, $77, $77, $77	; High samples
	.db $88, $88, $88, $88	; Low samples
	.db $b3			; Header: end, loop
	.db $77, $77, $77, $77
	.db $88, $88, $88, $88

; -----------------------------------------------------------------------------
; Play Beep on Voice 0
; -----------------------------------------------------------------------------
play_beep:
	; Set voice 0 volume
	mov DSPADDR, #DSP_VOL_L
	mov DSPDATA, #$40
	mov DSPADDR, #DSP_VOL_R
	mov DSPDATA, #$40

	; Set pitch (middle C roughly)
	mov DSPADDR, #DSP_PITCH_L
	mov DSPDATA, #$00
	mov DSPADDR, #DSP_PITCH_H
	mov DSPDATA, #$10

	; Set sample source
	mov DSPADDR, #DSP_SRCN
	mov DSPDATA, #$00	; Sample 0

	; Set ADSR (simple decay envelope)
	mov DSPADDR, #DSP_ADSR1
	mov DSPDATA, #%10001111	; ADSR enabled, attack=15, decay=7
	mov DSPADDR, #DSP_ADSR2
	mov DSPDATA, #%00011100	; Sustain=1, release=4

	; Key on voice 0
	mov DSPADDR, #DSP_KON
	mov DSPDATA, #$01

	ret

; -----------------------------------------------------------------------------
; Wait for Timer 0
; -----------------------------------------------------------------------------
wait_timer:
@wait:
	mov a, T0OUT		; Read clears the counter
	beq @wait
	ret

; =============================================================================
; Entry Point Vector (for SPC file format)
; =============================================================================

; The SPC file format expects execution to start at $0200
; The SpcFileBuilder will set PC to the start of this code
