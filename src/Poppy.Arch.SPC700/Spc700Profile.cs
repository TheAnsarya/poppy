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

	/// <inheritdoc />
	public bool TryHandleDirective(DirectiveNode node, SemanticAnalyzer analyzer) {
		var directiveName = node.Name.ToLowerInvariant();

		switch (directiveName) {
			case "spc_song_title":
			case "spc_game_title":
			case "spc_artist":
			case "spc_entry":
				return HandleSpcDirective(node, analyzer, directiveName);

			default:
				return false;
		}
	}

	private static SpcHeaderConfig GetOrCreateConfig(SemanticAnalyzer analyzer) {
		if (analyzer.HeaderConfig is SpcHeaderConfig config) return config;
		var newConfig = new SpcHeaderConfig();
		analyzer.HeaderConfig = newConfig;
		return newConfig;
	}

	private static bool HandleSpcDirective(DirectiveNode node, SemanticAnalyzer analyzer, string directiveName) {
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
			case "spc_song_title":
				if (stringValue is null) {
					analyzer.AddError(".spc_song_title directive requires a string value (max 32 characters)", node.Location);
					return true;
				}
				if (stringValue.Length > 32) {
					analyzer.AddError($".spc_song_title is too long ({stringValue.Length} characters, maximum is 32)", node.Location);
					return true;
				}
				config.SongTitle = stringValue;
				break;

			case "spc_game_title":
				if (stringValue is null) {
					analyzer.AddError(".spc_game_title directive requires a string value (max 32 characters)", node.Location);
					return true;
				}
				if (stringValue.Length > 32) {
					analyzer.AddError($".spc_game_title is too long ({stringValue.Length} characters, maximum is 32)", node.Location);
					return true;
				}
				config.GameTitle = stringValue;
				break;

			case "spc_artist":
				if (stringValue is null) {
					analyzer.AddError(".spc_artist directive requires a string value (max 32 characters)", node.Location);
					return true;
				}
				if (stringValue.Length > 32) {
					analyzer.AddError($".spc_artist is too long ({stringValue.Length} characters, maximum is 32)", node.Location);
					return true;
				}
				config.Artist = stringValue;
				break;

			case "spc_entry":
				if (value is null) {
					analyzer.AddError(".spc_entry directive requires an entry point address ($0000-$ffff)", node.Location);
					return true;
				}
				if (value < 0 || value > 0xffff) {
					analyzer.AddError($".spc_entry address must be $0000-$ffff (got ${value:x4})", node.Location);
					return true;
				}
				config.EntryPoint = (int)value;
				break;
		}

		return true;
	}

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
		private static readonly FrozenSet<string> s_mnemonics = InstructionSetSPC700.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);
		public IReadOnlySet<string> Mnemonics => s_mnemonics;

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
