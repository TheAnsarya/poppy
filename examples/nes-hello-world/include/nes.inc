; ============================================================================
; nes.inc - NES Hardware Definitions
; Poppy Assembly - Common Include File
; ============================================================================
; This file contains standard NES register definitions and constants.
; Include with: .include "include/nes.inc"
; ============================================================================

; ============================================================================
; PPU Registers ($2000-$2007)
; ============================================================================
PPU_CTRL    = $2000     ; PPU Control Register
PPU_MASK    = $2001     ; PPU Mask Register
PPU_STATUS  = $2002     ; PPU Status Register (read)
OAM_ADDR    = $2003     ; OAM Address
OAM_DATA    = $2004     ; OAM Data
PPU_SCROLL  = $2005     ; PPU Scroll Position
PPU_ADDR    = $2006     ; PPU Address Register
PPU_DATA    = $2007     ; PPU Data Register

; OAM DMA Register
OAM_DMA     = $4014     ; OAM DMA (write page address)

; ============================================================================
; PPU Control Bits ($2000)
; ============================================================================
PPUCTRL_NMI         = %10000000     ; Enable NMI on vblank
PPUCTRL_MASTER      = %01000000     ; PPU master/slave (unused)
PPUCTRL_SPR_HEIGHT  = %00100000     ; Sprite height (0=8x8, 1=8x16)
PPUCTRL_BG_TABLE    = %00010000     ; BG pattern table (0=$0000, 1=$1000)
PPUCTRL_SPR_TABLE   = %00001000     ; Sprite pattern table
PPUCTRL_VRAM_INC    = %00000100     ; VRAM increment (0=+1, 1=+32)
PPUCTRL_NAMETABLE   = %00000011     ; Base nametable address

; ============================================================================
; PPU Mask Bits ($2001)
; ============================================================================
PPUMASK_BLUE        = %10000000     ; Emphasize blue
PPUMASK_GREEN       = %01000000     ; Emphasize green
PPUMASK_RED         = %00100000     ; Emphasize red
PPUMASK_SPR_ENABLE  = %00010000     ; Show sprites
PPUMASK_BG_ENABLE   = %00001000     ; Show background
PPUMASK_SPR_LEFT    = %00000100     ; Show sprites in leftmost 8 pixels
PPUMASK_BG_LEFT     = %00000010     ; Show background in leftmost 8 pixels
PPUMASK_GRAYSCALE   = %00000001     ; Grayscale mode

; ============================================================================
; APU Registers ($4000-$4017)
; ============================================================================
; Pulse 1 (Square Wave)
APU_PULSE1_VOL      = $4000     ; Volume/envelope
APU_PULSE1_SWEEP    = $4001     ; Sweep control
APU_PULSE1_LO       = $4002     ; Timer low
APU_PULSE1_HI       = $4003     ; Timer high / length counter

; Pulse 2 (Square Wave)
APU_PULSE2_VOL      = $4004
APU_PULSE2_SWEEP    = $4005
APU_PULSE2_LO       = $4006
APU_PULSE2_HI       = $4007

; Triangle
APU_TRI_LINEAR      = $4008     ; Linear counter
APU_TRI_LO          = $400a     ; Timer low
APU_TRI_HI          = $400b     ; Timer high / length counter

; Noise
APU_NOISE_VOL       = $400c     ; Volume/envelope
APU_NOISE_PERIOD    = $400e     ; Period and mode
APU_NOISE_LEN       = $400f     ; Length counter

; DMC (Delta Modulation)
APU_DMC_CTRL        = $4010     ; Flags and rate
APU_DMC_LOAD        = $4011     ; Direct load
APU_DMC_ADDR        = $4012     ; Sample address
APU_DMC_LEN         = $4013     ; Sample length

; APU Control and Status
APU_STATUS          = $4015     ; Channel enable and status
APU_FRAME_CTR       = $4017     ; Frame counter mode

; ============================================================================
; Controller Ports ($4016-$4017)
; ============================================================================
JOYPAD1     = $4016     ; Controller 1 (read/write)
JOYPAD2     = $4017     ; Controller 2 (read only)

; Controller button masks
BTN_A       = %10000000
BTN_B       = %01000000
BTN_SELECT  = %00100000
BTN_START   = %00010000
BTN_UP      = %00001000
BTN_DOWN    = %00000100
BTN_LEFT    = %00000010
BTN_RIGHT   = %00000001

; ============================================================================
; Memory Map
; ============================================================================
RAM_START   = $0000     ; Internal RAM start
RAM_END     = $07ff     ; Internal RAM end (2KB)
RAM_SIZE    = $0800     ; 2KB

STACK_PAGE  = $0100     ; Stack page
OAM_PAGE    = $0200     ; Typical OAM shadow location

PPU_START   = $2000     ; PPU register start
PPU_END     = $3fff     ; PPU register end (mirrored)

APU_START   = $4000     ; APU/IO start
APU_END     = $401f     ; APU/IO end

CART_START  = $4020     ; Cartridge space start
PRG_ROM     = $8000     ; PRG-ROM start (typical)
PRG_END     = $ffff     ; PRG-ROM end

; ============================================================================
; VRAM Map
; ============================================================================
PATTERN_0   = $0000     ; Pattern Table 0 (CHR)
PATTERN_1   = $1000     ; Pattern Table 1 (CHR)
NAMETABLE_0 = $2000     ; Nametable 0
NAMETABLE_1 = $2400     ; Nametable 1
NAMETABLE_2 = $2800     ; Nametable 2
NAMETABLE_3 = $2c00     ; Nametable 3
PALETTE     = $3f00     ; Palette RAM
PALETTE_BG  = $3f00     ; Background palettes
PALETTE_SPR = $3f10     ; Sprite palettes

; ============================================================================
; Common Constants
; ============================================================================
SCREEN_WIDTH    = 256
SCREEN_HEIGHT   = 240
TILES_X         = 32    ; Tiles per row
TILES_Y         = 30    ; Tiles per column (visible)
TILE_SIZE       = 8     ; Pixels per tile
