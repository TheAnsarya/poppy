// ============================================================================
// PceRomGenerationTests.cs - PC Engine / TurboGrafx-16 ROM Generation Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Integration;

/// <summary>
/// Integration tests for PC Engine / TurboGrafx-16 ROM generation.
/// Verifies the full pipeline: lexer → parser → semantic analyzer → code generator
/// produces correct HuC6280 binary output with proper ROM layout and vectors.
/// </summary>
public sealed class PceRomGenerationTests {
	[Fact]
	public void Generate_MinimalPceRom_CreatesCorrectBinary() {
		// arrange - minimal PCE ROM with reset code
		var source = @"
.target tg16

.org $e000
reset:
	sei
	cld
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.HuC6280);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.HuC6280);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// PCE ROMs are bare binary with default size 32KB ($8000)
		Assert.Equal(0x8000, binary.Length);

		// Code at .org $e000 maps to ROM offset $6000 ($e000 & $7fff)
		Assert.Equal(0x78, binary[0x6000]);  // sei
		Assert.Equal(0xd8, binary[0x6001]);  // cld
		Assert.Equal(0xea, binary[0x6002]);  // nop
	}

	[Fact]
	public void Generate_PceRom_HasCorrectDefaultVectors() {
		// arrange - minimal PCE ROM
		var source = @"
.target tg16

.org $e000
reset:
	sei
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.HuC6280);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.HuC6280);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Vectors are at romSize-10 = $7ff6
		// All default vectors point to $e000
		int vectorBase = 0x7ff6;

		// IRQ2/BRK vector ($fff6)
		Assert.Equal(0x00, binary[vectorBase + 0]);     // low byte of $e000
		Assert.Equal(0xe0, binary[vectorBase + 1]);     // high byte of $e000

		// IRQ1/VDC vector ($fff8)
		Assert.Equal(0x00, binary[vectorBase + 2]);
		Assert.Equal(0xe0, binary[vectorBase + 3]);

		// Timer vector ($fffa)
		Assert.Equal(0x00, binary[vectorBase + 4]);
		Assert.Equal(0xe0, binary[vectorBase + 5]);

		// NMI vector ($fffc)
		Assert.Equal(0x00, binary[vectorBase + 6]);
		Assert.Equal(0xe0, binary[vectorBase + 7]);

		// Reset vector ($fffe)
		Assert.Equal(0x00, binary[vectorBase + 8]);
		Assert.Equal(0xe0, binary[vectorBase + 9]);
	}

	[Fact]
	public void Generate_HuC6280SpecificInstructions_EncodesCorrectly() {
		// arrange - PCE ROM with HuC6280-specific instructions
		var source = @"
.target huc6280

.org $e000
reset:
	sei             ; $78
	csh             ; $d4 - clock speed high (HuC6280)
	cld             ; $d8
	ldx #$ff        ; $a2, $ff
	txs             ; $9a
	lda #$ff        ; $a9, $ff
	tam #$01        ; $53, $01 - set MPR1 (HuC6280)
	st0 #$00        ; $03, $00 - VDC port write (HuC6280)
loop:
	jmp loop        ; $4c, $0c, $e0
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.HuC6280);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.HuC6280);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Code at .org $e000 maps to ROM offset $6000
		int offset = 0x6000;

		Assert.Equal(0x78, binary[offset]);      // sei
		Assert.Equal(0xd4, binary[offset + 1]);  // csh (HuC6280-specific)
		Assert.Equal(0xd8, binary[offset + 2]);  // cld
		Assert.Equal(0xa2, binary[offset + 3]);  // ldx
		Assert.Equal(0xff, binary[offset + 4]);  // #$ff
		Assert.Equal(0x9a, binary[offset + 5]);  // txs
		Assert.Equal(0xa9, binary[offset + 6]);  // lda
		Assert.Equal(0xff, binary[offset + 7]);  // #$ff
		Assert.Equal(0x53, binary[offset + 8]);  // tam (HuC6280-specific)
		Assert.Equal(0x01, binary[offset + 9]);  // #$01
		Assert.Equal(0x03, binary[offset + 10]); // st0 (HuC6280-specific)
		Assert.Equal(0x00, binary[offset + 11]); // #$00
		Assert.Equal(0x4c, binary[offset + 12]); // jmp
		Assert.Equal(0x0c, binary[offset + 13]); // low byte of $e00c (loop)
		Assert.Equal(0xe0, binary[offset + 14]); // high byte of $e00c (loop)
	}

	[Fact]
	public void Generate_PceTargetAlias_AllAliasesWork() {
		// All target aliases should produce the same result
		string[] aliases = ["tg16", "turbografx16", "pcengine", "huc6280"];

		foreach (var alias in aliases) {
			var source = $@"
.target {alias}

.org $e000
	sei
	nop
";
			var lexer = new Core.Lexer.Lexer(source, "test.pasm");
			var tokens = lexer.Tokenize();
			var parser = new Core.Parser.Parser(tokens);
			var program = parser.Parse();

			var analyzer = new SemanticAnalyzer(TargetArchitecture.HuC6280);
			analyzer.Analyze(program);

			var generator = new CodeGenerator(analyzer, TargetArchitecture.HuC6280);
			var binary = generator.Generate(program);

			Assert.False(analyzer.HasErrors, $"Alias '{alias}' had analyzer errors: {GetErrorsString(analyzer)}");
			Assert.False(generator.HasErrors, $"Alias '{alias}' had generator errors: {GetErrorsString(generator)}");

			// All should produce valid 32KB PCE ROM
			Assert.Equal(0x8000, binary.Length);

			// Code should be at correct offset
			Assert.Equal(0x78, binary[0x6000]);  // sei
			Assert.Equal(0xea, binary[0x6001]);  // nop
		}
	}

	[Fact]
	public void Generate_PceRom_UnusedSpaceFilledWithFF() {
		// arrange - minimal ROM to verify fill pattern
		var source = @"
.target tg16

.org $e000
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.HuC6280);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.HuC6280);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Unused ROM space should be $ff
		Assert.Equal(0xff, binary[0]);       // start of ROM
		Assert.Equal(0xff, binary[0x5fff]);  // just before code

		// Code location
		Assert.Equal(0xea, binary[0x6000]);  // nop

		// After code, before vectors, should be $ff
		Assert.Equal(0xff, binary[0x6002]);
	}

	[Fact]
	public void Generate_CompletePceRom_FullStartupSequence() {
		// arrange - complete bootable PCE ROM with typical startup
		var source = @"
.target tg16

.org $e000
reset:
	sei             ; disable interrupts
	csh             ; switch to high-speed clock (7.16 MHz)
	cld             ; clear decimal mode
	ldx #$ff
	txs             ; set up stack

	; set up memory page registers
	lda #$ff
	tam #$01        ; MPR1 = $ff (I/O page)
	lda #$f8
	tam #$02        ; MPR2 = $f8 (RAM page)

	; initialize VDC
	st0 #$00        ; VDC register select: write address low

	; enter infinite loop
loop:
	bra loop        ; branch to self
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.HuC6280);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.HuC6280);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		Assert.Equal(0x8000, binary.Length);

		// Verify the complete startup sequence at ROM offset $6000
		int o = 0x6000;
		Assert.Equal(0x78, binary[o]);       // sei
		Assert.Equal(0xd4, binary[o + 1]);   // csh
		Assert.Equal(0xd8, binary[o + 2]);   // cld
		Assert.Equal(0xa2, binary[o + 3]);   // ldx #$ff
		Assert.Equal(0xff, binary[o + 4]);
		Assert.Equal(0x9a, binary[o + 5]);   // txs
		Assert.Equal(0xa9, binary[o + 6]);   // lda #$ff
		Assert.Equal(0xff, binary[o + 7]);
		Assert.Equal(0x53, binary[o + 8]);   // tam #$01
		Assert.Equal(0x01, binary[o + 9]);
		Assert.Equal(0xa9, binary[o + 10]);  // lda #$f8
		Assert.Equal(0xf8, binary[o + 11]);
		Assert.Equal(0x53, binary[o + 12]);  // tam #$02
		Assert.Equal(0x02, binary[o + 13]);
		Assert.Equal(0x03, binary[o + 14]);  // st0 #$00
		Assert.Equal(0x00, binary[o + 15]);
		Assert.Equal(0x80, binary[o + 16]);  // bra loop (branch to self)
		Assert.Equal(0xfe, binary[o + 17]);  // relative offset -2

		// Verify reset vector points to $e000
		Assert.Equal(0x00, binary[0x7ffe]);  // reset vector low
		Assert.Equal(0xe0, binary[0x7fff]);  // reset vector high

		// Verify ROM is valid
		Assert.True(TurboGrafxRomBuilder.ValidateRom(binary));
	}

	[Fact]
	public void Generate_PceRom_RegisterSwapAndSpeedControl() {
		// arrange - HuC6280-specific register swap and speed control
		var source = @"
.target huc6280

.org $e000
reset:
	sei
	csh             ; $d4 - high-speed mode
	sax             ; $22 - swap A and X
	say             ; $42 - swap A and Y
	sxy             ; $02 - swap X and Y
	set             ; $f4 - set T flag
	csl             ; $54 - low-speed mode
	rts
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.HuC6280);
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.HuC6280);
		var binary = generator.Generate(program);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		int o = 0x6000;
		Assert.Equal(0x78, binary[o]);       // sei
		Assert.Equal(0xd4, binary[o + 1]);   // csh
		Assert.Equal(0x22, binary[o + 2]);   // sax
		Assert.Equal(0x42, binary[o + 3]);   // say
		Assert.Equal(0x02, binary[o + 4]);   // sxy
		Assert.Equal(0xf4, binary[o + 5]);   // set
		Assert.Equal(0x54, binary[o + 6]);   // csl
		Assert.Equal(0x60, binary[o + 7]);   // rts
	}

	[Fact]
	public void Generate_PceRom_VdcStoreInstructions() {
		// arrange - ST0/ST1/ST2 VDC port writes
		var source = @"
.target tg16

.org $e000
reset:
	sei
	st0 #$00        ; $03, $00 - select VDC register 0 (MAWR)
	st1 #$00        ; $13, $00 - write data low byte
	st2 #$10        ; $23, $10 - write data high byte
	st0 #$05        ; $03, $05 - select VDC register 5 (CR)
	st1 #$cc        ; $13, $cc - write control register low
	st2 #$00        ; $23, $00 - write control register high
	rts
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.HuC6280);
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.HuC6280);
		var binary = generator.Generate(program);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		int o = 0x6000;
		Assert.Equal(0x78, binary[o]);       // sei
		Assert.Equal(0x03, binary[o + 1]);   // st0
		Assert.Equal(0x00, binary[o + 2]);   // #$00
		Assert.Equal(0x13, binary[o + 3]);   // st1
		Assert.Equal(0x00, binary[o + 4]);   // #$00
		Assert.Equal(0x23, binary[o + 5]);   // st2
		Assert.Equal(0x10, binary[o + 6]);   // #$10
		Assert.Equal(0x03, binary[o + 7]);   // st0
		Assert.Equal(0x05, binary[o + 8]);   // #$05
		Assert.Equal(0x13, binary[o + 9]);   // st1
		Assert.Equal(0xcc, binary[o + 10]);  // #$cc
		Assert.Equal(0x23, binary[o + 11]);  // st2
		Assert.Equal(0x00, binary[o + 12]);  // #$00
		Assert.Equal(0x60, binary[o + 13]);  // rts
	}

	[Fact]
	public void Generate_PceRom_65C02ExtensionInstructions() {
		// arrange - 65C02 extensions available on HuC6280
		var source = @"
.target huc6280

.org $e000
reset:
	sei
	phx             ; $da - push X
	phy             ; $5a - push Y
	stz $00         ; $64, $00 - store zero to zp
	inc a           ; $1a - INC A
	dec a           ; $3a - DEC A
	ply             ; $7a - pull Y
	plx             ; $fa - pull X
	bra skip        ; $80, $02 - unconditional branch
	nop
	nop
skip:
	rts
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.HuC6280);
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.HuC6280);
		var binary = generator.Generate(program);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		int o = 0x6000;
		Assert.Equal(0x78, binary[o]);       // sei
		Assert.Equal(0xda, binary[o + 1]);   // phx
		Assert.Equal(0x5a, binary[o + 2]);   // phy
		Assert.Equal(0x64, binary[o + 3]);   // stz zp
		Assert.Equal(0x00, binary[o + 4]);   // $00
		Assert.Equal(0x1a, binary[o + 5]);   // inc A
		Assert.Equal(0x3a, binary[o + 6]);   // dec A
		Assert.Equal(0x7a, binary[o + 7]);   // ply
		Assert.Equal(0xfa, binary[o + 8]);   // plx
		Assert.Equal(0x80, binary[o + 9]);   // bra
		Assert.Equal(0x02, binary[o + 10]);  // +2 (skip over 2 nops)
		Assert.Equal(0xea, binary[o + 11]);  // nop
		Assert.Equal(0xea, binary[o + 12]);  // nop
		Assert.Equal(0x60, binary[o + 13]);  // rts (skip label)
	}

	[Fact]
	public void Generate_PceRom_MemoryMapping() {
		// arrange - TAM/TMA memory mapping instructions
		var source = @"
.target tg16

.org $e000
reset:
	sei
	cld
	; Map I/O to MPR1 ($2000-$3fff)
	lda #$ff
	tam #$02        ; $53, $02 - MPR1
	; Map RAM to MPR2 ($4000-$5fff)
	lda #$f8
	tam #$04        ; $53, $04 - MPR2
	; Read MPR7 (reset page)
	tma #$80        ; $43, $80 - read MPR7
	rts
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.HuC6280);
		analyzer.Analyze(program);

		var generator = new CodeGenerator(analyzer, TargetArchitecture.HuC6280);
		var binary = generator.Generate(program);

		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		int o = 0x6000;
		Assert.Equal(0x78, binary[o]);       // sei
		Assert.Equal(0xd8, binary[o + 1]);   // cld
		Assert.Equal(0xa9, binary[o + 2]);   // lda #$ff
		Assert.Equal(0xff, binary[o + 3]);
		Assert.Equal(0x53, binary[o + 4]);   // tam #$02
		Assert.Equal(0x02, binary[o + 5]);
		Assert.Equal(0xa9, binary[o + 6]);   // lda #$f8
		Assert.Equal(0xf8, binary[o + 7]);
		Assert.Equal(0x53, binary[o + 8]);   // tam #$04
		Assert.Equal(0x04, binary[o + 9]);
		Assert.Equal(0x43, binary[o + 10]);  // tma #$80
		Assert.Equal(0x80, binary[o + 11]);
		Assert.Equal(0x60, binary[o + 12]);  // rts
	}

	/// <summary>
	/// Helper to get error messages for test output.
	/// </summary>
	private static string GetErrorsString(SemanticAnalyzer analyzer) {
		if (!analyzer.HasErrors) return string.Empty;
		return string.Join("\n", analyzer.Errors.Select(e => e.Message));
	}

	/// <summary>
	/// Helper to get error messages for test output.
	/// </summary>
	private static string GetErrorsString(CodeGenerator generator) {
		if (!generator.HasErrors) return string.Empty;
		return string.Join("\n", generator.Errors.Select(e => e.Message));
	}
}
