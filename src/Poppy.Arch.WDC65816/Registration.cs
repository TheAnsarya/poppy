using Poppy.Core.Arch;

namespace Poppy.Arch.WDC65816;

/// <summary>
/// Registers the WDC 65816 profile with the <see cref="TargetResolver"/>.
/// </summary>
public static class Registration {
	/// <summary>
	/// Registers the WDC65816 profile.
	/// </summary>
	public static void RegisterAll() {
		TargetResolver.Register(Wdc65816Profile.Instance);
	}
}
