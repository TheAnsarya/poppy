// ============================================================================
// ExporterTests.cs - Unit Tests for PASM Exporters
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Converters;
using Xunit;

namespace Poppy.Tests.Converters;

// ============================================================================
// ExporterFactory Tests
// ============================================================================

public class ExporterFactoryTests {
	[Fact]
	public void SupportedTargets_ContainsExpectedTargets() {
		var targets = ExporterFactory.SupportedTargets;

		Assert.Contains("ASAR", targets);
		Assert.Contains("CA65", targets);
		Assert.Contains("XKAS", targets);
	}

	[Theory]
	[InlineData("ASAR")]
	[InlineData("asar")]
	[InlineData("CA65")]
	[InlineData("ca65")]
	[InlineData("XKAS")]
	[InlineData("xkas")]
	public void Create_ValidTarget_ReturnsExporter(string target) {
		var exporter = ExporterFactory.Create(target);

		Assert.NotNull(exporter);
	}

	[Fact]
	public void Create_InvalidTarget_ThrowsArgumentException() {
		Assert.Throws<ArgumentException>(() => ExporterFactory.Create("invalid"));
	}

	[Fact]
	public void TryCreate_ValidTarget_ReturnsTrue() {
		var result = ExporterFactory.TryCreate("ASAR", out var exporter);

		Assert.True(result);
		Assert.NotNull(exporter);
	}

	[Fact]
	public void TryCreate_InvalidTarget_ReturnsFalse() {
		var result = ExporterFactory.TryCreate("invalid", out var exporter);

		Assert.False(result);
		Assert.Null(exporter);
	}

	[Fact]
	public void Create_ASAR_ReturnsPasmToAsarExporter() {
		var exporter = ExporterFactory.Create("ASAR");
		Assert.Equal("ASAR", exporter.TargetAssembler);
	}

	[Fact]
	public void Create_CA65_ReturnsPasmToCa65Exporter() {
		var exporter = ExporterFactory.Create("CA65");
		Assert.Equal("CA65", exporter.TargetAssembler);
	}

	[Fact]
	public void Create_XKAS_ReturnsPasmToXkasExporter() {
		var exporter = ExporterFactory.Create("XKAS");
		Assert.Equal("XKAS", exporter.TargetAssembler);
	}
}

// ============================================================================
// Reverse DirectiveMapping Tests
// ============================================================================

public class ReverseDirectiveMappingTests {
	[Theory]
	[InlineData("ASAR", "db", "db")]
	[InlineData("ASAR", "include", "incsrc")]
	[InlineData("ASAR", "incbin", "incbin")]
	[InlineData("ASAR", "org", "org")]
	[InlineData("ASAR", "lorom", "lorom")]
	[InlineData("ASAR", "hirom", "hirom")]
	[InlineData("CA65", "db", ".byte")]
	[InlineData("CA65", "dw", ".word")]
	[InlineData("CA65", "dd", ".dword")]
	[InlineData("CA65", "include", ".include")]
	[InlineData("CA65", "incbin", ".incbin")]
	[InlineData("CA65", "fill", ".res")]
	[InlineData("XKAS", "db", "db")]
	[InlineData("XKAS", "dw", "dw")]
	[InlineData("XKAS", "include", "incsrc")]
	public void TryTranslateReverse_KnownDirective_ReturnsTrue(
		string assembler, string pasmDirective, string expected) {
		var result = DirectiveMapping.TryTranslateReverse(assembler, pasmDirective, out var translated);

		Assert.True(result);
		Assert.Equal(expected, translated);
	}

	[Theory]
	[InlineData("ASAR", "unknown_directive")]
	[InlineData("CA65", "unknown_directive")]
	[InlineData("XKAS", "unknown_directive")]
	public void TryTranslateReverse_UnknownDirective_ReturnsFalse(
		string assembler, string directive) {
		var result = DirectiveMapping.TryTranslateReverse(assembler, directive, out var translated);

		Assert.False(result);
		Assert.Null(translated);
	}

	[Theory]
	[InlineData("ASAR")]
	[InlineData("CA65")]
	[InlineData("XKAS")]
	public void GetReverseMapping_ValidAssembler_ReturnsNonEmpty(string assembler) {
		var mapping = DirectiveMapping.GetReverseMapping(assembler);

		Assert.NotNull(mapping);
		Assert.NotEmpty(mapping);
	}

	[Fact]
	public void GetReverseMapping_InvalidAssembler_Throws() {
		Assert.Throws<ArgumentException>(() => DirectiveMapping.GetReverseMapping("invalid"));
	}
}

// ============================================================================
// PasmToAsarExporter Tests
// ============================================================================

public class PasmToAsarExporterTests {
	private readonly PasmToAsarExporter _exporter = new();
	private readonly ConversionOptions _options = new();

	[Fact]
	public void TargetAssembler_ReturnsASAR() {
		Assert.Equal("ASAR", _exporter.TargetAssembler);
	}

	[Fact]
	public void DefaultExtension_ReturnsAsm() {
		Assert.Equal(".asm", _exporter.DefaultExtension);
	}

	[Fact]
	public void CanExport_PasmFile_ReturnsTrue() {
		Assert.True(_exporter.CanExport("test.pasm"));
	}

	[Fact]
	public void CanExport_NonPasmFile_ReturnsFalse() {
		Assert.False(_exporter.CanExport("test.asm"));
	}

	[Fact]
	public void ExportFile_NonExistentFile_ReturnsError() {
		var result = _exporter.ExportFile("nonexistent.pasm", _options);

		Assert.False(result.Success);
		Assert.NotEmpty(result.Errors);
		Assert.Equal("EXP001", result.Errors[0].Code);
	}

	[Fact]
	public void ExportFile_SimpleInstruction_PassesThrough() {
		var tempFile = CreateTempPasm("lda #$00\nsta $2000");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("lda #$00", result.Content);
			Assert.Contains("sta $2000", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Label_PassesThrough() {
		var tempFile = CreateTempPasm("main_loop:\n\tlda #$00");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("main_loop:", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_DataDirective_PassesThrough() {
		var tempFile = CreateTempPasm("db $00, $01, $02\ndw $1234");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("db $00, $01, $02", result.Content);
			Assert.Contains("dw $1234", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Include_ConvertsToIncsrc() {
		var tempFile = CreateTempPasm("include \"other.pasm\"");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("incsrc \"other.asm\"", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Comment_Preserved() {
		var tempFile = CreateTempPasm("lda #$00 ; load zero");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("; load zero", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_FullLineComment_Preserved() {
		var tempFile = CreateTempPasm("; This is a comment\nlda #$00");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("; This is a comment", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_RomMapping_PassesThrough() {
		var tempFile = CreateTempPasm("lorom\norg $8000");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("lorom", result.Content);
			Assert.Contains("org $8000", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_EmptyLines_Preserved() {
		var tempFile = CreateTempPasm("lda #$00\n\nsta $2000");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("lda #$00", result.Content);
			Assert.Contains("sta $2000", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Header_ContainsASARReference() {
		var tempFile = CreateTempPasm("lda #$00");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("ASAR", result.Content);
			Assert.Contains("Converted from PASM", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Namespace_PassesThrough() {
		var tempFile = CreateTempPasm("namespace MyNS\nlda #$00\nendnamespace");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("namespace MyNS", result.Content);
			Assert.Contains("endnamespace", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_FillDirectives_Preserved() {
		var tempFile = CreateTempPasm("fill 16, $ff\nfillbyte $00\npad $8100");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("fill 16, $ff", result.Content);
			Assert.Contains("fillbyte $00", result.Content);
			Assert.Contains("pad $8100", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	private static string CreateTempPasm(string content) {
		var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.pasm");
		File.WriteAllText(tempFile, content);
		return tempFile;
	}
}

// ============================================================================
// PasmToCa65Exporter Tests
// ============================================================================

public class PasmToCa65ExporterTests {
	private readonly PasmToCa65Exporter _exporter = new();
	private readonly ConversionOptions _options = new();

	[Fact]
	public void TargetAssembler_ReturnsCA65() {
		Assert.Equal("CA65", _exporter.TargetAssembler);
	}

	[Fact]
	public void DefaultExtension_ReturnsS() {
		Assert.Equal(".s", _exporter.DefaultExtension);
	}

	[Fact]
	public void CanExport_PasmFile_ReturnsTrue() {
		Assert.True(_exporter.CanExport("test.pasm"));
	}

	[Fact]
	public void ExportFile_DataDirective_ConvertsToByteWord() {
		var tempFile = CreateTempPasm("db $00, $01, $02\ndw $1234");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains(".byte $00, $01, $02", result.Content);
			Assert.Contains(".word $1234", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Include_ConvertsToInclude() {
		var tempFile = CreateTempPasm("include \"other.pasm\"");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains(".include \"other.s\"", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Incbin_ConvertsToIncbin() {
		var tempFile = CreateTempPasm("incbin \"data.bin\"");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains(".incbin \"data.bin\"", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Macro_ConvertsToMacro() {
		var tempFile = CreateTempPasm("macro push_all()\n\tpha\n\tphx\nendmacro");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains(".macro push_all()", result.Content);
			Assert.Contains(".endmacro", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Conditional_ConvertsToConditional() {
		var tempFile = CreateTempPasm("ifdef DEBUG\n\tlda #$01\nendif");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains(".ifdef DEBUG", result.Content);
			Assert.Contains(".endif", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Fill_ConvertsToDotRes() {
		var tempFile = CreateTempPasm("fill 16, $00");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains(".res 16, $00", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Segment_ConvertsToDotSegment() {
		var tempFile = CreateTempPasm("segment \"CODE\"");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains(".segment \"CODE\"", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Proc_ConvertsToDotProc() {
		var tempFile = CreateTempPasm("proc MyFunc\n\trts\nendproc");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains(".proc MyFunc", result.Content);
			Assert.Contains(".endproc", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Comment_Preserved() {
		var tempFile = CreateTempPasm("; This is a full-line comment\nlda #$00 ; inline");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("; This is a full-line comment", result.Content);
			Assert.Contains("; inline", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_SimpleInstruction_PassesThrough() {
		var tempFile = CreateTempPasm("lda #$ff\nsta $4200\nrts");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("lda #$ff", result.Content);
			Assert.Contains("sta $4200", result.Content);
			Assert.Contains("rts", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Header_ContainsCA65Reference() {
		var tempFile = CreateTempPasm("rts");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("CA65", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Scope_ConvertsToDotScope() {
		var tempFile = CreateTempPasm("scope MyScope\n\tnop\nendscope");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains(".scope MyScope", result.Content);
			Assert.Contains(".endscope", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Enum_ConvertsToDotEnum() {
		var tempFile = CreateTempPasm("enum $00\n\tFOO\n\tBAR\nendenum");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains(".enum $00", result.Content);
			Assert.Contains(".endenum", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	private static string CreateTempPasm(string content) {
		var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.pasm");
		File.WriteAllText(tempFile, content);
		return tempFile;
	}
}

// ============================================================================
// PasmToXkasExporter Tests
// ============================================================================

public class PasmToXkasExporterTests {
	private readonly PasmToXkasExporter _exporter = new();
	private readonly ConversionOptions _options = new();

	[Fact]
	public void TargetAssembler_ReturnsXKAS() {
		Assert.Equal("XKAS", _exporter.TargetAssembler);
	}

	[Fact]
	public void DefaultExtension_ReturnsAsm() {
		Assert.Equal(".asm", _exporter.DefaultExtension);
	}

	[Fact]
	public void CanExport_PasmFile_ReturnsTrue() {
		Assert.True(_exporter.CanExport("test.pasm"));
	}

	[Fact]
	public void ExportFile_SimpleInstruction_PassesThrough() {
		var tempFile = CreateTempPasm("lda #$00\nsta $2000");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("lda #$00", result.Content);
			Assert.Contains("sta $2000", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Comment_ConvertedToDoubleSlash() {
		var tempFile = CreateTempPasm("; This is a comment");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("// This is a comment", result.Content);
			// Make sure no semicolon comments in output (except header)
			var lines = result.Content.Split('\n');
			var nonHeaderLines = lines.Skip(4); // skip header
			foreach (var line in nonHeaderLines) {
				if (line.Trim().Length > 0 && line.Trim() != "// This is a comment") {
					Assert.DoesNotContain("; This is a comment", line);
				}
			}
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_InlineComment_ConvertedToDoubleSlash() {
		var tempFile = CreateTempPasm("lda #$00 ; load zero");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("// load zero", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_DataDirective_PassesThrough() {
		var tempFile = CreateTempPasm("db $00, $01\ndw $1234\ndl $123456");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("db $00, $01", result.Content);
			Assert.Contains("dw $1234", result.Content);
			Assert.Contains("dl $123456", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Include_ConvertsToIncsrc() {
		var tempFile = CreateTempPasm("include \"other.pasm\"");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("incsrc \"other.asm\"", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_RomMapping_PassesThrough() {
		var tempFile = CreateTempPasm("lorom\norg $8000\nhirom");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("lorom", result.Content);
			Assert.Contains("org $8000", result.Content);
			Assert.Contains("hirom", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_UnsupportedDirective_GeneratesWarningComment() {
		var tempFile = CreateTempPasm("macro push_all()\n\tpha\nendmacro");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("// UNSUPPORTED:", result.Content);
			Assert.NotEmpty(result.Warnings);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Header_ContainsXKASReference() {
		var tempFile = CreateTempPasm("nop");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("XKAS", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_HeaderComment_UsesDoubleSlash() {
		var tempFile = CreateTempPasm("nop");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			// Header should use // not ;
			var firstLine = result.Content.Split('\n')[0];
			Assert.StartsWith("//", firstLine);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Fill_PassesThrough() {
		var tempFile = CreateTempPasm("fill 32\nfillbyte $ff");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("fill 32", result.Content);
			Assert.Contains("fillbyte $ff", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void ExportFile_Label_PassesThrough() {
		var tempFile = CreateTempPasm("main:\n\tnop\nloop:\n\tbra loop");

		try {
			var result = _exporter.ExportFile(tempFile, _options);

			Assert.True(result.Success);
			Assert.Contains("main:", result.Content);
			Assert.Contains("loop:", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	private static string CreateTempPasm(string content) {
		var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.pasm");
		File.WriteAllText(tempFile, content);
		return tempFile;
	}
}

// ============================================================================
// Cross-Exporter Roundtrip Tests
// ============================================================================

public class ExporterRoundtripTests {
	[Theory]
	[InlineData("ASAR")]
	[InlineData("CA65")]
	[InlineData("XKAS")]
	public void Export_SimpleInstructions_AllContainCode(string target) {
		var exporter = ExporterFactory.Create(target);
		var tempFile = CreateTempPasm("lda #$ff\nsta $2000\nrts");

		try {
			var result = exporter.ExportFile(tempFile, new ConversionOptions());

			Assert.True(result.Success);
			Assert.Contains("lda #$ff", result.Content);
			Assert.Contains("sta $2000", result.Content);
			Assert.Contains("rts", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Theory]
	[InlineData("ASAR")]
	[InlineData("CA65")]
	[InlineData("XKAS")]
	public void Export_EmptyFile_Succeeds(string target) {
		var exporter = ExporterFactory.Create(target);
		var tempFile = CreateTempPasm("");

		try {
			var result = exporter.ExportFile(tempFile, new ConversionOptions());

			Assert.True(result.Success);
			Assert.NotNull(result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Theory]
	[InlineData("ASAR")]
	[InlineData("CA65")]
	[InlineData("XKAS")]
	public void Export_Labels_AllContainLabels(string target) {
		var exporter = ExporterFactory.Create(target);
		var tempFile = CreateTempPasm("start:\n\tlda #$00\nloop:\n\tnop\n\tbra loop");

		try {
			var result = exporter.ExportFile(tempFile, new ConversionOptions());

			Assert.True(result.Success);
			Assert.Contains("start:", result.Content);
			Assert.Contains("loop:", result.Content);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Theory]
	[InlineData("ASAR")]
	[InlineData("CA65")]
	[InlineData("XKAS")]
	public void Export_NonExistentFile_AllReturnError(string target) {
		var exporter = ExporterFactory.Create(target);

		var result = exporter.ExportFile("nonexistent.pasm", new ConversionOptions());

		Assert.False(result.Success);
		Assert.NotEmpty(result.Errors);
	}

	private static string CreateTempPasm(string content) {
		var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.pasm");
		File.WriteAllText(tempFile, content);
		return tempFile;
	}
}
