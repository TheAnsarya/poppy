namespace Poppy.Core.Arch;

using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
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

	/// <summary>
	/// Gets the effective bank size, which may vary from <see cref="DefaultBankSize"/>
	/// based on runtime configuration (e.g., SNES memory mapping mode).
	/// Default returns <see cref="DefaultBankSize"/>.
	/// </summary>
	int GetBankSize(SemanticAnalyzer analyzer) => DefaultBankSize;

	/// <summary>
	/// Validates a memory address for architecture-specific constraints
	/// (e.g., hardware register writes, ROM regions).
	/// Default: no validation.
	/// </summary>
	void ValidateMemoryAddress(string mnemonic, long address, SourceLocation location,
		Action<string, SourceLocation> reportError, Action<string, SourceLocation> reportWarning) { }

	/// <summary>
	/// Creates a processor state object for architectures with flag-dependent
	/// operand sizing (e.g., 65816 M/X flags). Returns null for architectures
	/// that don't track processor state.
	/// </summary>
	ProcessorState? CreateProcessorState() => null;

	/// <summary>
	/// Tries to handle a processor state directive (e.g., .a8, .a16, .i8, .i16, .smart).
	/// Returns true if the directive was handled by this profile.
	/// </summary>
	/// <param name="directiveName">The lowercase directive name.</param>
	/// <param name="state">The processor state to update.</param>
	bool TryHandleProcessorDirective(string directiveName, ProcessorState state) => false;

	/// <summary>
	/// Gets the operand size for an instruction, accounting for architecture-specific
	/// processor flags (e.g., 65816 M/X flags for immediate mode).
	/// Default returns <paramref name="encodingSize"/> - 1.
	/// </summary>
	int GetOperandSize(string mnemonic, AddressingMode mode, int encodingSize,
		ProcessorState? state) => encodingSize - 1;

	/// <summary>
	/// Updates processor flag state after an instruction is emitted.
	/// Used for architectures with flag-dependent operand sizes (e.g., 65816 REP/SEP).
	/// Default: no-op.
	/// </summary>
	void UpdateProcessorFlags(string mnemonic, long? operandValue, ProcessorState? state) { }

	/// <summary>
	/// Size of the ROM file header in bytes (e.g., 16 for iNES header).
	/// Used by output generators to convert CPU addresses to file offsets.
	/// Default: 0 (no header).
	/// </summary>
	int RomFileHeaderSize => 0;

	/// <summary>
	/// Maps a CPU address to a ROM offset (excluding file header).
	/// Returns -1 if the address is not in ROM space.
	/// Default: identity mapping (returns cpuAddress unchanged).
	/// </summary>
	int MapCpuToRomOffset(int cpuAddress) => cpuAddress;

	/// <summary>
	/// Gets the memory region classification name for an address.
	/// Used by symbol exporters for debug format output.
	/// Default: "PRG".
	/// </summary>
	string GetMemoryRegionName(long address) => "PRG";

	/// <summary>
	/// Gets the bank number for a given address.
	/// Used by symbol exporters for banked ROM systems.
	/// Default: 0.
	/// </summary>
	int GetAddressBank(long address) => 0;

	/// <summary>
	/// Gets the default memory segments for this platform.
	/// Returns an empty list if no defaults are defined.
	/// </summary>
	IReadOnlyList<(string Name, long StartAddress, long MaxSize, SegmentType Type)> GetDefaultSegments()
		=> [];

	/// <summary>
	/// Tries to handle a platform-specific directive.
	/// Returns true if the directive was handled by this profile.
	/// Returns false if the directive is not recognized by this platform.
	/// </summary>
	/// <param name="node">The directive AST node.</param>
	/// <param name="analyzer">The semantic analyzer (for evaluating expressions, reporting errors, and setting header properties).</param>
	bool TryHandleDirective(DirectiveNode node, SemanticAnalyzer analyzer) => false;
}
