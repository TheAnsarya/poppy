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
		// Only create if at least one iNES directive was used
		if (analyzer.InesPrgSize == null && analyzer.InesChrSize == null && analyzer.InesMapper == null &&
			analyzer.InesSubmapper == null && analyzer.InesMirroring == null && !analyzer.InesBattery &&
			!analyzer.InesTrainer && !analyzer.InesFourScreen && !analyzer.InesPal && analyzer.InesPrgRamSize == null &&
			analyzer.InesChrRamSize == null) {
			return null;
		}

		var builder = new INesHeaderBuilder();

		if (analyzer.InesPrgSize != null) builder.SetPrgRomSize(analyzer.InesPrgSize.Value);
		if (analyzer.InesChrSize != null) builder.SetChrRomSize(analyzer.InesChrSize.Value);
		if (analyzer.InesMapper != null) builder.SetMapper(analyzer.InesMapper.Value);
		if (analyzer.InesSubmapper != null) builder.SetSubmapper(analyzer.InesSubmapper.Value);
		if (analyzer.InesMirroring != null) builder.SetMirroring(analyzer.InesMirroring.Value);
		if (analyzer.InesPrgRamSize != null) builder.SetPrgRamSize(analyzer.InesPrgRamSize.Value);
		if (analyzer.InesChrRamSize != null) builder.SetChrRamSize(analyzer.InesChrRamSize.Value);

		builder.SetBatteryBacked(analyzer.InesBattery);
		builder.SetTrainer(analyzer.InesTrainer);
		builder.SetFourScreen(analyzer.InesFourScreen);
		builder.SetPal(analyzer.InesPal);
		builder.SetINes2(analyzer.UseINes2);

		return builder;
	}

	/// <summary>
	/// Gets Lynx header/boot settings if any Lynx directives were used.
	/// </summary>
	/// <returns>
	/// A record containing Lynx configuration settings, or null if no Lynx directives were used.
	/// </returns>
	public static LynxSettings? GetLynxSettings(this SemanticAnalyzer analyzer) {
		// Only create if at least one Lynx directive was used
		if (analyzer.LynxGameName == null && analyzer.LynxManufacturer == null && analyzer.LynxRotation == null &&
			analyzer.LynxBank0Size == null && analyzer.LynxBank1Size == null && analyzer.LynxEntryPoint == null &&
			!analyzer.LynxUseBootCode) {
			return null;
		}

		return new LynxSettings(
			GameName: analyzer.LynxGameName ?? "Poppy Game",
			Manufacturer: analyzer.LynxManufacturer ?? "Poppy",
			Rotation: (LynxRotation)(analyzer.LynxRotation ?? 0),
			Bank0Size: analyzer.LynxBank0Size ?? 131072,        // Default 128KB
			Bank1Size: analyzer.LynxBank1Size ?? 0,
			EntryPoint: analyzer.LynxEntryPoint ?? 0x0200,
			UseBootCode: analyzer.LynxUseBootCode
		);
	}
}
