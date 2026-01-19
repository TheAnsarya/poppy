// ============================================================================
// ConverterTests.cs - Unit Tests for Project Converters
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Converters;
using Xunit;

namespace Poppy.Tests.Converters;

/// <summary>
/// Tests for the ConverterFactory class.
/// </summary>
public class ConverterFactoryTests {
	[Fact]
	public void SupportedAssemblers_ContainsExpectedAssemblers() {
		var supported = ConverterFactory.SupportedAssemblers;

		Assert.Contains("ASAR", supported);
		Assert.Contains("CA65", supported);
		Assert.Contains("XKAS", supported);
	}

	[Theory]
	[InlineData("ASAR")]
	[InlineData("asar")]
	[InlineData("CA65")]
	[InlineData("ca65")]
	[InlineData("XKAS")]
	[InlineData("xkas")]
	public void Create_ValidAssembler_ReturnsConverter(string assembler) {
		var converter = ConverterFactory.Create(assembler);

		Assert.NotNull(converter);
	}

	[Fact]
	public void Create_InvalidAssembler_ThrowsArgumentException() {
		Assert.Throws<ArgumentException>(() => ConverterFactory.Create("invalid"));
	}

	[Fact]
	public void TryCreate_ValidAssembler_ReturnsTrue() {
		var result = ConverterFactory.TryCreate("ASAR", out var converter);

		Assert.True(result);
		Assert.NotNull(converter);
	}

	[Fact]
	public void TryCreate_InvalidAssembler_ReturnsFalse() {
		var result = ConverterFactory.TryCreate("invalid", out var converter);

		Assert.False(result);
		Assert.Null(converter);
	}
}

/// <summary>
/// Tests for the DirectiveMapping class.
/// </summary>
public class DirectiveMappingTests {
	[Theory]
	[InlineData("ASAR", "db", "db")]
	[InlineData("ASAR", "incsrc", "include")]
	[InlineData("ASAR", "incbin", "incbin")]
	[InlineData("CA65", ".byte", "db")]
	[InlineData("CA65", ".word", "dw")]
	[InlineData("CA65", ".include", "include")]
	[InlineData("XKAS", "db", "db")]
	[InlineData("XKAS", "incsrc", "include")]
	public void TryTranslate_KnownDirective_ReturnsTrue(
		string assembler, string directive, string expected) {
		var result = DirectiveMapping.TryTranslate(assembler, directive, out var translated);

		Assert.True(result);
		Assert.Equal(expected, translated);
	}

	[Theory]
	[InlineData("ASAR", "unknown_directive")]
	[InlineData("CA65", "unknown_directive")]
	[InlineData("XKAS", "unknown_directive")]
	public void TryTranslate_UnknownDirective_ReturnsFalse(string assembler, string directive) {
		var result = DirectiveMapping.TryTranslate(assembler, directive, out var translated);

		Assert.False(result);
		Assert.Null(translated);
	}

	[Theory]
	[InlineData("ASAR", "pushpc")]
	[InlineData("CA65", ".debuginfo")]
	public void IsUnsupported_UnsupportedDirective_ReturnsTrue(string assembler, string directive) {
		var result = DirectiveMapping.IsUnsupported(assembler, directive);

		Assert.True(result);
	}
}

/// <summary>
/// Tests for the AsarConverter class.
/// </summary>
public class AsarConverterTests {
	private readonly AsarConverter _converter = new();
	private readonly ConversionOptions _options = new();

	[Fact]
	public void SourceAssembler_ReturnsASAR() {
		Assert.Equal("ASAR", _converter.SourceAssembler);
	}

	[Fact]
	public void SupportedExtensions_ContainsAsm() {
		Assert.Contains(".asm", _converter.SupportedExtensions);
	}

	[Fact]
	public void CanConvert_AsmFile_ReturnsTrue() {
		Assert.True(_converter.CanConvert("test.asm"));
	}

	[Fact]
	public void CanConvert_NonAsmFile_ReturnsFalse() {
		Assert.False(_converter.CanConvert("test.txt"));
	}

	[Fact]
	public void ConvertFile_NonExistentFile_ReturnsError() {
		var result = _converter.ConvertFile("nonexistent.asm", _options);

		Assert.False(result.Success);
		Assert.NotEmpty(result.Errors);
		Assert.Equal("CONV001", result.Errors[0].Code);
	}

	[Fact]
	public void ConvertFile_SimpleInstruction_ConvertsCorrectly() {
		// Create temp file
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, "lda #$00\nsta $2000");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("lda #$00", result.Content);
			Assert.Contains("sta $2000", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_Label_ConvertsCorrectly() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, "main_loop:\n\tlda #$00\n\tbne main_loop");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("main_loop:", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_DataDirective_ConvertsCorrectly() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, "db $00, $01, $02\ndw $1234");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("db $00, $01, $02", result.Content);
			Assert.Contains("dw $1234", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_RomMapping_ConvertsCorrectly() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, "lorom\norg $8000");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("lorom", result.Content);
			Assert.Contains("org $8000", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_Include_ConvertsAndTracksReference() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, "incsrc \"other.asm\"");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("include \"other.pasm\"", result.Content);
			Assert.Contains("other.pasm", result.ReferencedFiles);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_Comment_PreservedByDefault() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, "lda #$00 ; load zero");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("; load zero", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_Comment_RemovedWhenOptionDisabled() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, "lda #$00 ; load zero");
		var options = new ConversionOptions { PreserveComments = false };

		try {
			var result = _converter.ConvertFile(tempFile, options);

			Assert.True(result.Success);
			Assert.DoesNotContain("; load zero", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}
}

/// <summary>
/// Tests for the Ca65Converter class.
/// </summary>
public class Ca65ConverterTests {
	private readonly Ca65Converter _converter = new();
	private readonly ConversionOptions _options = new();

	[Fact]
	public void SourceAssembler_ReturnsCA65() {
		Assert.Equal("CA65", _converter.SourceAssembler);
	}

	[Fact]
	public void SupportedExtensions_ContainsExpected() {
		Assert.Contains(".s", _converter.SupportedExtensions);
		Assert.Contains(".asm", _converter.SupportedExtensions);
		Assert.Contains(".inc", _converter.SupportedExtensions);
	}

	[Fact]
	public void ConvertFile_ByteDirective_ConvertsToDB() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, ".byte $00, $01, $02");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("db $00, $01, $02", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_Segment_ConvertsCorrectly() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, ".segment \"CODE\"");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("segment \"CODE\"", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_Proc_ConvertsCorrectly() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, ".proc MyProc\n\trts\n.endproc");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("proc MyProc", result.Content);
			Assert.Contains("endproc", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_LocalLabel_ConvertsAtToDot() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, "@loop:\n\tdex\n\tbne @loop");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains(".loop:", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_Include_ConvertsExtension() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, ".include \"header.s\"");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("include \"header.pasm\"", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_Macro_ConvertsCorrectly() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, ".macro push_all\n\tpha\n\tphx\n.endmacro");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("macro push_all()", result.Content);
			Assert.Contains("endmacro", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}
}

/// <summary>
/// Tests for the XkasConverter class.
/// </summary>
public class XkasConverterTests {
	private readonly XkasConverter _converter = new();
	private readonly ConversionOptions _options = new();

	[Fact]
	public void SourceAssembler_ReturnsXKAS() {
		Assert.Equal("XKAS", _converter.SourceAssembler);
	}

	[Fact]
	public void SupportedExtensions_ContainsAsm() {
		Assert.Contains(".asm", _converter.SupportedExtensions);
	}

	[Fact]
	public void ConvertFile_BasicCode_ConvertsCorrectly() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, "lorom\norg $8000\nlda #$00");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("lorom", result.Content);
			Assert.Contains("org $8000", result.Content);
			Assert.Contains("lda #$00", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_CppStyleComment_ConvertedToSemicolon() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, "lda #$00 // load zero");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("; load zero", result.Content);
			Assert.DoesNotContain("//", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_Arch_ConvertsCorrectly() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, "arch 65816");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("arch 65816", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ConvertFile_Table_ConvertsCorrectly() {
		var tempFile = Path.GetTempFileName();
		File.WriteAllText(tempFile, "table \"text.tbl\"\ncleartable");

		try {
			var result = _converter.ConvertFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("table \"text.tbl\"", result.Content);
			Assert.Contains("cleartable", result.Content);
		}
		finally {
			File.Delete(tempFile);
		}
	}
}

/// <summary>
/// Tests for ConversionResult and related types.
/// </summary>
public class ConversionResultTests {
	[Fact]
	public void ConversionResult_DefaultValues_AreCorrect() {
		var result = new ConversionResult();

		Assert.False(result.Success);
		Assert.Empty(result.Content);
		Assert.Empty(result.SourcePath);
		Assert.Empty(result.OutputPath);
		Assert.Empty(result.Warnings);
		Assert.Empty(result.Errors);
		Assert.Empty(result.ReferencedFiles);
	}

	[Fact]
	public void ConversionResult_WithExpression_CreatesNewInstance() {
		var result1 = new ConversionResult { Success = false };
		var result2 = result1 with { Success = true };

		Assert.False(result1.Success);
		Assert.True(result2.Success);
	}

	[Fact]
	public void ProjectConversionResult_ComputedProperties_AreCorrect() {
		var fileResults = new List<ConversionResult> {
			new() { Success = true },
			new() { Success = true },
			new() { Success = false }
		};

		var result = new ProjectConversionResult { FileResults = fileResults };

		Assert.Equal(3, result.TotalFiles);
		Assert.Equal(2, result.SuccessfulFiles);
		Assert.Equal(1, result.FailedFiles);
	}

	[Fact]
	public void ConversionMessage_ToString_FormatsCorrectly() {
		var message = new ConversionMessage {
			FilePath = "test.asm",
			Line = 10,
			Column = 5,
			Code = "CONV001",
			Message = "Test error",
			Severity = MessageSeverity.Error
		};

		var str = message.ToString();

		Assert.Equal("test.asm(10,5): error CONV001: Test error", str);
	}
}
