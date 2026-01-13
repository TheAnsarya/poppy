// ============================================================================
// Preprocessor.cs - Assembly Preprocessor for Include Handling
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.Lexer;

namespace Poppy.Core.Parser;

/// <summary>
/// Preprocesses assembly source files, handling includes and other preprocessor directives.
/// </summary>
public sealed class Preprocessor {
	private readonly List<string> _includePaths;
	private readonly HashSet<string> _includedFiles;
	private readonly List<PreprocessorError> _errors;
	private readonly int _maxIncludeDepth;

	/// <summary>
	/// Gets the list of preprocessor errors.
	/// </summary>
	public IReadOnlyList<PreprocessorError> Errors => _errors;

	/// <summary>
	/// Gets whether preprocessing encountered any errors.
	/// </summary>
	public bool HasErrors => _errors.Count > 0;

	/// <summary>
	/// Creates a new preprocessor.
	/// </summary>
	/// <param name="includePaths">Additional paths to search for include files.</param>
	/// <param name="maxIncludeDepth">Maximum nesting depth for includes (default: 100).</param>
	public Preprocessor(IEnumerable<string>? includePaths = null, int maxIncludeDepth = 100) {
		_includePaths = includePaths?.ToList() ?? [];
		_includedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		_errors = [];
		_maxIncludeDepth = maxIncludeDepth;
	}

	/// <summary>
	/// Preprocesses a source file, expanding all includes.
	/// </summary>
	/// <param name="source">The source code.</param>
	/// <param name="filePath">The path to the source file.</param>
	/// <returns>A list of preprocessed tokens.</returns>
	public List<Token> Process(string source, string filePath) {
		_includedFiles.Clear();
		_errors.Clear();

		var fullPath = Path.GetFullPath(filePath);
		_includedFiles.Add(fullPath);

		return ProcessSource(source, filePath, 0);
	}

	/// <summary>
	/// Processes source code, handling includes recursively.
	/// </summary>
	private List<Token> ProcessSource(string source, string filePath, int depth) {
		if (depth > _maxIncludeDepth) {
			_errors.Add(new PreprocessorError(
				$"Maximum include depth ({_maxIncludeDepth}) exceeded",
				new SourceLocation(filePath, 1, 1, 0)));
			return [];
		}

		var lexer = new Lexer.Lexer(source, filePath);
		var tokens = new List<Token>();
		var baseDir = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? ".";

		Token token;
		do {
			token = lexer.NextToken();

			// Check for .include directive
			if (token.Type == TokenType.Directive &&
				token.Text.Equals(".include", StringComparison.OrdinalIgnoreCase)) {
				var includeTokens = HandleInclude(lexer, token.Location, baseDir, depth);
				tokens.AddRange(includeTokens);
				continue;
			}

			// Check for .incbin directive
			if (token.Type == TokenType.Directive &&
				token.Text.Equals(".incbin", StringComparison.OrdinalIgnoreCase)) {
				// For .incbin, we keep the directive and just pass it through
				// The binary inclusion happens during code generation
				tokens.Add(token);
				continue;
			}

			tokens.Add(token);
		} while (token.Type != TokenType.EndOfFile);

		return tokens;
	}

	/// <summary>
	/// Handles an .include directive.
	/// </summary>
	private List<Token> HandleInclude(Lexer.Lexer lexer, SourceLocation location, string baseDir, int depth) {
		var tokens = new List<Token>();

		// Skip whitespace to get to the filename
		Token filenameToken;
		do {
			filenameToken = lexer.NextToken();
		} while (filenameToken.Type == TokenType.Comment);

		if (filenameToken.Type != TokenType.String) {
			_errors.Add(new PreprocessorError(
				$"Expected filename string after .include, got {filenameToken.Type}",
				filenameToken.Location));
			return tokens;
		}

		var filename = filenameToken.Text;

		// Resolve the include path
		var resolvedPath = ResolveIncludePath(filename, baseDir);
		if (resolvedPath is null) {
			_errors.Add(new PreprocessorError(
				$"Include file not found: {filename}",
				filenameToken.Location));
			return tokens;
		}

		// Check for circular includes
		var fullPath = Path.GetFullPath(resolvedPath);
		if (_includedFiles.Contains(fullPath)) {
			_errors.Add(new PreprocessorError(
				$"Circular include detected: {filename}",
				filenameToken.Location));
			return tokens;
		}

		// Read and process the included file
		string includeSource;
		try {
			includeSource = File.ReadAllText(resolvedPath);
		} catch (Exception ex) {
			_errors.Add(new PreprocessorError(
				$"Error reading include file '{filename}': {ex.Message}",
				filenameToken.Location));
			return tokens;
		}

		// Mark file as included
		_includedFiles.Add(fullPath);

		// Process the included file recursively
		var includeTokens = ProcessSource(includeSource, resolvedPath, depth + 1);

		// Remove the EOF token from included content (we'll add our own at the end)
		if (includeTokens.Count > 0 && includeTokens[^1].Type == TokenType.EndOfFile) {
			includeTokens.RemoveAt(includeTokens.Count - 1);
		}

		tokens.AddRange(includeTokens);
		return tokens;
	}

	/// <summary>
	/// Resolves an include path by searching include directories.
	/// </summary>
	private string? ResolveIncludePath(string filename, string baseDir) {
		// First, try relative to the current file
		var relativePath = Path.Combine(baseDir, filename);
		if (File.Exists(relativePath)) {
			return relativePath;
		}

		// Then try each include path
		foreach (var includePath in _includePaths) {
			var fullPath = Path.Combine(includePath, filename);
			if (File.Exists(fullPath)) {
				return fullPath;
			}
		}

		// Finally, try as an absolute path
		if (Path.IsPathRooted(filename) && File.Exists(filename)) {
			return filename;
		}

		return null;
	}
}

/// <summary>
/// Represents a preprocessor error.
/// </summary>
public sealed class PreprocessorError {
	/// <summary>
	/// The error message.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// The source location where the error occurred.
	/// </summary>
	public SourceLocation Location { get; }

	/// <summary>
	/// Creates a new preprocessor error.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="location">The source location.</param>
	public PreprocessorError(string message, SourceLocation location) {
		Message = message;
		Location = location;
	}

	/// <inheritdoc />
	public override string ToString() => $"{Location}: error: {Message}";
}

