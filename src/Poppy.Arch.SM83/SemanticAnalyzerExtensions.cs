namespace Poppy.Arch.SM83;

using Poppy.Core.Semantics;

/// <summary>
/// Extension methods for <see cref="SemanticAnalyzer"/> to build Game Boy platform headers.
/// </summary>
public static class SemanticAnalyzerExtensions {
	/// <summary>
	/// Gets a GB header builder if GB header directives were used.
	/// </summary>
	public static GbHeaderBuilder? GetGbHeaderBuilder(this SemanticAnalyzer analyzer) {
		// Only create if at least one GB directive was used
		if (analyzer.GbTitle == null && analyzer.GbCgbMode == null && analyzer.GbCartridgeType == null &&
			analyzer.GbRomSizeKb == null && analyzer.GbRamSizeKb == null && analyzer.GbRegion == null &&
			analyzer.GbVersion == null && !analyzer.GbSgbEnabled) {
			return null;
		}

		var builder = new GbHeaderBuilder();

		if (analyzer.GbTitle != null) builder.SetTitle(analyzer.GbTitle);

		if (analyzer.GbCgbMode != null) {
			var mode = analyzer.GbCgbMode.Value switch {
				0 => GbCgbMode.DmgOnly,
				1 => GbCgbMode.CgbCompatible,
				2 => GbCgbMode.CgbOnly,
				_ => GbCgbMode.DmgOnly
			};
			builder.SetCgbMode(mode);
		}

		if (analyzer.GbSgbEnabled) builder.SetSgbEnabled(true);

		if (analyzer.GbCartridgeType != null) {
			// Map numeric type to enum
			builder.SetCartridgeType((GbCartridgeType)analyzer.GbCartridgeType.Value);
		}

		if (analyzer.GbRomSizeKb != null) builder.SetRomSize(analyzer.GbRomSizeKb.Value);
		if (analyzer.GbRamSizeKb != null) builder.SetRamSize(analyzer.GbRamSizeKb.Value);

		if (analyzer.GbRegion != null) {
			var region = analyzer.GbRegion.Value == 0 ? GbRegion.Japan : GbRegion.International;
			builder.SetRegion(region);
		}

		if (analyzer.GbVersion != null) {
			builder.SetVersion((byte)analyzer.GbVersion.Value);
		}

		return builder;
	}
}
