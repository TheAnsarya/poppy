; ============================================================================
; channelf.inc - Fairchild Channel F Hardware Definitions
; Poppy Compiler - Multi-System Assembly Compiler
; ============================================================================
;
; Fairchild F8 CPU @ ~1.7898 MHz
; 64-byte scratchpad RAM (registers r0-r63)
; 2KB cartridge ROM, system BIOS in separate ROMs
;
; Reference: http://www.seanriddle.com/chanelf.html
; ============================================================================

; ============================================================================
; Memory Map
; ============================================================================

; $0000-$03ff   BIOS ROM 1 (SL31253 / SL90025)
; $0400-$07ff   BIOS ROM 2 (SL31254 / Hockey+Tennis)
; $0800-$17ff   Cartridge ROM (4KB max)
; $2800-$2fff   System RAM (2KB)
; $3000-$37ff   Video RAM (2KB, 128x64 pixels, 2bpp)
; $3800-$38ff   I/O Registers (Ports)

; ============================================================================
; Memory Region Equates
; ============================================================================

BIOS1_START		= $0000
BIOS1_END		= $03ff
BIOS2_START		= $0400
BIOS2_END		= $07ff
CART_ROM_START	= $0800
CART_ROM_END	= $17ff
RAM_START		= $2800
RAM_END			= $2fff
VRAM_START		= $3000
VRAM_END		= $37ff
IO_START		= $3800
IO_END			= $38ff

; ============================================================================
; I/O Port Registers ($3800-$3803)
; ============================================================================

; Port 0 - Console buttons and controller hand-off
CH_F_PORT0		= $3800

; Port 1 - Right controller input
; Bits:
;   0 - Right
;   1 - Left
;   2 - Backward (pull)
;   3 - Forward (push)
;   4 - Counterclockwise (twist)
;   5 - Clockwise (twist)
;   6 - Push down
;   7 - Not used
CH_F_PORT1		= $3801

; Port 4 - Left controller input (active player 2)
; Same bit layout as Port 1
CH_F_PORT4		= $3804

; Port 5 - Buzzer / sound output
CH_F_PORT5		= $3805

; ============================================================================
; Console Panel Buttons (Port 0 bit masks)
; ============================================================================

BTN_TIME		= $01		; TIME button
BTN_MODE		= $02		; MODE button
BTN_HOLD		= $04		; HOLD button
BTN_START		= $08		; START button

; ============================================================================
; Controller Direction Masks (Port 1 / Port 4)
; ============================================================================

CTRL_RIGHT		= $01		; Right
CTRL_LEFT		= $02		; Left
CTRL_BACK		= $04		; Backward (pull)
CTRL_FWD		= $08		; Forward (push)
CTRL_CCW		= $10		; Counterclockwise twist
CTRL_CW			= $20		; Clockwise twist
CTRL_PUSH		= $40		; Push down (fire)

; ============================================================================
; Video RAM Constants
; ============================================================================

VRAM_WIDTH		= $80		; 128 pixels per row
VRAM_HEIGHT		= $40		; 64 rows
VRAM_BPP		= $02		; 2 bits per pixel (4 colors)
VRAM_ROW_BYTES	= $20		; 32 bytes per row (128 pixels / 4 pixels per byte)

; ============================================================================
; Color Palette (2-bit values)
; ============================================================================

COLOR_GREEN		= $00		; Background / green
COLOR_YELLOW	= $01		; Yellow
COLOR_BLUE		= $02		; Blue
COLOR_GREY		= $03		; Grey / white (varies by system)

; ============================================================================
; System Constants
; ============================================================================

RESET_VECTOR	= $0800		; Cartridge entry point
STACK_TOP		= $2fff		; Top of system RAM (no hardware stack)

; ============================================================================
; BIOS Entry Points (System I - Fairchild)
; ============================================================================

BIOS_CLEAR_SCREEN	= $00d0	; Clear video RAM
BIOS_DELAY			= $008f	; Delay loop
BIOS_PUSH_K			= $0107	; Push accumulator to scratchpad

; ============================================================================
; F8 CPU Scratchpad Registers (r0-r63)
; ============================================================================
; The F8 CPU has 64 general-purpose 8-bit registers accessed via
; ISAR (Indirect Scratchpad Address Register).
; Registers r0-r11 are directly addressable by some instructions.
; Registers 9 (Q) and 10-11 (Qu/Ql) have special roles.
;
; r0-r8    General purpose
; r9 (Q)   Status / indirect high
; r10 (Qu) Indirect high byte
; r11 (Ql) Indirect low byte
; r12-r63  General purpose (ISAR addressed)
; ============================================================================

REG_Q			= $09		; Status register (scratchpad r9)
REG_QU			= $0a		; Indirect address high (scratchpad r10)
REG_QL			= $0b		; Indirect address low (scratchpad r11)
