namespace Poppy.Core.CodeGen;

/// <summary>
/// SNES ROM mapping modes.
/// </summary>
public enum SnesMapMode : byte {
	/// <summary>LoROM (mode $20).</summary>
	LoRom = 0x20,
	/// <summary>HiROM (mode $21).</summary>
	HiRom = 0x21,
	/// <summary>LoROM + S-DD1 (mode $22).</summary>
	LoRomSDD1 = 0x22,
	/// <summary>LoROM + SA-1 (mode $23).</summary>
	LoRomSA1 = 0x23,
	/// <summary>ExHiROM (mode $25).</summary>
	ExHiRom = 0x25,
	/// <summary>HiROM + S-DD1 (mode $32).</summary>
	HiRomSDD1 = 0x32,
	/// <summary>HiROM + SA-1 (mode $35).</summary>
	HiRomSA1 = 0x35
}
