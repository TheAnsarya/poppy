# GitHub Copilot Instructions for Poppy Compiler

## Project Overview

**Poppy Compiler** is a multi-system assembly compiler targeting retro gaming platforms (NES, SNES, GB, GBA, Genesis, Lynx, PCE, WonderSwan, Atari 2600). The project aims to compile projects like DW1, FFMQ, DW4, and DQ3r.

**Purpose:**
- Multi-architecture assembler (6502, 65816, SM83, M68000, ARM7TDMI, etc.)
- C# core library for compilation pipeline
- CLI tool for assembling `.pasm` files
- VS Code extension for syntax highlighting and diagnostics
- Pansy metadata generation (symbols, CDL, cross-refs)
- Integration with Peony (disassembler) and Pansy (metadata format)

## GitHub Issue Management

### ⚠️ CRITICAL: Always Create Issues on GitHub Directly

**NEVER just document issues in markdown files.** Always create actual GitHub issues using the `gh` CLI:

```powershell
# Create an issue
gh issue create --repo TheAnsarya/poppy --title "Issue Title" --body "Description" --label "label1,label2"

# Add labels
gh issue edit <number> --repo TheAnsarya/poppy --add-label "label"

# Close issue
gh issue close <number> --repo TheAnsarya/poppy --comment "Completed in commit abc123"
```

### ⚠️ MANDATORY: Issue-First Workflow

**Always create GitHub issues BEFORE starting implementation work.** This is non-negotiable.

1. **Before Implementation:**
   - Create a GitHub issue describing the planned work
   - Include scope, approach, and acceptance criteria
   - Add appropriate labels

2. **During Implementation:**
   - Reference issue number in commits: `git commit -m "Fix parser bug - #247"`
   - Update issue with progress comments if work spans multiple sessions
   - Add sub-issues for discovered work

3. **After Implementation:**
   - Close issue with completion comment including commit hash
   - Link related issues if applicable

**Workflow Pattern:**
```powershell
# 1. Create issue FIRST
gh issue create --repo TheAnsarya/poppy --title "Description" --body "Details" --label "label"

# 2. Add prompt tracking comment (for AI-created issues)
gh issue comment <number> --repo TheAnsarya/poppy --body "Prompt for work:`n{original user prompt}"

# 3. Implement the fix/feature

# 4. Commit with issue reference
git add .
git commit -m "Brief description - #<issue-number>"

# 5. Close issue with summary
gh issue close <number> --repo TheAnsarya/poppy --comment "Completed in <commit-hash>"
```

### ⚠️ MANDATORY: Prompt Tracking for AI-Created Issues

When creating GitHub issues from AI prompts, **IMMEDIATELY** add the original user prompt as the **FIRST comment** right after creating the issue — before doing any implementation work.

## Coding Standards

### Indentation
- **TABS for indentation** — enforced by `.editorconfig`
- **Tab width: 4 spaces** — ALWAYS use 4-space-equivalent tabs
- **Applies to all file types** — C#, TypeScript, JSON, YAML, Markdown, scripts, and config files
- NEVER use spaces for indentation — only tabs
- Inside code blocks in markdown, use spaces for alignment of ASCII art/diagrams

### Brace Style — K&R (One True Brace)
- **Opening braces on the SAME LINE** as the statement — ALWAYS
- This applies to ALL constructs: `if`, `else`, `for`, `while`, `switch`, `try`, `catch`, functions, methods, classes, structs, namespaces, lambdas, properties, enum declarations, etc.
- `else` and `else if` go on the same line as the closing brace: `} else {`
- `catch` goes on the same line as the closing brace: `} catch (...) {`
- **NEVER use Allman style** (brace on its own line)

#### C# Examples

```csharp
// ✅ CORRECT — K&R style
if (condition) {
	DoSomething();
} else if (other) {
	DoOther();
} else {
	DoFallback();
}

public void Execute(int param) {
	// body
}

public class MyClass : Base {
	public void Method() {
		// body
	}
}

// ❌ WRONG — Allman style (DO NOT USE)
if (condition)
{
	DoSomething();
}
```

### Hexadecimal Values
- **Always lowercase**: `0xff00`, not `0xFF00`
- Use `$` as the hex indicator in assembly: `$40df`, `$ff`, `$0a`
- Never use `0x` prefix in assembly code

### Assembly Code
- **Poppy uses `*.pasm` files, NOT `*.asm`** — This is the Poppy Assembly format
- All opcodes/operands in **lowercase** (`lda`, `sta`, `jsr`, NOT `LDA`, `STA`, `JSR`)
- All hex values in **lowercase** with `$` prefix

```asm
; ✅ CORRECT - lowercase
lda #$ff
sta $2000
jsr subroutine
bra .loop

; ❌ WRONG - uppercase (NEVER use)
LDA #$FF
STA $2000
```

### C# Standard
- **.NET 10** with latest C# features
- File-scoped namespaces where applicable
- Nullable reference types enabled
- Modern pattern matching

### Encoding & Line Endings
- **UTF-8** encoding with BOM for all files
- **CRLF** line endings (Windows style)
- Support for Unicode and emojis

### ⚠️ Comment Safety Rule
**When adding or modifying comments, NEVER change the actual code.**

## Testing Guidelines

### ⚠️ MANDATORY: Before/After Testing

**EVERY code change MUST include before/after test runs.** This is non-negotiable.

```powershell
# Run all tests
dotnet test src/Poppy.Tests -c Release --nologo

# Run specific test class
dotnet test src/Poppy.Tests -c Release --filter "ClassName=PansyGeneratorTests"
```

### Verification Checklist (for EVERY code change):
1. ✅ All tests pass (`dotnet test src/Poppy.Tests -c Release`)
2. ✅ Build succeeds (`dotnet build src/Poppy.sln -c Release`)
3. ✅ New tests added for new/changed functionality
4. ✅ No new warnings in build output
5. ✅ Code formatted (tabs, K&R braces, lowercase hex)

## Build Commands

### .NET (Core Library, CLI, Tests)
```powershell
# Build entire solution
dotnet build src/Poppy.sln -c Release

# Run tests
dotnet test src/Poppy.Tests -c Release --nologo

# Run CLI
dotnet run --project src/Poppy.CLI -- <args>
```

### VS Code Extension (TypeScript)
- **ALWAYS use Yarn** — never npm or npx
- Yarn version: 1.22.22
- Commands: `yarn install`, `yarn compile`, `yarn test`, `yarn package`

## Project Structure

```
/                          # Root
├── .github/               # GitHub configuration
├── configs/               # Configuration files
├── docs/                  # User documentation (linked from README)
├── examples/              # Example .pasm files
├── includes/              # Include files for assembly
├── project-base/          # Project templates
├── src/                   # Source code
│   ├── Poppy.Core/        # Core compiler library (.NET 10)
│   │   ├── CodeGen/       # Code generators (PansyGenerator, CDL, etc.)
│   │   ├── Lexer/         # Tokenizer
│   │   ├── Parser/        # AST parser
│   │   ├── Semantics/     # Semantic analysis
│   │   └── Project/       # Project file handling
│   ├── Poppy.CLI/         # Command-line interface
│   ├── Poppy.Tests/       # xUnit test project
│   └── Poppy.sln          # .NET solution file
├── templates/             # Code generation templates
├── vscode-extension/      # VS Code extension (TypeScript/Yarn)
├── ~docs/                 # Project documentation
│   ├── chat-logs/         # AI conversation logs
│   └── session-logs/      # Session summaries
├── ~plans/                # Short/long term plans
├── ~manual-testing/       # Manual test files
└── ~reference-files/      # Reference materials
```

## Documentation

### ⚠️ MANDATORY: Session Logs

**Always create a session log at the end of every conversation that involves code changes, issue creation, or significant research.** This is non-negotiable.

- File: `~docs/session-logs/YYYY-MM-DD-session-NN.md`
- Increment `NN` if a log already exists for that date
- Include: summary of work done, issues created/closed, commits made, files changed, and next steps
- Commit the session log as part of the final commit

### Paths

- All docs reachable from `README.md`
- Session logs: `~docs/session-logs/YYYY-MM-DD-session-NN.md`
- **NEVER edit** `~docs/poppy-manual-prompts-log.txt` (user-maintained)

## Git Workflow

### ⚠️ MANDATORY: Always Include Modified/Untracked Files

Always include unexpected/stray/untracked modified files in commits by default. Do not pause or ask follow-up questions about file selection.

- Stage all modified and untracked files along with task changes
- Commit and push without additional confirmation prompts
- Continue implementation work without stopping on dirty-tree surprises

- Create feature branches: `feature/description`, `fix/description`
- Logical, atomic commits — one concern per commit
- **Always reference issue numbers**: `Brief description - #<issue-number>`
- Use conventional prefixes: `feat:`, `fix:`, `test:`, `docs:`, `perf:`, `refactor:`

## Target Systems

- **Primary:** NES (6502), SNES (65816)
- **Secondary:** Game Boy (SM83), Atari Lynx (65SC02), Atari 2600 (6507)
- **Additional:** Genesis (M68000), GBA (ARM7TDMI), TurboGrafx-16 (HuC6280), WonderSwan (V30MZ)
- Reference compilers: ASAR, XKAS, Ophis, ca65, DASM, RGBDS

## Pansy Integration

Poppy generates Pansy metadata files (`.pansy`) via `--pansy` CLI flag:
- Code/data map (CDL flags: CODE, DATA, JUMP_TARGET, SUB_ENTRY, OPCODE, DRAWN, READ, INDIRECT)
- Symbols (Label, Constant, Enum, Struct, Macro, Local, Anonymous, InterruptVector, Function)
- Memory regions with types (ROM, RAM, VRAM, IO, SRAM, WRAM, OpenBus, Mirror)
- Cross-references (Jsr=1, Jmp=2, Branch=3, Read=4, Write=5)
- Source map (when listing is available)
- Metadata (project name, author, version)

**Spec:** Pansy v1.0, 32-byte header, DEFLATE compression, section table format.
See `src/Poppy.Core/CodeGen/PansyGenerator.cs`.

## Problem-Solving Philosophy

### ⚠️ NEVER GIVE UP on Hard Problems

1. **NEVER declare something "too hard"** and close the issue
2. **Break it down** — Create smaller sub-issues
3. **Research first** — Investigate approaches
4. **Document** — Create analysis in `~plans/`
5. **Incremental progress** — Even partial progress is valuable
6. **Create issues for future work** — Well-documented issues for later

## Related Projects

- **Pansy** — Metadata format for disassembly data
- **Peony** — Disassembler using Pansy format
- **Nexen** — Multi-system emulator (exports Pansy metadata)
- **GameInfo** — ROM hacking toolkit
- **BPS-Patch** — Binary patching system

## ⚠️ Important Notes

1. **Never use spaces for indentation** — TABS ONLY
2. **Never use uppercase hex** — always lowercase (`0xff`, `$ff`)
3. **Never modify** the manual prompts log file
4. **Always** add BOM to UTF-8 files
5. **Always** ensure documentation is linked from README
6. **Always use `.pasm` file extension** — Poppy Assembly files, never `.asm`
7. **Always use Yarn** for JavaScript/TypeScript — never npm/npx
8. **Always** create GitHub issues before starting work
9. **Always** run tests before and after code changes
10. **Always** format code before committing (tabs, K&R, lowercase hex)
11. **Always** tie commits to issues with `#<number>` references

## Markdown Formatting

### ⚠️ MANDATORY: Fix Markdownlint Warnings

**Always fix markdownlint warnings when editing or creating markdown files.** This is non-negotiable.

Key rules to enforce:

- **MD022** — Blank lines above and below headings
- **MD031** — Blank lines around fenced code blocks
- **MD032** — Blank lines around lists (ordered and unordered)
- **MD047** — Files must end with a single newline character
- **MD007** — Disabled (tab indentation is 1 character, not 4)
- **MD010** — Disabled (hard tabs are REQUIRED per our indentation rules)

When generating new markdown content, **always include proper blank line spacing** around headings, lists, and code blocks.

### ⚠️ MANDATORY: Documentation Link-Tree

**Every markdown file in the repository must be reachable from `README.md` through a hierarchical link structure.**

- The main `README.md` must link to all documentation directories and key files
- Subdirectory docs should link back to parent and to sibling docs
- No orphan markdown files — if a `.md` file exists, it must be discoverable from the root README
- When adding new documentation, always update `README.md` with a link to it
- Internal docs (`~docs/`) should have their own index linked from the main README

