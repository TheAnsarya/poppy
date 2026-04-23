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
			if (!InstructionSetARM7TDMI.TryParseCondition(mnemonic, out var baseMnemonic, out _)) {
				return 0;
			}

			return baseMnemonic.ToLowerInvariant() switch {
				"mov" or "movs" => hasOperand && additionalOperands is { Count: 1 } ? 4 : 0,
				"add" or "adds" or "sub" or "subs" => hasOperand && additionalOperands is { Count: 2 } ? 4 : 0,
				"cmp" => hasOperand && additionalOperands is { Count: 1 } ? 4 : 0,
				"b" or "bl" => hasOperand ? 4 : 0,
				"bx" => hasOperand ? 4 : 0,
				"swi" or "svc" => hasOperand ? 4 : 0,
				"nop" => 4,
				_ => 0
			};
		}

		public bool TryEmitSpecialInstruction(SpecialInstructionContext context, ICodeEmitter emitter) {
			if (!InstructionSetARM7TDMI.TryParseCondition(context.Mnemonic, out var baseMnemonic, out var condition)) {
				return false;
			}

			var normalized = baseMnemonic.ToLowerInvariant();
			if (normalized is not ("mov" or "movs" or "add" or "adds" or "sub" or "subs" or "cmp" or "b" or "bl" or "bx" or "swi" or "svc" or "nop")) {
				return false;
			}

			return normalized switch {
				"mov" or "movs" => EmitMov(context, emitter, normalized, condition),
				"add" or "adds" => EmitDataProcessingThreeOperand(context, emitter, InstructionSetARM7TDMI.ArmOpcodes.ADD, normalized, condition),
				"sub" or "subs" => EmitDataProcessingThreeOperand(context, emitter, InstructionSetARM7TDMI.ArmOpcodes.SUB, normalized, condition),
				"cmp" => EmitCmp(context, emitter, condition),
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
