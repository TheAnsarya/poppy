namespace Poppy.Arch.V30MZ;

using System.Collections.Frozen;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// NEC V30MZ target profile (WonderSwan).
/// </summary>
internal sealed class V30mzProfile : ITargetProfile {
	public static readonly V30mzProfile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.V30MZ;
	public IInstructionEncoder Encoder { get; } = new V30mzEncoder();
	public int DefaultBankSize => 0x4000; // Default
	public long GetBankCpuBase(int bank) => -1;

	/// <inheritdoc />
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new V30mzRomBuilderAdapter();

	private sealed class V30mzRomBuilderAdapter : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			var builder = new WonderSwanRomBuilder();
			foreach (var segment in segments) {
				builder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}
			return builder.Build();
		}
	}

	private sealed class V30mzEncoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSetV30MZ.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSetV30MZ.TryGetEncodingFromShared(mnemonic, mode, out var opcode, out var size)) {
				encoding = new EncodedInstruction(opcode, size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetV30MZ.IsBranchInstruction(mnemonic);

		public bool IsRegister(string name) =>
			InstructionSetV30MZ.IsRegister(name);

		public bool IsSegmentRegister(string name) =>
			InstructionSetV30MZ.IsSegmentRegister(name);

		public int GetSpecialInstructionSize(string mnemonic, string? operandIdentifier, bool hasOperand, char? sizeSuffix) {
			return GetSpecialInstructionSize(mnemonic, operandIdentifier, hasOperand, sizeSuffix, null);
		}

		public int GetSpecialInstructionSize(string mnemonic, string? operandIdentifier, bool hasOperand, char? sizeSuffix,
			IReadOnlyList<ResolvedOperand>? additionalOperands) {
			var lower = mnemonic.ToLowerInvariant();

			// Conditional jumps: opcode + rel8 = 2 bytes
			if (InstructionSetV30MZ.TryGetConditionalJump(lower, out _))
				return 2;

			// Loop instructions: opcode + rel8 = 2 bytes
			if (InstructionSetV30MZ.TryGetLoopInstruction(lower, out _))
				return 2;

			// Check if operand is a register/segment identifier
			bool operandIsRegister = operandIdentifier is not null && InstructionSetV30MZ.IsRegister(operandIdentifier);
			bool operandIsSegment = operandIdentifier is not null && InstructionSetV30MZ.IsSegmentRegister(operandIdentifier);

			// Multi-byte implied: AAM ($d4 $0a), AAD ($d5 $0a) = 2 bytes
			if (lower is "aam" or "aad")
				return 2;

			// === Two-operand ModR/M sizing ===
			if (operandIsRegister && !operandIsSegment && additionalOperands is { Count: > 0 }) {
				InstructionSetV30MZ.TryGetRegister(operandIdentifier!, out _, out var destIsWord);
				var src = additionalOperands[0];
				bool srcIsRegister = src.Identifier is not null && InstructionSetV30MZ.IsRegister(src.Identifier);

				// ALU reg, reg/imm
				if (InstructionSetV30MZ.TryGetAluOp(lower, out _, out _)) {
					if (srcIsRegister) {
						return 2; // opcode + ModR/M
					}
					// ALU reg, imm — check accumulator shortcut
					if (operandIdentifier!.Equals("al", StringComparison.OrdinalIgnoreCase) ||
						operandIdentifier.Equals("ax", StringComparison.OrdinalIgnoreCase)) {
						return destIsWord ? 3 : 2; // opcode + imm16/imm8
					}
					// General reg, imm16: opcode + ModR/M + imm16 (or imm8 if fits sign-extended)
					if (destIsWord) {
						if (src.Value.HasValue && src.Value.Value >= -128 && src.Value.Value <= 127) {
							return 3; // 0x83 + ModR/M + simm8
						}
						return 4; // 0x81 + ModR/M + imm16
					}
					return 3; // 0x80 + ModR/M + imm8
				}

				// MOV reg, reg/imm
				if (lower == "mov") {
					if (srcIsRegister) {
						return 2; // opcode + ModR/M
					}
					// MOV reg, imm — short form: $b0+rb / $b8+rw
					return destIsWord ? 3 : 2; // opcode + imm16/imm8
				}

				// TEST reg, reg/imm
				if (lower == "test") {
					if (srcIsRegister) {
						return 2; // opcode + ModR/M
					}
					// TEST acc, imm shortcut
					if (operandIdentifier!.Equals("al", StringComparison.OrdinalIgnoreCase) ||
						operandIdentifier.Equals("ax", StringComparison.OrdinalIgnoreCase)) {
						return destIsWord ? 3 : 2;
					}
					return destIsWord ? 4 : 3; // 0xf6/f7 + ModR/M + imm
				}

				// XCHG reg, reg (two-operand form)
				if (lower == "xchg") {
					return 2; // opcode + ModR/M
				}

				// Shift/rotate reg, imm1/CL
				if (InstructionSetV30MZ.TryGetShiftOp(lower, out _)) {
					return 2; // opcode + ModR/M
				}
			}

			// === Single-operand register instructions ===
			if (operandIsRegister || operandIsSegment) {
				// Unary operations: NOT, NEG, MUL, IMUL, DIV, IDIV
				if (InstructionSetV30MZ.TryGetUnaryOp(lower, out _)) {
					return 2; // opcode + ModR/M
				}

				if (lower is "push" or "pop" or "inc" or "dec" or "xchg")
					return 1;
			}

			// INT n = 2 bytes (opcode + imm8)
			if (lower == "int")
				return 2;

			// Near JMP = 3 bytes (opcode + rel16), near CALL = 3 bytes (opcode + rel16)
			if (lower is "jmp" or "call")
				return 3;

			// RET/RETF with operand = 3 bytes (opcode + imm16)
			if (lower is "ret" or "retf" && hasOperand)
				return 3;

			// PUSH immediate: 2 bytes (push imm8 $6a) or 3 bytes (push imm16 $68)
			if (lower == "push" && hasOperand && !operandIsRegister && !operandIsSegment) {
				if (sizeSuffix == 'b')
					return 2;
				return 3;
			}

			// Fall through to generic sizing
			return 0;
		}

		public bool TryEmitSpecialInstruction(SpecialInstructionContext context, ICodeEmitter emitter) {
			var lower = context.Mnemonic.ToLowerInvariant();

			// === Multi-byte implied instructions ===
			if (context.OperandIdentifier is null && !context.OperandValue.HasValue &&
				InstructionSetV30MZ.TryGetImpliedOpcode(lower, out var impliedBytes) && impliedBytes.Length > 1) {
				foreach (var b in impliedBytes) {
					emitter.EmitByte(b);
				}
				return true;
			}

			// === Register operand instructions ===
			if (context.OperandIdentifier is not null &&
				InstructionSetV30MZ.TryGetRegister(context.OperandIdentifier, out var regEnc, out var isWord)) {
				var isSeg = InstructionSetV30MZ.IsSegmentRegister(context.OperandIdentifier);

				// === Two-operand ModR/M instructions ===
				if (!isSeg && context.AdditionalOperands is { Count: > 0 }) {
					var src = context.AdditionalOperands[0];
					int srcRegEnc = 0;
					bool srcIsWord = false;
					bool srcIsReg = src.Identifier is not null &&
						InstructionSetV30MZ.TryGetRegister(src.Identifier, out srcRegEnc, out srcIsWord);

					// ALU reg, reg (e.g., add ax, bx)
					if (srcIsReg && InstructionSetV30MZ.TryGetAluOp(lower, out var aluBase, out _)) {
						var opcode = (byte)(aluBase + (isWord ? 1 : 0)); // r/m, r form
						emitter.EmitByte(opcode);
						emitter.EmitByte(InstructionSetV30MZ.EncodeModRM(3, srcRegEnc, regEnc));
						return true;
					}

					// ALU reg, imm (e.g., add ax, #$1234)
					if (!srcIsReg && src.Value.HasValue && InstructionSetV30MZ.TryGetAluOp(lower, out aluBase, out var aluDigit)) {
						return EmitAluImmediate(regEnc, isWord, aluBase, aluDigit, src.Value.Value, context.OperandIdentifier, emitter);
					}

					// MOV reg, reg (e.g., mov ax, bx)
					if (srcIsReg && lower == "mov") {
						var opcode = (byte)(isWord ? 0x89 : 0x88); // r/m, r form
						emitter.EmitByte(opcode);
						emitter.EmitByte(InstructionSetV30MZ.EncodeModRM(3, srcRegEnc, regEnc));
						return true;
					}

					// MOV reg, imm (e.g., mov ax, #$1234)
					if (!srcIsReg && src.Value.HasValue && lower == "mov") {
						if (isWord) {
							// MOV r16, imm16: $b8+rw
							emitter.EmitByte((byte)(0xb8 + regEnc));
							emitter.EmitWord((ushort)(src.Value.Value & 0xffff));
						} else {
							// MOV r8, imm8: $b0+rb
							emitter.EmitByte((byte)(0xb0 + regEnc));
							emitter.EmitByte((byte)(src.Value.Value & 0xff));
						}
						return true;
					}

					// TEST reg, reg (e.g., test ax, bx)
					if (srcIsReg && lower == "test") {
						var opcode = (byte)(isWord ? 0x85 : 0x84);
						emitter.EmitByte(opcode);
						emitter.EmitByte(InstructionSetV30MZ.EncodeModRM(3, srcRegEnc, regEnc));
						return true;
					}

					// TEST reg, imm (e.g., test al, #$ff)
					if (!srcIsReg && src.Value.HasValue && lower == "test") {
						return EmitTestImmediate(regEnc, isWord, src.Value.Value, context.OperandIdentifier, emitter);
					}

					// XCHG reg, reg (two-operand form, e.g., xchg bx, cx)
					if (srcIsReg && lower == "xchg") {
						// If either operand is AX, use short form $90+rw
						if (isWord && srcIsWord) {
							if (regEnc == InstructionSetV30MZ.Registers.AX) {
								emitter.EmitByte((byte)(0x90 + srcRegEnc));
								return true;
							}
							if (srcRegEnc == InstructionSetV30MZ.Registers.AX) {
								emitter.EmitByte((byte)(0x90 + regEnc));
								return true;
							}
						}
						// General form: 0x86 (8-bit) / 0x87 (16-bit) + ModR/M
						var opcode = (byte)(isWord ? 0x87 : 0x86);
						emitter.EmitByte(opcode);
						emitter.EmitByte(InstructionSetV30MZ.EncodeModRM(3, srcRegEnc, regEnc));
						return true;
					}

					// Shift/rotate reg, 1 or CL (e.g., shl ax, #$01 or shl ax, cl)
					if (InstructionSetV30MZ.TryGetShiftOp(lower, out var shiftDigit)) {
						bool shiftByCl = src.Identifier is not null &&
							src.Identifier.Equals("cl", StringComparison.OrdinalIgnoreCase);
						bool shiftByOne = src.Value.HasValue && src.Value.Value == 1;

						if (shiftByCl) {
							// 0xd2 (8-bit) / 0xd3 (16-bit) + ModR/M
							emitter.EmitByte((byte)(isWord ? 0xd3 : 0xd2));
							emitter.EmitByte(InstructionSetV30MZ.EncodeModRM(3, shiftDigit, regEnc));
							return true;
						}
						if (shiftByOne) {
							// 0xd0 (8-bit) / 0xd1 (16-bit) + ModR/M
							emitter.EmitByte((byte)(isWord ? 0xd1 : 0xd0));
							emitter.EmitByte(InstructionSetV30MZ.EncodeModRM(3, shiftDigit, regEnc));
							return true;
						}
						emitter.ReportError($"Shift count must be 1 or CL on 8086/V30MZ", context.Location);
						return true;
					}
				}

				// === Unary ModR/M instructions (single register operand) ===
				if (!isSeg && InstructionSetV30MZ.TryGetUnaryOp(lower, out var unaryDigit)) {
					emitter.EmitByte((byte)(isWord ? 0xf7 : 0xf6));
					emitter.EmitByte(InstructionSetV30MZ.EncodeModRM(3, unaryDigit, regEnc));
					return true;
				}

				switch (lower) {
					case "push":
						if (isSeg) {
							// PUSH segment: ES=$06, CS=$0e, SS=$16, DS=$1e
							emitter.EmitByte((byte)(0x06 + regEnc * 8));
						} else if (isWord) {
							// PUSH r16: $50+rw
							emitter.EmitByte((byte)(0x50 + regEnc));
						} else {
							return false; // 8-bit register push not supported
						}
						return true;

					case "pop":
						if (isSeg) {
							if (regEnc == InstructionSetV30MZ.Registers.CS) {
								emitter.ReportError("POP CS is not a valid instruction", context.Location);
								return true;
							}
							// POP segment: ES=$07, SS=$17, DS=$1f
							emitter.EmitByte((byte)(0x07 + regEnc * 8));
						} else if (isWord) {
							// POP r16: $58+rw
							emitter.EmitByte((byte)(0x58 + regEnc));
						} else {
							return false; // 8-bit register pop not supported
						}
						return true;

					case "inc":
						if (isWord && !isSeg) {
							// INC r16: $40+rw
							emitter.EmitByte((byte)(0x40 + regEnc));
							return true;
						}
						return false;

					case "dec":
						if (isWord && !isSeg) {
							// DEC r16: $48+rw
							emitter.EmitByte((byte)(0x48 + regEnc));
							return true;
						}
						return false;

					case "xchg":
						if (isWord && !isSeg && regEnc != 0) {
							// XCHG AX, r16: $90+rw (implicit AX as first operand)
							emitter.EmitByte((byte)(0x90 + regEnc));
							return true;
						}
						return false;
				}
			}

			// === Numeric operand instructions ===
			if (context.OperandValue.HasValue) {
				var value = context.OperandValue.Value;

				switch (lower) {
					case "int":
						// INT n: $cd + imm8
						emitter.EmitByte(0xcd);
						emitter.EmitByte((byte)(value & 0xff));
						return true;

					case "jmp":
						return EmitNearJump(value, context.Location, emitter);

					case "call":
						return EmitNearCall(value, context.Location, emitter);

					case "ret":
						// RET imm16: $c2 + imm16
						emitter.EmitByte(0xc2);
						emitter.EmitWord((ushort)(value & 0xffff));
						return true;

					case "retf":
						// RETF imm16: $ca + imm16
						emitter.EmitByte(0xca);
						emitter.EmitWord((ushort)(value & 0xffff));
						return true;

					case "push":
						if (context.AddressingMode == AddressingMode.Immediate) {
							if (value >= -128 && value <= 127) {
								// PUSH imm8: $6a + imm8 (80186+)
								emitter.EmitByte(0x6a);
								emitter.EmitByte((byte)(value & 0xff));
							} else {
								// PUSH imm16: $68 + imm16 (80186+)
								emitter.EmitByte(0x68);
								emitter.EmitWord((ushort)(value & 0xffff));
							}
							return true;
						}
						return false;
				}
			}

			return false; // Fall through to generic pipeline
		}

		/// <summary>
		/// Emits an ALU reg, immediate instruction with accumulator shortcut optimization.
		/// </summary>
		private static bool EmitAluImmediate(int regEnc, bool isWord, byte aluBase, int aluDigit, long value, string regName, ICodeEmitter emitter) {
			// Accumulator shortcuts: ADD AL,imm8 / ADD AX,imm16 (no ModR/M)
			if (regName.Equals("al", StringComparison.OrdinalIgnoreCase)) {
				emitter.EmitByte((byte)(aluBase + 4)); // AL, imm8
				emitter.EmitByte((byte)(value & 0xff));
				return true;
			}
			if (regName.Equals("ax", StringComparison.OrdinalIgnoreCase)) {
				emitter.EmitByte((byte)(aluBase + 5)); // AX, imm16
				emitter.EmitWord((ushort)(value & 0xffff));
				return true;
			}

			// General immediate-to-register forms
			if (isWord) {
				// Sign-extended imm8 optimization: 0x83 /digit + ModR/M + simm8
				if (value >= -128 && value <= 127) {
					emitter.EmitByte(0x83);
					emitter.EmitByte(InstructionSetV30MZ.EncodeModRM(3, aluDigit, regEnc));
					emitter.EmitByte((byte)(value & 0xff));
				} else {
					// Full imm16: 0x81 /digit + ModR/M + imm16
					emitter.EmitByte(0x81);
					emitter.EmitByte(InstructionSetV30MZ.EncodeModRM(3, aluDigit, regEnc));
					emitter.EmitWord((ushort)(value & 0xffff));
				}
			} else {
				// 8-bit register, imm8: 0x80 /digit + ModR/M + imm8
				emitter.EmitByte(0x80);
				emitter.EmitByte(InstructionSetV30MZ.EncodeModRM(3, aluDigit, regEnc));
				emitter.EmitByte((byte)(value & 0xff));
			}
			return true;
		}

		/// <summary>
		/// Emits a TEST reg, immediate instruction with accumulator shortcut.
		/// </summary>
		private static bool EmitTestImmediate(int regEnc, bool isWord, long value, string regName, ICodeEmitter emitter) {
			// Accumulator shortcuts: TEST AL,imm8 ($a8) / TEST AX,imm16 ($a9)
			if (regName.Equals("al", StringComparison.OrdinalIgnoreCase)) {
				emitter.EmitByte(0xa8);
				emitter.EmitByte((byte)(value & 0xff));
				return true;
			}
			if (regName.Equals("ax", StringComparison.OrdinalIgnoreCase)) {
				emitter.EmitByte(0xa9);
				emitter.EmitWord((ushort)(value & 0xffff));
				return true;
			}

			// General form: 0xf6 /0 (8-bit) or 0xf7 /0 (16-bit) + ModR/M + imm
			emitter.EmitByte((byte)(isWord ? 0xf7 : 0xf6));
			emitter.EmitByte(InstructionSetV30MZ.EncodeModRM(3, 0, regEnc));
			if (isWord) {
				emitter.EmitWord((ushort)(value & 0xffff));
			} else {
				emitter.EmitByte((byte)(value & 0xff));
			}
			return true;
		}

		/// <summary>
		/// Emits a V30MZ near JMP instruction ($e9 + rel16).
		/// </summary>
		private static bool EmitNearJump(long targetAddress, SourceLocation location, ICodeEmitter emitter) {
			var instructionAddress = (uint)emitter.CurrentAddress;
			emitter.EmitByte(0xe9);
			// Near relative offset is from the address AFTER the 3-byte instruction
			var nextInstruction = emitter.CurrentAddress + 2;
			var offset = targetAddress - nextInstruction;
			if (offset < -32768 || offset > 32767) {
				emitter.ReportError(
					$"Near jump target out of range ({offset} bytes, must be -32768 to +32767)",
					location);
			}
			emitter.EmitWord((ushort)(offset & 0xffff));
			emitter.RegisterJumpTarget(targetAddress);
			emitter.AddCrossReference(instructionAddress, (uint)targetAddress, 2); // Jmp=2
			return true;
		}

		/// <summary>
		/// Emits a V30MZ near CALL instruction ($e8 + rel16).
		/// </summary>
		private static bool EmitNearCall(long targetAddress, SourceLocation location, ICodeEmitter emitter) {
			var instructionAddress = (uint)emitter.CurrentAddress;
			emitter.EmitByte(0xe8);
			// Near relative offset is from the address AFTER the 3-byte instruction
			var nextInstruction = emitter.CurrentAddress + 2;
			var offset = targetAddress - nextInstruction;
			if (offset < -32768 || offset > 32767) {
				emitter.ReportError(
					$"Near call target out of range ({offset} bytes, must be -32768 to +32767)",
					location);
			}
			emitter.EmitWord((ushort)(offset & 0xffff));
			emitter.RegisterSubroutineEntry(targetAddress);
			emitter.AddCrossReference(instructionAddress, (uint)targetAddress, 1); // Jsr=1
			return true;
		}
	}
}
