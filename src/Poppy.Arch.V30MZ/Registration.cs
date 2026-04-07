using Poppy.Core.Arch;

namespace Poppy.Arch.V30MZ;

/// <summary>
/// Registers the V30MZ profile with the <see cref="TargetResolver"/>.
/// </summary>
public static class Registration {
	/// <summary>
	/// Registers the V30MZ profile.
	/// </summary>
	public static void RegisterAll() {
		TargetResolver.Register(V30mzProfile.Instance);
	}
}
