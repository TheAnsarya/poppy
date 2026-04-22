# Issue 341 Plan - Asset Manifest Pipeline

## Goal

Add assembly-time asset inclusion/conversion support for binary, JSON, and PNG/BMP graphics data using new pasm directive(s), with validation tests and benchmark coverage.

## Scope

- Add `.asset_manifest` directive in code generation.
- Add `.asset` directive for single-entry asset inclusion.
- Add manifest parser support for:
	- `binary`
	- `json-u8`
	- `json-u16le`
	- `chr` (PNG/BMP image to CHR data)
- Extend graphics converter to actually support PNG input (not only BMP).
- Add tests for directives and conversion paths.
- Add benchmark for manifest-driven asset assembly throughput.
- Update syntax documentation.

## Design Notes

- Manifest format: JSON object with `assets` array.
- Paths resolve relative to the manifest location.
- JSON extraction supports root arrays and dotted property paths.
- `chr` conversion uses existing `ImageToChrConverter` tile format options.
- Directive processing emits bytes directly into current segment before ROM builder runs.

## Validation

- Before changes: `dotnet test src/Poppy.sln -c Release --nologo`.
- After changes: same full test command.
- Bench quick run: `dotnet run --project src/Poppy.Benchmarks -c Release -- --filter "*Asset*" --job dry`.

## Deliverables

- Code: core directive handling + PNG support.
- Tests: new directive tests and converter coverage.
- Benchmarks: new benchmark class for asset pipeline stage.
- Docs: syntax updates for directives and manifest schema.
- Session log: required log file for the work session.
