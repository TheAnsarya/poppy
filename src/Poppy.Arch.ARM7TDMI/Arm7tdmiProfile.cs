namespace Poppy.Arch.ARM7TDMI;

using System.Collections.Frozen;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// ARM7TDMI target profile (Game Boy Advance).
/// </summary>
internal sealed class Arm7tdmiProfile : ITargetProfile {
	public static readonly Arm7tdmiProfile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.ARM7TDMI;
	public IInstructionEncoder Encoder { get; } = new Arm7tdmiEncoder();
	public int DefaultBankSize => 0x4000; // Default
	public long GetBankCpuBase(int bank) => -1;

	/// <inheritdoc />
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new Arm7tdmiRomBuilderAdapter(analyzer);

	/// <inheritdoc />
	public bool TryHandleDirective(DirectiveNode node, SemanticAnalyzer analyzer) {
		var directiveName = node.Name.ToLowerInvariant();

		switch (directiveName) {
			case "gba_title":
			case "gba_game_code":
			case "gba_maker_code":
			case "gba_version":
			case "gba_entry":
				return HandleGbaDirective(node, analyzer, directiveName);

			default:
				return false;
		}
	}

	private static GbaHeaderConfig GetOrCreateConfig(SemanticAnalyzer analyzer) {
		if (analyzer.HeaderConfig is GbaHeaderConfig config) return config;
		var newConfig = new GbaHeaderConfig();
		analyzer.HeaderConfig = newConfig;
		return newConfig;
	}

	private static bool HandleGbaDirective(DirectiveNode node, SemanticAnalyzer analyzer, string directiveName) {
		if (analyzer.Pass != 1) return true;

		long? value = null;
		string? stringValue = null;

		if (node.Arguments.Count > 0) {
			if (node.Arguments[0] is StringLiteralNode stringLit) {
				stringValue = stringLit.Value;
			} else {
				value = analyzer.EvaluateExpression(node.Arguments[0]);
			}
		}

		var config = GetOrCreateConfig(analyzer);

		switch (directiveName) {
			case "gba_title":
				if (stringValue is null) {
					analyzer.AddError(".gba_title directive requires a string value (max 12 characters, uppercase ASCII)", node.Location);
					return true;
				}
				if (stringValue.Length > 12) {
					analyzer.AddError($".gba_title is too long ({stringValue.Length} characters, maximum is 12)", node.Location);
					return true;
				}
				config.Title = stringValue;
				break;

			case "gba_game_code":
				if (stringValue is null) {
					analyzer.AddError(".gba_game_code directive requires a 4-character string (e.g., \"AXVE\")", node.Location);
					return true;
				}
				if (stringValue.Length != 4) {
					analyzer.AddError($".gba_game_code must be exactly 4 characters (got {stringValue.Length})", node.Location);
					return true;
				}
				config.GameCode = stringValue;
				break;

			case "gba_maker_code":
				if (stringValue is null) {
					analyzer.AddError(".gba_maker_code directive requires a 2-character string (e.g., \"01\")", node.Location);
					return true;
				}
				if (stringValue.Length != 2) {
					analyzer.AddError($".gba_maker_code must be exactly 2 characters (got {stringValue.Length})", node.Location);
					return true;
				}
				config.MakerCode = stringValue;
				break;

			case "gba_version":
				if (value is null) {
					analyzer.AddError(".gba_version directive requires a version number (0-255)", node.Location);
					return true;
				}
				if (value < 0 || value > 255) {
					analyzer.AddError($".gba_version must be 0-255 (got {value})", node.Location);
					return true;
				}
				config.Version = (int)value;
				break;

			case "gba_entry":
				if (value is null) {
					analyzer.AddError(".gba_entry directive requires an entry point address", node.Location);
					return true;
				}
				config.EntryPoint = (int)value;
				break;
		}

		return true;
	}

	private sealed class Arm7tdmiRomBuilderAdapter(SemanticAnalyzer analyzer) : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			var headerBuilder = analyzer.GetGbaHeaderBuilder();
			var header = headerBuilder?.Build() ?? new byte[192];

			const uint gbaRomBase = 0x08000000;

			// Determine ROM size from segments
			long maxOffset = header.Length;
			foreach (var segment in segments) {
				var fileOffset = segment.StartAddress >= gbaRomBase
					? segment.StartAddress - gbaRomBase
					: segment.StartAddress;
				var end = fileOffset + (uint)segment.Data.Count;
				if (end > maxOffset) maxOffset = end;
			}

			var rom = new byte[maxOffset];
			Array.Copy(header, 0, rom, 0, header.Length);

			foreach (var segment in segments) {
				var fileOffset = segment.StartAddress >= gbaRomBase
					? (int)(segment.StartAddress - gbaRomBase)
					: (int)segment.StartAddress;
				segment.Data.CopyTo(rom, fileOffset);
			}

			return rom;
		}
	}

	private sealed class Arm7tdmiEncoder : IInstructionEncoder {
		private static readonly FrozenDictionary<string, byte> s_conditionSuffixes = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase) {
			["eq"] = InstructionSetARM7TDMI.Conditions.EQ,
			["ne"] = InstructionSetARM7TDMI.Conditions.NE,
			["cs"] = InstructionSetARM7TDMI.Conditions.CS,
			["hs"] = InstructionSetARM7TDMI.Conditions.HS,
			["cc"] = InstructionSetARM7TDMI.Conditions.CC,
			["lo"] = InstructionSetARM7TDMI.Conditions.LO,
			["mi"] = InstructionSetARM7TDMI.Conditions.MI,
			["pl"] = InstructionSetARM7TDMI.Conditions.PL,
			["vs"] = InstructionSetARM7TDMI.Conditions.VS,
			["vc"] = InstructionSetARM7TDMI.Conditions.VC,
			["hi"] = InstructionSetARM7TDMI.Conditions.HI,
			["ls"] = InstructionSetARM7TDMI.Conditions.LS,
			["ge"] = InstructionSetARM7TDMI.Conditions.GE,
			["lt"] = InstructionSetARM7TDMI.Conditions.LT,
			["gt"] = InstructionSetARM7TDMI.Conditions.GT,
			["le"] = InstructionSetARM7TDMI.Conditions.LE,
			["al"] = InstructionSetARM7TDMI.Conditions.AL
		}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

		private static readonly FrozenSet<string> s_specialSupportedMnemonics = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"mov", "movs", "add", "adds", "sub", "subs", "cmp",
			"b", "bl", "bx", "swi", "svc", "nop",
			"ldr", "str", "ldrb", "strb",
			"mul", "muls", "mla", "mlas",
			"umull", "umulls", "smull", "smulls", "umlal", "umlals", "smlal", "smlals"
		}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		private static readonly FrozenSet<string> s_mnemonics = InstructionSetARM7TDMI.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);
		public IReadOnlySet<string> Mnemonics => s_mnemonics;

		public bool IsRegister(string name) =>
			InstructionSetARM7TDMI.TryGetRegister(name, out _);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			encoding = default;
			return false;
		}

		public int GetSpecialInstructionSize(string mnemonic, string? operandIdentifier, bool hasOperand, char? sizeSuffix,
			IReadOnlyList<ResolvedOperand>? additionalOperands) {
			if (!TryNormalizeSpecialMnemonic(mnemonic, out var baseMnemonic, out _)) {
				return 0;
			}

			return baseMnemonic.ToLowerInvariant() switch {
				"mov" or "movs" => hasOperand && additionalOperands is { Count: 1 } ? 4 : 0,
				"add" or "adds" or "sub" or "subs" => hasOperand && additionalOperands is { Count: 2 } ? 4 : 0,
				"cmp" => hasOperand && additionalOperands is { Count: 1 } ? 4 : 0,
				"ldr" or "str" or "ldrb" or "strb" => hasOperand && additionalOperands is not null && (additionalOperands.Count == 1 || additionalOperands.Count == 2) ? 4 : 0,
				"mul" or "muls" => hasOperand && additionalOperands is { Count: 2 } ? 4 : 0,
				"mla" or "mlas" => hasOperand && additionalOperands is { Count: 3 } ? 4 : 0,
				"umull" or "umulls" or "smull" or "smulls" or "umlal" or "umlals" or "smlal" or "smlals" => hasOperand && additionalOperands is { Count: 3 } ? 4 : 0,
				"b" or "bl" => hasOperand ? 4 : 0,
				"bx" => hasOperand ? 4 : 0,
				"swi" or "svc" => hasOperand ? 4 : 0,
				"nop" => 4,
				_ => 0
			};
		}

		public bool TryEmitSpecialInstruction(SpecialInstructionContext context, ICodeEmitter emitter) {
			if (!TryNormalizeSpecialMnemonic(context.Mnemonic, out var baseMnemonic, out var condition)) {
				return false;
			}

			var normalized = baseMnemonic.ToLowerInvariant();
			if (!s_specialSupportedMnemonics.Contains(normalized)) {
				return false;
			}

			return normalized switch {
				"mov" or "movs" => EmitMov(context, emitter, normalized, condition),
				"add" or "adds" => EmitDataProcessingThreeOperand(context, emitter, InstructionSetARM7TDMI.ArmOpcodes.ADD, normalized, condition),
				"sub" or "subs" => EmitDataProcessingThreeOperand(context, emitter, InstructionSetARM7TDMI.ArmOpcodes.SUB, normalized, condition),
				"cmp" => EmitCmp(context, emitter, condition),
				"ldr" => EmitLoadStore(context, emitter, isLoad: true, isByte: false, condition: condition),
				"str" => EmitLoadStore(context, emitter, isLoad: false, isByte: false, condition: condition),
				"ldrb" => EmitLoadStore(context, emitter, isLoad: true, isByte: true, condition: condition),
				"strb" => EmitLoadStore(context, emitter, isLoad: false, isByte: true, condition: condition),
				"mul" or "muls" => EmitMultiply(context, emitter, accumulate: false, setFlags: normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase), condition: condition),
				"mla" or "mlas" => EmitMultiply(context, emitter, accumulate: true, setFlags: normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase), condition: condition),
				"umull" or "umulls" => EmitMultiplyLong(context, emitter, isUnsigned: true, accumulate: false, setFlags: normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase), condition: condition),
				"smull" or "smulls" => EmitMultiplyLong(context, emitter, isUnsigned: false, accumulate: false, setFlags: normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase), condition: condition),
				"umlal" or "umlals" => EmitMultiplyLong(context, emitter, isUnsigned: true, accumulate: true, setFlags: normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase), condition: condition),
				"smlal" or "smlals" => EmitMultiplyLong(context, emitter, isUnsigned: false, accumulate: true, setFlags: normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase), condition: condition),
				"b" => EmitBranch(context, emitter, false, condition),
				"bl" => EmitBranch(context, emitter, true, condition),
				"bx" => EmitBx(context, emitter, condition),
				"swi" or "svc" => EmitSwi(context, emitter, condition),
				"nop" => EmitNop(emitter, condition),
				_ => false
			};
		}

		private static bool EmitMov(SpecialInstructionContext context, ICodeEmitter emitter, string mnemonic, byte condition) {
			if (!TryResolveRegister(context.OperandIdentifier, out var rd, context, emitter)) {
				return true;
			}

			if (context.AdditionalOperands is not { Count: 1 }) {
				emitter.ReportError($"'{context.Mnemonic}' requires two operands", context.Location);
				return true;
			}

			var op2 = context.AdditionalOperands[0];
			var setFlags = mnemonic.EndsWith("s", StringComparison.OrdinalIgnoreCase);

			if (op2.Identifier is not null) {
				if (!TryResolveRegister(op2.Identifier, out var rm, context, emitter)) {
					return true;
				}

				var bytes = InstructionSetARM7TDMI.EncodeDataProcessingRegister(
					InstructionSetARM7TDMI.ArmOpcodes.MOV, rd, 0, rm,
					setFlags: setFlags, condition: condition);
				EmitLong(emitter, bytes);
				return true;
			}

			if (!TryResolveImmediate(op2.Value, out var imm8, out var rotate, context, emitter)) {
				return true;
			}

			var immBytes = InstructionSetARM7TDMI.EncodeDataProcessingImmediate(
				InstructionSetARM7TDMI.ArmOpcodes.MOV, rd, 0, imm8, rotate,
				setFlags: setFlags, condition: condition);
			EmitLong(emitter, immBytes);
			return true;
		}

		private static bool EmitDataProcessingThreeOperand(SpecialInstructionContext context, ICodeEmitter emitter,
			byte opcode, string mnemonic, byte condition) {
			if (!TryResolveRegister(context.OperandIdentifier, out var rd, context, emitter)) {
				return true;
			}

			if (context.AdditionalOperands is not { Count: 2 }) {
				emitter.ReportError($"'{context.Mnemonic}' requires three operands", context.Location);
				return true;
			}

			var rnOperand = context.AdditionalOperands[0];
			if (!TryResolveRegister(rnOperand.Identifier, out var rn, context, emitter)) {
				return true;
			}

			var op2 = context.AdditionalOperands[1];
			var setFlags = mnemonic.EndsWith("s", StringComparison.OrdinalIgnoreCase);

			if (op2.Identifier is not null) {
				if (!TryResolveRegister(op2.Identifier, out var rm, context, emitter)) {
					return true;
				}

				var bytes = InstructionSetARM7TDMI.EncodeDataProcessingRegister(
					opcode, rd, rn, rm, setFlags: setFlags, condition: condition);
				EmitLong(emitter, bytes);
				return true;
			}

			if (!TryResolveImmediate(op2.Value, out var imm8, out var rotate, context, emitter)) {
				return true;
			}

			var immBytes = InstructionSetARM7TDMI.EncodeDataProcessingImmediate(
				opcode, rd, rn, imm8, rotate, setFlags: setFlags, condition: condition);
			EmitLong(emitter, immBytes);
			return true;
		}

		private static bool EmitCmp(SpecialInstructionContext context, ICodeEmitter emitter, byte condition) {
			if (!TryResolveRegister(context.OperandIdentifier, out var rn, context, emitter)) {
				return true;
			}

			if (context.AdditionalOperands is not { Count: 1 }) {
				emitter.ReportError($"'{context.Mnemonic}' requires two operands", context.Location);
				return true;
			}

			var op2 = context.AdditionalOperands[0];
			if (op2.Identifier is not null) {
				if (!TryResolveRegister(op2.Identifier, out var rm, context, emitter)) {
					return true;
				}

				var bytes = InstructionSetARM7TDMI.EncodeDataProcessingRegister(
					InstructionSetARM7TDMI.ArmOpcodes.CMP, 0, rn, rm,
					setFlags: true, condition: condition);
				EmitLong(emitter, bytes);
				return true;
			}

			if (!TryResolveImmediate(op2.Value, out var imm8, out var rotate, context, emitter)) {
				return true;
			}

			var immBytes = InstructionSetARM7TDMI.EncodeDataProcessingImmediate(
				InstructionSetARM7TDMI.ArmOpcodes.CMP, 0, rn, imm8, rotate,
				setFlags: true, condition: condition);
			EmitLong(emitter, immBytes);
			return true;
		}

		private static bool EmitLoadStore(SpecialInstructionContext context, ICodeEmitter emitter, bool isLoad, bool isByte, byte condition) {
			if (!TryResolveRegister(context.OperandIdentifier, out var rd, context, emitter)) {
				return true;
			}

			if (context.AdditionalOperands is not { Count: 1 or 2 }) {
				emitter.ReportError($"'{context.Mnemonic}' requires two or three operands", context.Location);
				return true;
			}

			if (!TryResolveRegister(context.AdditionalOperands[0].Identifier, out var rn, context, emitter)) {
				return true;
			}

			var offset = 0;
			int? registerOffset = null;
			var registerOffsetShift = 0;
			int? registerOffsetShiftRegister = null;
			var registerOffsetShiftType = InstructionSetARM7TDMI.ShiftTypes.LSL;
			var registerOffsetAdd = true;
			var preIndexed = context.AddressingMode != AddressingMode.MemoryReferencePostIndexed;
			var writeBack = context.AddressingMode == AddressingMode.MemoryReferenceWriteBack;
			if (context.AdditionalOperands.Count == 2) {
				var offsetOperand = context.AdditionalOperands[1];
				if (offsetOperand.Identifier is not null) {
					if (!TryResolveRegister(offsetOperand.Identifier, out var rm, context, emitter)) {
						return true;
					}

					registerOffset = rm;
					registerOffsetAdd = !offsetOperand.IsNegative;

					if (!string.IsNullOrWhiteSpace(offsetOperand.ShiftOperator)) {
						if (offsetOperand.ShiftOperator.Equals("rrx", StringComparison.OrdinalIgnoreCase)) {
							registerOffsetShiftType = InstructionSetARM7TDMI.ShiftTypes.ROR;
							registerOffsetShift = 0;
							registerOffsetShiftRegister = null;
						} else
						if (!InstructionSetARM7TDMI.TryGetShiftType(offsetOperand.ShiftOperator, out registerOffsetShiftType)) {
							emitter.ReportError($"'{context.Mnemonic}' unsupported register offset shift operator '{offsetOperand.ShiftOperator}'", context.Location);
							return true;
						}
					}

					if (!string.IsNullOrWhiteSpace(offsetOperand.ShiftRegisterIdentifier)) {
						if (offsetOperand.ShiftOperator is not "lsl" and not "lsr" and not "asr" and not "ror") {
							emitter.ReportError($"'{context.Mnemonic}' register-specified shift is only valid for lsl/lsr/asr/ror", context.Location);
							return true;
						}

						if (!TryResolveRegister(offsetOperand.ShiftRegisterIdentifier, out var rs, context, emitter)) {
							return true;
						}

						registerOffsetShiftRegister = rs;
					}

					if (offsetOperand.Value.HasValue) {
						if (registerOffsetShiftRegister.HasValue) {
							emitter.ReportError($"'{context.Mnemonic}' cannot specify both immediate and register shift amount", context.Location);
							return true;
						}

						if (offsetOperand.Value.Value < 0 || offsetOperand.Value.Value > 31) {
							emitter.ReportError($"'{context.Mnemonic}' register offset shift amount must be in range 0..31", context.Location);
							return true;
						}

						registerOffsetShift = (int)offsetOperand.Value.Value;
					}
				} else {
					if (!offsetOperand.Value.HasValue) {
						emitter.ReportError($"'{context.Mnemonic}' offset must resolve to a constant", context.Location);
						return true;
					}

					if (offsetOperand.Value.Value < -4095 || offsetOperand.Value.Value > 4095) {
						emitter.ReportError($"'{context.Mnemonic}' immediate offset must be in range -4095..4095", context.Location);
						return true;
					}

					offset = (int)offsetOperand.Value.Value;
				}
			}

			var bytes = registerOffset.HasValue
				? EncodeLoadStoreRegisterOffset(isLoad, rd, rn, registerOffset.Value, isByte, condition,
					preIndexed: preIndexed, addOffset: registerOffsetAdd, writeBack: writeBack,
					shiftAmount: registerOffsetShift, shiftType: registerOffsetShiftType, shiftRegister: registerOffsetShiftRegister)
				: InstructionSetARM7TDMI.EncodeLoadStoreImmediate(isLoad, rd, rn, offset, isByte, preIndexed: preIndexed, writeBack: writeBack, condition: condition);
			EmitLong(emitter, bytes);
			return true;
		}

		private static bool EmitMultiply(SpecialInstructionContext context, ICodeEmitter emitter, bool accumulate, bool setFlags, byte condition) {
			if (!TryResolveRegister(context.OperandIdentifier, out var rd, context, emitter)) {
				return true;
			}

			var expectedAdditional = accumulate ? 3 : 2;
			if (context.AdditionalOperands is null || context.AdditionalOperands.Count != expectedAdditional) {
				emitter.ReportError($"'{context.Mnemonic}' requires {(accumulate ? "four" : "three")} register operands", context.Location);
				return true;
			}

			if (!TryResolveRegister(context.AdditionalOperands[0].Identifier, out var rm, context, emitter)) {
				return true;
			}

			if (!TryResolveRegister(context.AdditionalOperands[1].Identifier, out var rs, context, emitter)) {
				return true;
			}

			var rn = 0;
			if (accumulate && !TryResolveRegister(context.AdditionalOperands[2].Identifier, out rn, context, emitter)) {
				return true;
			}

			var bytes = EncodeMultiply(rd, rm, rs, rn, accumulate, setFlags, condition);
			EmitLong(emitter, bytes);
			return true;
		}

		private static bool EmitMultiplyLong(SpecialInstructionContext context, ICodeEmitter emitter, bool isUnsigned, bool accumulate, bool setFlags, byte condition) {
			if (!TryResolveRegister(context.OperandIdentifier, out var rdLo, context, emitter)) {
				return true;
			}

			if (context.AdditionalOperands is null || context.AdditionalOperands.Count != 3) {
				emitter.ReportError($"'{context.Mnemonic}' requires four register operands", context.Location);
				return true;
			}

			if (!TryResolveRegister(context.AdditionalOperands[0].Identifier, out var rdHi, context, emitter)) {
				return true;
			}

			if (!TryResolveRegister(context.AdditionalOperands[1].Identifier, out var rm, context, emitter)) {
				return true;
			}

			if (!TryResolveRegister(context.AdditionalOperands[2].Identifier, out var rs, context, emitter)) {
				return true;
			}

			var bytes = EncodeMultiplyLong(rdLo, rdHi, rm, rs, isUnsigned, accumulate, setFlags, condition);
			EmitLong(emitter, bytes);
			return true;
		}

		private static bool EmitBranch(SpecialInstructionContext context, ICodeEmitter emitter, bool link, byte condition) {
			if (!context.OperandValue.HasValue) {
				emitter.ReportError($"'{context.Mnemonic}' requires a resolvable branch target", context.Location);
				return true;
			}

			var instructionAddress = emitter.CurrentAddress;
			var nextAddress = instructionAddress + 8;
			var offset = (int)(context.OperandValue.Value - nextAddress);
			if ((offset & 0x3) != 0) {
				emitter.ReportError($"'{context.Mnemonic}' target must be word-aligned", context.Location);
				return true;
			}

			var bytes = InstructionSetARM7TDMI.EncodeBranch(offset, link, condition);
			EmitLong(emitter, bytes);

			if (link) {
				emitter.RegisterSubroutineEntry(context.OperandValue.Value);
				emitter.AddCrossReference((uint)instructionAddress, (uint)context.OperandValue.Value, 1);
			} else {
				emitter.RegisterJumpTarget(context.OperandValue.Value);
				emitter.AddCrossReference((uint)instructionAddress, (uint)context.OperandValue.Value, 3);
			}

			return true;
		}

		private static bool EmitBx(SpecialInstructionContext context, ICodeEmitter emitter, byte condition) {
			if (!TryResolveRegister(context.OperandIdentifier, out var rm, context, emitter)) {
				return true;
			}

			var bytes = InstructionSetARM7TDMI.EncodeBranchExchange(rm, condition);
			EmitLong(emitter, bytes);
			return true;
		}

		private static bool EmitSwi(SpecialInstructionContext context, ICodeEmitter emitter, byte condition) {
			if (!context.OperandValue.HasValue) {
				emitter.ReportError($"'{context.Mnemonic}' requires an immediate value", context.Location);
				return true;
			}

			var value = context.OperandValue.Value;
			if (value < 0 || value > 0xffffff) {
				emitter.ReportError($"'{context.Mnemonic}' immediate must be 0..$ffffff", context.Location);
				return true;
			}

			var bytes = InstructionSetARM7TDMI.EncodeSoftwareInterrupt((int)value, condition);
			EmitLong(emitter, bytes);
			return true;
		}

		private static bool EmitNop(ICodeEmitter emitter, byte condition) {
			var bytes = InstructionSetARM7TDMI.EncodeDataProcessingRegister(
				InstructionSetARM7TDMI.ArmOpcodes.MOV, 0, 0, 0,
				setFlags: false, condition: condition);
			EmitLong(emitter, bytes);
			return true;
		}

		private static void EmitLong(ICodeEmitter emitter, byte[] bytes) {
			foreach (var b in bytes) {
				emitter.EmitByte(b);
			}
		}

		private static byte[] EncodeMultiply(int rd, int rm, int rs, int rn, bool accumulate, bool setFlags, byte condition) {
			uint instruction = 0;

			instruction |= (uint)(condition & 0xf) << 28;
			if (accumulate) {
				instruction |= 1u << 21;
			}

			if (setFlags) {
				instruction |= 1u << 20;
			}

			instruction |= (uint)(rd & 0xf) << 16;
			instruction |= (uint)(rn & 0xf) << 12;
			instruction |= (uint)(rs & 0xf) << 8;
			instruction |= 0x90;
			instruction |= (uint)(rm & 0xf);

			return [
				(byte)(instruction & 0xff),
				(byte)((instruction >> 8) & 0xff),
				(byte)((instruction >> 16) & 0xff),
				(byte)((instruction >> 24) & 0xff)
			];
		}

		private static byte[] EncodeMultiplyLong(int rdLo, int rdHi, int rm, int rs, bool isUnsigned, bool accumulate, bool setFlags, byte condition) {
			uint instruction = 0;

			instruction |= (uint)(condition & 0xf) << 28;
			instruction |= 1u << 23;

			if (isUnsigned) {
				instruction |= 1u << 22;
			}

			if (accumulate) {
				instruction |= 1u << 21;
			}

			if (setFlags) {
				instruction |= 1u << 20;
			}

			instruction |= (uint)(rdHi & 0xf) << 16;
			instruction |= (uint)(rdLo & 0xf) << 12;
			instruction |= (uint)(rs & 0xf) << 8;
			instruction |= 0x90;
			instruction |= (uint)(rm & 0xf);

			return [
				(byte)(instruction & 0xff),
				(byte)((instruction >> 8) & 0xff),
				(byte)((instruction >> 16) & 0xff),
				(byte)((instruction >> 24) & 0xff)
			];
		}

		private static byte[] EncodeLoadStoreRegisterOffset(bool isLoad, int rd, int rn, int rm, bool isByte, byte condition,
			bool preIndexed, bool addOffset, bool writeBack, int shiftAmount, byte shiftType, int? shiftRegister) {
			uint instruction = 0;

			instruction |= (uint)(condition & 0xf) << 28;
			instruction |= 1u << 26;
			instruction |= 1u << 25; // Register offset

			if (preIndexed) {
				instruction |= 1u << 24;
			}

			if (addOffset) {
				instruction |= 1u << 23;
			}

			if (writeBack) {
				instruction |= 1u << 21;
			}

			if (isByte) {
				instruction |= 1u << 22;
			}

			if (isLoad) {
				instruction |= 1u << 20;
			}

			instruction |= (uint)(rn & 0xf) << 16;
			instruction |= (uint)(rd & 0xf) << 12;

			if (shiftRegister.HasValue) {
				instruction |= (uint)(shiftRegister.Value & 0xf) << 8;
				instruction |= (uint)(shiftType & 0x3) << 5;
				instruction |= 1u << 4;
			} else {
				instruction |= (uint)(shiftAmount & 0x1f) << 7;
				instruction |= (uint)(shiftType & 0x3) << 5;
			}
			instruction |= (uint)(rm & 0xf);

			return [
				(byte)(instruction & 0xff),
				(byte)((instruction >> 8) & 0xff),
				(byte)((instruction >> 16) & 0xff),
				(byte)((instruction >> 24) & 0xff)
			];
		}

		private static bool TryNormalizeSpecialMnemonic(string mnemonic, out string baseMnemonic, out byte condition) {
			baseMnemonic = mnemonic.ToLowerInvariant();
			condition = InstructionSetARM7TDMI.Conditions.AL;

			if (s_specialSupportedMnemonics.Contains(baseMnemonic)) {
				return true;
			}

			if (baseMnemonic.Length <= 2) {
				return false;
			}

			var suffix = baseMnemonic[^2..];
			if (!s_conditionSuffixes.TryGetValue(suffix, out var parsedCondition)) {
				return false;
			}

			var candidate = baseMnemonic[..^2];
			if (!s_specialSupportedMnemonics.Contains(candidate)) {
				return false;
			}

			baseMnemonic = candidate;
			condition = parsedCondition;
			return true;
		}

		private static bool TryResolveRegister(string? registerName, out int register,
			SpecialInstructionContext context, ICodeEmitter emitter) {
			register = 0;
			if (string.IsNullOrWhiteSpace(registerName) || !InstructionSetARM7TDMI.TryGetRegister(registerName, out register)) {
				emitter.ReportError($"'{context.Mnemonic}' expects a register operand", context.Location);
				return false;
			}

			return true;
		}

		private static bool TryResolveImmediate(long? value, out byte immediate, out byte rotate,
			SpecialInstructionContext context, ICodeEmitter emitter) {
			immediate = 0;
			rotate = 0;

			if (!value.HasValue) {
				emitter.ReportError($"'{context.Mnemonic}' immediate operand must be a constant", context.Location);
				return false;
			}

			if (!InstructionSetARM7TDMI.TryEncodeImmediate((uint)value.Value, out immediate, out rotate)) {
				emitter.ReportError($"'{context.Mnemonic}' immediate cannot be encoded in ARM rotated-immediate format", context.Location);
				return false;
			}

			return true;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetARM7TDMI.IsBranchInstruction(mnemonic);
	}
}
