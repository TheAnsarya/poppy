.target channelf
.org $0800
entry:
	ldi #$42
	nop
	jmp entry
	.db $00
