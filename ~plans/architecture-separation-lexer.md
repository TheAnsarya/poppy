# Architecture Separation: Target-Aware Lexer Design

## Research for [#240](https://github.com/TheAnsarya/poppy/issues/240)

Parent: [#238](https://github.com/TheAnsarya/poppy/issues/238)

## Problem

The `Lexer.IsMnemonic()` method (line 471 in `Lexer.cs`) recognizes mnemonics from **all 11+ architectures simultaneously**. A `TODO` comment acknowledges this: `// TODO: Make this architecture-aware`.

This means:

- `mov` (V30MZ/ARM) is tokenized as a mnemonic when assembling NES code
- `halt` (SM83) is a mnemonic during SNES assembly
- `dbcc` (M68000) is a mnemonic during Game Boy assembly
- Future mnemonic conflicts between architectures would be unresolvable

## Current Pipeline

```
Lexer (doesn't know target) → classifies ALL known mnemonics as TokenType.Mnemonic
  ↓
Parser (builds AST, tokens already classified)
  ↓
SemanticAnalyzer (NOW sees .target, calls TargetResolver.Resolve())
  ↓
CodeGenerator (has _profile with Encoder)
```

The `.target` directive is processed in **Pass 1 of SemanticAnalyzer** (line ~1078), far too late for the lexer.

## Current Lexer Constructor

```csharp
public Lexer(string source, string filePath = "<input>") {
	_source = source;
	_filePath = filePath;
	_position = 0;
	_line = 1;
	_column = 1;
}
```

No architecture parameter. The `IsMnemonic` method is `static`, returning `true` for ~400+ mnemonics across all architectures.

## Current IInstructionEncoder Interface

```csharp
public interface IInstructionEncoder {
	bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding);
	bool IsBranchInstruction(string mnemonic);
}
```

No `Mnemonics` property. Some InstructionSet classes (e.g., `InstructionSetARM7TDMI`) expose `FrozenSet<string>` properties like `ArmMnemonics`, but this isn't standardized.

## Design Options

### Option A: Two-Phase Lexing (Recommended)

1. **Pre-scan** for `.target` directive before full lexing
2. Load the appropriate mnemonic set based on target
3. Full lex with target-aware `IsMnemonic()`

**Pros:** Clean separation, correct mnemonic classification from the start
**Cons:** Slight overhead from pre-scan (negligible — finding `.target` is ~1 regex or simple scan)

### Option B: Deferred Mnemonic Resolution

Keep current behavior (accept all), validate in semantic phase.

**Pros:** Zero lexer changes, backward compatible
**Cons:** Doesn't solve the problem — mnemonic conflicts remain undetectable until semantic analysis. Bad error messages.

### Option C: Command-Line Target (Always Known)

Require target as CLI parameter or project config. Lexer is always given the target.

**Pros:** Simple, always has context
**Cons:** `.target` directive becomes redundant, breaks current workflow

### Option D: Hybrid (Recommended Implementation)

1. Add `IReadOnlySet<string> Mnemonics` to `IInstructionEncoder`
2. Add optional `TargetArchitecture?` to Lexer constructor
3. If target known at construction: use target-specific mnemonic set
4. If target unknown: use union of all mnemonics (current behavior)
5. `Program.cs` / `CompilationPipeline` pre-scans for `.target` before creating Lexer

**Pros:** Backward compatible, progressively better as target resolution improves
**Cons:** Slight complexity in supporting both modes

## Decision: Option D (Hybrid)

### Rationale

- **Backward compatible**: Existing code that creates `new Lexer(source)` gets current behavior
- **Progressive improvement**: When target is known (CLI flag, project config, pre-scan), mnemonics are filtered
- **Minimal risk**: Falls back to union set when target is unknown
- **Foundation for #245**: Per-architecture projects will make mnemonic sets natural

## Implementation Plan

### Step 1: Add `Mnemonics` to `IInstructionEncoder`

```csharp
public interface IInstructionEncoder {
	bool TryEncode(string mnemonic, AddressingMode mode, out EncodedInstruction encoding);
	bool IsBranchInstruction(string mnemonic);
	IReadOnlySet<string> Mnemonics { get; }  // NEW
}
```

Each encoder exposes its mnemonic set. For dictionary-based encoders, extract unique keys. For ARM7TDMI, expose existing `ArmMnemonics`/`ThumbMnemonics` union.

### Step 2: Add Optional Target to Lexer

```csharp
public Lexer(string source, string filePath = "<input>", IReadOnlySet<string>? mnemonics = null) {
	_mnemonics = mnemonics;
	...
}

private bool IsMnemonic(string text) {
	if (_mnemonics is not null) {
		return _mnemonics.Contains(text);
	}
	// Fallback: current monolithic switch
	return text switch { ... };
}
```

### Step 3: Pre-Scan in Pipeline

```csharp
// In Program.cs or CompilationPipeline
var target = PreScanForTarget(source);
var profile = target.HasValue ? TargetResolver.Resolve(target.Value) : null;
var mnemonics = profile?.Encoder.Mnemonics;
var lexer = new Lexer(source, filePath, mnemonics);
```

### Step 4: Benchmark

Compare `FrozenSet<string>.Contains()` vs current `switch` expression for mnemonic lookup:

- **FrozenSet**: O(1) hash lookup, case-insensitive via `StringComparer.OrdinalIgnoreCase`
- **Switch**: Compiler-generated jump table (very fast for known strings)
- Expected: both sub-nanosecond — no measurable difference in typical usage

## Performance Considerations

The switch expression compiles to a hash-based jump table in Roslyn. `FrozenSet<string>` uses perfect hashing. Both should be extremely fast. The real benefit is **correctness**, not performance.

Benchmark to create: `MnemonicLookupBenchmarks.cs` comparing:

- Current `switch` expression
- `FrozenSet<string>` with `OrdinalIgnoreCase`
- `HashSet<string>` with `OrdinalIgnoreCase`

## Files to Modify

| File | Change |
|------|--------|
| `Arch/IInstructionEncoder.cs` | Add `Mnemonics` property |
| All 11 encoder implementations | Implement `Mnemonics` |
| `Lexer/Lexer.cs` | Add optional mnemonic set parameter |
| `Program.cs` | Pre-scan for target before lexing |
| `Poppy.Benchmarks/` | Add `MnemonicLookupBenchmarks.cs` |

## Backward Compatibility

- `new Lexer(source)` — unchanged, uses monolithic set
- `new Lexer(source, filePath, mnemonics)` — target-aware
- All existing tests pass without modification
- New tests verify target-specific mnemonic filtering

## Next Steps

1. Implement `Mnemonics` property on `IInstructionEncoder` and all encoders
2. Add optional parameter to `Lexer` constructor
3. Add pre-scan utility
4. Create benchmarks
5. Update tests
