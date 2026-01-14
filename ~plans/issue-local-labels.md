## Description
Implement local labels that are scoped to their parent global label.

## Syntax
```asm
global_label:
    @loop:              ; Local label
        dex
        bne @loop       ; Reference local
    
    @done:
        rts

other_global:
    @loop:              ; Different label, same name OK
        dey
        bne @loop
```

## Acceptance Criteria

- [ ] Support @local syntax for local labels
- [ ] Scope local labels to nearest preceding global label
- [ ] Allow same local label name in different scopes
- [ ] Forward reference to local labels within scope
- [ ] Error for out-of-scope local label references
- [ ] Unit tests for local label resolution

## Implementation Notes

- Track current global label scope in parser
- Store local labels with scope prefix internally
- Resolve local references within current scope
- Clear local scope on new global label

## Related
Part of #10 (Label System Epic)
