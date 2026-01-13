using Poppy.Core.Lexer;
using Poppy.Core.Parser;
using Poppy.Core.Semantics;

var source = @"
.macro load_default value=$42
	lda #value
.endmacro

@load_default
";

var lexer = new Lexer(source, "test.pasm");
var tokens = lexer.Tokenize();
var parser = new Parser(tokens);
var program = parser.Parse();

var analyzer = new SemanticAnalyzer(TargetArchitecture.MOS6502);
analyzer.Analyze(program);

Console.WriteLine($"Has Errors: {analyzer.HasErrors}");
if (analyzer.HasErrors) {
	Console.WriteLine("Errors:");
	foreach (var error in analyzer.Errors) {
		Console.WriteLine($"  - {error}");
	}
}
