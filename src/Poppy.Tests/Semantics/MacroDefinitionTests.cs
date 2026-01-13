using Poppy.Core.Semantics;
using Xunit;

namespace Poppy.Tests.Semantics;

public class MacroDefinitionTests {
	[Fact]
	public void BasicMacroDefinition_StoresCorrectly() {
		// arrange
		var source = @"
.macro test_macro
	nop
.endmacro
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		Assert.True(analyzer.MacroTable.IsDefined("test_macro"));
		var macro = analyzer.MacroTable.Get("test_macro");
		Assert.NotNull(macro);
		Assert.Equal("test_macro", macro.Name);
		Assert.Empty(macro.Parameters);
	}

	[Fact]
	public void MacroWithParameters_StoresParameters() {
		// arrange
		var source = @"
.macro sprite_dma, addr, count
	lda addr
	sta $2002
	ldx count
.endmacro
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var macro = analyzer.MacroTable.Get("sprite_dma");
		Assert.NotNull(macro);
		Assert.Equal(2, macro.Parameters.Count);
		Assert.Equal("addr", macro.Parameters[0].Name);
		Assert.Equal("count", macro.Parameters[1].Name);
	}

	[Fact]
	public void EmptyMacro_AllowedAndStored() {
		// arrange
		var source = @"
.macro empty
.endmacro
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		Assert.True(analyzer.MacroTable.IsDefined("empty"));
	}

	[Fact]
	public void DuplicateMacroName_ReportsError() {
		// arrange
		var source = @"
.macro test
	nop
.endmacro

.macro test
	inc $00
.endmacro
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
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("already defined"));
	}

	[Fact]
	public void MacroNameConflictWithOpcode_ReportsError() {
		// arrange
		var source = @"
.macro lda
	nop
.endmacro
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
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("reserved word"));
	}

	[Fact]
	public void MacroNameConflictWithDirective_ReportsError() {
		// arrange
		var source = @"
.macro org
	nop
.endmacro
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
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("reserved word"));
	}

	[Fact]
	public void DuplicateParameterName_ReportsError() {
		// arrange
		var source = @"
.macro bad, param, param
	nop
.endmacro
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
		Assert.Contains(analyzer.Errors, e => e.Message.Contains("Duplicate parameter"));
	}

	[Fact]
	public void MacroWithManyParameters_AllStored() {
		// arrange
		var source = @"
.macro complex, p1, p2, p3, p4, p5
	lda p1
	sta p2
	ldx p3
	ldy p4
	jmp p5
.endmacro
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		var macro = analyzer.MacroTable.Get("complex");
		Assert.NotNull(macro);
		Assert.Equal(5, macro.Parameters.Count);
		Assert.Equal("p1", macro.Parameters[0].Name);
		Assert.Equal("p5", macro.Parameters[4].Name);
	}

	[Fact]
	public void MultipleMacros_AllStored() {
		// arrange
		var source = @"
.macro macro1
	nop
.endmacro

.macro macro2
	inc $00
.endmacro

.macro macro3
	dec $01
.endmacro
";
		var lexer = new Poppy.Core.Lexer.Lexer(source, "test.pasm");
		var tokens = lexer.Tokenize();
		var parser = new Poppy.Core.Parser.Parser(tokens);
		var program = parser.Parse();

		// act
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(program);

		// assert
		Assert.False(analyzer.HasErrors);
		Assert.Equal(3, analyzer.MacroTable.Count);
		Assert.True(analyzer.MacroTable.IsDefined("macro1"));
		Assert.True(analyzer.MacroTable.IsDefined("macro2"));
		Assert.True(analyzer.MacroTable.IsDefined("macro3"));
	}

	[Fact]
	public void MacroTable_IsReservedWord_ChecksCorrectly() {
		// assert
		Assert.True(MacroTable.IsReservedWord("lda"));
		Assert.True(MacroTable.IsReservedWord("org"));
		Assert.True(MacroTable.IsReservedWord("macro"));
		Assert.True(MacroTable.IsReservedWord("if"));
		Assert.False(MacroTable.IsReservedWord("my_macro"));
		Assert.False(MacroTable.IsReservedWord("test123"));
	}
}
