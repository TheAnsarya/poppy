.system:nes

.org $8000

.macro load_default value=$42
	lda #value
.endmacro

main:
	@load_default
	rts
