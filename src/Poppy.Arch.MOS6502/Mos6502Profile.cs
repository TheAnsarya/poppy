﻿namespace Poppy.Arch.MOS6502;

using System.Collections.Frozen;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// MOS 6502 target profile (NES).
/// </summary>
internal sealed class Mos6502Profile : ITargetProfile {
	public static readonly Mos6502Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.MOS6502;
	public IInstructionEncoder Encoder { get; } = new Mos6502Encoder();
	public int DefaultBankSize => 0x4000; // 16KB NES PRG bank
	public long GetBankCpuBase(int bank) => 0x8000;

	/// <inheritdoc />
	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new Mos6502RomBuilderAdapter(analyzer);

	/// <inheritdoc />
	public int RomFileHeaderSize => 16; // 16-byte iNES header

	/// <inheritdoc />
	public int MapCpuToRomOffset(int cpuAddress) =>
		cpuAddress >= 0x8000 ? cpuAddress - 0x8000 : -1;

	/// <inheritdoc />
	public string GetMemoryRegionName(long address) => address switch {
		< 0x2000 => "RAM",     // Internal RAM
		< 0x8000 => "REG",     // PPU/APU registers, cartridge space
		_ => "PRG"             // PRG ROM
	};

	/// <inheritdoc />
	public int GetAddressBank(long address) =>
		address >= 0x8000 ? (int)((address - 0x8000) / 0x4000) : 0;

	/// <inheritdoc />
	public IReadOnlyList<(string Name, long StartAddress, long MaxSize, SegmentType Type)> GetDefaultSegments() => [
		("ZEROPAGE", 0x0000, 0x0100, SegmentType.ZeroPage),
		("RAM", 0x0200, 0x0600, SegmentType.Ram),
		("CODE", 0x8000, 0x8000, SegmentType.Code),
	];

	/// <inheritdoc />
	public bool TryHandleDirective(DirectiveNode node, SemanticAnalyzer analyzer) {
		var directiveName = node.Name.ToLowerInvariant();

		switch (directiveName) {
			case "mapper":
				return HandleMapperDirective(node, analyzer);

			case "ines_prg":
			case "ines_chr":
			case "ines_mapper":
			case "ines_submapper":
			case "ines_mirroring":
			case "ines_battery":
			case "ines_trainer":
			case "ines_fourscreen":
			case "ines_prgram":
			case "ines_chrram":
			case "ines_pal":
			case "ines2":
				return HandleINesDirective(node, analyzer, directiveName);

			default:
				return false;
		}
	}

	private static NesHeaderConfig GetOrCreateConfig(SemanticAnalyzer analyzer) {
		if (analyzer.HeaderConfig is NesHeaderConfig config) return config;
		var newConfig = new NesHeaderConfig();
		analyzer.HeaderConfig = newConfig;
		return newConfig;
	}

	private static bool HandleMapperDirective(DirectiveNode node, SemanticAnalyzer analyzer) {
		if (analyzer.Pass != 1) return true;

		if (node.Arguments.Count < 1) {
			analyzer.AddError(".mapper directive requires a mapper number", node.Location);
			return true;
		}

		var mapperValue = analyzer.EvaluateExpression(node.Arguments[0]);
		if (mapperValue is null) {
			analyzer.AddError(".mapper directive requires a constant mapper number", node.Location);
			return true;
		}

		var config = GetOrCreateConfig(analyzer);
		if (config.NesMapper is not null) {
			analyzer.AddError("Mapper already set - cannot change", node.Location);
			return true;
		}

		config.NesMapper = (int)mapperValue;
		return true;
	}

	private static bool HandleINesDirective(DirectiveNode node, SemanticAnalyzer analyzer, string directiveName) {
		if (analyzer.Pass != 1) return true;

		// Get the value from first argument (if required)
		long? value = null;
		if (node.Arguments.Count > 0) {
			value = analyzer.EvaluateExpression(node.Arguments[0]);
			if (value is null) {
				analyzer.AddError($".{directiveName} directive requires a constant value", node.Location);
				return true;
			}
		}

		var config = GetOrCreateConfig(analyzer);

		switch (directiveName) {
			case "ines_prg":
				if (value is null) {
					analyzer.AddError(".ines_prg directive requires a PRG ROM size (in 16KB units)", node.Location);
					return true;
				}
				config.PrgSize = (int)value;
				break;

			case "ines_chr":
				if (value is null) {
					analyzer.AddError(".ines_chr directive requires a CHR ROM size (in 8KB units)", node.Location);
					return true;
				}
				config.ChrSize = (int)value;
				break;

			case "ines_mapper":
				if (value is null) {
					analyzer.AddError(".ines_mapper directive requires a mapper number", node.Location);
					return true;
				}
				config.Mapper = (int)value;
				break;

			case "ines_submapper":
				if (value is null) {
					analyzer.AddError(".ines_submapper directive requires a submapper number", node.Location);
					return true;
				}
				config.Submapper = (int)value;
				break;

			case "ines_mirroring":
				if (value is null) {
					analyzer.AddError(".ines_mirroring directive requires a mirroring mode (0=horizontal, 1=vertical)", node.Location);
					return true;
				}
				config.Mirroring = value != 0;    // 0 = horizontal, 1 = vertical
				break;

			case "ines_battery":
				config.Battery = value is null || value != 0;
				break;

			case "ines_trainer":
				config.Trainer = value is null || value != 0;
				break;

			case "ines_fourscreen":
				config.FourScreen = value is null || value != 0;
				break;

			case "ines_prgram":
				if (value is null) {
					analyzer.AddError(".ines_prgram directive requires a PRG RAM size (in 8KB units)", node.Location);
					return true;
				}
				config.PrgRamSize = (int)value;
				break;

			case "ines_chrram":
				if (value is null) {
					analyzer.AddError(".ines_chrram directive requires a CHR RAM size (in 8KB units)", node.Location);
					return true;
				}
				config.ChrRamSize = (int)value;
				break;

			case "ines_pal":
				config.Pal = value is null || value != 0;
				break;

			case "ines2":
				config.UseINes2 = value is null || value != 0;
				break;
		}

		return true;
	}

	private sealed class Mos6502RomBuilderAdapter(SemanticAnalyzer analyzer) : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			var headerBuilder = analyzer.GetINesHeaderBuilder();
			if (headerBuilder is null) {
				return flatBinary;
			}

			var header = headerBuilder.Build();
			var output = new byte[header.Length + flatBinary.Length];
			Array.Copy(header, 0, output, 0, header.Length);
			Array.Copy(flatBinary, 0, output, header.Length, flatBinary.Length);
			return output;
		}
	}

	private sealed class Mos6502Encoder : IInstructionEncoder {
		private static readonly FrozenSet<string> s_mnemonics = InstructionSet6502.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);
		public IReadOnlySet<string> Mnemonics => s_mnemonics;

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSet6502.TryGetEncoding(mnemonic, mode, out var enc)) {
				encoding = new EncodedInstruction(enc.Opcode, enc.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			mnemonic.Equals("bcc", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("bcs", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("beq", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("bmi", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("bne", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("bpl", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("bvc", StringComparison.OrdinalIgnoreCase) ||
			mnemonic.Equals("bvs", StringComparison.OrdinalIgnoreCase);
	}
}
