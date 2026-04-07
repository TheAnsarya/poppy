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

			// Register-operand instructions: single opcode byte
			if (operandIsRegister || operandIsSegment) {
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
