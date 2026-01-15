// ============================================================================
// InstructionSet6507.cs - 6507 Instruction Encoding (Atari 2600)
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Parser;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Provides instruction encoding for the MOS 6507 processor (Atari 2600).
/// The 6507 is functionally identical to the 6502, with a reduced 13-bit address space.
/// All instructions and opcodes are the same as the 6502.
/// </summary>
public static class InstructionSet6507 {
	/// <summary>
	/// Instruction encoding information.
	/// </summary>
	/// <param name="Opcode">The opcode byte.</param>
	/// <param name="Size">The total instruction size in bytes.</param>
	public readonly record struct InstructionEncoding(byte Opcode, int Size);

	/// <summary>
	/// Attempts to get the instruction encoding for the given mnemonic and addressing mode.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic (e.g., "lda", "sta").</param>
	/// <param name="mode">The addressing mode.</param>
	/// <param name="encoding">The instruction encoding, if found.</param>
	/// <returns>True if the instruction was found, false otherwise.</returns>
	public static bool TryGetEncoding(string mnemonic, AddressingMode mode, out InstructionEncoding encoding) {
		// The 6507 uses the same instruction set as the 6502
		// The only difference is the reduced address space (13-bit vs 16-bit)
		// which is handled at the code generation level, not the instruction encoding level
		var result = InstructionSet6502.TryGetEncoding(mnemonic, mode, out var encoding6502);

		// Convert the 6502 encoding to 6507 encoding (same values)
		encoding = new InstructionEncoding(encoding6502.Opcode, encoding6502.Size);

		return result;
	}

	/// <summary>
	/// Gets all supported mnemonics for the 6507.
	/// </summary>
	/// <returns>A collection of all supported instruction mnemonics.</returns>
	public static IEnumerable<string> GetAllMnemonics() {
		// 6507 supports all 6502 instructions
		return InstructionSet6502.GetAllMnemonics();
	}

	/// <summary>
	/// Gets all supported addressing modes for a given mnemonic.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <returns>A collection of supported addressing modes for the mnemonic.</returns>
	public static IEnumerable<AddressingMode> GetSupportedModes(string mnemonic) {
		// 6507 supports all 6502 addressing modes for each instruction
		return InstructionSet6502.GetSupportedModes(mnemonic);
	}
}
