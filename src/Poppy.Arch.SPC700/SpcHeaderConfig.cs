using Poppy.Core.Arch;

namespace Poppy.Arch.SPC700;

/// <summary>
/// SPC700 file configuration extracted from SemanticAnalyzer.
/// </summary>
public sealed class SpcHeaderConfig : IHeaderConfig {
	/// <summary>SPC700 song title.</summary>
	public string? SongTitle { get; set; }
	/// <summary>SPC700 game title.</summary>
	public string? GameTitle { get; set; }
	/// <summary>SPC700 artist name.</summary>
	public string? Artist { get; set; }
	/// <summary>SPC700 entry point address.</summary>
	public int? EntryPoint { get; set; }
}
