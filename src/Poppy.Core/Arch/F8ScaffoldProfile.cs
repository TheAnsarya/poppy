namespace Poppy.Core.Arch;

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
		private static readonly FrozenSet<string> s_empty = FrozenSet<string>.Empty;

		public IReadOnlySet<string> Mnemonics => s_empty;

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) => false;
	}
}
