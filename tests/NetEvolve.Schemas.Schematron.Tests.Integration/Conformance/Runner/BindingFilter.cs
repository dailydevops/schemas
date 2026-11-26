namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>Selects cases by query binding.</summary>
internal static class BindingFilter
{
    /// <summary>
    /// A case is selected when at least one binding that lists it is selected; a selected case keeps only its selected bindings,
    /// the bindings it runs under.
    /// </summary>
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
            var selectedBindings = new List<string>();
            foreach (var rule in rules)
            {
                if (!schematronCase.Bindings.Contains(rule.Binding, StringComparer.Ordinal))
                {
                    continue;
                }

                if (rule.IsSelected)
                {
                    selectedBindings.Add(rule.Binding);
                }

                if (rule.ExclusionReason is { } reason && !reasons.Contains(reason))
                {
                    reasons.Add(reason);
                }
            }

            if (selectedBindings.Count > 0)
            {
                selected.Add(schematronCase with { Bindings = selectedBindings });
            }
            else
            {
                excluded.Add(new ExcludedCase(schematronCase, string.Join(" ", reasons)));
            }
        }

        return new CaseSelection(selected, excluded);
    }
}
