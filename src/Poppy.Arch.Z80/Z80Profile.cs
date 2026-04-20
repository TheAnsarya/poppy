namespace Poppy.Arch.Z80;

using System.Collections.Frozen;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// Zilog Z80 target profile (Sega Master System).
/// </summary>
internal sealed class Z80Profile : ITargetProfile {
	public static readonly Z80Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.Z80;
	public IInstructionEncoder Encoder { get; } = new Z80Encoder();
	public int DefaultBankSize => 0x4000; // Default
	public long GetBankCpuBase(int bank) => -1;

	/// <inheritdoc />
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new Z80RomBuilderAdapter();

	private sealed class Z80RomBuilderAdapter : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			var builder = new MasterSystemRomBuilder();
			foreach (var segment in segments) {
				builder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}
			return builder.Build();
		}
	}

	private sealed class Z80Encoder : IInstructionEncoder {
		private static readonly FrozenSet<string> s_mnemonics = InstructionSetZ80.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);
		public IReadOnlySet<string> Mnemonics => s_mnemonics;

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSetZ80.TryGetEncodingFromShared(mnemonic, mode, out var opcode, out var size)) {
				encoding = new EncodedInstruction(opcode, size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetZ80.IsRelativeBranch(mnemonic);
	}
}
