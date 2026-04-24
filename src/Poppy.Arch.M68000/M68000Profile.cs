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
	}
}
