; ============================================================================
; NES Hello World - Poppy Assembly Example Project
; ============================================================================
; This example demonstrates basic Poppy features for NES development:
; - iNES header generation
; - Label usage (global, local, anonymous)
; - Macro definitions and invocations
; - Include files
; - Data directives
; ============================================================================

; Include common NES definitions
.include "include/nes.inc"

; ============================================================================
; iNES Header Configuration
; ============================================================================
; This generates the 16-byte iNES header automatically
.ines {"mapper": 0, "prg": 2, "chr": 1, "mirroring": "horizontal"}

; ============================================================================
; Zero Page Variables ($00-$ff)
; ============================================================================
.org $00

frame_counter:  .ds 1       ; Frame counter for timing
temp:           .ds 2       ; Temporary storage
scroll_x:       .ds 1       ; Horizontal scroll position
scroll_y:       .ds 1       ; Vertical scroll position

; ============================================================================
; PRG-ROM Bank 0 ($8000-$bfff)
; ============================================================================
.org $8000

; ----------------------------------------------------------------------------
; Macro: wait_vblank
; Wait for the next vertical blank period
; ----------------------------------------------------------------------------
.macro wait_vblank
-:
    bit PPU_STATUS          ; Read $2002 to check vblank
    bpl -                   ; Loop until bit 7 is set
.endmacro

; ----------------------------------------------------------------------------
; Macro: ppu_addr
; Set the PPU address register
; Parameters: \1 = address (16-bit)
; ----------------------------------------------------------------------------
.macro ppu_addr, addr
    lda #>(addr)            ; High byte first
    sta PPU_ADDR
    lda #<(addr)            ; Then low byte
    sta PPU_ADDR
.endmacro

; ============================================================================
; Reset Handler - Entry Point
; ============================================================================
Reset:
    sei                     ; Disable interrupts
    cld                     ; Clear decimal mode (not used on NES)
    ldx #$40
    stx APU_FRAME_CTR       ; Disable APU frame IRQ
    ldx #$ff
    txs                     ; Initialize stack pointer

    ; Disable PPU during initialization
    inx                     ; X = 0
    stx PPU_CTRL            ; Disable NMI
    stx PPU_MASK            ; Disable rendering
    stx APU_DMC_CTRL        ; Disable DMC IRQs

    ; Wait for first vblank
    %wait_vblank

    ; Clear RAM ($0000-$07ff)
    lda #$00
    ldx #$00
.clear_ram:
    sta $0000, x
    sta $0100, x
    sta $0200, x
    sta $0300, x
    sta $0400, x
    sta $0500, x
    sta $0600, x
    sta $0700, x
    inx
    bne .clear_ram

    ; Wait for second vblank (PPU fully ready)
    %wait_vblank

    ; Initialize PPU
    jsr init_palette
    jsr init_nametable

    ; Enable rendering
    lda #%10010000          ; Enable NMI, sprites from Pattern Table 0
    sta PPU_CTRL
    lda #%00011110          ; Enable sprites and background
    sta PPU_MASK

; Main loop - runs continuously
.main_loop:
    ; Wait for NMI to complete frame processing
    lda frame_counter
-:
    cmp frame_counter
    beq -                   ; Wait until frame_counter changes

    ; Game logic goes here
    ; ...

    jmp .main_loop

; ============================================================================
; NMI Handler - Called every vblank
; ============================================================================
NMI:
    pha                     ; Save registers
    txa
    pha
    tya
    pha

    ; Increment frame counter
    inc frame_counter

    ; Update scroll position
    lda #$00
    sta PPU_SCROLL          ; X scroll
    sta PPU_SCROLL          ; Y scroll

    pla                     ; Restore registers
    tay
    pla
    tax
    pla
    rti

; ============================================================================
; IRQ Handler - Not used in this example
; ============================================================================
IRQ:
    rti

; ============================================================================
; Subroutines
; ============================================================================

; ----------------------------------------------------------------------------
; init_palette - Load palette data into PPU
; ----------------------------------------------------------------------------
init_palette:
    %ppu_addr $3f00         ; Palette RAM address

    ldx #$00
.loop:
    lda palette_data, x
    sta PPU_DATA
    inx
    cpx #32                 ; 32 palette entries
    bne .loop
    rts

; ----------------------------------------------------------------------------
; init_nametable - Draw "HELLO WORLD" on screen
; ----------------------------------------------------------------------------
init_nametable:
    ; Set address to center of screen (row 14, column 11)
    ; Nametable address = $2000 + (row * 32) + column
    ; $2000 + (14 * 32) + 11 = $21cb
    %ppu_addr $21cb

    ldx #$00
.loop:
    lda hello_text, x
    beq .done               ; Zero terminator
    sta PPU_DATA
    inx
    bne .loop               ; Always branches (text < 256 chars)
.done:
    rts

; ============================================================================
; Data Section
; ============================================================================

; Background and sprite palettes (32 bytes total)
palette_data:
    ; Background palettes (16 bytes)
    .db $0f, $00, $10, $20  ; Palette 0: Black, Gray, Lt Gray, White
    .db $0f, $06, $16, $26  ; Palette 1: Black, Red shades
    .db $0f, $08, $18, $28  ; Palette 2: Black, Yellow shades
    .db $0f, $0a, $1a, $2a  ; Palette 3: Black, Green shades

    ; Sprite palettes (16 bytes)
    .db $0f, $01, $11, $21  ; Palette 0: Black, Blue shades
    .db $0f, $06, $16, $26  ; Palette 1: Black, Red shades
    .db $0f, $08, $18, $28  ; Palette 2: Black, Yellow shades
    .db $0f, $0a, $1a, $2a  ; Palette 3: Black, Green shades

; "HELLO WORLD" text (tile indices assuming ASCII-like tileset)
; In a real project, you'd have CHR-ROM with these characters
hello_text:
    .db "HELLO WORLD!", $00

; ============================================================================
; Interrupt Vectors ($fffa-$ffff)
; ============================================================================
.org $fffa
    .dw NMI                 ; NMI vector
    .dw Reset               ; Reset vector
    .dw IRQ                 ; IRQ/BRK vector

; ============================================================================
; CHR-ROM Bank (Pattern Tables)
; ============================================================================
; In a real project, you would .incbin a CHR file here
; For this example, we'll leave it empty (would need actual graphics)
.org $10000
    .ds $2000               ; 8KB of CHR-ROM (placeholder)
