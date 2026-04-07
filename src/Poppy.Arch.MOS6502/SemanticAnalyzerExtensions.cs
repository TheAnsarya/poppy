namespace Poppy.Arch.MOS6502;

using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;

/// <summary>
/// Extension methods for <see cref="SemanticAnalyzer"/> to build iNES and Lynx platform headers.
/// </summary>
public static class SemanticAnalyzerExtensions {
	/// <summary>
	/// Gets the iNES header builder with all configured settings, or null if not configured.
	/// </summary>
	public static INesHeaderBuilder? GetINesHeaderBuilder(this SemanticAnalyzer analyzer) {
		if (analyzer.HeaderConfig is not NesHeaderConfig config) return null;

		// Only create if at least one iNES directive was used
		if (config.PrgSize == null && config.ChrSize == null && config.Mapper == null &&
			config.Submapper == null && config.Mirroring == null && !config.Battery &&
			!config.Trainer && !config.FourScreen && !config.Pal && config.PrgRamSize == null &&
			config.ChrRamSize == null) {
			return null;
		}

		var builder = new INesHeaderBuilder();

		if (config.PrgSize != null) builder.SetPrgRomSize(config.PrgSize.Value);
		if (config.ChrSize != null) builder.SetChrRomSize(config.ChrSize.Value);
		if (config.Mapper != null) builder.SetMapper(config.Mapper.Value);
		if (config.Submapper != null) builder.SetSubmapper(config.Submapper.Value);
		if (config.Mirroring != null) builder.SetMirroring(config.Mirroring.Value);
		if (config.PrgRamSize != null) builder.SetPrgRamSize(config.PrgRamSize.Value);
		if (config.ChrRamSize != null) builder.SetChrRamSize(config.ChrRamSize.Value);

		builder.SetBatteryBacked(config.Battery);
		builder.SetTrainer(config.Trainer);
		builder.SetFourScreen(config.FourScreen);
		builder.SetPal(config.Pal);
		builder.SetINes2(config.UseINes2);

		return builder;
	}

	/// <summary>
	/// Gets Lynx header/boot settings if any Lynx directives were used.
	/// </summary>
	/// <returns>
	/// A record containing Lynx configuration settings, or null if no Lynx directives were used.
	/// </returns>
	public static LynxSettings? GetLynxSettings(this SemanticAnalyzer analyzer) {
		if (analyzer.HeaderConfig is not LynxHeaderConfig config) return null;

		// Only create if at least one Lynx directive was used (config exists, so at least one was set)
		return new LynxSettings(
			GameName: config.GameName ?? "Poppy Game",
			Manufacturer: config.Manufacturer ?? "Poppy",
			Rotation: (LynxRotation)(config.Rotation ?? 0),
			Bank0Size: config.Bank0Size ?? 131072,        // Default 128KB
			Bank1Size: config.Bank1Size ?? 0,
			EntryPoint: config.EntryPoint ?? 0x0200,
			UseBootCode: config.UseBootCode
		);
	}
}
