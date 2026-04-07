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
	}
}
