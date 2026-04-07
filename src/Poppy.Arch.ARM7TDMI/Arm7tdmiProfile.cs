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
				analyzer.GbaTitle = stringValue;
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
				analyzer.GbaGameCode = stringValue;
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
				analyzer.GbaMakerCode = stringValue;
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
				analyzer.GbaVersion = (int)value;
				break;

			case "gba_entry":
				if (value is null) {
					analyzer.AddError(".gba_entry directive requires an entry point address", node.Location);
					return true;
				}
				analyzer.GbaEntryPoint = (int)value;
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
