## Description
Implement alignment and padding directives for memory layout control.

## Syntax
```asm
.align 256            ; Align to 256-byte boundary
.align $100           ; Same as above

.pad $c000            ; Pad with zeros until $c000
.pad $c000, $ff       ; Pad with $ff until $c000

.fill 16              ; 16 zero bytes  
.fill 16, $ea         ; 16 bytes of $ea (NOP)
```

## Acceptance Criteria
- [ ] .align N - align to N-byte boundary with zeros
- [ ] .pad address - pad to specific address
- [ ] .pad address, value - pad with specific byte
- [ ] .fill count - fill with zeros (already exists, verify)
- [ ] .fill count, value - fill with value (already exists, verify)
- [ ] Error if padding would go backward
- [ ] Unit tests for alignment

## Implementation Notes
- Calculate padding needed for alignment
- Verify current address < target for .pad
- Use existing fill infrastructure
- Track in listing output

## Related
Part of #11 (Directive System Epic)
