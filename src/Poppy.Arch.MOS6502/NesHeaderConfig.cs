using Poppy.Core.Arch;

namespace Poppy.Arch.MOS6502;

/// <summary>
/// NES/iNES header configuration extracted from SemanticAnalyzer.
/// </summary>
public sealed class NesHeaderConfig : IHeaderConfig {
	/// <summary>iNES PRG ROM size (in 16KB units).</summary>
	public int? PrgSize { get; set; }
	/// <summary>iNES CHR ROM size (in 8KB units).</summary>
	public int? ChrSize { get; set; }
	/// <summary>iNES mapper number.</summary>
	public int? Mapper { get; set; }
	/// <summary>iNES 2.0 submapper number.</summary>
	public int? Submapper { get; set; }
	/// <summary>iNES mirroring mode (true = vertical, false = horizontal).</summary>
	public bool? Mirroring { get; set; }
	/// <summary>Whether the cartridge has battery-backed save RAM.</summary>
	public bool Battery { get; set; }
	/// <summary>Whether the ROM has a 512-byte trainer.</summary>
	public bool Trainer { get; set; }
	/// <summary>Whether to use four-screen VRAM mode.</summary>
	public bool FourScreen { get; set; }
	/// <summary>iNES PRG RAM size.</summary>
	public int? PrgRamSize { get; set; }
	/// <summary>iNES CHR RAM size.</summary>
	public int? ChrRamSize { get; set; }
	/// <summary>Whether the ROM targets PAL (true) or NTSC (false).</summary>
	public bool Pal { get; set; }
	/// <summary>Whether to use iNES 2.0 format (default: true).</summary>
	public bool UseINes2 { get; set; } = true;
	/// <summary>NES mapper number (from .mapper directive).</summary>
	public int? NesMapper { get; set; }
}
