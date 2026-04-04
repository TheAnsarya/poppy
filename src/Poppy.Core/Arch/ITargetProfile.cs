namespace Poppy.Core.Arch;

using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// Complete profile for a target architecture. Provides all target-specific
/// behavior needed by CodeGenerator and SemanticAnalyzer.
/// Replaces the scattered if/else dispatch chains throughout the compiler.
/// </summary>
public interface ITargetProfile {
	/// <summary>
	/// The target architecture enum value.
	/// </summary>
	TargetArchitecture Architecture { get; }

	/// <summary>
	/// The instruction encoder for this architecture.
	/// </summary>
	IInstructionEncoder Encoder { get; }

	/// <summary>
	/// Default bank size in bytes for this platform.
	/// </summary>
	int DefaultBankSize { get; }

	/// <summary>
	/// CPU base address for a given bank number, or -1 if not applicable.
	/// </summary>
	long GetBankCpuBase(int bank);

	/// <summary>
	/// Size of the .long directive in bytes (3 for 65816, 4 for others).
	/// </summary>
	int LongDirectiveSize => 4;

	/// <summary>
	/// Optionally adjust the addressing mode before encoding.
	/// Returns null if no adjustment is needed.
	/// Used for: 65SC02 INC/DEC → Accumulator, etc.
	/// </summary>
	AddressingMode? AdjustAddressingMode(string mnemonic, AddressingMode mode) => null;

	/// <summary>
	/// Creates a ROM builder for this platform, or null if the platform
	/// just outputs raw binary (flattened segments).
	/// </summary>
	IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer);
}
