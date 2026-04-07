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
		if (analyzer.HeaderConfig is not GbHeaderConfig config) return null;

		// Only create if at least one GB directive was used
		if (config.Title == null && config.CgbMode == null && config.CartridgeType == null &&
			config.RomSizeKb == null && config.RamSizeKb == null && config.Region == null &&
			config.Version == null && !config.SgbEnabled) {
			return null;
		}

		var builder = new GbHeaderBuilder();

		if (config.Title != null) builder.SetTitle(config.Title);

		if (config.CgbMode != null) {
			var mode = config.CgbMode.Value switch {
				0 => GbCgbMode.DmgOnly,
				1 => GbCgbMode.CgbCompatible,
				2 => GbCgbMode.CgbOnly,
				_ => GbCgbMode.DmgOnly
			};
			builder.SetCgbMode(mode);
		}

		if (config.SgbEnabled) builder.SetSgbEnabled(true);

		if (config.CartridgeType != null) {
			// Map numeric type to enum
			builder.SetCartridgeType((GbCartridgeType)config.CartridgeType.Value);
		}

		if (config.RomSizeKb != null) builder.SetRomSize(config.RomSizeKb.Value);
		if (config.RamSizeKb != null) builder.SetRamSize(config.RamSizeKb.Value);

		if (config.Region != null) {
			var region = config.Region.Value == 0 ? GbRegion.Japan : GbRegion.International;
			builder.SetRegion(region);
		}

		if (config.Version != null) {
			builder.SetVersion((byte)config.Version.Value);
		}

		return builder;
	}
}
