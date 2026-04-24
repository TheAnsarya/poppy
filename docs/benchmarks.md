# Benchmarks

Poppy includes a BenchmarkDotNet benchmark suite in `src/Poppy.Benchmarks/`.

## Scope

The benchmark suite covers:

- Full compile pipeline benchmarks across multiple target systems
- Architecture comparison benchmarks (including Channel F/F8)
- Asset/pipeline helper benchmarks
	- Asset-heavy per-target compile scenarios (NES, Atari 2600, SNES, GB, SMS, TG16, Channel F) using a shared JSON+binary asset manifest workload
- ARM7TDMI special-emission micro-benchmarks for:
	- Data-processing instruction snippets
	- Branch/call instruction snippets
	- Load/store instruction snippets
	- Multiply instruction snippets

## Run Benchmarks

Run all benchmarks:

```powershell
dotnet run --project src/Poppy.Benchmarks -c Release
```

Run only ARM special-emission benchmarks:

```powershell
dotnet run --project src/Poppy.Benchmarks -c Release -- --filter "*ArmSpecialEmission*"
```

Run quick dry-job validation for ARM benchmarks:

```powershell
dotnet run --project src/Poppy.Benchmarks -c Release -- --job dry --filter "*ArmSpecialEmission*"
```

Run quick dry-job validation for architecture comparison benchmarks:

```powershell
dotnet run --project src/Poppy.Benchmarks -c Release -- --job dry --filter "*ArchitectureComparison*"
```

Run quick dry-job validation for asset pipeline benchmarks:

```powershell
dotnet run --project src/Poppy.Benchmarks -c Release -- --job dry --filter "*AssetPipeline*"
```

## Output

BenchmarkDotNet writes result artifacts under `BenchmarkDotNet.Artifacts/`.

Use the generated summaries in issue comments when tracking performance deltas for follow-up ARM instruction slices.
