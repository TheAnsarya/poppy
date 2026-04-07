using Poppy.Core.Arch;

namespace Poppy.Arch.Z80;

/// <summary>
/// Registers the Z80 profile with the <see cref="TargetResolver"/>.
/// </summary>
public static class Registration {
	/// <summary>
	/// Registers the Z80 profile.
	/// </summary>
	public static void RegisterAll() {
		TargetResolver.Register(Z80Profile.Instance);
	}
}
