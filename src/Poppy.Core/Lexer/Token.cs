// ============================================================================
// Token.cs - Lexer Token Definition
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.Lexer;

/// <summary>
/// Represents a single token from the source code.
/// </summary>
public sealed class Token {
	/// <summary>
	/// The type of this token.
	/// </summary>
	public TokenType Type { get; }

	/// <summary>
	/// The raw text of this token from the source.
	/// </summary>
	public string Text { get; }

	/// <summary>
	/// The numeric value for number tokens.
	/// </summary>
	public long? NumericValue { get; }

	/// <summary>
	/// The location in the source file where this token starts.
	/// </summary>
	public SourceLocation Location { get; }

	/// <summary>
	/// Creates a new token.
	/// </summary>
	public Token(TokenType type, string text, SourceLocation location, long? numericValue = null) {
		Type = type;
		Text = text;
		Location = location;
		NumericValue = numericValue;
	}

	/// <summary>
	/// Returns a string representation of this token.
	/// </summary>
	public override string ToString() {
		if (NumericValue.HasValue) {
			return $"Token({Type}, \"{Text}\", ${NumericValue.Value:x}, {Location})";
		}

		return $"Token({Type}, \"{Text}\", {Location})";
	}
}

/// <summary>
/// Represents a location in a source file.
/// </summary>
public readonly struct SourceLocation {
	/// <summary>
	/// The source file path.
	/// </summary>
	public string FilePath { get; }

	/// <summary>
	/// The 1-based line number.
	/// </summary>
	public int Line { get; }

	/// <summary>
	/// The 1-based column number.
	/// </summary>
	public int Column { get; }

	/// <summary>
	/// The character offset from the start of the file.
	/// </summary>
	public int Offset { get; }

	/// <summary>
	/// Creates a new source location.
	/// </summary>
	/// <param name="filePath">The source file path.</param>
	/// <param name="line">The 1-based line number.</param>
	/// <param name="column">The 1-based column number.</param>
	/// <param name="offset">The character offset from start of file.</param>
	public SourceLocation(string filePath, int line, int column, int offset) {
		FilePath = filePath;
		Line = line;
		Column = column;
		Offset = offset;
	}

	/// <summary>
	/// Returns a string representation of this location.
	/// </summary>
	public override string ToString() => $"{FilePath}:{Line}:{Column}";
}

