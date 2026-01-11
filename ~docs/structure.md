# Project Structure - project-base

**Last Updated:** December 25, 2025

---

## Directory Structure

```
project-base/
├── .editorconfig           # Code formatting and style configuration
├── .gitignore             # Git ignore patterns
├── LICENSE                # Project license (TBD)
├── README.md              # Project overview and usage (TBD)
├── CONTRIBUTING.md        # Contribution guidelines (TBD)
│
├── ~docs/                 # Meta-documentation (project creation)
│   ├── manual-prompts-log.txt      # User-maintained prompt history
│   ├── roadmap.md                  # Project roadmap and milestones
│   ├── structure.md                # This file - project structure
│   ├── chat-logs/                  # AI conversation logs
│   │   └── YYYY-MM-DD-chat-NN.md
│   └── session-logs/               # Work session summaries
│       └── YYYY-MM-DD-session-NN.md
│
├── docs/                  # Project-specific documentation
│   └── (project documentation files)
│
└── .github/               # GitHub-specific files (TBD)
	├── ISSUE_TEMPLATE/
	│   ├── bug_report.md
	│   ├── feature_request.md
	│   └── task.md
	├── PULL_REQUEST_TEMPLATE.md
	└── workflows/         # GitHub Actions (if needed)
```

---

## File Purposes

### Root Level

#### `.editorconfig`
Defines code formatting rules for all file types:
- Enforces tabs over spaces
- Sets UTF-8 encoding and CRLF line endings
- Configures brace style (K&R)
- Language-specific rules for C#, JavaScript, Python, etc.

#### `.gitignore`
Excludes unwanted files from version control:
- Build artifacts and binaries
- IDE-specific files
- Temporary files
- User-specific settings
- Package management files

#### `LICENSE`
Specifies the project's licensing terms (to be determined based on project use).

#### `README.md`
Main project documentation including:
- Project description and purpose
- Setup instructions
- Usage examples
- Contributing information
- Links to additional documentation

#### `CONTRIBUTING.md`
Guidelines for contributing to the project:
- Code style requirements
- Branch naming conventions
- Commit message format
- Pull request process
- Code review guidelines

---

### ~docs/ Directory
**Purpose:** Documentation about the creation and maintenance of the project itself.

#### `manual-prompts-log.txt`
User-maintained log of significant prompts given to the AI. AI should never edit this file.

#### `roadmap.md`
Project roadmap with milestones, phases, and success criteria.

#### `structure.md`
This file - documents the project structure and file organization.

#### `chat-logs/`
Contains markdown files with detailed AI conversation logs:
- Naming: `YYYY-MM-DD-chat-NN.md` (e.g., `2025-12-25-chat-01.md`)
- Documents what was discussed and decided
- Includes actions taken during conversations

#### `session-logs/`
Contains markdown files with work session summaries:
- Naming: `YYYY-MM-DD-session-NN.md` (e.g., `2025-12-25-session-01.md`)
- Summarizes objectives, completed tasks, and decisions
- Lists pending tasks and next steps

---

### docs/ Directory
**Purpose:** Project-specific documentation.

Content depends on project type but may include:
- API documentation
- Architecture diagrams
- Design decisions
- User guides
- Development setup
- Deployment instructions

---

### .github/ Directory
**Purpose:** GitHub-specific configurations and templates.

#### `ISSUE_TEMPLATE/`
Templates for different types of issues:
- `bug_report.md` - For reporting bugs
- `feature_request.md` - For requesting new features
- `task.md` - For general tasks and improvements

#### `PULL_REQUEST_TEMPLATE.md`
Template for pull request descriptions ensuring consistent PR quality.

#### `workflows/`
GitHub Actions workflows for automation (if needed):
- CI/CD pipelines
- Automated testing
- Code quality checks
- Release automation

---

## File Naming Conventions

### General Rules
- Use lowercase for file extensions
- Use kebab-case for multi-word files (e.g., `session-logs`)
- Use PascalCase for C# files
- Use camelCase for JavaScript/TypeScript files

### Markdown Files
- Use `.md` extension
- PascalCase for main docs (e.g., `README.md`, `CONTRIBUTING.md`)
- lowercase-with-hyphens for supplementary docs

### Log Files
- **Chat logs:** `YYYY-MM-DD-chat-NN.md`
- **Session logs:** `YYYY-MM-DD-session-NN.md`
- NN is a zero-padded 2-digit sequence number

---

## Folder Naming Conventions

- `~docs/` - Tilde prefix indicates meta/hidden documentation
- `docs/` - Standard documentation folder
- `.github/` - Dot prefix for GitHub configuration
- Lowercase with hyphens for multi-word folders

---

## Essential vs. Optional Files

### Essential (Must Have)
- ✅ `.editorconfig`
- ✅ `.gitignore`
- ✅ `README.md`
- ✅ `~docs/` structure
- ⬜ `LICENSE`

### Important (Should Have)
- ⬜ `CONTRIBUTING.md`
- ⬜ Issue templates
- ⬜ PR template
- ⬜ Documentation in `docs/`

### Optional (Nice to Have)
- ⬜ GitHub Actions workflows
- ⬜ Additional automation
- ⬜ Advanced templates

---

## Growth Strategy

As the project evolves:
1. Keep essential files current
2. Add project-specific content to `docs/`
3. Expand issue templates as needs arise
4. Add automation only when it provides clear value
5. Document new patterns in appropriate places

---

## Maintenance Guidelines

### When Adding Files
- Consider if truly necessary
- Place in appropriate directory
- Follow naming conventions
- Update this structure document if adding new categories

### When Removing Files
- Ensure no dependencies
- Update documentation
- Consider archiving instead of deleting

### Regular Reviews
- Quarterly review of structure
- Remove obsolete files
- Update documentation
- Consolidate redundant content

---
