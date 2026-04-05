namespace Poppy.Core.Arch;

using Poppy.Core.Parser;

/// <summary>
/// Architecture-neutral instruction encoding result.
/// Replaces the per-architecture InstructionEncoding types as universal currency.
/// </summary>
public readonly record struct EncodedInstruction(byte Opcode, int Size);

/// <summary>
/// Encodes mnemonics into machine code for a specific architecture.
/// Replaces the static InstructionSetXxx dispatch chains.
/// </summary>
public interface IInstructionEncoder {
	/// <summary>
	/// Tries to encode a mnemonic + addressing mode into an instruction.
	/// </summary>
	bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding);

	/// <summary>
	/// Returns true if the mnemonic is a relative branch instruction.
	/// </summary>
	bool IsBranchInstruction(string mnemonic);

	/// <summary>
	/// Gets the set of all mnemonics recognized by this encoder.
	/// Used by the lexer for target-aware mnemonic tokenization.
	/// </summary>
	IReadOnlySet<string> Mnemonics { get; }
}
