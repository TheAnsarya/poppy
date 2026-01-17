# CDL/DIZ Workflow Guide

This document describes the roundtrip workflow between **Poppy** (assembler) and **Peony** (disassembler) using CDL (Code/Data Log) and DIZ (DiztinGUIsh) files.

## Overview

```
┌────────────────────────────────────────────────────────────────────┐
│                    CDL/DIZ Roundtrip Workflow                      │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  ┌─────────┐        ┌─────────┐        ┌─────────┐                │
│  │  Poppy  │───────►│  ROM    │───────►│ Emulator│                │
│  │Assembler│        │  File   │        │(Mesen)  │                │
│  └─────────┘        └─────────┘        └────┬────┘                │
│       │                                     │                     │
│       │ --cdl game.cdl                      │ Generate CDL        │
│       │ --diz game.diz                      │ while playing       │
│       ▼                                     ▼                     │
│  ┌─────────┐                          ┌─────────┐                 │
│  │  CDL    │◄─────────────────────────│  CDL    │                 │
│  │  File   │      Same format!        │  File   │                 │
│  └─────────┘                          └─────────┘                 │
│       │                                     │                     │
│       └──────────────┬──────────────────────┘                     │
│                      ▼                                            │
│                 ┌─────────┐                                       │
│                 │  Peony  │                                       │
│                 │Disasm   │                                       │
│                 └─────────┘                                       │
│                      │                                            │
│                      ▼                                            │
│                 ┌─────────┐                                       │
│                 │  .pasm  │ Enhanced disassembly with             │
│                 │  Source │ accurate code/data separation         │
│                 └─────────┘                                       │
│                                                                   │
└────────────────────────────────────────────────────────────────────┘
```

## File Formats

### CDL (Code/Data Log)

CDL files track which ROM bytes have been executed as code or read as data during emulation.

**Supported Formats:**
- **Mesen** - Header "CDL\x01" + byte array (recommended)
- **FCEUX** - Raw byte array, no header

**Flag Values (per byte):**
| Flag | Mesen | FCEUX | Meaning |
|------|-------|-------|---------|
| Code | 0x01 | 0x01 | Byte was executed as an opcode/operand |
| Data | 0x02 | 0x02 | Byte was read as data |
| Jump Target | 0x04 | - | Byte is a branch destination |
| Sub Entry | 0x08 | 0x10 | Byte is a subroutine entry point |

### DIZ (DiztinGUIsh Project)

DIZ files are gzip-compressed JSON containing rich disassembly metadata.

**Contents:**
- Project name and ROM mapping info
- Per-byte data type classification
- Labels with names and comments
- ROM checksum for verification

**Data Types:**
| Value | Name | Description |
|-------|------|-------------|
| 0 | Unreached | Not accessed |
| 1 | Opcode | Instruction start |
| 2 | Operand | Instruction operand bytes |
| 3 | Data8 | 8-bit data |
| 7 | Data16 | 16-bit data |
| 4 | Graphics | Tile/graphics data |
| 5 | Music | Audio data |
| 13 | Text | String data |

## Workflow Examples

### 1. Assemble with CDL/DIZ Output

```bash
# Generate ROM with CDL and DIZ files
poppy game.pasm -o game.nes --cdl game.cdl --diz game.diz

# Specify FCEUX format for CDL
poppy game.pasm -o game.nes --cdl game.cdl --cdl-format fceux

# Full build with all outputs
poppy game.pasm \
  -o game.nes \
  --cdl game.cdl \
  --diz game.diz \
  -s game.sym \
  -l game.lst \
  -m game.map
```

### 2. Enhance CDL with Emulator Trace

```bash
# 1. Build your ROM
poppy game.pasm -o game.nes --cdl initial.cdl

# 2. Play in Mesen, generate comprehensive CDL
#    (Mesen: Debug → CDL → Save CDL File)

# 3. Use emulator CDL for disassembly
peony game.nes -c traced.cdl -o enhanced.pasm
```

### 3. Disassemble with CDL/DIZ Hints

```bash
# Use CDL file for code/data hints
peony game.nes -c game.cdl -o output.pasm

# Use DIZ file for labels and data types
peony game.nes -d game.diz -o output.pasm

# Combine multiple hint sources
peony game.nes -c game.cdl -d game.diz -s labels.sym -o output.pasm
```

### 4. Complete Roundtrip

```bash
# Step 1: Initial assembly
poppy original.pasm -o game.nes --cdl build.cdl --diz build.diz

# Step 2: Test in emulator, generate play CDL
#    (Play game thoroughly to mark code/data)

# Step 3: Merge CDLs and disassemble
peony game.nes -c play.cdl -d build.diz -o disasm.pasm

# Step 4: Verify roundtrip
poppy disasm.pasm -o rebuilt.nes
diff game.nes rebuilt.nes  # Should be identical
```

## API Usage

### Poppy - Generating CDL/DIZ

```csharp
// After code generation
var generator = new CodeGenerator(analyzer, target);
var code = generator.Generate(program);

// Generate CDL file (Mesen format)
var cdlGen = new CdlGenerator(
    analyzer.SymbolTable,
    analyzer.Target,
    generator.Segments);
cdlGen.Export("output.cdl", code.Length, CdlGenerator.CdlFormat.Mesen);

// Generate DIZ file (gzip compressed)
var dizGen = new DizGenerator(
    analyzer.SymbolTable,
    analyzer.Target,
    generator.Segments,
    "MyProject");
dizGen.Export("output.diz", code, compress: true);
```

### Peony - Loading CDL/DIZ

```csharp
// Create symbol loader and load hint files
var symbols = new SymbolLoader();
symbols.Load("trace.cdl");  // CDL from emulator
symbols.Load("project.diz"); // DIZ from previous disassembly

// Create disassembly engine with symbol loader
var engine = new DisassemblyEngine(cpuDecoder, platformAnalyzer);
engine.SetSymbolLoader(symbols);

// Disassemble - automatically uses CDL/DIZ hints
var result = engine.Disassemble(romData, entryPoints);

// Check CDL/DIZ data directly
if (symbols.IsCode(offset) == true) {
    // CDL/DIZ says this is code
}

// Get coverage statistics
var (codeBytes, dataBytes, total, percent) = symbols.CdlData.GetCoverageStats();
Console.WriteLine($"CDL coverage: {percent:F1}%");
```

## Best Practices

### For ROM Hackers

1. **Always generate CDL/DIZ when building** - They capture your knowledge of code vs data
2. **Play test extensively with CDL logging** - Emulators mark executed code accurately
3. **Merge multiple CDL sources** - Combine build CDL with play CDL for best coverage
4. **Use DIZ for complex projects** - Labels and comments survive the roundtrip

### For Disassembly Projects

1. **Start with CDL from playtesting** - Let the emulator identify code paths
2. **Iterate: disassemble → fix → reassemble** - CDL improves each iteration
3. **Export DIZ regularly** - Save your labeling work for future sessions
4. **Verify roundtrip frequently** - Assembled ROM should match original

## Troubleshooting

### CDL Not Detecting Code

- Ensure emulator CDL logging is enabled during play
- Play through all game paths (menus, battles, etc.)
- Check CDL format matches (Mesen vs FCEUX)

### DIZ Labels Not Importing

- Verify DIZ file is valid JSON (try uncompressing with gzip)
- Check address format (decimal in JSON keys)
- Ensure DIZ file matches ROM (checksum)

### Roundtrip Mismatch

- Check for uninitialized data regions
- Verify data vs code classification
- Compare listing files for differences

## Related Documentation

- [Poppy Assembler CLI](../poppy/docs/cli.md)
- [Peony Disassembler CLI](../peony/docs/cli.md)
- [CDL Format Specification](../docs/formats/cdl-format.md)
- [DIZ Format Specification](../docs/formats/diz-format.md)
