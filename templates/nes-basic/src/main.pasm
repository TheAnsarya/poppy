; =============================================================================
; NES Basic Template - Main Entry Point
; =============================================================================
; A minimal NES ROM skeleton ready for your game code
; =============================================================================

.include "constants.pasm"

; =============================================================================
; Zero Page Variables ($00-$ff)
; =============================================================================

.enum $00
	frame_counter:	.ds 1	; Frame counter
	nmi_ready:		.ds 1	; NMI ready flag
	controller1:	.ds 1	; Controller 1 state
	controller2:	.ds 1	; Controller 2 state
	temp:			.ds 4	; Temporary variables
.ende

; =============================================================================
; OAM Buffer ($0200-$02ff)
; =============================================================================

OAM_BUF = $0200

; =============================================================================
; PRG-ROM Bank 0 (16KB at $c000)
; =============================================================================

.org $c000

; =============================================================================
; NMI Handler - Called every VBlank
; =============================================================================

nmi:
	; Save registers
	pha
	txa
	pha
	tya
	pha

	; Check if we're ready for NMI processing
	lda nmi_ready
	beq @skip_nmi

	; Sprite DMA
	lda #$00
	sta OAMADDR
	lda #$02
	sta OAMDMA

	; Reset scroll
	lda #$00
	sta PPUSCROLL
	sta PPUSCROLL

	; Increment frame counter
	inc frame_counter

	; Clear ready flag
	lda #$00
	sta nmi_ready

@skip_nmi:
	; Restore registers
	pla
	tay
	pla
	tax
	pla
	rti

; =============================================================================
; IRQ Handler
; =============================================================================

irq:
	rti

; =============================================================================
; Reset Handler - Entry Point
; =============================================================================

reset:
	; Disable interrupts
	sei
	cld

	; Disable APU frame IRQ
	ldx #$40
	stx APU_FRAME

	; Initialize stack
	ldx #$ff
	txs

	; Disable NMI and rendering
	inx					; X = 0
	stx PPUCTRL
	stx PPUMASK
	stx APU_STATUS		; Disable APU

	; Wait for first VBlank
@vblank1:
	bit PPUSTATUS
	bpl @vblank1

	; Clear RAM
	lda #$00
@clear_ram:
	sta $0000, x
	sta $0100, x
	sta $0200, x
	sta $0300, x
	sta $0400, x
	sta $0500, x
	sta $0600, x
	sta $0700, x
	inx
	bne @clear_ram

	; Wait for second VBlank (PPU warmup)
@vblank2:
	bit PPUSTATUS
	bpl @vblank2

	; Initialize OAM buffer (hide all sprites)
	lda #$ff
	ldx #$00
@init_oam:
	sta OAM_BUF, x
	inx
	bne @init_oam

	; Initialize PPU
	jsr init_ppu

	; Initialize game state
	jsr init_game

	; Enable NMI
	lda #PPUCTRL_NMI
	sta PPUCTRL

	; Enable rendering
	lda #(PPUMASK_BG_ON | PPUMASK_SPR_ON | PPUMASK_BG_LEFT | PPUMASK_SPR_LEFT)
	sta PPUMASK

; =============================================================================
; Main Loop
; =============================================================================

main_loop:
	; Wait for NMI
	lda #$01
	sta nmi_ready
@wait_nmi:
	lda nmi_ready
	bne @wait_nmi

	; Read controllers
	jsr read_controllers

	; Update game logic
	jsr update_game

	; Loop forever
	jmp main_loop

; =============================================================================
; Initialize PPU
; =============================================================================

init_ppu:
	; Wait for VBlank
	bit PPUSTATUS

	; Set PPU address to palette ($3f00)
	lda #$3f
	sta PPUADDR
	lda #$00
	sta PPUADDR

	; Load palette
	ldx #$00
@load_palette:
	lda palette_data, x
	sta PPUDATA
	inx
	cpx #$20
	bne @load_palette

	; Clear nametable 0
	lda #$20
	sta PPUADDR
	lda #$00
	sta PPUADDR

	lda #$00
	ldx #$00
	ldy #$04
@clear_nt:
	sta PPUDATA
	inx
	bne @clear_nt
	dey
	bne @clear_nt

	rts

; =============================================================================
; Initialize Game State
; =============================================================================

init_game:
	; TODO: Add your game initialization here
	rts

; =============================================================================
; Read Controllers
; =============================================================================

read_controllers:
	; Latch controllers
	lda #$01
	sta JOYPAD1
	lda #$00
	sta JOYPAD1

	; Read 8 buttons
	ldx #$08
@read_loop:
	lda JOYPAD1
	lsr a
	rol controller1
	dex
	bne @read_loop

	rts

; =============================================================================
; Update Game Logic
; =============================================================================

update_game:
	; TODO: Add your game logic here
	rts

; =============================================================================
; Data Section
; =============================================================================

palette_data:
	; Background palette
	.db $0f, $00, $10, $30	; Palette 0: Black, gray, light gray, white
	.db $0f, $06, $16, $26	; Palette 1: Reds
	.db $0f, $09, $19, $29	; Palette 2: Greens
	.db $0f, $02, $12, $22	; Palette 3: Blues

	; Sprite palette
	.db $0f, $00, $10, $30	; Palette 0
	.db $0f, $06, $16, $26	; Palette 1
	.db $0f, $09, $19, $29	; Palette 2
	.db $0f, $02, $12, $22	; Palette 3

; =============================================================================
; Vectors
; =============================================================================

.org $fffa
	.dw nmi		; NMI vector
	.dw reset	; Reset vector
	.dw irq		; IRQ/BRK vector

; =============================================================================
; CHR-ROM (8KB of pattern data)
; =============================================================================

.org $10000
	; CHR bank 0 - Pattern tables
	; TODO: Include your CHR data here
	; .incbin "graphics.chr"
	.fill $2000, $00	; 8KB of blank tiles
