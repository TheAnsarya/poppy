; ============================================================================
; lynx.inc - Atari Lynx Hardware Register Definitions
; Poppy Compiler - Multi-System Assembly Compiler
; ============================================================================
;
; WDC 65SC02 @ 4 MHz (16 MHz master clock / 4)
; 64KB RAM, 160x102 display, 16 colors from 4096 palette
;
; Reference: https://www.monlynx.de/lynx/lynxhw.html
; ============================================================================

; ============================================================================
; Memory Map
; ============================================================================

; $0000-$00ff   Zero Page
; $0100-$01ff   Stack
; $0200-$fbff   Work RAM / Program / Display Buffer
; $fc00-$fcff   Suzy Registers (Sprite/Math/Input)
; $fd00-$fdff   Mikey Registers (Timer/Audio/Display/IRQ)
; $fe00-$ffff   Boot ROM (can be disabled)

; ============================================================================
; Suzy Registers ($fc00-$fcff) - Sprite Engine / Math / Input
; ============================================================================

; Pen Index (color remap table, 16 entries)
PENIDX0			= $fc00
PENIDX1			= $fc01
PENIDX2			= $fc02
PENIDX3			= $fc03
PENIDX4			= $fc04
PENIDX5			= $fc05
PENIDX6			= $fc06
PENIDX7			= $fc07
PENIDX8			= $fc08
PENIDX9			= $fc09
PENIDXA			= $fc0a
PENIDXB			= $fc0b
PENIDXC			= $fc0c
PENIDXD			= $fc0d
PENIDXE			= $fc0e
PENIDXF			= $fc0f

; Temporary Address
TMPADRH			= $fc10
TMPADRL			= $fc11

; Tilt Accumulator
TILTACUMH		= $fc12
TILTACUML		= $fc13

; Horizontal/Vertical Offset
HOFFL			= $fc14
HOFFH			= $fc15
VOFFL			= $fc16
VOFFH			= $fc17

; Video Buffer Base Address
VIDBASL			= $fc18
VIDBASH			= $fc19

; Collision Buffer Base Address
COLLBASL		= $fc1a
COLLBASH		= $fc1b

; Current Video Address
VIDADRL			= $fc1c
VIDADRH			= $fc1d

; Current Collision Address
COLLADRL		= $fc1e
COLLADRH		= $fc1f

; Next SCB Address (sprite control block chain)
SCBNEXTL		= $fc20
SCBNEXTH		= $fc21

; Sprite Data Line
SPRDLINEL		= $fc22
SPRDLINEH		= $fc23

; Sprite Position Start
HPOSSTRTL		= $fc24
HPOSSTRTH		= $fc25
VPOSSTRTL		= $fc26
VPOSSTRTH		= $fc27

; Sprite Size Start (8.8 fixed point)
SPRHSIZL		= $fc28
SPRHSIZH		= $fc29
SPRVSIZL		= $fc2a
SPRVSIZH		= $fc2b

; Stretch and Tilt
STRETCHL		= $fc2c
STRETCHH		= $fc2d
TILTL			= $fc2e
TILTH			= $fc2f

; Current SCB Address
SCBADRL			= $fc30
SCBADRH			= $fc31

; Process Address
PROCADRL		= $fc32
PROCADRH		= $fc33

; ============================================================================
; Suzy Math Hardware ($fc50-$fc6f)
; ============================================================================

; Math Operands (write to trigger operation)
; Write order: E, D, C, B, then A triggers operation
MATHD			= $fc52		; Operand D (signed)
MATHC			= $fc53		; Operand C (signed)
MATHB			= $fc54		; Operand B
MATHA			= $fc55		; Operand A (write triggers multiply)

; Math Results (read after operation completes)
MATHP			= $fc56		; P result
MATHN			= $fc57		; N result
MATHH			= $fc60		; H result
MATHG			= $fc61		; G result
MATHF			= $fc62		; F result
MATHE			= $fc63		; E result
MATHM			= $fc6c		; M result (accumulator high)
MATHL			= $fc6d		; L result
MATHK			= $fc6e		; K result (low word)
MATHJ			= $fc6f		; J result

; ============================================================================
; Suzy Sprite Control ($fc80-$fc9f)
; ============================================================================

SPRCTL0			= $fc80		; Sprite Control 0 (type, bpp, flip)
SPRCTL1			= $fc81		; Sprite Control 1 (reload, action)
SPRCOLL			= $fc82		; Sprite Collision Number
SPRINIT			= $fc83		; Sprite Init (reload SCB fields)
SUZYBUSEN		= $fc90		; Suzy Bus Enable
SPRGO			= $fc91		; Start Sprite Engine
SPRSYS			= $fc92		; Sprite System Control/Status

; SPRCTL0 Bit Definitions
SPRCTL0_TYPE_MASK	= %00000111	; Sprite type (0-7)
SPRCTL0_BPP_MASK	= %11000000	; Bits per pixel
SPRCTL0_BPP_1		= %00000000	; 1 bpp
SPRCTL0_BPP_2		= %01000000	; 2 bpp
SPRCTL0_BPP_3		= %10000000	; 3 bpp
SPRCTL0_BPP_4		= %11000000	; 4 bpp
SPRCTL0_HFLIP		= %00100000	; Horizontal flip
SPRCTL0_VFLIP		= %00010000	; Vertical flip

; SPRCTL1 Bit Definitions
SPRCTL1_LITERAL		= %10000000	; Literal sprite data
SPRCTL1_ALGO3		= %01000000	; Algorithm 3
SPRCTL1_REUSEPLT	= %00100000	; Reuse palette
SPRCTL1_SKIP		= %00010000	; Skip sprite
SPRCTL1_DRAWUP		= %00001000	; Draw up (flip Y direction)
SPRCTL1_DRAWLEFT	= %00000100	; Draw left (flip X direction)
SPRCTL1_DRAWABS		= %00000010	; Absolute coordinates
SPRCTL1_LITERAL2	= %00000001	; Second literal flag

; SPRSYS Bit Definitions (read)
SPRSYS_MATHWORKING	= %10000000	; Math operation in progress
SPRSYS_MATHWARNING	= %01000000	; Math warning
SPRSYS_MATHCARRY	= %00100000	; Math carry
SPRSYS_VSTRETCHING	= %00010000	; V stretching active
SPRSYS_LEFTHANDED	= %00001000	; Left-handed mode
SPRSYS_UNSAFEACC	= %00000100	; Unsafe access occurred
SPRSYS_SPRITETOSPRITE = %00000010	; Sprite to sprite collision
SPRSYS_SPRITEWORKING = %00000001	; Sprite engine working

; ============================================================================
; Suzy Input Registers ($fca0-$fcbf)
; ============================================================================

JOYSTICK		= $fcb0		; Joystick Register
SWITCHES		= $fcb1		; System Switches

; Joystick Bit Definitions (active LOW)
JOY_RIGHT		= %00010000
JOY_LEFT		= %00100000
JOY_DOWN		= %01000000
JOY_UP			= %10000000
JOY_OPTION1		= %00001000
JOY_OPTION2		= %00000100
JOY_INSIDE		= %00000010	; B button
JOY_OUTSIDE		= %00000001	; A button

; Switch Bit Definitions
SW_CART1		= %00000001	; Cart bank select 1
SW_CART0		= %00000010	; Cart bank select 0
SW_PAUSE		= %00000100	; Pause button
SW_AUDIN		= %00001000	; Audio in

; ============================================================================
; Mikey Registers ($fd00-$fdff) - Timers / Audio / Display / IRQ
; ============================================================================

; ============================================================================
; Timer Registers ($fd00-$fd1f)
; Each timer: BACKUP, CTLA, COUNT, CTLB (4 bytes each)
; ============================================================================

; Timer 0 - Horizontal Sync
TIM0BKUP		= $fd00		; Timer 0 Backup (reload value)
TIM0CTLA		= $fd01		; Timer 0 Control A
TIM0CNT			= $fd02		; Timer 0 Count
TIM0CTLB		= $fd03		; Timer 0 Control B

; Timer 1 - Audio Channel 0
TIM1BKUP		= $fd04
TIM1CTLA		= $fd05
TIM1CNT			= $fd06
TIM1CTLB		= $fd07

; Timer 2 - Vertical Sync
TIM2BKUP		= $fd08
TIM2CTLA		= $fd09
TIM2CNT			= $fd0a
TIM2CTLB		= $fd0b

; Timer 3 - Audio Channel 1
TIM3BKUP		= $fd0c
TIM3CTLA		= $fd0d
TIM3CNT			= $fd0e
TIM3CTLB		= $fd0f

; Timer 4 - Audio Channel 2
TIM4BKUP		= $fd10
TIM4CTLA		= $fd11
TIM4CNT			= $fd12
TIM4CTLB		= $fd13

; Timer 5 - Audio Channel 3
TIM5BKUP		= $fd14
TIM5CTLA		= $fd15
TIM5CNT			= $fd16
TIM5CTLB		= $fd17

; Timer 6 - Serial Timer
TIM6BKUP		= $fd18
TIM6CTLA		= $fd19
TIM6CNT			= $fd1a
TIM6CTLB		= $fd1b

; Timer 7 - Linked Timer
TIM7BKUP		= $fd1c
TIM7CTLA		= $fd1d
TIM7CNT			= $fd1e
TIM7CTLB		= $fd1f

; Timer Control A Bit Definitions
TIMCTLA_ENABLE		= %10000000	; Timer enable/disable
TIMCTLA_RESETDONE	= %01000000	; Reset done flag when set
TIMCTLA_MAGICTAP	= %00100000	; Magic tap enable
TIMCTLA_IRQEN		= %00010000	; IRQ on timer done
TIMCTLA_CLKMASK		= %00000111	; Clock source mask
TIMCTLA_CLK_1MHZ	= %00000000	; 1 MHz
TIMCTLA_CLK_500KHZ	= %00000001	; 500 kHz
TIMCTLA_CLK_250KHZ	= %00000010	; 250 kHz
TIMCTLA_CLK_125KHZ	= %00000011	; 125 kHz
TIMCTLA_CLK_62_5KHZ	= %00000100	; 62.5 kHz
TIMCTLA_CLK_31_25KHZ	= %00000101	; 31.25 kHz
TIMCTLA_CLK_15_625KHZ	= %00000110	; 15.625 kHz
TIMCTLA_CLK_LINKED	= %00000111	; Linked to previous timer

; Timer Control B Bit Definitions
TIMCTLB_DONE		= %00001000	; Timer done flag
TIMCTLB_LASTTICK	= %00000100	; Last clock tick
TIMCTLB_BORROWIN	= %00000010	; Borrow in
TIMCTLB_BORROWOUT	= %00000001	; Borrow out

; ============================================================================
; Audio Registers ($fd20-$fd5f)
; Each channel: VOLUME, FEEDBACK, OUTPUT, SHIFT, BACKUP, CONTROL, COUNTER, OTHER
; ============================================================================

; Audio Channel 0 (uses Timer 1)
AUD0VOL			= $fd20		; Volume
AUD0FEED		= $fd21		; Feedback (LFSR taps)
AUD0OUT			= $fd22		; Output value (signed)
AUD0SHIFT		= $fd23		; Shift register low
AUD0SHIFTHI		= $fd24		; Shift register high
AUD0BKUP		= $fd25		; Backup (same as TIM1BKUP)
AUD0CTL			= $fd26		; Control
AUD0COUNT		= $fd27		; Counter (same as TIM1CNT)
AUD0MISC		= $fd28		; Misc control

; Audio Channel 1 (uses Timer 3)
AUD1VOL			= $fd28
AUD1FEED		= $fd29
AUD1OUT			= $fd2a
AUD1SHIFT		= $fd2b
AUD1SHIFTHI		= $fd2c
AUD1BKUP		= $fd2d
AUD1CTL			= $fd2e
AUD1COUNT		= $fd2f
AUD1MISC		= $fd30

; Audio Channel 2 (uses Timer 4)
AUD2VOL			= $fd30
AUD2FEED		= $fd31
AUD2OUT			= $fd32
AUD2SHIFT		= $fd33
AUD2SHIFTHI		= $fd34
AUD2BKUP		= $fd35
AUD2CTL			= $fd36
AUD2COUNT		= $fd37
AUD2MISC		= $fd38

; Audio Channel 3 (uses Timer 5)
AUD3VOL			= $fd38
AUD3FEED		= $fd39
AUD3OUT			= $fd3a
AUD3SHIFT		= $fd3b
AUD3SHIFTHI		= $fd3c
AUD3BKUP		= $fd3d
AUD3CTL			= $fd3e
AUD3COUNT		= $fd3f
AUD3MISC		= $fd40

; Stereo Control
STEREO			= $fd50		; Stereo enable/pan

; ============================================================================
; Interrupt Registers ($fd80-$fd8f)
; ============================================================================

INTRST			= $fd80		; Interrupt Reset (write 1 to clear)
INTSET			= $fd81		; Interrupt Status (read) / Set (write)
MAGRDY0			= $fd84		; Magic/Ready 0
MAGRDY1			= $fd85		; Magic/Ready 1
AUDIN			= $fd86		; Audio In
SYSCTL1			= $fd87		; System Control 1
MIKEYHREV		= $fd88		; Mikey Hardware Revision
MIKEYSREV		= $fd89		; Mikey Software Revision

; Interrupt Bit Definitions (one per timer)
INT_TIMER0		= %00000001	; Timer 0 (HSync)
INT_TIMER1		= %00000010	; Timer 1 (Audio 0)
INT_TIMER2		= %00000100	; Timer 2 (VSync)
INT_TIMER3		= %00001000	; Timer 3 (Audio 1)
INT_TIMER4		= %00010000	; Timer 4 (Audio 2)
INT_TIMER5		= %00100000	; Timer 5 (Audio 3)
INT_TIMER6		= %01000000	; Timer 6 (Serial)
INT_TIMER7		= %10000000	; Timer 7 (Linked)

; ============================================================================
; I/O and Serial ($fd88-$fd8f)
; ============================================================================

IODIR			= $fd8a		; I/O Direction (1=output, 0=input)
IODAT			= $fd8b		; I/O Data
SERCTL			= $fd8c		; Serial Control
SERDAT			= $fd8d		; Serial Data
SDONEACK		= $fd90		; Serial Done Acknowledge
CPUSLEEP		= $fd91		; CPU Sleep (WAI trigger)

; ============================================================================
; Display Registers ($fd92-$fd9f)
; ============================================================================

DISPCTL			= $fd92		; Display Control
PBKUP			= $fd93		; Parameter Backup
DISPADRL		= $fd94		; Display Address Low
DISPADRH		= $fd95		; Display Address High

; Display Control Bit Definitions
DISPCTL_ENABLE		= %00000001	; Display enable
DISPCTL_4BIT		= %00000010	; 4-bit mode (vs 2-bit)
DISPCTL_COLOR		= %00000100	; Color enable
DISPCTL_FLIP		= %00001000	; Flip display

; ============================================================================
; Palette Registers ($fda0-$fdbf)
; ============================================================================

; Green Palette (16 entries, 4 bits each = 16 green levels)
GREEN0			= $fda0
GREEN1			= $fda1
GREEN2			= $fda2
GREEN3			= $fda3
GREEN4			= $fda4
GREEN5			= $fda5
GREEN6			= $fda6
GREEN7			= $fda7
GREEN8			= $fda8
GREEN9			= $fda9
GREENA			= $fdaa
GREENB			= $fdab
GREENC			= $fdac
GREEND			= $fdad
GREENE			= $fdae
GREENF			= $fdaf

; Blue/Red Palette (16 entries, high nibble=blue, low nibble=red)
BLUERED0		= $fdb0
BLUERED1		= $fdb1
BLUERED2		= $fdb2
BLUERED3		= $fdb3
BLUERED4		= $fdb4
BLUERED5		= $fdb5
BLUERED6		= $fdb6
BLUERED7		= $fdb7
BLUERED8		= $fdb8
BLUERED9		= $fdb9
BLUEREDA		= $fdba
BLUEREDB		= $fdbb
BLUEREDC		= $fdbc
BLUEREDD		= $fdbd
BLUEREDE		= $fdbe
BLUEREDF		= $fdbf

; ============================================================================
; Display Constants
; ============================================================================

SCREEN_WIDTH		= 160
SCREEN_HEIGHT		= 102
SCREEN_PIXELS		= SCREEN_WIDTH * SCREEN_HEIGHT	; 16320

; Frame buffer size (4bpp = 160 * 102 / 2 = 8160 bytes)
FRAMEBUF_SIZE		= 8160

; ============================================================================
; Vector Addresses
; ============================================================================

VEC_NMI			= $fffa		; NMI vector (unused on Lynx)
VEC_RESET		= $fffc		; Reset vector
VEC_IRQ			= $fffe		; IRQ/BRK vector

; ============================================================================
; Boot ROM
; ============================================================================

BOOTROM_START		= $fe00
BOOTROM_SIZE		= 512		; 512 bytes

; ============================================================================
; Typical Load/Start Address
; ============================================================================

LOAD_ADDR		= $0200		; Typical program load address

