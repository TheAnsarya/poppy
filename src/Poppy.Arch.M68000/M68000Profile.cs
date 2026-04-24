namespace Poppy.Arch.M68000;

using System.Collections.Frozen;
using System.Numerics;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// Motorola 68000 target profile (Sega Genesis / Mega Drive).
/// </summary>
internal sealed class M68000Profile : ITargetProfile {
	public static readonly M68000Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.M68000;
	public IInstructionEncoder Encoder { get; } = new M68000Encoder();
	public int DefaultBankSize => 0x4000; // Default
	public long GetBankCpuBase(int bank) => -1;

	/// <inheritdoc />
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new M68000RomBuilderAdapter();

	private sealed class M68000RomBuilderAdapter : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			// Auto-size ROM based on content (power of 2, minimum 32KB for header/vectors)
			int maxEnd = 0;
			foreach (var segment in segments) {
				var end = (int)segment.StartAddress + segment.Data.Count;
				if (end > maxEnd) maxEnd = end;
			}
			var romSize = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(maxEnd, 0x8000));

			var builder = new GenesisRomBuilder(romSize);
			foreach (var segment in segments) {
				builder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}
			return builder.Build();
		}
	}

	private sealed class M68000Encoder : IInstructionEncoder {
		private static readonly FrozenSet<string> s_mnemonics = InstructionSetM68000.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);
		public IReadOnlySet<string> Mnemonics => s_mnemonics;

		private const string MoveUnsupportedSourceMessage = "'move' source operand shape is not supported by deterministic Genesis M68000 special emission";
		private const string MoveUnsupportedDestinationMessage = "'move' destination must be data register direct (dn) for deterministic Genesis M68000 special emission";
		private const string MoveaUnsupportedSourceMessage = "'movea' source operand shape is not supported by deterministic Genesis M68000 special emission";
		private const string MoveaUnsupportedDestinationMessage = "'movea' destination must be address register direct (an) for deterministic Genesis M68000 special emission";

		private enum SourceExtensionKind {
			None,
			Long,
		}

		private static bool TryGetDataRegister(string name, out int register) {
			register = -1;
			if (!InstructionSetM68000.TryGetRegister(name, out var encoding, out var isAddress) || isAddress) {
				return false;
			}

			register = encoding;
			return register >= 0 && register <= 7;
		}

		private static bool TryGetAddressRegister(string name, out int register) {
			register = -1;
			if (!InstructionSetM68000.TryGetRegister(name, out var encoding, out var isAddress) || !isAddress) {
				return false;
			}

			register = encoding;
			return register >= 0 && register <= 7;
		}

		public int GetSpecialInstructionSize(string mnemonic, string? operandIdentifier, bool hasOperand, char? sizeSuffix,
			IReadOnlyList<ResolvedOperand>? additionalOperands) {
			if (mnemonic.Equals("lea", StringComparison.OrdinalIgnoreCase)) {
				if (!hasOperand || additionalOperands is null || additionalOperands.Count != 1) {
					return 0;
				}

				var destination = additionalOperands[0].Identifier;
				if (string.IsNullOrEmpty(destination) || !TryGetAddressRegister(destination, out _)) {
					return 0;
				}

				return 2 + GetSourceExtensionSize(operandIdentifier, hasOperand);
			}

			if (mnemonic.Equals("pea", StringComparison.OrdinalIgnoreCase)) {
				if (!hasOperand || additionalOperands is not null && additionalOperands.Count > 0) {
					return 0;
				}

				return 2 + GetSourceExtensionSize(operandIdentifier, hasOperand);
			}

			if (mnemonic.Equals("move", StringComparison.OrdinalIgnoreCase)) {
				if ((sizeSuffix.HasValue && sizeSuffix.Value != 'l') || !hasOperand || additionalOperands is null || additionalOperands.Count != 1) {
					return 0;
				}

				var destination = additionalOperands[0].Identifier;
				if (string.IsNullOrEmpty(destination) || !TryGetDataRegister(destination, out _)) {
					return 0;
				}

				return 2 + GetSourceExtensionSize(operandIdentifier, hasOperand);
			}

			if (mnemonic.Equals("movea", StringComparison.OrdinalIgnoreCase)) {
				if ((sizeSuffix.HasValue && sizeSuffix.Value != 'l') || !hasOperand || additionalOperands is null || additionalOperands.Count != 1) {
					return 0;
				}

				var destination = additionalOperands[0].Identifier;
				if (string.IsNullOrEmpty(destination) || !TryGetAddressRegister(destination, out _)) {
					return 0;
				}

				return 2 + GetSourceExtensionSize(operandIdentifier, hasOperand);
			}

			if (mnemonic.Equals("moveq", StringComparison.OrdinalIgnoreCase)) {
				if (!hasOperand || string.IsNullOrEmpty(operandIdentifier) || additionalOperands is null || additionalOperands.Count != 1) {
					return 0;
				}

				var destination = additionalOperands[0].Identifier;
				return !string.IsNullOrEmpty(destination) && TryGetDataRegister(destination, out _) ? 2 : 0;
			}

			if (mnemonic.Equals("jmp", StringComparison.OrdinalIgnoreCase) || mnemonic.Equals("jsr", StringComparison.OrdinalIgnoreCase)) {
				if (!hasOperand || additionalOperands is not null && additionalOperands.Count > 0) {
					return 0;
				}

				if (!string.IsNullOrEmpty(operandIdentifier) && TryGetAddressRegister(operandIdentifier, out _)) {
					return 2;
				}

				return 6;
			}

			if (hasOperand || !string.IsNullOrEmpty(operandIdentifier)) {
				return 0;
			}

			if (additionalOperands is not null && additionalOperands.Count > 0) {
				return 0;
			}

			return InstructionSetM68000.TryGetBaseOpcode(mnemonic, out _) ? 2 : 0;
		}

		public bool TryEmitSpecialInstruction(SpecialInstructionContext context, ICodeEmitter emitter) {
			if (context.Mnemonic.Equals("lea", StringComparison.OrdinalIgnoreCase)) {
				if (context.AdditionalOperands is null || context.AdditionalOperands.Count != 1) {
					return false;
				}

				var destination = context.AdditionalOperands[0].Identifier;
				if (string.IsNullOrEmpty(destination) || !TryGetAddressRegister(destination, out var destinationRegister)) {
					return false;
				}

				if (!TryEncodeSourceEffectiveAddress(context, allowImmediate: false, out var sourceEa, out var extensionKind)) {
					return false;
				}

				var opcode = (ushort)(0x41c0 | (destinationRegister << 9) | sourceEa);
				EmitWord(emitter, opcode);
				EmitSourceExtension(emitter, context.OperandValue, extensionKind);
				return true;
			}

			if (context.Mnemonic.Equals("pea", StringComparison.OrdinalIgnoreCase)) {
				if (context.AdditionalOperands is not null && context.AdditionalOperands.Count > 0) {
					return false;
				}

				if (!TryEncodeSourceEffectiveAddress(context, allowImmediate: false, out var sourceEa, out var extensionKind)) {
					return false;
				}

				var opcode = (ushort)(0x4840 | sourceEa);
				EmitWord(emitter, opcode);
				EmitSourceExtension(emitter, context.OperandValue, extensionKind);
				return true;
			}

			if (context.Mnemonic.Equals("move", StringComparison.OrdinalIgnoreCase)) {
				if (context.SizeSuffix.HasValue && char.ToLowerInvariant(context.SizeSuffix.Value) != 'l') {
					emitter.ReportError("'move' deterministic Genesis special emission currently supports only '.l' size", context.Location);
					return true;
				}

				if (context.AdditionalOperands is null || context.AdditionalOperands.Count != 1) {
					emitter.ReportError("'move' requires two operands", context.Location);
					return true;
				}

				var destination = context.AdditionalOperands[0].Identifier;
				if (string.IsNullOrEmpty(destination) || !TryGetDataRegister(destination, out var destinationRegister)) {
					emitter.ReportError(MoveUnsupportedDestinationMessage, context.Location);
					return true;
				}

				if (!TryEncodeSourceEffectiveAddress(context, allowImmediate: true, out var sourceEa, out var extensionKind)) {
					emitter.ReportError(MoveUnsupportedSourceMessage, context.Location);
					return true;
				}

				var opcode = (ushort)(0x2000 | (destinationRegister << 9) | sourceEa);
				EmitWord(emitter, opcode);
				EmitSourceExtension(emitter, context.OperandValue, extensionKind);
				return true;
			}

			if (context.Mnemonic.Equals("movea", StringComparison.OrdinalIgnoreCase)) {
				if (context.SizeSuffix.HasValue && char.ToLowerInvariant(context.SizeSuffix.Value) != 'l') {
					emitter.ReportError("'movea' deterministic Genesis special emission currently supports only '.l' size", context.Location);
					return true;
				}

				if (context.AdditionalOperands is null || context.AdditionalOperands.Count != 1) {
					emitter.ReportError("'movea' requires two operands", context.Location);
					return true;
				}

				var destination = context.AdditionalOperands[0].Identifier;
				if (string.IsNullOrEmpty(destination) || !TryGetAddressRegister(destination, out var destinationRegister)) {
					emitter.ReportError(MoveaUnsupportedDestinationMessage, context.Location);
					return true;
				}

				if (!TryEncodeSourceEffectiveAddress(context, allowImmediate: true, out var sourceEa, out var extensionKind)) {
					emitter.ReportError(MoveaUnsupportedSourceMessage, context.Location);
					return true;
				}

				var opcode = (ushort)(0x2040 | (destinationRegister << 9) | sourceEa);
				EmitWord(emitter, opcode);
				EmitSourceExtension(emitter, context.OperandValue, extensionKind);
				return true;
			}

			if (context.Mnemonic.Equals("moveq", StringComparison.OrdinalIgnoreCase)) {
				if (!context.OperandValue.HasValue || context.AdditionalOperands is null || context.AdditionalOperands.Count != 1) {
					return false;
				}

				var destination = context.AdditionalOperands[0].Identifier;
				if (string.IsNullOrEmpty(destination) || !TryGetDataRegister(destination, out var dataRegister)) {
					return false;
				}

				if (context.OperandValue.Value < -128 || context.OperandValue.Value > 255) {
					return false;
				}

				var imm8 = (byte)(context.OperandValue.Value & 0xff);
				var opcode = (ushort)(0x7000 | (dataRegister << 9) | imm8);
				emitter.EmitByte((byte)((opcode >> 8) & 0xff));
				emitter.EmitByte((byte)(opcode & 0xff));
				return true;
			}

			if (context.Mnemonic.Equals("jmp", StringComparison.OrdinalIgnoreCase) || context.Mnemonic.Equals("jsr", StringComparison.OrdinalIgnoreCase)) {
				if (context.AdditionalOperands is not null && context.AdditionalOperands.Count > 0) {
					return false;
				}

				if (!InstructionSetM68000.TryGetBaseOpcode(context.Mnemonic, out var baseOpcode)) {
					return false;
				}

				if (!string.IsNullOrEmpty(context.OperandIdentifier)
					&& context.AddressingMode == AddressingMode.Indirect
					&& TryGetAddressRegister(context.OperandIdentifier, out var addressRegister)) {
					var eaBits = (2 << 3) | addressRegister;
					var opcode = (ushort)(baseOpcode | eaBits);
					emitter.EmitByte((byte)((opcode >> 8) & 0xff));
					emitter.EmitByte((byte)(opcode & 0xff));
					return true;
				}

				if (!context.OperandValue.HasValue) {
					return false;
				}

				// Encode absolute long effective address (mode=111, reg=001).
				var absoluteLongOpcode = (ushort)(baseOpcode | 0x0039);
				emitter.EmitByte((byte)((absoluteLongOpcode >> 8) & 0xff));
				emitter.EmitByte((byte)(absoluteLongOpcode & 0xff));
				var value = context.OperandValue.Value;
				emitter.EmitByte((byte)((value >> 24) & 0xff));
				emitter.EmitByte((byte)((value >> 16) & 0xff));
				emitter.EmitByte((byte)((value >> 8) & 0xff));
				emitter.EmitByte((byte)(value & 0xff));
				return true;
			}

			if (!string.IsNullOrEmpty(context.OperandIdentifier) || context.OperandValue.HasValue) {
				return false;
			}

			if (context.AdditionalOperands is not null && context.AdditionalOperands.Count > 0) {
				return false;
			}

			if (!InstructionSetM68000.TryGetBaseOpcode(context.Mnemonic, out var impliedBaseOpcode)) {
				return false;
			}

			// M68000 opcodes are 16-bit big-endian words.
			emitter.EmitByte((byte)((impliedBaseOpcode >> 8) & 0xff));
			emitter.EmitByte((byte)(impliedBaseOpcode & 0xff));
			return true;
		}

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSetM68000.TryGetEncodingFromShared(mnemonic, mode, out var opcode, out var size)) {
				encoding = new EncodedInstruction(opcode, size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetM68000.IsBranchInstruction(mnemonic);

		public bool IsRegister(string name) =>
			InstructionSetM68000.TryGetRegister(name, out _, out _);

		private static int GetSourceExtensionSize(string? operandIdentifier, bool hasOperand) {
			if (!hasOperand) {
				return 0;
			}

			if (!string.IsNullOrEmpty(operandIdentifier)
				&& (TryGetDataRegister(operandIdentifier, out _) || TryGetAddressRegister(operandIdentifier, out _))) {
				return 0;
			}

			return 4;
		}

		private static bool TryEncodeSourceEffectiveAddress(SpecialInstructionContext context, bool allowImmediate,
			out ushort sourceEaBits, out SourceExtensionKind extensionKind) {
			sourceEaBits = 0;
			extensionKind = SourceExtensionKind.None;

			if (!string.IsNullOrEmpty(context.OperandIdentifier)) {
				if (context.AddressingMode == AddressingMode.Indirect && TryGetAddressRegister(context.OperandIdentifier, out var indirectAddressRegister)) {
					sourceEaBits = (ushort)((2 << 3) | indirectAddressRegister);
					return true;
				}

				if (TryGetDataRegister(context.OperandIdentifier, out var dataRegister)) {
					sourceEaBits = (ushort)dataRegister;
					return true;
				}

				if (TryGetAddressRegister(context.OperandIdentifier, out var addressRegister)) {
					sourceEaBits = (ushort)((1 << 3) | addressRegister);
					return true;
				}
			}

			if (allowImmediate && context.AddressingMode == AddressingMode.Immediate && context.OperandValue.HasValue) {
				sourceEaBits = 0x003c;
				extensionKind = SourceExtensionKind.Long;
				return true;
			}

			if (context.OperandValue.HasValue) {
				sourceEaBits = 0x0039;
				extensionKind = SourceExtensionKind.Long;
				return true;
			}

			return false;
		}

		private static void EmitSourceExtension(ICodeEmitter emitter, long? operandValue, SourceExtensionKind extensionKind) {
			if (extensionKind == SourceExtensionKind.None || !operandValue.HasValue) {
				return;
			}

			var value = operandValue.Value;
			emitter.EmitByte((byte)((value >> 24) & 0xff));
			emitter.EmitByte((byte)((value >> 16) & 0xff));
			emitter.EmitByte((byte)((value >> 8) & 0xff));
			emitter.EmitByte((byte)(value & 0xff));
		}

		private static void EmitWord(ICodeEmitter emitter, ushort value) {
			emitter.EmitByte((byte)((value >> 8) & 0xff));
			emitter.EmitByte((byte)(value & 0xff));
		}
	}
}
