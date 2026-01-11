// ============================================================================
// SemanticAnalyzer.cs - Semantic Analysis Phase
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;
using Poppy.Core.Parser;

namespace Poppy.Core.Semantics;

/// <summary>
/// Performs semantic analysis on the AST.
/// </summary>
/// <remarks>
/// The semantic analyzer performs two passes:
/// 1. First pass: Collect all symbol definitions and calculate addresses
/// 2. Second pass: Resolve all symbol references and validate
/// </remarks>
public sealed class SemanticAnalyzer : IAstVisitor<object?> {
	private readonly SymbolTable _symbolTable;
	private readonly List<SemanticError> _errors;
	private TargetArchitecture _target;
	private bool _targetSetFromSource;
	private string? _memoryMapping;
	private int? _nesMapper;
	private long _currentAddress;
	private int _pass;

	/// <summary>
	/// Gets the symbol table.
	/// </summary>
	public SymbolTable SymbolTable => _symbolTable;

	/// <summary>
	/// Gets the current target architecture.
	/// </summary>
	public TargetArchitecture Target => _target;

	/// <summary>
	/// Gets the SNES memory mapping (lorom, hirom, exhirom).
	/// </summary>
	public string? MemoryMapping => _memoryMapping;

	/// <summary>
	/// Gets the NES mapper number.
	/// </summary>
	public int? NesMapper => _nesMapper;

	/// <summary>
	/// Gets all semantic errors.
	/// </summary>
	public IReadOnlyList<SemanticError> Errors => _errors;

	/// <summary>
	/// Gets whether analysis encountered any errors.
	/// </summary>
	public bool HasErrors => _errors.Count > 0;

	/// <summary>
	/// Gets the current address counter.
	/// </summary>
	public long CurrentAddress => _currentAddress;

	/// <summary>
	/// Creates a new semantic analyzer.
	/// </summary>
	/// <param name="target">The target architecture.</param>
	public SemanticAnalyzer(TargetArchitecture target = TargetArchitecture.MOS6502) {
		_symbolTable = new SymbolTable();
		_errors = [];
		_target = target;
		_currentAddress = 0;
		_pass = 0;
	}

	/// <summary>
	/// Analyzes a program AST.
	/// </summary>
	/// <param name="program">The program to analyze.</param>
	public void Analyze(ProgramNode program) {
		// First pass: collect definitions
		_pass = 1;
		_currentAddress = 0;
		program.Accept(this);

		// Validate all symbols are defined
		_symbolTable.ValidateAllDefined();
		_errors.AddRange(_symbolTable.Errors);

		// Second pass: resolve references
		_pass = 2;
		_currentAddress = 0;
		program.Accept(this);

		// Collect any errors from pass 2 (e.g., anonymous label resolution)
		_errors.AddRange(_symbolTable.Errors.Skip(_errors.Count));
	}

	/// <inheritdoc />
	public object? VisitProgram(ProgramNode node) {
		foreach (var statement in node.Statements) {
			statement.Accept(this);
		}
		return null;
	}

	/// <inheritdoc />
	public object? VisitLabel(LabelNode node) {
		if (_pass == 1) {
			// Handle anonymous labels (+ or -)
			if (IsAnonymousLabelName(node.Name)) {
				bool isForward = node.Name[0] == '+';
				_symbolTable.DefineAnonymousLabel(isForward, _currentAddress, node.Location);
			} else {
				_symbolTable.Define(
					node.Name,
					SymbolType.Label,
					_currentAddress,
					node.Location);
			}
		}
		return null;
	}

	/// <summary>
	/// Checks if a name represents an anonymous label (all + or all -).
	/// </summary>
	private static bool IsAnonymousLabelName(string name) {
		if (string.IsNullOrEmpty(name)) return false;
		char first = name[0];
		if (first != '+' && first != '-') return false;
		return name.All(c => c == first);
	}

	/// <inheritdoc />
	public object? VisitInstruction(InstructionNode node) {
		// Visit operand to resolve symbols
		node.Operand?.Accept(this);

		// Calculate instruction size
		var size = GetInstructionSize(node);
		_currentAddress += size;

		return null;
	}

	/// <summary>
	/// Checks if an instruction is a branch instruction (uses relative addressing).
	/// </summary>
	private static bool IsBranchInstruction(string mnemonic) {
		return mnemonic.ToLowerInvariant() switch {
			"bcc" or "bcs" or "beq" or "bmi" or "bne" or "bpl" or "bvc" or "bvs" => true,
			"bra" => true, // 65816/65C02
			_ => false
		};
	}

	/// <inheritdoc />
	public object? VisitDirective(DirectiveNode node) {
		switch (node.Name.ToLowerInvariant()) {
			case "org":
				HandleOrgDirective(node);
				break;

			case "equ":
			case "=":
				HandleEquDirective(node);
				break;

			case "byte":
			case "db":
				HandleDataDirective(node, 1);
				break;

			case "word":
			case "dw":
				HandleDataDirective(node, 2);
				break;

			case "long":
			case "dl":
			case "dd":
				HandleDataDirective(node, _target == TargetArchitecture.WDC65816 ? 3 : 4);
				break;

			case "ds":
			case "fill":
			case "res":
				HandleSpaceDirective(node);
				break;

			case "define":
				HandleDefineDirective(node);
				break;

			// Target directives
			case "target":
				HandleTargetDirective(node);
				break;

			case "nes":
				SetTarget(TargetArchitecture.MOS6502, node);
				break;

			case "snes":
				SetTarget(TargetArchitecture.WDC65816, node);
				break;

			case "gb":
			case "gameboy":
				SetTarget(TargetArchitecture.SM83, node);
				break;

			// SNES memory mapping
			case "lorom":
			case "hirom":
			case "exhirom":
				HandleMemoryMapping(node);
				break;

			// NES mapper
			case "mapper":
				HandleMapperDirective(node);
				break;

			// Assertions and diagnostics
			case "assert":
				HandleAssertDirective(node);
				break;

			case "error":
				HandleErrorDirective(node);
				break;

			case "warning":
				HandleWarningDirective(node);
				break;

			default:
				// Visit arguments for symbol resolution
				foreach (var arg in node.Arguments) {
					arg.Accept(this);
				}
				break;
		}

		return null;
	}

	/// <inheritdoc />
	public object? VisitExpression(ExpressionNode node) {
		// Base expression - should not be called directly
		return null;
	}

	/// <inheritdoc />
	public object? VisitBinaryExpression(BinaryExpressionNode node) {
		node.Left.Accept(this);
		node.Right.Accept(this);
		return null;
	}

	/// <inheritdoc />
	public object? VisitUnaryExpression(UnaryExpressionNode node) {
		node.Operand.Accept(this);
		return null;
	}

	/// <inheritdoc />
	public object? VisitNumberLiteral(NumberLiteralNode node) {
		return node.Value;
	}

	/// <inheritdoc />
	public object? VisitStringLiteral(StringLiteralNode node) {
		return node.Value;
	}

	/// <inheritdoc />
	public object? VisitIdentifier(IdentifierNode node) {
		// Handle special identifiers
		if (node.Name == "*" || node.Name == "$") {
			return _currentAddress;
		}

		// Handle anonymous label references (+ or -) only in pass 2
		if (IsAnonymousLabelName(node.Name)) {
			if (_pass == 2) {
				bool isForward = node.Name[0] == '+';
				int count = node.Name.Length;
				var address = _symbolTable.ResolveAnonymousLabel(isForward, count, _currentAddress, node.Location);
				return address;
			}
			return null;
		}

		// Reference the symbol
		_symbolTable.Reference(node.Name, node.Location);
		return null;
	}

	/// <inheritdoc />
	public object? VisitMacroDefinition(MacroDefinitionNode node) {
		if (_pass == 1) {
			_symbolTable.Define(
				node.Name,
				SymbolType.Macro,
				null,
				node.Location);
		}
		return null;
	}

	/// <inheritdoc />
	public object? VisitMacroInvocation(MacroInvocationNode node) {
		// Reference the macro
		_symbolTable.Reference(node.Name, node.Location);

		// Visit arguments
		foreach (var arg in node.Arguments) {
			arg.Accept(this);
		}
		return null;
	}

	// ========================================================================
	// Directive Handlers
	// ========================================================================

	/// <summary>
	/// Handles .org directive to set the current address.
	/// </summary>
	private void HandleOrgDirective(DirectiveNode node) {
		if (node.Arguments.Count < 1) {
			_errors.Add(new SemanticError(
				".org directive requires an address argument",
				node.Location));
			return;
		}

		var value = EvaluateExpression(node.Arguments[0]);
		if (value.HasValue) {
			_currentAddress = value.Value;
		}
	}

	/// <summary>
	/// Handles .equ or = directive to define a constant.
	/// </summary>
	private void HandleEquDirective(DirectiveNode node) {
		if (_pass != 1) return;

		if (node.Arguments.Count < 2) {
			_errors.Add(new SemanticError(
				".equ directive requires a name and value",
				node.Location));
			return;
		}

		if (node.Arguments[0] is not IdentifierNode nameNode) {
			_errors.Add(new SemanticError(
				".equ directive requires an identifier as the first argument",
				node.Location));
			return;
		}

		var value = EvaluateExpression(node.Arguments[1]);
		_symbolTable.Define(nameNode.Name, SymbolType.Constant, value, node.Location);
	}

	/// <summary>
	/// Handles data directives (.byte, .word, etc.).
	/// </summary>
	private void HandleDataDirective(DirectiveNode node, int bytesPerElement) {
		foreach (var arg in node.Arguments) {
			if (arg is StringLiteralNode strNode) {
				// Each character is one byte
				_currentAddress += strNode.Value.Length;
			}
			else {
				arg.Accept(this);
				_currentAddress += bytesPerElement;
			}
		}
	}

	/// <summary>
	/// Handles space/reserve directives (.ds, .fill, .res).
	/// </summary>
	private void HandleSpaceDirective(DirectiveNode node) {
		if (node.Arguments.Count < 1) {
			_errors.Add(new SemanticError(
				$".{node.Name} directive requires a count argument",
				node.Location));
			return;
		}

		var count = EvaluateExpression(node.Arguments[0]);
		if (count.HasValue) {
			_currentAddress += count.Value;
		}
	}

	/// <summary>
	/// Handles .define directive.
	/// </summary>
	private void HandleDefineDirective(DirectiveNode node) {
		if (_pass != 1) return;

		if (node.Arguments.Count < 1) {
			_errors.Add(new SemanticError(
				".define directive requires at least a name",
				node.Location));
			return;
		}

		if (node.Arguments[0] is not IdentifierNode nameNode) {
			_errors.Add(new SemanticError(
				".define directive requires an identifier",
				node.Location));
			return;
		}

		long? value = null;
		if (node.Arguments.Count >= 2) {
			value = EvaluateExpression(node.Arguments[1]);
		}

		_symbolTable.Define(nameNode.Name, SymbolType.Constant, value ?? 1, node.Location);
	}

	/// <summary>
	/// Handles .target directive with architecture argument.
	/// </summary>
	private void HandleTargetDirective(DirectiveNode node) {
		if (_pass != 1) return;

		if (node.Arguments.Count < 1) {
			_errors.Add(new SemanticError(
				".target directive requires an architecture (nes, snes, gb)",
				node.Location));
			return;
		}

		if (node.Arguments[0] is not IdentifierNode targetNode) {
			_errors.Add(new SemanticError(
				".target directive requires an identifier",
				node.Location));
			return;
		}

		var targetName = targetNode.Name.ToLowerInvariant();
		var target = targetName switch {
			"nes" or "6502" => TargetArchitecture.MOS6502,
			"snes" or "65816" => TargetArchitecture.WDC65816,
			"gb" or "gameboy" or "sm83" => TargetArchitecture.SM83,
			_ => (TargetArchitecture?)null
		};

		if (target is null) {
			_errors.Add(new SemanticError(
				$"Unknown target architecture: {targetNode.Name}",
				node.Location));
			return;
		}

		SetTarget(target.Value, node);
	}

	/// <summary>
	/// Sets the target architecture.
	/// </summary>
	private void SetTarget(TargetArchitecture target, DirectiveNode node) {
		// Allow setting to the same value (idempotent)
		if (_targetSetFromSource && _target != target) {
			_errors.Add(new SemanticError(
				"Target architecture already set - cannot change",
				node.Location));
			return;
		}

		_target = target;
		_targetSetFromSource = true;
	}

	/// <summary>
	/// Handles SNES memory mapping directives (.lorom, .hirom, .exhirom).
	/// </summary>
	private void HandleMemoryMapping(DirectiveNode node) {
		if (_pass != 1) return;

		if (_memoryMapping is not null) {
			_errors.Add(new SemanticError(
				"Memory mapping already set - cannot change",
				node.Location));
			return;
		}

		_memoryMapping = node.Name.ToLowerInvariant();

		// Ensure target is SNES
		if (_target != TargetArchitecture.WDC65816) {
			_errors.Add(new SemanticError(
				$".{node.Name} directive is only valid for SNES/65816 target",
				node.Location));
		}
	}

	/// <summary>
	/// Handles .mapper directive for NES mapper selection.
	/// </summary>
	private void HandleMapperDirective(DirectiveNode node) {
		if (_pass != 1) return;

		if (node.Arguments.Count < 1) {
			_errors.Add(new SemanticError(
				".mapper directive requires a mapper number",
				node.Location));
			return;
		}

		var mapperValue = EvaluateExpression(node.Arguments[0]);
		if (mapperValue is null) {
			_errors.Add(new SemanticError(
				".mapper directive requires a constant mapper number",
				node.Location));
			return;
		}

		if (_nesMapper is not null) {
			_errors.Add(new SemanticError(
				"Mapper already set - cannot change",
				node.Location));
			return;
		}

		_nesMapper = (int)mapperValue;

		// Ensure target is NES
		if (_target != TargetArchitecture.MOS6502) {
			_errors.Add(new SemanticError(
				".mapper directive is only valid for NES/6502 target",
				node.Location));
		}
	}

	/// <summary>
	/// Handles .assert directive for compile-time assertions.
	/// </summary>
	private void HandleAssertDirective(DirectiveNode node) {
		// Assertions are checked in pass 2 after all symbols are defined
		if (_pass != 2) return;

		if (node.Arguments.Count < 1) {
			_errors.Add(new SemanticError(
				".assert directive requires a condition expression",
				node.Location));
			return;
		}

		// Evaluate the assertion condition
		var condition = EvaluateExpression(node.Arguments[0]);
		if (condition is null) {
			_errors.Add(new SemanticError(
				".assert condition could not be evaluated",
				node.Location));
			return;
		}

		// Check if assertion passes
		if (condition == 0) {
			// Get optional message
			string message = "Assertion failed";
			if (node.Arguments.Count >= 2 && node.Arguments[1] is StringLiteralNode strNode) {
				message = strNode.Value;
			}

			_errors.Add(new SemanticError(message, node.Location));
		}
	}

	/// <summary>
	/// Handles .error directive for unconditional errors.
	/// </summary>
	private void HandleErrorDirective(DirectiveNode node) {
		if (_pass != 1) return;

		string message = "Error directive";
		if (node.Arguments.Count >= 1 && node.Arguments[0] is StringLiteralNode strNode) {
			message = strNode.Value;
		}

		_errors.Add(new SemanticError(message, node.Location));
	}

	/// <summary>
	/// Handles .warning directive for unconditional warnings.
	/// </summary>
	private void HandleWarningDirective(DirectiveNode node) {
		if (_pass != 1) return;

		string message = "Warning";
		if (node.Arguments.Count >= 1 && node.Arguments[0] is StringLiteralNode strNode) {
			message = strNode.Value;
		}

		// For now, treat warnings as errors (could add separate warning list later)
		_errors.Add(new SemanticError($"Warning: {message}", node.Location));
	}

	// ========================================================================
	// Expression Evaluation
	// ========================================================================

	/// <summary>
	/// Attempts to evaluate an expression to a constant value.
	/// </summary>
	/// <param name="expr">The expression to evaluate.</param>
	/// <returns>The value, or null if not evaluable.</returns>
	public long? EvaluateExpression(ExpressionNode expr) {
		return expr switch {
			NumberLiteralNode num => num.Value,
			IdentifierNode id => EvaluateIdentifier(id),
			BinaryExpressionNode bin => EvaluateBinary(bin),
			UnaryExpressionNode un => EvaluateUnary(un),
			_ => null
		};
	}

	/// <summary>
	/// Evaluates an identifier reference.
	/// </summary>
	private long? EvaluateIdentifier(IdentifierNode node) {
		// Special identifiers
		if (node.Name == "*" || node.Name == "$") {
			return _currentAddress;
		}

		if (_symbolTable.TryGetSymbol(node.Name, out var symbol) && symbol?.Value.HasValue == true) {
			return symbol.Value;
		}

		return null;
	}

	/// <summary>
	/// Evaluates a binary expression.
	/// </summary>
	private long? EvaluateBinary(BinaryExpressionNode node) {
		var left = EvaluateExpression(node.Left);
		var right = EvaluateExpression(node.Right);

		if (!left.HasValue || !right.HasValue) {
			return null;
		}

		return node.Operator switch {
			BinaryOperator.Add => left.Value + right.Value,
			BinaryOperator.Subtract => left.Value - right.Value,
			BinaryOperator.Multiply => left.Value * right.Value,
			BinaryOperator.Divide => right.Value != 0 ? left.Value / right.Value : null,
			BinaryOperator.Modulo => right.Value != 0 ? left.Value % right.Value : null,
			BinaryOperator.BitwiseAnd => left.Value & right.Value,
			BinaryOperator.BitwiseOr => left.Value | right.Value,
			BinaryOperator.BitwiseXor => left.Value ^ right.Value,
			BinaryOperator.LeftShift => left.Value << (int)right.Value,
			BinaryOperator.RightShift => left.Value >> (int)right.Value,
			BinaryOperator.Equal => left.Value == right.Value ? 1 : 0,
			BinaryOperator.NotEqual => left.Value != right.Value ? 1 : 0,
			BinaryOperator.LessThan => left.Value < right.Value ? 1 : 0,
			BinaryOperator.GreaterThan => left.Value > right.Value ? 1 : 0,
			BinaryOperator.LessOrEqual => left.Value <= right.Value ? 1 : 0,
			BinaryOperator.GreaterOrEqual => left.Value >= right.Value ? 1 : 0,
			BinaryOperator.LogicalAnd => (left.Value != 0 && right.Value != 0) ? 1 : 0,
			BinaryOperator.LogicalOr => (left.Value != 0 || right.Value != 0) ? 1 : 0,
			_ => null
		};
	}

	/// <summary>
	/// Evaluates a unary expression.
	/// </summary>
	private long? EvaluateUnary(UnaryExpressionNode node) {
		var operand = EvaluateExpression(node.Operand);

		if (!operand.HasValue) {
			return null;
		}

		return node.Operator switch {
			UnaryOperator.Negate => -operand.Value,
			UnaryOperator.BitwiseNot => ~operand.Value,
			UnaryOperator.LogicalNot => operand.Value == 0 ? 1 : 0,
			UnaryOperator.LowByte => operand.Value & 0xff,
			UnaryOperator.HighByte => (operand.Value >> 8) & 0xff,
			UnaryOperator.BankByte => (operand.Value >> 16) & 0xff,
			_ => null
		};
	}

	// ========================================================================
	// Instruction Size Calculation
	// ========================================================================

	/// <summary>
	/// Calculates the size of an instruction in bytes.
	/// </summary>
	private int GetInstructionSize(InstructionNode node) {
		// Branch instructions are always 2 bytes (opcode + relative offset)
		var mnemonic = node.Mnemonic;
		if (mnemonic.Length > 2 && mnemonic[^2] == '.') {
			mnemonic = mnemonic[..^2];
		}

		if (IsBranchInstruction(mnemonic)) {
			return 2; // opcode + 1 byte relative offset
		}

		// Base opcode is 1 byte
		int size = 1;

		// Add operand size based on addressing mode
		size += GetOperandSize(node.AddressingMode, node.SizeSuffix);

		return size;
	}

	/// <summary>
	/// Gets the operand size for an addressing mode.
	/// </summary>
	private int GetOperandSize(AddressingMode mode, char? sizeSuffix) {
		// Size suffix overrides
		if (sizeSuffix.HasValue) {
			return sizeSuffix.Value switch {
				'b' => 1,
				'w' => 2,
				'l' => 3,
				_ => 0
			};
		}

		return mode switch {
			AddressingMode.Implied => 0,
			AddressingMode.Accumulator => 0,
			AddressingMode.Immediate => _target == TargetArchitecture.WDC65816 ? 2 : 1, // Depends on M/X flags
			AddressingMode.ZeroPage => 1,
			AddressingMode.ZeroPageX => 1,
			AddressingMode.ZeroPageY => 1,
			AddressingMode.Absolute => 2,
			AddressingMode.AbsoluteX => 2,
			AddressingMode.AbsoluteY => 2,
			AddressingMode.Indirect => 2,
			AddressingMode.IndexedIndirect => 1,
			AddressingMode.IndirectIndexed => 1,
			AddressingMode.Relative => 1,
			AddressingMode.AbsoluteLong => 3,
			AddressingMode.AbsoluteLongX => 3,
			AddressingMode.StackRelative => 1,
			AddressingMode.StackRelativeIndirectIndexed => 1,
			AddressingMode.DirectPageIndirectLong => 1,
			AddressingMode.DirectPageIndirectLongY => 1,
			AddressingMode.AbsoluteIndirectLong => 2,
			AddressingMode.AbsoluteIndexedIndirect => 2,
			AddressingMode.BlockMove => 2,
			AddressingMode.MemoryReference => 0, // GB-specific, varies
			_ => 0
		};
	}
}

/// <summary>
/// Target CPU architecture for the assembler.
/// </summary>
public enum TargetArchitecture {
	/// <summary>MOS 6502 (NES, Commodore 64, etc.)</summary>
	MOS6502,

	/// <summary>WDC 65816 (SNES)</summary>
	WDC65816,

	/// <summary>Sharp SM83 (Game Boy)</summary>
	SM83,
}

