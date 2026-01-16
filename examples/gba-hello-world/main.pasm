; 🌸 Game Boy Advance Hello World - Poppy Compiler Example
; Displays a colored screen using Mode 3 bitmap

	.target gba
	.thumb			; Use Thumb mode for efficiency

; ═══════════════════════════════════════════════════════════════════════════
; GBA Header
; ═══════════════════════════════════════════════════════════════════════════

	.org $08000000		; ROM start
Header:
	b	EntryPoint	; Branch to code (ARM)

	; Nintendo logo (required for boot)
	.org $08000004
	.fill 156, $00		; Logo placeholder

	; Game title (12 chars)
	.org $080000a0
	.db "HELLOPOPPY  "

	; Game code (4 chars)
	.db "HPOP"

	; Maker code (2 chars)
	.db "PP"

	; Fixed value
	.db $96

	; Main unit code
	.db $00

	; Device type
	.db $00

	; Reserved
	.fill 7, $00

	; Software version
	.db $00

	; Complement check (calculate later)
	.db $00

	; Reserved
	.dw $0000

; ═══════════════════════════════════════════════════════════════════════════
; Memory Mapped I/O
; ═══════════════════════════════════════════════════════════════════════════

REG_DISPCNT	= $04000000	; Display control
REG_DISPSTAT	= $04000004	; Display status
REG_VCOUNT	= $04000006	; Vertical count
REG_BG2CNT	= $0400000c	; BG2 control
VRAM		= $06000000	; Video RAM (96KB)

; Display control flags
DCNT_MODE3	= $0003		; Mode 3: 240x160 bitmap, 15-bit color
DCNT_BG2	= $0400		; Enable BG2

; ═══════════════════════════════════════════════════════════════════════════
; Entry Point
; ═══════════════════════════════════════════════════════════════════════════

	.org $080000c0
	.arm			; ARM mode for entry
EntryPoint:
	; Set up display: Mode 3, BG2 enabled
	ldr	r0, =REG_DISPCNT
	ldr	r1, =(DCNT_MODE3 | DCNT_BG2)
	strh	r1, [r0]

	; Fill screen with gradient
	ldr	r0, =VRAM
	ldr	r1, =240*160		; Screen size in pixels
	mov	r2, #0			; Starting color

.fillLoop:
	strh	r2, [r0], #2		; Write pixel, increment pointer
	add	r2, r2, #1		; Next color
	and	r2, r2, #$7fff		; Wrap at 15-bit max
	subs	r1, r1, #1
	bne	.fillLoop

	; Main loop - wait forever
MainLoop:
	; Wait for VBlank
	ldr	r0, =REG_DISPSTAT
.waitVBlank:
	ldrh	r1, [r0]
	tst	r1, #1			; Check VBlank flag
	beq	.waitVBlank

	b	MainLoop

; ═══════════════════════════════════════════════════════════════════════════
; ROM Padding
; ═══════════════════════════════════════════════════════════════════════════

	.org $08000200
	.fill $100, $ff			; Padding to minimum size
