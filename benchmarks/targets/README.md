# Benchmark targets

Recorded targets for the benchmark gate. The harness lives in `benchmarks/NetEvolve.Schemas.Benchmarks`.

## Layout

- Benchmark classes: `benchmarks/NetEvolve.Schemas.Benchmarks/<Pkg>/<Area>/`.
- Targets: one `benchmarks/targets/<name>.json` per benchmark package.

## Target format

A JSON array with one entry per benchmark (excluding the baseline method):

```json
[
  { "name": "Type.Method", "meanNs": 100.0, "allocatedBytes": 0, "baselineRatio": 0.5 }
]
```

- `name`: `Type.Method`, with `(Parameters)` appended for parameterized benchmarks.
- `meanNs`, `allocatedBytes`: recorded mean and allocation per operation.
- `baselineRatio`: optional, recorded mean divided by the mean of the `System.Xml` baseline of the same class.

## System.Xml baseline

Mark the baseline method `[Benchmark(Baseline = true)]` and name it `<Method>SystemXml` (see `SystemXmlBaseline`).

## Gate

`dotnet run -c Release -f net10.0 --project benchmarks/NetEvolve.Schemas.Benchmarks -- --filter "<filter>"`
writes the full JSON report (memory diagnoser enabled); `... -- gate <report.json> [targetsDirectory]` then fails when

- a benchmark has no target (an error, never skipped),
- allocation per operation exceeds the target (no tolerance),
- the ratio to the baseline is more than 15 % worse than `baselineRatio` (`BenchmarkGate.RatioTolerance` = 1.15, absorbs shared-runner noise; the message shows ratio, target and limit), or
- without a `baselineRatio`, the mean is more than 5 % above `meanNs`.

## Summary

`... -- summary <report.json|resultsDirectory> [targetsDirectory]` prints a markdown table with one row per benchmark of the report (a directory means all `*-report-full.json` files in it); the workflow writes it to the job summary. Columns: Benchmark, Mean (ns), Target mean, delta %, Allocated (B), Target allocated, delta %, Ratio to System.Xml, Target ratio, Status. Target columns show `-` when there is no target.

Status uses the rules of `gate`: `violation` (allocation above target, ratio above `baselineRatio` * 1.15, or without a `baselineRatio` the mean more than 5 % above target), `within tolerance` (ratio above the target but within 1.15 times it), `ok`, `no target`, `baseline` (the `System.Xml` method) or `n/a` (no measurement, e.g. a failed run). The command always exits 0; `gate` decides pass or fail. Pipe characters in names are escaped.

On `net11.0` BenchmarkDotNet 0.15.8 cannot drive its own out-of-process toolchain (the runtime moniker is unknown), so `Program.cs` runs in-process there (limitation; remove when BenchmarkDotNet knows net11.0).

The sample benchmark (`*Sample*`) has no target; do not gate its report.

## Exemption

The core (`NetEvolve.Schemas`) and `NetEvolve.Schemas.DependencyInjection` packages have no hot path and no benchmarks of their own: input dispatch is covered by the FND-56 allocation test and resolve cost by the DI-50 singleton test.
