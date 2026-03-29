; Fairchild Channel F Hello World - Poppy Compiler Example
; Fills the screen with a solid color using direct VRAM writes

.system:channelf
.org $0800

; ===========================================================================
; Entry Point ($0800 - Cartridge starts here after BIOS handoff)
; ===========================================================================

CartEntry:
	; The BIOS jumps to $0800 after initialization.
	; At entry, the F8 accumulator and scratchpad are available.

	; Fill VRAM with a solid color
	; VRAM is $3000-$37ff (2048 bytes, 128x64 at 2bpp)
	; Each byte holds 4 pixels: bits 7-6, 5-4, 3-2, 1-0
	; COLOR_BLUE = $02, so a full blue byte = %10101010 = $aa

	li $aa			; Load blue fill pattern into accumulator
	lr 1, a			; Save pattern in r1

	; Set up ISAR to point to scratchpad r12 for address tracking
	; We'll use r12/r13 as a 16-bit VRAM pointer

	li $30			; High byte of VRAM start ($3000)
	lr 10, a		; r10 (Qu) = $30
	li $00			; Low byte of VRAM start
	lr 11, a		; r11 (Ql) = $00

FillLoop:
	lr a, 1			; Reload blue pattern from r1
	st			; Store accumulator to [DC] and increment DC

	; Check if we've passed VRAM end ($3800)
	lr a, 10		; Load current high byte
	ci $38			; Compare with $38 (past VRAM end)
	bz FillDone		; If equal, we're done

	br FillLoop		; Continue filling

FillDone:
	; Infinite loop (halt)
	; Channel F has no halt instruction, so we spin

.halt:
	br .halt		; Loop forever

; ===========================================================================
; Vectors
; ===========================================================================

	; The cartridge entry point is at $0800, handled by the BIOS
	; which reads the first instruction and jumps to it.
