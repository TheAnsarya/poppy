## Description
Generate debug symbol files compatible with popular NES/SNES emulators and debuggers.

## Formats to Support

### FCEUX .nl format
```
$8000#reset#Reset vector entry point
$8010#nmi#NMI handler
$0000#temp#Temporary variable
```

### Mesen .mlb format
```
PRG:$8000:reset
PRG:$8010:nmi
RAM:$0000:temp
```

### Generic .sym format
```
00:8000 reset
00:8010 nmi
```

## Acceptance Criteria

- [ ] Output FCEUX .nl format
- [ ] Output Mesen .mlb format  
- [ ] Output generic .sym format
- [ ] Include all labels and constants
- [ ] Support -s/--symbols flag
- [ ] Auto-detect format from extension
- [ ] Unit tests for symbol output

## Implementation Notes

- Collect all symbols after assembly
- Format based on output extension
- Include RAM vs ROM classification for .mlb
- Add bank info for banked ROMs

## Related
Part of #13 (Output Formats Epic)
