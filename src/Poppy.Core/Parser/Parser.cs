// ============================================================================
// Parser.cs - Assembly Source Parser
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;

namespace Poppy.Core.Parser;

/// <summary>
/// Parses a stream of tokens into an Abstract Syntax Tree (AST).
/// </summary>
public sealed class Parser {
	private readonly List<Token> _tokens;
	private readonly List<ParseError> _errors;
	private int _current;

	/// <summary>
	/// Gets the list of parse errors encountered.
	/// </summary>
	public IReadOnlyList<ParseError> Errors => _errors;

	/// <summary>
	/// Gets whether parsing encountered any errors.
	/// </summary>
	public bool HasErrors => _errors.Count > 0;

	/// <summary>
	/// Creates a new parser for the given tokens.
	/// </summary>
	public Parser(List<Token> tokens) {
		_tokens = tokens;
		_errors = [];
		_current = 0;
	}

	/// <summary>
	/// Parses the tokens into a program AST.
	/// </summary>
	public ProgramNode Parse() {
		var statements = new List<StatementNode>();

		while (!IsAtEnd()) {
			SkipNewlines();
			if (IsAtEnd()) break;

			try {
				var statement = ParseStatement();
				if (statement is not null) {
					statements.Add(statement);
				}
			}
			catch (ParseException ex) {
				_errors.Add(new ParseError(ex.Message, ex.Location));
				Synchronize();
			}
		}

		var location = _tokens.Count > 0 ? _tokens[0].Location : new SourceLocation("", 1, 1, 0);
		return new ProgramNode(location, statements);
	}

	// ========================================================================
	// Statement Parsing
	// ========================================================================

	private StatementNode? ParseStatement() {
		// Skip comments
		if (Check(TokenType.Comment)) {
			Advance();
			return null;
		}

		// Directive (starts with .)
		if (Check(TokenType.Directive)) {
			return ParseDirective();
		}

		// Label or instruction
		if (Check(TokenType.Identifier)) {
			return ParseLabelOrIdentifier();
		}

		// Mnemonic instruction
		if (Check(TokenType.Mnemonic)) {
			return ParseInstruction();
		}

		// Anonymous label forward (+)
		if (Check(TokenType.Plus)) {
			return ParseAnonymousLabel(isForward: true);
		}

		// Anonymous label backward (-)
		if (Check(TokenType.Minus)) {
			return ParseAnonymousLabel(isForward: false);
		}

		// Skip unexpected tokens
		var token = Advance();
		ReportError($"Unexpected token: {token.Type}", token.Location);
		return null;
	}

	private StatementNode ParseDirective() {
		var token = Advance();
		var directiveName = token.Text[1..]; // Remove leading .

		// Check for macro definition
		if (directiveName.Equals("macro", StringComparison.OrdinalIgnoreCase)) {
			return ParseMacroDefinition(token.Location);
		}

		// Parse directive arguments
		var arguments = new List<ExpressionNode>();

		// Parse first argument if present
		if (!IsAtEndOfStatement()) {
			arguments.Add(ParseExpression());

			// Parse additional comma-separated arguments
			while (Match(TokenType.Comma)) {
				arguments.Add(ParseExpression());
			}
		}

		ExpectEndOfStatement();
		return new DirectiveNode(token.Location, directiveName, arguments);
	}

	private StatementNode ParseLabelOrIdentifier() {
		var token = Advance();
		var isLocal = token.Text.StartsWith('@');

		// If followed by colon, it's a label definition
		if (Check(TokenType.Colon)) {
			Advance(); // consume colon
			return new LabelNode(token.Location, token.Text, isLocal);
		}

		// If followed by equals, it's an assignment (EQU-style)
		if (Check(TokenType.Equals)) {
			Advance(); // consume equals
			var value = ParseExpression();
			ExpectEndOfStatement();
			return new DirectiveNode(token.Location, "equ", [new IdentifierNode(token.Location, token.Text), value]);
		}

		// Otherwise, it could be a macro invocation
		// For now, report error - identifiers at statement level need context
		ReportError($"Expected label definition or assignment, found identifier: {token.Text}", token.Location);
		return new LabelNode(token.Location, token.Text, isLocal); // Return as label for error recovery
	}

	private StatementNode ParseInstruction() {
		var token = Advance();
		var mnemonic = token.Text;
		char? sizeSuffix = null;

		// Check for size suffix (e.g., lda.b)
		if (mnemonic.Length > 2 && mnemonic[^2] == '.') {
			sizeSuffix = char.ToLowerInvariant(mnemonic[^1]);
			mnemonic = mnemonic[..^2];
		}

		// Implied addressing (no operand)
		if (IsAtEndOfStatement()) {
			return new InstructionNode(token.Location, mnemonic, sizeSuffix, null, AddressingMode.Implied);
		}

		// Parse operand and determine addressing mode
		var (operand, addressingMode) = ParseOperand();

		ExpectEndOfStatement();
		return new InstructionNode(token.Location, mnemonic, sizeSuffix, operand, addressingMode);
	}

	private (ExpressionNode? Operand, AddressingMode Mode) ParseOperand() {
		// Accumulator addressing (A or a)
		if (Check(TokenType.Identifier) && CurrentToken.Text.Equals("a", StringComparison.OrdinalIgnoreCase)) {
			Advance();
			return (null, AddressingMode.Accumulator);
		}

		// Immediate addressing (#)
		if (Match(TokenType.Hash)) {
			var expr = ParseExpression();
			return (expr, AddressingMode.Immediate);
		}

		// Indirect addressing (parentheses or brackets)
		if (Check(TokenType.LeftParen)) {
			return ParseIndirectOperand();
		}

		if (Check(TokenType.LeftBracket)) {
			return ParseBracketOperand();
		}

		// Direct/absolute addressing with possible indexing
		var operand = ParseExpression();

		// Check for indexing
		if (Match(TokenType.Comma)) {
			var indexToken = Advance();
			var indexReg = indexToken.Text.ToLowerInvariant();

			return indexReg switch {
				"x" => (operand, AddressingMode.AbsoluteX),
				"y" => (operand, AddressingMode.AbsoluteY),
				"s" => (operand, AddressingMode.StackRelative),
				_ => throw new ParseException($"Invalid index register: {indexToken.Text}", indexToken.Location)
			};
		}

		// Plain absolute/zero page addressing (determined later by value)
		return (operand, AddressingMode.Absolute);
	}

	private (ExpressionNode Operand, AddressingMode Mode) ParseIndirectOperand() {
		Advance(); // consume (

		var expr = ParseExpression();

		// ($00,x) - Indexed Indirect
		if (Match(TokenType.Comma)) {
			var indexToken = Advance();
			if (!indexToken.Text.Equals("x", StringComparison.OrdinalIgnoreCase)) {
				throw new ParseException($"Expected 'X' for indexed indirect, got: {indexToken.Text}", indexToken.Location);
			}
			Expect(TokenType.RightParen, "Expected ')' after indexed indirect operand");

			return (expr, AddressingMode.IndexedIndirect);
		}

		Expect(TokenType.RightParen, "Expected ')' after indirect operand");

		// ($00),y - Indirect Indexed
		if (Match(TokenType.Comma)) {
			var indexToken = Advance();
			if (!indexToken.Text.Equals("y", StringComparison.OrdinalIgnoreCase)) {
				throw new ParseException($"Expected 'Y' for indirect indexed, got: {indexToken.Text}", indexToken.Location);
			}

			return (expr, AddressingMode.IndirectIndexed);
		}

		// Plain indirect (JMP ($fffc))
		return (expr, AddressingMode.Indirect);
	}

	private (ExpressionNode Operand, AddressingMode Mode) ParseBracketOperand() {
		Advance(); // consume [

		var expr = ParseExpression();
		Expect(TokenType.RightBracket, "Expected ']' after bracket operand");

		// [$00],y - Indirect Long Indexed
		if (Match(TokenType.Comma)) {
			var indexToken = Advance();
			if (!indexToken.Text.Equals("y", StringComparison.OrdinalIgnoreCase)) {
				throw new ParseException($"Expected 'Y' for indirect long indexed, got: {indexToken.Text}", indexToken.Location);
			}

			return (expr, AddressingMode.DirectPageIndirectLongY);
		}

		// Plain indirect long
		return (expr, AddressingMode.DirectPageIndirectLong);
	}

	private StatementNode ParseAnonymousLabel(bool isForward) {
		var token = Advance();

		// Check if it's a label definition (followed by colon)
		if (Check(TokenType.Colon)) {
			Advance();
			return new LabelNode(token.Location, isForward ? "+" : "-");
		}

		// Otherwise, treat as an instruction operand (branch target)
		ReportError("Anonymous labels as statement must be followed by ':'", token.Location);
		return new LabelNode(token.Location, isForward ? "+" : "-");
	}

	private MacroDefinitionNode ParseMacroDefinition(SourceLocation location) {
		// Parse macro name
		var nameToken = Expect(TokenType.Identifier, "Expected macro name after .macro");
		var name = nameToken.Text;

		// Parse parameters
		var parameters = new List<string>();
		while (Check(TokenType.Identifier)) {
			parameters.Add(Advance().Text);
			if (!Match(TokenType.Comma)) break;
		}

		ExpectEndOfStatement();

		// Parse body until .endmacro
		var body = new List<StatementNode>();
		while (!IsAtEnd()) {
			SkipNewlines();
			if (IsAtEnd()) break;

			// Check for .endmacro
			if (Check(TokenType.Directive) && CurrentToken.Text.Equals(".endmacro", StringComparison.OrdinalIgnoreCase)) {
				Advance();
				break;
			}

			var statement = ParseStatement();
			if (statement is not null) {
				body.Add(statement);
			}
		}

		return new MacroDefinitionNode(location, name, parameters, body);
	}

	// ========================================================================
	// Expression Parsing (Precedence Climbing)
	// ========================================================================

	private ExpressionNode ParseExpression() {
		return ParseLogicalOr();
	}

	private ExpressionNode ParseLogicalOr() {
		var left = ParseLogicalAnd();

		while (Match(TokenType.PipePipe)) {
			var location = Previous.Location;
			var right = ParseLogicalAnd();
			left = new BinaryExpressionNode(location, left, BinaryOperator.LogicalOr, right);
		}

		return left;
	}

	private ExpressionNode ParseLogicalAnd() {
		var left = ParseBitwiseOr();

		while (Match(TokenType.AmpersandAmpersand)) {
			var location = Previous.Location;
			var right = ParseBitwiseOr();
			left = new BinaryExpressionNode(location, left, BinaryOperator.LogicalAnd, right);
		}

		return left;
	}

	private ExpressionNode ParseBitwiseOr() {
		var left = ParseBitwiseXor();

		while (Match(TokenType.Pipe)) {
			var location = Previous.Location;
			var right = ParseBitwiseXor();
			left = new BinaryExpressionNode(location, left, BinaryOperator.BitwiseOr, right);
		}

		return left;
	}

	private ExpressionNode ParseBitwiseXor() {
		var left = ParseBitwiseAnd();

		while (Match(TokenType.Caret)) {
			var location = Previous.Location;
			var right = ParseBitwiseAnd();
			left = new BinaryExpressionNode(location, left, BinaryOperator.BitwiseXor, right);
		}

		return left;
	}

	private ExpressionNode ParseBitwiseAnd() {
		var left = ParseEquality();

		while (Match(TokenType.Ampersand)) {
			var location = Previous.Location;
			var right = ParseEquality();
			left = new BinaryExpressionNode(location, left, BinaryOperator.BitwiseAnd, right);
		}

		return left;
	}

	private ExpressionNode ParseEquality() {
		var left = ParseComparison();

		while (true) {
			if (Match(TokenType.EqualsEquals)) {
				var location = Previous.Location;
				var right = ParseComparison();
				left = new BinaryExpressionNode(location, left, BinaryOperator.Equal, right);
			}
			else if (Match(TokenType.BangEquals)) {
				var location = Previous.Location;
				var right = ParseComparison();
				left = new BinaryExpressionNode(location, left, BinaryOperator.NotEqual, right);
			}
			else {
				break;
			}
		}

		return left;
	}

	private ExpressionNode ParseComparison() {
		var left = ParseShift();

		while (true) {
			if (Match(TokenType.LessEquals)) {
				var location = Previous.Location;
				var right = ParseShift();
				left = new BinaryExpressionNode(location, left, BinaryOperator.LessOrEqual, right);
			}
			else if (Match(TokenType.GreaterEquals)) {
				var location = Previous.Location;
				var right = ParseShift();
				left = new BinaryExpressionNode(location, left, BinaryOperator.GreaterOrEqual, right);
			}
			else {
				break;
			}
		}

		return left;
	}

	private ExpressionNode ParseShift() {
		var left = ParseAdditive();

		while (true) {
			if (Match(TokenType.LeftShift)) {
				var location = Previous.Location;
				var right = ParseAdditive();
				left = new BinaryExpressionNode(location, left, BinaryOperator.LeftShift, right);
			}
			else if (Match(TokenType.RightShift)) {
				var location = Previous.Location;
				var right = ParseAdditive();
				left = new BinaryExpressionNode(location, left, BinaryOperator.RightShift, right);
			}
			else {
				break;
			}
		}

		return left;
	}

	private ExpressionNode ParseAdditive() {
		var left = ParseMultiplicative();

		while (true) {
			if (Match(TokenType.Plus)) {
				var location = Previous.Location;
				var right = ParseMultiplicative();
				left = new BinaryExpressionNode(location, left, BinaryOperator.Add, right);
			}
			else if (Match(TokenType.Minus)) {
				var location = Previous.Location;
				var right = ParseMultiplicative();
				left = new BinaryExpressionNode(location, left, BinaryOperator.Subtract, right);
			}
			else {
				break;
			}
		}

		return left;
	}

	private ExpressionNode ParseMultiplicative() {
		var left = ParseUnary();

		while (true) {
			if (Match(TokenType.Star)) {
				var location = Previous.Location;
				var right = ParseUnary();
				left = new BinaryExpressionNode(location, left, BinaryOperator.Multiply, right);
			}
			else if (Match(TokenType.Slash)) {
				var location = Previous.Location;
				var right = ParseUnary();
				left = new BinaryExpressionNode(location, left, BinaryOperator.Divide, right);
			}
			else if (Match(TokenType.Percent)) {
				var location = Previous.Location;
				var right = ParseUnary();
				left = new BinaryExpressionNode(location, left, BinaryOperator.Modulo, right);
			}
			else {
				break;
			}
		}

		return left;
	}

	private ExpressionNode ParseUnary() {
		// Check for anonymous label reference first
		// Anonymous labels (+ or -) are used when NOT followed by a primary expression start
		if (Check(TokenType.Plus) || Check(TokenType.Minus)) {
			bool isPlus = Check(TokenType.Plus);
			// Look ahead to see what follows
			int lookahead = _current + 1;
			bool hasPrimary = lookahead < _tokens.Count &&
				IsPrimaryExpressionStart(_tokens[lookahead].Type);

			// If not followed by a primary expression, treat as anonymous label
			if (!hasPrimary) {
				return ParsePrimary(); // This will handle anonymous labels
			}
		}

		// Negation (-)
		if (Match(TokenType.Minus)) {
			var location = Previous.Location;
			var operand = ParseUnary();
			return new UnaryExpressionNode(location, UnaryOperator.Negate, operand);
		}

		// Bitwise NOT (~)
		if (Match(TokenType.Tilde)) {
			var location = Previous.Location;
			var operand = ParseUnary();
			return new UnaryExpressionNode(location, UnaryOperator.BitwiseNot, operand);
		}

		// Logical NOT (!)
		if (Match(TokenType.Bang)) {
			var location = Previous.Location;
			var operand = ParseUnary();
			return new UnaryExpressionNode(location, UnaryOperator.LogicalNot, operand);
		}

		// Low byte (<)
		if (Match(TokenType.LessThan)) {
			var location = Previous.Location;
			var operand = ParseUnary();
			return new UnaryExpressionNode(location, UnaryOperator.LowByte, operand);
		}

		// High byte (>)
		if (Match(TokenType.GreaterThan)) {
			var location = Previous.Location;
			var operand = ParseUnary();
			return new UnaryExpressionNode(location, UnaryOperator.HighByte, operand);
		}

		// Bank byte (^) - 65816 specific
		if (Match(TokenType.Caret)) {
			var location = Previous.Location;
			var operand = ParseUnary();
			return new UnaryExpressionNode(location, UnaryOperator.BankByte, operand);
		}

		return ParsePrimary();
	}

	/// <summary>
	/// Checks if a token type can start a primary expression.
	/// </summary>
	private static bool IsPrimaryExpressionStart(TokenType type) {
		return type switch {
			TokenType.Number => true,
			TokenType.String => true,
			TokenType.Identifier => true,
			TokenType.Mnemonic => true,
			TokenType.Star => true,
			TokenType.LeftParen => true,
			_ => false
		};
	}

	private ExpressionNode ParsePrimary() {
		// Number literal
		if (Check(TokenType.Number)) {
			var token = Advance();
			return new NumberLiteralNode(token.Location, token.NumericValue ?? 0);
		}

		// String literal
		if (Check(TokenType.String)) {
			var token = Advance();
			return new StringLiteralNode(token.Location, token.Text);
		}

		// Identifier
		if (Check(TokenType.Identifier) || Check(TokenType.Mnemonic)) {
			var token = Advance();
			return new IdentifierNode(token.Location, token.Text);
		}

		// Current address (*)
		if (Match(TokenType.Star)) {
			return new IdentifierNode(Previous.Location, "*");
		}

		// Anonymous label reference (+ or -)
		// Handles +, ++, +++, ... and -, --, ---, ...
		if (Check(TokenType.Plus) || Check(TokenType.Minus)) {
			var location = CurrentToken.Location;
			bool isForward = Check(TokenType.Plus);
			var builder = new System.Text.StringBuilder();
			while (Check(TokenType.Plus) == isForward && (Check(TokenType.Plus) || Check(TokenType.Minus))) {
				builder.Append(isForward ? '+' : '-');
				Advance();
			}
			return new IdentifierNode(location, builder.ToString());
		}

		// Grouped expression
		if (Match(TokenType.LeftParen)) {
			var expr = ParseExpression();
			Expect(TokenType.RightParen, "Expected ')' after grouped expression");
			return expr;
		}

		throw new ParseException($"Expected expression, got: {CurrentToken.Type}", CurrentToken.Location);
	}

	// ========================================================================
	// Helper Methods
	// ========================================================================

	private bool IsAtEnd() =>
		_current >= _tokens.Count || _tokens[_current].Type == TokenType.EndOfFile;

	private bool IsAtEndOfStatement() =>
		IsAtEnd() || Check(TokenType.Newline) || Check(TokenType.Comment);

	private Token CurrentToken => _tokens[_current];

	private Token Previous => _tokens[_current - 1];

	private bool Check(TokenType type) =>
		!IsAtEnd() && _tokens[_current].Type == type;

	private Token Advance() {
		if (!IsAtEnd()) {
			_current++;
		}
		return Previous;
	}

	private bool Match(TokenType type) {
		if (Check(type)) {
			Advance();
			return true;
		}
		return false;
	}

	private Token Expect(TokenType type, string message) {
		if (Check(type)) {
			return Advance();
		}
		throw new ParseException($"{message}. Got: {CurrentToken.Type}", CurrentToken.Location);
	}

	private void ExpectEndOfStatement() {
		if (!IsAtEndOfStatement()) {
			ReportError($"Expected end of statement, got: {CurrentToken.Type}", CurrentToken.Location);
		}
		SkipNewlines();
	}

	private void SkipNewlines() {
		while (Check(TokenType.Newline) || Check(TokenType.Comment)) {
			Advance();
		}
	}

	private void Synchronize() {
		while (!IsAtEnd()) {
			if (Check(TokenType.Newline)) {
				Advance();
				return;
			}
			Advance();
		}
	}

	private void ReportError(string message, SourceLocation location) {
		_errors.Add(new ParseError(message, location));
	}
}

/// <summary>
/// Represents a parse error.
/// </summary>
public sealed class ParseError {
	/// <summary>
	/// The error message.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// The source location where the error occurred.
	/// </summary>
	public SourceLocation Location { get; }

	/// <summary>
	/// Creates a new parse error.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="location">The source location where the error occurred.</param>
	public ParseError(string message, SourceLocation location) {
		Message = message;
		Location = location;
	}

	/// <inheritdoc />
	public override string ToString() =>
		$"{Location}: error: {Message}";
}

/// <summary>
/// Exception thrown during parsing for error recovery.
/// </summary>
public class ParseException : Exception {
	/// <summary>
	/// The source location where the error occurred.
	/// </summary>
	public SourceLocation Location { get; }

	/// <summary>
	/// Creates a new parse exception.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="location">The source location.</param>
	public ParseException(string message, SourceLocation location)
		: base(message) {
		Location = location;
	}
}
