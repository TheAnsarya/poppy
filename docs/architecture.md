# 🏗️ Poppy Compiler Architecture

> Design Document v0.1 - January 11, 2026

This document outlines the architecture and design of the Poppy multi-system assembler.

---

## 📐 High-Level Architecture

```text
┌─────────────────────────────────────────────────────────────────────┐
│                           POPPY COMPILER                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────────────┐  │
│  │  Source │───▶│  Lexer  │───▶│ Parser  │───▶│       AST       │  │
│  │  Files  │    │         │    │         │    │ (Abstract Tree) │  │
│  └─────────┘    └─────────┘    └─────────┘    └────────┬────────┘  │
│                                                        │           │
│                                                        ▼           │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                     SEMANTIC ANALYSIS                        │   │
│  │  • Symbol Resolution  • Type Checking  • Expression Eval     │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                        │           │
│                                                        ▼           │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                      CODE GENERATOR                          │   │
│  │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────────────┐ │   │
│  │  │  6502   │  │  65816  │  │  SM83   │  │     SPC700      │ │   │
│  │  │  (NES)  │  │ (SNES)  │  │  (GB)   │  │  (SNES Audio)   │ │   │
│  │  └─────────┘  └─────────┘  └─────────┘  └─────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                        │           │
│                                                        ▼           │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                       OUTPUT STAGE                           │   │
│  │  • Binary Generation  • ROM Building  • Patch Creation       │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🧩 Component Details

### 1. Lexer (Tokenizer)

**Purpose**: Convert source text into a stream of tokens.

**Input**: Raw source code text
**Output**: Token stream

#### Token Types

```csharp
enum TokenType {
	// Literals
	Number,             // $ff, 255, %1010
	String,             // "hello"
	Character,          // 'A'

	// Identifiers
	Identifier,         // labels, defines
	Mnemonic,           // lda, sta, jmp
	Directive,          // .org, .db, .include

	// Operators
	Plus,               // +
	Minus,              // -
	Star,               // *
	Slash,              // /
	Percent,            // %
	Ampersand,          // &
	Pipe,               // |
	Caret,              // ^
	Tilde,              // ~
	LessThan,           // <
	GreaterThan,        // >
	Equals,             // =
	Bang,               // !
	Hash,               // #
	LeftShift,          // <<
	RightShift,         // >>

	// Delimiters
	Colon,              // :
	Comma,              // ,
	Dot,                // .
	LeftParen,          // (
	RightParen,         // )
	LeftBracket,        // [
	RightBracket,       // ]

	// Special
	Newline,            // End of statement
	Comment,            // ; or /* */
	EndOfFile,          // EOF
}
```

#### Lexer State Machine

```text
┌───────────┐  letter    ┌────────────┐
│   START   │───────────▶│ IDENTIFIER │
└───────────┘            └────────────┘
      │
      │ '$'     ┌─────────────┐
      ├────────▶│  HEX_NUMBER │
      │         └─────────────┘
      │
      │ '%'     ┌─────────────┐
      ├────────▶│  BIN_NUMBER │
      │         └─────────────┘
      │
      │ digit   ┌─────────────┐
      ├────────▶│  DEC_NUMBER │
      │         └─────────────┘
      │
      │ '"'     ┌─────────────┐
      └────────▶│   STRING    │
                └─────────────┘
```

---

### 2. Parser

**Purpose**: Convert token stream into Abstract Syntax Tree (AST).

**Input**: Token stream
**Output**: AST

#### Grammar (Simplified EBNF)

```ebnf
program         = { line } EOF ;
line            = [ label ] [ statement ] NEWLINE ;
label           = IDENTIFIER ":" ;
statement       = instruction | directive | assignment ;
instruction     = mnemonic [ operand ] ;
directive       = "." IDENTIFIER [ arguments ] ;
assignment      = IDENTIFIER "=" expression ;
operand         = addressing_mode ;
expression      = term { ("+" | "-") term } ;
term            = factor { ("*" | "/" | "%") factor } ;
factor          = NUMBER | IDENTIFIER | "(" expression ")" | unary ;
unary           = ("<" | ">" | "^" | "~" | "-") factor ;
```

#### AST Node Types

```csharp
abstract class AstNode {
	SourceLocation Location { get; }
}

class ProgramNode : AstNode {
	List<StatementNode> Statements { get; }
}

class LabelNode : AstNode {
	string Name { get; }
	bool IsLocal { get; }
}

class InstructionNode : AstNode {
	string Mnemonic { get; }
	OperandNode Operand { get; }
	AddressingMode Mode { get; }
}

class DirectiveNode : AstNode {
	string Name { get; }
	List<ExpressionNode> Arguments { get; }
}

class ExpressionNode : AstNode {
	// Binary, Unary, Literal, Identifier, etc.
}
```

---

### 3. Semantic Analyzer

**Purpose**: Validate AST and resolve symbols.

**Responsibilities**:

- Build symbol table
- Resolve label references
- Evaluate constant expressions
- Check instruction validity for target architecture
- Process defines and macros

#### Symbol Table

```csharp
class SymbolTable {
	Dictionary<string, Symbol> GlobalSymbols { get; }
	Stack<Scope> ScopeStack { get; }

	void DefineLabel(string name, int address);
	void DefineConstant(string name, int value);
	Symbol Resolve(string name);
}

class Symbol {
	string Name { get; }
	SymbolType Type { get; }  // Label, Constant, Macro
	int Value { get; }
	int Bank { get; }
	bool IsDefined { get; }
	List<Reference> References { get; }
}
```

---

### 4. Code Generator

**Purpose**: Convert validated AST to machine code.

**Architecture**: Strategy pattern for different CPU targets.

```csharp
interface ICodeGenerator {
	void Initialize(OutputBuffer buffer);
	void EmitInstruction(InstructionNode node);
	void EmitData(DataNode node);
	byte[] GetOutput();
}

class CodeGenerator6502 : ICodeGenerator {
	// 6502-specific encoding
}

class CodeGenerator65816 : ICodeGenerator {
	// 65816-specific encoding with mode tracking
}

class CodeGeneratorSM83 : ICodeGenerator {
	// Game Boy-specific encoding
}
```

#### Multi-Pass Assembly

```text
Pass 1: Symbol Collection
  - Scan all labels
  - Calculate preliminary addresses
  - Note forward references

Pass 2: Code Generation
  - Resolve all references
  - Generate final machine code
  - Verify address ranges
```

---

### 5. Output Stage

**Purpose**: Generate final output files.

#### Output Formats

| Format | Description | Use Case |
|--------|-------------|----------|
| Raw Binary | Plain machine code | ROM insertion |
| iNES | NES ROM format | NES homebrew |
| SFC/SMC | SNES ROM format | SNES homebrew |
| GB/GBC | Game Boy ROM | GB homebrew |
| IPS Patch | Simple patches | ROM hacking |
| BPS Patch | Better patches | ROM hacking |

---

## 📁 Project Structure

```text
src/
├── Poppy.Core/              # Core library
│   ├── Lexer/
│   │   ├── Lexer.cs
│   │   ├── Token.cs
│   │   └── TokenType.cs
│   ├── Parser/
│   │   ├── Parser.cs
│   │   ├── Ast/
│   │   │   ├── AstNode.cs
│   │   │   ├── InstructionNode.cs
│   │   │   └── ...
│   │   └── Grammar/
│   ├── Semantic/
│   │   ├── SemanticAnalyzer.cs
│   │   ├── SymbolTable.cs
│   │   └── ExpressionEvaluator.cs
│   ├── CodeGen/
│   │   ├── ICodeGenerator.cs
│   │   ├── CodeGenerator6502.cs
│   │   ├── CodeGenerator65816.cs
│   │   └── CodeGeneratorSM83.cs
│   ├── Output/
│   │   ├── OutputBuffer.cs
│   │   ├── RomBuilder.cs
│   │   └── PatchGenerator.cs
│   └── Common/
│       ├── ErrorReporter.cs
│       ├── SourceLocation.cs
│       └── DiagnosticMessage.cs
│
├── Poppy.Cli/               # Command-line interface
│   ├── Program.cs
│   └── CommandLineOptions.cs
│
├── Poppy.Tests/             # Unit tests
│   ├── Lexer/
│   ├── Parser/
│   ├── CodeGen/
│   └── Integration/
│
└── Poppy.Benchmarks/        # Performance tests
```

---

## 🔄 Data Flow

```text
Source File
    │
    ▼
┌─────────────────────────────────────┐
│              LEXER                  │
│  "lda #$ff" → [LDA] [#] [$FF]       │
└─────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────┐
│              PARSER                 │
│  Instruction(LDA, Immediate($FF))   │
└─────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────┐
│         SEMANTIC ANALYSIS           │
│  Validate: LDA supports Immediate   │
│  Evaluate: $FF = 255                │
└─────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────┐
│          CODE GENERATOR             │
│  LDA #$FF → [A9 FF]                 │
└─────────────────────────────────────┘
    │
    ▼
Binary Output
```

---

## 🎯 Design Principles

### 1. Separation of Concerns

Each component has a single responsibility:

- Lexer: Text → Tokens
- Parser: Tokens → AST
- Analyzer: AST validation
- Generator: AST → Machine code

### 2. Extensibility

Adding a new CPU target requires:

1. Instruction definition table
2. Code generator implementation
3. Test suite

### 3. Error Handling

```csharp
class DiagnosticMessage {
	DiagnosticSeverity Severity { get; }  // Error, Warning, Info
	string Code { get; }                   // P0001, P0002, etc.
	string Message { get; }
	SourceLocation Location { get; }
	string[] Suggestions { get; }
}

// Example:
// error P0001: Unknown mnemonic 'xyz'
//   --> main.asm:10:5
//   |
// 10|     xyz #$00
//   |     ^^^
//   = suggestion: Did you mean 'xor'?
```

### 4. Two-Phase Design

Phase 1: Symbol collection (addresses unknown)
Phase 2: Code generation (addresses resolved)

This handles forward references efficiently.

---

## 🧪 Testing Strategy

### Unit Tests

```csharp
[Test]
public void Lexer_HexNumber_ParsesCorrectly() {
	var lexer = new Lexer("$ff");
	var token = lexer.NextToken();
	Assert.AreEqual(TokenType.Number, token.Type);
	Assert.AreEqual(0xff, token.Value);
}

[Test]
public void CodeGen_LdaImmediate_EncodesCorrectly() {
	var gen = new CodeGenerator6502();
	var node = new InstructionNode("lda", AddressingMode.Immediate, 0xff);
	gen.EmitInstruction(node);
	Assert.AreEqual(new byte[] { 0xa9, 0xff }, gen.GetOutput());
}
```

### Integration Tests

- Assemble known working files
- Compare output with reference assemblers
- Test error message quality

---

## 📈 Performance Considerations

1. **Streaming Lexer**: Don't load entire file into memory
2. **Intern Strings**: Share string references for identifiers
3. **Pre-compiled Instruction Tables**: Fast opcode lookup
4. **Parallel File Processing**: For multi-file projects

---

## 🔮 Future Considerations

1. **Language Server Protocol (LSP)**: IDE integration
2. **Debugger Support**: Source-level debugging info
3. **Optimization Passes**: Peephole optimization
4. **Linker**: Separate compilation and linking

---

## 📝 Implementation Order

1. **Phase 1**: Basic lexer and token types
2. **Phase 2**: Parser for simple instructions
3. **Phase 3**: 6502 code generator
4. **Phase 4**: Directives (.org, .db, etc.)
5. **Phase 5**: Labels and expressions
6. **Phase 6**: 65816 code generator
7. **Phase 7**: Macros and defines
8. **Phase 8**: SM83 code generator
9. **Phase 9**: Output formats (iNES, SFC)
10. **Phase 10**: Polish and optimization

---

