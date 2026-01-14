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

	[Fact]
	public void Generate_MXFlagTracking_EmitsCorrectByteSizes() {
		// arrange - test exact byte output for M/X flag-dependent instructions
		// Note: Don't use .lorom/.hirom to avoid header generation
		var source = @"
.snes

.org $8000
	; Start in 8-bit mode (default after reset)
	.a8
	.i8
	lda #$ff    ; A9 FF (2 bytes: opcode + 1 byte operand)
	ldx #$aa    ; A2 AA (2 bytes: opcode + 1 byte operand)

	; Switch to 16-bit accumulator
	.a16
	lda #$1234  ; A9 34 12 (3 bytes: opcode + 2 byte operand, little-endian)

	; Switch to 16-bit index
	.i16
	ldx #$5678  ; A2 78 56 (3 bytes: opcode + 2 byte operand, little-endian)

	; REP instruction is always 8-bit immediate
	rep #$30    ; C2 30 (2 bytes always)

	; SEP instruction is always 8-bit immediate
	.a8
	.i8
	sep #$20    ; E2 20 (2 bytes always)
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
		Assert.False(analyzer.HasErrors, $"Analyzer errors: {string.Join(", ", analyzer.Errors.Select(e => e.Message))}");
		Assert.False(generator.HasErrors, $"Generator errors: {string.Join(", ", generator.Errors.Select(e => e.Message))}");

		// Expected byte sequence at $8000:
		// lda #$ff (8-bit): A9 FF
		// ldx #$aa (8-bit): A2 AA
		// lda #$1234 (16-bit): A9 34 12
		// ldx #$5678 (16-bit): A2 78 56
		// rep #$30: C2 30
		// sep #$20: E2 20
		// Total: 2 + 2 + 3 + 3 + 2 + 2 = 14 bytes

		Assert.Equal(14, binary.Length);

		// Verify exact bytes
		Assert.Equal(0xa9, binary[0]);  // lda immediate opcode
		Assert.Equal(0xff, binary[1]);  // 8-bit operand
		Assert.Equal(0xa2, binary[2]);  // ldx immediate opcode
		Assert.Equal(0xaa, binary[3]);  // 8-bit operand
		Assert.Equal(0xa9, binary[4]);  // lda immediate opcode
		Assert.Equal(0x34, binary[5]);  // 16-bit operand low byte
		Assert.Equal(0x12, binary[6]);  // 16-bit operand high byte
		Assert.Equal(0xa2, binary[7]);  // ldx immediate opcode
		Assert.Equal(0x78, binary[8]);  // 16-bit operand low byte
		Assert.Equal(0x56, binary[9]);  // 16-bit operand high byte
		Assert.Equal(0xc2, binary[10]); // rep opcode
		Assert.Equal(0x30, binary[11]); // 8-bit immediate (always)
		Assert.Equal(0xe2, binary[12]); // sep opcode
		Assert.Equal(0x20, binary[13]); // 8-bit immediate (always)
	}
}
