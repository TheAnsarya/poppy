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
public sealed class CodeGenerator : IAstVisitor<object?> {
	private readonly SemanticAnalyzer _analyzer;
	private readonly TargetArchitecture _target;
	private readonly List<CodeError> _errors;
	private readonly List<OutputSegment> _segments;
	private readonly MacroExpander _macroExpander;
	private OutputSegment? _currentSegment;
	private long _currentAddress;

	// 65816 M/X flag tracking for correct immediate operand sizes
	private bool _accumulatorIs16Bit = false;  // M flag: false = 8-bit, true = 16-bit
	private bool _indexIs16Bit = false;        // X flag: false = 8-bit, true = 16-bit

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
	public CodeGenerator(SemanticAnalyzer analyzer, TargetArchitecture target = TargetArchitecture.MOS6502) {
		_analyzer = analyzer;
		_target = target;
		_errors = [];
		_segments = [];
		_macroExpander = new MacroExpander(analyzer.MacroTable);
		_currentAddress = 0;
	}

	/// <summary>
	/// Generates code for a program.
	/// </summary>
	/// <param name="program">The program AST.</param>
	/// <returns>The generated binary data.</returns>
	public byte[] Generate(ProgramNode program) {
		_currentAddress = 0;
		_currentSegment = null;
		_segments.Clear();

		// Generate code for all statements
		foreach (var statement in program.Statements) {
			statement.Accept(this);
		}

		// Flatten segments into output
		var binary = FlattenSegments();

		// Prepend iNES header if configured (for NES target only)
		if (_target == TargetArchitecture.MOS6502) {
			var headerBuilder = _analyzer.GetINesHeaderBuilder();
			if (headerBuilder is not null) {
				var header = headerBuilder.Build();
				var output = new byte[header.Length + binary.Length];
				Array.Copy(header, 0, output, 0, header.Length);
				Array.Copy(binary, 0, output, header.Length, binary.Length);
				return output;
			}
		}

		// Build Atari 2600 ROM if configured (for Atari 2600 target only)
		if (_target == TargetArchitecture.MOS6507) {
			// Default to 4K ROM if no header builder is configured
			var romSize = 4096;  // TODO: Get from header builder or directive
			var romBuilder = new Atari2600RomBuilder(romSize, Atari2600RomBuilder.BankSwitchingMethod.None);

			// Add all segments to the ROM builder
			foreach (var segment in _segments) {
				romBuilder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}

			return romBuilder.Build();
		}

		// Build Atari Lynx ROM if configured (for Atari Lynx target only)
		if (_target == TargetArchitecture.MOS65SC02) {
			// Default to 128K ROM if no header builder is configured
			var romSize = 131072;  // 128K
			var romBuilder = new AtariLynxRomBuilder(romSize, "Poppy Game");

			// Add all segments to the ROM builder
			foreach (var segment in _segments) {
				romBuilder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}

			return romBuilder.Build();
		}

		// Prepend SNES header if configured (for SNES/65816 target only)
		if (_target == TargetArchitecture.WDC65816) {
			var headerBuilder = _analyzer.GetSnesHeaderBuilder();
			if (headerBuilder is not null) {
				var header = headerBuilder.Build();
				var mapMode = GetSnesMapMode();

				// Use SnesRomBuilder to place header at correct offset
				var romBuilder = new SnesRomBuilder(mapMode, header);

				// Add all segments to the ROM builder
				foreach (var segment in _segments) {
					romBuilder.AddSegment(segment.StartAddress, segment.Data.ToArray());
				}

				return romBuilder.Build();
			}
		}

		// Prepend GB header if configured (for Game Boy target only)
		if (_target == TargetArchitecture.SM83) {
			var headerBuilder = _analyzer.GetGbHeaderBuilder();
			if (headerBuilder is not null) {
				var header = headerBuilder.Build();

				// Use GbRomBuilder to place header at $0100
				var romBuilder = new GbRomBuilder(header);

				// Add all segments to the ROM builder
				foreach (var segment in _segments) {
					romBuilder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
				}

				return romBuilder.Build();
			}
		}

		return binary;
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
		// Labels don't generate code, just update address tracking
		return null;
	}

	/// <inheritdoc />
	public object? VisitInstruction(InstructionNode node) {
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
			// Sync the analyzer's current address for anonymous label resolution
			_analyzer.CurrentAddress = _currentAddress;
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
			} else {
				// Emit operand based on size
				// For 65816 immediate mode, size depends on M/X flags
				var operandSize = GetOperandSize(mnemonic, addressingMode, encoding);
				EmitValue(operandValue.Value, operandSize, node.SizeSuffix);
			}

			// Track REP/SEP instructions for M/X flag state (65816)
			if (_target == TargetArchitecture.WDC65816) {
				var lower = mnemonic.ToLowerInvariant();
				if (lower == "rep" && operandValue.HasValue) {
					// REP clears flags (sets to 16-bit mode)
					if ((operandValue.Value & 0x20) != 0) _accumulatorIs16Bit = true;  // M flag
					if ((operandValue.Value & 0x10) != 0) _indexIs16Bit = true;        // X flag
				} else if (lower == "sep" && operandValue.HasValue) {
					// SEP sets flags (sets to 8-bit mode)
					if ((operandValue.Value & 0x20) != 0) _accumulatorIs16Bit = false; // M flag
					if ((operandValue.Value & 0x10) != 0) _indexIs16Bit = false;       // X flag
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Resolves the best addressing mode based on operand value.
	/// </summary>
	private AddressingMode ResolveAddressingMode(string mnemonic, AddressingMode mode, long value) {
		// Convert Absolute to Relative for branch instructions
		if (IsBranchInstruction(mnemonic) && mode == AddressingMode.Absolute) {
			return AddressingMode.Relative;
		}

		// Check if we can optimize to zero page variant
		var isZeroPage = value >= 0 && value <= 0xff;

		var optimizedMode = mode switch {
			// Optimize absolute to zero page
			AddressingMode.Absolute when isZeroPage
				&& TryGetInstructionEncoding(mnemonic, AddressingMode.ZeroPage, out _)
				=> AddressingMode.ZeroPage,

			AddressingMode.AbsoluteX when isZeroPage
				&& TryGetInstructionEncoding(mnemonic, AddressingMode.ZeroPageX, out _)
				=> AddressingMode.ZeroPageX,

			AddressingMode.AbsoluteY when isZeroPage
				&& TryGetInstructionEncoding(mnemonic, AddressingMode.ZeroPageY, out _)
				=> AddressingMode.ZeroPageY,

			// Keep original mode
			_ => mode
		};

		return optimizedMode;
	}

	/// <inheritdoc />
	public object? VisitDirective(DirectiveNode node) {
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

			case "incbin":
				HandleIncbinDirective(node);
				break;

			case "align":
				HandleAlignDirective(node);
				break;

			case "pad":
				HandlePadDirective(node);
				break;

			// 65816 register size directives
			case "a8":
				_accumulatorIs16Bit = false;
				break;
			case "a16":
				_accumulatorIs16Bit = true;
				break;
			case "i8":
				_indexIs16Bit = false;
				break;
			case "i16":
				_indexIs16Bit = true;
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
	public object? VisitIdentifier(IdentifierNode node) {
		if (_analyzer.SymbolTable.TryGetSymbol(node.Name, out var symbol) && symbol?.Value.HasValue == true) {
			return symbol.Value;
		}

		return null;
	}

	/// <inheritdoc />
	public object? VisitMacroDefinition(MacroDefinitionNode node) {
		// Macro definitions don't generate code, they're stored in the macro table
		return null;
	}

	/// <inheritdoc />
	public object? VisitMacroInvocation(MacroInvocationNode node) {
		// Expand the macro and generate code for each expanded statement
		var expandedStatements = _macroExpander.Expand(node, node.Arguments);

		// Report any expansion errors
		foreach (var error in _macroExpander.Errors) {
			_errors.Add(new CodeError(error.Message, error.Location));
		}

		// Generate code for each expanded statement
		foreach (var statement in expandedStatements) {
			statement.Accept(this);
		}

		return null;
	}

	/// <inheritdoc />
	public object? VisitConditional(ConditionalNode node) {
		// Evaluate the condition
		var conditionValue = _analyzer.EvaluateConditionalExpression(node.Condition);

		// Determine which block to execute
		if (conditionValue != 0) {
			// Execute the then block
			foreach (var statement in node.ThenBlock) {
				statement.Accept(this);
			}
		} else {
			// Try elseif branches
			bool executed = false;
			foreach (var (condition, block) in node.ElseIfBranches) {
				var elseIfValue = _analyzer.EvaluateConditionalExpression(condition);
				if (elseIfValue != 0) {
					foreach (var statement in block) {
						statement.Accept(this);
					}

					executed = true;
					break;
				}
			}

			// Execute else block if no conditions were true
			if (!executed && node.ElseBlock is not null) {
				foreach (var statement in node.ElseBlock) {
					statement.Accept(this);
				}
			}
		}

		return null;
	}

	/// <inheritdoc />
	public object? VisitRepeatBlock(RepeatBlockNode node) {
		// Evaluate the repeat count
		var countValue = _analyzer.EvaluateExpression(node.Count);
		if (!countValue.HasValue) {
			_errors.Add(new CodeError(
				"Cannot evaluate repeat count",
				node.Location));
			return null;
		}

		var count = (int)countValue.Value;
		if (count < 0) {
			_errors.Add(new CodeError(
				$"Repeat count cannot be negative: {count}",
				node.Location));
			return null;
		}

		// Generate code for the body 'count' times
		for (int i = 0; i < count; i++) {
			foreach (var statement in node.Body) {
				statement.Accept(this);
			}
		}

		return null;
	}

	/// <inheritdoc />
	public object? VisitEnumerationBlock(EnumerationBlockNode node) {
		// Enumeration blocks don't generate code, they define symbols
		// which are already handled by the semantic analyzer
		return null;
	}

	// ========================================================================
	// Directive Handlers
	// ========================================================================

	/// <summary>
	/// Handles .org directive.
	/// </summary>
	private void HandleOrgDirective(DirectiveNode node) {
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
	private void HandleByteDirective(DirectiveNode node) {
		EnsureSegment(node.Location);

		foreach (var arg in node.Arguments) {
			if (arg is StringLiteralNode strNode) {
				// Emit each character as a byte
				foreach (var c in strNode.Value) {
					EmitByte((byte)c);
				}
			} else {
				var value = _analyzer.EvaluateExpression(arg);
				if (value.HasValue) {
					EmitByte((byte)(value.Value & 0xff));
				} else {
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
	private void HandleWordDirective(DirectiveNode node) {
		EnsureSegment(node.Location);

		foreach (var arg in node.Arguments) {
			var value = _analyzer.EvaluateExpression(arg);
			if (value.HasValue) {
				EmitWord((ushort)(value.Value & 0xffff));
			} else {
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
	private void HandleLongDirective(DirectiveNode node) {
		EnsureSegment(node.Location);

		var bytes = _target == TargetArchitecture.WDC65816 ? 3 : 4;

		foreach (var arg in node.Arguments) {
			var value = _analyzer.EvaluateExpression(arg);
			if (value.HasValue) {
				EmitValue(value.Value, bytes, null);
			} else {
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
	private void HandleSpaceDirective(DirectiveNode node) {
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

	/// <summary>
	/// Handles .incbin directive for binary file inclusion.
	/// </summary>
	private void HandleIncbinDirective(DirectiveNode node) {
		EnsureSegment(node.Location);

		if (node.Arguments.Count < 1) {
			_errors.Add(new CodeError("Missing filename for .incbin directive", node.Location));
			return;
		}

		// Get filename
		if (node.Arguments[0] is not StringLiteralNode filenameNode) {
			_errors.Add(new CodeError("Expected filename string for .incbin directive", node.Location));
			return;
		}

		var filename = filenameNode.Value;

		// Resolve path relative to source file
		var basePath = Path.GetDirectoryName(node.Location.FilePath) ?? ".";
		var fullPath = Path.Combine(basePath, filename);

		if (!File.Exists(fullPath)) {
			_errors.Add(new CodeError($"Binary file not found: {filename}", node.Location));
			return;
		}

		byte[] data;
		try {
			data = File.ReadAllBytes(fullPath);
		} catch (Exception ex) {
			_errors.Add(new CodeError($"Error reading binary file: {ex.Message}", node.Location));
			return;
		}

		// Parse optional offset and length
		long offset = 0;
		long length = data.Length;

		if (node.Arguments.Count >= 2) {
			var offsetValue = _analyzer.EvaluateExpression(node.Arguments[1]);
			if (offsetValue.HasValue) {
				offset = offsetValue.Value;
			}
		}

		if (node.Arguments.Count >= 3) {
			var lengthValue = _analyzer.EvaluateExpression(node.Arguments[2]);
			if (lengthValue.HasValue) {
				length = lengthValue.Value;
			}
		}

		// Validate offset and length
		if (offset < 0 || offset >= data.Length) {
			_errors.Add(new CodeError($"Invalid offset {offset} for file of size {data.Length}", node.Location));
			return;
		}

		if (length < 0 || offset + length > data.Length) {
			_errors.Add(new CodeError($"Invalid length {length} at offset {offset} for file of size {data.Length}", node.Location));
			return;
		}

		// Emit the binary data
		for (long i = 0; i < length; i++) {
			EmitByte(data[offset + i]);
		}
	}

	/// <summary>
	/// Handles .align directive for memory alignment.
	/// </summary>
	private void HandleAlignDirective(DirectiveNode node) {
		EnsureSegment(node.Location);

		if (node.Arguments.Count < 1) {
			_errors.Add(new CodeError("Missing alignment value for .align directive", node.Location));
			return;
		}

		var alignValue = _analyzer.EvaluateExpression(node.Arguments[0]);
		if (!alignValue.HasValue || alignValue.Value <= 0) {
			_errors.Add(new CodeError("Invalid alignment value", node.Location));
			return;
		}

		byte fillValue = 0;
		if (node.Arguments.Count >= 2) {
			var fill = _analyzer.EvaluateExpression(node.Arguments[1]);
			if (fill.HasValue) {
				fillValue = (byte)(fill.Value & 0xff);
			}
		}

		// Calculate padding needed
		var alignment = alignValue.Value;
		var remainder = _currentAddress % alignment;
		if (remainder != 0) {
			var padding = alignment - remainder;
			for (long i = 0; i < padding; i++) {
				EmitByte(fillValue);
			}
		}
	}

	/// <summary>
	/// Handles .pad directive for padding to specific address.
	/// </summary>
	private void HandlePadDirective(DirectiveNode node) {
		EnsureSegment(node.Location);

		if (node.Arguments.Count < 1) {
			_errors.Add(new CodeError("Missing target address for .pad directive", node.Location));
			return;
		}

		var targetAddress = _analyzer.EvaluateExpression(node.Arguments[0]);
		if (!targetAddress.HasValue) {
			_errors.Add(new CodeError("Cannot evaluate target address", node.Location));
			return;
		}

		byte fillValue = 0;
		if (node.Arguments.Count >= 2) {
			var fill = _analyzer.EvaluateExpression(node.Arguments[1]);
			if (fill.HasValue) {
				fillValue = (byte)(fill.Value & 0xff);
			}
		}

		// Check if we're already past the target
		if (_currentAddress > targetAddress.Value) {
			_errors.Add(new CodeError(
				$"Cannot pad backwards: current address ${_currentAddress:x} > target ${targetAddress.Value:x}",
				node.Location));
			return;
		}

		// Emit padding bytes
		var count = targetAddress.Value - _currentAddress;
		for (long i = 0; i < count; i++) {
			EmitByte(fillValue);
		}
	}

	// ========================================================================
	// Helper Methods
	// ========================================================================

	/// <summary>
	/// Ensures a current segment exists.
	/// </summary>
	private void EnsureSegment(SourceLocation location) {
		if (_currentSegment is null) {
			_currentSegment = new OutputSegment(_currentAddress);
			_segments.Add(_currentSegment);
		}
	}

	/// <summary>
	/// Emits a single byte.
	/// </summary>
	private void EmitByte(byte value) {
		_currentSegment?.Data.Add(value);
		_currentAddress++;
	}

	/// <summary>
	/// Emits a 16-bit word (little-endian).
	/// </summary>
	private void EmitWord(ushort value) {
		EmitByte((byte)(value & 0xff));
		EmitByte((byte)((value >> 8) & 0xff));
	}

	/// <summary>
	/// Emits a value with the specified number of bytes.
	/// </summary>
	private void EmitValue(long value, int bytes, char? sizeSuffix) {
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
	/// Gets the operand size for an instruction, accounting for 65816 M/X flags.
	/// </summary>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <param name="mode">The addressing mode.</param>
	/// <param name="encoding">The instruction encoding.</param>
	/// <returns>The operand size in bytes.</returns>
	private int GetOperandSize(string mnemonic, AddressingMode mode, InstructionSet6502.InstructionEncoding encoding) {
		// For 65816 immediate mode, size depends on M/X flags
		if (_target == TargetArchitecture.WDC65816 && mode == AddressingMode.Immediate) {
			var lower = mnemonic.ToLowerInvariant();

			// Index register instructions use X flag
			if (lower is "ldx" or "ldy" or "cpx" or "cpy") {
				return _indexIs16Bit ? 2 : 1;
			}

			// Accumulator instructions use M flag
			if (lower is "lda" or "adc" or "sbc" or "cmp" or "and" or "ora" or "eor" or "bit") {
				return _accumulatorIs16Bit ? 2 : 1;
			}

			// REP/SEP are always 8-bit immediate
			if (lower is "rep" or "sep") {
				return 1;
			}

			// PEA is always 16-bit immediate
			if (lower is "pea") {
				return 2;
			}

			// Default to current accumulator size for other immediate instructions
			return _accumulatorIs16Bit ? 2 : 1;
		}

		// For non-65816 or non-immediate modes, use the encoding's static size
		return encoding.Size - 1;
	}

	/// <summary>
	/// Tries to get instruction encoding from the appropriate instruction set.
	/// </summary>
	private bool TryGetInstructionEncoding(string mnemonic, AddressingMode mode, out InstructionSet6502.InstructionEncoding encoding) {
		// Select instruction set based on target architecture
		if (_target == TargetArchitecture.SM83) {
			if (InstructionSetSM83.TryGetEncoding(mnemonic, mode, out var sm83Encoding)) {
				// Convert SM83 encoding to 6502 encoding format for compatibility
				encoding = new InstructionSet6502.InstructionEncoding(sm83Encoding.Opcode, sm83Encoding.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		if (_target == TargetArchitecture.WDC65816) {
			if (InstructionSet65816.TryGetEncoding(mnemonic, mode, out var enc65816)) {
				encoding = new InstructionSet6502.InstructionEncoding(enc65816.Opcode, enc65816.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		if (_target == TargetArchitecture.MOS6507) {
			if (InstructionSet6507.TryGetEncoding(mnemonic, mode, out var enc6507)) {
				encoding = new InstructionSet6502.InstructionEncoding(enc6507.Opcode, enc6507.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		if (_target == TargetArchitecture.MOS65SC02) {
			if (InstructionSet65SC02.TryGetEncoding(mnemonic, mode, out var enc65sc02)) {
				encoding = new InstructionSet6502.InstructionEncoding(enc65sc02.Opcode, enc65sc02.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		// Default to 6502
		return InstructionSet6502.TryGetEncoding(mnemonic, mode, out encoding);
	}

	/// <summary>
	/// Checks if an instruction is a branch instruction.
	/// </summary>
	private bool IsBranchInstruction(string mnemonic) {
		if (_target == TargetArchitecture.SM83) {
			return InstructionSetSM83.IsRelativeBranch(mnemonic);
		}

		if (_target == TargetArchitecture.WDC65816) {
			return InstructionSet65816.IsBranchInstruction(mnemonic);
		}

		if (_target == TargetArchitecture.MOS65SC02) {
			return InstructionSet65SC02.IsBranchInstruction(mnemonic);
		}

		return mnemonic.ToLowerInvariant() switch {
			"bcc" or "bcs" or "beq" or "bmi" or "bne" or "bpl" or "bvc" or "bvs" => true,
			_ => false
		};
	}

	/// <summary>
	/// Gets the SNES memory mapping mode from the analyzer.
	/// </summary>
	private SnesMapMode GetSnesMapMode() {
		var mapping = _analyzer.MemoryMapping?.ToLowerInvariant();
		return mapping switch {
			"lorom" => SnesMapMode.LoRom,
			"hirom" => SnesMapMode.HiRom,
			"exhirom" => SnesMapMode.ExHiRom,
			_ => SnesMapMode.LoRom // Default to LoROM
		};
	}

	/// <summary>
	/// Flattens all segments into a single byte array.
	/// </summary>
	private byte[] FlattenSegments() {
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
public sealed class OutputSegment {
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
	public OutputSegment(long startAddress) {
		StartAddress = startAddress;
	}
}

/// <summary>
/// Represents a code generation error.
/// </summary>
public sealed class CodeError {
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
	public CodeError(string message, SourceLocation location) {
		Message = message;
		Location = location;
	}

	/// <inheritdoc />
	public override string ToString() => $"{Location}: error: {Message}";
}

