namespace NetEvolve.Schemas.Benchmarks.Gate;

/// <summary>Compares results with recorded targets and the <c>System.Xml</c> baseline.</summary>
internal static class BenchmarkGate
{
    /// <summary>Allowed slowdown against the recorded mean when no baseline exists.</summary>
    public const double AllowedRegression = 0.05;

    /// <summary>
    /// Factor applied to a recorded <c>baselineRatio</c> before it fails: the measured ratio may be up to 15 % above
    /// the target, which absorbs the noise of shared CI runners. Allocations stay at zero tolerance.
    /// </summary>
    public const double RatioTolerance = 1.15;

    public static IReadOnlyList<GateViolation> Evaluate(
        IReadOnlyList<BenchmarkResult> results,
        IReadOnlyList<BenchmarkTarget> targets
    )
    {
        var byName = new Dictionary<string, BenchmarkTarget>(StringComparer.Ordinal);
        foreach (var t in targets)
        {
            byName[t.Name] = t;
        }

        var baselines = new Dictionary<string, BenchmarkResult>(StringComparer.Ordinal);
        foreach (var r in results)
        {
            if (r.IsBaseline)
            {
                baselines[r.Group] = r;
            }
        }

        var violations = new List<GateViolation>();
        foreach (var r in results)
        {
            if (r.IsBaseline)
            {
                continue; // The baseline is the reference, not a gated benchmark.
            }

            if (!byName.TryGetValue(r.Name, out var t))
            {
                violations.Add(new(r.Name, GateViolationKind.MissingTarget, "no recorded target"));
                continue;
            }

            Check(r, t, baselines, violations);
        }
        return violations;
    }

    private static void Check(
        BenchmarkResult r,
        BenchmarkTarget t,
        Dictionary<string, BenchmarkResult> baselines,
        List<GateViolation> violations
    )
    {
        if (r.AllocatedBytes > t.AllocatedBytes)
        {
            violations.Add(
                new(
                    r.Name,
                    GateViolationKind.AllocationGrowth,
                    $"allocated {r.AllocatedBytes} B > target {t.AllocatedBytes} B"
                )
            );
        }

        if (t.BaselineRatio is { } targetRatio)
        {
            if (!baselines.TryGetValue(r.Group, out var baseline))
            {
                violations.Add(
                    new(r.Name, GateViolationKind.MissingBaseline, "target has a baseline ratio but no baseline result")
                );
            }
            else
            {
                var ratio = r.MeanNs / baseline.MeanNs;
                var limit = targetRatio * RatioTolerance;
                if (ratio > limit)
                {
                    violations.Add(
                        new(
                            r.Name,
                            GateViolationKind.WorseBaselineRatio,
                            FormattableString.Invariant(
                                $"ratio {ratio:F3} > target {targetRatio:F3} (limit {limit:F3})"
                            )
                        )
                    );
                }
            }
        }
        else if (r.MeanNs > t.MeanNs * (1 + AllowedRegression))
        {
            violations.Add(
                new(r.Name, GateViolationKind.Regression, $"mean {r.MeanNs:F1} ns > target {t.MeanNs:F1} ns + 5 %")
            );
        }
    }

    /// <summary>Gates one report against all <c>*.json</c> files of a targets directory. Returns 0 on success, 1 on violations.</summary>
    public static int Run(string resultsPath, string targetsDirectory, TextWriter output)
    {
        var results = Path.GetFullPath(resultsPath);
        var targets = new List<BenchmarkTarget>();
        foreach (var file in Directory.GetFiles(targetsDirectory, "*.json"))
        {
            if (!string.Equals(Path.GetFullPath(file), results, StringComparison.OrdinalIgnoreCase))
            {
                targets.AddRange(TargetReader.Read(File.ReadAllText(file)));
            }
        }

        var violations = Evaluate(ResultReader.Read(File.ReadAllText(results)), targets);
        foreach (var v in violations)
        {
            output.WriteLine($"{v.Kind}: {v.Name}: {v.Message}");
        }
        return violations.Count == 0 ? 0 : 1;
    }
}
