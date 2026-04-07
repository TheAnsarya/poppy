namespace Poppy.Arch.WDC65816;

using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;

/// <summary>
/// Extension methods for <see cref="SemanticAnalyzer"/> to build SNES platform headers.
/// </summary>
public static class SemanticAnalyzerExtensions {
	/// <summary>
	/// Gets the SNES header builder with all configured settings, or null if not configured.
	/// </summary>
	public static SnesHeaderBuilder? GetSnesHeaderBuilder(this SemanticAnalyzer analyzer) {
		if (analyzer.HeaderConfig is not SnesHeaderConfig config) return null;

		// Only create if at least one SNES directive was used or memory mapping is set
		if (config.MemoryMapping == null && config.Title == null && config.Region == null &&
			config.Version == null && config.RomSizeKb == null && config.RamSizeKb == null &&
			!config.FastRom) {
			return null;
		}

		var builder = new SnesHeaderBuilder();

		// Set mapping mode from MemoryMapping directive
		if (config.MemoryMapping != null) {
			var mode = config.MemoryMapping switch {
				"lorom" => SnesMapMode.LoRom,
				"hirom" => SnesMapMode.HiRom,
				"exhirom" => SnesMapMode.ExHiRom,
				_ => SnesMapMode.LoRom
			};
			builder.SetMapMode(mode);
		}

		if (config.Title != null) builder.SetTitle(config.Title);
		if (config.RomSizeKb != null) builder.SetRomSize(config.RomSizeKb.Value);
		if (config.RamSizeKb != null) builder.SetRamSize(config.RamSizeKb.Value);
		if (config.FastRom) builder.SetFastRom(true);

		// Set region
		if (config.Region != null) {
			var region = config.Region.ToLowerInvariant() switch {
				"japan" => SnesRegion.Japan,
				"usa" => SnesRegion.NorthAmerica,
				"europe" => SnesRegion.Europe,
				_ => SnesRegion.Japan
			};
			builder.SetRegion(region);
		}

		if (config.Version != null) {
			builder.SetVersion((byte)config.Version.Value);
		}

		return builder;
	}
}
