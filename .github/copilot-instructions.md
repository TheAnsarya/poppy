# GitHub Copilot Instructions for Poppy Compiler

## 🎯 Project Overview

**Poppy Compiler** is a multi-system assembly compiler targeting retro gaming platforms (NES, SNES, GB). The project aims to compile projects like DW1, FFMQ, DW4, and DQ3r.

## 📋 Coding Standards

### Indentation & Whitespace
- **ALWAYS use TABS for indentation** - Never spaces, in any file type
- Tab width: 4 spaces (8 for assembly files)
- Remove trailing whitespace from all lines
- Include a blank line at the end of every file

### Brace Style
- **K&R style** - Opening braces on the SAME line as the statement
- Example:
  ```csharp
  if (condition) {
      // code
  } else {
      // code
  }
  ```

### Hexadecimal Values
- **Always lowercase** for all hex values
- Use `$` as the hex indicator (e.g., `$40df`, `$ff`, `$0a`)
- Never use `0x` prefix unless required by the language

### Assembly Code
- **Poppy uses `*.pasm` files, NOT `*.asm`** - This is the Poppy Assembly format
- All opcodes/operands in **lowercase** (e.g., `lda`, `sta`, `jsr`, `inc`, `tya`)
- All hex values in **lowercase** with `$` prefix
- Example:
  ```asm
  lda #$40
  sta $2000
  jsr subroutine
  ```

### Encoding & Line Endings
- **UTF-8** encoding with BOM for all files
- **CRLF** line endings (Windows style)
- Support for Unicode and emojis

## 📁 Project Structure

```
/                     # Root
├── .github/          # GitHub configuration
├── docs/             # User documentation (linked from README)
├── src/              # Source code
├── ~docs/            # Project creation documentation
│   ├── chat-logs/    # AI conversation logs
│   └── session-logs/ # Session summaries
├── ~plans/           # Short/long term plans
├── ~manual-testing/  # Manual test files
└── ~reference-files/ # Reference materials
```

## 🛠️ Build Tools

### Package Manager
- **ALWAYS use Yarn** for JavaScript/TypeScript projects (vscode-extension)
- Never use npm or npx commands
- Yarn version: 1.22.22
- Common commands: `yarn install`, `yarn compile`, `yarn test`, `yarn package`

## 📝 Documentation Requirements

### Code Comments
- Comment ALL code thoroughly
- Document function parameters and return values
- Explain complex logic and algorithms
- Include examples where helpful

### Documentation Files
- All docs should be reachable from `README.md`
- Use emojis and formatting for readability
- Keep markdown files in `/docs/` or inline with code

### Log Files
- Chat logs: `~docs/chat-logs/YYYY-MM-DD-chat-NN.md`
- Session logs: `~docs/session-logs/YYYY-MM-DD-session-NN.md`
- **NEVER edit** `~docs/poppy-manual-prompts-log.txt` (user-maintained)

## 🔀 Git Workflow

### Branching
- Create feature branches for significant work
- Branch naming: `feature/description`, `fix/description`
- Merge back to `main` when complete

### Commits
- Logical, atomic commits
- Always reference GitHub issues in commit messages
- Format: `Brief description (#issue-number)`

### Issues
- Create GitHub issues for all planned work
- Use Kanban board for project management
- Link all commits to relevant issues

## 🎮 Target Systems

- **Primary:** NES (6502), SNES (65816)
- **Secondary:** Game Boy (Z80-like)
- Reference compilers: ASAR, XKAS, Ophis, ca65

## ⚠️ Important Notes

1. **Never use spaces for indentation** - TABS ONLY
2. **Never use uppercase hex** - always lowercase
3. **Never modify** the manual prompts log file
4. **Always** add BOM to UTF-8 files
5. **Always** ensure documentation is linked from README
6. **Always use Yarn** for JavaScript/TypeScript - never npm/npx
7. **Always use `.pasm` file extension** - Poppy Assembly files, never `.asm`

