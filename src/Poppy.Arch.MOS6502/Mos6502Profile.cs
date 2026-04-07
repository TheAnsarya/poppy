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
		public IReadOnlySet<string> Mnemonics { get; } = InstructionSet6502.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

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
