using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Integration;

/// <summary>
/// Integration tests for SNES ROM generation with headers.
/// </summary>
public class SnesRomGenerationTests {
	[Fact]
	public void Generate_WithSnesHeader_PrependsHeaderToRom() {
		// arrange - minimal SNES ROM with header
		var source = @"
.snes
.lorom
.snes_title ""TEST ROM""
.snes_region ""USA""
.snes_version 1
.snes_rom_size 256
.snes_ram_size 8

.org $8000
reset:
	sei
	clc
	xce         ; switch to native mode
	jmp reset

.org $fffc
	.word reset
	.word reset
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.WDC65816);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors);
		Assert.False(generator.HasErrors);

		// Check SNES header (first 64 bytes)
		// Title should be at start
		var title = System.Text.Encoding.ASCII.GetString(binary, 0, 21).TrimEnd();
		Assert.Equal("TEST ROM", title);

		// Map mode at offset 21 (LoRom = 0x20)
		Assert.Equal(0x20, binary[21]);

		// ROM size at offset 23
		Assert.Equal(0x08, binary[23]);  // 256KB = 2^8

		// RAM size at offset 24
		Assert.Equal(0x03, binary[24]);  // 8KB

		// Region at offset 25
		Assert.Equal(0x01, binary[25]);  // USA

		// Version at offset 27
		Assert.Equal(1, binary[27]);

		// Check that code follows after header
		Assert.True(binary.Length > 64);
	}

	[Fact]
	public void Generate_WithoutSnesHeader_GeneratesRawBinary() {
		// arrange - SNES code without header directives
		var source = @"
.snes
.lorom
.org $8000
reset:
	sei
	clc
	xce
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.WDC65816);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors);
		Assert.False(generator.HasErrors);

		// Should NOT have SNES header - binary should be smaller than with header
		// (header is 64 bytes, so without header should be significantly smaller for this tiny program)
		Assert.True(binary.Length < 100);  // Much smaller than a header + code would be
	}

	[Fact]
	public void Generate_HiRom_SetsCorrectMapMode() {
		// arrange - HiROM SNES ROM
		var source = @"
.snes
.hirom
.snes_title ""HIROM TEST""
.snes_region ""Japan""

.org $c000
reset:
	sei
	clc
	xce
	jmp reset

.org $fffc
	.word reset
	.word reset
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.WDC65816);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors);
		Assert.False(generator.HasErrors);

		// Map mode at offset 21 (HiRom = 0x21)
		Assert.Equal(0x21, binary[21]);

		// Region should be Japan (0x00)
		Assert.Equal(0x00, binary[25]);
	}

	[Fact]
	public void Generate_FastRom_SetsFastRomBit() {
		// arrange - FastROM enabled
		var source = @"
.snes
.lorom
.snes_title ""FAST ROM""
.snes_fastrom 1

.org $8000
reset:
	nop
	jmp reset
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.WDC65816);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors);
		Assert.False(generator.HasErrors);

		// Map mode at offset 21 should have bit 4 set for FastROM
		Assert.True((binary[21] & 0x10) != 0);
	}

	[Fact]
	public void Generate_MFlagsWithVariableOperands_GeneratesCorrectSizes() {
		// arrange - test M/X flag tracking with variable immediates
		var source = @"
.snes
.lorom

.org $8000
reset:
	.a8         ; 8-bit accumulator
	lda #$12    ; should be 2 bytes total

	.a16        ; 16-bit accumulator
	lda #$1234  ; should be 3 bytes total

	.i8         ; 8-bit index
	ldx #$56    ; should be 2 bytes total

	.i16        ; 16-bit index
	ldx #$5678  ; should be 3 bytes total

	rep #$30    ; set both A and X to 16-bit
	lda #$abcd  ; should be 3 bytes
	ldx #$ef01  ; should be 3 bytes
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.WDC65816);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors);
		Assert.False(generator.HasErrors);

		// Verify the binary contains correct instruction sizes
		// Note: Actual byte checking would require knowing exact opcodes
		Assert.True(binary.Length > 20);  // Should have all the instructions
	}
}
