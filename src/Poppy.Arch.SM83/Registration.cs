using Poppy.Core.Arch;

namespace Poppy.Arch.SM83;

/// <summary>
/// Registers the SM83 profile with the <see cref="TargetResolver"/>.
/// </summary>
public static class Registration {
	/// <summary>
	/// Registers the SM83 profile.
	/// </summary>
	public static void RegisterAll() {
		TargetResolver.Register(Sm83Profile.Instance);
	}
}
