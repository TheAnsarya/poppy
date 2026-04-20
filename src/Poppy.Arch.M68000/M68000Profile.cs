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
	}
}
