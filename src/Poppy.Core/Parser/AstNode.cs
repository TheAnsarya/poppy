// ============================================================================
// AstNode.cs - Abstract Syntax Tree Node Definitions
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;

namespace Poppy.Core.Parser;

/// <summary>
/// Base class for all AST nodes.
/// </summary>
public abstract class AstNode {
	/// <summary>
	/// The source location where this node begins.
	/// </summary>
	public SourceLocation Location { get; }

	/// <summary>
	/// Creates a new AST node.
	/// </summary>
	protected AstNode(SourceLocation location) {
		Location = location;
	}

	/// <summary>
	/// Accepts a visitor for the visitor pattern.
	/// </summary>
	public abstract T Accept<T>(IAstVisitor<T> visitor);
}

/// <summary>
/// Visitor interface for traversing the AST.
/// </summary>
/// <typeparam name="T">The return type of the visitor methods.</typeparam>
public interface IAstVisitor<T> {
	/// <summary>Visits a program node.</summary>
	/// <param name="node">The program node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitProgram(ProgramNode node);

	/// <summary>Visits a label node.</summary>
	/// <param name="node">The label node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitLabel(LabelNode node);

	/// <summary>Visits an instruction node.</summary>
	/// <param name="node">The instruction node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitInstruction(InstructionNode node);

	/// <summary>Visits a directive node.</summary>
	/// <param name="node">The directive node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitDirective(DirectiveNode node);

	/// <summary>Visits an expression node.</summary>
	/// <param name="node">The expression node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitExpression(ExpressionNode node);

	/// <summary>Visits a binary expression node.</summary>
	/// <param name="node">The binary expression node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitBinaryExpression(BinaryExpressionNode node);

	/// <summary>Visits a unary expression node.</summary>
	/// <param name="node">The unary expression node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitUnaryExpression(UnaryExpressionNode node);

	/// <summary>Visits a number literal node.</summary>
	/// <param name="node">The number literal node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitNumberLiteral(NumberLiteralNode node);

	/// <summary>Visits a string literal node.</summary>
	/// <param name="node">The string literal node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitStringLiteral(StringLiteralNode node);

	/// <summary>Visits an identifier node.</summary>
	/// <param name="node">The identifier node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitIdentifier(IdentifierNode node);

	/// <summary>Visits a macro definition node.</summary>
	/// <param name="node">The macro definition node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitMacroDefinition(MacroDefinitionNode node);

	/// <summary>Visits a macro invocation node.</summary>
	/// <param name="node">The macro invocation node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitMacroInvocation(MacroInvocationNode node);

	/// <summary>Visits a conditional assembly node.</summary>
	/// <param name="node">The conditional assembly node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitConditional(ConditionalNode node);

	/// <summary>Visits a repeat block node.</summary>
	/// <param name="node">The repeat block node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitRepeatBlock(RepeatBlockNode node);

	/// <summary>Visits an enumeration block node.</summary>
	/// <param name="node">The enumeration block node to visit.</param>
	/// <returns>The result of visiting the node.</returns>
	T VisitEnumerationBlock(EnumerationBlockNode node);
}

// ============================================================================
// Program Structure Nodes
// ============================================================================

/// <summary>
/// Represents an entire assembly program (the root node).
/// </summary>
public sealed class ProgramNode : AstNode {
	/// <summary>
	/// The list of statements in the program.
	/// </summary>
	public IReadOnlyList<StatementNode> Statements { get; }

	/// <summary>
	/// Creates a new program node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="statements">The list of statements in the program.</param>
	public ProgramNode(SourceLocation location, IReadOnlyList<StatementNode> statements)
		: base(location) {
		Statements = statements;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitProgram(this);
}

/// <summary>
/// Base class for statement-level nodes (labels, instructions, directives).
/// </summary>
public abstract class StatementNode : AstNode {
	/// <summary>
	/// Creates a new statement node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	protected StatementNode(SourceLocation location) : base(location) { }
}

/// <summary>
/// Represents a label definition (e.g., "main_loop:").
/// </summary>
public sealed class LabelNode : StatementNode {
	/// <summary>
	/// The name of the label.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Whether this is a local label (starts with .).
	/// </summary>
	public bool IsLocal { get; }

	/// <summary>
	/// Creates a new label node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="name">The name of the label.</param>
	/// <param name="isLocal">Whether this is a local label.</param>
	public LabelNode(SourceLocation location, string name, bool isLocal = false)
		: base(location) {
		Name = name;
		IsLocal = isLocal;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLabel(this);
}

/// <summary>
/// Represents a CPU instruction (e.g., "lda #$00").
/// </summary>
public sealed class InstructionNode : StatementNode {
	/// <summary>
	/// The instruction mnemonic (e.g., "lda", "sta", "jmp").
	/// </summary>
	public string Mnemonic { get; }

	/// <summary>
	/// Optional size suffix (b, w, l for 65816).
	/// </summary>
	public char? SizeSuffix { get; }

	/// <summary>
	/// The operand expression, if any.
	/// </summary>
	public ExpressionNode? Operand { get; }

	/// <summary>
	/// The addressing mode of this instruction.
	/// </summary>
	public AddressingMode AddressingMode { get; }

	/// <summary>
	/// Creates a new instruction node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <param name="sizeSuffix">Optional size suffix (b, w, l for 65816).</param>
	/// <param name="operand">The operand expression, if any.</param>
	/// <param name="addressingMode">The addressing mode of this instruction.</param>
	public InstructionNode(
		SourceLocation location,
		string mnemonic,
		char? sizeSuffix = null,
		ExpressionNode? operand = null,
		AddressingMode addressingMode = AddressingMode.Implied)
		: base(location) {
		Mnemonic = mnemonic;
		SizeSuffix = sizeSuffix;
		Operand = operand;
		AddressingMode = addressingMode;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitInstruction(this);
}

/// <summary>
/// Addressing modes for CPU instructions.
/// </summary>
public enum AddressingMode {
	/// <summary>No operand (e.g., nop, rts)</summary>
	Implied,

	/// <summary>Accumulator (e.g., asl a)</summary>
	Accumulator,

	/// <summary>Immediate value (e.g., lda #$00)</summary>
	Immediate,

	/// <summary>Zero page address (e.g., lda $00)</summary>
	ZeroPage,

	/// <summary>Zero page indexed by X (e.g., lda $00,x)</summary>
	ZeroPageX,

	/// <summary>Zero page indexed by Y (e.g., ldx $00,y)</summary>
	ZeroPageY,

	/// <summary>Absolute address (e.g., lda $2000)</summary>
	Absolute,

	/// <summary>Absolute indexed by X (e.g., lda $2000,x)</summary>
	AbsoluteX,

	/// <summary>Absolute indexed by Y (e.g., lda $2000,y)</summary>
	AbsoluteY,

	/// <summary>Indirect address (e.g., jmp ($fffc))</summary>
	Indirect,

	/// <summary>Indexed indirect (e.g., lda ($00,x))</summary>
	IndexedIndirect,

	/// <summary>Indirect indexed (e.g., lda ($00),y)</summary>
	IndirectIndexed,

	/// <summary>Relative branch (e.g., beq label)</summary>
	Relative,

	// 65816 specific modes

	/// <summary>Long absolute (e.g., lda $7e0000)</summary>
	AbsoluteLong,

	/// <summary>Long absolute indexed by X (e.g., lda $7e0000,x)</summary>
	AbsoluteLongX,

	/// <summary>Stack relative (e.g., lda $01,s)</summary>
	StackRelative,

	/// <summary>Stack relative indirect indexed (e.g., lda ($01,s),y)</summary>
	StackRelativeIndirectIndexed,

	/// <summary>Direct page indirect (e.g., lda [$00])</summary>
	DirectPageIndirectLong,

	/// <summary>Direct page indirect long indexed (e.g., lda [$00],y)</summary>
	DirectPageIndirectLongY,

	/// <summary>Absolute indirect long (e.g., jml [$fffc])</summary>
	AbsoluteIndirectLong,

	/// <summary>Absolute indexed indirect (e.g., jmp ($fffc,x))</summary>
	AbsoluteIndexedIndirect,

	/// <summary>Block move (e.g., mvn $00,$7e)</summary>
	BlockMove,

	// Game Boy specific

	/// <summary>Memory reference with brackets (e.g., ld [hl],a)</summary>
	MemoryReference,
}

/// <summary>
/// Represents an assembler directive (e.g., ".org $8000").
/// </summary>
public sealed class DirectiveNode : StatementNode {
	/// <summary>
	/// The directive name without the leading dot (e.g., "org", "byte").
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The arguments to the directive.
	/// </summary>
	public IReadOnlyList<ExpressionNode> Arguments { get; }

	/// <summary>
	/// Creates a new directive node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="name">The directive name without the leading dot.</param>
	/// <param name="arguments">The arguments to the directive.</param>
	public DirectiveNode(SourceLocation location, string name, IReadOnlyList<ExpressionNode> arguments)
		: base(location) {
		Name = name;
		Arguments = arguments;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDirective(this);
}

// ============================================================================
// Expression Nodes
// ============================================================================

/// <summary>
/// Base class for expression nodes.
/// </summary>
public abstract class ExpressionNode : AstNode {
	/// <summary>
	/// Creates a new expression node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	protected ExpressionNode(SourceLocation location) : base(location) { }
}

/// <summary>
/// Represents a binary expression (e.g., "a + b", "label &amp; $ff").
/// </summary>
public sealed class BinaryExpressionNode : ExpressionNode {
	/// <summary>
	/// The left operand.
	/// </summary>
	public ExpressionNode Left { get; }

	/// <summary>
	/// The operator.
	/// </summary>
	public BinaryOperator Operator { get; }

	/// <summary>
	/// The right operand.
	/// </summary>
	public ExpressionNode Right { get; }

	/// <summary>
	/// Creates a new binary expression node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="left">The left operand.</param>
	/// <param name="op">The binary operator.</param>
	/// <param name="right">The right operand.</param>
	public BinaryExpressionNode(
		SourceLocation location,
		ExpressionNode left,
		BinaryOperator op,
		ExpressionNode right)
		: base(location) {
		Left = left;
		Operator = op;
		Right = right;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBinaryExpression(this);
}

/// <summary>
/// Binary operators.
/// </summary>
public enum BinaryOperator {
	/// <summary>Addition operator (+).</summary>
	Add,
	/// <summary>Subtraction operator (-).</summary>
	Subtract,
	/// <summary>Multiplication operator (*).</summary>
	Multiply,
	/// <summary>Division operator (/).</summary>
	Divide,
	/// <summary>Modulo operator (%).</summary>
	Modulo,
	/// <summary>Bitwise AND operator (&amp;).</summary>
	BitwiseAnd,
	/// <summary>Bitwise OR operator (|).</summary>
	BitwiseOr,
	/// <summary>Bitwise XOR operator (^).</summary>
	BitwiseXor,
	/// <summary>Left shift operator (&lt;&lt;).</summary>
	LeftShift,
	/// <summary>Right shift operator (&gt;&gt;).</summary>
	RightShift,
	/// <summary>Equality operator (==).</summary>
	Equal,
	/// <summary>Inequality operator (!=).</summary>
	NotEqual,
	/// <summary>Less than operator (&lt;).</summary>
	LessThan,
	/// <summary>Greater than operator (&gt;).</summary>
	GreaterThan,
	/// <summary>Less than or equal operator (&lt;=).</summary>
	LessOrEqual,
	/// <summary>Greater than or equal operator (&gt;=).</summary>
	GreaterOrEqual,
	/// <summary>Logical AND operator (&amp;&amp;).</summary>
	LogicalAnd,
	/// <summary>Logical OR operator (||).</summary>
	LogicalOr,
}

/// <summary>
/// Represents a unary expression (e.g., "-x", "~mask", "&lt;label").
/// </summary>
public sealed class UnaryExpressionNode : ExpressionNode {
	/// <summary>
	/// The operator.
	/// </summary>
	public UnaryOperator Operator { get; }

	/// <summary>
	/// The operand.
	/// </summary>
	public ExpressionNode Operand { get; }

	/// <summary>
	/// Creates a new unary expression node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="op">The unary operator.</param>
	/// <param name="operand">The operand expression.</param>
	public UnaryExpressionNode(
		SourceLocation location,
		UnaryOperator op,
		ExpressionNode operand)
		: base(location) {
		Operator = op;
		Operand = operand;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUnaryExpression(this);
}

/// <summary>
/// Unary operators.
/// </summary>
public enum UnaryOperator {
	/// <summary>Negation operator (-).</summary>
	Negate,
	/// <summary>Bitwise NOT operator (~).</summary>
	BitwiseNot,
	/// <summary>Logical NOT operator (!).</summary>
	LogicalNot,
	/// <summary>Low byte extraction operator (&lt;).</summary>
	LowByte,
	/// <summary>High byte extraction operator (&gt;).</summary>
	HighByte,
	/// <summary>Bank byte extraction operator (^) for 65816.</summary>
	BankByte,
	/// <summary>Immediate addressing mode prefix (#).</summary>
	Immediate,
}

/// <summary>
/// Represents a numeric literal (e.g., "$ff", "255", "%10101010").
/// </summary>
public sealed class NumberLiteralNode : ExpressionNode {
	/// <summary>
	/// The numeric value.
	/// </summary>
	public long Value { get; }

	/// <summary>
	/// Creates a new number literal node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="value">The numeric value.</param>
	public NumberLiteralNode(SourceLocation location, long value)
		: base(location) {
		Value = value;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitNumberLiteral(this);
}

/// <summary>
/// Represents a string literal (e.g., "Hello, World!").
/// </summary>
public sealed class StringLiteralNode : ExpressionNode {
	/// <summary>
	/// The string value.
	/// </summary>
	public string Value { get; }

	/// <summary>
	/// Creates a new string literal node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="value">The string value.</param>
	public StringLiteralNode(SourceLocation location, string value)
		: base(location) {
		Value = value;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitStringLiteral(this);
}

/// <summary>
/// Represents an identifier reference (e.g., "label", "CONSTANT").
/// </summary>
public sealed class IdentifierNode : ExpressionNode {
	/// <summary>
	/// The identifier name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Creates a new identifier node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="name">The identifier name.</param>
	public IdentifierNode(SourceLocation location, string name)
		: base(location) {
		Name = name;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIdentifier(this);
}

// ============================================================================
// Macro Nodes
// ============================================================================

/// <summary>
/// Represents a macro definition.
/// </summary>
public sealed class MacroDefinitionNode : StatementNode {
	/// <summary>
	/// The name of the macro.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The parameter names for the macro.
	/// </summary>
	public IReadOnlyList<string> Parameters { get; }

	/// <summary>
	/// The body of the macro (statements between .macro and .endmacro).
	/// </summary>
	public IReadOnlyList<StatementNode> Body { get; }

	/// <summary>
	/// Creates a new macro definition node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="name">The name of the macro.</param>
	/// <param name="parameters">The parameter names for the macro.</param>
	/// <param name="body">The body of the macro.</param>
	public MacroDefinitionNode(
		SourceLocation location,
		string name,
		IReadOnlyList<string> parameters,
		IReadOnlyList<StatementNode> body)
		: base(location) {
		Name = name;
		Parameters = parameters;
		Body = body;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMacroDefinition(this);
}

/// <summary>
/// Represents a macro invocation.
/// </summary>
public sealed class MacroInvocationNode : StatementNode {
	/// <summary>
	/// The name of the macro being invoked.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The arguments passed to the macro.
	/// </summary>
	public IReadOnlyList<ExpressionNode> Arguments { get; }

	/// <summary>
	/// Creates a new macro invocation node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="name">The name of the macro being invoked.</param>
	/// <param name="arguments">The arguments passed to the macro.</param>
	public MacroInvocationNode(
		SourceLocation location,
		string name,
		IReadOnlyList<ExpressionNode> arguments)
		: base(location) {
		Name = name;
		Arguments = arguments;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMacroInvocation(this);
}

/// <summary>
/// Represents a conditional assembly block (.if/.elseif/.else/.endif).
/// </summary>
public sealed class ConditionalNode : StatementNode {
	/// <summary>
	/// The condition expression for the .if or .elseif branch.
	/// </summary>
	public ExpressionNode Condition { get; }

	/// <summary>
	/// The statements to execute if the condition is true.
	/// </summary>
	public IReadOnlyList<StatementNode> ThenBlock { get; }

	/// <summary>
	/// The list of .elseif branches (condition + statements).
	/// </summary>
	public IReadOnlyList<(ExpressionNode Condition, IReadOnlyList<StatementNode> Block)> ElseIfBranches { get; }

	/// <summary>
	/// The .else block (executed if all conditions are false).
	/// </summary>
	public IReadOnlyList<StatementNode>? ElseBlock { get; }

	/// <summary>
	/// Creates a new conditional assembly node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="condition">The condition expression for the .if branch.</param>
	/// <param name="thenBlock">The statements to execute if the condition is true.</param>
	/// <param name="elseIfBranches">The list of .elseif branches.</param>
	/// <param name="elseBlock">The .else block, if any.</param>
	public ConditionalNode(
		SourceLocation location,
		ExpressionNode condition,
		IReadOnlyList<StatementNode> thenBlock,
		IReadOnlyList<(ExpressionNode, IReadOnlyList<StatementNode>)> elseIfBranches,
		IReadOnlyList<StatementNode>? elseBlock = null)
		: base(location) {
		Condition = condition;
		ThenBlock = thenBlock;
		ElseIfBranches = elseIfBranches;
		ElseBlock = elseBlock;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitConditional(this);
}

/// <summary>
/// Represents a repeat block (.rept/.endr).
/// </summary>
public sealed class RepeatBlockNode : StatementNode {
	/// <summary>
	/// The number of times to repeat the block.
	/// </summary>
	public ExpressionNode Count { get; }

	/// <summary>
	/// The statements to repeat.
	/// </summary>
	public IReadOnlyList<StatementNode> Body { get; }

	/// <summary>
	/// Creates a new repeat block node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="count">The number of times to repeat the block.</param>
	/// <param name="body">The statements to repeat.</param>
	public RepeatBlockNode(
		SourceLocation location,
		ExpressionNode count,
		IReadOnlyList<StatementNode> body)
		: base(location) {
		Count = count;
		Body = body;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitRepeatBlock(this);
}

/// <summary>
/// Represents an enumeration block (.enum/.ende) member.
/// </summary>
public sealed class EnumerationMember {
	/// <summary>
	/// The name of the enumeration member.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Optional explicit value for this member.
	/// </summary>
	public ExpressionNode? Value { get; }

	/// <summary>
	/// Optional size directive (.db, .dw, .dl).
	/// </summary>
	public string? SizeDirective { get; }

	/// <summary>
	/// Creates a new enumeration member.
	/// </summary>
	public EnumerationMember(string name, ExpressionNode? value = null, string? sizeDirective = null) {
		Name = name;
		Value = value;
		SizeDirective = sizeDirective;
	}
}

/// <summary>
/// Represents an enumeration block (.enum/.ende).
/// </summary>
public sealed class EnumerationBlockNode : StatementNode {
	/// <summary>
	/// The starting address/value for the enumeration.
	/// </summary>
	public ExpressionNode StartValue { get; }

	/// <summary>
	/// The list of enumeration members.
	/// </summary>
	public IReadOnlyList<EnumerationMember> Members { get; }

	/// <summary>
	/// Creates a new enumeration block node.
	/// </summary>
	/// <param name="location">The source location where this node begins.</param>
	/// <param name="startValue">The starting address/value for the enumeration.</param>
	/// <param name="members">The list of enumeration members.</param>
	public EnumerationBlockNode(
		SourceLocation location,
		ExpressionNode startValue,
		IReadOnlyList<EnumerationMember> members)
		: base(location) {
		StartValue = startValue;
		Members = members;
	}

	/// <inheritdoc />
	public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitEnumerationBlock(this);
}
