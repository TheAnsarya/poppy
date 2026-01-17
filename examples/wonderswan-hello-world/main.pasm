; 🌸 WonderSwan Hello World - Poppy Compiler Example
; Basic hardware initialization for WonderSwan

	.system:wonderswan
	.org $0000

; ═══════════════════════════════════════════════════════════════════════════
; WonderSwan Hardware Constants
; ═══════════════════════════════════════════════════════════════════════════

; Display registers (I/O ports)
DISP_CTRL	= $00		; Display control
BACK_COLOR	= $01		; Background color
LINE_CUR	= $02		; Current line
LINE_CMP	= $03		; Line compare
SPR_BASE	= $04		; Sprite table base
SPR_FIRST	= $05		; First sprite
SPR_COUNT	= $06		; Sprite count
MAP_BASE	= $07		; Screen map base
SCR2_WIN_X1	= $08		; Screen 2 window X1
SCR2_WIN_Y1	= $09		; Screen 2 window Y1
SCR2_WIN_X2	= $0a		; Screen 2 window X2
SCR2_WIN_Y2	= $0b		; Screen 2 window Y2
SPR_WIN_X1	= $0c		; Sprite window X1
SPR_WIN_Y1	= $0d		; Sprite window Y1
SPR_WIN_X2	= $0e		; Sprite window X2
SPR_WIN_Y2	= $0f		; Sprite window Y2

; Scroll registers
SCR1_SCRL_X	= $10		; Screen 1 scroll X
SCR1_SCRL_Y	= $11		; Screen 1 scroll Y
SCR2_SCRL_X	= $12		; Screen 2 scroll X
SCR2_SCRL_Y	= $13		; Screen 2 scroll Y

; LCD control
LCD_CTRL	= $14		; LCD control
LCD_ICON	= $15		; LCD icons

; Palette registers (start at $20)
PALETTE		= $20		; Palette RAM base

; DMA registers
DMA_SRC		= $40		; DMA source (3 bytes)
DMA_DST		= $44		; DMA destination (2 bytes)
DMA_LEN		= $46		; DMA length (2 bytes)
DMA_CTRL	= $48		; DMA control

; Sound registers
SND_CH1		= $80		; Sound channel 1
SND_CH2		= $88		; Sound channel 2
SND_CH3		= $90		; Sound channel 3
SND_CH4		= $98		; Sound channel 4
SND_CTRL	= $90		; Sound control
SND_OUTPUT	= $91		; Sound output control
SND_VOL		= $9e		; Sound volume

; System control
SYS_CTRL1	= $a0		; System control 1
SYS_CTRL2	= $a2		; System control 2
SYS_CTRL3	= $a4		; System control 3
BANK_ROM0	= $c0		; ROM bank 0
BANK_SRAM	= $c1		; SRAM bank
BANK_ROM1	= $c2		; ROM bank 1
BANK_ROM2	= $c3		; ROM bank 2

; Interrupt registers
INT_BASE	= $b0		; Interrupt vector base
INT_ENABLE	= $b2		; Interrupt enable
INT_STATUS	= $b4		; Interrupt status
INT_ACK		= $b6		; Interrupt acknowledge

; ═══════════════════════════════════════════════════════════════════════════
; ROM Code Start
; ═══════════════════════════════════════════════════════════════════════════

	.org $0000
Start:
	; Disable interrupts during init
	cli

	; Set up segment registers
	; CS is already set by hardware
	xor	ax, ax
	mov	ds, ax
	mov	es, ax

	; Set stack at end of IRAM
	mov	ax, $0000
	mov	ss, ax
	mov	sp, $3fff

	; Initialize display
	mov	al, $00
	out	DISP_CTRL, al		; Disable display

	; Set background color to blue
	mov	al, $11			; Blue palette index
	out	BACK_COLOR, al

	; Clear scrolling
	xor	al, al
	out	SCR1_SCRL_X, al
	out	SCR1_SCRL_Y, al
	out	SCR2_SCRL_X, al
	out	SCR2_SCRL_Y, al

	; Enable display
	mov	al, $01			; Enable BG layer
	out	DISP_CTRL, al

	; Enable interrupts
	sti

; ═══════════════════════════════════════════════════════════════════════════
; Main Loop
; ═══════════════════════════════════════════════════════════════════════════

MainLoop:
	; Wait for VBlank
	hlt
	jmp	MainLoop

; ═══════════════════════════════════════════════════════════════════════════
; Interrupt Handlers
; ═══════════════════════════════════════════════════════════════════════════

	.org $0100
VBlankHandler:
	push	ax
	; Acknowledge VBlank interrupt
	in	al, INT_STATUS
	and	al, $01
	out	INT_ACK, al
	pop	ax
	iret

; ═══════════════════════════════════════════════════════════════════════════
; ROM Footer (last 16 bytes before reset vector)
; ═══════════════════════════════════════════════════════════════════════════

	; Pad to near end of ROM
	.org $fff0

	; Jump to entry point
	jmp	$f000:Start

	; Publisher ID, Color mode, Game ID, etc. filled by ROM builder
	.fill 5, $00

	; Reset vector points to $FFFF:$0000 (handled by hardware)
