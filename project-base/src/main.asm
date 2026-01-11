; Main entry point for the game
; This is a template file - customize it for your retro game

.org $8000

START:
    ; Initialize the system
    SEI             ; Disable interrupts
    CLD             ; Clear decimal mode
    
    ; TODO: Add your game initialization code here
    
MAIN_LOOP:
    ; TODO: Add your game logic here
    
    JMP MAIN_LOOP   ; Loop forever

; Interrupt vectors
.org $FFFA
.word START         ; NMI vector
.word START         ; Reset vector  
.word START         ; IRQ vector
