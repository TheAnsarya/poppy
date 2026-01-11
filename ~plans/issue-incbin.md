## Description
Implement the .incbin directive to include binary files directly into the output.

## Syntax
```asm
.incbin "data.bin"                    ; Include entire file
.incbin "data.bin", $100              ; Skip first $100 bytes
.incbin "data.bin", $100, $200        ; Skip $100, read $200 bytes
```

## Acceptance Criteria
- [ ] Parse .incbin "filename" syntax
- [ ] Support optional offset parameter
- [ ] Support optional length parameter
- [ ] Error handling for missing files
- [ ] Error handling for invalid offset/length
- [ ] Unit tests for binary inclusion

## Implementation Notes
- Add IncbinDirective AST node
- Read binary file during code generation
- Validate file exists and parameters are valid
- Insert raw bytes into output

## Related
Part of #9 (Include System Epic)
