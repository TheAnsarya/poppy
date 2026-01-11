using Xunit;
using Poppy.Core.Semantics;
using Poppy.Core.CodeGen;

namespace Poppy.Tests.Semantics;

public class INesDirectiveTests
{
	[Fact]
	public void INes_Prg_SetsPrgRomSize()
	{
		// arrange
		var source = @"
.nes
.ines_prg 2
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetINesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();
		Assert.Equal(2, header[4]);		// byte 4: PRG ROM size
	}

	[Fact]
	public void INes_Chr_SetsChrRomSize()
	{
		// arrange
		var source = @"
.nes
.ines_chr 1
";
		var lexer = new Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetINesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();
		Assert.Equal(1, header[5]);		// byte 5: CHR ROM size
	}

	[Fact]
	public void INes_Mapper_SetsMapperNumber()
	{
		// arrange
		var source = @"
.nes
.ines_mapper 1
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetINesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();
		Assert.Equal(0x11, header[6]);	// flags 6: mapper 1 low nybble
	}

	[Fact]
	public void INes_Submapper_SetsSubmapperNumber()
	{
		// arrange
		var source = @"
.nes
.ines_mapper 1
.ines_submapper 5
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetINesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();
		Assert.Equal(0x05, header[8] & 0x0f);	// byte 8 low nybble: submapper
	}

	[Fact]
	public void INes_Mirroring_SetsHorizontalMirroring()
	{
		// arrange
		var source = @"
.nes
.ines_mirroring 0
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetINesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();
		Assert.Equal(0x00, header[6] & 0x01);	// mirroring bit should be clear
	}

	[Fact]
	public void INes_Mirroring_SetsVerticalMirroring()
	{
		// arrange
		var source = @"
.nes
.ines_mirroring 1
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetINesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();
		Assert.Equal(0x01, header[6] & 0x01);	// mirroring bit should be set
	}

	[Fact]
	public void INes_Battery_SetsBatteryFlag()
	{
		// arrange
		var source = @"
.nes
.ines_battery
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetINesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();
		Assert.Equal(0x02, header[6] & 0x02);	// battery bit should be set
	}

	[Fact]
	public void INes_FourScreen_SetsFourScreenFlag()
	{
		// arrange
		var source = @"
.nes
.ines_fourscreen
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetINesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();
		Assert.Equal(0x08, header[6] & 0x08);	// four-screen bit should be set
	}

	[Fact]
	public void INes_Pal_SetsPalFlag()
	{
		// arrange
		var source = @"
.nes
.ines_pal
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetINesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();
		Assert.Equal(1, header[12]);	// byte 12: PAL timing
	}

	[Fact]
	public void INes_CompleteHeader_BuildsCorrectly()
	{
		// arrange - simulate Super Mario Bros. 3 header
		var source = @"
.nes
.ines_prg 32        ; 512KB PRG ROM
.ines_chr 16        ; 128KB CHR ROM
.ines_mapper 4      ; MMC3
.ines_mirroring 0   ; horizontal
.ines_battery
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetINesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();
		
		Assert.Equal(0x4e, header[0]);	// 'N'
		Assert.Equal(0x45, header[1]);	// 'E'
		Assert.Equal(0x53, header[2]);	// 'S'
		Assert.Equal(0x1a, header[3]);	// MS-DOS EOF
		Assert.Equal(32, header[4]);	// PRG ROM size
		Assert.Equal(16, header[5]);	// CHR ROM size
		Assert.Equal(0x42, header[6]);	// flags 6: horizontal, battery, mapper 4 low
		Assert.Equal(0x08, header[7]);	// flags 7: iNES 2.0
	}

	[Fact]
	public void INes_WithoutNesTarget_ReportsError()
	{
		// arrange
		var source = @"
.snes
.ines_prg 2
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("only valid for NES"));
	}

	[Fact]
	public void INes_ReturnsNull_WhenNoDirectivesUsed()
	{
		// arrange
		var source = @"
.nes
nop
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.asm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetINesHeaderBuilder();
		Assert.Null(headerBuilder);
	}
}
