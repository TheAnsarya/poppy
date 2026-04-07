using Poppy.Core.Arch;

namespace Poppy.Arch.MOS6502;

/// <summary>
/// Registers all MOS6502-family profiles with the <see cref="TargetResolver"/>.
/// </summary>
public static class Registration {
	/// <summary>
	/// Registers the MOS6502, MOS6507, and MOS65SC02 profiles.
	/// </summary>
	public static void RegisterAll() {
		TargetResolver.Register(Mos6502Profile.Instance);
		TargetResolver.Register(Mos6507Profile.Instance);
		TargetResolver.Register(Mos65sc02Profile.Instance);
	}
}
