// ============================================================================
// GbRomGenerationTests.cs - Game Boy ROM Generation Integration Tests
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Integration;

/// <summary>
/// Integration tests for Game Boy ROM generation with headers.
/// </summary>
/// <remarks>
/// Note: Full ROM layout integration (placing header at $0100 and code at $0150)
/// is pending implementation of CodeGenerator + GbRomBuilder integration.
/// These tests verify the header builder functionality works correctly.
/// </remarks>
public sealed class GbRomGenerationTests {
	[Fact]
	public void Generate_WithGbHeader_IncludesHeaderInOutput() {
		// arrange - minimal Game Boy ROM with header directives
		var source = @"
.gb
.gb_title ""TESTROM""
.gb_cartridge_type 0
.gb_rom_size 32
.gb_region 0

.org $0
start:
	di
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.SM83);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.SM83);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Header is prepended, so check header content at start
		// Currently the header is simply prepended to the binary
		// Entry point should be nop; jp $0150
		Assert.Equal(0x00, binary[0]);   // nop
		Assert.Equal(0xc3, binary[1]);   // jp
		Assert.Equal(0x50, binary[2]);   // $0150 low
		Assert.Equal(0x01, binary[3]);   // $0150 high

		// Nintendo logo at offset 4 (within prepended header)
		Assert.Equal(0xce, binary[4]);   // First byte of Nintendo logo
		Assert.Equal(0xed, binary[5]);   // Second byte
	}

	[Fact]
	public void Generate_WithoutGbHeader_GeneratesRawBinary() {
		// arrange - Game Boy code without header directives
		var source = @"
.gb
.org $0
start:
	di
	nop
	nop
";
		var lexer = new Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Core.Parser.Parser(tokens);
		var program = parser.Parse();

		var analyzer = new SemanticAnalyzer(TargetArchitecture.SM83);
		analyzer.Analyze(program);

		// act
		var generator = new CodeGenerator(analyzer, TargetArchitecture.SM83);
		var binary = generator.Generate(program);

		// assert
		Assert.False(analyzer.HasErrors, GetErrorsString(analyzer));
		Assert.False(generator.HasErrors, GetErrorsString(generator));

		// Without header directives, code should be raw
		// di = $f3, nop = $00
		Assert.Equal(0xf3, binary[0]);  // di
		Assert.Equal(0x00, binary[1]);  // nop
		Assert.Equal(0x00, binary[2]);  // nop
	}

	[Fact]
	public void GbHeaderBuilder_CgbMode_SetsCorrectFlag() {
		// Test the header builder directly
		var header = new GbHeaderBuilder()
			.SetTitle("CGBTEST")
			.SetCgbMode(GbCgbMode.CgbCompatible)
			.SetCartridgeType(GbCartridgeType.RomOnly)
			.SetRomSize(32)
			.Build();

		// CGB flag at header offset 0x43 (within 80-byte header)
		Assert.Equal(0x80, header[0x43]);
	}

	[Fact]
	public void GbHeaderBuilder_CartridgeType_SetsCorrectValue() {
		// Test the header builder directly
		var header = new GbHeaderBuilder()
			.SetTitle("MBC1TEST")
			.SetCartridgeType(GbCartridgeType.Mbc1RamBattery)
			.SetRomSize(64)
			.SetRamSize(8)
			.Build();

		// Cartridge type at header offset 0x47
		Assert.Equal(0x03, header[0x47]);

		// ROM size at header offset 0x48 (64KB = code 0x01)
		Assert.Equal(0x01, header[0x48]);

		// RAM size at header offset 0x49 (8KB = code 0x02)
		Assert.Equal(0x02, header[0x49]);
	}

	[Fact]
	public void GbHeaderBuilder_Checksum_IsCalculatedCorrectly() {
		// Test the header builder checksum calculation
		var header = new GbHeaderBuilder()
			.SetTitle("CHECKSUMTEST")
			.SetCartridgeType(GbCartridgeType.RomOnly)
			.SetRomSize(32)
			.Build();

		// Manually verify header checksum at offset 0x4d
		int checksum = 0;
		for (var i = 0x34; i <= 0x4c; i++) {
			checksum = checksum - header[i] - 1;
		}
		checksum &= 0xff;

		Assert.Equal(checksum, header[0x4d]);
	}

	[Fact]
	public void GbHeaderBuilder_NintendoLogo_IsIncluded() {
		// Test the header builder includes the Nintendo logo
		var header = new GbHeaderBuilder()
			.SetTitle("LOGOTEST")
			.Build();

		// Nintendo logo at header offset 4-51 (48 bytes)
		byte[] expectedLogo = [
			0xce, 0xed, 0x66, 0x66, 0xcc, 0x0d, 0x00, 0x0b,
			0x03, 0x73, 0x00, 0x83, 0x00, 0x0c, 0x00, 0x0d,
			0x00, 0x08, 0x11, 0x1f, 0x88, 0x89, 0x00, 0x0e,
			0xdc, 0xcc, 0x6e, 0xe6, 0xdd, 0xdd, 0xd9, 0x99,
			0xbb, 0xbb, 0x67, 0x63, 0x6e, 0x0e, 0xec, 0xcc,
			0xdd, 0xdc, 0x99, 0x9f, 0xbb, 0xb9, 0x33, 0x3e
		];

		for (var i = 0; i < expectedLogo.Length; i++) {
			Assert.Equal(expectedLogo[i], header[4 + i]);
		}
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
