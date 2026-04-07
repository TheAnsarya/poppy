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
		// Only create if at least one SPC directive was used
		if (analyzer.SpcSongTitle == null && analyzer.SpcGameTitle == null && analyzer.SpcArtist == null &&
			analyzer.SpcEntryPoint == null) {
			return null;
		}

		var builder = new SpcFileBuilder();

		if (analyzer.SpcSongTitle != null) builder.SetSongTitle(analyzer.SpcSongTitle);
		if (analyzer.SpcGameTitle != null) builder.SetGameTitle(analyzer.SpcGameTitle);
		if (analyzer.SpcArtist != null) builder.SetArtistName(analyzer.SpcArtist);
		if (analyzer.SpcEntryPoint != null) builder.SetPC((ushort)analyzer.SpcEntryPoint.Value);

		return builder;
	}
}
