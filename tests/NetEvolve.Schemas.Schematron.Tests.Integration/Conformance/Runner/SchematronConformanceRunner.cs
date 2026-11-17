namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

using NetEvolve.Schemas.Conformance.Results;

/// <summary>Runs the selected cases through a validator and records one result per case.</summary>
internal static class SchematronConformanceRunner
{
    /// <summary>Runs the selection.</summary>
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
        var results = new List<CaseResult>(selection.Selected.Count + selection.Excluded.Count);
        foreach (var schematronCase in selection.Selected)
        {
            var observed = await validator.ValidateAsync(schematronCase, cancellationToken);
            results.Add(
                observed == schematronCase.Expect
                    ? new CaseResult(schematronCase.Id, schematronCase.TestSet, CaseOutcome.Pass)
                    : new CaseResult(
                        schematronCase.Id,
                        schematronCase.TestSet,
                        CaseOutcome.Fail,
                        $"Expected {schematronCase.Expect} but observed {observed}."
                    )
            );
        }

        foreach (var (schematronCase, reason) in selection.Excluded)
        {
            results.Add(new CaseResult(schematronCase.Id, schematronCase.TestSet, CaseOutcome.Skipped, reason));
        }

        return results;
    }
}
