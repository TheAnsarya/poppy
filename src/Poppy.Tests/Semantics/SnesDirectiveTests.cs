using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Semantics;

/// <summary>
/// Tests for SNES header directives (.snes_title, .snes_region, etc.).
/// </summary>
public class SnesDirectiveTests {
	[Fact]
	public void SnesTitle_SetsTitle() {
		// arrange
		var source = @"
.snes
.lorom
.snes_title ""CHRONO TRIGGER""
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetSnesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();

		// Title is at offset 0 in the header (first 21 bytes)
		var title = System.Text.Encoding.ASCII.GetString(header, 0, 21).TrimEnd();
		Assert.Equal("CHRONO TRIGGER", title);
	}

	[Fact]
	public void SnesTitle_TooLong_ReportsError() {
		// arrange
		var source = @"
.snes
.lorom
.snes_title ""THIS TITLE IS WAY TOO LONG FOR SNES HEADER""
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("too long"));
	}

	[Fact]
	public void SnesRegion_Japan_SetsRegion() {
		// arrange
		var source = @"
.snes
.lorom
.snes_region ""Japan""
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetSnesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();

		// Region code is at offset 25
		Assert.Equal(0x00, header[25]);  // Japan = 0x00
	}

	[Fact]
	public void SnesRegion_USA_SetsRegion() {
		// arrange
		var source = @"
.snes
.lorom
.snes_region ""USA""
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetSnesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();

		// Region code is at offset 25
		Assert.Equal(0x01, header[25]);  // USA = 0x01
	}

	[Fact]
	public void SnesRegion_Invalid_ReportsError() {
		// arrange
		var source = @"
.snes
.lorom
.snes_region ""Mars""
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("not valid"));
	}

	[Fact]
	public void SnesVersion_SetsVersion() {
		// arrange
		var source = @"
.snes
.lorom
.snes_version 2
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetSnesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();

		// Version is at offset 27
		Assert.Equal(2, header[27]);
	}

	[Fact]
	public void SnesVersion_OutOfRange_ReportsError() {
		// arrange
		var source = @"
.snes
.lorom
.snes_version 256
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("0-255"));
	}

	[Fact]
	public void SnesRomSize_512KB_SetsRomSize() {
		// arrange
		var source = @"
.snes
.lorom
.snes_rom_size 512
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetSnesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();

		// ROM size is at offset 23
		Assert.Equal(0x09, header[23]);  // 512KB = 2^9 * 1024 bytes
	}

	[Fact]
	public void SnesRomSize_NotPowerOf2_ReportsError() {
		// arrange
		var source = @"
.snes
.lorom
.snes_rom_size 300
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("power of 2"));
	}

	[Fact]
	public void SnesRamSize_8KB_SetsRamSize() {
		// arrange
		var source = @"
.snes
.lorom
.snes_ram_size 8
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetSnesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();

		// RAM size is at offset 24
		Assert.Equal(0x03, header[24]);  // 8KB = code 0x03
	}

	[Fact]
	public void SnesRamSize_Invalid_ReportsError() {
		// arrange
		var source = @"
.snes
.lorom
.snes_ram_size 16
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("0, 2, 8, or 32"));
	}

	[Fact]
	public void SnesFastRom_Enabled_SetsFastRomBit() {
		// arrange
		var source = @"
.snes
.lorom
.snes_fastrom 1
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetSnesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();

		// Map mode is at offset 21 (bit 4 = FastROM)
		Assert.True((header[21] & 0x10) != 0);
	}

	[Fact]
	public void SnesDirective_OnNesTarget_ReportsError() {
		// arrange
		var source = @"
.nes
.snes_title ""INVALID""
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.True(analyzer.HasErrors);
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("only valid for SNES/65816"));
	}

	[Fact]
	public void MultipleDirectives_AllApplied() {
		// arrange
		var source = @"
.snes
.lorom
.snes_title ""FINAL FANTASY VI""
.snes_region ""USA""
.snes_version 1
.snes_rom_size 1024
.snes_ram_size 8
.snes_fastrom 1
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var headerBuilder = analyzer.GetSnesHeaderBuilder();
		Assert.NotNull(headerBuilder);
		var header = headerBuilder.Build();

		// Verify all settings
		var title = System.Text.Encoding.ASCII.GetString(header, 0, 21).TrimEnd();
		Assert.Equal("FINAL FANTASY VI", title);
		Assert.Equal(0x01, header[25]);  // USA region
		Assert.Equal(1, header[27]);     // Version 1
		Assert.Equal(0x0a, header[23]);  // 1024KB = 2^10
		Assert.Equal(0x03, header[24]);  // 8KB RAM
		Assert.True((header[21] & 0x10) != 0);  // FastROM enabled
	}
}
