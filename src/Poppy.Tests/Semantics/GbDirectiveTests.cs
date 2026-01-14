using Xunit;
using Poppy.Core.Semantics;
using Poppy.Core.CodeGen;
using Poppy.Core;

namespace Poppy.Tests.Semantics;

/// <summary>
/// Tests for Game Boy header directives (.gb_*).
/// </summary>
public class GbDirectiveTests
{
	[Fact]
	public void GbTitle_ValidTitle_SetsTitle()
	{
		// Arrange
		var source = @"
			.gb
			.gb_title ""TETRIS""
		";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// Act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.SM83);
		analyzer.Analyze(program);

		// Assert
		Assert.False(analyzer.HasErrors);
		var builder = analyzer.GetGbHeaderBuilder();
		Assert.NotNull(builder);
		var header = builder.Build();
		// Title starts at offset 0x34 in 80-byte header
		var titleBytes = header[0x34..0x3C];
		var title = System.Text.Encoding.ASCII.GetString(titleBytes).TrimEnd('\0');
		Assert.Equal("TETRIS", title);
	}

	[Fact]
	public void GbCgb_CgbCompatible_SetsCgbMode()
	{
		// Arrange
		var source = @"
			.gb
			.gb_cgb 1
		";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// Act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.SM83);
		analyzer.Analyze(program);

		// Assert
		Assert.False(analyzer.HasErrors);
		var builder = analyzer.GetGbHeaderBuilder();
		Assert.NotNull(builder);
		var header = builder.Build();
		// CGB flag at offset 0x43
		Assert.Equal(0x80, header[0x43]); // CGB compatible
	}

	[Fact]
	public void GbCartridgeType_Mbc1_SetsCartridgeType()
	{
		// Arrange
		var source = @"
			.gb
			.gb_cartridge_type 1
		";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// Act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.SM83);
		analyzer.Analyze(program);

		// Assert
		Assert.False(analyzer.HasErrors);
		var builder = analyzer.GetGbHeaderBuilder();
		Assert.NotNull(builder);
		var header = builder.Build();
		// Cartridge type at offset 0x47
		Assert.Equal(0x01, header[0x47]); // MBC1
	}

	[Fact]
	public void GbRomSize_64KB_SetsRomSize()
	{
		// Arrange
		var source = @"
			.gb
			.gb_rom_size 64
		";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// Act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.SM83);
		analyzer.Analyze(program);

		// Assert
		Assert.False(analyzer.HasErrors);
		var builder = analyzer.GetGbHeaderBuilder();
		Assert.NotNull(builder);
		var header = builder.Build();
		// ROM size at offset 0x48
		Assert.Equal(0x01, header[0x48]); // 64KB (code 0x01)
	}

	[Fact]
	public void MultipleGbDirectives_BuildsCompleteHeader()
	{
		// Arrange
		var source = @"
			.gb
			.gb_title ""POKEMON RED""
			.gb_cgb 1
			.gb_sgb 1
			.gb_cartridge_type 3
			.gb_rom_size 512
			.gb_ram_size 32
			.gb_region 1
			.gb_version 10
		";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// Act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.SM83);
		analyzer.Analyze(program);

		// Assert
		Assert.False(analyzer.HasErrors);
		var builder = analyzer.GetGbHeaderBuilder();
		Assert.NotNull(builder);
		var header = builder.Build();
		
		// Verify header fields
		var title = System.Text.Encoding.ASCII.GetString(header[0x34..0x3E]).TrimEnd('\0');
		Assert.StartsWith("POKEMON RE", title); // Title is 11 chars max when CGB flag present
		Assert.Equal(0x80, header[0x43]); // CGB compatible
		Assert.Equal(0x03, header[0x46]); // SGB enabled
		Assert.Equal(0x03, header[0x47]); // MBC1+RAM+BATTERY
		Assert.Equal(0x04, header[0x48]); // 512KB ROM (code 0x04)
		Assert.Equal(0x03, header[0x49]); // 32KB RAM
		Assert.Equal(0x01, header[0x4A]); // International
		Assert.Equal(10, header[0x4C]); // Version 10
	}
}
