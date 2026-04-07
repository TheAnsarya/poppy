// ============================================================================
// ModuleInitializer.cs - Architecture Registration for Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Runtime.CompilerServices;

namespace Poppy.Arch.SM83.Tests;

/// <summary>
/// Registers architecture plugins before any test runs in this project.
/// </summary>
internal static class TestModuleInitializer {
	[ModuleInitializer]
	internal static void Initialize() {
		Poppy.Arch.SM83.Registration.RegisterAll();
	}
}
