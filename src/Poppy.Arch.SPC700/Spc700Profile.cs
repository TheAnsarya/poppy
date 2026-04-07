namespace Poppy.Arch.SPC700;

using System.Collections.Frozen;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// Sony SPC700 target profile (SNES Audio).
/// </summary>
internal sealed class Spc700Profile : ITargetProfile {
	public static readonly Spc700Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.SPC700;
	public IInstructionEncoder Encoder { get; } = new Spc700Encoder();
	public int DefaultBankSize => 0x4000; // Default
	public long GetBankCpuBase(int bank) => -1;

	/// <inheritdoc />
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new Spc700RomBuilderAdapter(analyzer);

	private sealed class Spc700RomBuilderAdapter(SemanticAnalyzer analyzer) : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			var spcBuilder = analyzer.GetSpcFileBuilder() ?? new SpcFileBuilder();

			foreach (var segment in segments) {
				if (segment.StartAddress <= 0xffff) {
					spcBuilder.SetRamAt((ushort)segment.StartAddress, segment.Data.ToArray());
				}
			}

			// If no explicit entry point was set, use the first segment's address
			if (segments.Count > 0 && analyzer.GetSpcFileBuilder() is null) {
				spcBuilder.SetPC((ushort)segments[0].StartAddress);
			}

			return spcBuilder.Build();
		}
	}

	private sealed class Spc700Encoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSetSPC700.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSetSPC700.TryGetEncoding(mnemonic, mode, out var opcode, out var size)) {
				encoding = new EncodedInstruction(opcode, size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetSPC700.IsBranchInstruction(mnemonic);
	}
}
