namespace Poppy.Arch.SM83;

using System.Collections.Frozen;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// Sharp SM83 target profile (Game Boy / GBC).
/// </summary>
internal sealed class Sm83Profile : ITargetProfile {
	public static readonly Sm83Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.SM83;
	public IInstructionEncoder Encoder { get; } = new Sm83Encoder();
	public int DefaultBankSize => 0x4000; // 16KB Game Boy bank

	public long GetBankCpuBase(int bank) =>
		bank == 0 ? 0x0000 : 0x4000;

	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new Sm83RomBuilderAdapter(analyzer);

	/// <inheritdoc />
	public int MapCpuToRomOffset(int cpuAddress) =>
		cpuAddress >= 0 && cpuAddress < 0x8000 ? cpuAddress : -1;

	/// <inheritdoc />
	public string GetMemoryRegionName(long address) => address switch {
		< 0x8000 => "ROM",
		< 0xa000 => "VRAM",
		< 0xc000 => "SRAM",
		_ => "WRAM"
	};

	/// <inheritdoc />
	public IReadOnlyList<(string Name, long StartAddress, long MaxSize, SegmentType Type)> GetDefaultSegments() => [
		("ROM0", 0x0000, 0x4000, SegmentType.Rom),
		("ROMX", 0x4000, 0x4000, SegmentType.Rom),
		("VRAM", 0x8000, 0x2000, SegmentType.Ram),
		("WRAM0", 0xc000, 0x1000, SegmentType.Ram),
		("HRAM", 0xff80, 0x007f, SegmentType.Ram),
	];

	/// <inheritdoc />
	public bool TryHandleDirective(DirectiveNode node, SemanticAnalyzer analyzer) {
		var directiveName = node.Name.ToLowerInvariant();

		switch (directiveName) {
			case "gb_title":
			case "gb_cgb":
			case "gb_sgb":
			case "gb_cartridge_type":
			case "gb_rom_size":
			case "gb_ram_size":
			case "gb_region":
			case "gb_version":
				return HandleGbDirective(node, analyzer, directiveName);

			default:
				return false;
		}
	}

	private static bool HandleGbDirective(DirectiveNode node, SemanticAnalyzer analyzer, string directiveName) {
		if (analyzer.Pass != 1) return true;

		long? value = null;
		string? stringValue = null;

		if (node.Arguments.Count > 0) {
			if (node.Arguments[0] is StringLiteralNode stringLit) {
				stringValue = stringLit.Value;
			} else {
				value = analyzer.EvaluateExpression(node.Arguments[0]);
				if (value is null) {
					analyzer.AddError($".{directiveName} directive requires a constant value", node.Location);
					return true;
				}
			}
		}

		switch (directiveName) {
			case "gb_title":
				if (stringValue is null) {
					analyzer.AddError(".gb_title directive requires a string value (max 16 characters)", node.Location);
					return true;
				}
				if (stringValue.Length > 16) {
					analyzer.AddError($".gb_title is too long ({stringValue.Length} characters, maximum is 16)", node.Location);
					return true;
				}
				analyzer.GbTitle = stringValue;
				break;

			case "gb_cgb":
				if (value is null) {
					analyzer.AddError(".gb_cgb directive requires a mode (0=DMG only, 1=CGB compatible, 2=CGB only)", node.Location);
					return true;
				}
				if (value < 0 || value > 2) {
					analyzer.AddError($".gb_cgb mode must be 0, 1, or 2 (got {value})", node.Location);
					return true;
				}
				analyzer.GbCgbMode = (int)value;
				break;

			case "gb_sgb":
				analyzer.GbSgbEnabled = value is null || value != 0;
				break;

			case "gb_cartridge_type":
				if (value is null) {
					analyzer.AddError(".gb_cartridge_type directive requires a cartridge type code", node.Location);
					return true;
				}
				if (value < 0 || value > 0x1e) {
					analyzer.AddError($".gb_cartridge_type must be 0-$1e (got ${value:x})", node.Location);
					return true;
				}
				analyzer.GbCartridgeType = (int)value;
				break;

			case "gb_rom_size":
				if (value is null) {
					analyzer.AddError(".gb_rom_size directive requires a ROM size in KB (32, 64, 128, 256, etc.)", node.Location);
					return true;
				}
				if (value < 32 || (value & (value - 1)) != 0) {
					analyzer.AddError($".gb_rom_size must be a power of 2 >= 32 (got {value})", node.Location);
					return true;
				}
				analyzer.GbRomSizeKb = (int)value;
				break;

			case "gb_ram_size":
				if (value is null) {
					analyzer.AddError(".gb_ram_size directive requires a RAM size in KB (0, 2, 8, 32, 64, or 128)", node.Location);
					return true;
				}
				var validRamSizes = new[] { 0, 2, 8, 32, 64, 128 };
				if (!validRamSizes.Contains((int)value)) {
					analyzer.AddError($".gb_ram_size must be 0, 2, 8, 32, 64, or 128 KB (got {value})", node.Location);
					return true;
				}
				analyzer.GbRamSizeKb = (int)value;
				break;

			case "gb_region":
				if (value is null) {
					analyzer.AddError(".gb_region directive requires a region code (0=Japan, 1=International)", node.Location);
					return true;
				}
				if (value < 0 || value > 1) {
					analyzer.AddError($".gb_region must be 0 or 1 (got {value})", node.Location);
					return true;
				}
				analyzer.GbRegion = (int)value;
				break;

			case "gb_version":
				if (value is null) {
					analyzer.AddError(".gb_version directive requires a version number (0-255)", node.Location);
					return true;
				}
				if (value < 0 || value > 255) {
					analyzer.AddError($".gb_version must be 0-255 (got {value})", node.Location);
					return true;
				}
				analyzer.GbVersion = (int)value;
				break;
		}

		return true;
	}

	private sealed class Sm83RomBuilderAdapter(SemanticAnalyzer analyzer) : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			var headerBuilder = analyzer.GetGbHeaderBuilder();
			if (headerBuilder is null) {
				return flatBinary;
			}

			var header = headerBuilder.Build();
			var romBuilder = new GbRomBuilder(header);
			foreach (var segment in segments) {
				romBuilder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}
			return romBuilder.Build();
		}
	}

	private sealed class Sm83Encoder : IInstructionEncoder {
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSetSM83.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSetSM83.TryGetEncoding(mnemonic, mode, out var enc)) {
				encoding = new EncodedInstruction(enc.Opcode, enc.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSetSM83.IsRelativeBranch(mnemonic);
	}
}
