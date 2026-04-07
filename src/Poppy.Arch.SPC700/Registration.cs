using Poppy.Core.Arch;

namespace Poppy.Arch.SPC700;

/// <summary>
/// Registers the SPC700 profile with the <see cref="TargetResolver"/>.
/// </summary>
public static class Registration {
	/// <summary>
	/// Registers the SPC700 profile.
	/// </summary>
	public static void RegisterAll() {
		TargetResolver.Register(Spc700Profile.Instance);
	}
}
