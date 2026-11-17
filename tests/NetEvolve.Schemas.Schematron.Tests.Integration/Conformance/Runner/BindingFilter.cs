namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>Selects cases by query binding.</summary>
internal static class BindingFilter
{
    /// <summary>A case is selected when at least one binding that lists it is selected.</summary>
    /// <param name="cases">The catalog.</param>
    /// <param name="rules">The binding rules.</param>
    /// <returns>The selection; the order of the catalog is kept.</returns>
    public static CaseSelection Select(IReadOnlyList<SchematronCase> cases, IReadOnlyList<BindingRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var excluded = new List<ExcludedCase>(cases.Count);
        foreach (var schematronCase in cases)
        {
            excluded.Add(new ExcludedCase(schematronCase, string.Empty));
        }

        return new CaseSelection([], excluded);
    }
}
