// ============================================================================
// TokenType.cs - Token Type Enumeration
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.Lexer;

/// <summary>
/// Defines all token types recognized by the Poppy lexer.
/// </summary>
public enum TokenType {
	// ========================================================================
	// Literals
	// ========================================================================

	/// <summary>Numeric literal (hex $ff, decimal 255, binary %1010)</summary>
	Number,

	/// <summary>String literal ("hello")</summary>
	String,

	/// <summary>Character literal ('A')</summary>
	Character,

	// ========================================================================
	// Identifiers
	// ========================================================================

	/// <summary>General identifier (labels, define names, etc.)</summary>
	Identifier,

	/// <summary>CPU mnemonic (lda, sta, jmp, etc.)</summary>
	Mnemonic,

	/// <summary>Assembler directive (.org, .db, .include, etc.)</summary>
	Directive,

	// ========================================================================
	// Operators - Arithmetic
	// ========================================================================

	/// <summary>Plus sign (+)</summary>
	Plus,

	/// <summary>Minus sign (-)</summary>
	Minus,

	/// <summary>Asterisk/star (*)</summary>
	Star,

	/// <summary>Forward slash (/)</summary>
	Slash,

	/// <summary>Percent sign (%)</summary>
	Percent,

	// ========================================================================
	// Operators - Bitwise
	// ========================================================================

	/// <summary>Ampersand (&amp;)</summary>
	Ampersand,

	/// <summary>Pipe (|)</summary>
	Pipe,

	/// <summary>Caret (^)</summary>
	Caret,

	/// <summary>Tilde (~)</summary>
	Tilde,

	/// <summary>Left shift (&lt;&lt;)</summary>
	LeftShift,

	/// <summary>Right shift (&gt;&gt;)</summary>
	RightShift,

	// ========================================================================
	// Operators - Comparison
	// ========================================================================

	/// <summary>Less than (&lt;)</summary>
	LessThan,

	/// <summary>Greater than (&gt;)</summary>
	GreaterThan,

	/// <summary>Equals (=)</summary>
	Equals,

	/// <summary>Double equals (==)</summary>
	EqualsEquals,

	/// <summary>Not equals (!=)</summary>
	BangEquals,

	/// <summary>Less than or equal (&lt;=)</summary>
	LessEquals,

	/// <summary>Greater than or equal (&gt;=)</summary>
	GreaterEquals,

	// ========================================================================
	// Operators - Logical
	// ========================================================================

	/// <summary>Exclamation mark (!)</summary>
	Bang,

	/// <summary>Logical AND (&amp;&amp;)</summary>
	AmpersandAmpersand,

	/// <summary>Logical OR (||)</summary>
	PipePipe,

	// ========================================================================
	// Punctuation
	// ========================================================================

	/// <summary>Hash/pound (#)</summary>
	Hash,

	/// <summary>Colon (:)</summary>
	Colon,

	/// <summary>Comma (,)</summary>
	Comma,

	/// <summary>Dot/period (.)</summary>
	Dot,

	/// <summary>Left parenthesis (()</summary>
	LeftParen,

	/// <summary>Right parenthesis ())</summary>
	RightParen,

	/// <summary>Left bracket ([)</summary>
	LeftBracket,

	/// <summary>Right bracket (])</summary>
	RightBracket,

	/// <summary>At sign (@)</summary>
	At,

	/// <summary>Dollar sign ($) - standalone</summary>
	Dollar,

	// ========================================================================
	// Whitespace and Structure
	// ========================================================================

	/// <summary>End of line (statement terminator)</summary>
	Newline,

	/// <summary>Comment (; or /* */)</summary>
	Comment,

	/// <summary>End of file</summary>
	EndOfFile,

	// ========================================================================
	// Special
	// ========================================================================

	/// <summary>Anonymous forward label (+)</summary>
	AnonymousForward,

	/// <summary>Anonymous backward label (-)</summary>
	AnonymousBackward,

	/// <summary>Invalid/error token</summary>
	Error,
}

