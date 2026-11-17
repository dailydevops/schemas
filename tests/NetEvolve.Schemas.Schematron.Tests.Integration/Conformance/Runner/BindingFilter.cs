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
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(rules);
        var selected = new List<SchematronCase>();
        var excluded = new List<ExcludedCase>();
        foreach (var schematronCase in cases)
        {
            var reasons = new List<string>();
            var isSelected = false;
            foreach (var rule in rules)
            {
                if (!schematronCase.Bindings.Contains(rule.Binding, StringComparer.Ordinal))
                {
                    continue;
                }

                isSelected |= rule.IsSelected;
                if (rule.ExclusionReason is { } reason && !reasons.Contains(reason))
                {
                    reasons.Add(reason);
                }
            }

            if (isSelected)
            {
                selected.Add(schematronCase);
            }
            else
            {
                excluded.Add(new ExcludedCase(schematronCase, string.Join(" ", reasons)));
            }
        }

        return new CaseSelection(selected, excluded);
    }
}
