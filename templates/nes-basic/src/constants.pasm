; NES Hardware Constants
; Standard memory-mapped registers for the NES

; =============================================================================
; PPU Registers ($2000-$2007)
; =============================================================================

PPUCTRL		= $2000		; PPU control register
PPUMASK		= $2001		; PPU mask register
PPUSTATUS	= $2002		; PPU status register
OAMADDR		= $2003		; OAM address
OAMDATA		= $2004		; OAM data
PPUSCROLL	= $2005		; PPU scroll position
PPUADDR		= $2006		; PPU address
PPUDATA		= $2007		; PPU data

; =============================================================================
; APU Registers ($4000-$4017)
; =============================================================================

; Pulse 1
APU_PULSE1_VOL		= $4000		; Volume/envelope
APU_PULSE1_SWEEP	= $4001		; Sweep control
APU_PULSE1_LO		= $4002		; Period low
APU_PULSE1_HI		= $4003		; Period high/length

; Pulse 2
APU_PULSE2_VOL		= $4004
APU_PULSE2_SWEEP	= $4005
APU_PULSE2_LO		= $4006
APU_PULSE2_HI		= $4007

; Triangle
APU_TRI_LINEAR		= $4008		; Linear counter
APU_TRI_LO			= $400a		; Period low
APU_TRI_HI			= $400b		; Period high/length

; Noise
APU_NOISE_VOL		= $400c		; Volume/envelope
APU_NOISE_LO		= $400e		; Period/mode
APU_NOISE_HI		= $400f		; Length

; DMC
APU_DMC_FREQ		= $4010		; Frequency
APU_DMC_RAW			= $4011		; Direct load
APU_DMC_START		= $4012		; Sample address
APU_DMC_LEN			= $4013		; Sample length

; OAM DMA
OAMDMA		= $4014		; OAM DMA register

; APU Control
APU_STATUS	= $4015		; APU status/channel enable
APU_FRAME	= $4017		; APU frame counter

; =============================================================================
; Controller Registers
; =============================================================================

JOYPAD1		= $4016		; Controller 1
JOYPAD2		= $4017		; Controller 2

; Controller button masks
BTN_A		= %10000000
BTN_B		= %01000000
BTN_SELECT	= %00100000
BTN_START	= %00010000
BTN_UP		= %00001000
BTN_DOWN	= %00000100
BTN_LEFT	= %00000010
BTN_RIGHT	= %00000001

; =============================================================================
; PPU Control Flags
; =============================================================================

; PPUCTRL flags
PPUCTRL_NMI			= %10000000	; Enable NMI on VBlank
PPUCTRL_MASTER		= %01000000	; PPU master/slave (unused)
PPUCTRL_SPR_SIZE	= %00100000	; 8x16 sprites
PPUCTRL_BG_ADDR		= %00010000	; BG pattern table at $1000
PPUCTRL_SPR_ADDR	= %00001000	; Sprite pattern table at $1000
PPUCTRL_INC32		= %00000100	; VRAM increment 32 (down)
PPUCTRL_NT_2000		= %00000000	; Base nametable $2000
PPUCTRL_NT_2400		= %00000001	; Base nametable $2400
PPUCTRL_NT_2800		= %00000010	; Base nametable $2800
PPUCTRL_NT_2C00		= %00000011	; Base nametable $2C00

; PPUMASK flags
PPUMASK_BLUE		= %10000000	; Emphasize blue
PPUMASK_GREEN		= %01000000	; Emphasize green
PPUMASK_RED			= %00100000	; Emphasize red
PPUMASK_SPR_ON		= %00010000	; Show sprites
PPUMASK_BG_ON		= %00001000	; Show background
PPUMASK_SPR_LEFT	= %00000100	; Show sprites in left 8 pixels
PPUMASK_BG_LEFT		= %00000010	; Show BG in left 8 pixels
PPUMASK_GRAY		= %00000001	; Grayscale

; =============================================================================
; Memory Map
; =============================================================================

; RAM
RAM_START	= $0000
RAM_END		= $07ff
STACK		= $0100		; Stack page

; PPU
PPU_START	= $2000
PPU_END		= $3fff

; APU/IO
APU_START	= $4000
APU_END		= $401f

; Cartridge
PRG_START	= $8000
PRG_END		= $ffff

; Vectors
NMI_VECTOR		= $fffa
RESET_VECTOR	= $fffc
IRQ_VECTOR		= $fffe
