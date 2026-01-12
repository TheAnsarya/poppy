using Poppy.Core;
using Poppy.Core.Lexer;
using Xunit;

namespace Poppy.Tests;

/// <summary>
/// Tests for the ErrorFormatter class.
/// </summary>
public class ErrorFormatterTests {
	[Fact]
	public void Format_WithRegisteredSource_ShowsLineContext() {
		// arrange
		var formatter = new ErrorFormatter();
		var source = @"lda #$42
sta $0200
invalid_opcode
sta $0201";
		formatter.RegisterSource("test.pasm", source);

		var location = new SourceLocation("test.pasm", 3, 1, 0);

		// act
		var result = formatter.Format("Unknown instruction 'invalid_opcode'", location);

		// assert
		Assert.Contains("test.pasm:3:1: error: Unknown instruction 'invalid_opcode'", result);
		Assert.Contains("3 | invalid_opcode", result);
		Assert.Contains("| ^", result);
	}

	[Fact]
	public void Format_WithColumnOffset_ShowsCaretAtCorrectPosition() {
		// arrange
		var formatter = new ErrorFormatter();
		var source = "lda #$ZZ";
		formatter.RegisterSource("test.pasm", source);

		var location = new SourceLocation("test.pasm", 1, 6, 5);

		// act
		var result = formatter.Format("Invalid hex digit 'Z'", location);

		// assert
		Assert.Contains("test.pasm:1:6: error: Invalid hex digit 'Z'", result);
		Assert.Contains("1 | lda #$ZZ", result);
		// Caret should be at column 6 (under the first Z)
		Assert.Contains("|      ^", result);
	}

	[Fact]
	public void Format_WithRange_ShowsUnderline() {
		// arrange
		var formatter = new ErrorFormatter();
		var source = "unknown_directive";
		formatter.RegisterSource("test.pasm", source);

		var location = new SourceLocation("test.pasm", 1, 1, 0);

		// act
		var result = formatter.Format("Unknown directive", location, 17);

		// assert
		Assert.Contains("test.pasm:1:1: error: Unknown directive", result);
		Assert.Contains("1 | unknown_directive", result);
		Assert.Contains("| ^~~~~~~~~~~~~~~~~", result);
	}

	[Fact]
	public void Format_WithoutRegisteredSource_ShowsLocationOnly() {
		// arrange
		var formatter = new ErrorFormatter();
		var location = new SourceLocation("missing.pasm", 5, 10, 0);

		// act
		var result = formatter.Format("Some error", location);

		// assert
		Assert.Contains("missing.pasm:5:10: error: Some error", result);
		// Should not contain line context since source is not registered
		Assert.DoesNotContain("|", result);
	}

	[Fact]
	public void Format_MultiLineSource_ShowsCorrectLine() {
		// arrange
		var formatter = new ErrorFormatter();
		var source = @"line1
line2
line3 with error
line4";
		formatter.RegisterSource("test.pasm", source);

		var location = new SourceLocation("test.pasm", 3, 7, 0);

		// act
		var result = formatter.Format("Error at 'with'", location);

		// assert
		Assert.Contains("3 | line3 with error", result);
		Assert.Contains("|       ^", result);
	}

	[Fact]
	public void FormatAll_MultipleErrors_FormatsEach() {
		// arrange
		var formatter = new ErrorFormatter();
		var source = @"lda badarg
sta badaddr
unknown";
		formatter.RegisterSource("test.pasm", source);

		var errors = new (string, SourceLocation)[] {
			("Error 1", new SourceLocation("test.pasm", 1, 5, 0)),
			("Error 2", new SourceLocation("test.pasm", 2, 5, 0)),
			("Error 3", new SourceLocation("test.pasm", 3, 1, 0))
		};

		// act
		var result = formatter.FormatAll(errors);

		// assert
		Assert.Contains("Error 1", result);
		Assert.Contains("Error 2", result);
		Assert.Contains("Error 3", result);
		Assert.Contains("Build failed with 3 error(s)", result);
	}

	[Fact]
	public void Format_WindowsLineEndings_HandledCorrectly() {
		// arrange
		var formatter = new ErrorFormatter();
		var source = "line1\r\nline2\r\nline3";
		formatter.RegisterSource("test.pasm", source);

		var location = new SourceLocation("test.pasm", 2, 1, 0);

		// act
		var result = formatter.Format("Error on line 2", location);

		// assert - The source line should be "line2" without any \r attached
		// If \r wasn't stripped, we'd see "line2\r" in the output which would break formatting
		Assert.Contains("| line2", result);
		// The line should end with "line2", not "line2\r"
		Assert.DoesNotContain("line2\r|", result);
	}

	[Fact]
	public void Format_LargeLineNumber_ProperlyPadded() {
		// arrange
		var formatter = new ErrorFormatter();
		var lines = new string[1000];
		for (int i = 0; i < 1000; i++) {
			lines[i] = $"line {i + 1}";
		}
		formatter.RegisterSource("test.pasm", string.Join("\n", lines));

		var location = new SourceLocation("test.pasm", 999, 1, 0);

		// act
		var result = formatter.Format("Error on line 999", location);

		// assert
		Assert.Contains(" 999 | line 999", result);
	}

	[Fact]
	public void Format_EmptySource_NoException() {
		// arrange
		var formatter = new ErrorFormatter();
		formatter.RegisterSource("test.pasm", "");

		var location = new SourceLocation("test.pasm", 1, 1, 0);

		// act
		var result = formatter.Format("Error in empty file", location);

		// assert
		Assert.Contains("test.pasm:1:1: error: Error in empty file", result);
	}

	[Fact]
	public void Format_LineOutOfRange_NoException() {
		// arrange
		var formatter = new ErrorFormatter();
		formatter.RegisterSource("test.pasm", "single line");

		var location = new SourceLocation("test.pasm", 100, 1, 0);

		// act
		var result = formatter.Format("Error at invalid line", location);

		// assert
		Assert.Contains("test.pasm:100:1: error: Error at invalid line", result);
		// Should not crash and should not show line context
	}

	[Fact]
	public void Format_TabCharacter_CaretPositionCorrect() {
		// arrange
		var formatter = new ErrorFormatter();
		var source = "\t\tlda #$42";  // Two tabs before instruction
		formatter.RegisterSource("test.pasm", source);

		var location = new SourceLocation("test.pasm", 1, 3, 2);

		// act
		var result = formatter.Format("Error at lda", location);

		// assert
		// Should handle the position even with tabs
		Assert.Contains("1 | \t\tlda #$42", result);
	}

	[Fact]
	public void RegisterSource_NullFilePath_NoException() {
		// arrange
		var formatter = new ErrorFormatter();

		// act & assert - should not throw
		formatter.RegisterSource(null!, "source");
		formatter.RegisterSource("", "source");
	}
}
