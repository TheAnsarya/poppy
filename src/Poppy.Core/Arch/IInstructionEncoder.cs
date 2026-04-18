namespace Poppy.Core.Arch;

using Poppy.Core.Lexer;
using Poppy.Core.Parser;

/// <summary>
/// Architecture-neutral instruction encoding result.
/// Replaces the per-architecture InstructionEncoding types as universal currency.
/// </summary>
public readonly record struct EncodedInstruction(byte Opcode, int Size);

/// <summary>
/// Represents a resolved operand for architecture-specific instruction emission.
/// </summary>
public readonly record struct ResolvedOperand(string? Identifier, long? Value);

/// <summary>
/// Context for architecture-specific special instruction emission.
/// Flattened from AST node to avoid coupling encoders to the parser AST.
/// </summary>
public readonly record struct SpecialInstructionContext(
	string Mnemonic,
	string? OperandIdentifier,
	AddressingMode AddressingMode,
	long? OperandValue,
	SourceLocation Location,
	IReadOnlyList<ResolvedOperand>? AdditionalOperands = null);

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

	/// <summary>
	/// Returns true if the name is a register for this architecture.
	/// Used to exclude register names from undefined-symbol validation.
	/// Default: false (no register names).
	/// </summary>
	bool IsRegister(string name) => false;

	/// <summary>
	/// Returns true if the name is a segment register for this architecture.
	/// Default: false (no segment registers).
	/// </summary>
	bool IsSegmentRegister(string name) => false;

	/// <summary>
	/// Gets the instruction size in bytes for architecture-specific instructions
	/// that cannot be sized through the generic encoding pipeline.
	/// Returns 0 to fall through to generic sizing.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <param name="operandIdentifier">The operand identifier name (if operand is a named identifier), or null.</param>
	/// <param name="hasOperand">Whether the instruction has any operand.</param>
	/// <param name="sizeSuffix">The size suffix character (.b, .w), or null.</param>
	int GetSpecialInstructionSize(string mnemonic, string? operandIdentifier, bool hasOperand, char? sizeSuffix) => 0;

	/// <summary>
	/// Gets the instruction size with additional operand information for multi-operand architectures.
	/// Default implementation delegates to the single-operand overload.
	/// </summary>
	int GetSpecialInstructionSize(string mnemonic, string? operandIdentifier, bool hasOperand, char? sizeSuffix,
		IReadOnlyList<ResolvedOperand>? additionalOperands) =>
		GetSpecialInstructionSize(mnemonic, operandIdentifier, hasOperand, sizeSuffix);

	/// <summary>
	/// Tries to emit architecture-specific instructions that cannot be expressed
	/// through the generic opcode + operand pipeline.
	/// Returns true if the instruction was handled; false to fall through.
	/// </summary>
	bool TryEmitSpecialInstruction(SpecialInstructionContext context, ICodeEmitter emitter) => false;
}
