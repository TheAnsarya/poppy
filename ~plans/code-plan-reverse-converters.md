# Code Plan: Reverse Converters (PASM → ASAR/ca65/xkas)

## Issue: #143

## Overview

Add reverse conversion capability: PASM → ASAR, ca65, xkas. This mirrors the existing
forward converters (ASAR/ca65/xkas → PASM) using the same infrastructure.

## Architecture

### New Interface: `IPasmExporter`

Separate interface from `IProjectConverter` since the direction is reversed:
- Input: PASM source files
- Output: Target assembler format

```csharp
public interface IPasmExporter {
    string TargetAssembler { get; }
    IReadOnlyList<string> OutputExtensions { get; }
    ConversionResult ExportFile(string sourceFile, ConversionOptions options);
    ProjectConversionResult ExportProject(
        string sourceDirectory, string outputDirectory, ConversionOptions options);
}
```

### New Base Class: `BaseExporter`

Mirror of `BaseConverter` but in reverse direction:
- Reads `.pasm` files
- Converts line-by-line to target format
- Handles directive mapping (PASM → target)
- Preserves comments and formatting

### Reverse Directive Mappings

Invert the existing DirectiveMapping tables:
- PASM `db` → ASAR `db`, ca65 `.byte`, xkas `db`
- PASM `dw` → ASAR `dw`, ca65 `.word`, xkas `dw`
- PASM `include` → ASAR `incsrc`, ca65 `.include`, xkas `incsrc`
- PASM `fill` → ASAR `fill`, ca65 `.res`, xkas (manual)
- PASM local labels `.label` → ASAR `.label`, ca65 `@label`, xkas `.label`

### Implementation Order

1. **Phase 1**: IPasmExporter interface + BaseExporter
2. **Phase 2**: PasmToAsarExporter (simplest — closest syntax to PASM)
3. **Phase 3**: PasmToCa65Exporter (most different syntax)
4. **Phase 4**: PasmToXkasExporter
5. **Phase 5**: CLI `export` command
6. **Phase 6**: Tests for all exporters

## Key Differences from Forward Converters

| Feature | Forward (→ PASM) | Reverse (PASM →) |
|---------|------------------|-------------------|
| Labels | Convert to `.` prefix | Convert from `.` prefix |
| Hex | Convert to `$` prefix | May need `0x` or other |
| Macros | Convert `%name()` | Convert to target macro syntax |
| Directives | Map to PASM names | Map from PASM names |
| Comments | Preserve `;` | May need to convert to `//` |
| File ext | `.asm` → `.pasm` | `.pasm` → `.asm`/`.s` |

## PASM → ASAR Key Mappings

- `db` → `db`
- `dw` → `dw`
- `dl` → `dl`
- `dd` → `dd`
- `include "X"` → `incsrc "X"`
- `incbin "X"` → `incbin "X"`
- `org $XXXX` → `org $XXXX`
- `.localLabel` → `.localLabel`
- `arch 65816` → (already implied in ASAR)
- `fill N, $XX` → `fill N : db $XX` or `fillbyte $XX : fill N`

## PASM → ca65 Key Mappings

- `db` → `.byte`
- `dw` → `.word`
- `dd` → `.dword`
- `include "X"` → `.include "X"`
- `incbin "X"` → `.incbin "X"`
- `org $XXXX` → `.org $XXXX`
- `.localLabel` → `@localLabel`
- `arch 6502` → `.p02`
- `arch 65816` → `.p816`
- `fill N, $XX` → `.res N, $XX`
- `; comment` → `; comment` (same)

## PASM → xkas Key Mappings

- `db` → `db`
- `dw` → `dw`
- `dl` → `dl`
- `dd` → `dd`
- `include "X"` → `incsrc "X"`
- `incbin "X"` → `incbin "X"`
- `org $XXXX` → `org $XXXX`
- `arch 65816` → `arch 65816.wdc`
- `; comment` → `// comment`
