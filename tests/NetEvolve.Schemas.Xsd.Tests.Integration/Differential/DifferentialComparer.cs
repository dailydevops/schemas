namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>Compares what this library and the baseline observed for one case.</summary>
/// <remarks>
/// Limitation: issue positions are compared only when the schema and instance verdicts both agree, because the issues of
/// two engines that disagree on validity do not correspond. Messages are never compared.
/// </remarks>
internal static class DifferentialComparer
{
    /// <summary>Lists the differences between the two observations of a case.</summary>
    /// <param name="differentialCase">The case.</param>
    /// <param name="ours">What this library observed.</param>
    /// <param name="baseline">What System.Xml.Schema observed.</param>
    /// <returns>The differences; empty when the observations agree.</returns>
    public static IReadOnlyList<Difference> Compare(
        DifferentialCase differentialCase,
        EngineResult ours,
        EngineResult baseline
    )
    {
        ArgumentNullException.ThrowIfNull(differentialCase);
        ArgumentNullException.ThrowIfNull(ours);
        ArgumentNullException.ThrowIfNull(baseline);
        var differences = new List<Difference>();
        if (ours.Schema != baseline.Schema)
        {
            differences.Add(
                new Difference(
                    differentialCase.Id,
                    DifferenceKind.SchemaVerdict,
                    $"schema verdict: ours={ours.Schema}, baseline={baseline.Schema}"
                )
            );
        }

        if (ours.Instance != baseline.Instance)
        {
            differences.Add(
                new Difference(
                    differentialCase.Id,
                    DifferenceKind.InstanceVerdict,
                    $"instance verdict: ours={ours.Instance}, baseline={baseline.Instance}"
                )
            );
        }

        if (differences.Count == 0)
        {
            var ourPositions = Format(ours.Issues);
            var baselinePositions = Format(baseline.Issues);
            if (!string.Equals(ourPositions, baselinePositions, StringComparison.Ordinal))
            {
                differences.Add(
                    new Difference(
                        differentialCase.Id,
                        DifferenceKind.IssuePositions,
                        $"issue positions: ours=[{ourPositions}], baseline=[{baselinePositions}]"
                    )
                );
            }
        }

        return differences;
    }

    private static string Format(IReadOnlyList<IssuePosition> issues)
    {
        var sorted = new IssuePosition[issues.Count];
        for (var i = 0; i < sorted.Length; i++)
        {
            sorted[i] = issues[i];
        }

        Array.Sort(sorted, static (a, b) => a.Line != b.Line ? a.Line.CompareTo(b.Line) : a.Column.CompareTo(b.Column));
        return string.Join(", ", sorted.Select(static p => p.ToString()));
    }
}
