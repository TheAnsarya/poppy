; 🌸 Sega Genesis Hello World - Poppy Compiler Example
; Displays "HELLO WORLD" using VDP text mode

	.system:genesis
	.org $000000

; ═══════════════════════════════════════════════════════════════════════════
; ROM Header
; ═══════════════════════════════════════════════════════════════════════════

	.org $000000
Vectors:
	.dl $00ffe000		; Initial stack pointer
	.dl EntryPoint		; Reset vector
	.dl Exception		; Bus error
	.dl Exception		; Address error
	.dl Exception		; Illegal instruction
	.dl Exception		; Divide by zero
	.dl Exception		; CHK exception
	.dl Exception		; TRAPV exception
	.dl Exception		; Privilege violation
	.dl Exception		; Trace
	.dl Exception		; Line A emulator
	.dl Exception		; Line F emulator
	.fill 12, $00000000	; Reserved
	.dl Exception		; Spurious interrupt
	.dl Exception		; Level 1 interrupt (unused)
	.dl Exception		; Level 2 interrupt (external)
	.dl Exception		; Level 3 interrupt (unused)
	.dl HBlank		; Level 4 interrupt (horizontal)
	.dl Exception		; Level 5 interrupt (unused)
	.dl VBlank		; Level 6 interrupt (vertical)
	.dl Exception		; Level 7 interrupt (unused)
	.fill 16, $00000000	; TRAP vectors

	.org $000100
Header:
	.db "SEGA GENESIS    "	; Console name
	.db "(C)POPPY 2026   "	; Copyright
	.db "HELLO WORLD DEMO"	; Domestic name
	.fill 32, $20		; Padding
	.db "HELLO WORLD DEMO"	; Overseas name
	.fill 32, $20		; Padding
	.db "GM 00000000-00"	; Serial/version
	.dw $0000		; Checksum (calculate later)
	.db "J               "	; I/O support
	.dl $00000000		; ROM start
	.dl $0003ffff		; ROM end
	.dl $00ff0000		; RAM start
	.dl $00ffffff		; RAM end
	.fill 12, $20		; SRAM info
	.fill 40, $20		; Modem info
	.fill 40, $20		; Notes
	.db "JUE             "	; Region

; ═══════════════════════════════════════════════════════════════════════════
; VDP Registers
; ═══════════════════════════════════════════════════════════════════════════

VDP_DATA	= $c00000
VDP_CTRL	= $c00004

; ═══════════════════════════════════════════════════════════════════════════
; Entry Point
; ═══════════════════════════════════════════════════════════════════════════

	.org $000200
EntryPoint:
	; TMSS handshake (Trademark Security System)
	move.b	($a10001), d0
	andi.b	#$0f, d0
	beq.s	.skipTMSS
	move.l	#'SEGA', ($a14000)
.skipTMSS:

	; Initialize VDP
	lea	VDP_CTRL, a0
	lea	VDPInitData, a1
	moveq	#18, d0
.vdpLoop:
	move.w	(a1)+, (a0)
	dbra	d0, .vdpLoop

	; Clear VRAM
	move.l	#$40000000, (a0)	; VRAM write to $0000
	lea	VDP_DATA, a0
	move.w	#$3fff, d0
.clearVram:
	move.w	#0, (a0)
	dbra	d0, .clearVram

	; Enable display
	lea	VDP_CTRL, a0
	move.w	#$8144, (a0)		; Mode 2: Display on

MainLoop:
	jmp	MainLoop

; ═══════════════════════════════════════════════════════════════════════════
; Interrupt Handlers
; ═══════════════════════════════════════════════════════════════════════════

Exception:
HBlank:
VBlank:
	rte

; ═══════════════════════════════════════════════════════════════════════════
; VDP Initialization Data
; ═══════════════════════════════════════════════════════════════════════════

VDPInitData:
	.dw $8004		; Reg 0: H-int off, HV counter on
	.dw $8114		; Reg 1: Display off, V-int off, DMA on, 224 lines
	.dw $8230		; Reg 2: Plane A at $c000
	.dw $832c		; Reg 3: Window at $b000
	.dw $8407		; Reg 4: Plane B at $e000
	.dw $857c		; Reg 5: Sprite table at $f800
	.dw $8600		; Reg 6: Unused
	.dw $8700		; Reg 7: Background color 0
	.dw $8800		; Reg 8: Unused
	.dw $8900		; Reg 9: Unused
	.dw $8a00		; Reg 10: H-int counter
	.dw $8b00		; Reg 11: Full scroll, no ext int
	.dw $8c81		; Reg 12: 320px wide, no shadow
	.dw $8d3f		; Reg 13: HScroll at $fc00
	.dw $8e00		; Reg 14: Unused
	.dw $8f02		; Reg 15: Auto-increment 2
	.dw $9001		; Reg 16: 64x32 plane size
	.dw $9100		; Reg 17: Window H pos
	.dw $9200		; Reg 18: Window V pos
