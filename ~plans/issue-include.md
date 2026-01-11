## Description
Implement the .include directive to include other assembly files.

## Syntax
```asm
.include "filename.pasm"
.include "path/to/file.inc"
```

## Acceptance Criteria
- [ ] Parse .include "filename" syntax
- [ ] Recursively process included files
- [ ] Track include paths for error reporting
- [ ] Prevent circular includes with detection
- [ ] Support relative and absolute paths
- [ ] Add -I flag for include search paths
- [ ] Unit tests for include resolution

## Implementation Notes
- Modify Lexer to handle include directive
- Add IncludeDirective AST node
- Process includes during parsing phase
- Track file stack for error messages

## Related
Part of #9 (Include System Epic)
