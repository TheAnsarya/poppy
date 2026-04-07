﻿namespace Poppy.Core.Arch.Profiles;

using System.Collections.Frozen;
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
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSetARM7TDMI.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			// ARM7TDMI uses a different encoding model; TryGetInstructionEncoding
			// is not dispatched through the shared pipeline currently.
			// This is a placeholder for future integration.
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetARM7TDMI.IsBranchInstruction(mnemonic);
	}
}
