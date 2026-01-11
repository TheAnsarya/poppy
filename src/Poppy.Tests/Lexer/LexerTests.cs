// Poppy Compiler - Lexer Unit Tests
// Copyright © 2026

using Poppy.Core.Lexer;

namespace Poppy.Tests.Lexer;

/// <summary>
/// Comprehensive unit tests for the Poppy Lexer.
/// Tests verify the lexer correctly tokenizes assembly source code.
/// </summary>
public class LexerTests
{
	#region Basic Token Tests

	[Fact]
	public void Tokenize_EmptySource_ReturnsOnlyEndOfFile()
	{
		var lexer = new Core.Lexer.Lexer("");
		var tokens = lexer.Tokenize();

		var token = Assert.Single(tokens);
		Assert.Equal(TokenType.EndOfFile, token.Type);
	}

	[Fact]
	public void Tokenize_WhitespaceOnly_ReturnsOnlyEndOfFile()
	{
		var lexer = new Core.Lexer.Lexer("   \t\t   ");
		var tokens = lexer.Tokenize();

		var token = Assert.Single(tokens);
		Assert.Equal(TokenType.EndOfFile, token.Type);
	}

	[Fact]
	public void Tokenize_NewlineOnly_ReturnsNewlineAndEndOfFile()
	{
		var lexer = new Core.Lexer.Lexer("\n");
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Newline, tokens[0].Type);
		Assert.Equal(TokenType.EndOfFile, tokens[1].Type);
	}

	#endregion

	#region Comment Tests

	[Fact]
	public void Tokenize_SemicolonComment_ReturnsCommentToken()
	{
		var lexer = new Core.Lexer.Lexer("; This is a comment");
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Comment, tokens[0].Type);
		Assert.Equal("; This is a comment", tokens[0].Text);
	}

	[Fact]
	public void Tokenize_DoubleSlashComment_ReturnsCommentToken()
	{
		var lexer = new Core.Lexer.Lexer("// This is a comment");
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Comment, tokens[0].Type);
		Assert.Equal("// This is a comment", tokens[0].Text);
	}

	[Fact]
	public void Tokenize_CommentAfterInstruction_ReturnsBothTokens()
	{
		var lexer = new Core.Lexer.Lexer("lda #$00 ; load zero");
		var tokens = lexer.Tokenize();

		Assert.Equal(5, tokens.Count);
		Assert.Equal(TokenType.Mnemonic, tokens[0].Type); // lda is a mnemonic
		Assert.Equal(TokenType.Hash, tokens[1].Type);
		Assert.Equal(TokenType.Number, tokens[2].Type);
		Assert.Equal(TokenType.Comment, tokens[3].Type);
		Assert.Equal(TokenType.EndOfFile, tokens[4].Type);
	}

	#endregion

	#region Number Tests

	[Theory]
	[InlineData("$00", 0x00)]
	[InlineData("$ff", 0xff)]
	[InlineData("$FF", 0xff)]
	[InlineData("$1234", 0x1234)]
	[InlineData("$abcd", 0xabcd)]
	[InlineData("$DEADBEEF", 0xDEADBEEF)]
	public void Tokenize_HexNumber_ReturnsCorrectValue(string input, long expected)
	{
		var lexer = new Core.Lexer.Lexer(input);
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Number, tokens[0].Type);
		Assert.Equal(expected, tokens[0].NumericValue);
	}

	[Theory]
	[InlineData("0", 0)]
	[InlineData("42", 42)]
	[InlineData("255", 255)]
	[InlineData("65535", 65535)]
	[InlineData("12345678", 12345678)]
	public void Tokenize_DecimalNumber_ReturnsCorrectValue(string input, long expected)
	{
		var lexer = new Core.Lexer.Lexer(input);
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Number, tokens[0].Type);
		Assert.Equal(expected, tokens[0].NumericValue);
	}

	[Theory]
	[InlineData("%00000000", 0b00000000)]
	[InlineData("%11111111", 0b11111111)]
	[InlineData("%10101010", 0b10101010)]
	[InlineData("%00001111", 0b00001111)]
	[InlineData("%1111111100000000", 0b1111111100000000)]
	public void Tokenize_BinaryNumber_ReturnsCorrectValue(string input, long expected)
	{
		var lexer = new Core.Lexer.Lexer(input);
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Number, tokens[0].Type);
		Assert.Equal(expected, tokens[0].NumericValue);
	}

	#endregion

	#region String Tests

	[Fact]
	public void Tokenize_DoubleQuotedString_ReturnsStringToken()
	{
		var lexer = new Core.Lexer.Lexer("\"Hello, World!\"");
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.String, tokens[0].Type);
		Assert.Equal("Hello, World!", tokens[0].Text);
	}

	[Fact]
	public void Tokenize_SingleQuotedCharacter_ReturnsCharacterToken()
	{
		// Single quotes are for character literals, not strings
		var lexer = new Core.Lexer.Lexer("'A'");
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Character, tokens[0].Type);
		Assert.Equal("A", tokens[0].Text);
	}

	#endregion

	#region Identifier and Mnemonic Tests

	[Theory]
	[InlineData("label")]
	[InlineData("_private")]
	[InlineData("Label123")]
	[InlineData("some_long_name")]
	[InlineData("CamelCase")]
	public void Tokenize_Identifier_ReturnsIdentifierToken(string input)
	{
		var lexer = new Core.Lexer.Lexer(input);
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Identifier, tokens[0].Type);
		Assert.Equal(input, tokens[0].Text);
	}

	[Theory]
	[InlineData("lda")]
	[InlineData("sta")]
	[InlineData("jmp")]
	[InlineData("jsr")]
	[InlineData("rts")]
	[InlineData("rti")]
	[InlineData("nop")]
	public void Tokenize_Mnemonic_ReturnsMnemonicToken(string input)
	{
		var lexer = new Core.Lexer.Lexer(input);
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Mnemonic, tokens[0].Type);
		Assert.Equal(input, tokens[0].Text);
	}

	#endregion

	#region Operator Tests

	[Theory]
	[InlineData("+", TokenType.Plus)]
	[InlineData("-", TokenType.Minus)]
	[InlineData("*", TokenType.Star)]
	[InlineData("/", TokenType.Slash)]
	[InlineData("&", TokenType.Ampersand)]
	[InlineData("|", TokenType.Pipe)]
	[InlineData("^", TokenType.Caret)]
	[InlineData("~", TokenType.Tilde)]
	[InlineData("<", TokenType.LessThan)]
	[InlineData(">", TokenType.GreaterThan)]
	[InlineData("=", TokenType.Equals)]
	[InlineData("!", TokenType.Bang)]
	public void Tokenize_SingleOperator_ReturnsCorrectToken(string input, TokenType expected)
	{
		var lexer = new Core.Lexer.Lexer(input);
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(expected, tokens[0].Type);
	}

	[Theory]
	[InlineData("<<", TokenType.LeftShift)]
	[InlineData(">>", TokenType.RightShift)]
	[InlineData("==", TokenType.EqualsEquals)]
	[InlineData("!=", TokenType.BangEquals)]
	[InlineData("<=", TokenType.LessEquals)]
	[InlineData(">=", TokenType.GreaterEquals)]
	[InlineData("&&", TokenType.AmpersandAmpersand)]
	[InlineData("||", TokenType.PipePipe)]
	public void Tokenize_DoubleOperator_ReturnsCorrectToken(string input, TokenType expected)
	{
		var lexer = new Core.Lexer.Lexer(input);
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(expected, tokens[0].Type);
	}

	#endregion

	#region Punctuation Tests

	[Theory]
	[InlineData("(", TokenType.LeftParen)]
	[InlineData(")", TokenType.RightParen)]
	[InlineData("[", TokenType.LeftBracket)]
	[InlineData("]", TokenType.RightBracket)]
	[InlineData(",", TokenType.Comma)]
	[InlineData(":", TokenType.Colon)]
	[InlineData("#", TokenType.Hash)]
	public void Tokenize_Punctuation_ReturnsCorrectToken(string input, TokenType expected)
	{
		var lexer = new Core.Lexer.Lexer(input);
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(expected, tokens[0].Type);
	}

	[Fact]
	public void Tokenize_AtSign_ReturnsIdentifier()
	{
		// @ by itself is a local label identifier prefix
		var lexer = new Core.Lexer.Lexer("@");
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Identifier, tokens[0].Type);
		Assert.Equal("@", tokens[0].Text);
	}

	[Fact]
	public void Tokenize_AtLabel_ReturnsIdentifier()
	{
		// @loop is a local label
		var lexer = new Core.Lexer.Lexer("@loop");
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Identifier, tokens[0].Type);
		Assert.Equal("@loop", tokens[0].Text);
	}

	#endregion

	#region Directive Tests

	[Theory]
	[InlineData(".org")]
	[InlineData(".base")]
	[InlineData(".byte")]
	[InlineData(".word")]
	[InlineData(".long")]
	[InlineData(".dword")]
	[InlineData(".text")]
	[InlineData(".ascii")]
	[InlineData(".asciiz")]
	[InlineData(".incbin")]
	[InlineData(".include")]
	[InlineData(".macro")]
	[InlineData(".endmacro")]
	[InlineData(".if")]
	[InlineData(".else")]
	[InlineData(".endif")]
	[InlineData(".ifdef")]
	[InlineData(".ifndef")]
	[InlineData(".define")]
	[InlineData(".equ")]
	[InlineData(".enum")]
	[InlineData(".endenum")]
	[InlineData(".struct")]
	[InlineData(".endstruct")]
	[InlineData(".proc")]
	[InlineData(".endproc")]
	[InlineData(".scope")]
	[InlineData(".endscope")]
	[InlineData(".segment")]
	[InlineData(".bank")]
	[InlineData(".fillbyte")]
	[InlineData(".fill")]
	[InlineData(".align")]
	[InlineData(".error")]
	[InlineData(".warning")]
	[InlineData(".assert")]
	public void Tokenize_Directive_ReturnsDirectiveToken(string input)
	{
		var lexer = new Core.Lexer.Lexer(input);
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Directive, tokens[0].Type);
		Assert.Equal(input, tokens[0].Text);
	}

	[Fact]
	public void Tokenize_LocalLabel_ReturnsDirectiveToken()
	{
		// Local labels like .localLabel are tokenized as Directive
		// The parser will distinguish local labels from directives
		var lexer = new Core.Lexer.Lexer(".localLabel");
		var tokens = lexer.Tokenize();

		Assert.Equal(2, tokens.Count);
		Assert.Equal(TokenType.Directive, tokens[0].Type);
		Assert.Equal(".localLabel", tokens[0].Text);
	}

	#endregion

	#region Instruction Tests

	[Fact]
	public void Tokenize_ImmediateAddressing_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("lda #$40");
		var tokens = lexer.Tokenize();

		Assert.Equal(4, tokens.Count);
		Assert.Equal(TokenType.Mnemonic, tokens[0].Type);
		Assert.Equal("lda", tokens[0].Text);
		Assert.Equal(TokenType.Hash, tokens[1].Type);
		Assert.Equal(TokenType.Number, tokens[2].Type);
		Assert.Equal(0x40, tokens[2].NumericValue);
		Assert.Equal(TokenType.EndOfFile, tokens[3].Type);
	}

	[Fact]
	public void Tokenize_AbsoluteAddressing_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("sta $2000");
		var tokens = lexer.Tokenize();

		Assert.Equal(3, tokens.Count);
		Assert.Equal(TokenType.Mnemonic, tokens[0].Type);
		Assert.Equal("sta", tokens[0].Text);
		Assert.Equal(TokenType.Number, tokens[1].Type);
		Assert.Equal(0x2000, tokens[1].NumericValue);
	}

	[Fact]
	public void Tokenize_IndexedAddressing_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("lda $2000,x");
		var tokens = lexer.Tokenize();

		Assert.Equal(5, tokens.Count);
		Assert.Equal(TokenType.Mnemonic, tokens[0].Type);
		Assert.Equal("lda", tokens[0].Text);
		Assert.Equal(TokenType.Number, tokens[1].Type);
		Assert.Equal(TokenType.Comma, tokens[2].Type);
		Assert.Equal(TokenType.Identifier, tokens[3].Type);
		Assert.Equal("x", tokens[3].Text);
	}

	[Fact]
	public void Tokenize_IndirectAddressing_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("jmp ($fffc)");
		var tokens = lexer.Tokenize();

		Assert.Equal(5, tokens.Count);
		Assert.Equal(TokenType.Mnemonic, tokens[0].Type);
		Assert.Equal("jmp", tokens[0].Text);
		Assert.Equal(TokenType.LeftParen, tokens[1].Type);
		Assert.Equal(TokenType.Number, tokens[2].Type);
		Assert.Equal(0xfffc, tokens[2].NumericValue);
		Assert.Equal(TokenType.RightParen, tokens[3].Type);
	}

	[Fact]
	public void Tokenize_IndexedIndirectAddressing_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("lda ($00,x)");
		var tokens = lexer.Tokenize();

		Assert.Equal(7, tokens.Count);
		Assert.Equal(TokenType.Mnemonic, tokens[0].Type); // lda
		Assert.Equal(TokenType.LeftParen, tokens[1].Type);
		Assert.Equal(TokenType.Number, tokens[2].Type);
		Assert.Equal(TokenType.Comma, tokens[3].Type);
		Assert.Equal(TokenType.Identifier, tokens[4].Type); // x
		Assert.Equal(TokenType.RightParen, tokens[5].Type);
	}

	[Fact]
	public void Tokenize_IndirectIndexedAddressing_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("lda ($00),y");
		var tokens = lexer.Tokenize();

		Assert.Equal(7, tokens.Count);
		Assert.Equal(TokenType.Mnemonic, tokens[0].Type); // lda
		Assert.Equal(TokenType.LeftParen, tokens[1].Type);
		Assert.Equal(TokenType.Number, tokens[2].Type);
		Assert.Equal(TokenType.RightParen, tokens[3].Type);
		Assert.Equal(TokenType.Comma, tokens[4].Type);
		Assert.Equal(TokenType.Identifier, tokens[5].Type); // y
	}

	#endregion

	#region Complex Source Tests

	[Fact]
	public void Tokenize_LabelDefinition_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("main_loop:");
		var tokens = lexer.Tokenize();

		Assert.Equal(3, tokens.Count);
		Assert.Equal(TokenType.Identifier, tokens[0].Type);
		Assert.Equal("main_loop", tokens[0].Text);
		Assert.Equal(TokenType.Colon, tokens[1].Type);
	}

	[Fact]
	public void Tokenize_MultiLineSource_ReturnsCorrectTokens()
	{
		var source = """
			.org $8000
			main:
				lda #$00
				sta $2000
			""";

		var lexer = new Core.Lexer.Lexer(source);
		var tokens = lexer.Tokenize();

		// Find all meaningful tokens (ignore newlines and EOF for this check)
		// Expected: .org, $8000, main, :, lda, #, $00, sta, $2000 = 9 tokens
		var meaningfulTokens = tokens.Where(t =>
			t.Type != TokenType.Newline &&
			t.Type != TokenType.EndOfFile).ToList();

		Assert.Equal(9, meaningfulTokens.Count);
		Assert.Equal(TokenType.Directive, meaningfulTokens[0].Type);
		Assert.Equal(".org", meaningfulTokens[0].Text);
		Assert.Equal(0x8000, meaningfulTokens[1].NumericValue);
		Assert.Equal("main", meaningfulTokens[2].Text);
		Assert.Equal(TokenType.Colon, meaningfulTokens[3].Type);
	}

	[Fact]
	public void Tokenize_MacroDefinition_ReturnsCorrectTokens()
	{
		var source = """
			.macro PUSH_ALL
				pha
				phx
				phy
			.endmacro
			""";

		var lexer = new Core.Lexer.Lexer(source);
		var tokens = lexer.Tokenize();

		var directives = tokens.Where(t => t.Type == TokenType.Directive).ToList();

		Assert.Equal(2, directives.Count);
		Assert.Equal(".macro", directives[0].Text);
		Assert.Equal(".endmacro", directives[1].Text);
	}

	[Fact]
	public void Tokenize_ConditionalAssembly_ReturnsCorrectTokens()
	{
		var source = """
			.ifdef DEBUG
				lda #$ff
			.else
				lda #$00
			.endif
			""";

		var lexer = new Core.Lexer.Lexer(source);
		var tokens = lexer.Tokenize();

		var directives = tokens.Where(t => t.Type == TokenType.Directive).ToList();

		Assert.Equal(3, directives.Count);
		Assert.Equal(".ifdef", directives[0].Text);
		Assert.Equal(".else", directives[1].Text);
		Assert.Equal(".endif", directives[2].Text);
	}

	[Fact]
	public void Tokenize_Expression_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("lda #<(label+$10)");
		var tokens = lexer.Tokenize();

		Assert.Contains(tokens, t => t.Type == TokenType.LessThan);
		Assert.Contains(tokens, t => t.Type == TokenType.LeftParen);
		Assert.Contains(tokens, t => t.Type == TokenType.Plus);
		Assert.Contains(tokens, t => t.Type == TokenType.RightParen);
	}

	[Fact]
	public void Tokenize_DataDirective_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer(".byte $00, $01, $02, $03");
		var tokens = lexer.Tokenize();

		Assert.Equal(TokenType.Directive, tokens[0].Type);
		Assert.Equal(".byte", tokens[0].Text);

		var numbers = tokens.Where(t => t.Type == TokenType.Number).ToList();
		Assert.Equal(4, numbers.Count);
		Assert.Equal(0, numbers[0].NumericValue);
		Assert.Equal(1, numbers[1].NumericValue);
		Assert.Equal(2, numbers[2].NumericValue);
		Assert.Equal(3, numbers[3].NumericValue);
	}

	#endregion

	#region Location Tracking Tests

	[Fact]
	public void Tokenize_TracksLineNumbers()
	{
		var source = "lda\nsta\njmp";
		var lexer = new Core.Lexer.Lexer(source);
		var tokens = lexer.Tokenize();

		var mnemonics = tokens.Where(t => t.Type == TokenType.Mnemonic).ToList();

		Assert.Equal(3, mnemonics.Count);
		Assert.Equal(1, mnemonics[0].Location.Line);
		Assert.Equal(2, mnemonics[1].Location.Line);
		Assert.Equal(3, mnemonics[2].Location.Line);
	}

	[Fact]
	public void Tokenize_TracksColumnNumbers()
	{
		var lexer = new Core.Lexer.Lexer("  lda #$00");
		var tokens = lexer.Tokenize();

		var mnemonics = tokens.Where(t => t.Type == TokenType.Mnemonic).ToList();
		Assert.Single(mnemonics);
		Assert.Equal(3, mnemonics[0].Location.Column); // After 2 spaces
	}

	[Fact]
	public void Tokenize_TracksFilename()
	{
		var lexer = new Core.Lexer.Lexer("lda", "test.asm");
		var tokens = lexer.Tokenize();

		Assert.Equal("test.asm", tokens[0].Location.FilePath);
	}

	#endregion

	#region Error Handling Tests

	[Fact]
	public void Tokenize_UnterminatedString_ReturnsErrorToken()
	{
		var lexer = new Core.Lexer.Lexer("\"unterminated string");
		var tokens = lexer.Tokenize();

		Assert.Contains(tokens, t => t.Type == TokenType.Error);
	}

	#endregion

	#region 65816 Specific Tests

	[Fact]
	public void Tokenize_65816_LongAddressing_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("lda $7e0000");
		var tokens = lexer.Tokenize();

		Assert.Equal(3, tokens.Count);
		Assert.Equal(TokenType.Mnemonic, tokens[0].Type);
		Assert.Equal(TokenType.Number, tokens[1].Type);
		Assert.Equal(0x7e0000, tokens[1].NumericValue);
	}

	[Fact]
	public void Tokenize_65816_StackRelative_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("lda $01,s");
		var tokens = lexer.Tokenize();

		Assert.Contains(tokens, t => t.Text == "lda");
		Assert.Contains(tokens, t => t.NumericValue == 0x01);
		Assert.Contains(tokens, t => t.Text == "s");
	}

	[Fact]
	public void Tokenize_65816_BlockMove_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("mvn $00,$7e");
		var tokens = lexer.Tokenize();

		Assert.Contains(tokens, t => t.Text == "mvn");
		Assert.Equal(2, tokens.Count(t => t.Type == TokenType.Number));
	}

	#endregion

	#region Game Boy Specific Tests

	[Fact]
	public void Tokenize_GB_LoadHL_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("ld [hl],a");
		var tokens = lexer.Tokenize();

		Assert.Contains(tokens, t => t.Text == "ld");
		Assert.Contains(tokens, t => t.Type == TokenType.LeftBracket);
		Assert.Contains(tokens, t => t.Text == "hl");
		Assert.Contains(tokens, t => t.Type == TokenType.RightBracket);
		Assert.Contains(tokens, t => t.Text == "a");
	}

	[Fact]
	public void Tokenize_GB_BitInstruction_ReturnsCorrectTokens()
	{
		var lexer = new Core.Lexer.Lexer("bit 7,a");
		var tokens = lexer.Tokenize();

		Assert.Contains(tokens, t => t.Text == "bit");
		Assert.Contains(tokens, t => t.NumericValue == 7);
		Assert.Contains(tokens, t => t.Text == "a");
	}

	#endregion

	#region Size Suffix Tests

	[Fact]
	public void Tokenize_SizeSuffix_ReturnsCorrectToken()
	{
		var lexer = new Core.Lexer.Lexer("lda.b #$00");
		var tokens = lexer.Tokenize();

		Assert.Equal(4, tokens.Count);
		Assert.Equal(TokenType.Mnemonic, tokens[0].Type);
		Assert.Equal("lda.b", tokens[0].Text);
	}

	[Fact]
	public void Tokenize_WordSizeSuffix_ReturnsCorrectToken()
	{
		var lexer = new Core.Lexer.Lexer("lda.w #$0000");
		var tokens = lexer.Tokenize();

		Assert.Equal(TokenType.Mnemonic, tokens[0].Type);
		Assert.Equal("lda.w", tokens[0].Text);
	}

	[Fact]
	public void Tokenize_LongSizeSuffix_ReturnsCorrectToken()
	{
		var lexer = new Core.Lexer.Lexer("jsr.l $c00000");
		var tokens = lexer.Tokenize();

		Assert.Equal(TokenType.Mnemonic, tokens[0].Type);
		Assert.Equal("jsr.l", tokens[0].Text);
	}

	#endregion
}
