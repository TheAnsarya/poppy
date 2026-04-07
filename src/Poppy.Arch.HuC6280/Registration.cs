using Poppy.Core.Arch;

namespace Poppy.Arch.HuC6280;

/// <summary>
/// Registers the HuC6280 profile with the <see cref="TargetResolver"/>.
/// </summary>
public static class Registration {
	/// <summary>
	/// Registers the HuC6280 profile.
	/// </summary>
	public static void RegisterAll() {
		TargetResolver.Register(Huc6280Profile.Instance);
	}
}
