// ============================================================================
// ModuleInitializer.cs - Architecture Registration for Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Runtime.CompilerServices;

namespace Poppy.Tests;

/// <summary>
/// Registers all architecture plugins before any test runs.
/// </summary>
internal static class TestModuleInitializer {
	[ModuleInitializer]
	internal static void Initialize() {
		Poppy.Arch.MOS6502.Registration.RegisterAll();
		Poppy.Arch.WDC65816.Registration.RegisterAll();
		Poppy.Arch.SM83.Registration.RegisterAll();
		Poppy.Arch.M68000.Registration.RegisterAll();
		Poppy.Arch.Z80.Registration.RegisterAll();
		Poppy.Arch.V30MZ.Registration.RegisterAll();
		Poppy.Arch.ARM7TDMI.Registration.RegisterAll();
		Poppy.Arch.SPC700.Registration.RegisterAll();
		Poppy.Arch.HuC6280.Registration.RegisterAll();
	}
}
