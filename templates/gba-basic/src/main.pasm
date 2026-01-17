; =============================================================================
; Game Boy Advance Basic Template
; =============================================================================

.system:gameboya"
.cpu "arm7tdmi"

; =============================================================================
; Hardware Constants
; =============================================================================

; Display Control
REG_DISPCNT		= $4000000
REG_DISPSTAT	= $4000004
REG_VCOUNT		= $4000006

; Background Control
REG_BG0CNT		= $4000008
REG_BG1CNT		= $400000a
REG_BG2CNT		= $400000c
REG_BG3CNT		= $400000e

; Background Scroll
REG_BG0HOFS		= $4000010
REG_BG0VOFS		= $4000012
REG_BG1HOFS		= $4000014
REG_BG1VOFS		= $4000016
REG_BG2HOFS		= $4000018
REG_BG2VOFS		= $400001a
REG_BG3HOFS		= $400001c
REG_BG3VOFS		= $400001e

; Input
REG_KEYINPUT	= $4000130

; Interrupt Control
REG_IE			= $4000200
REG_IF			= $4000202
REG_IME			= $4000208

; Memory Regions
IWRAM			= $3000000	; Internal Work RAM (32KB)
EWRAM			= $2000000	; External Work RAM (256KB)
VRAM			= $6000000	; Video RAM (96KB)
PALETTE			= $5000000	; Palette RAM (1KB)
OAM				= $7000000	; Object Attribute Memory (1KB)

; BIOS Calls
SWI_VBLANKINTRWAIT	= $05

; =============================================================================
; ROM Header
; =============================================================================

.org $8000000

header:
	; Entry point (ARM branch)
	b		start

	; Nintendo logo (156 bytes, filled by Poppy)
	.ds 156, $00

	; Game title (12 bytes)
	.db "MYGAME", 0, 0, 0, 0, 0, 0

	; Game code (4 bytes)
	.db "AXXX"

	; Maker code (2 bytes)
	.db "01"

	; Fixed value
	.db $96

	; Unit code
	.db $00

	; Device type
	.db $00

	; Reserved
	.ds 7, $00

	; Software version
	.db $00

	; Header checksum (filled by Poppy)
	.db $00

	; Reserved
	.ds 2, $00

; =============================================================================
; Entry Point
; =============================================================================

.org $80000c0
.arm

start:
	; Set up IRQ handler
	ldr		r0, =irq_handler
	ldr		r1, =$3007ffc		; IRQ handler address
	str		r0, [r1]

	; Initialize stack pointers
	mov		r0, #$12			; IRQ mode
	msr		cpsr_c, r0
	ldr		sp, =$3007fa0		; IRQ stack

	mov		r0, #$1f			; System mode
	msr		cpsr_c, r0
	ldr		sp, =$3007f00		; User stack

	; Initialize display
	mov		r0, #$0000
	ldr		r1, =REG_DISPCNT
	strh	r0, [r1]			; Disable everything initially

	; Clear VRAM
	mov		r0, #0
	ldr		r1, =VRAM
	ldr		r2, =$18000			; 96KB
@clear_vram:
	str		r0, [r1], #4
	subs	r2, r2, #4
	bne		@clear_vram

	; Clear palette
	mov		r0, #0
	ldr		r1, =PALETTE
	mov		r2, #$400			; 1KB
@clear_pal:
	strh	r0, [r1], #2
	subs	r2, r2, #2
	bne		@clear_pal

	; Clear OAM (hide all sprites)
	mov		r0, #$200			; Y = 160 (off-screen)
	ldr		r1, =OAM
	mov		r2, #128			; 128 sprites
@clear_oam:
	strh	r0, [r1], #8		; Skip to next OBJ
	subs	r2, r2, #1
	bne		@clear_oam

	; Enable display (Mode 0, BG0 enabled)
	mov		r0, #$0100
	ldr		r1, =REG_DISPCNT
	strh	r0, [r1]

	; Enable VBlank interrupt
	mov		r0, #$0001
	ldr		r1, =REG_IE
	strh	r0, [r1]

	mov		r0, #$0008			; VBlank IRQ enable in DISPSTAT
	ldr		r1, =REG_DISPSTAT
	strh	r0, [r1]

	mov		r0, #$0001			; Master interrupt enable
	ldr		r1, =REG_IME
	strh	r0, [r1]

main_loop:
	; Wait for VBlank
	swi		SWI_VBLANKINTRWAIT

	bl		read_keys
	bl		update_game

	b		main_loop

; =============================================================================
; Read Keys
; =============================================================================

read_keys:
	ldr		r0, =REG_KEYINPUT
	ldrh	r0, [r0]
	mvn		r0, r0				; Invert (keys are active low)
	and		r0, r0, #$03ff		; Mask to 10 buttons

	ldr		r1, =key_state
	strh	r0, [r1]

	bx		lr

; =============================================================================
; Update Game
; =============================================================================

update_game:
	; TODO: Add game logic
	bx		lr

; =============================================================================
; IRQ Handler
; =============================================================================

irq_handler:
	; Save registers
	stmfd	sp!, {r0-r3, r12, lr}

	; Read and acknowledge interrupt
	ldr		r0, =REG_IF
	ldrh	r1, [r0]
	strh	r1, [r0]			; Acknowledge

	; Also acknowledge in BIOS flags
	ldr		r0, =$3007ff8
	ldrh	r2, [r0]
	orr		r2, r2, r1
	strh	r2, [r0]

	; Restore and return
	ldmfd	sp!, {r0-r3, r12, lr}
	subs	pc, lr, #4

; =============================================================================
; Variables (IWRAM)
; =============================================================================

.org $3000000

key_state:
	.ds 2
