; =============================================================================
; WonderSwan Basic Template
; =============================================================================

.system:wonderswan
.cpu "v30mz"

; =============================================================================
; Hardware Constants
; =============================================================================

; Display Control
DISP_CTRL	= $0000		; Display control
BACK_COLOR	= $0001		; Background color
LINE_CUR	= $0002		; Current scanline
LINE_CMP	= $0003		; Line compare (for IRQ)
SPR_BASE	= $0004		; Sprite table base
SPR_FIRST	= $0005		; First sprite
SPR_COUNT	= $0006		; Sprite count
MAP_BASE	= $0007		; Tilemap base addresses

SCR1_X		= $0010		; Screen 1 X scroll
SCR1_Y		= $0011		; Screen 1 Y scroll
SCR2_X		= $0012		; Screen 2 X scroll
SCR2_Y		= $0013		; Screen 2 Y scroll

; LCD Control
LCD_CTRL	= $0014		; LCD control (sleep)
LCD_ICON	= $0015		; LCD icons

; Palette
PAL_MONO	= $001c		; Monochrome palette (8 sets × 4 colors)

; DMA
DMA_SRC_L	= $0040		; DMA source low
DMA_SRC_H	= $0041		; DMA source high
DMA_SRC_B	= $0042		; DMA source bank
DMA_DST_L	= $0044		; DMA destination low
DMA_DST_H	= $0045		; DMA destination high
DMA_LEN_L	= $0046		; DMA length low
DMA_LEN_H	= $0047		; DMA length high
DMA_CTRL	= $0048		; DMA control

; Sound
SND_CH1_L	= $0080		; Channel 1 frequency low
SND_CH1_H	= $0081		; Channel 1 frequency high
SND_CH2_L	= $0082		; Channel 2
SND_CH2_H	= $0083
SND_CH3_L	= $0084		; Channel 3
SND_CH3_H	= $0085
SND_CH4_L	= $0086		; Channel 4
SND_CH4_H	= $0087
SND_VOL		= $0088		; Channel volumes (2 bits each)
SND_VOL2	= $0089		; More volume
SND_SWEEP	= $008a		; Sweep value
SND_SWEEP_T	= $008b		; Sweep time
SND_NOISE	= $008c		; Noise control
SND_WAVE_B	= $008e		; Wavetable base
SND_CTRL	= $008f		; Sound control
SND_OUTPUT	= $0090		; Sound output control
SND_RNOISE	= $0092		; Random noise
SND_VOICE	= $0094		; Voice volume

; System Control
SYS_CTRL1	= $0060		; System control 1
SYS_CTRL2	= $00a0		; System control 2
SYS_CTRL3	= $00b0		; System control 3

; Input
KEY_CTRL	= $00b5		; Keypad control

; Interrupts
INT_BASE	= $00b0		; Interrupt base
INT_ENABLE	= $00b2		; Interrupt enable
INT_STATUS	= $00b4		; Interrupt status/acknowledge

; Timer
TIMER_CTRL	= $00a2		; Timer control
HBLANK_PRE	= $00a4		; HBlank timer preset
VBLANK_PRE	= $00a6		; VBlank timer preset
HBLANK_CNT	= $00a8		; HBlank counter
VBLANK_CNT	= $00aa		; VBlank counter

; Memory
IRAM		= $0000		; Internal RAM (segment 0)
VRAM_BASE	= $2000		; Video RAM start

; =============================================================================
; ROM Header (at end of ROM)
; =============================================================================

.org $ffff0

header:
	jmp far start		; Reset vector (5 bytes)
	.ds 5, $00			; Padding
	.db $ff				; Publisher ID
	.db $00				; Color flag
	.db $00				; Game ID
	.db $01				; Version
	.db $01				; ROM size (1 = 2Mbit)
	.db $00				; Save type
	.db $00				; Flags
	.dw $0000			; Checksum (filled by Poppy)

; =============================================================================
; Main Code
; =============================================================================

.org $0000

start:
	cli
	cld

	; Set up data segment
	mov ax, $0000
	mov ds, ax

	; Initialize stack
	mov ax, $0000
	mov ss, ax
	mov sp, $2000

	; Initialize display
	call init_display

	; Clear VRAM
	call clear_vram

	; Enable VBlank interrupt
	mov al, $01			; VBlank
	out INT_ENABLE, al

	sti

main_loop:
	; Wait for VBlank
	hlt

	call read_keys
	call update_game

	jmp main_loop

; =============================================================================
; Initialize Display
; =============================================================================

init_display:
	; Disable display during setup
	mov al, $00
	out DISP_CTRL, al

	; Set background color
	mov al, $00
	out BACK_COLOR, al

	; Set tilemap bases
	mov al, $00			; SCR1 at $2000, SCR2 at $2800
	out MAP_BASE, al

	; Set sprite table
	mov al, $00
	out SPR_BASE, al
	mov al, 0
	out SPR_FIRST, al
	mov al, 0
	out SPR_COUNT, al

	; Initialize mono palettes (4 shades)
	mov al, %11100100	; 3-2-1-0
	out PAL_MONO+$00, al
	out PAL_MONO+$02, al
	out PAL_MONO+$04, al
	out PAL_MONO+$06, al

	; Enable display (Screen 1 on)
	mov al, $01
	out DISP_CTRL, al

	ret

; =============================================================================
; Clear VRAM
; =============================================================================

clear_vram:
	; Set up DMA to clear VRAM
	; Use CPU loop for simplicity
	mov ax, VRAM_BASE
	mov di, ax
	mov cx, $4000		; 16KB
	xor ax, ax
@loop:
	mov [di], al
	inc di
	loop @loop

	ret

; =============================================================================
; Read Keys
; =============================================================================

read_keys:
	; Select Y keys (D-pad)
	mov al, $10
	out KEY_CTRL, al
	nop
	nop
	in al, KEY_CTRL
	and al, $0f
	mov ah, al

	; Select X keys (buttons)
	mov al, $20
	out KEY_CTRL, al
	nop
	nop
	in al, KEY_CTRL
	shl al, 4
	or al, ah

	mov [key_state], al
	ret

; =============================================================================
; Update Game
; =============================================================================

update_game:
	; TODO: Add game logic
	ret

; =============================================================================
; Interrupt Handlers
; =============================================================================

.org $0080

int_handler:
	push ax

	; Acknowledge interrupt
	in al, INT_STATUS
	out INT_STATUS, al

	pop ax
	iret

; =============================================================================
; Variables (IRAM)
; =============================================================================

.org $0100

key_state:
	.ds 1

vblank_flag:
	.ds 1
