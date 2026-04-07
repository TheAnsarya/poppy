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
		if (analyzer.HeaderConfig is not GbaHeaderConfig config) return null;

		var builder = new GbaRomBuilder();

		if (config.Title != null) builder.SetTitle(config.Title);
		if (config.GameCode != null) builder.SetGameCode(config.GameCode);
		if (config.MakerCode != null) builder.SetMakerCode(config.MakerCode);
		if (config.Version != null) builder.SetVersion((byte)config.Version.Value);
		if (config.EntryPoint != null) builder.SetEntryPointAddress((uint)config.EntryPoint.Value);

		return builder;
	}
}
