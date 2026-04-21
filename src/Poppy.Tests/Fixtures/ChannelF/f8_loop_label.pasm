.target f8
.org $0800
start:
	nop
	ldi #$01
	jmp start
