# Assembler Completeness Audit - 2026-04-22

## Scope

Evaluated current coverage for:

- NES (MOS6502)
- Game Boy (SM83)
- Atari 2600 (MOS6507)

Focus areas:

- Header/directive coverage
- ROM generation integration
- Bankswitching and layout correctness
- Existing benchmark coverage

## NES Status

Coverage observed:

- Integration coverage in `src/Poppy.Arch.MOS6502.Tests/Integration/NesRomGenerationTests.cs`:
	- iNES header emission
	- raw binary path without iNES directives
	- complete ROM with vectors
	- mapper/mirroring/battery combinations
- Directive coverage in `src/Poppy.Arch.MOS6502.Tests/Semantics/INesDirectiveTests.cs`:
	- `ines_prg`, `ines_chr`, `ines_mapper`, `ines_submapper`
	- mirroring, battery, four-screen, PAL
	- error handling when used outside NES target

Assessment:

- Core NES assembler and iNES directive stack appear complete for mainstream workflows.

## Game Boy Status

Coverage observed:

- Directive semantics coverage in `src/Poppy.Arch.SM83.Tests/Semantics/GbDirectiveTests.cs`.
- Integration coverage in `src/Poppy.Arch.SM83.Tests/Integration/GbRomGenerationTests.cs` including header placement checks.

Gap noted from integration remarks:

- Test file comments still mention pending layout integration, but assertions validate practical ROM/header output.

Assessment:

- Game Boy assembler support is functionally mature for header + ROM generation paths, with opportunity to refresh stale test commentary.

## Atari 2600 Status

Coverage observed:

- Integration coverage in `src/Poppy.Arch.MOS6502.Tests/Integration/Atari2600RomGenerationTests.cs`:
	- 4K ROM generation
	- reset vector correctness
	- startup sequence encoding
	- target aliases
- Bankswitching coverage in `src/Poppy.Arch.MOS6502.Tests/CodeGen/Atari2600BankswitchingTests.cs`:
	- F8/F6/F4/FE/E0/E7/3F validation
	- size constraints and fill semantics

Assessment:

- Atari 2600 assembler path has strong ROM builder and bankswitching validation coverage.

## New Work Added In Issue 341

- `.asset` directive for inline asset inclusion/conversion.
- `.asset_manifest` directive for manifest-driven multi-asset reinsertion.
- Asset types introduced:
	- `binary`
	- `json-u8`
	- `json-u16le`
	- `chr` (PNG/BMP to CHR bytes)
- Added benchmark coverage for manifest-driven compile stage.

## Follow-up Recommendations

- Add fixture-based roundtrip integration for asset manifests in example projects (NES/GB/2600).
- Add dedicated benchmark cases for PNG CHR conversion and JSON parsing separately.
- Add schema validation tooling for asset manifest files in CLI preflight.
