// ============================================================================
// Lexer.cs - Assembly Language Tokenizer
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

namespace Poppy.Core.Lexer;

using System.Collections.Frozen;

/// <summary>
/// Tokenizes Poppy assembly source code into a stream of tokens.
/// </summary>
public sealed class Lexer {
	private readonly string _source;
	private readonly string _filePath;
	private readonly IReadOnlySet<string>? _targetMnemonics;
	private int _position;
	private int _line;
	private int _column;

	/// <summary>
	/// Creates a new lexer for the given source code.
	/// </summary>
	/// <param name="source">The source code to tokenize.</param>
	/// <param name="filePath">The path to the source file (for error reporting).</param>
	/// <param name="targetMnemonics">Optional target-specific mnemonic set. If null, all known mnemonics are accepted.</param>
	public Lexer(string source, string filePath = "<input>", IReadOnlySet<string>? targetMnemonics = null) {
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_filePath = filePath;
		_targetMnemonics = targetMnemonics;
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
		List<Token> tokens = [];
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

	private TokenType ClassifyIdentifier(string text) {
		// Check if it's a directive (starts with .)
		if (text.StartsWith('.')) {
			return TokenType.Directive;
		}

		// Check if it's a known mnemonic (with or without size suffix)

		// Strip size suffix for mnemonic check (e.g., lda.b -> lda)
		var baseText = text;
		if (text.Length > 2 && text[^2] == '.') {
			var suffix = text[^1];
			if (suffix is 'b' or 'B' or 'w' or 'W' or 'l' or 'L') {
				baseText = text[..^2];
			}
		}

		if (IsMnemonic(baseText)) {
			return TokenType.Mnemonic;
		}

		return TokenType.Identifier;
	}

	private bool IsMnemonic(string text) {
		// If a target-specific mnemonic set was provided, use it for precise matching
		if (_targetMnemonics is not null) {
			return _targetMnemonics.Contains(text) || IsConditionalMnemonicVariant(text, _targetMnemonics);
		}

		// Fallback: accept all known mnemonics from all architectures (case-insensitive FrozenSet)
		return s_allMnemonics.Contains(text) || IsConditionalMnemonicVariant(text, s_allMnemonics);
	}

	private static bool IsConditionalMnemonicVariant(string text, IReadOnlySet<string> knownMnemonics) {
		if (text.Length <= 2) {
			return false;
		}

		var suffix = text[^2..];
		if (!s_armConditionSuffixes.Contains(suffix)) {
			return false;
		}

		var baseMnemonic = text[..^2];
		return knownMnemonics.Contains(baseMnemonic);
	}

	private static readonly FrozenSet<string> s_armConditionSuffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
		"eq", "ne", "cs", "hs", "cc", "lo", "mi", "pl",
		"vs", "vc", "hi", "ls", "ge", "lt", "gt", "le", "al"
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// All known mnemonics across all supported architectures.
	/// Used as fallback when no target-specific mnemonic set is provided.
	/// </summary>
	private static readonly FrozenSet<string> s_allMnemonics = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
		// 6502 mnemonics
		"adc", "and", "asl", "bcc", "bcs", "beq", "bit", "bmi",
		"bne", "bpl", "brk", "bvc", "bvs", "clc", "cld", "cli",
		"clv", "cmp", "cpx", "cpy", "dec", "dex", "dey", "eor",
		"inc", "inx", "iny", "jmp", "jsr", "lda", "ldx", "ldy",
		"lsr", "nop", "ora", "pha", "php", "pla", "plp", "rol",
		"ror", "rti", "rts", "sbc", "sec", "sed", "sei", "sta",
		"stx", "sty", "tax", "tay", "tsx", "txa", "txs", "tya",
		// 65816 additional mnemonics
		"bra", "brl", "cop", "jml", "jsl", "mvn", "mvp", "pea",
		"pei", "per", "phb", "phd", "phk", "phx", "phy", "plb",
		"pld", "plx", "ply", "rep", "rtl", "sep", "stp", "stz",
		"tcd", "tcs", "tdc", "trb", "tsb", "tsc", "txy", "tyx",
		"wai", "wdm", "xba", "xce",
		// Game Boy SM83 mnemonics (not already covered)
		"ld", "ldh", "ldi", "ldd", "add", "sub", "cp",
		"rl", "rr", "rlc", "rrc", "sla", "sra", "srl", "swap",
		"res", "set", "halt", "stop", "di", "ei", "reti", "rst",
		"ccf", "scf", "daa", "cpl", "jr", "jp", "call", "ret",
		"push", "pop",
		// HuC6280 (PC Engine / TurboGrafx-16) specific mnemonics
		"csh", "csl", "tam", "tma",
		"st0", "st1", "st2",
		"tii", "tdd", "tin", "tia", "tai",
		"sax", "say", "sxy", "tst",
		// HuC6280 bit-indexed instructions (bbr0-7, bbs0-7, rmb0-7, smb0-7)
		"bbr0", "bbr1", "bbr2", "bbr3", "bbr4", "bbr5", "bbr6", "bbr7",
		"bbs0", "bbs1", "bbs2", "bbs3", "bbs4", "bbs5", "bbs6", "bbs7",
		"rmb0", "rmb1", "rmb2", "rmb3", "rmb4", "rmb5", "rmb6", "rmb7",
		"smb0", "smb1", "smb2", "smb3", "smb4", "smb5", "smb6", "smb7",
		// V30MZ (WonderSwan) mnemonics (not already covered by 6502/SM83)
		"aaa", "aad", "aam", "aas", "cbw", "cmc", "cwd", "das",
		"div", "hlt", "idiv", "imul", "in", "int", "int3", "into",
		"iret", "lahf", "lea", "mov", "mul", "neg", "not", "or",
		"out", "popa", "popf", "pusha", "pushf", "rcl", "rcr",
		"retf", "sahf", "sal", "sar", "shr", "shl",
		"sbb", "stc", "std", "sti", "test", "wait", "xchg", "xlat", "xlatb", "xor",
		// V30MZ conditional jumps
		"ja", "jae", "jb", "jbe", "jc", "jcxz", "je", "jg",
		"jge", "jl", "jle", "jna", "jnae", "jnb", "jnbe", "jnc",
		"jne", "jng", "jnge", "jnl", "jnle", "jno", "jnp", "jns",
		"jnz", "jo", "jpe", "jpo", "js", "jz",
		// V30MZ string/loop instructions
		"cmpsb", "cmpsw", "lodsb", "lodsw", "movsb", "movsw",
		"scasb", "scasw", "stosb", "stosw",
		"loop", "loope", "loopne", "loopnz", "loopz",
		"repe", "repne", "repnz", "repz",
		// M68000 (Genesis/Mega Drive) mnemonics (not already covered)
		"abcd", "adda", "addi", "addq", "addx", "andi",
		"bchg", "bclr", "bset", "bsr", "btst", "chk", "clr",
		"cmpa", "cmpi", "cmpm", "divs", "divu", "exg", "ext",
		"illegal", "link", "movea", "movem", "muls", "mulu",
		"nbcd", "negx", "ori", "reset", "roxl", "roxr",
		"sbcd", "suba", "subi", "subq", "subx", "tas",
		"trap", "trapv", "unlk",
		// M68000 set-on-condition (scc variants)
		"seq", "sne", "sge", "sgt", "sle", "slt", "shi", "sls",
		"scc", "scs", "spl", "smi", "svc", "svs", "sf", "st",
		// M68000 decrement-and-branch
		"dbcc", "dbcs", "dbeq", "dbge", "dbgt", "dbhi", "dble",
		"dbls", "dblt", "dbmi", "dbne", "dbpl", "dbra", "dbt",
		"dbvc", "dbvs", "dbf",
		// ARM7TDMI (GBA) mnemonics (not already covered)
		"b", "bl", "bx", "cmn", "bic", "mvn", "orr",
		"ldr", "ldrb", "ldrh", "ldrsb", "ldrsh",
		"str", "strb", "strh",
		"ldm", "ldmia", "ldmib", "ldmda", "ldmdb",
		"stm", "stmia", "stmib", "stmda", "stmdb",
		"mrs", "msr", "swi", "swp", "swpb",
		"mla", "mlas", "smull", "smulls", "smlal", "smlals", "umull", "umulls", "umlal", "umlals",
		"teq", "lsl", "asr",
		// ARM7TDMI conditional suffixes handled at parser level
		"adds", "subs", "ands", "orrs", "eors", "bics", "muls",
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
}
