namespace Poppy.Core.Arch;

using System.Collections.Generic;
using System.Collections.Frozen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// Minimal Fairchild F8 (Channel F) target scaffold profile.
///
/// This profile exists to keep target selection and generic directive workflows
/// (.org/.db/.dw) functional before full F8 opcode/codegen support lands.
/// </summary>
internal sealed class F8ScaffoldProfile : ITargetProfile {
	public static readonly F8ScaffoldProfile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.F8;
	public IInstructionEncoder Encoder { get; } = new F8ScaffoldEncoder();

	// Channel F ROMs are typically small; 2KB keeps bank math deterministic for the scaffold path.
	public int DefaultBankSize => 0x0800;

	public long GetBankCpuBase(int bank) => -1;

	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => null;

	private sealed class F8ScaffoldEncoder : IInstructionEncoder {
		private static readonly FrozenDictionary<string, EncodedInstruction> s_encodings =
			new Dictionary<string, EncodedInstruction>(StringComparer.OrdinalIgnoreCase) {
				// MVP subset for early Channel F fixtures. This is intentionally small and explicit.
				[BuildKey("nop", AddressingMode.Implied)] = new EncodedInstruction(0x2b, 1),
				[BuildKey("jmp", AddressingMode.Absolute)] = new EncodedInstruction(0x29, 2),
				[BuildKey("ldi", AddressingMode.Immediate)] = new EncodedInstruction(0x20, 2),
			}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

		private static readonly FrozenSet<string> s_mnemonics =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
				"nop",
				"jmp",
				"ldi",
			}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public IReadOnlySet<string> Mnemonics => s_mnemonics;

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			return s_encodings.TryGetValue(BuildKey(mnemonic, mode), out encoding);
		}

		public bool IsBranchInstruction(string mnemonic) => false;

		private static string BuildKey(string mnemonic, AddressingMode mode) {
			return string.Concat(mnemonic, "|", mode);
		}
	}
}
