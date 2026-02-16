// ============================================================================
// AtariLynxCodeGeneratorTests.cs - Atari Lynx Code Generator Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

using PoppyLexer = Poppy.Core.Lexer.Lexer;
using PoppyParser = Poppy.Core.Parser.Parser;

namespace Poppy.Tests.CodeGen;

/// <summary>
/// Unit tests for Atari Lynx (65SC02) code generation.
/// </summary>
public class AtariLynxCodeGeneratorTests {
	/// <summary>
	/// Helper to generate Atari Lynx code from source.
	/// </summary>
	private static (byte[] Code, CodeGenerator Generator) GenerateAtariLynxCode(string source) {
		var lexer = new PoppyLexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new PoppyParser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer();
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS65SC02);
		var code = generator.Generate(program);

		return (code, generator);
	}

	// ========================================================================
	// 65SC02 New Instruction Tests
	// ========================================================================

	[Fact]
	public void Generate_Lynx_BraBranchAlways() {
		var source = @"
.target lynx
.org $0200
loop:
	nop
	bra loop
";
		var (code, gen) = GenerateAtariLynxCode(source);

		Assert.False(gen.HasErrors);
		// ROM has 64-byte header
		Assert.True(code.Length >= 64);

		// Check header magic "LYNX"
		Assert.Equal(0x4c, code[0]); // 'L'
		Assert.Equal(0x59, code[1]); // 'Y'
		Assert.Equal(0x4e, code[2]); // 'N'
		Assert.Equal(0x58, code[3]); // 'X'
	}

	[Fact]
	public void Generate_Lynx_PhxPhyInstructions() {
		var source = @"
.target lynx
.org $0200
	phx
	phy
	ply
	plx
";
		var (code, gen) = GenerateAtariLynxCode(source);

		Assert.False(gen.HasErrors);
		// Skip header (64 bytes) and check instructions
		var codeStart = 64;
		Assert.Equal(0xda, code[codeStart]);     // PHX
		Assert.Equal(0x5a, code[codeStart + 1]); // PHY
		Assert.Equal(0x7a, code[codeStart + 2]); // PLY
		Assert.Equal(0xfa, code[codeStart + 3]); // PLX
	}

	[Fact]
	public void Generate_Lynx_StzStoreZero() {
		var source = @"
.target lynx
.org $0200
	stz $80
	stz $80,x
	stz $2000
	stz $2000,x
";
		var (code, gen) = GenerateAtariLynxCode(source);

		Assert.False(gen.HasErrors);
		var codeStart = 64;
		Assert.Equal(0x64, code[codeStart]);     // STZ zp
		Assert.Equal(0x74, code[codeStart + 2]); // STZ zp,X
		Assert.Equal(0x9c, code[codeStart + 4]); // STZ abs
		Assert.Equal(0x9e, code[codeStart + 7]); // STZ abs,X
	}

	[Fact]
	public void Generate_Lynx_TrbTsbBitTest() {
		var source = @"
.target lynx
.org $0200
	trb $80
	trb $2000
	tsb $80
	tsb $2000
";
		var (code, gen) = GenerateAtariLynxCode(source);

		Assert.False(gen.HasErrors);
		var codeStart = 64;
		Assert.Equal(0x14, code[codeStart]);     // TRB zp
		Assert.Equal(0x1c, code[codeStart + 2]); // TRB abs
		Assert.Equal(0x04, code[codeStart + 5]); // TSB zp
		Assert.Equal(0x0c, code[codeStart + 7]); // TSB abs
	}

	[Fact]
	public void Generate_Lynx_BitImmediate() {
		var source = @"
.target lynx
.org $0200
	bit #$42
";
		var (code, gen) = GenerateAtariLynxCode(source);

		Assert.False(gen.HasErrors);
		var codeStart = 64;
		Assert.Equal(0x89, code[codeStart]); // BIT immediate
		Assert.Equal(0x42, code[codeStart + 1]);
	}

	[Fact]
	public void Generate_Lynx_IncDecAccumulator() {
		var source = @"
.target lynx
.org $0200
	inc a
	dec a
";
		var (code, gen) = GenerateAtariLynxCode(source);

		Assert.False(gen.HasErrors);
		var codeStart = 64;
		Assert.Equal(0x1a, code[codeStart]);     // INC A
		Assert.Equal(0x3a, code[codeStart + 1]); // DEC A
	}

	// ========================================================================
	// ROM Building Tests
	// ========================================================================

	[Fact]
	public void RomBuilder_CreatesHeaderWithMagic() {
		var builder = new AtariLynxRomBuilder(bank0Size: 131072, gameName: "Test Game");
		var rom = builder.Build();

		// Check magic "LYNX"
		Assert.Equal(0x4c, rom[0]); // 'L'
		Assert.Equal(0x59, rom[1]); // 'Y'
		Assert.Equal(0x4e, rom[2]); // 'N'
		Assert.Equal(0x58, rom[3]); // 'X'
	}

	[Fact]
	public void RomBuilder_SetsBank0PageCount() {
		var builder = new AtariLynxRomBuilder(bank0Size: 131072, gameName: "Test"); // 128K = 512 pages
		var rom = builder.Build();

		// Bank 0 page count at offset 4-5 (little-endian)
		var bank0Pages = rom[4] | (rom[5] << 8);
		Assert.Equal(512, bank0Pages); // 131072 / 256 = 512
	}

	[Fact]
	public void RomBuilder_SetsBank1PageCountToZero() {
		var builder = new AtariLynxRomBuilder(bank0Size: 131072, gameName: "Test");
		var rom = builder.Build();

		// Bank 1 page count at offset 6-7 (little-endian)
		var bank1Pages = rom[6] | (rom[7] << 8);
		Assert.Equal(0, bank1Pages); // No bank 1
	}

	[Fact]
	public void RomBuilder_SetsGameName() {
		var builder = new AtariLynxRomBuilder(bank0Size: 131072, gameName: "Poppy Test");
		var rom = builder.Build();

		// Game name at offset 10-41
		var gameName = System.Text.Encoding.ASCII.GetString(rom, 10, 10);
		Assert.Equal("Poppy Test", gameName);
	}

	[Fact]
	public void RomBuilder_TruncatesLongGameName() {
		var longName = "This is a very long game name that exceeds 32 characters";
		var builder = new AtariLynxRomBuilder(bank0Size: 131072, gameName: longName);
		var rom = builder.Build();

		// Should be truncated to 32 chars
		var gameName = System.Text.Encoding.ASCII.GetString(rom, 10, 32).TrimEnd('\0');
		Assert.True(gameName.Length <= 32);
	}

	[Fact]
	public void RomBuilder_AddsSegment() {
		var builder = new AtariLynxRomBuilder(bank0Size: 131072, gameName: "Test");
		var data = new byte[] { 0xa9, 0x42, 0x60 }; // LDA #$42, RTS

		builder.AddSegment(0, data);
		var rom = builder.Build();

		// Data should be after 64-byte header
		Assert.Equal(0xa9, rom[64]);
		Assert.Equal(0x42, rom[65]);
		Assert.Equal(0x60, rom[66]);
	}

	[Fact]
	public void RomBuilder_DualBank_SetsBothPageCounts() {
		var builder = new AtariLynxRomBuilder(
			bank0Size: 131072,   // 128K = 512 pages
			bank1Size: 65536,    // 64K = 256 pages
			gameName: "Dual Bank Test");
		var rom = builder.Build();

		var bank0Pages = rom[4] | (rom[5] << 8);
		var bank1Pages = rom[6] | (rom[7] << 8);

		Assert.Equal(512, bank0Pages);
		Assert.Equal(256, bank1Pages);
		Assert.Equal(64 + 131072 + 65536, rom.Length);
	}

	[Fact]
	public void RomBuilder_SetsRotation() {
		var builder = new AtariLynxRomBuilder(
			bank0Size: 131072,
			gameName: "Rotated Game",
			rotation: LynxRotation.Left);
		var rom = builder.Build();

		Assert.Equal(1, rom[58]); // Left rotation = 1
	}

	[Fact]
	public void RomBuilder_SetsVersion() {
		var builder = new AtariLynxRomBuilder(
			bank0Size: 131072,
			gameName: "Version Test",
			version: 2);
		var rom = builder.Build();

		var version = rom[8] | (rom[9] << 8);
		Assert.Equal(2, version);
	}

	[Fact]
	public void RomBuilder_BuildRaw_ExcludesHeader() {
		var builder = new AtariLynxRomBuilder(bank0Size: 1024, gameName: "Raw Test");
		var data = new byte[] { 0xea, 0xea, 0xea }; // NOP NOP NOP

		builder.AddSegment(0, data);
		var raw = builder.BuildRaw();

		Assert.Equal(1024, raw.Length); // No 64-byte header
		Assert.Equal(0xea, raw[0]);
		Assert.Equal(0xea, raw[1]);
		Assert.Equal(0xea, raw[2]);
	}

	[Fact]
	public void RomBuilder_AddSegmentToBank1() {
		var builder = new AtariLynxRomBuilder(
			bank0Size: 1024,
			bank1Size: 1024,
			gameName: "Multi-Bank");
		var data = new byte[] { 0xaa, 0xbb, 0xcc };

		builder.AddSegment(0, data, bank: 1);
		var rom = builder.Build();

		// Bank 1 data starts after header (64) + bank0 (1024)
		Assert.Equal(0xaa, rom[64 + 1024]);
		Assert.Equal(0xbb, rom[64 + 1024 + 1]);
		Assert.Equal(0xcc, rom[64 + 1024 + 2]);
	}

	[Fact]
	public void RomBuilder_SetsManufacturer() {
		var builder = new AtariLynxRomBuilder(
			bank0Size: 1024,
			gameName: "Test",
			manufacturer: "Poppy Dev");
		var rom = builder.Build();

		// Manufacturer at offset 42-57
		var mfg = System.Text.Encoding.ASCII.GetString(rom, 42, 9);
		Assert.Equal("Poppy Dev", mfg);
	}

	// ========================================================================
	// Instruction Set Tests
	// ========================================================================

	[Fact]
	public void InstructionSet65SC02_SupportsNewInstructions() {
		Assert.True(InstructionSet65SC02.TryGetEncoding("bra", Poppy.Core.Parser.AddressingMode.Relative, out var bra));
		Assert.Equal(0x80, bra.Opcode);

		Assert.True(InstructionSet65SC02.TryGetEncoding("phx", Poppy.Core.Parser.AddressingMode.Implied, out var phx));
		Assert.Equal(0xda, phx.Opcode);

		Assert.True(InstructionSet65SC02.TryGetEncoding("phy", Poppy.Core.Parser.AddressingMode.Implied, out var phy));
		Assert.Equal(0x5a, phy.Opcode);

		Assert.True(InstructionSet65SC02.TryGetEncoding("stz", Poppy.Core.Parser.AddressingMode.ZeroPage, out var stz));
		Assert.Equal(0x64, stz.Opcode);
	}

	[Fact]
	public void InstructionSet65SC02_Supports6502Instructions() {
		// Should fall back to 6502 for standard instructions
		Assert.True(InstructionSet65SC02.TryGetEncoding("lda", Poppy.Core.Parser.AddressingMode.Immediate, out var lda));
		Assert.Equal(0xa9, lda.Opcode);

		Assert.True(InstructionSet65SC02.TryGetEncoding("sta", Poppy.Core.Parser.AddressingMode.ZeroPage, out var sta));
		Assert.Equal(0x85, sta.Opcode);
	}

	[Fact]
	public void InstructionSet65SC02_IsBranchInstruction() {
		Assert.True(InstructionSet65SC02.IsBranchInstruction("bra"));
		Assert.True(InstructionSet65SC02.IsBranchInstruction("beq"));
		Assert.True(InstructionSet65SC02.IsBranchInstruction("bne"));
		Assert.False(InstructionSet65SC02.IsBranchInstruction("lda"));
	}

	[Fact]
	public void InstructionSet65SC02_ReturnsAllMnemonics() {
		var mnemonics = InstructionSet65SC02.GetAllMnemonics().ToList();

		// Should have both 65SC02 and 6502 mnemonics
		Assert.Contains("bra", mnemonics, StringComparer.OrdinalIgnoreCase);
		Assert.Contains("stz", mnemonics, StringComparer.OrdinalIgnoreCase);
		Assert.Contains("lda", mnemonics, StringComparer.OrdinalIgnoreCase);
		Assert.Contains("sta", mnemonics, StringComparer.OrdinalIgnoreCase);
	}

	// ========================================================================
	// Integration Tests
	// ========================================================================

	[Fact]
	public void Generate_Lynx_CompleteProgram() {
		var source = @"
.target lynx
.org $0200

start:
	; Clear screen using new 65SC02 features
	ldx #$00
	lda #$00
clear_loop:
	stz $c000,x  ; Use STZ instead of LDA #0 / STA
	inx
	bne clear_loop

	; Use BRA for infinite loop
main_loop:
	nop
	bra main_loop
";
		var (code, gen) = GenerateAtariLynxCode(source);

		Assert.False(gen.HasErrors);
		Assert.True(code.Length > 64); // Has header + code

		// Verify header
		Assert.Equal(0x4c, code[0]); // 'L'
		Assert.Equal(0x59, code[1]); // 'Y'
		Assert.Equal(0x4e, code[2]); // 'N'
		Assert.Equal(0x58, code[3]); // 'X'
	}
}
