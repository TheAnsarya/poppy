using Poppy.Core.Lexer;
using Poppy.Core.Parser;

var source = @"
.ifeq 5
.byte $01
.endif
";

var lexer = new Lexer(source, "test.pasm");
var tokens = lexer.Tokenize();

Console.WriteLine("Tokens:");
foreach (var token in tokens) {
Console.WriteLine($"  {token.Type}: '{token.Text}'");
}

var parser = new Parser(tokens);
try {
var program = parser.Parse();
Console.WriteLine("\nParse succeeded - no exception thrown!");
Console.WriteLine($"Statements: {program.Statements.Count}");
} catch (ParseException ex) {
Console.WriteLine($"\nParseException thrown: {ex.Message}");
}
