// ============================================================================
// Lexer.cs - Assembly Language Tokenizer
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.Lexer;

/// <summary>
/// Tokenizes Poppy assembly source code into a stream of tokens.
/// </summary>
public sealed class Lexer {
	private readonly string _source;
	private readonly string _filePath;
	private int _position;
	private int _line;
	private int _column;

	/// <summary>
	/// Creates a new lexer for the given source code.
	/// </summary>
	/// <param name="source">The source code to tokenize.</param>
	/// <param name="filePath">The path to the source file (for error reporting).</param>
	public Lexer(string source, string filePath = "<input>") {
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_filePath = filePath;
		_position = 0;
		_line = 1;
		_column = 1;
	}

	/// <summary>
	/// Gets the next token from the source code.
	/// </summary>
	public Token NextToken() {
		SkipWhitespace();

		if (IsAtEnd()) {
			return MakeToken(TokenType.EndOfFile, "");
		}

		var location = CurrentLocation();
		var c = Advance();

		// Newlines
		if (c == '\n') {
			return MakeToken(TokenType.Newline, "\n", location);
		}

		// Comments
		if (c == ';') {
			return ScanLineComment(location, 1);
		}

		if (c == '/' && Peek() == '*') {
			Advance(); // consume *
			return ScanBlockComment(location);
		}

		if (c == '/' && Peek() == '/') {
			Advance(); // consume second /
			return ScanLineComment(location, 2);
		}

		// Numbers
		if (c == '$') {
			return ScanHexNumber(location);
		}

		if (c == '%' && IsDigit(Peek(), 2)) {
			return ScanBinaryNumber(location);
		}

		if (IsDigit(c, 10)) {
			return ScanDecimalNumber(location, c);
		}

		// Strings
		if (c == '"') {
			return ScanString(location);
		}

		if (c == '\'') {
			return ScanCharacter(location);
		}

		// Directives start with . followed by an identifier
		if (c == '.' && IsIdentifierStart(Peek())) {
			return ScanDirective(location);
		}

		// Identifiers (including mnemonics)
		if (IsIdentifierStart(c)) {
			return ScanIdentifier(location, c);
		}

		// Operators and punctuation
		return ScanOperator(location, c);
	}

	/// <summary>
	/// Tokenizes the entire source and returns all tokens.
	/// </summary>
	public List<Token> Tokenize() {
		var tokens = new List<Token>();
		Token token;

		do {
			token = NextToken();
			tokens.Add(token);
		} while (token.Type != TokenType.EndOfFile);

		return tokens;
	}

	// ========================================================================
	// Scanning Methods
	// ========================================================================

	private Token ScanLineComment(SourceLocation location, int prefixLength) {
		var start = _position - prefixLength;

		while (!IsAtEnd() && Peek() != '\n') {
			Advance();
		}

		var text = _source[start.._position];
		return MakeToken(TokenType.Comment, text, location);
	}

	private Token ScanBlockComment(SourceLocation location) {
		var start = _position - 2;
		var depth = 1;

		while (!IsAtEnd() && depth > 0) {
			if (Peek() == '/' && PeekNext() == '*') {
				Advance();
				Advance();
				depth++;
			} else if (Peek() == '*' && PeekNext() == '/') {
				Advance();
				Advance();
				depth--;
			} else {
				if (Peek() == '\n') {
					_line++;
					_column = 0;
				}

				Advance();
			}
		}

		var text = _source[start.._position];
		return MakeToken(TokenType.Comment, text, location);
	}

	private Token ScanHexNumber(SourceLocation location) {
		var start = _position - 1;
		long value = 0;

		while (IsDigit(Peek(), 16)) {
			var digit = (long)HexDigitValue(Advance());
			value = (value << 4) | digit;
		}

		// Check for bank:address notation ($bb:aaaa)
		// This is common in SNES development for 24-bit addresses
		if (Peek() == ':' && IsDigit(PeekNext(), 16)) {
			Advance(); // consume ':'

			// The value we have so far is the bank byte
			var bank = value;
			value = 0;

			// Parse the address portion
			while (IsDigit(Peek(), 16)) {
				var digit = (long)HexDigitValue(Advance());
				value = (value << 4) | digit;
			}

			// Combine: (bank << 16) | address
			value = (bank << 16) | (value & 0xffff);
		}

		var text = _source[start.._position];
		return MakeToken(TokenType.Number, text, location, value);
	}

	private Token ScanBinaryNumber(SourceLocation location) {
		var start = _position - 1;
		long value = 0;

		while (IsDigit(Peek(), 2) || Peek() == '_') {
			var c = Advance();
			if (c != '_') {
				value = (value << 1) | (c == '1' ? 1L : 0L);
			}
		}

		var text = _source[start.._position];
		return MakeToken(TokenType.Number, text, location, value);
	}

	private Token ScanDecimalNumber(SourceLocation location, char first) {
		var start = _position - 1;
		long value = first - '0';

		while (IsDigit(Peek(), 10)) {
			value = (value * 10) + (Advance() - '0');
		}

		var text = _source[start.._position];
		return MakeToken(TokenType.Number, text, location, value);
	}

	private Token ScanString(SourceLocation location) {
		var start = _position;

		while (!IsAtEnd() && Peek() != '"' && Peek() != '\n') {
			if (Peek() == '\\' && PeekNext() == '"') {
				Advance(); // skip backslash
			}

			Advance();
		}

		if (IsAtEnd() || Peek() == '\n') {
			return MakeToken(TokenType.Error, "Unterminated string", location);
		}

		var text = _source[start.._position];
		Advance(); // consume closing quote

		return MakeToken(TokenType.String, text, location);
	}

	private Token ScanCharacter(SourceLocation location) {
		var start = _position;

		if (Peek() == '\\') {
			Advance(); // escape char
		}

		if (!IsAtEnd()) {
			Advance();
		}

		if (Peek() != '\'') {
			return MakeToken(TokenType.Error, "Unterminated character literal", location);
		}

		var text = _source[start.._position];
		Advance(); // consume closing quote

		return MakeToken(TokenType.Character, text, location);
	}

	private Token ScanDirective(SourceLocation location) {
		// Start position is at the . (which is already consumed)
		var start = _position - 1;

		// Scan the identifier part after the .
		while (IsIdentifierContinue(Peek())) {
			Advance();
		}

		var text = _source[start.._position];
		return MakeToken(TokenType.Directive, text, location);
	}

	private Token ScanIdentifier(SourceLocation location, char first) {
		var start = _position - 1;

		while (IsIdentifierContinue(Peek())) {
			Advance();
		}

		// Check for size suffix (.b, .w, .l)
		if (Peek() == '.' && IsSizeSuffix(PeekNext())) {
			Advance(); // consume .
			Advance(); // consume suffix
		}

		var text = _source[start.._position];
		var type = ClassifyIdentifier(text);

		return MakeToken(type, text, location);
	}

	private Token ScanOperator(SourceLocation location, char c) {
		return c switch {
			'+' => ScanPlusOrNamedAnonymous(location),
			'-' => ScanMinusOrNamedAnonymous(location),
			'*' => MakeToken(TokenType.Star, "*", location),
			'/' => MakeToken(TokenType.Slash, "/", location),
			'&' => Match('&') ? MakeToken(TokenType.AmpersandAmpersand, "&&", location)
				   : MakeToken(TokenType.Ampersand, "&", location),
			'|' => Match('|') ? MakeToken(TokenType.PipePipe, "||", location)
				   : MakeToken(TokenType.Pipe, "|", location),
			'^' => MakeToken(TokenType.Caret, "^", location),
			'~' => MakeToken(TokenType.Tilde, "~", location),
			'<' => Match('<') ? MakeToken(TokenType.LeftShift, "<<", location)
				   : Match('=') ? MakeToken(TokenType.LessEquals, "<=", location)
				   : MakeToken(TokenType.LessThan, "<", location),
			'>' => Match('>') ? MakeToken(TokenType.RightShift, ">>", location)
				   : Match('=') ? MakeToken(TokenType.GreaterEquals, ">=", location)
				   : MakeToken(TokenType.GreaterThan, ">", location),
			'=' => Match('=') ? MakeToken(TokenType.EqualsEquals, "==", location)
				   : MakeToken(TokenType.Equals, "=", location),
			'!' => Match('=') ? MakeToken(TokenType.BangEquals, "!=", location)
				   : MakeToken(TokenType.Bang, "!", location),
			'#' => MakeToken(TokenType.Hash, "#", location),
			':' => MakeToken(TokenType.Colon, ":", location),
			',' => MakeToken(TokenType.Comma, ",", location),
			'.' => MakeToken(TokenType.Dot, ".", location),
			'(' => MakeToken(TokenType.LeftParen, "(", location),
			')' => MakeToken(TokenType.RightParen, ")", location),
			'[' => MakeToken(TokenType.LeftBracket, "[", location),
			']' => MakeToken(TokenType.RightBracket, "]", location),
			'@' => MakeToken(TokenType.At, "@", location),
			'%' => MakeToken(TokenType.Percent, "%", location),
			_ => MakeToken(TokenType.Error, $"Unexpected character: '{c}'", location),
		};
	}

	/// <summary>
	/// Scans a + operator, anonymous forward label (++), or named anonymous forward label (+name).
	/// </summary>
	private Token ScanPlusOrNamedAnonymous(SourceLocation location) {
		// Check for ++ (anonymous forward)
		if (Match('+')) {
			return MakeToken(TokenType.AnonymousForward, "++", location);
		}

		// Check for +name (named anonymous forward)
		if (IsIdentifierStart(Peek())) {
			var start = _position - 1; // include the +
			while (IsIdentifierContinue(Peek())) {
				Advance();
			}

			var text = _source[start.._position];
			return MakeToken(TokenType.NamedAnonymousForward, text, location);
		}

		// Just a plus operator
		return MakeToken(TokenType.Plus, "+", location);
	}

	/// <summary>
	/// Scans a - operator, anonymous backward label (--), or named anonymous backward label (-name).
	/// </summary>
	private Token ScanMinusOrNamedAnonymous(SourceLocation location) {
		// Check for -- (anonymous backward)
		if (Match('-')) {
			return MakeToken(TokenType.AnonymousBackward, "--", location);
		}

		// Check for -name (named anonymous backward)
		if (IsIdentifierStart(Peek())) {
			var start = _position - 1; // include the -
			while (IsIdentifierContinue(Peek())) {
				Advance();
			}

			var text = _source[start.._position];
			return MakeToken(TokenType.NamedAnonymousBackward, text, location);
		}

		// Just a minus operator
		return MakeToken(TokenType.Minus, "-", location);
	}

	// ========================================================================
	// Helper Methods
	// ========================================================================

	private void SkipWhitespace() {
		while (!IsAtEnd()) {
			var c = Peek();
			if (c == ' ' || c == '\t' || c == '\r') {
				Advance();
			} else {
				break;
			}
		}
	}

	private bool IsAtEnd() => _position >= _source.Length;

	private char Peek() => IsAtEnd() ? '\0' : _source[_position];

	private char PeekNext() => _position + 1 >= _source.Length ? '\0' : _source[_position + 1];

	private char Advance() {
		var c = _source[_position++];
		if (c == '\n') {
			_line++;
			_column = 1;
		} else {
			_column++;
		}

		return c;
	}

	private bool Match(char expected) {
		if (IsAtEnd() || _source[_position] != expected) {
			return false;
		}

		_position++;
		_column++;
		return true;
	}

	private SourceLocation CurrentLocation() =>
		new(_filePath, _line, _column, _position);

	private Token MakeToken(TokenType type, string text, SourceLocation? location = null, long? value = null) =>
		new(type, text, location ?? CurrentLocation(), value);

	private static bool IsDigit(char c, int @base) => @base switch {
		2 => c == '0' || c == '1',
		10 => c >= '0' && c <= '9',
		16 => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'),
		_ => false,
	};

	private static int HexDigitValue(char c) => c switch {
		>= '0' and <= '9' => c - '0',
		>= 'a' and <= 'f' => c - 'a' + 10,
		>= 'A' and <= 'F' => c - 'A' + 10,
		_ => 0,
	};

	private static bool IsIdentifierStart(char c) =>
		(c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == '@';

	private static bool IsIdentifierContinue(char c) =>
		IsIdentifierStart(c) || (c >= '0' && c <= '9');

	private static bool IsSizeSuffix(char c) =>
		c == 'b' || c == 'w' || c == 'l' || c == 'B' || c == 'W' || c == 'L';

	private static TokenType ClassifyIdentifier(string text) {
		// Check if it's a directive (starts with .)
		if (text.StartsWith('.')) {
			return TokenType.Directive;
		}

		// Check if it's a known mnemonic (with or without size suffix)
		var lower = text.ToLowerInvariant();

		// Strip size suffix for mnemonic check (e.g., lda.b -> lda)
		var baseText = lower;
		if (lower.Length > 2 && lower[^2] == '.') {
			var suffix = lower[^1];
			if (suffix == 'b' || suffix == 'w' || suffix == 'l') {
				baseText = lower[..^2];
			}
		}

		if (IsMnemonic(baseText)) {
			return TokenType.Mnemonic;
		}

		return TokenType.Identifier;
	}

	private static bool IsMnemonic(string text) {
		// TODO: Make this architecture-aware
		return text switch {
			// 6502 mnemonics
			"adc" or "and" or "asl" or "bcc" or "bcs" or "beq" or "bit" or "bmi" or
			"bne" or "bpl" or "brk" or "bvc" or "bvs" or "clc" or "cld" or "cli" or
			"clv" or "cmp" or "cpx" or "cpy" or "dec" or "dex" or "dey" or "eor" or
			"inc" or "inx" or "iny" or "jmp" or "jsr" or "lda" or "ldx" or "ldy" or
			"lsr" or "nop" or "ora" or "pha" or "php" or "pla" or "plp" or "rol" or
			"ror" or "rti" or "rts" or "sbc" or "sec" or "sed" or "sei" or "sta" or
			"stx" or "sty" or "tax" or "tay" or "tsx" or "txa" or "txs" or "tya" or
			// 65816 additional mnemonics
			"bra" or "brl" or "cop" or "jml" or "jsl" or "mvn" or "mvp" or "pea" or
			"pei" or "per" or "phb" or "phd" or "phk" or "phx" or "phy" or "plb" or
			"pld" or "plx" or "ply" or "rep" or "rtl" or "sep" or "stp" or "stz" or
			"tcd" or "tcs" or "tdc" or "trb" or "tsb" or "tsc" or "txy" or "tyx" or
			"wai" or "wdm" or "xba" or "xce" or
			// Game Boy SM83 mnemonics (not already covered)
			"ld" or "ldh" or "ldi" or "ldd" or "add" or "sub" or "sbc" or "cp" or
			"rl" or "rr" or "rlc" or "rrc" or "sla" or "sra" or "srl" or "swap" or
			"res" or "set" or "halt" or "stop" or "di" or "ei" or "reti" or "rst" or
			"ccf" or "scf" or "daa" or "cpl" or "jr" or "jp" or "call" or "ret" or
			"push" or "pop"
			=> true,
			_ => false,
		};
	}
}

