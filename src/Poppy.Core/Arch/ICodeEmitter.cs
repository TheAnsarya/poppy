namespace Poppy.Core.Arch;

using Poppy.Core.Lexer;

/// <summary>
/// Abstraction for emitting machine code bytes, reporting errors, and
/// recording CDL/cross-reference metadata. Passed to architecture-specific
/// encoders that need to emit multi-byte instruction sequences.
/// </summary>
public interface ICodeEmitter {
	/// <summary>
	/// The current output address (advances as bytes are emitted).
	/// </summary>
	long CurrentAddress { get; }

	/// <summary>
	/// Emits a single byte at the current address.
	/// </summary>
	void EmitByte(byte value);

	/// <summary>
	/// Emits a 16-bit word (little-endian) at the current address.
	/// </summary>
	void EmitWord(ushort value);

	/// <summary>
	/// Reports a compilation error at the given source location.
	/// </summary>
	void ReportError(string message, SourceLocation location);

	/// <summary>
	/// Registers a jump target address for CDL generation.
	/// </summary>
	void RegisterJumpTarget(long address);

	/// <summary>
	/// Registers a subroutine entry address for CDL generation.
	/// </summary>
	void RegisterSubroutineEntry(long address);

	/// <summary>
	/// Records a cross-reference from one address to another.
	/// </summary>
	/// <param name="fromAddress">The source address of the reference.</param>
	/// <param name="toAddress">The target address being referenced.</param>
	/// <param name="type">Cross-reference type (1=Jsr, 2=Jmp, 3=Branch).</param>
	void AddCrossReference(uint fromAddress, uint toAddress, int type);
}
