namespace Poppy.Arch.SPC700;

using Poppy.Core.Semantics;

/// <summary>
/// Extension methods for <see cref="SemanticAnalyzer"/> to build SPC700 file structures.
/// </summary>
public static class SemanticAnalyzerExtensions {
	/// <summary>
	/// Gets an SPC file builder if SPC700 directives were used, or null if not configured.
	/// </summary>
	public static SpcFileBuilder? GetSpcFileBuilder(this SemanticAnalyzer analyzer) {
		if (analyzer.HeaderConfig is not SpcHeaderConfig config) return null;

		var builder = new SpcFileBuilder();

		if (config.SongTitle != null) builder.SetSongTitle(config.SongTitle);
		if (config.GameTitle != null) builder.SetGameTitle(config.GameTitle);
		if (config.Artist != null) builder.SetArtistName(config.Artist);
		if (config.EntryPoint != null) builder.SetPC((ushort)config.EntryPoint.Value);

		return builder;
	}
}
