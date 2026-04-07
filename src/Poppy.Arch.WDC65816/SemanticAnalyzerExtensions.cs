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
		// Only create if at least one SNES directive was used or memory mapping is set
		if (analyzer.MemoryMapping == null && analyzer.SnesTitle == null && analyzer.SnesRegion == null &&
			analyzer.SnesVersion == null && analyzer.SnesRomSizeKb == null && analyzer.SnesRamSizeKb == null &&
			!analyzer.SnesFastRom) {
			return null;
		}

		var builder = new SnesHeaderBuilder();

		// Set mapping mode from MemoryMapping directive
		if (analyzer.MemoryMapping != null) {
			var mode = analyzer.MemoryMapping switch {
				"lorom" => SnesMapMode.LoRom,
				"hirom" => SnesMapMode.HiRom,
				"exhirom" => SnesMapMode.ExHiRom,
				_ => SnesMapMode.LoRom
			};
			builder.SetMapMode(mode);
		}

		if (analyzer.SnesTitle != null) builder.SetTitle(analyzer.SnesTitle);
		if (analyzer.SnesRomSizeKb != null) builder.SetRomSize(analyzer.SnesRomSizeKb.Value);
		if (analyzer.SnesRamSizeKb != null) builder.SetRamSize(analyzer.SnesRamSizeKb.Value);
		if (analyzer.SnesFastRom) builder.SetFastRom(true);

		// Set region
		if (analyzer.SnesRegion != null) {
			var region = analyzer.SnesRegion.ToLowerInvariant() switch {
				"japan" => SnesRegion.Japan,
				"usa" => SnesRegion.NorthAmerica,
				"europe" => SnesRegion.Europe,
				_ => SnesRegion.Japan
			};
			builder.SetRegion(region);
		}

		if (analyzer.SnesVersion != null) {
			builder.SetVersion((byte)analyzer.SnesVersion.Value);
		}

		return builder;
	}
}
