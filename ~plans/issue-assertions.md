## Description
Implement compile-time assertion directives for validation.

## Syntax
```asm
.assert * < $c000, "Code too large for bank!"
.assert BUFFER_SIZE >= 256, "Buffer too small"

.error "This code path should not be reached"
.warning "Deprecated: use new_routine instead"

.ifdef DEBUG
    .warning "Debug build - not for release"
.endif
```

## Acceptance Criteria
- [ ] .assert condition, "message" - fail if false
- [ ] .error "message" - unconditional error
- [ ] .warning "message" - unconditional warning
- [ ] Support expressions in assert conditions
- [ ] Show source location in messages
- [ ] Unit tests for assertion directives

## Implementation Notes
- Evaluate assertions during semantic analysis
- Add AssertDirective, ErrorDirective, WarningDirective AST nodes
- Collect warnings, fail on errors
- Support forward references in assertions (second pass)

## Related
Part of #11 (Directive System Epic)
