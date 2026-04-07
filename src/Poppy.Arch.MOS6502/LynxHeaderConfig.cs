using Poppy.Core.Arch;

namespace Poppy.Arch.MOS6502;

/// <summary>
/// Atari Lynx header/boot configuration extracted from SemanticAnalyzer.
/// </summary>
public sealed class LynxHeaderConfig : IHeaderConfig {
	/// <summary>Atari Lynx game name.</summary>
	public string? GameName { get; set; }
	/// <summary>Atari Lynx manufacturer name.</summary>
	public string? Manufacturer { get; set; }
	/// <summary>Atari Lynx screen rotation value.</summary>
	public int? Rotation { get; set; }
	/// <summary>Atari Lynx bank 0 size in bytes.</summary>
	public int? Bank0Size { get; set; }
	/// <summary>Atari Lynx bank 1 size in bytes.</summary>
	public int? Bank1Size { get; set; }
	/// <summary>Atari Lynx entry point address.</summary>
	public int? EntryPoint { get; set; }
	/// <summary>Whether to generate Lynx standard boot code.</summary>
	public bool UseBootCode { get; set; }
}
