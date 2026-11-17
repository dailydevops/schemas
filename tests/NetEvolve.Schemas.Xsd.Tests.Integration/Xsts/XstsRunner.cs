namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

using NetEvolve.Schemas.Conformance.Results;

/// <summary>Runs the selected cases through a validator and records one result per case.</summary>
internal static class XstsRunner
{
    /// <summary>Runs the selection.</summary>
    /// <param name="selection">The selection; excluded cases become skipped results with their reason.</param>
    /// <param name="validator">The validator seam.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per selected and excluded case.</returns>
    public static async Task<IReadOnlyList<CaseResult>> RunAsync(
        XstsSelection selection,
        IXstsValidator validator,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(validator);
        var results = new List<CaseResult>(selection.Selected.Count + selection.Excluded.Count);
        foreach (var xstsCase in selection.Selected)
        {
            var observed = await (
                xstsCase.Case.Kind == XstsKind.Schema
                    ? validator.CompileSchemaAsync(xstsCase, cancellationToken)
                    : validator.ValidateInstanceAsync(xstsCase, cancellationToken)
            );
            var expected = xstsCase.ExpectValid ? XstsOutcome.Valid : XstsOutcome.Invalid;
            results.Add(
                observed == expected
                    ? new CaseResult(xstsCase.Id, xstsCase.TestSet, CaseOutcome.Pass)
                    : new CaseResult(
                        xstsCase.Id,
                        xstsCase.TestSet,
                        CaseOutcome.Fail,
                        $"Expected {expected} but observed {observed}."
                    )
            );
        }

        foreach (var excluded in selection.Excluded)
        {
            results.Add(new CaseResult(excluded.Id, excluded.TestSet, CaseOutcome.Skipped, excluded.Reason));
        }

        return results;
    }
}
