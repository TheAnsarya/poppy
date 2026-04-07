namespace Poppy.Arch.ARM7TDMI;

using Poppy.Core.Semantics;

/// <summary>
/// Extension methods for <see cref="SemanticAnalyzer"/> to build GBA platform headers.
/// </summary>
public static class SemanticAnalyzerExtensions {
	/// <summary>
	/// Gets a GBA header builder if GBA header directives were used, or null if not configured.
	/// </summary>
	public static GbaRomBuilder? GetGbaHeaderBuilder(this SemanticAnalyzer analyzer) {
		// Only create if at least one GBA directive was used
		if (analyzer.GbaTitle == null && analyzer.GbaGameCode == null && analyzer.GbaMakerCode == null &&
			analyzer.GbaVersion == null && analyzer.GbaEntryPoint == null) {
			return null;
		}

		var builder = new GbaRomBuilder();

		if (analyzer.GbaTitle != null) builder.SetTitle(analyzer.GbaTitle);
		if (analyzer.GbaGameCode != null) builder.SetGameCode(analyzer.GbaGameCode);
		if (analyzer.GbaMakerCode != null) builder.SetMakerCode(analyzer.GbaMakerCode);
		if (analyzer.GbaVersion != null) builder.SetVersion((byte)analyzer.GbaVersion.Value);
		if (analyzer.GbaEntryPoint != null) builder.SetEntryPointAddress((uint)analyzer.GbaEntryPoint.Value);

		return builder;
	}
}
