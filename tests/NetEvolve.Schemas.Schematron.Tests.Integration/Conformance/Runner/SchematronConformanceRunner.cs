namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

using NetEvolve.Schemas.Conformance.Results;

/// <summary>Runs the selected cases through a validator and records one result per case.</summary>
internal static class SchematronConformanceRunner
{
    /// <summary>Runs the selection: every selected case once per selected binding, in the test set <c>&lt;binding&gt;.&lt;case folder&gt;</c>.</summary>
    /// <param name="selection">The selection; excluded cases become skipped results with their reason.</param>
    /// <param name="validator">The validator seam.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per selected and excluded case.</returns>
    public static async Task<IReadOnlyList<CaseResult>> RunAsync(
        CaseSelection selection,
        ISchematronValidator validator,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(validator);
        var results = new List<CaseResult>(selection.Selected.Sum(c => c.Bindings.Count) + selection.Excluded.Count);
        foreach (var schematronCase in selection.Selected)
        {
            foreach (var binding in schematronCase.Bindings)
            {
                var testSet = binding + "." + schematronCase.TestSet;
                var observed = await validator.ValidateAsync(schematronCase, binding, cancellationToken);
                results.Add(
                    observed.Outcome == schematronCase.Expect && observed.Reason is null
                        ? new CaseResult(schematronCase.Id, testSet, CaseOutcome.Pass)
                        : new CaseResult(
                            schematronCase.Id,
                            testSet,
                            CaseOutcome.Fail,
                            Describe(schematronCase, observed)
                        )
                );
            }
        }

        foreach (var (schematronCase, reason) in selection.Excluded)
        {
            results.Add(new CaseResult(schematronCase.Id, schematronCase.TestSet, CaseOutcome.Skipped, reason));
        }

        return results;
    }

    private static string Describe(SchematronCase schematronCase, SchematronObservation observed)
    {
        if (observed.Outcome == schematronCase.Expect)
        {
            return observed.Reason!;
        }

        var mismatch = $"Expected {schematronCase.Expect} but observed {observed.Outcome}.";
        return observed.Reason is null ? mismatch : $"{mismatch} {observed.Reason}";
    }
}
