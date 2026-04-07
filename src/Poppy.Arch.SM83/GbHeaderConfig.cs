using Poppy.Core.Arch;

namespace Poppy.Arch.SM83;

/// <summary>
/// Game Boy header configuration extracted from SemanticAnalyzer.
/// </summary>
public sealed class GbHeaderConfig : IHeaderConfig {
	/// <summary>Game Boy ROM title.</summary>
	public string? Title { get; set; }
	/// <summary>Game Boy CGB mode value.</summary>
	public int? CgbMode { get; set; }
	/// <summary>Whether Super Game Boy features are enabled.</summary>
	public bool SgbEnabled { get; set; }
	/// <summary>Game Boy cartridge type value.</summary>
	public int? CartridgeType { get; set; }
	/// <summary>Game Boy ROM size in kilobytes.</summary>
	public int? RomSizeKb { get; set; }
	/// <summary>Game Boy RAM size in kilobytes.</summary>
	public int? RamSizeKb { get; set; }
	/// <summary>Game Boy region code.</summary>
	public int? Region { get; set; }
	/// <summary>Game Boy ROM version.</summary>
	public int? Version { get; set; }
}
