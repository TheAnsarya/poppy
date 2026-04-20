using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Poppy.Core.Arch;
using Poppy.Core.CodeGen;
using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

namespace Poppy.Benchmarks;

/// <summary>
/// Benchmarks for the Poppy compiler pipeline: Lexer, Parser, SemanticAnalyzer, CodeGenerator.
/// Uses real-world .pasm example files as input.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PipelineBenchmarks {
	private string _nesSource = null!;
	private string _snesSource = null!;
	private string _gbSource = null!;
	private string _genesisSource = null!;

	// Pre-lexed tokens for parser-only benchmarks
	private List<Token> _nesTokens = null!;
	private List<Token> _snesTokens = null!;

	// Pre-parsed ASTs for semantic-only benchmarks
	private ProgramNode _nesAst = null!;
	private ProgramNode _snesAst = null!;

	[GlobalSetup]
	public void Setup() {
		// Register architecture profiles
		Poppy.Arch.MOS6502.Registration.RegisterAll();
		Poppy.Arch.WDC65816.Registration.RegisterAll();
		Poppy.Arch.SM83.Registration.RegisterAll();
		Poppy.Arch.M68000.Registration.RegisterAll();
		Poppy.Arch.Z80.Registration.RegisterAll();
		Poppy.Arch.V30MZ.Registration.RegisterAll();
		Poppy.Arch.ARM7TDMI.Registration.RegisterAll();
		Poppy.Arch.SPC700.Registration.RegisterAll();
		Poppy.Arch.HuC6280.Registration.RegisterAll();

		var examplesDir = FindExamplesDirectory();

		_nesSource = File.ReadAllText(Path.Combine(examplesDir, "nes-hello-world", "main.pasm"));
		_snesSource = File.ReadAllText(Path.Combine(examplesDir, "snes-hello-world", "main.pasm"));
		_gbSource = File.ReadAllText(Path.Combine(examplesDir, "gb-hello-world", "main.pasm"));
		_genesisSource = File.ReadAllText(Path.Combine(examplesDir, "genesis-hello-world", "main.pasm"));

		// Pre-lex for parser benchmarks
		_nesTokens = new Lexer(_nesSource, "nes.pasm").Tokenize();
		_snesTokens = new Lexer(_snesSource, "snes.pasm").Tokenize();

		// Pre-parse for semantic benchmarks
		_nesAst = new Parser(_nesTokens).Parse();
		_snesAst = new Parser(_snesTokens).Parse();
	}

	// ========================================================================
	// Lexer Benchmarks
	// ========================================================================

	[Benchmark(Description = "Lexer: NES (7.6 KB)")]
	public List<Token> Lexer_NES() {
		return new Lexer(_nesSource, "nes.pasm").Tokenize();
	}

	[Benchmark(Description = "Lexer: SNES (4.4 KB)")]
	public List<Token> Lexer_SNES() {
		return new Lexer(_snesSource, "snes.pasm").Tokenize();
	}

	[Benchmark(Description = "Lexer: GB (4.8 KB)")]
	public List<Token> Lexer_GB() {
		return new Lexer(_gbSource, "gb.pasm").Tokenize();
	}

	[Benchmark(Description = "Lexer: Genesis (5.4 KB)")]
	public List<Token> Lexer_Genesis() {
		return new Lexer(_genesisSource, "genesis.pasm").Tokenize();
	}

	// ========================================================================
	// Parser Benchmarks
	// ========================================================================

	[Benchmark(Description = "Parser: NES")]
	public ProgramNode Parser_NES() {
		return new Parser(new List<Token>(_nesTokens)).Parse();
	}

	[Benchmark(Description = "Parser: SNES")]
	public ProgramNode Parser_SNES() {
		return new Parser(new List<Token>(_snesTokens)).Parse();
	}

	// ========================================================================
	// Full Pipeline (Lex + Parse + Analyze)
	// ========================================================================

	[Benchmark(Description = "Pipeline: NES (Lex+Parse+Analyze)")]
	public SemanticAnalyzer FullPipeline_NES() {
		var tokens = new Lexer(_nesSource, "nes.pasm").Tokenize();
		var ast = new Parser(tokens).Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(ast);
		return analyzer;
	}

	[Benchmark(Description = "Pipeline: SNES (Lex+Parse+Analyze)")]
	public SemanticAnalyzer FullPipeline_SNES() {
		var tokens = new Lexer(_snesSource, "snes.pasm").Tokenize();
		var ast = new Parser(tokens).Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(ast);
		return analyzer;
	}

	// ========================================================================
	// Full Pipeline (Lex + Parse + Analyze + CodeGen)
	// ========================================================================

	[Benchmark(Description = "Pipeline: NES (Full Compile)")]
	public byte[] FullCompile_NES() {
		var tokens = new Lexer(_nesSource, "nes.pasm").Tokenize();
		var ast = new Parser(tokens).Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
		analyzer.Analyze(ast);
		return new CodeGenerator(analyzer, TargetArchitecture.MOS6502).Generate(ast);
	}

	[Benchmark(Description = "Pipeline: SNES (Full Compile)")]
	public byte[] FullCompile_SNES() {
		var tokens = new Lexer(_snesSource, "snes.pasm").Tokenize();
		var ast = new Parser(tokens).Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.WDC65816);
		analyzer.Analyze(ast);
		return new CodeGenerator(analyzer, TargetArchitecture.WDC65816).Generate(ast);
	}

	[Benchmark(Description = "Pipeline: GB (Full Compile)")]
	public byte[] FullCompile_GB() {
		var tokens = new Lexer(_gbSource, "gb.pasm").Tokenize();
		var ast = new Parser(tokens).Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.SM83);
		analyzer.Analyze(ast);
		return new CodeGenerator(analyzer, TargetArchitecture.SM83).Generate(ast);
	}

	[Benchmark(Description = "Pipeline: Genesis (Full Compile)")]
	public byte[] FullCompile_Genesis() {
		var tokens = new Lexer(_genesisSource, "genesis.pasm").Tokenize();
		var ast = new Parser(tokens).Parse();
		var analyzer = new SemanticAnalyzer(TargetArchitecture.M68000);
		analyzer.Analyze(ast);
		return new CodeGenerator(analyzer, TargetArchitecture.M68000).Generate(ast);
	}

	// ========================================================================
	// Helpers
	// ========================================================================

	private static string FindExamplesDirectory() {
		// Try multiple strategies to find the examples directory
		// 1. Relative to current directory (when running from repo root)
		var candidates = new[] {
			Path.Combine(Environment.CurrentDirectory, "examples"),
			Path.Combine(Environment.CurrentDirectory, "..", "..", "examples"),
			Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "examples"),
			Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "examples"),
			Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "..", "examples"),
		};

		foreach (var candidate in candidates) {
			var full = Path.GetFullPath(candidate);
			if (Directory.Exists(full)) {
				return full;
			}
		}

		// 2. Walk up from AppContext.BaseDirectory
		var dir = AppContext.BaseDirectory;
		for (int i = 0; i < 15; i++) {
			var candidate = Path.Combine(dir, "examples");
			if (Directory.Exists(candidate)) {
				return candidate;
			}

			var parent = Path.GetDirectoryName(dir);
			if (parent == null || parent == dir) break;
			dir = parent;
		}

		throw new DirectoryNotFoundException(
			$"Could not find examples/ directory. CWD={Environment.CurrentDirectory}, Base={AppContext.BaseDirectory}");
	}
}
