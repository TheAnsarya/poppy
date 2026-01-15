; ============================================================================
; SNES Hello World
; Minimal SNES initialization and setup
; ============================================================================

.target snes

; ROM Header Configuration
.snes_name "HELLO WORLD"
.snes_map_mode LOROM           ; LoROM mapping
.snes_rom_speed SLOW           ; SlowROM
.snes_rom_type ROM             ; No special chips
.snes_rom_size 3               ; 256KB
.snes_ram_size 0               ; No SRAM
.snes_region NORTH_AMERICA     ; NTSC
.snes_developer "HW"           ; Developer ID
.snes_version 0                ; Version 1.0

; ============================================================================
; Hardware Registers
; ============================================================================

; PPU registers
INIDISP  = $2100               ; Screen Display
BGMODE   = $2105               ; BG Mode and Character Size
TM       = $212c               ; Main Screen Designation
NMITIMEN = $4200               ; Interrupt Enable

; CPU registers
MEMSEL   = $420d               ; ROM Speed

; ============================================================================
; Entry Points
; ============================================================================

; Native mode reset vector
.org $808000
reset:
	; Switch to native mode
	clc                        ; Clear carry for native mode
	xce                        ; Exchange carry with emulation flag
	
	; Set up processor state
	sei                        ; Disable interrupts
	
	; Set 16-bit accumulator and index registers
	rep #$30                   ; Clear M and X flags (16-bit mode)
	
	; Set up stack
	lda #$1fff                 ; Stack at top of RAM
	tcs                        ; Transfer to stack pointer
	
	; Initialize hardware
	jsr init_snes
	
	; Main loop
@loop:
	wai                        ; Wait for interrupt
	bra @loop

; ============================================================================
; SNES Initialization
; ============================================================================

init_snes:
	; Disable interrupts and DMA
	sep #$20                   ; 8-bit accumulator
	lda #$00
	sta NMITIMEN               ; Disable NMI and IRQ
	
	; Force blank (screen off)
	lda #$80
	sta INIDISP                ; Force blank
	
	; Set video mode
	lda #$09                   ; Mode 1, BG3 priority
	sta BGMODE
	
	; Clear all PPU registers
	; This is a simplified version - full init would clear more
	ldx #$2100
@clear_ppu:
	stz $00,x                  ; Zero out register
	inx
	cpx #$2134
	bne @clear_ppu
	
	; Set ROM speed to FastROM if available
	lda #$01
	sta MEMSEL
	
	; Enable main screen layers
	lda #$01                   ; Enable BG1
	sta TM
	
	; Turn on screen (end force blank)
	lda #$0f                   ; Full brightness
	sta INIDISP
	
	rep #$30                   ; Back to 16-bit mode
	rts

; ============================================================================
; Interrupt Handlers
; ============================================================================

; NMI (VBlank) handler
nmi_handler:
	rti

; IRQ handler
irq_handler:
	rti

; COP handler
cop_handler:
	rti

; BRK handler
brk_handler:
	rti

; ============================================================================
; Interrupt Vectors (Native Mode)
; ============================================================================

.org $80ffe0                   ; Native mode vectors (LoROM: $00ffe0 maps to $80ffe0)

; Unused vectors
.word $0000                    ; Reserved
.word $0000                    ; Reserved
.word cop_handler              ; COP
.word brk_handler              ; BRK
.word $0000                    ; ABORT
.word nmi_handler              ; NMI
.word reset                    ; RESET (unused in native)
.word irq_handler              ; IRQ/BRK

; Emulation mode vectors
.org $80fff0                   ; Emulation mode vectors
.word $0000                    ; Reserved
.word $0000                    ; Reserved
.word cop_handler              ; COP
.word $0000                    ; Reserved (unused)
.word $0000                    ; ABORT
.word nmi_handler              ; NMI
.word reset                    ; RESET (startup vector)
.word irq_handler              ; IRQ/BRK
