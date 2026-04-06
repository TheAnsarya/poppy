// ============================================================================
// CodeGenerator.cs - Binary Code Generation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Arch;
using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

namespace Poppy.Core.CodeGen;

/// <summary>
/// Generates binary code from an analyzed AST.
/// </summary>
public sealed class CodeGenerator : IAstVisitor<object?> {
	private readonly SemanticAnalyzer _analyzer;
	private TargetArchitecture _target;
	private readonly TargetArchitecture _initialTarget;
	private ITargetProfile _profile;
	private readonly List<CodeError> _errors;
	private readonly List<CodeWarning> _warnings;
	private readonly List<OutputSegment> _segments;
	private readonly MacroExpander _macroExpander;
	private OutputSegment? _currentSegment;
	private long _currentAddress;

	// Optional CDL generator for tracking jump/call targets
	private readonly CdlGenerator? _cdlGenerator;

	// Optional listing generator for source map tracking
	private readonly ListingGenerator? _listingGenerator;

	// Cross-reference tracking from instruction analysis
	private readonly List<(uint From, uint To, byte Type)> _crossRefs = [];

	// Bank tracking for multi-bank ROM assembly
	private int _currentBank = -1;        // Current bank number (-1 = unbanked)
	private int _bankSize;                // Bank size in bytes (auto-detected or .banksize)
	private long _bankRomOffset = -1;     // ROM file offset of current bank start
	private long _bankCpuBase = -1;       // CPU base address of banked window

	// 65816 M/X flag tracking for correct immediate operand sizes
	private bool _accumulatorIs16Bit = false;  // M flag: false = 8-bit, true = 16-bit
	private bool _indexIs16Bit = false;        // X flag: false = 8-bit, true = 16-bit

	/// <summary>
	/// Gets all code generation errors.
	/// </summary>
	public IReadOnlyList<CodeError> Errors => _errors;

	/// <summary>
	/// Gets all code generation warnings.
	/// </summary>
	public IReadOnlyList<CodeWarning> Warnings => _warnings;

	/// <summary>
	/// Gets whether generation encountered any errors.
	/// </summary>
	public bool HasErrors => _errors.Count > 0;

	/// <summary>
	/// Gets whether generation encountered any warnings.
	/// </summary>
	public bool HasWarnings => _warnings.Count > 0;

	/// <summary>
	/// Gets the output segments.
	/// </summary>
	public IReadOnlyList<OutputSegment> Segments => _segments;

	/// <summary>
	/// Gets the current target architecture.
	/// </summary>
	public TargetArchitecture CurrentTarget => _target;

	/// <summary>
	/// Gets cross-references discovered during code generation.
	/// Each tuple is (FromAddress, ToAddress, CrossRefType) where type matches Pansy spec:
	/// Jsr=1, Jmp=2, Branch=3.
	/// </summary>
	public IReadOnlyList<(uint From, uint To, byte Type)> CrossReferences => _crossRefs;

	/// <summary>
	/// Gets the listing generator (if provided), for passing to PansyGenerator.
	/// </summary>
	public ListingGenerator? ListingGenerator => _listingGenerator;

	/// <summary>
	/// Creates a new code generator.
	/// </summary>
	/// <param name="analyzer">The semantic analyzer with symbol table.</param>
	/// <param name="target">The target architecture.</param>
	/// <param name="cdlGenerator">Optional CDL generator for tracking jump/call targets.</param>
	/// <param name="listingGenerator">Optional listing generator for source map tracking.</param>
	public CodeGenerator(SemanticAnalyzer analyzer, TargetArchitecture target = TargetArchitecture.MOS6502, CdlGenerator? cdlGenerator = null, ListingGenerator? listingGenerator = null) {
		_analyzer = analyzer;
		_target = target;
		_initialTarget = target;
		_profile = TargetResolver.GetProfile(target);
		_cdlGenerator = cdlGenerator;
		_listingGenerator = listingGenerator;
		_errors = [];
		_warnings = [];
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
			// Default to 128K ROM (single bank) if no header builder is configured
			var bank0Size = 131072;  // 128K
			var romBuilder = new AtariLynxRomBuilder(
				bank0Size: bank0Size,
				bank1Size: 0,
				gameName: "Poppy Game");

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

		// Build TurboGrafx-16/PCE ROM if configured (for HuC6280 target only)
		if (_target == TargetArchitecture.HuC6280) {
			var romBuilder = new TurboGrafxRomBuilder();

			// Add all segments to the ROM builder
			foreach (var segment in _segments) {
				romBuilder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}

			return romBuilder.Build();
		}

		// Build WonderSwan ROM if configured (for V30MZ target only)
		if (_target == TargetArchitecture.V30MZ) {
			var romBuilder = new WonderSwanRomBuilder();

			// Add all segments to the ROM builder
			foreach (var segment in _segments) {
				romBuilder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}

			return romBuilder.Build();
		}

		// Build Sega Master System/Game Gear ROM if configured (for Z80 target only)
		if (_target == TargetArchitecture.Z80) {
			var romBuilder = new MasterSystemRomBuilder();

			// Add all segments to the ROM builder
			foreach (var segment in _segments) {
				romBuilder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}

			return romBuilder.Build();
		}

		// Build Sega Genesis/Mega Drive ROM if configured (for M68000 target only)
		if (_target == TargetArchitecture.M68000) {
			var romBuilder = new GenesisRomBuilder();

			// Add all segments to the ROM builder
			foreach (var segment in _segments) {
				romBuilder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}

			return romBuilder.Build();
		}

		// Build GBA ROM if configured (for ARM7TDMI target only)
		if (_target == TargetArchitecture.ARM7TDMI) {
			var headerBuilder = _analyzer.GetGbaHeaderBuilder();
			var header = headerBuilder?.Build() ?? new byte[192];

			// GBA ROM addresses start at $08000000; file offset = address - $08000000
			const uint gbaRomBase = 0x08000000;

			// Determine ROM size from segments
			long maxOffset = header.Length;
			foreach (var segment in _segments) {
				var fileOffset = segment.StartAddress >= gbaRomBase
					? segment.StartAddress - gbaRomBase
					: segment.StartAddress;
				var end = fileOffset + (uint)segment.Data.Count;
				if (end > maxOffset) maxOffset = end;
			}

			var rom = new byte[maxOffset];
			Array.Copy(header, 0, rom, 0, header.Length);

			// Copy segments into ROM
			foreach (var segment in _segments) {
				var fileOffset = segment.StartAddress >= gbaRomBase
					? (int)(segment.StartAddress - gbaRomBase)
					: (int)segment.StartAddress;
				segment.Data.CopyTo(rom, fileOffset);
			}

			return rom;
		}

		// Build SPC file if configured (for SPC700 target only)
		if (_target == TargetArchitecture.SPC700) {
			var spcBuilder = _analyzer.GetSpcFileBuilder() ?? new SpcFileBuilder();

			// Load each segment into SPC700 RAM (64KB address space)
			foreach (var segment in _segments) {
				if (segment.StartAddress <= 0xffff) {
					spcBuilder.SetRamAt((ushort)segment.StartAddress, segment.Data.ToArray());
				}
			}

			// If no explicit entry point was set, use the first segment's address
			if (_segments.Count > 0 && _analyzer.GetSpcFileBuilder() is null) {
				spcBuilder.SetPC((ushort)_segments[0].StartAddress);
			}

			return spcBuilder.Build();
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

		// Track start address for listing/source map
		var instructionStartAddress = _currentAddress;

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

				// Validate memory writes to reserved addresses (platform-specific)
				ValidateMemoryAddress(mnemonic, operandValue.Value, node.Location);
			}
		}

		// Let profile adjust addressing mode (e.g. 65SC02 INC/DEC Implied → Accumulator)
		var adjusted = _profile.AdjustAddressingMode(mnemonic, addressingMode);
		if (adjusted.HasValue) {
			addressingMode = adjusted.Value;
		}

		// V30MZ dedicated encoding path for register operands and extended instructions
		if (_target == TargetArchitecture.V30MZ && TryEmitV30MZInstruction(node, mnemonic, operandValue)) {
			RecordListingEntry(instructionStartAddress, node.Location);
			return null;
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

			// Track JSR/JMP/branch targets for CDL and cross-references
			var instructionAddress = (uint)(_currentAddress - 1); // Before opcode was emitted
			var targetAddr = (uint)operandValue.Value;

			// JSR-type instructions (subroutine calls)
			if (EqualsAnyIgnoreCase(mnemonic, "jsr", "jsl", "call", "bsr")) {
				_cdlGenerator?.RegisterSubroutineEntry(operandValue.Value);
				_crossRefs.Add((instructionAddress, targetAddr, 1)); // Jsr=1
			}
			// JMP-type instructions (unconditional jumps)
			else if (EqualsAnyIgnoreCase(mnemonic, "jmp", "jml")) {
				_cdlGenerator?.RegisterJumpTarget(operandValue.Value);
				_crossRefs.Add((instructionAddress, targetAddr, 2)); // Jmp=2
			}
			// Unconditional relative branches
			else if (EqualsAnyIgnoreCase(mnemonic, "bra", "brl")) {
				_cdlGenerator?.RegisterJumpTarget(operandValue.Value);
				_crossRefs.Add((instructionAddress, targetAddr, 3)); // Branch=3
			}
			// Conditional branch instructions
			else if (IsBranchInstruction(mnemonic)) {
				_cdlGenerator?.RegisterJumpTarget(operandValue.Value);
				_crossRefs.Add((instructionAddress, targetAddr, 3)); // Branch=3
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
				if (mnemonic.Equals("rep", StringComparison.OrdinalIgnoreCase) && operandValue.HasValue) {
					// REP clears flags (sets to 16-bit mode)
					if ((operandValue.Value & 0x20) != 0) _accumulatorIs16Bit = true;  // M flag
					if ((operandValue.Value & 0x10) != 0) _indexIs16Bit = true;        // X flag
				} else if (mnemonic.Equals("sep", StringComparison.OrdinalIgnoreCase) && operandValue.HasValue) {
					// SEP sets flags (sets to 8-bit mode)
					if ((operandValue.Value & 0x20) != 0) _accumulatorIs16Bit = false; // M flag
					if ((operandValue.Value & 0x10) != 0) _indexIs16Bit = false;       // X flag
				}
			}
		}

		RecordListingEntry(instructionStartAddress, node.Location);

		return null;
	}

	/// <summary>
	/// Records a listing entry for source map generation.
	/// </summary>
	private void RecordListingEntry(long startAddress, SourceLocation location) {
		if (_listingGenerator is not null && _currentSegment is not null) {
			var byteCount = (int)(_currentAddress - startAddress);
			if (byteCount > 0) {
				var segmentOffset = _currentSegment.Data.Count - byteCount;
				var bytes = _currentSegment.Data.Skip(segmentOffset).Take(byteCount).ToArray();
				_listingGenerator.AddEntry(startAddress, bytes, location);
			}
		}
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

			// 65SC02: Indirect with zero-page operand should be ZeroPageIndirect
			AddressingMode.Indirect when isZeroPage
				&& TryGetInstructionEncoding(mnemonic, AddressingMode.ZeroPageIndirect, out _)
				=> AddressingMode.ZeroPageIndirect,

			// 65SC02: IndexedIndirect with absolute address should be AbsoluteIndexedIndirect
			// The parser uses IndexedIndirect for all (addr,x) syntax, but 65SC02 has
			// a separate AbsoluteIndexedIndirect mode for JMP (abs,x)
			AddressingMode.IndexedIndirect when !isZeroPage
				&& TryGetInstructionEncoding(mnemonic, AddressingMode.AbsoluteIndexedIndirect, out _)
				=> AddressingMode.AbsoluteIndexedIndirect,

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

			// Platform switching directive
			case "platform":
				HandlePlatformDirective(node);
				break;

			case "bank":
				HandleBankDirective(node);
				break;

			case "banksize":
				HandleBanksizeDirective(node);
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

			// If banking is active, compute ROM offset for this segment
			if (_currentBank >= 0 && _bankRomOffset >= 0) {
				var offsetInBank = _bankCpuBase >= 0
					? _currentAddress - _bankCpuBase
					: 0L;
				_currentSegment.RomOffset = _bankRomOffset + offsetInBank;
				_currentSegment.Bank = _currentBank;
			}

			_segments.Add(_currentSegment);
		}
	}

	/// <summary>
	/// Handles .bank N directive to set the current bank for ROM placement.
	/// </summary>
	private void HandleBankDirective(DirectiveNode node) {
		if (node.Arguments.Count < 1) {
			_errors.Add(new CodeError(
				".bank directive requires a bank number",
				node.Location));
			return;
		}

		var value = _analyzer.EvaluateExpression(node.Arguments[0]);
		if (!value.HasValue) {
			_errors.Add(new CodeError(
				"Cannot evaluate .bank argument",
				node.Location));
			return;
		}

		var bankNumber = (int)value.Value;
		if (bankNumber < 0) {
			_errors.Add(new CodeError(
				$"Bank number cannot be negative: {bankNumber}",
				node.Location));
			return;
		}

		_currentBank = bankNumber;

		// Auto-detect bank size if not explicitly set
		if (_bankSize == 0) {
			_bankSize = GetDefaultBankSize();
		}

		_bankRomOffset = (long)bankNumber * _bankSize;
		_bankCpuBase = GetBankCpuBase();

		// Create a new segment at the bank's ROM offset
		_currentAddress = _bankCpuBase >= 0 ? _bankCpuBase : _bankRomOffset;
		_currentSegment = new OutputSegment(_currentAddress) {
			RomOffset = _bankRomOffset,
			Bank = _currentBank
		};
		_segments.Add(_currentSegment);
	}

	/// <summary>
	/// Handles .banksize N directive to set the bank size in bytes.
	/// </summary>
	private void HandleBanksizeDirective(DirectiveNode node) {
		if (node.Arguments.Count < 1) {
			_errors.Add(new CodeError(
				".banksize directive requires a size argument",
				node.Location));
			return;
		}

		var value = _analyzer.EvaluateExpression(node.Arguments[0]);
		if (value.HasValue && value.Value > 0) {
			_bankSize = (int)value.Value;
		} else {
			_errors.Add(new CodeError(
				".banksize must be a positive integer",
				node.Location));
		}
	}

	/// <summary>
	/// Gets the default bank size for the current architecture.
	/// </summary>
	private int GetDefaultBankSize() {
		// SNES bank size depends on map mode (runtime state)
		if (_target == TargetArchitecture.WDC65816) {
			return (GetSnesMapMode() == SnesMapMode.HiRom) ? 0x10000 : 0x8000;
		}
		return _profile.DefaultBankSize;
	}

	/// <summary>
	/// Gets the CPU base address for the banked window.
	/// </summary>
	private long GetBankCpuBase() {
		return _profile.GetBankCpuBase(_currentBank);
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

		var bytes = _profile.LongDirectiveSize;

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

	/// <summary>
	/// Handles .platform directive for inline platform/architecture switching.
	/// </summary>
	/// <remarks>
	/// Allows changing the target architecture mid-source for multi-CPU systems
	/// or testing different instruction sets. Example: .platform "lynx"
	/// </remarks>
	private void HandlePlatformDirective(DirectiveNode node) {
		if (node.Arguments.Count < 1) {
			_errors.Add(new CodeError(
				".platform directive requires an architecture (nes, snes, gb, lynx, genesis, sms, ws, gba, spc700, tg16, channelf)",
				node.Location));
			return;
		}

		// Get the platform name from the argument
		string? platformName = node.Arguments[0] switch {
			IdentifierNode id => id.Name,
			StringLiteralNode str => str.Value,
			_ => null
		};

		if (platformName is null) {
			_errors.Add(new CodeError(
				".platform directive requires an identifier or string",
				node.Location));
			return;
		}

		var target = TargetResolver.Resolve(platformName);

		if (target is null) {
			_errors.Add(new CodeError(
				$"Unknown platform: {platformName}",
				node.Location));
			return;
		}

		_target = target.Value;
		_profile = TargetResolver.GetProfile(_target);

		// Emit a comment in verbose mode for debugging
		// (platform changes don't generate code, they change instruction encoding)
	}

	// ========================================================================
	// Helper Methods
	// ========================================================================

	/// <summary>
	/// Validates memory writes to reserved addresses for the current platform.
	/// </summary>
	/// <remarks>
	/// For Atari Lynx (65SC02), validates writes to hardware registers and Boot ROM:
	/// - $fc00-$fcff: Suzy hardware registers (warning)
	/// - $fd00-$fdff: Mikey hardware registers (warning)
	/// - $fe00-$ffff: Boot ROM (error - cannot write to ROM)
	/// </remarks>
	/// <param name="mnemonic">The instruction mnemonic.</param>
	/// <param name="address">The target memory address.</param>
	/// <param name="location">The source location for error/warning reporting.</param>
	private void ValidateMemoryAddress(string mnemonic, long address, SourceLocation location) {
		// Only validate for Lynx platform
		if (_target != TargetArchitecture.MOS65SC02) return;

		// Check if this is a memory-writing instruction
		var isStoreInstruction = EqualsAnyIgnoreCase(mnemonic,
			"sta", "stx", "sty", "stz",
			"inc", "dec", "asl", "lsr", "rol", "ror",
			"tsb", "trb",
			"rmb0", "rmb1", "rmb2", "rmb3",
			"rmb4", "rmb5", "rmb6", "rmb7",
			"smb0", "smb1", "smb2", "smb3",
			"smb4", "smb5", "smb6", "smb7");

		if (!isStoreInstruction) return;

		// Lynx memory map validation
		// $0000-$fbff: RAM (64KB - 1KB reserved)
		// $fc00-$fcff: Suzy hardware registers
		// $fd00-$fdff: Mikey hardware registers
		// $fe00-$ffff: Boot ROM (512 bytes)

		if (address is >= 0xfe00 and <= 0xffff) {
			// Boot ROM - cannot write to ROM
			_errors.Add(new CodeError(
				$"Cannot write to Lynx Boot ROM at ${address:x4}",
				location));
		} else if (address is >= 0xfd00 and <= 0xfdff) {
			// Mikey hardware registers
			_warnings.Add(new CodeWarning(
				$"Writing to Lynx Mikey hardware register at ${address:x4}",
				location));
		} else if (address is >= 0xfc00 and <= 0xfcff) {
			// Suzy hardware registers
			_warnings.Add(new CodeWarning(
				$"Writing to Lynx Suzy hardware register at ${address:x4}",
				location));
		}
	}

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
	private int GetOperandSize(string mnemonic, AddressingMode mode, EncodedInstruction encoding) {
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
	private bool TryGetInstructionEncoding(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
		// Delegate to architecture profile's encoder
		return _profile.Encoder.TryEncode(mnemonic, mode, out encoding);
	}

	/// <summary>
	/// Checks if an instruction is a branch instruction.
	/// </summary>
	private bool IsBranchInstruction(string mnemonic) {
		return _profile.Encoder.IsBranchInstruction(mnemonic);
	}

	/// <summary>
	/// Tries to emit a V30MZ instruction using the dedicated encoding path.
	/// Handles register operands, near jumps/calls, INT, and other instructions
	/// that cannot be expressed through the generic 6502-like pipeline.
	/// </summary>
	/// <param name="node">The instruction AST node.</param>
	/// <param name="mnemonic">The instruction mnemonic (already stripped of size suffix).</param>
	/// <param name="operandValue">The evaluated operand value (null for register operands).</param>
	/// <returns>True if the instruction was handled; false to fall through to the generic pipeline.</returns>
	private bool TryEmitV30MZInstruction(InstructionNode node, string mnemonic, long? operandValue) {
		var lower = mnemonic.ToLowerInvariant();

		// === Multi-byte implied instructions (must be handled here, not in generic pipeline) ===
		if (node.Operand is null && InstructionSetV30MZ.TryGetImpliedOpcode(lower, out var impliedBytes) && impliedBytes.Length > 1) {
			foreach (var b in impliedBytes) {
				EmitByte(b);
			}
			return true;
		}

		// === Register operand instructions ===
		if (node.Operand is IdentifierNode idNode &&
			InstructionSetV30MZ.TryGetRegister(idNode.Name, out var regEnc, out var isWord)) {
			var isSeg = InstructionSetV30MZ.IsSegmentRegister(idNode.Name);

			switch (lower) {
				case "push":
					if (isSeg) {
						// PUSH segment: ES=$06, CS=$0e, SS=$16, DS=$1e
						EmitByte((byte)(0x06 + regEnc * 8));
					} else if (isWord) {
						// PUSH r16: $50+rw
						EmitByte((byte)(0x50 + regEnc));
					} else {
						return false; // 8-bit register push not supported
					}
					return true;

				case "pop":
					if (isSeg) {
						if (regEnc == InstructionSetV30MZ.Registers.CS) {
							_errors.Add(new CodeError("POP CS is not a valid instruction", node.Location));
							return true;
						}
						// POP segment: ES=$07, SS=$17, DS=$1f
						EmitByte((byte)(0x07 + regEnc * 8));
					} else if (isWord) {
						// POP r16: $58+rw
						EmitByte((byte)(0x58 + regEnc));
					} else {
						return false; // 8-bit register pop not supported
					}
					return true;

				case "inc":
					if (isWord && !isSeg) {
						// INC r16: $40+rw
						EmitByte((byte)(0x40 + regEnc));
						return true;
					}
					return false;

				case "dec":
					if (isWord && !isSeg) {
						// DEC r16: $48+rw
						EmitByte((byte)(0x48 + regEnc));
						return true;
					}
					return false;

				case "xchg":
					if (isWord && !isSeg && regEnc != 0) {
						// XCHG AX, r16: $90+rw (implicit AX as first operand)
						EmitByte((byte)(0x90 + regEnc));
						return true;
					}
					return false;
			}
		}

		// === Numeric operand instructions ===
		if (operandValue.HasValue) {
			var value = operandValue.Value;

			switch (lower) {
				case "int":
					// INT n: $cd + imm8
					EmitByte(0xcd);
					EmitByte((byte)(value & 0xff));
					return true;

				case "jmp":
					return EmitV30MZNearJump(value, node.Location);

				case "call":
					return EmitV30MZNearCall(value, node.Location);

				case "ret":
					// RET imm16: $c2 + imm16 (near return with stack adjustment)
					EmitByte(0xc2);
					EmitWord((ushort)(value & 0xffff));
					return true;

				case "retf":
					// RETF imm16: $ca + imm16 (far return with stack adjustment)
					EmitByte(0xca);
					EmitWord((ushort)(value & 0xffff));
					return true;

				case "push":
					if (node.AddressingMode == AddressingMode.Immediate) {
						if (value >= -128 && value <= 127) {
							// PUSH imm8: $6a + imm8 (80186+)
							EmitByte(0x6a);
							EmitByte((byte)(value & 0xff));
						} else {
							// PUSH imm16: $68 + imm16 (80186+)
							EmitByte(0x68);
							EmitWord((ushort)(value & 0xffff));
						}
						return true;
					}
					return false;
			}
		}

		return false; // Fall through to generic pipeline
	}

	/// <summary>
	/// Emits a V30MZ near JMP instruction ($e9 + rel16).
	/// </summary>
	private bool EmitV30MZNearJump(long targetAddress, SourceLocation location) {
		var instructionAddress = (uint)_currentAddress;
		EmitByte(0xe9);
		// Near relative offset is from the address AFTER the 3-byte instruction
		var nextInstruction = _currentAddress + 2;
		var offset = targetAddress - nextInstruction;
		if (offset < -32768 || offset > 32767) {
			_errors.Add(new CodeError(
				$"Near jump target out of range ({offset} bytes, must be -32768 to +32767)",
				location));
		}
		EmitWord((ushort)(offset & 0xffff));
		_cdlGenerator?.RegisterJumpTarget(targetAddress);
		_crossRefs.Add((instructionAddress, (uint)targetAddress, 2)); // Jmp=2
		return true;
	}

	/// <summary>
	/// Emits a V30MZ near CALL instruction ($e8 + rel16).
	/// </summary>
	private bool EmitV30MZNearCall(long targetAddress, SourceLocation location) {
		var instructionAddress = (uint)_currentAddress;
		EmitByte(0xe8);
		// Near relative offset is from the address AFTER the 3-byte instruction
		var nextInstruction = _currentAddress + 2;
		var offset = targetAddress - nextInstruction;
		if (offset < -32768 || offset > 32767) {
			_errors.Add(new CodeError(
				$"Near call target out of range ({offset} bytes, must be -32768 to +32767)",
				location));
		}
		EmitWord((ushort)(offset & 0xffff));
		_cdlGenerator?.RegisterSubroutineEntry(targetAddress);
		_crossRefs.Add((instructionAddress, (uint)targetAddress, 1)); // Jsr=1
		return true;
	}

	/// <summary>
	/// Gets the SNES memory mapping mode from the analyzer.
	/// </summary>
	private SnesMapMode GetSnesMapMode() {
		var mapping = _analyzer.MemoryMapping;
		if (mapping is null)
			return SnesMapMode.LoRom;
		if (mapping.Equals("lorom", StringComparison.OrdinalIgnoreCase))
			return SnesMapMode.LoRom;
		if (mapping.Equals("hirom", StringComparison.OrdinalIgnoreCase))
			return SnesMapMode.HiRom;
		if (mapping.Equals("exhirom", StringComparison.OrdinalIgnoreCase))
			return SnesMapMode.ExHiRom;
		return SnesMapMode.LoRom;
	}

	/// <summary>
	/// Flattens all segments into a single byte array.
	/// </summary>
	private byte[] FlattenSegments() {
		if (_segments.Count == 0) {
			return [];
		}

		// Check if any segments use bank-based ROM offsets
		bool hasBankedSegments = _segments.Any(s => s.RomOffset.HasValue);

		if (hasBankedSegments) {
			return FlattenBankedSegments();
		}

		// Unbanked: use CPU addresses directly (original behavior)
		var minAddress = _segments.Min(s => s.StartAddress);
		var maxAddress = _segments.Max(s => s.StartAddress + s.Data.Count);

		var output = new byte[maxAddress - minAddress];

		foreach (var segment in _segments) {
			var offset = segment.StartAddress - minAddress;
			for (int i = 0; i < segment.Data.Count; i++) {
				output[offset + i] = segment.Data[i];
			}
		}

		return output;
	}

	/// <summary>
	/// Flattens segments using ROM offsets from bank directives.
	/// </summary>
	private byte[] FlattenBankedSegments() {
		// Compute total ROM size needed
		long maxRomEnd = 0;
		long maxCpuEnd = 0;

		foreach (var segment in _segments) {
			if (segment.RomOffset.HasValue) {
				var end = segment.RomOffset.Value + segment.Data.Count;
				if (end > maxRomEnd) maxRomEnd = end;
			} else {
				var end = segment.StartAddress + segment.Data.Count;
				if (end > maxCpuEnd) maxCpuEnd = end;
			}
		}

		var romSize = Math.Max(maxRomEnd, maxCpuEnd);
		var output = new byte[romSize];

		foreach (var segment in _segments) {
			long offset;
			if (segment.RomOffset.HasValue) {
				offset = segment.RomOffset.Value;
			} else {
				// Unbanked segments use their CPU address as ROM offset
				offset = segment.StartAddress;
			}

			for (int i = 0; i < segment.Data.Count; i++) {
				var pos = offset + i;
				if (pos >= 0 && pos < romSize) {
					output[pos] = segment.Data[i];
				}
			}
		}

		return output;
	}

	private static bool EqualsAnyIgnoreCase(string value, params ReadOnlySpan<string> candidates) {
		foreach (var candidate in candidates) {
			if (value.Equals(candidate, StringComparison.OrdinalIgnoreCase))
				return true;
		}
		return false;
	}
}

/// <summary>
/// Represents an output segment with a start address and data.
/// </summary>
public sealed class OutputSegment {
	/// <summary>
	/// The CPU starting address of this segment (for label resolution).
	/// </summary>
	public long StartAddress { get; }

	/// <summary>
	/// The ROM file offset for this segment. When set, FlattenSegments uses this
	/// instead of StartAddress for placement. Used by .bank directive.
	/// </summary>
	public long? RomOffset { get; set; }

	/// <summary>
	/// The bank number this segment belongs to, or -1 if unbanked.
	/// </summary>
	public int Bank { get; set; } = -1;

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

/// <summary>
/// Represents a code generation warning.
/// </summary>
public sealed class CodeWarning {
	/// <summary>
	/// The warning message.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// The source location where the warning occurred.
	/// </summary>
	public SourceLocation Location { get; }

	/// <summary>
	/// Creates a new code warning.
	/// </summary>
	/// <param name="message">The warning message.</param>
	/// <param name="location">The source location.</param>
	public CodeWarning(string message, SourceLocation location) {
		Message = message;
		Location = location;
	}

	/// <inheritdoc />
	public override string ToString() => $"{Location}: warning: {Message}";
}

