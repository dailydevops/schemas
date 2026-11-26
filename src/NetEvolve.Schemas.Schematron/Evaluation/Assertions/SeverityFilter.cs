namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;

/// <summary>
/// The allow-list of <c>severity</c> values of a validator (#384). A finding whose severity is not listed is dropped; a finding without
/// a severity is always kept, because it carries nothing to compare.
/// </summary>
internal sealed class SeverityFilter
{
    private readonly string[] _allowed;
    private readonly StringComparison _comparison;

    internal SeverityFilter(IReadOnlyList<string> severities, StringComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(severities);
        ArgumentException.ThrowIfNullOrEmpty(severities);

        var allowed = new string[severities.Count];
        for (var i = 0; i < allowed.Length; i++)
        {
            var severity = severities[i];
            ArgumentNullException.ThrowIfNullOrEmpty(severity);
            allowed[i] = severity;
        }

        _allowed = allowed;
        _comparison = comparison;
    }

    /// <summary>Tells whether a finding with this severity is kept.</summary>
    /// <param name="severity">The resolved severity of the finding; empty when the assertion has none.</param>
    /// <returns><see langword="true"/> when the finding is kept.</returns>
    internal bool Allows(ReadOnlySpan<char> severity)
    {
        if (severity.IsEmpty)
        {
            return true;
        }

        foreach (var allowed in _allowed)
        {
            if (severity.Equals(allowed.AsSpan(), _comparison))
            {
                return true;
            }
        }

        return false;
    }
}
