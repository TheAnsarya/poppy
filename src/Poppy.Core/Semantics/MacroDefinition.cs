// ============================================================================
// MacroDefinition.cs - Macro Definition Storage
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;
using Poppy.Core.Parser;

namespace Poppy.Core.Semantics;

/// <summary>
/// Represents a macro definition with parameters and body.
/// </summary>
public sealed class MacroDefinition
{
	/// <summary>
	/// The name of the macro.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The parameter names for this macro.
	/// </summary>
	public IReadOnlyList<MacroParameter> Parameters { get; }

	/// <summary>
	/// The statements that make up the macro body.
	/// </summary>
	public IReadOnlyList<StatementNode> Body { get; }

	/// <summary>
	/// The source location where the macro was defined.
	/// </summary>
	public SourceLocation Location { get; }

	/// <summary>
	/// Creates a new macro definition.
	/// </summary>
	/// <param name="name">The macro name.</param>
	/// <param name="parameters">The macro parameters.</param>
	/// <param name="body">The macro body statements.</param>
	/// <param name="location">The source location.</param>
	public MacroDefinition(
		string name,
		IReadOnlyList<MacroParameter> parameters,
		IReadOnlyList<StatementNode> body,
		SourceLocation location)
	{
		Name = name;
		Parameters = parameters;
		Body = body;
		Location = location;
	}
}

/// <summary>
/// Represents a macro parameter with optional default value.
/// </summary>
public sealed class MacroParameter
{
	/// <summary>
	/// The parameter name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The default value expression tokens (null if no default).
	/// </summary>
	public IReadOnlyList<Token>? DefaultValue { get; }

	/// <summary>
	/// Whether this parameter has a default value.
	/// </summary>
	public bool HasDefault => DefaultValue is not null;

	/// <summary>
	/// Creates a new macro parameter.
	/// </summary>
	/// <param name="name">The parameter name.</param>
	/// <param name="defaultValue">Optional default value tokens.</param>
	public MacroParameter(string name, IReadOnlyList<Token>? defaultValue = null)
	{
		Name = name;
		DefaultValue = defaultValue;
	}
}
