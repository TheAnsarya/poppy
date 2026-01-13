using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Integration;

public class NesRomGenerationTests {
	[Fact]
	public void Generate_WithINesHeader_PrependsHeaderToRom() {
		// arrange - minimal NES ROM with iNES header
		var source = @"
.nes
.ines_prg 1         ; 16KB PRG ROM
.ines_chr 0         ; no CHR ROM
.ines_mapper 0      ; NROM mapper

.org $8000
reset:
	lda #$00
	sta $2000
	jmp reset

.org $fffc
	.word reset
	.word reset
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6502);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors);
		Assert.False(generator.HasErrors);

		// Check iNES header (first 16 bytes)
		Assert.Equal(0x4e, binary[0]);  // 'N'
		Assert.Equal(0x45, binary[1]);  // 'E'
		Assert.Equal(0x53, binary[2]);  // 'S'
		Assert.Equal(0x1a, binary[3]);  // MS-DOS EOF
		Assert.Equal(1, binary[4]);     // PRG ROM size
		Assert.Equal(0, binary[5]);     // CHR ROM size

		// Check that code follows after header
		Assert.True(binary.Length > 16);
	}

	[Fact]
	public void Generate_WithoutINesHeader_GeneratesRawBinary() {
		// arrange - NES code without iNES header directives
		var source = @"
.nes
.org $8000
reset:
	lda #$00
	sta $2000
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6502);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors);
		Assert.False(generator.HasErrors);

		// Should NOT have iNES header (first bytes should be machine code)
		// lda #$00 = $a9 $00
		Assert.NotEqual(0x4e, binary[0]);   // NOT 'N'
	}

	[Fact]
	public void Generate_CompleteNesRom_CreatesValidInesFile() {
		// arrange - more complete NES ROM
		var source = @"
.nes
.ines_prg 2         ; 32KB PRG ROM
.ines_chr 1         ; 8KB CHR ROM
.ines_mapper 0      ; NROM mapper
.ines_mirroring 1   ; vertical

.org $8000
reset:
	sei
	cld
	ldx #$ff
	txs
	lda #$00
	sta $2000
	sta $2001
vblankwait:
	bit $2002
	bpl vblankwait
	jmp main

main:
	lda #$01
	sta $2001
loop:
	jmp loop

nmi:
	rti

.org $fffa
	.word nmi
	.word reset
	.word 0
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6502);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors);
		Assert.False(generator.HasErrors);

		// Verify iNES header
		Assert.Equal(0x4e, binary[0]);  // 'N'
		Assert.Equal(0x45, binary[1]);  // 'E'
		Assert.Equal(0x53, binary[2]);  // 'S'
		Assert.Equal(0x1a, binary[3]);  // MS-DOS EOF
		Assert.Equal(2, binary[4]);     // 32KB PRG ROM
		Assert.Equal(1, binary[5]);     // 8KB CHR ROM
		Assert.Equal(0x01, binary[6] & 0x01);   // vertical mirroring

		// Verify binary size (header + PRG data)
		// Note: This is the actual assembled size, not necessarily 32KB + header
		Assert.True(binary.Length >= 16);   // at least has header
	}

	[Fact]
	public void Generate_SuperMarioBros3Style_CreatesCorrectHeader() {
		// arrange - SMB3-style header (MMC3, battery backup)
		var source = @"
.nes
.ines_prg 32        ; 512KB PRG ROM
.ines_chr 16        ; 128KB CHR ROM
.ines_mapper 4      ; MMC3
.ines_mirroring 0   ; horizontal
.ines_battery       ; battery-backed save RAM

.org $8000
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.MOS6502);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors);
		Assert.False(generator.HasErrors);

		// Verify iNES 2.0 header
		Assert.Equal(32, binary[4]);    // PRG ROM size
		Assert.Equal(16, binary[5]);    // CHR ROM size
		Assert.Equal(0x42, binary[6]);  // horizontal, battery, mapper 4 low
		Assert.Equal(0x08, binary[7]);  // iNES 2.0 identifier
	}
}
