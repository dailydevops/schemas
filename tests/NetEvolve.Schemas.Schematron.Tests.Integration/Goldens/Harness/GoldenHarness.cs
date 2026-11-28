namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

using NetEvolve.Schemas.Conformance.Results;

/// <summary>Compares the rules a validator fired per document with the expected rules.</summary>
/// <remarks>
/// One <see cref="CaseResult"/> is recorded per expectation: the case id is the document, the test set is the rule
/// set. <c>ConformanceReport.Compare</c> then yields the pass rate per rule set. The rules are compared as sets of
/// ids, because a rule fires once per matching context node.
/// </remarks>
internal static class GoldenHarness
{
    /// <summary>Validates every expectation of every rule set and records one result per expectation.</summary>
    /// <param name="corpus">The pinned corpus the documents are fetched from.</param>
    /// <param name="ruleSets">The rule sets with their validators and expectations.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The results in rule set and expectation order.</returns>
    /// <exception cref="GoldenSourceException">A source is not backed by a pinned suite; nothing is validated then.</exception>
    public static async Task<IReadOnlyList<CaseResult>> RunAsync(
        GoldenCorpus corpus,
        IReadOnlyList<GoldenRuleSet> ruleSets,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(corpus);
        ArgumentNullException.ThrowIfNull(ruleSets);
        var count = 0;
        foreach (var ruleSet in ruleSets)
        {
            ArgumentNullException.ThrowIfNull(ruleSet);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(ruleSet.Name);
            ArgumentNullException.ThrowIfNull(ruleSet.Validator);
            ArgumentNullException.ThrowIfNull(ruleSet.Expectations);
            foreach (var expectation in ruleSet.Expectations)
            {
                ArgumentNullException.ThrowIfNull(expectation);
                ArgumentNullException.ThrowIfNull(expectation.Source);
                ArgumentNullException.ThrowIfNull(expectation.ExpectedRuleIds);
                foreach (var id in expectation.ExpectedRuleIds)
                {
                    ArgumentNullException.ThrowIfNullOrWhiteSpace(id);
                }

                corpus.EnsurePinned(expectation.Source);
                count++;
            }
        }

        var results = new List<CaseResult>(count);
        foreach (var ruleSet in ruleSets)
        {
            foreach (var expectation in ruleSet.Expectations)
            {
                var document = await corpus.ResolveAsync(expectation.Source, cancellationToken);
                var observed = await ruleSet.Validator.ValidateAsync(document, cancellationToken);
                ArgumentNullException.ThrowIfNull(observed);
                ArgumentNullException.ThrowIfNull(observed.FiredRuleIds);
                var reason = Compare(expectation, observed);
                results.Add(
                    reason is null
                        ? new CaseResult(document.Id, ruleSet.Name, CaseOutcome.Pass)
                        : new CaseResult(document.Id, ruleSet.Name, CaseOutcome.Fail, reason)
                );
            }
        }

        return results;
    }

    private static string? Compare(GoldenExpectation expectation, GoldenObservation observed)
    {
        var expected = new HashSet<string>(expectation.ExpectedRuleIds, StringComparer.Ordinal);
        var fired = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in observed.FiredRuleIds)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                _ = fired.Add(id);
            }
        }

        var missing = new List<string>();
        foreach (var id in expected)
        {
            if (!fired.Contains(id))
            {
                missing.Add(id);
            }
        }

        var unexpected = new List<string>();
        foreach (var id in fired)
        {
            if (!expected.Contains(id))
            {
                unexpected.Add(id);
            }
        }

        if (observed.IsValid == expectation.ExpectedValid && missing.Count == 0 && unexpected.Count == 0)
        {
            return null;
        }

        missing.Sort(StringComparer.Ordinal);
        unexpected.Sort(StringComparer.Ordinal);
        var parts = new List<string>(missing.Count + unexpected.Count + 1);
        if (observed.IsValid != expectation.ExpectedValid)
        {
            parts.Add(
                $"Expected {(expectation.ExpectedValid ? "valid" : "invalid")} but observed {(observed.IsValid ? "valid" : "invalid")}."
            );
        }

        foreach (var id in missing)
        {
            parts.Add($"Missing expected rule '{id}'.");
        }

        foreach (var id in unexpected)
        {
            parts.Add($"Unexpected fired rule '{id}'.");
        }

        return string.Join(" ", parts);
    }
}
