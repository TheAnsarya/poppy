namespace Poppy.Core.Arch;

using Poppy.Core.CodeGen;

/// <summary>
/// Builds a ROM binary from assembled memory segments.
/// Replaces the if/else chain in CodeGenerator.Generate().
/// </summary>
public interface IRomBuilder {
	/// <summary>
	/// Builds the final ROM binary from assembled output segments.
	/// </summary>
	byte[] Build(IReadOnlyList<OutputSegment> segments);
}
