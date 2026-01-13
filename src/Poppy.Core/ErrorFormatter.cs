// ============================================================================
// ErrorFormatter.cs - Error Message Formatting with Source Context
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using System.Text;
using Poppy.Core.Lexer;

namespace Poppy.Core;

/// <summary>
/// Formats error messages with source code context.
/// </summary>
public sealed class ErrorFormatter {
	private readonly Dictionary<string, string[]> _sourceCache = new();

	/// <summary>
	/// Registers source code for a file to enable context display.
	/// </summary>
	/// <param name="filePath">The file path.</param>
	/// <param name="source">The source code content.</param>
	public void RegisterSource(string filePath, string source) {
		if (string.IsNullOrEmpty(filePath))
			return;

		// Split into lines, preserving empty lines
		var lines = source.Split('\n');
		// Remove carriage returns if present
		for (int i = 0; i < lines.Length; i++) {
			lines[i] = lines[i].TrimEnd('\r');
		}

		_sourceCache[filePath] = lines;
	}

	/// <summary>
	/// Formats an error with source context.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="location">The source location.</param>
	/// <returns>A formatted error string with context.</returns>
	public string Format(string message, SourceLocation location) {
		var sb = new StringBuilder();

		// Header: file:line:column: error: message
		sb.AppendLine($"{location}: error: {message}");

		// Try to get source context
		if (_sourceCache.TryGetValue(location.FilePath, out var lines)) {
			var lineIndex = location.Line - 1; // Convert to 0-based

			if (lineIndex >= 0 && lineIndex < lines.Length) {
				var sourceLine = lines[lineIndex];

				// Show the source line with line number
				var lineNumWidth = Math.Max(4, location.Line.ToString().Length);
				var lineNumStr = location.Line.ToString().PadLeft(lineNumWidth);
				sb.AppendLine($" {lineNumStr} | {sourceLine}");

				// Show the caret pointing to the error location
				var padding = new string(' ', lineNumWidth);
				var caretColumn = Math.Max(1, location.Column);
				var caret = new string(' ', caretColumn - 1) + "^";
				sb.AppendLine($" {padding} | {caret}");
			}
		}

		return sb.ToString().TrimEnd();
	}

	/// <summary>
	/// Formats an error with source context and optional range highlight.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="location">The source location.</param>
	/// <param name="length">The length of the error span to highlight.</param>
	/// <returns>A formatted error string with context.</returns>
	public string Format(string message, SourceLocation location, int length) {
		var sb = new StringBuilder();

		// Header: file:line:column: error: message
		sb.AppendLine($"{location}: error: {message}");

		// Try to get source context
		if (_sourceCache.TryGetValue(location.FilePath, out var lines)) {
			var lineIndex = location.Line - 1; // Convert to 0-based

			if (lineIndex >= 0 && lineIndex < lines.Length) {
				var sourceLine = lines[lineIndex];

				// Show the source line with line number
				var lineNumWidth = Math.Max(4, location.Line.ToString().Length);
				var lineNumStr = location.Line.ToString().PadLeft(lineNumWidth);
				sb.AppendLine($" {lineNumStr} | {sourceLine}");

				// Show the caret/underline pointing to the error location
				var padding = new string(' ', lineNumWidth);
				var caretColumn = Math.Max(1, location.Column);
				var underlineLength = Math.Max(1, Math.Min(length, sourceLine.Length - caretColumn + 2));
				var underline = new string(' ', caretColumn - 1) + "^" + new string('~', underlineLength - 1);
				sb.AppendLine($" {padding} | {underline}");
			}
		}

		return sb.ToString().TrimEnd();
	}

	/// <summary>
	/// Formats multiple errors.
	/// </summary>
	/// <param name="errors">The errors to format.</param>
	/// <returns>A formatted string containing all errors.</returns>
	public string FormatAll(IEnumerable<(string Message, SourceLocation Location)> errors) {
		var sb = new StringBuilder();
		var errorList = errors.ToList();

		foreach (var (Message, Location) in errorList) {
			sb.AppendLine(Format(Message, Location));
			sb.AppendLine();
		}

		if (errorList.Count > 0) {
			sb.AppendLine($"Build failed with {errorList.Count} error(s).");
		}

		return sb.ToString().TrimEnd();
	}
}
