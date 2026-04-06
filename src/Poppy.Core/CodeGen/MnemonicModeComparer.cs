// ============================================================================
// MnemonicModeComparer.cs - Case-Insensitive Mnemonic + Mode Comparer
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.CodeGen;

/// <summary>
/// Shared comparer for (mnemonic, addressing mode) tuple keys.
/// Uses case-insensitive mnemonic comparison without allocating via ToLowerInvariant.
/// </summary>
internal sealed class MnemonicModeComparer<TMode> : IEqualityComparer<(string Mnemonic, TMode Mode)> where TMode : struct {
	public static readonly MnemonicModeComparer<TMode> Instance = new();

	public bool Equals((string Mnemonic, TMode Mode) x, (string Mnemonic, TMode Mode) y) {
		return string.Equals(x.Mnemonic, y.Mnemonic, StringComparison.OrdinalIgnoreCase)
			&& EqualityComparer<TMode>.Default.Equals(x.Mode, y.Mode);
	}

	public int GetHashCode((string Mnemonic, TMode Mode) obj) {
		return HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Mnemonic), obj.Mode);
	}
}
