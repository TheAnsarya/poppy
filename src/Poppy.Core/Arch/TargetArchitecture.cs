// ============================================================================
// TargetArchitecture.cs - Target CPU Architecture Enum
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.Arch;

/// <summary>
/// Target CPU architecture for the assembler.
/// </summary>
public enum TargetArchitecture {
	/// <summary>MOS 6502 (NES, Commodore 64, etc.)</summary>
	MOS6502,

	/// <summary>MOS 6507 (Atari 2600 - 6502 variant with 13-bit addressing)</summary>
	MOS6507,

	/// <summary>MOS 65SC02 (Atari Lynx - 65C02 without decimal mode)</summary>
	MOS65SC02,

	/// <summary>WDC 65816 (SNES)</summary>
	WDC65816,

	/// <summary>Sharp SM83 (Game Boy)</summary>
	SM83,

	/// <summary>Motorola 68000 (Sega Genesis/Mega Drive)</summary>
	M68000,

	/// <summary>Zilog Z80 (Sega Master System, Game Gear)</summary>
	Z80,

	/// <summary>NEC V30MZ (WonderSwan, WonderSwan Color - 80186 compatible)</summary>
	V30MZ,

	/// <summary>ARM7TDMI (Game Boy Advance)</summary>
	ARM7TDMI,

	/// <summary>Sony SPC700 (SNES Audio Processor)</summary>
	SPC700,

	/// <summary>Hudson HuC6280 (TurboGrafx-16/PC Engine - 65C02 variant)</summary>
	HuC6280,

	/// <summary>Fairchild F8 (Channel F)</summary>
	F8,
}
