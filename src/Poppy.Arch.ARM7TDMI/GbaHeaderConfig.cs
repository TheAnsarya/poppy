using Poppy.Core.Arch;

namespace Poppy.Arch.ARM7TDMI;

/// <summary>
/// GBA header configuration extracted from SemanticAnalyzer.
/// </summary>
public sealed class GbaHeaderConfig : IHeaderConfig {
	/// <summary>GBA game title.</summary>
	public string? Title { get; set; }
	/// <summary>GBA 4-character game code.</summary>
	public string? GameCode { get; set; }
	/// <summary>GBA 2-character maker code.</summary>
	public string? MakerCode { get; set; }
	/// <summary>GBA software version.</summary>
	public int? Version { get; set; }
	/// <summary>GBA entry point address.</summary>
	public int? EntryPoint { get; set; }
}
