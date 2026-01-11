## Description
Implement anonymous forward (+) and backward (-) labels for quick short branches.

## Syntax
```asm
    ldx #10
-                       ; Backward target
    dex
    bne -               ; Branch to previous -

    lda #0
    beq +               ; Branch to next +
    nop
+                       ; Forward target

; Multiple levels
--                      ; Far backward
    nop
-                       ; Near backward  
    dex
    bne -               ; Go to near -
    beq --              ; Go to far --
```

## Acceptance Criteria
- [ ] + label creates forward reference point
- [ ] - label creates backward reference point
- [ ] Multiple + or - for farther references (++, ---)
- [ ] Works with all branch instructions
- [ ] Proper address calculation
- [ ] Unit tests for anonymous labels

## Implementation Notes
- Track anonymous label stacks during parsing
- + pushes to forward stack, resolves later
- - references most recent backward label
- Count repeated symbols for distance

## Related
Part of #10 (Label System Epic)
