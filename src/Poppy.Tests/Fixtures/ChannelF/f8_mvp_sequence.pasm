.target channel_f
.org $0800
	ldi #$12
	.db $34, $56
	.dw $789a
	nop
	jmp $20
