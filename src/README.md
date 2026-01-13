# 🏗️ Poppy Source Code

This folder will contain the Poppy Compiler source code.

## Structure (Planned)

```
src/
├── Poppy.Core/              # Core compiler library
│   ├── Lexer/               # Tokenization
│   ├── Parser/              # AST generation
│   ├── Semantic/            # Symbol resolution
│   ├── CodeGen/             # Machine code output
│   ├── Output/              # ROM/patch generation
│   └── Common/              # Shared utilities
│
├── Poppy.Cli/               # Command-line interface
│
├── Poppy.Tests/             # Unit tests
│
└── Poppy.sln                # Solution file
```

## Technology

- **Language:** C# (.NET 8+)
- **Target:** Cross-platform (Windows, macOS, Linux)
- **Build:** MSBuild / dotnet CLI

---

