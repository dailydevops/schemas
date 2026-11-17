namespace NetEvolve.Schemas.Conformance.Results;

using NetEvolve.Schemas.Conformance.Baseline;

/// <summary>The comparison of a conformance run against the known-failure baselines of its test sets.</summary>
public sealed class ConformanceReport
{
    private ConformanceReport(IReadOnlyList<TestSetReport> testSets, TestSetCounts counts, bool isSuccess)
    {
        TestSets = testSets;
        Counts = counts;
        IsSuccess = isSuccess;
    }

    /// <summary>Gets the per-test-set reports, sorted by test set name.</summary>
    public IReadOnlyList<TestSetReport> TestSets { get; }

    /// <summary>Gets the case counts of the whole suite.</summary>
    public TestSetCounts Counts { get; }

    /// <summary>Gets a value indicating whether no test set has a regression or an unexpected pass.</summary>
    public bool IsSuccess { get; }

    /// <summary>Compares the results of a run against the baselines.</summary>
    /// <param name="results">The case results of the run.</param>
    /// <param name="baselines">The baselines by test set name; a test set without an entry has an empty baseline.</param>
    /// <returns>The report.</returns>
    public static ConformanceReport Compare(
        IEnumerable<CaseResult> results,
        IReadOnlyDictionary<string, KnownFailureBaseline> baselines
    )
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(baselines);
        var sets = new Dictionary<string, Accumulator>(StringComparer.Ordinal);
        foreach (var result in results)
        {
            if (!sets.TryGetValue(result.TestSet, out var set))
            {
                set = new Accumulator(
                    baselines.TryGetValue(result.TestSet, out var baseline) ? baseline : KnownFailureBaseline.Empty
                );
                sets.Add(result.TestSet, set);
            }

            set.Add(result);
        }

        var names = new List<string>(sets.Keys);
        names.Sort(StringComparer.Ordinal);

        var reports = new List<TestSetReport>(names.Count);
        int passed = 0,
            failed = 0,
            skipped = 0;
        var isSuccess = true;
        foreach (var name in names)
        {
            var report = sets[name].ToReport(name);
            reports.Add(report);
            passed += report.Counts.Passed;
            failed += report.Counts.Failed;
            skipped += report.Counts.Skipped;
            isSuccess &= report.Regressions.Count == 0 && report.UnexpectedPasses.Count == 0;
        }

        return new ConformanceReport(reports, new TestSetCounts(passed, failed, skipped), isSuccess);
    }

    private sealed class Accumulator(KnownFailureBaseline baseline)
    {
        private readonly List<string> _regressions = [];
        private readonly List<string> _unexpectedPasses = [];
        private int _passed;
        private int _failed;
        private int _skipped;

        public void Add(CaseResult result)
        {
            switch (result.Outcome)
            {
                case CaseOutcome.Pass:
                    _passed++;
                    if (baseline.Contains(result.CaseId))
                    {
                        _unexpectedPasses.Add(result.CaseId);
                    }

                    break;
                case CaseOutcome.Fail:
                    _failed++;
                    if (!baseline.Contains(result.CaseId))
                    {
                        _regressions.Add(result.CaseId);
                    }

                    break;
                default:
                    _skipped++;
                    break;
            }
        }

        public TestSetReport ToReport(string testSet)
        {
            _regressions.Sort(StringComparer.Ordinal);
            _unexpectedPasses.Sort(StringComparer.Ordinal);
            return new TestSetReport(
                testSet,
                new TestSetCounts(_passed, _failed, _skipped),
                _regressions,
                _unexpectedPasses
            );
        }
    }
}
