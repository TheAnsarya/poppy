// ============================================================================
// ProcessorState.cs - Mutable Processor Flag State
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.Arch;

/// <summary>
/// Holds mutable processor flag state that varies during code processing.
/// Each pass (SemanticAnalyzer, CodeGenerator) owns its own instance.
/// Created by <see cref="ITargetProfile.CreateProcessorState"/> for architectures
/// that have flag-dependent operand sizing (e.g., 65816 M/X flags).
/// </summary>
public class ProcessorState {
	/// <summary>
	/// Gets or sets whether the accumulator is in 16-bit mode (65816 M flag clear).
	/// </summary>
	public bool AccumulatorIs16Bit { get; set; }

	/// <summary>
	/// Gets or sets whether index registers are in 16-bit mode (65816 X flag clear).
	/// </summary>
	public bool IndexIs16Bit { get; set; }
}
