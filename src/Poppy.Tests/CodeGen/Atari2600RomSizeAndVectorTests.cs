// ============================================================================
// Atari2600RomSizeAndVectorTests.cs - ROM Size Validation & Vector Placement
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

using PoppyLexer = Poppy.Core.Lexer.Lexer;
using PoppyParser = Poppy.Core.Parser.Parser;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Tests for Atari 2600 ROM size validation, padding, and vector placement.
/// </summary>
public class Atari2600RomSizeAndVectorTests {
	/// <summary>
	/// Helper to generate Atari 2600 code from source.
	/// </summary>
	private static (byte[] Code, CodeGenerator Generator) GenerateAtari2600Code(string source) {
		var lexer = new PoppyLexer(source);
		var tokens = lexer.Tokenize();
		var parser = new PoppyParser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer();
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6507);
		var code = generator.Generate(program);

		return (code, generator);
	}

	// ========================================================================
	// ROM Size — 2K
	// ========================================================================

	[Fact]
	public void RomBuilder_2K_CorrectSize() {
		var builder = new Atari2600RomBuilder(2048, Atari2600RomBuilder.BankSwitchingMethod.None);
		var rom = builder.Build();
		Assert.Equal(2048, rom.Length);
	}

	[Fact]
	public void RomBuilder_2K_SegmentMapping() {
		var builder = new Atari2600RomBuilder(2048, Atari2600RomBuilder.BankSwitchingMethod.None);
		// 2K ROM maps $1000-$17ff
		builder.AddSegment(0x1000, [0xea]); // NOP
		var rom = builder.Build();
		Assert.Equal(0xea, rom[0]);
	}

	[Fact]
	public void RomBuilder_2K_MirrorMapping() {
		var builder = new Atari2600RomBuilder(2048, Atari2600RomBuilder.BankSwitchingMethod.None);
		// $1800-$1fff should mirror to $0000-$07ff in 2K ROM
		builder.AddSegment(0x1800, [0xea]); // NOP
		var rom = builder.Build();
		Assert.Equal(0xea, rom[0]);
	}

	// ========================================================================
	// ROM Size — 4K
	// ========================================================================

	[Fact]
	public void RomBuilder_4K_CorrectSize() {
		var builder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.None);
		var rom = builder.Build();
		Assert.Equal(4096, rom.Length);
	}

	[Fact]
	public void RomBuilder_4K_SegmentMapping() {
		var builder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.None);
		builder.AddSegment(0x1000, [0xa9, 0xff]); // LDA #$ff
		var rom = builder.Build();
		Assert.Equal(0xa9, rom[0]);
		Assert.Equal(0xff, rom[1]);
	}

	[Fact]
	public void RomBuilder_4K_EndOfRom() {
		var builder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.None);
		builder.AddSegment(0x1ffe, [0x42, 0x43]); // Last 2 bytes
		var rom = builder.Build();
		Assert.Equal(0x42, rom[4094]);
		Assert.Equal(0x43, rom[4095]);
	}

	// ========================================================================
	// ROM Size — 8K (F8)
	// ========================================================================

	[Fact]
	public void RomBuilder_8K_F8_CorrectSize() {
		var builder = new Atari2600RomBuilder(8192, Atari2600RomBuilder.BankSwitchingMethod.F8);
		var rom = builder.Build();
		Assert.Equal(8192, rom.Length);
	}

	// ========================================================================
	// ROM Size — 16K (F6)
	// ========================================================================

	[Fact]
	public void RomBuilder_16K_F6_CorrectSize() {
		var builder = new Atari2600RomBuilder(16384, Atari2600RomBuilder.BankSwitchingMethod.F6);
		var rom = builder.Build();
		Assert.Equal(16384, rom.Length);
	}

	// ========================================================================
	// ROM Size — 32K (F4)
	// ========================================================================

	[Fact]
	public void RomBuilder_32K_F4_CorrectSize() {
		var builder = new Atari2600RomBuilder(32768, Atari2600RomBuilder.BankSwitchingMethod.F4);
		var rom = builder.Build();
		Assert.Equal(32768, rom.Length);
	}

	// ========================================================================
	// ROM Size Validation Failures
	// ========================================================================

	[Fact]
	public void RomBuilder_None_Rejects8K() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(8192, Atari2600RomBuilder.BankSwitchingMethod.None));
	}

	[Fact]
	public void RomBuilder_None_Rejects1K() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(1024, Atari2600RomBuilder.BankSwitchingMethod.None));
	}

	[Fact]
	public void RomBuilder_Rejects0Size() {
		Assert.Throws<ArgumentException>(() =>
			new Atari2600RomBuilder(0, Atari2600RomBuilder.BankSwitchingMethod.None));
	}

	// ========================================================================
	// Reset Vector Placement
	// ========================================================================

	[Fact]
	public void ResetVector_4K_DefaultPlacement() {
		var builder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.None);
		var rom = builder.Build();

		// Reset vector at ROM_SIZE - 4 (offset 4092-4093)
		// Default should point to $1000 (start of ROM)
		int vectorOffset = 4092;
		ushort resetAddr = (ushort)(rom[vectorOffset] | (rom[vectorOffset + 1] << 8));
		Assert.Equal(0x1000, resetAddr);
	}

	[Fact]
	public void ResetVector_2K_DefaultPlacement() {
		var builder = new Atari2600RomBuilder(2048, Atari2600RomBuilder.BankSwitchingMethod.None);
		var rom = builder.Build();

		// Reset vector at ROM_SIZE - 4 (offset 2044-2045)
		int vectorOffset = 2044;
		ushort resetAddr = (ushort)(rom[vectorOffset] | (rom[vectorOffset + 1] << 8));
		Assert.Equal(0x1000, resetAddr);
	}

	[Fact]
	public void ResetVector_ExplicitPlacement_Preserved() {
		var builder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.None);
		// Place explicit reset vector pointing to $1080
		builder.AddSegment(0x1ffc, [0x80, 0x10]); // $1080 in little-endian
		var rom = builder.Build();

		int vectorOffset = 4092;
		ushort resetAddr = (ushort)(rom[vectorOffset] | (rom[vectorOffset + 1] << 8));
		Assert.Equal(0x1080, resetAddr);
	}

	[Fact]
	public void ResetVector_CodeGen_ExplicitVector() {
		var source = @"
.target atari2600
.org $1000
start:
	nop
.org $1ffc
.word start
";
		var (code, gen) = GenerateAtari2600Code(source);

		Assert.False(gen.HasErrors);

		// Verify reset vector at offset 4092-4093 points to $1000
		Assert.Equal(0x00, code[4092]); // Low byte of $1000
		Assert.Equal(0x10, code[4093]); // High byte of $1000
	}

	// ========================================================================
	// IRQ/BRK Vector
	// ========================================================================

	[Fact]
	public void IrqVector_ExplicitPlacement() {
		var builder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.None);
		// Place IRQ vector at $1ffe-$1fff pointing to $1050
		builder.AddSegment(0x1ffe, [0x50, 0x10]); // $1050 in little-endian
		var rom = builder.Build();

		int vectorOffset = 4094;
		ushort irqAddr = (ushort)(rom[vectorOffset] | (rom[vectorOffset + 1] << 8));
		Assert.Equal(0x1050, irqAddr);
	}

	[Fact]
	public void IrqVector_CodeGen_Placement() {
		var source = @"
.target atari2600
.org $1000
start:
	nop
irq_handler:
	rti
.org $1ffc
.word start
.word irq_handler
";
		var (code, gen) = GenerateAtari2600Code(source);

		Assert.False(gen.HasErrors);

		// Reset vector at 4092-4093
		Assert.Equal(0x00, code[4092]); // Low byte of $1000 (start)
		Assert.Equal(0x10, code[4093]); // High byte of $1000

		// IRQ vector at 4094-4095 should point to irq_handler ($1001 = NOP is 1 byte)
		ushort irqAddr = (ushort)(code[4094] | (code[4095] << 8));
		Assert.Equal(0x1001, irqAddr);
	}

	// ========================================================================
	// NMI Vector
	// ========================================================================

	[Fact]
	public void NmiVector_ExplicitPlacement() {
		var builder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.None);
		// NMI vector at $1ffa-$1ffb
		builder.AddSegment(0x1ffa, [0x30, 0x10]); // $1030 in little-endian
		var rom = builder.Build();

		int vectorOffset = 4090;
		ushort nmiAddr = (ushort)(rom[vectorOffset] | (rom[vectorOffset + 1] << 8));
		Assert.Equal(0x1030, nmiAddr);
	}

	// ========================================================================
	// Full Vector Table
	// ========================================================================

	[Fact]
	public void AllVectors_ExplicitPlacement() {
		var builder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.None);
		// NMI at $1ffa, RESET at $1ffc, IRQ at $1ffe
		builder.AddSegment(0x1ffa, [
			0x00, 0x10, // NMI → $1000
			0x80, 0x10, // RESET → $1080
			0x40, 0x10, // IRQ → $1040
		]);
		var rom = builder.Build();

		ushort nmiAddr = (ushort)(rom[4090] | (rom[4091] << 8));
		ushort resetAddr = (ushort)(rom[4092] | (rom[4093] << 8));
		ushort irqAddr = (ushort)(rom[4094] | (rom[4095] << 8));

		Assert.Equal(0x1000, nmiAddr);
		Assert.Equal(0x1080, resetAddr);
		Assert.Equal(0x1040, irqAddr);
	}

	[Fact]
	public void AllVectors_CodeGen_FullTable() {
		var source = @"
.target atari2600
.org $1000
nmi_handler:
	rti
start:
	nop
irq_handler:
	rti
.org $1ffa
.word nmi_handler
.word start
.word irq_handler
";
		var (code, gen) = GenerateAtari2600Code(source);

		Assert.False(gen.HasErrors);
		Assert.Equal(4096, code.Length);

		// NMI → $1000 (nmi_handler = first byte)
		ushort nmiAddr = (ushort)(code[4090] | (code[4091] << 8));
		Assert.Equal(0x1000, nmiAddr);

		// RESET → $1001 (start = after RTI)
		ushort resetAddr = (ushort)(code[4092] | (code[4093] << 8));
		Assert.Equal(0x1001, resetAddr);

		// IRQ → $1002 (irq_handler = after NOP)
		ushort irqAddr = (ushort)(code[4094] | (code[4095] << 8));
		Assert.Equal(0x1002, irqAddr);
	}

	// ========================================================================
	// ROM Padding
	// ========================================================================

	[Fact]
	public void RomPadding_UnusedBytesAreFF() {
		var builder = new Atari2600RomBuilder(4096, Atari2600RomBuilder.BankSwitchingMethod.None);
		builder.AddSegment(0x1000, [0xea]); // Just one NOP
		var rom = builder.Build();

		// Byte 0 should be NOP
		Assert.Equal(0xea, rom[0]);
		// Byte 1-4091 should be $ff (excluding vector area)
		for (int i = 1; i < 4090; i++) {
			Assert.Equal(0xff, rom[i]);
		}
	}

	[Theory]
	[InlineData(2048)]
	[InlineData(4096)]
	[InlineData(8192)]
	[InlineData(16384)]
	[InlineData(32768)]
	public void AllSizes_HaveCorrectLength(int expectedSize) {
		var method = expectedSize switch {
			2048 or 4096 => Atari2600RomBuilder.BankSwitchingMethod.None,
			8192 => Atari2600RomBuilder.BankSwitchingMethod.F8,
			16384 => Atari2600RomBuilder.BankSwitchingMethod.F6,
			32768 => Atari2600RomBuilder.BankSwitchingMethod.F4,
			_ => Atari2600RomBuilder.BankSwitchingMethod.None,
		};

		var builder = new Atari2600RomBuilder(expectedSize, method);
		var rom = builder.Build();
		Assert.Equal(expectedSize, rom.Length);
	}
}
