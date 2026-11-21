namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>Runs cases through this library and the baseline and checks every difference against the deviation list.</summary>
/// <remarks>
/// Limitation: a deviation entry is stale only when its case ran; an entry of a case outside the run is left alone, so a
/// run of a chunk of the suite does not fail on the entries of the other chunks.
/// </remarks>
internal static class DifferentialRunner
{
    /// <summary>Runs the cases.</summary>
    /// <param name="cases">The cases.</param>
    /// <param name="ours">This library.</param>
    /// <param name="baseline">System.Xml.Schema; it runs only on the leg that <see cref="DifferentialLeg"/> names.</param>
    /// <param name="deviations">The accepted differences.</param>
    /// <param name="environment">The leg and runtime of the run.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>
    /// On the baseline leg: one failure per unlisted difference and per stale entry. On every other leg: one failure per
    /// verdict of this library that differs from the recorded expectation.
    /// </returns>
    public static async Task<DifferentialReport> RunAsync(
        IReadOnlyList<DifferentialCase> cases,
        IDifferentialEngine ours,
        IDifferentialEngine baseline,
        DeviationList deviations,
        DifferentialEnvironment environment,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(ours);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(deviations);
        ArgumentNullException.ThrowIfNull(environment);
        var failures = new List<string>();
        var runBaseline = DifferentialLeg.RunsBaseline(environment.TargetFramework);
        if (
            runBaseline
            && !string.Equals(deviations.BaselineRuntime, environment.RuntimeVersion, StringComparison.Ordinal)
        )
        {
            failures.Add(
                $"The deviation list records baseline runtime {deviations.BaselineRuntime}, but this leg runs {environment.RuntimeVersion}; compare again and update the list."
            );
        }

        var ran = new HashSet<string>(StringComparer.Ordinal);
        var matched = new HashSet<(string, DifferenceKind)>();
        foreach (var differentialCase in cases)
        {
            var observed = await ours.RunAsync(differentialCase, cancellationToken);
            if (!runBaseline)
            {
                CheckExpectation(differentialCase, observed, failures);
                continue;
            }

            var reference = await baseline.RunAsync(differentialCase, cancellationToken);
            _ = ran.Add(differentialCase.Id);
            foreach (var difference in DifferentialComparer.Compare(differentialCase, observed, reference))
            {
                var entry = deviations.Find(difference.CaseId, difference.Kind);
                if (entry is not null && string.Equals(entry.Observed, difference.Observed, StringComparison.Ordinal))
                {
                    _ = matched.Add((difference.CaseId, difference.Kind));
                }
                else
                {
                    failures.Add(
                        $"Unlisted difference in case {difference.CaseId} ({difference.Kind}): {difference.Observed}"
                    );
                }
            }
        }

        if (runBaseline)
        {
            foreach (var entry in deviations.Entries)
            {
                if (ran.Contains(entry.CaseId) && !matched.Contains((entry.CaseId, entry.Kind)))
                {
                    failures.Add($"Stale deviation entry for case {entry.CaseId} ({entry.Kind}): {entry.Observed}");
                }
            }
        }

        return new DifferentialReport(runBaseline, failures);
    }

    private static void CheckExpectation(
        DifferentialCase differentialCase,
        EngineResult observed,
        List<string> failures
    )
    {
        if (observed.Schema != differentialCase.ExpectedSchema)
        {
            failures.Add(
                $"Case {differentialCase.Id}: schema verdict {observed.Schema}, recorded expectation {differentialCase.ExpectedSchema}."
            );
        }

        if (
            differentialCase.ExpectedInstance != Verdict.NotRun
            && observed.Instance != differentialCase.ExpectedInstance
        )
        {
            failures.Add(
                $"Case {differentialCase.Id}: instance verdict {observed.Instance}, recorded expectation {differentialCase.ExpectedInstance}."
            );
        }
    }
}
