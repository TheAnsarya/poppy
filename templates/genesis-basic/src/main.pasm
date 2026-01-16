; =============================================================================
; Sega Genesis/Mega Drive Basic Template
; =============================================================================

.target "genesis"
.cpu "m68000"

; =============================================================================
; Hardware Constants
; =============================================================================

; VDP Ports
VDP_DATA	= $c00000
VDP_CTRL	= $c00004
VDP_HVCNT	= $c00008

; Z80 Ports
Z80_RAM		= $a00000
Z80_BUSREQ	= $a11100
Z80_RESET	= $a11200

; I/O Ports
IO_VER		= $a10001
IO_DATA1	= $a10003
IO_DATA2	= $a10005
IO_DATA3	= $a10007
IO_CTRL1	= $a10009
IO_CTRL2	= $a1000b
IO_CTRL3	= $a1000d

; PSG Port
PSG			= $c00011

; =============================================================================
; Vector Table
; =============================================================================

.org $000000

vectors:
	.dl $00ff0000		; Initial stack pointer
	.dl start			; Reset vector
	.dl int_bus_error	; Bus error
	.dl int_addr_error	; Address error
	.dl int_illegal		; Illegal instruction
	.dl int_div_zero	; Division by zero
	.dl int_chk			; CHK exception
	.dl int_trapv		; TRAPV exception
	.dl int_priv		; Privilege violation
	.dl int_trace		; Trace
	.dl int_line_a		; Line A emulation
	.dl int_line_f		; Line F emulation
	.ds 12 * 4, 0		; Reserved
	.dl int_spurious	; Spurious interrupt
	.dl int_irq1		; IRQ level 1
	.dl int_ext			; External interrupt (level 2)
	.dl int_irq3		; IRQ level 3
	.dl int_hblank		; H-Blank (level 4)
	.dl int_irq5		; IRQ level 5
	.dl int_vblank		; V-Blank (level 6)
	.dl int_irq7		; IRQ level 7
	.ds 16 * 4, 0		; TRAP vectors
	.ds 16 * 4, 0		; Reserved

; =============================================================================
; Header
; =============================================================================

.org $000100

header:
	.db "SEGA MEGA DRIVE "					; Console name
	.db "(C)YOUR 2025.JAN"					; Copyright
	.db "MY GAME                                         "	; Domestic name
	.db "MY GAME                                         "	; Overseas name
	.db "GM 00000000-00"					; Product code
	.dw $0000							; Checksum
	.db "J               "					; I/O support
	.dl $00000000						; ROM start
	.dl $000fffff						; ROM end
	.dl $00ff0000						; RAM start
	.dl $00ffffff						; RAM end
	.db "            "					; SRAM info
	.db "            "					; Modem info
	.db "                                        "		; Memo
	.db "JUE             "					; Region

; =============================================================================
; Entry Point
; =============================================================================

.org $000200

start:
	; Check for TMSS (Trademark Security System)
	move.b	IO_VER, d0
	andi.b	#$0f, d0
	beq.s	@skip_tmss
	move.l	#'SEGA', $a14000
@skip_tmss:

	; Stop the Z80
	move.w	#$0100, Z80_BUSREQ
	move.w	#$0100, Z80_RESET
@wait_z80:
	btst	#0, Z80_BUSREQ
	bne.s	@wait_z80

	; Initialize PSG (silence)
	move.b	#$9f, PSG			; Channel 0 off
	move.b	#$bf, PSG			; Channel 1 off
	move.b	#$df, PSG			; Channel 2 off
	move.b	#$ff, PSG			; Channel 3 off

	; Initialize controller ports
	move.b	#$40, IO_CTRL1
	move.b	#$40, IO_CTRL2
	move.b	#$40, IO_DATA1
	move.b	#$40, IO_DATA2

	; Initialize VDP
	lea		vdp_regs(pc), a0
	move.l	#VDP_CTRL, a1
	moveq	#23, d0
@init_vdp:
	move.w	(a0)+, (a1)
	dbf		d0, @init_vdp

	; Clear VRAM
	move.l	#$40000000, VDP_CTRL	; VRAM write $0000
	move.l	#VDP_DATA, a0
	move.w	#$7fff, d0
@clear_vram:
	move.w	#0, (a0)
	dbf		d0, @clear_vram

	; Clear CRAM
	move.l	#$c0000000, VDP_CTRL	; CRAM write $0000
	moveq	#31, d0
@clear_cram:
	move.w	#$0000, VDP_DATA
	dbf		d0, @clear_cram

	; Clear VSRAM
	move.l	#$40000010, VDP_CTRL	; VSRAM write $0000
	moveq	#39, d0
@clear_vsram:
	move.w	#$0000, VDP_DATA
	dbf		d0, @clear_vsram

	; Release Z80
	move.w	#$0000, Z80_RESET
	move.w	#$0000, Z80_BUSREQ

	; Enable display
	move.w	#$8144, VDP_CTRL		; Mode 5, display on

	; Enable interrupts
	move	#$2000, sr

main_loop:
	; Wait for VBlank
	move.w	#$8174, VDP_CTRL		; Enable VBlank IRQ
@wait_vblank:
	btst	#3, VDP_CTRL+1
	beq.s	@wait_vblank

	jsr		read_joypad
	jsr		update_game

	bra.s	main_loop

; =============================================================================
; VDP Register Initialization
; =============================================================================

vdp_regs:
	.dw $8004		; Mode 1: disable HBlank INT
	.dw $8134		; Mode 2: enable display, enable VBlank INT, DMA on
	.dw $8230		; Plane A at $c000
	.dw $8328		; Window at $a000
	.dw $8407		; Plane B at $e000
	.dw $8554		; Sprite table at $a800
	.dw $8600		; Unused
	.dw $8700		; Background color: palette 0, color 0
	.dw $8800		; Unused
	.dw $8900		; Unused
	.dw $8a00		; HBlank counter
	.dw $8b00		; Mode 3: full scroll
	.dw $8c81		; Mode 4: 40 cells, no interlace
	.dw $8d2e		; HScroll at $b800
	.dw $8e00		; Unused
	.dw $8f02		; Auto-increment: 2
	.dw $9001		; Scroll size: 64x32
	.dw $9100		; Window X
	.dw $9200		; Window Y
	.dw $93ff		; DMA length low
	.dw $94ff		; DMA length high
	.dw $9500		; DMA source low
	.dw $9600		; DMA source mid
	.dw $9700		; DMA source high

; =============================================================================
; Read Joypad
; =============================================================================

read_joypad:
	move.b	#$40, IO_DATA1
	nop
	nop
	move.b	IO_DATA1, d0		; Start, A, 0, 0, Down, Up, 0, 0

	move.b	#$00, IO_DATA1
	nop
	nop
	move.b	IO_DATA1, d1		; C, B, 0, 0, Down, Up, Left, Right

	andi.w	#$003f, d0
	andi.w	#$003f, d1
	lsl.w	#6, d0
	or.w	d1, d0

	move.w	d0, joypad_state
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

int_bus_error:
int_addr_error:
int_illegal:
int_div_zero:
int_chk:
int_trapv:
int_priv:
int_trace:
int_line_a:
int_line_f:
int_spurious:
int_irq1:
int_ext:
int_irq3:
int_hblank:
int_irq5:
int_irq7:
	rte

int_vblank:
	; TODO: VBlank processing
	rte

; =============================================================================
; Variables
; =============================================================================

.org $ff0000

joypad_state:
	.ds 2
