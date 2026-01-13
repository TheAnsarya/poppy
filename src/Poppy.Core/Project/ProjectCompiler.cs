// ============================================================================
// ProjectCompiler.cs - Multi-File Project Compilation
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

namespace Poppy.Core.Project;

/// <summary>
/// Compiles a multi-file project from a poppy.json configuration.
/// </summary>
public sealed class ProjectCompiler {
	private readonly ProjectFile _project;
	private readonly string _projectDir;
	private readonly List<ProjectError> _errors;
	private readonly List<string> _warnings;
	private readonly SemanticAnalyzer _globalAnalyzer;
	private readonly List<string> _compiledFiles;

	/// <summary>
	/// Gets all compilation errors.
	/// </summary>
	public IReadOnlyList<ProjectError> Errors => _errors;

	/// <summary>
	/// Gets all warnings.
	/// </summary>
	public IReadOnlyList<string> Warnings => _warnings;

	/// <summary>
	/// Gets whether compilation encountered any errors.
	/// </summary>
	public bool HasErrors => _errors.Count > 0;

	/// <summary>
	/// Gets the list of files that were compiled.
	/// </summary>
	public IReadOnlyList<string> CompiledFiles => _compiledFiles;

	/// <summary>
	/// Creates a new project compiler.
	/// </summary>
	/// <param name="project">The project configuration.</param>
	/// <param name="projectDir">The directory containing the project file.</param>
	public ProjectCompiler(ProjectFile project, string projectDir) {
		_project = project;
		_projectDir = Path.GetFullPath(projectDir);
		_errors = [];
		_warnings = [];
		_compiledFiles = [];
		_globalAnalyzer = new SemanticAnalyzer(project.TargetArchitecture);

		// Apply project-level defines to the global symbol table
		foreach (var (name, value) in project.Defines) {
			_globalAnalyzer.SymbolTable.Define(name, SymbolType.Constant, value, new SourceLocation("", 0, 0, 0));
		}

		// Enable auto-labels if configured
		_globalAnalyzer.AutoGenerateRoutineLabels = project.AutoLabels;
	}

	/// <summary>
	/// Compiles the project and returns the generated binary.
	/// </summary>
	/// <returns>The compiled binary, or null if compilation failed.</returns>
	public byte[]? Compile() {
		_errors.Clear();
		_warnings.Clear();
		_compiledFiles.Clear();

		// Validate project file
		var validationErrors = _project.Validate();
		if (validationErrors.Count > 0) {
			foreach (var error in validationErrors) {
				_errors.Add(new ProjectError(error, "", 0, 0));
			}

			return null;
		}

		// Resolve source files
		var sourceFiles = ResolveSourceFiles();
		if (sourceFiles.Count == 0) {
			_errors.Add(new ProjectError("No source files found", "", 0, 0));
			return null;
		}

		// Build include paths
		var includePaths = BuildIncludePaths();

		// Compile all source files into a single AST
		var allStatements = new List<StatementNode>();

		foreach (var sourceFile in sourceFiles) {
			var statements = CompileFile(sourceFile, includePaths);
			if (statements is null) {
				// Errors already recorded
				continue;
			}

			allStatements.AddRange(statements);
			_compiledFiles.Add(sourceFile);
		}

		if (HasErrors) {
			return null;
		}

		// Create combined program
		var location = allStatements.Count > 0 ? allStatements[0].Location : new SourceLocation("", 0, 0, 0);
		var program = new ProgramNode(location, allStatements);

		// Perform semantic analysis
		_globalAnalyzer.Analyze(program);

		if (_globalAnalyzer.HasErrors) {
			foreach (var error in _globalAnalyzer.Errors) {
				_errors.Add(new ProjectError(error.Message, error.Location.FilePath, error.Location.Line, error.Location.Column));
			}

			return null;
		}

		// Generate code
		var generator = new CodeGenerator(_globalAnalyzer, _project.TargetArchitecture);
		var binary = generator.Generate(program);

		if (generator.HasErrors) {
			foreach (var error in generator.Errors) {
				_errors.Add(new ProjectError(error.Message, error.Location.FilePath, error.Location.Line, error.Location.Column));
			}

			return null;
		}

		return binary;
	}

	/// <summary>
	/// Compiles the project and writes output files.
	/// </summary>
	/// <returns>True if compilation succeeded.</returns>
	public bool CompileAndWrite() {
		var binary = Compile();
		if (binary is null) {
			return false;
		}

		// Write output file
		var outputPath = GetOutputPath();
		try {
			var outputDir = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir)) {
				Directory.CreateDirectory(outputDir);
			}

			File.WriteAllBytes(outputPath, binary);
		} catch (Exception ex) {
			_errors.Add(new ProjectError($"Error writing output file: {ex.Message}", outputPath, 0, 0));
			return false;
		}

		// Write symbol file if configured
		if (!string.IsNullOrEmpty(_project.Symbols)) {
			WriteSymbolFile();
		}

		// Write listing file if configured
		if (!string.IsNullOrEmpty(_project.Listing)) {
			WriteListingFile();
		}

		// Write map file if configured
		if (!string.IsNullOrEmpty(_project.MapFile)) {
			WriteMapFile();
		}

		return true;
	}

	/// <summary>
	/// Resolves source file paths from project configuration.
	/// </summary>
	private List<string> ResolveSourceFiles() {
		var files = new List<string>();

		// Add main file if specified
		if (!string.IsNullOrEmpty(_project.Main)) {
			var mainPath = ResolvePath(_project.Main);
			if (File.Exists(mainPath)) {
				files.Add(mainPath);
			} else {
				_errors.Add(new ProjectError($"Main file not found: {_project.Main}", mainPath, 0, 0));
			}
		}

		// Add files from sources list (supports glob patterns)
		foreach (var sourcePattern in _project.Sources) {
			var resolvedFiles = ResolveGlobPattern(sourcePattern);
			files.AddRange(resolvedFiles);
		}

		// Remove duplicates while preserving order
		return files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	/// <summary>
	/// Resolves a glob pattern to a list of files.
	/// </summary>
	private List<string> ResolveGlobPattern(string pattern) {
		var files = new List<string>();

		// Check if it's a glob pattern
		if (pattern.Contains('*') || pattern.Contains('?')) {
			var searchDir = _projectDir;
			var searchPattern = pattern;

			// Extract directory from pattern if present
			var patternDir = Path.GetDirectoryName(pattern);
			if (!string.IsNullOrEmpty(patternDir)) {
				searchDir = Path.Combine(_projectDir, patternDir);
				searchPattern = Path.GetFileName(pattern);
			}

			if (Directory.Exists(searchDir)) {
				var searchOption = pattern.Contains("**")
					? SearchOption.AllDirectories
					: SearchOption.TopDirectoryOnly;

				// Replace ** with * for Directory.GetFiles
				var normalizedPattern = searchPattern.Replace("**", "*");

				try {
					var matchedFiles = Directory.GetFiles(searchDir, normalizedPattern, searchOption);
					files.AddRange(matchedFiles);
				} catch (Exception ex) {
					_warnings.Add($"Error searching pattern '{pattern}': {ex.Message}");
				}
			}
		} else {
			// Single file path
			var filePath = ResolvePath(pattern);
			if (File.Exists(filePath)) {
				files.Add(filePath);
			} else {
				_warnings.Add($"Source file not found: {pattern}");
			}
		}

		return files;
	}

	/// <summary>
	/// Builds include paths from project configuration.
	/// </summary>
	private List<string> BuildIncludePaths() {
		var paths = new List<string> { _projectDir };

		foreach (var includePath in _project.Includes) {
			var fullPath = ResolvePath(includePath);
			if (Directory.Exists(fullPath)) {
				paths.Add(fullPath);
			} else {
				_warnings.Add($"Include directory not found: {includePath}");
			}
		}

		return paths;
	}

	/// <summary>
	/// Compiles a single source file and returns its statements.
	/// </summary>
	private List<StatementNode>? CompileFile(string filePath, List<string> includePaths) {
		// Read source
		string source;
		try {
			source = File.ReadAllText(filePath);
		} catch (Exception ex) {
			_errors.Add(new ProjectError($"Error reading file: {ex.Message}", filePath, 0, 0));
			return null;
		}

		// Preprocess (handle includes)
		var preprocessor = new Preprocessor(includePaths);
		var tokens = preprocessor.Process(source, filePath);

		if (preprocessor.HasErrors) {
			foreach (var error in preprocessor.Errors) {
				_errors.Add(new ProjectError(error.Message, error.Location.FilePath, error.Location.Line, error.Location.Column));
			}

			return null;
		}

		// Check for lexer errors
		var lexerErrors = tokens.Where(t => t.Type == TokenType.Error).ToList();
		if (lexerErrors.Count > 0) {
			foreach (var error in lexerErrors) {
				_errors.Add(new ProjectError(error.Text, error.Location.FilePath, error.Location.Line, error.Location.Column));
			}

			return null;
		}

		// Parse
		var parser = new Parser.Parser(tokens);
		ProgramNode program;
		try {
			program = parser.Parse();
		} catch (ParseException ex) {
			_errors.Add(new ProjectError(ex.Message, ex.Location.FilePath, ex.Location.Line, ex.Location.Column));
			return null;
		}

		return program.Statements.ToList();
	}

	/// <summary>
	/// Resolves a relative path against the project directory.
	/// </summary>
	private string ResolvePath(string relativePath) {
		if (Path.IsPathRooted(relativePath)) {
			return relativePath;
		}

		return Path.GetFullPath(Path.Combine(_projectDir, relativePath));
	}

	/// <summary>
	/// Gets the output file path.
	/// </summary>
	private string GetOutputPath() {
		if (!string.IsNullOrEmpty(_project.Output)) {
			return ResolvePath(_project.Output);
		}

		// Default to project name with appropriate extension
		var extension = _project.TargetArchitecture switch {
			TargetArchitecture.MOS6502 => ".nes",
			TargetArchitecture.WDC65816 => ".sfc",
			TargetArchitecture.SM83 => ".gb",
			_ => ".bin"
		};

		return ResolvePath($"{_project.Name}{extension}");
	}

	/// <summary>
	/// Writes the symbol file.
	/// </summary>
	private void WriteSymbolFile() {
		var symbolPath = ResolvePath(_project.Symbols!);
		try {
			var exporter = new SymbolExporter(_globalAnalyzer.SymbolTable, _project.TargetArchitecture);
			exporter.Export(symbolPath);
		} catch (Exception ex) {
			_warnings.Add($"Error writing symbol file: {ex.Message}");
		}
	}

	/// <summary>
	/// Writes the listing file.
	/// </summary>
	private void WriteListingFile() {
		var listingPath = ResolvePath(_project.Listing!);
		try {
			// Create listing generator and write
			using var writer = new StreamWriter(listingPath);
			writer.WriteLine($"; Poppy Compiler Listing");
			writer.WriteLine($"; Project: {_project.Name}");
			writer.WriteLine($"; Target: {_project.Target}");
			writer.WriteLine($"; Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			writer.WriteLine();

			// Write symbols
			writer.WriteLine("; --- Symbols ---");
			foreach (var symbol in _globalAnalyzer.SymbolTable.Symbols.OrderBy(s => s.Value.Value)) {
				var addr = symbol.Value.Value.HasValue ? $"${symbol.Value.Value.Value:x4}" : "????";
				writer.WriteLine($"; {symbol.Key,-20} = {addr}");
			}

			writer.WriteLine();
			writer.WriteLine("; --- Source Files ---");
			foreach (var file in _compiledFiles) {
				writer.WriteLine($"; {file}");
			}
		} catch (Exception ex) {
			_warnings.Add($"Error writing listing file: {ex.Message}");
		}
	}

	/// <summary>
	/// Writes the memory map file.
	/// </summary>
	private void WriteMapFile() {
		var mapPath = ResolvePath(_project.MapFile!);
		try {
			using var writer = new StreamWriter(mapPath);
			writer.WriteLine($"; Poppy Compiler Memory Map");
			writer.WriteLine($"; Project: {_project.Name}");
			writer.WriteLine($"; Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			writer.WriteLine();

			// Group symbols by type
			var labels = _globalAnalyzer.SymbolTable.Symbols
				.Where(s => s.Value.Type == SymbolType.Label && s.Value.Value.HasValue)
				.OrderBy(s => s.Value.Value!.Value)
				.ToList();

			var constants = _globalAnalyzer.SymbolTable.Symbols
				.Where(s => s.Value.Type == SymbolType.Constant && s.Value.Value.HasValue)
				.OrderBy(s => s.Key)
				.ToList();

			writer.WriteLine("; --- Labels ---");
			foreach (var (name, symbol) in labels) {
				writer.WriteLine($"${symbol.Value!.Value:x4} {name}");
			}

			writer.WriteLine();
			writer.WriteLine("; --- Constants ---");
			foreach (var (name, symbol) in constants) {
				writer.WriteLine($"${symbol.Value!.Value:x4} {name}");
			}
		} catch (Exception ex) {
			_warnings.Add($"Error writing map file: {ex.Message}");
		}
	}

	/// <summary>
	/// Loads and compiles a project from a project file path.
	/// </summary>
	/// <param name="projectPath">Path to the poppy.json file.</param>
	/// <returns>A ProjectCompiler instance.</returns>
	public static ProjectCompiler FromFile(string projectPath) {
		var project = ProjectFile.Load(projectPath);
		var projectDir = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? ".";
		return new ProjectCompiler(project, projectDir);
	}
}

/// <summary>
/// Represents a project compilation error.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="FilePath">The source file path.</param>
/// <param name="Line">The line number (1-based).</param>
/// <param name="Column">The column number (1-based).</param>
public sealed record ProjectError(string Message, string FilePath, int Line, int Column) {
	/// <summary>
	/// Formats the error for display.
	/// </summary>
	public override string ToString() {
		if (string.IsNullOrEmpty(FilePath)) {
			return $"error: {Message}";
		}

		if (Line > 0) {
			return $"{FilePath}({Line},{Column}): error: {Message}";
		}

		return $"{FilePath}: error: {Message}";
	}
}

