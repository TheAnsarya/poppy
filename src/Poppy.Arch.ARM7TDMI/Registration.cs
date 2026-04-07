using Poppy.Core.Arch;

namespace Poppy.Arch.ARM7TDMI;

/// <summary>
/// Registers the ARM7TDMI profile with the <see cref="TargetResolver"/>.
/// </summary>
public static class Registration {
	/// <summary>
	/// Registers the ARM7TDMI profile.
	/// </summary>
	public static void RegisterAll() {
		TargetResolver.Register(Arm7tdmiProfile.Instance);
	}
}
