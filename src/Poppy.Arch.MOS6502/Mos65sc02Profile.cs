﻿namespace Poppy.Arch.MOS6502;

using System.Collections.Frozen;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

/// <summary>
/// MOS 65SC02 target profile (Atari Lynx).
/// </summary>
internal sealed class Mos65sc02Profile : ITargetProfile {
	public static readonly Mos65sc02Profile Instance = new();

	public TargetArchitecture Architecture => TargetArchitecture.MOS65SC02;
	public IInstructionEncoder Encoder { get; } = new Mos65sc02Encoder();
	public int DefaultBankSize => 0x4000; // 16KB default for Lynx

	public long GetBankCpuBase(int bank) => -1; // Lynx doesn't have standard banking

	public AddressingMode? AdjustAddressingMode(string mnemonic, AddressingMode mode) {
		// 65SC02 INC/DEC with implied → Accumulator
		if (mode == AddressingMode.Implied &&
			(mnemonic.Equals("inc", StringComparison.OrdinalIgnoreCase) ||
			 mnemonic.Equals("dec", StringComparison.OrdinalIgnoreCase))) {
			return AddressingMode.Accumulator;
		}
		return null;
	}

	public IRomBuilder? CreateRomBuilder(SemanticAnalyzer analyzer) => new Mos65sc02RomBuilderAdapter();

	/// <inheritdoc />
	public void ValidateMemoryAddress(string mnemonic, long address, SourceLocation location,
		Action<string, SourceLocation> reportError, Action<string, SourceLocation> reportWarning) {
		// Check if this is a memory-writing instruction
		var isStoreInstruction = mnemonic.ToLowerInvariant() is
			"sta" or "stx" or "sty" or "stz" or
			"inc" or "dec" or "asl" or "lsr" or "rol" or "ror" or
			"tsb" or "trb" or
			"rmb0" or "rmb1" or "rmb2" or "rmb3" or
			"rmb4" or "rmb5" or "rmb6" or "rmb7" or
			"smb0" or "smb1" or "smb2" or "smb3" or
			"smb4" or "smb5" or "smb6" or "smb7";

		if (!isStoreInstruction) return;

		// Lynx memory map validation
		// $0000-$fbff: RAM (64KB - 1KB reserved)
		// $fc00-$fcff: Suzy hardware registers
		// $fd00-$fdff: Mikey hardware registers
		// $fe00-$ffff: Boot ROM (512 bytes)
		if (address is >= 0xfe00 and <= 0xffff) {
			// Boot ROM - cannot write to ROM
			reportError($"Cannot write to Lynx Boot ROM at ${address:x4}", location);
		} else if (address is >= 0xfd00 and <= 0xfdff) {
			// Mikey hardware registers
			reportWarning($"Writing to Lynx Mikey hardware register at ${address:x4}", location);
		} else if (address is >= 0xfc00 and <= 0xfcff) {
			// Suzy hardware registers
			reportWarning($"Writing to Lynx Suzy hardware register at ${address:x4}", location);
		}
	}

	/// <inheritdoc />
	public bool TryHandleDirective(DirectiveNode node, SemanticAnalyzer analyzer) {
		var directiveName = node.Name.ToLowerInvariant();

		switch (directiveName) {
			case "lynx_name":
			case "lynx_manufacturer":
			case "lynx_rotation":
			case "lynx_bank0_size":
			case "lynx_bank1_size":
			case "lynxentry":
			case "lynxboot":
				return HandleLynxDirective(node, analyzer, directiveName);

			default:
				return false;
		}
	}

	private static LynxHeaderConfig GetOrCreateConfig(SemanticAnalyzer analyzer) {
		if (analyzer.HeaderConfig is LynxHeaderConfig config) return config;
		var newConfig = new LynxHeaderConfig();
		analyzer.HeaderConfig = newConfig;
		return newConfig;
	}

	private static bool HandleLynxDirective(DirectiveNode node, SemanticAnalyzer analyzer, string directiveName) {
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
			case "lynx_name":
				if (stringValue is null) {
					analyzer.AddError(".lynx_name directive requires a string value (up to 32 characters)", node.Location);
					return true;
				}
				if (stringValue.Length > 32) {
					analyzer.AddError($".lynx_name is too long ({stringValue.Length} characters, maximum is 32)", node.Location);
					return true;
				}
				config.GameName = stringValue;
				break;

			case "lynx_manufacturer":
				if (stringValue is null) {
					analyzer.AddError(".lynx_manufacturer directive requires a string value (up to 16 characters)", node.Location);
					return true;
				}
				if (stringValue.Length > 16) {
					analyzer.AddError($".lynx_manufacturer is too long ({stringValue.Length} characters, maximum is 16)", node.Location);
					return true;
				}
				config.Manufacturer = stringValue;
				break;

			case "lynx_rotation":
				if (value is null) {
					analyzer.AddError(".lynx_rotation directive requires a rotation mode (0=none, 1=left, 2=right)", node.Location);
					return true;
				}
				if (value < 0 || value > 2) {
					analyzer.AddError($".lynx_rotation must be 0, 1, or 2 (got {value})", node.Location);
					return true;
				}
				config.Rotation = (int)value;
				break;

			case "lynx_bank0_size":
				if (value is null) {
					analyzer.AddError(".lynx_bank0_size directive requires a ROM size in bytes (multiple of 256)", node.Location);
					return true;
				}
				if (value < 0 || value % 256 != 0) {
					analyzer.AddError($".lynx_bank0_size must be a positive multiple of 256 (got {value})", node.Location);
					return true;
				}
				config.Bank0Size = (int)value;
				break;

			case "lynx_bank1_size":
				if (value is null) {
					analyzer.AddError(".lynx_bank1_size directive requires a ROM size in bytes (multiple of 256, or 0)", node.Location);
					return true;
				}
				if (value < 0 || value % 256 != 0) {
					analyzer.AddError($".lynx_bank1_size must be a non-negative multiple of 256 (got {value})", node.Location);
					return true;
				}
				config.Bank1Size = (int)value;
				break;

			case "lynxentry":
				if (value is null) {
					analyzer.AddError(".lynxentry directive requires an entry point address (e.g., $0200)", node.Location);
					return true;
				}
				if (value < 0x0200 || value > 0xfbff) {
					analyzer.AddError($".lynxentry address must be in RAM range $0200-$fbff (got ${value:x4})", node.Location);
					return true;
				}
				config.EntryPoint = (int)value;
				break;

			case "lynxboot":
				config.UseBootCode = value is null || value != 0;
				break;
		}

		return true;
	}

	private sealed class Mos65sc02RomBuilderAdapter : IRomBuilder {
		public byte[] Build(IReadOnlyList<OutputSegment> segments, byte[] flatBinary) {
			var romBuilder = new AtariLynxRomBuilder(
				bank0Size: 131072,
				bank1Size: 0,
				gameName: "Poppy Game");
			foreach (var segment in segments) {
				romBuilder.AddSegment((int)segment.StartAddress, segment.Data.ToArray());
			}
			return romBuilder.Build();
		}
	}

	private sealed class Mos65sc02Encoder : IInstructionEncoder {
		private static readonly FrozenSet<string> s_mnemonics = InstructionSet65SC02.GetAllMnemonics().ToFrozenSet(StringComparer.OrdinalIgnoreCase);
		public IReadOnlySet<string> Mnemonics => s_mnemonics;

		public bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding) {
			if (InstructionSet65SC02.TryGetEncoding(mnemonic, mode, out var enc)) {
				encoding = new EncodedInstruction(enc.Opcode, enc.Size);
				return true;
			}
			encoding = default;
			return false;
		}

		public bool IsBranchInstruction(string mnemonic) =>
			InstructionSet65SC02.IsBranchInstruction(mnemonic);
	}
}
