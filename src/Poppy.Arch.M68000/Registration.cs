using Poppy.Core.Arch;

namespace Poppy.Arch.M68000;

/// <summary>
/// Registers the M68000 profile with the <see cref="TargetResolver"/>.
/// </summary>
public static class Registration {
	/// <summary>
	/// Registers the M68000 profile.
	/// </summary>
	public static void RegisterAll() {
		TargetResolver.Register(M68000Profile.Instance);
	}
}
