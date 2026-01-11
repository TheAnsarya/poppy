// ============================================================================
// CodeGenerator.cs - Binary Code Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Generates binary code from an analyzed AST.
/// </summary>
public sealed class CodeGenerator : IAstVisitor<object?>
{
	private readonly SemanticAnalyzer _analyzer;
	private readonly TargetArchitecture _target;
	private readonly List<CodeError> _errors;
	private readonly List<OutputSegment> _segments;
	private OutputSegment? _currentSegment;
	private long _currentAddress;

	/// <summary>
	/// Gets all code generation errors.
	/// </summary>
	public IReadOnlyList<CodeError> Errors => _errors;

	/// <summary>
	/// Gets whether generation encountered any errors.
	/// </summary>
	public bool HasErrors => _errors.Count > 0;

	/// <summary>
	/// Gets the output segments.
	/// </summary>
	public IReadOnlyList<OutputSegment> Segments => _segments;

	/// <summary>
	/// Creates a new code generator.
	/// </summary>
	/// <param name="analyzer">The semantic analyzer with symbol table.</param>
	/// <param name="target">The target architecture.</param>
	public CodeGenerator(SemanticAnalyzer analyzer, TargetArchitecture target = TargetArchitecture.MOS6502)
	{
		_analyzer = analyzer;
		_target = target;
		_errors = [];
		_segments = [];
		_currentAddress = 0;
	}

	/// <summary>
	/// Generates code for a program.
	/// </summary>
	/// <param name="program">The program AST.</param>
	/// <returns>The generated binary data.</returns>
	public byte[] Generate(ProgramNode program)
	{
		_currentAddress = 0;
		_currentSegment = null;
		_segments.Clear();

		// Generate code for all statements
		foreach (var statement in program.Statements) {
			statement.Accept(this);
		}

		// Flatten segments into output
		return FlattenSegments();
	}

	/// <inheritdoc />
	public object? VisitProgram(ProgramNode node)
	{
		foreach (var statement in node.Statements) {
			statement.Accept(this);
		}
		return null;
	}

	/// <inheritdoc />
	public object? VisitLabel(LabelNode node)
	{
		// Labels don't generate code, just update address tracking
		return null;
	}

	/// <inheritdoc />
	public object? VisitInstruction(InstructionNode node)
	{
		EnsureSegment(node.Location);

		var mnemonic = node.Mnemonic;

		// Handle size suffixes
		if (mnemonic.Length > 2 && mnemonic[^2] == '.') {
			mnemonic = mnemonic[..^2];
		}

		// Resolve addressing mode based on operand value
		var addressingMode = node.AddressingMode;
		long? operandValue = null;

		if (node.Operand is not null) {
			operandValue = _analyzer.EvaluateExpression(node.Operand);

			// Optimize Absolute to ZeroPage if value fits and instruction supports it
			if (operandValue.HasValue) {
				addressingMode = ResolveAddressingMode(mnemonic, addressingMode, operandValue.Value);
			}
		}

		// Get instruction encoding
		if (!TryGetInstructionEncoding(mnemonic, addressingMode, out var encoding)) {
			_errors.Add(new CodeError(
				$"Invalid addressing mode {addressingMode} for instruction '{mnemonic}'",
				node.Location));
			return null;
		}

		// Emit opcode
		EmitByte(encoding.Opcode);

		// Emit operand if present
		if (node.Operand is not null) {
			if (!operandValue.HasValue) {
				_errors.Add(new CodeError(
					$"Cannot evaluate operand for instruction '{mnemonic}'",
					node.Location));
				return null;
			}

			// Handle branch instructions (relative addressing)
			if (IsBranchInstruction(mnemonic)) {
				// Branch offset is relative to the address AFTER the branch instruction
				// After opcode is emitted, _currentAddress points to the operand byte
				// The offset is calculated from the next instruction (operand address + 1)
				var nextInstructionAddress = _currentAddress + 1;
				var offset = operandValue.Value - nextInstructionAddress;
				if (offset < -128 || offset > 127) {
					_errors.Add(new CodeError(
						$"Branch target out of range ({offset} bytes, must be -128 to +127)",
						node.Location));
				}
				EmitByte((byte)(offset & 0xff));
			}
			else {
				// Emit operand based on size
				var operandSize = encoding.Size - 1;
				EmitValue(operandValue.Value, operandSize, node.SizeSuffix);
			}
		}

		return null;
	}

	/// <summary>
	/// Resolves the best addressing mode based on operand value.
	/// </summary>
	private AddressingMode ResolveAddressingMode(string mnemonic, AddressingMode mode, long value)
	{
		// Check if we can optimize to zero page variant
		var isZeroPage = value >= 0 && value <= 0xff;

		var optimizedMode = mode switch {
			// Optimize absolute to zero page
			AddressingMode.Absolute when isZeroPage
				&& InstructionSet6502.TryGetEncoding(mnemonic, AddressingMode.ZeroPage, out _)
				=> AddressingMode.ZeroPage,

			AddressingMode.AbsoluteX when isZeroPage
				&& InstructionSet6502.TryGetEncoding(mnemonic, AddressingMode.ZeroPageX, out _)
				=> AddressingMode.ZeroPageX,

			AddressingMode.AbsoluteY when isZeroPage
				&& InstructionSet6502.TryGetEncoding(mnemonic, AddressingMode.ZeroPageY, out _)
				=> AddressingMode.ZeroPageY,

			// Keep original mode
			_ => mode
		};

		return optimizedMode;
	}

	/// <inheritdoc />
	public object? VisitDirective(DirectiveNode node)
	{
		switch (node.Name.ToLowerInvariant()) {
			case "org":
				HandleOrgDirective(node);
				break;

			case "byte":
			case "db":
				HandleByteDirective(node);
				break;

			case "word":
			case "dw":
				HandleWordDirective(node);
				break;

			case "long":
			case "dl":
			case "dd":
				HandleLongDirective(node);
				break;

			case "ds":
			case "fill":
			case "res":
				HandleSpaceDirective(node);
				break;

			// Other directives don't generate code
		}

		return null;
	}

	/// <inheritdoc />
	public object? VisitExpression(ExpressionNode node) => null;

	/// <inheritdoc />
	public object? VisitBinaryExpression(BinaryExpressionNode node) => null;

	/// <inheritdoc />
	public object? VisitUnaryExpression(UnaryExpressionNode node) => null;

	/// <inheritdoc />
	public object? VisitNumberLiteral(NumberLiteralNode node) => node.Value;

	/// <inheritdoc />
	public object? VisitStringLiteral(StringLiteralNode node) => node.Value;

	/// <inheritdoc />
	public object? VisitIdentifier(IdentifierNode node)
	{
		if (_analyzer.SymbolTable.TryGetSymbol(node.Name, out var symbol) && symbol?.Value.HasValue == true) {
			return symbol.Value;
		}
		return null;
	}

	/// <inheritdoc />
	public object? VisitMacroDefinition(MacroDefinitionNode node) => null;

	/// <inheritdoc />
	public object? VisitMacroInvocation(MacroInvocationNode node) => null;

	// ========================================================================
	// Directive Handlers
	// ========================================================================

	/// <summary>
	/// Handles .org directive.
	/// </summary>
	private void HandleOrgDirective(DirectiveNode node)
	{
		if (node.Arguments.Count < 1) return;

		var value = _analyzer.EvaluateExpression(node.Arguments[0]);
		if (value.HasValue) {
			_currentAddress = value.Value;

			// Create a new segment at the new address
			_currentSegment = new OutputSegment(_currentAddress);
			_segments.Add(_currentSegment);
		}
	}

	/// <summary>
	/// Handles .byte / .db directive.
	/// </summary>
	private void HandleByteDirective(DirectiveNode node)
	{
		EnsureSegment(node.Location);

		foreach (var arg in node.Arguments) {
			if (arg is StringLiteralNode strNode) {
				// Emit each character as a byte
				foreach (var c in strNode.Value) {
					EmitByte((byte)c);
				}
			}
			else {
				var value = _analyzer.EvaluateExpression(arg);
				if (value.HasValue) {
					EmitByte((byte)(value.Value & 0xff));
				}
				else {
					_errors.Add(new CodeError(
						"Cannot evaluate .byte argument",
						node.Location));
					EmitByte(0);
				}
			}
		}
	}

	/// <summary>
	/// Handles .word / .dw directive.
	/// </summary>
	private void HandleWordDirective(DirectiveNode node)
	{
		EnsureSegment(node.Location);

		foreach (var arg in node.Arguments) {
			var value = _analyzer.EvaluateExpression(arg);
			if (value.HasValue) {
				EmitWord((ushort)(value.Value & 0xffff));
			}
			else {
				_errors.Add(new CodeError(
					"Cannot evaluate .word argument",
					node.Location));
				EmitWord(0);
			}
		}
	}

	/// <summary>
	/// Handles .long / .dl / .dd directive.
	/// </summary>
	private void HandleLongDirective(DirectiveNode node)
	{
		EnsureSegment(node.Location);

		var bytes = _target == TargetArchitecture.WDC65816 ? 3 : 4;

		foreach (var arg in node.Arguments) {
			var value = _analyzer.EvaluateExpression(arg);
			if (value.HasValue) {
				EmitValue(value.Value, bytes, null);
			}
			else {
				_errors.Add(new CodeError(
					"Cannot evaluate .long argument",
					node.Location));
				for (int i = 0; i < bytes; i++) {
					EmitByte(0);
				}
			}
		}
	}

	/// <summary>
	/// Handles .ds / .fill / .res directive.
	/// </summary>
	private void HandleSpaceDirective(DirectiveNode node)
	{
		EnsureSegment(node.Location);

		if (node.Arguments.Count < 1) return;

		var count = _analyzer.EvaluateExpression(node.Arguments[0]);
		if (!count.HasValue) return;

		byte fillValue = 0;
		if (node.Arguments.Count >= 2) {
			var fill = _analyzer.EvaluateExpression(node.Arguments[1]);
			if (fill.HasValue) {
				fillValue = (byte)(fill.Value & 0xff);
			}
		}

		for (int i = 0; i < count.Value; i++) {
			EmitByte(fillValue);
		}
	}

	// ========================================================================
	// Helper Methods
	// ========================================================================

	/// <summary>
	/// Ensures a current segment exists.
	/// </summary>
	private void EnsureSegment(SourceLocation location)
	{
		if (_currentSegment is null) {
			_currentSegment = new OutputSegment(_currentAddress);
			_segments.Add(_currentSegment);
		}
	}

	/// <summary>
	/// Emits a single byte.
	/// </summary>
	private void EmitByte(byte value)
	{
		_currentSegment?.Data.Add(value);
		_currentAddress++;
	}

	/// <summary>
	/// Emits a 16-bit word (little-endian).
	/// </summary>
	private void EmitWord(ushort value)
	{
		EmitByte((byte)(value & 0xff));
		EmitByte((byte)((value >> 8) & 0xff));
	}

	/// <summary>
	/// Emits a value with the specified number of bytes.
	/// </summary>
	private void EmitValue(long value, int bytes, char? sizeSuffix)
	{
		// Size suffix overrides
		if (sizeSuffix.HasValue) {
			bytes = sizeSuffix.Value switch {
				'b' => 1,
				'w' => 2,
				'l' => 3,
				_ => bytes
			};
		}

		for (int i = 0; i < bytes; i++) {
			EmitByte((byte)((value >> (i * 8)) & 0xff));
		}
	}

	/// <summary>
	/// Tries to get instruction encoding from the appropriate instruction set.
	/// </summary>
	private bool TryGetInstructionEncoding(string mnemonic, AddressingMode mode, out InstructionSet6502.InstructionEncoding encoding)
	{
		// For now, only 6502 is implemented
		return InstructionSet6502.TryGetEncoding(mnemonic, mode, out encoding);
	}

	/// <summary>
	/// Checks if an instruction is a branch instruction.
	/// </summary>
	private static bool IsBranchInstruction(string mnemonic)
	{
		return mnemonic.ToLowerInvariant() switch {
			"bcc" or "bcs" or "beq" or "bmi" or "bne" or "bpl" or "bvc" or "bvs" => true,
			_ => false
		};
	}

	/// <summary>
	/// Flattens all segments into a single byte array.
	/// </summary>
	private byte[] FlattenSegments()
	{
		if (_segments.Count == 0) {
			return [];
		}

		// Find the address range
		var minAddress = _segments.Min(s => s.StartAddress);
		var maxAddress = _segments.Max(s => s.StartAddress + s.Data.Count);

		// Create output buffer
		var output = new byte[maxAddress - minAddress];

		// Copy each segment into the output
		foreach (var segment in _segments) {
			var offset = segment.StartAddress - minAddress;
			for (int i = 0; i < segment.Data.Count; i++) {
				output[offset + i] = segment.Data[i];
			}
		}

		return output;
	}
}

/// <summary>
/// Represents an output segment with a start address and data.
/// </summary>
public sealed class OutputSegment
{
	/// <summary>
	/// The starting address of this segment.
	/// </summary>
	public long StartAddress { get; }

	/// <summary>
	/// The data bytes in this segment.
	/// </summary>
	public List<byte> Data { get; } = [];

	/// <summary>
	/// Creates a new output segment.
	/// </summary>
	/// <param name="startAddress">The starting address.</param>
	public OutputSegment(long startAddress)
	{
		StartAddress = startAddress;
	}
}

/// <summary>
/// Represents a code generation error.
/// </summary>
public sealed class CodeError
{
	/// <summary>
	/// The error message.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// The source location where the error occurred.
	/// </summary>
	public SourceLocation Location { get; }

	/// <summary>
	/// Creates a new code error.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="location">The source location.</param>
	public CodeError(string message, SourceLocation location)
	{
		Message = message;
		Location = location;
	}

	/// <inheritdoc />
	public override string ToString() => $"{Location}: error: {Message}";
}

