using Poppy.Core.Arch;

namespace Poppy.Arch.WDC65816;

/// <summary>
/// SNES header configuration extracted from SemanticAnalyzer.
/// </summary>
public sealed class SnesHeaderConfig : IHeaderConfig {
	/// <summary>SNES game title.</summary>
	public string? Title { get; set; }
	/// <summary>SNES region string.</summary>
	public string? Region { get; set; }
	/// <summary>SNES ROM version.</summary>
	public int? Version { get; set; }
	/// <summary>SNES ROM size in kilobytes.</summary>
	public int? RomSizeKb { get; set; }
	/// <summary>SNES RAM size in kilobytes.</summary>
	public int? RamSizeKb { get; set; }
	/// <summary>Whether to use SNES fast ROM mode.</summary>
	public bool FastRom { get; set; }
	/// <summary>SNES memory mapping (lorom, hirom, exhirom).</summary>
	public string? MemoryMapping { get; set; }
}
