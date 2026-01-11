// ============================================================================
// Program.cs - Poppy Compiler CLI Entry Point
// Poppy Compiler - Multi-system Assembly Compiler
// ============================================================================

using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

namespace Poppy.CLI;

/// <summary>
/// Main entry point for the Poppy compiler CLI.
/// </summary>
internal static class Program
{
	private static readonly string Version = "0.1.0";
	private static readonly string AppName = "Poppy Compiler";

	/// <summary>
	/// Main entry point.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	/// <returns>Exit code (0 for success).</returns>
	public static int Main(string[] args)
	{
		// Parse command line arguments
		var options = ParseArguments(args);

		if (options.ShowHelp) {
			ShowHelp();
			return 0;
		}

		if (options.ShowVersion) {
			ShowVersion();
			return 0;
		}

		if (options.InputFile is null) {
			Console.Error.WriteLine("Error: No input file specified.");
			Console.Error.WriteLine("Use --help for usage information.");
			return 1;
		}

		// Compile the file
		return Compile(options);
	}

	/// <summary>
	/// Compiles a source file.
	/// </summary>
	private static int Compile(CompilerOptions options)
	{
		var inputFile = options.InputFile!;

		// Check input file exists
		if (!File.Exists(inputFile)) {
			Console.Error.WriteLine($"Error: Input file not found: {inputFile}");
			return 1;
		}

		// Read source file
		string source;
		try {
			source = File.ReadAllText(inputFile);
		}
		catch (Exception ex) {
			Console.Error.WriteLine($"Error reading input file: {ex.Message}");
			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"Compiling: {inputFile}");
		}

		// Lexical analysis
		var lexer = new Lexer(source);
		var tokens = lexer.Tokenize();

		// Check for lexer errors (Error tokens)
		var lexerErrors = tokens.Where(t => t.Type == TokenType.Error).ToList();
		if (lexerErrors.Count > 0) {
			foreach (var error in lexerErrors) {
				Console.Error.WriteLine($"{inputFile}:{error.Location.Line}:{error.Location.Column}: error: {error.Text}");
			}
			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"  Tokenized: {tokens.Count} tokens");
		}

		// Parsing
		var parser = new Parser(tokens);
		ProgramNode program;
		try {
			program = parser.Parse();
		}
		catch (ParseException ex) {
			Console.Error.WriteLine($"{inputFile}:{ex.Location.Line}:{ex.Location.Column}: error: {ex.Message}");
			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"  Parsed: {program.Statements.Count} statements");
		}

		// Semantic analysis
		var analyzer = new SemanticAnalyzer(options.Target);
		analyzer.Analyze(program);

		if (analyzer.HasErrors) {
			foreach (var error in analyzer.Errors) {
				Console.Error.WriteLine($"{inputFile}:{error.Location.Line}:{error.Location.Column}: error: {error.Message}");
			}
			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"  Analyzed: {analyzer.SymbolTable.Symbols.Count} symbols defined");
		}

		// Code generation
		var generator = new CodeGenerator(analyzer, options.Target);
		var code = generator.Generate(program);

		if (generator.HasErrors) {
			foreach (var error in generator.Errors) {
				Console.Error.WriteLine($"{inputFile}:{error.Location.Line}:{error.Location.Column}: error: {error.Message}");
			}
			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"  Generated: {code.Length} bytes");
		}

		// Determine output file
		var outputFile = options.OutputFile ?? Path.ChangeExtension(inputFile, ".bin");

		// Write output
		try {
			File.WriteAllBytes(outputFile, code);
		}
		catch (Exception ex) {
			Console.Error.WriteLine($"Error writing output file: {ex.Message}");
			return 1;
		}

		if (options.Verbose) {
			Console.WriteLine($"  Output: {outputFile}");
		}

		// Write listing file if requested
		if (options.ListingFile is not null) {
			try {
				WriteListing(options.ListingFile, program, analyzer, code);
				if (options.Verbose) {
					Console.WriteLine($"  Listing: {options.ListingFile}");
				}
			}
			catch (Exception ex) {
				Console.Error.WriteLine($"Error writing listing file: {ex.Message}");
				return 1;
			}
		}

		Console.WriteLine($"Assembled {inputFile} -> {outputFile} ({code.Length} bytes)");
		return 0;
	}

	/// <summary>
	/// Writes a listing file.
	/// </summary>
	private static void WriteListing(string filename, ProgramNode program, SemanticAnalyzer analyzer, byte[] code)
	{
		using var writer = new StreamWriter(filename);

		writer.WriteLine($"; {AppName} v{Version} Listing");
		writer.WriteLine($"; Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
		writer.WriteLine();

		writer.WriteLine("; Symbol Table");
		writer.WriteLine("; " + new string('-', 60));

		foreach (var symbol in analyzer.SymbolTable.Symbols.Values.OrderBy(s => s.Name)) {
			var valueStr = symbol.Value.HasValue ? $"${symbol.Value.Value:x4}" : "(undefined)";
			writer.WriteLine($"; {symbol.Name,-20} = {valueStr,-10} ({symbol.Type})");
		}

		writer.WriteLine();
		writer.WriteLine($"; Total code size: {code.Length} bytes (${code.Length:x})");
	}

	/// <summary>
	/// Parses command line arguments.
	/// </summary>
	private static CompilerOptions ParseArguments(string[] args)
	{
		var options = new CompilerOptions();

		for (int i = 0; i < args.Length; i++) {
			var arg = args[i];

			switch (arg) {
				case "-h":
				case "--help":
					options.ShowHelp = true;
					break;

				case "-v":
				case "--version":
					options.ShowVersion = true;
					break;

				case "-V":
				case "--verbose":
					options.Verbose = true;
					break;

				case "-o":
				case "--output":
					if (i + 1 < args.Length) {
						options.OutputFile = args[++i];
					}
					break;

				case "-l":
				case "--listing":
					if (i + 1 < args.Length) {
						options.ListingFile = args[++i];
					}
					break;

				case "-t":
				case "--target":
					if (i + 1 < args.Length) {
						options.Target = args[++i].ToLowerInvariant() switch {
							"6502" or "nes" => TargetArchitecture.MOS6502,
							"65816" or "snes" => TargetArchitecture.WDC65816,
							"sm83" or "gb" or "gameboy" => TargetArchitecture.SM83,
							_ => options.Target
						};
						i++;
					}
					break;

				default:
					if (!arg.StartsWith('-')) {
						options.InputFile = arg;
					}
					break;
			}
		}

		return options;
	}

	/// <summary>
	/// Shows help message.
	/// </summary>
	private static void ShowHelp()
	{
		Console.WriteLine($"{AppName} v{Version}");
		Console.WriteLine();
		Console.WriteLine("Usage: poppy [options] <input.asm>");
		Console.WriteLine();
		Console.WriteLine("Options:");
		Console.WriteLine("  -h, --help           Show this help message");
		Console.WriteLine("  -v, --version        Show version information");
		Console.WriteLine("  -V, --verbose        Enable verbose output");
		Console.WriteLine("  -o, --output <file>  Output file (default: input.bin)");
		Console.WriteLine("  -l, --listing <file> Generate listing file");
		Console.WriteLine("  -t, --target <arch>  Target architecture:");
		Console.WriteLine("                         6502, nes     - MOS 6502 (default)");
		Console.WriteLine("                         65816, snes   - WDC 65816");
		Console.WriteLine("                         sm83, gb      - Sharp SM83 (Game Boy)");
		Console.WriteLine();
		Console.WriteLine("Examples:");
		Console.WriteLine("  poppy game.asm                    Assemble to game.bin");
		Console.WriteLine("  poppy -o rom.nes game.asm         Assemble to rom.nes");
		Console.WriteLine("  poppy -t snes -l game.lst game.asm");
	}

	/// <summary>
	/// Shows version information.
	/// </summary>
	private static void ShowVersion()
	{
		Console.WriteLine($"{AppName} v{Version}");
		Console.WriteLine("Target architectures: 6502, 65816, SM83");
		Console.WriteLine("Copyright (c) 2024");
	}
}

/// <summary>
/// Compiler options parsed from command line.
/// </summary>
internal sealed class CompilerOptions
{
	/// <summary>Input source file.</summary>
	public string? InputFile { get; set; }

	/// <summary>Output binary file.</summary>
	public string? OutputFile { get; set; }

	/// <summary>Listing file path.</summary>
	public string? ListingFile { get; set; }

	/// <summary>Target architecture.</summary>
	public TargetArchitecture Target { get; set; } = TargetArchitecture.MOS6502;

	/// <summary>Show help message.</summary>
	public bool ShowHelp { get; set; }

	/// <summary>Show version.</summary>
	public bool ShowVersion { get; set; }

	/// <summary>Enable verbose output.</summary>
	public bool Verbose { get; set; }
}
