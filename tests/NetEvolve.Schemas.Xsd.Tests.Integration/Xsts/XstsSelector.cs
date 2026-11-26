namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>
/// Applies the version and status filter. A test applies to an XSD version when every enclosing <c>version</c>
/// attribute (test set, group, test) lists it; tokens of one attribute are alternatives. A token that is not an XSD
/// version is a processor feature: it gates or splits tests this runner cannot decide, so those are excluded.
/// </summary>
internal static class XstsSelector
{
    /// <summary>Selects the tests of one XSD version.</summary>
    /// <param name="cases">The tests as read from the suite.</param>
    /// <param name="filter">The version and the accepted statuses.</param>
    /// <returns>The selected and the excluded tests; tests of other versions are in neither list.</returns>
    public static XstsSelection Select(IEnumerable<XstsCase> cases, XstsFilter filter)
    {
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(filter);
        var selected = new List<XstsSelectedCase>();
        var excluded = new List<XstsExcludedCase>();
        foreach (var xstsCase in cases)
        {
            if (!TryAdmit(xstsCase, filter.Version, out var feature))
            {
                continue;
            }

            var id = $"{filter.Version}/{xstsCase.Group}/{xstsCase.Name}";
            var reason = xstsCase.ExclusionReason ?? FeatureReason(feature);
            if (reason is null && (xstsCase.Status is null || !filter.Statuses.Contains(xstsCase.Status)))
            {
                if (xstsCase.Status is null)
                {
                    reason =
                        filter.StatusLessTestSets?.ContainsKey(xstsCase.TestSet) == true
                            ? null
                            : "The case has no current status.";
                }
                else
                {
                    reason = $"The status '{xstsCase.Status}' is not accepted.";
                }
            }

            var validity = reason is null ? ResolveValidity(xstsCase, filter.Version, out reason) : null;
            if (reason is null && validity is not ("valid" or "invalid"))
            {
                reason = $"The expected validity '{validity}' is not a definite outcome.";
            }

            if (reason is null)
            {
                selected.Add(
                    new XstsSelectedCase(
                        id,
                        filter.Version,
                        string.Equals(validity, "valid", StringComparison.Ordinal),
                        xstsCase
                    )
                );
            }
            else
            {
                excluded.Add(new XstsExcludedCase(id, xstsCase.TestSet, reason));
            }
        }

        selected.Sort(
            (a, b) =>
            {
                var order = string.CompareOrdinal(a.TestSet, b.TestSet);
                return order != 0 ? order : string.CompareOrdinal(a.Id, b.Id);
            }
        );
        excluded.Sort(
            (a, b) =>
            {
                var order = string.CompareOrdinal(a.TestSet, b.TestSet);
                return order != 0 ? order : string.CompareOrdinal(a.Id, b.Id);
            }
        );
        return new XstsSelection(selected, excluded);
    }

    private static bool TryAdmit(XstsCase xstsCase, string version, out string? feature)
    {
        feature = null;
        foreach (var scope in xstsCase.VersionScopes)
        {
            if (scope.Contains(version, StringComparer.Ordinal))
            {
                continue;
            }

            var hasVersionToken = false;
            foreach (var token in scope)
            {
                hasVersionToken |= XstsVersions.IsVersion(token);
            }

            if (hasVersionToken)
            {
                return false;
            }

            feature ??= scope[0];
        }

        return true;
    }

    private static string? FeatureReason(string? feature) =>
        feature is null
            ? null
            : $"The case is gated by the processor feature '{feature}', which this runner does not model.";

    private static string? ResolveValidity(XstsCase xstsCase, string version, out string? reason)
    {
        string? specific = null;
        string? fallback = null;
        string? feature = null;
        foreach (var expected in xstsCase.Expectations)
        {
            if (expected.Tokens.Count == 0)
            {
                fallback ??= expected.Validity;
            }
            else if (expected.Tokens.Count == 1 && string.Equals(expected.Tokens[0], version, StringComparison.Ordinal))
            {
                specific ??= expected.Validity;
            }
            else if (!XstsVersions.IsVersion(expected.Tokens[0]))
            {
                feature ??= expected.Tokens[0];
            }
        }

        var validity = specific ?? fallback;
        reason =
            validity is not null ? null
            : feature is not null
                ? $"The expected result depends on the processor feature '{feature}', which this runner does not model."
            : $"The case has no expected result for XSD {version}.";
        return validity;
    }
}
