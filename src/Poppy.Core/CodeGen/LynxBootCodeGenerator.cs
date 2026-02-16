// ============================================================================
// LynxBootCodeGenerator.cs - Atari Lynx Boot Code Generator
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Generates standard Atari Lynx boot code.
/// </summary>
/// <remarks>
/// <para>
/// The Lynx boot code is placed at the start of ROM ($0200) and performs
/// essential hardware initialization before jumping to the main program.
/// </para>
/// <para>
/// Boot code responsibilities:
/// <list type="bullet">
/// <item>Disable interrupts</item>
/// <item>Clear decimal mode</item>
/// <item>Initialize stack pointer</item>
/// <item>Configure Suzy and Mikey registers</item>
/// <item>Set up display</item>
/// <item>Jump to entry point</item>
/// </list>
/// </para>
/// </remarks>
public static class LynxBootCodeGenerator {
	/// <summary>
	/// Load address for Lynx ROMs.
	/// </summary>
	public const int LoadAddress = 0x0200;

	/// <summary>
	/// Suzy hardware register addresses.
	/// </summary>
	private static class Suzy {
		public const int SprInit = 0xfc92;      // Sprite initialization
		public const int SprsYs = 0xfc94;       // Sprite system
		public const int MapCtl = 0xfff9;       // Memory map control
	}

	/// <summary>
	/// Mikey hardware register addresses.
	/// </summary>
	private static class Mikey {
		public const int DispCtl = 0xfd92;      // Display control
		public const int PbKup = 0xfd94;        // Pushbutton backup
		public const int IoDir = 0xfd8a;        // I/O direction
		public const int IoDat = 0xfd8b;        // I/O data
		public const int SerCtl = 0xfd8c;       // Serial control
		public const int Mapctl = 0xfff9;       // Memory map control
	}

	/// <summary>
	/// Generates boot code that initializes the Lynx and jumps to an entry point.
	/// </summary>
	/// <param name="entryPoint">The address to jump to after boot (default $0200).</param>
	/// <returns>The boot code bytes.</returns>
	public static byte[] GenerateBootCode(int entryPoint = 0x0200) {
		// If entry point is at boot code start, we need to adjust
		// In this case, entry point should be after boot code
		if (entryPoint == LoadAddress) {
			// Boot code will be placed at $0200, entry after boot code
			entryPoint = LoadAddress + StandardBootCodeSize;
		}

		var code = new List<byte>();

		// ===== Standard Lynx Boot Sequence =====

		// sei           ; Disable interrupts
		code.Add(0x78);

		// cld           ; Clear decimal mode
		code.Add(0xd8);

		// ldx #$ff      ; Initialize stack pointer
		code.Add(0xa2);
		code.Add(0xff);

		// txs           ; Transfer X to stack pointer
		code.Add(0x9a);

		// Initialize IODIR and IODAT for cart access
		// lda #$00
		code.Add(0xa9);
		code.Add(0x00);

		// sta $fd8a     ; IODIR - all inputs
		code.Add(0x8d);
		code.Add(0x8a);
		code.Add(0xfd);

		// sta $fd8b     ; IODAT - clear outputs
		code.Add(0x8d);
		code.Add(0x8b);
		code.Add(0xfd);

		// Initialize display control
		// lda #$08      ; Display enable with color
		code.Add(0xa9);
		code.Add(0x08);

		// sta $fd92     ; DISPCTL
		code.Add(0x8d);
		code.Add(0x92);
		code.Add(0xfd);

		// Initialize Suzy sprite system
		// lda #$01      ; Sprite initial value
		code.Add(0xa9);
		code.Add(0x01);

		// sta $fc92     ; SPRINIT
		code.Add(0x8d);
		code.Add(0x92);
		code.Add(0xfc);

		// sta $fc94     ; SPRSYS
		code.Add(0x8d);
		code.Add(0x94);
		code.Add(0xfc);

		// Clear memory map control to allow RAM access
		// stz $fff9     ; MAPCTL = 0 (RAM visible everywhere)
		code.Add(0x9c);     // STZ absolute (65SC02 instruction)
		code.Add(0xf9);
		code.Add(0xff);

		// Jump to entry point
		// jmp entry
		code.Add(0x4c);
		code.Add((byte)(entryPoint & 0xff));
		code.Add((byte)((entryPoint >> 8) & 0xff));

		return [.. code];
	}

	/// <summary>
	/// Gets the size of the standard boot code in bytes.
	/// </summary>
	public static int StandardBootCodeSize => GenerateBootCode(0x0300).Length;

	/// <summary>
	/// Generates minimal boot code (just disables interrupts and jumps).
	/// </summary>
	/// <param name="entryPoint">The address to jump to after minimal boot.</param>
	/// <returns>The minimal boot code bytes.</returns>
	public static byte[] GenerateMinimalBootCode(int entryPoint) {
		return [
			0x78,                                       // sei
			0xd8,                                       // cld
			0xa2, 0xff,                                 // ldx #$ff
			0x9a,                                       // txs
			0x4c,                                       // jmp
			(byte)(entryPoint & 0xff),
			(byte)((entryPoint >> 8) & 0xff)
		];
	}

	/// <summary>
	/// Gets the size of the minimal boot code in bytes.
	/// </summary>
	public const int MinimalBootCodeSize = 8;

	/// <summary>
	/// Generates a boot code stub that is included as inline assembly.
	/// </summary>
	/// <param name="entryPoint">The label or address to jump to.</param>
	/// <returns>Assembly source for the boot code.</returns>
	public static string GenerateBootCodeSource(string entryPoint) {
		return $@"; =============================================================================
; Lynx Standard Boot Code - Generated by Poppy
; =============================================================================

lynx_boot:
	sei			; Disable interrupts
	cld			; Clear decimal mode
	ldx #$ff		; Initialize stack pointer
	txs

	; Configure I/O
	lda #$00
	sta $fd8a		; IODIR - all inputs
	sta $fd8b		; IODAT - clear outputs

	; Enable display
	lda #$08		; Display enable with color
	sta $fd92		; DISPCTL

	; Initialize sprite system
	lda #$01
	sta $fc92		; SPRINIT
	sta $fc94		; SPRSYS

	; Clear memory map control
	stz $fff9		; MAPCTL = 0

	; Jump to main program
	jmp {entryPoint}
";
	}
}
