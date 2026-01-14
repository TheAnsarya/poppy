## Description
Implement directives to configure the target system from within source files.

## Syntax
```asm
.target nes           ; or .nes
.target snes          ; or .snes  
.target gb            ; or .gb

; SNES memory mapping
.lorom
.hirom
.exhirom

; NES mapper
.mapper 0             ; NROM
.mapper 1             ; MMC1
```

## Acceptance Criteria

- [ ] .target directive sets processor architecture
- [ ] .nes, .snes, .gb shortcuts
- [ ] .lorom, .hirom, .exhirom for SNES
- [ ] .mapper N for NES mapper selection
- [ ] Auto-configure defaults based on target
- [ ] Error if target set multiple times
- [ ] Unit tests for target directives

## Implementation Notes

- Add TargetDirective AST node
- Store target in compiler context
- Validate target before code generation
- Default to 6502 if not specified

## Related
Part of #11 (Directive System Epic)
