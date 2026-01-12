// ============================================================================
// MacroExpander.cs - Macro Expansion Engine
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;
using Poppy.Core.Parser;

namespace Poppy.Core.Semantics;

/// <summary>
/// Handles macro expansion with parameter substitution and local label generation.
/// </summary>
public sealed class MacroExpander
{
	private readonly MacroTable _macroTable;
	private readonly List<SemanticError> _errors = [];
	private int _expansionCounter = 0;

	/// <summary>
	/// Gets all errors encountered during macro expansion.
	/// </summary>
	public IReadOnlyList<SemanticError> Errors => _errors;

	/// <summary>
	/// Gets whether any errors have been recorded.
	/// </summary>
	public bool HasErrors => _errors.Count > 0;

	/// <summary>
	/// Creates a new macro expander.
	/// </summary>
	/// <param name="macroTable">The macro table containing macro definitions.</param>
	public MacroExpander(MacroTable macroTable)
	{
		_macroTable = macroTable;
	}

	/// <summary>
	/// Expands a macro invocation into a list of statements.
	/// </summary>
	/// <param name="invocation">The macro invocation node.</param>
	/// <param name="arguments">The arguments passed to the macro.</param>
	/// <returns>List of expanded statements, or empty list if macro not found or errors occurred.</returns>
	public List<StatementNode> Expand(MacroInvocationNode invocation, IReadOnlyList<ExpressionNode> arguments)
	{
		// Get macro definition
		var macro = _macroTable.Get(invocation.Name);
		if (macro == null)
		{
			_errors.Add(new SemanticError(
				$"Undefined macro '{invocation.Name}'",
				invocation.Location));
			return [];
		}

		// Count required parameters (those without defaults)
		var requiredCount = macro.Parameters.Count(p => !p.HasDefault);
		var totalCount = macro.Parameters.Count;

		// Validate argument count
		if (arguments.Count < requiredCount)
		{
			_errors.Add(new SemanticError(
				$"Macro '{invocation.Name}' requires at least {requiredCount} argument(s), got {arguments.Count}",
				invocation.Location));
			return [];
		}

		if (arguments.Count > totalCount)
		{
			_errors.Add(new SemanticError(
				$"Macro '{invocation.Name}' accepts at most {totalCount} argument(s), got {arguments.Count}",
				invocation.Location));
			return [];
		}

		// Create parameter substitution map, using defaults for missing arguments
		var substitutions = new Dictionary<string, ExpressionNode>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < macro.Parameters.Count; i++)
		{
			var param = macro.Parameters[i];
			if (i < arguments.Count)
			{
				// Use provided argument
				substitutions[param.Name] = arguments[i];
			}
			else if (param.HasDefault)
			{
				// Use default value (parse from tokens)
				var defaultExpr = ParseDefaultValue(param.DefaultValue!, invocation.Location);
				if (defaultExpr != null)
				{
					substitutions[param.Name] = defaultExpr;
				}
			}
			else
			{
				// This should never happen because we validated required params above
				_errors.Add(new SemanticError(
					$"Missing required parameter '{param.Name}' for macro '{invocation.Name}'",
					invocation.Location));
				return [];
			}
		}

		// Generate unique expansion ID for local labels
		var expansionId = ++_expansionCounter;

		// Expand macro body
		var expanded = new List<StatementNode>();
		foreach (var statement in macro.Body)
		{
			var expandedStatement = ExpandStatement(statement, substitutions, macro.Name, expansionId);
			if (expandedStatement != null)
			{
				expanded.Add(expandedStatement);
			}
		}

		return expanded;
	}

	/// <summary>
	/// Parses a default parameter value from tokens.
	/// </summary>
	private ExpressionNode? ParseDefaultValue(IReadOnlyList<Token> tokens, SourceLocation errorLocation)
	{
		try
		{
			// Create a parser for just these tokens
			var tokenList = tokens.ToList();
			var parser = new Parser.Parser(tokenList);
			return parser.ParseExpression();
		}
		catch (ParseException ex)
		{
			_errors.Add(new SemanticError(
				$"Invalid default parameter value: {ex.Message}",
				errorLocation));
			return null;
		}
	}

	/// <summary>
	/// Expands a single statement, substituting parameters and renaming local labels.
	/// </summary>
	private StatementNode? ExpandStatement(
		StatementNode statement,
		Dictionary<string, ExpressionNode> substitutions,
		string macroName,
		int expansionId)
	{
		return statement switch
		{
			InstructionNode instruction => ExpandInstruction(instruction, substitutions, macroName, expansionId),
			LabelNode label => ExpandLabel(label, macroName, expansionId),
			DirectiveNode directive => ExpandDirective(directive, substitutions, macroName, expansionId),
			_ => statement  // Other statement types pass through unchanged
		};
	}

	/// <summary>
	/// Expands an instruction, substituting parameter references in operands.
	/// </summary>
	private InstructionNode ExpandInstruction(
		InstructionNode instruction,
		Dictionary<string, ExpressionNode> substitutions,
		string macroName,
		int expansionId)
	{
		if (instruction.Operand == null)
		{
			return instruction;
		}

		var expandedOperand = ExpandExpression(instruction.Operand, substitutions, macroName, expansionId);

		return new InstructionNode(
			instruction.Location,
			instruction.Mnemonic,
			instruction.SizeSuffix,
			expandedOperand,
			instruction.AddressingMode);
	}

	/// <summary>
	/// Expands a label, renaming local labels to be unique to this expansion.
	/// </summary>
	private LabelNode ExpandLabel(LabelNode label, string macroName, int expansionId)
	{
		// Local labels (@name) become macro_name@name_expansionId
		if (label.Name.StartsWith('@'))
		{
			var uniqueName = $"{macroName}{label.Name}_{expansionId}";
			return new LabelNode(label.Location, uniqueName);
		}

		// Global labels pass through unchanged (though this might be an error)
		return label;
	}

	/// <summary>
	/// Expands a directive, substituting parameters in arguments.
	/// </summary>
	private DirectiveNode ExpandDirective(
		DirectiveNode directive,
		Dictionary<string, ExpressionNode> substitutions,
		string macroName,
		int expansionId)
	{
		var expandedArgs = directive.Arguments
			.Select(arg => ExpandExpression(arg, substitutions, macroName, expansionId))
			.ToList();

		return new DirectiveNode(directive.Location, directive.Name, expandedArgs);
	}

	/// <summary>
	/// Expands an expression, substituting parameter references.
	/// </summary>
	private ExpressionNode ExpandExpression(
		ExpressionNode expression,
		Dictionary<string, ExpressionNode> substitutions,
		string macroName,
		int expansionId)
	{
		return expression switch
		{
			// Identifier might be a parameter reference
			IdentifierNode identifier => ExpandIdentifier(identifier, substitutions, macroName, expansionId),

			// Binary operations - expand both sides
			BinaryExpressionNode binary => new BinaryExpressionNode(
				binary.Location,
				ExpandExpression(binary.Left, substitutions, macroName, expansionId),
				binary.Operator,
				ExpandExpression(binary.Right, substitutions, macroName, expansionId)),

			// Unary operations - expand the operand
			UnaryExpressionNode unary => new UnaryExpressionNode(
				unary.Location,
				unary.Operator,
				ExpandExpression(unary.Operand, substitutions, macroName, expansionId)),

			// Literals and other expressions pass through unchanged
			_ => expression
		};
	}

	/// <summary>
	/// Expands an identifier, substituting parameter references and renaming local label references.
	/// </summary>
	private ExpressionNode ExpandIdentifier(
		IdentifierNode identifier,
		Dictionary<string, ExpressionNode> substitutions,
		string macroName,
		int expansionId)
	{
		// Check if this identifier is a parameter
		if (substitutions.TryGetValue(identifier.Name, out var substitution))
		{
			return substitution;
		}

		// Check if this is a local label reference (@name)
		if (identifier.Name.StartsWith('@'))
		{
			var uniqueName = $"{macroName}{identifier.Name}_{expansionId}";
			return new IdentifierNode(identifier.Location, uniqueName);
		}

		// Regular identifier passes through unchanged
		return identifier;
	}

	/// <summary>
	/// Clears all errors.
	/// </summary>
	public void Clear()
	{
		_errors.Clear();
		_expansionCounter = 0;
	}
}
