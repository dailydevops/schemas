namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using System;
using System.Collections.Generic;

/// <summary>Checks the coverage matrix against the error tables and the tests that exist.</summary>
internal static class ConstraintMatrixChecker
{
    private const string XmlConfPrefix = "xmlconf:";

    /// <summary>Checks the matrix.</summary>
    /// <param name="entries">The entries of the matrix.</param>
    /// <param name="codes">The rows of the error tables.</param>
    /// <param name="testExists">Tells whether a unit test reference exists.</param>
    /// <param name="xmlConfCaseExists">Tells whether a case of the conformance suite exists.</param>
    /// <returns>Every broken rule; empty when the matrix is complete.</returns>
    internal static IReadOnlyList<ConstraintViolation> Check(
        IReadOnlyList<ConstraintEntry> entries,
        IReadOnlyList<ConstraintCode> codes,
        Func<string, bool> testExists,
        Func<string, bool> xmlConfCaseExists
    )
    {
        var violations = new List<ConstraintViolation>();
        var codeByAlias = new Dictionary<string, ConstraintCode>(StringComparer.Ordinal);
        foreach (var code in codes)
        {
            codeByAlias[code.Alias] = code;
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        var mappedCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (!keys.Add(entry.Key))
            {
                violations.Add(
                    new(ConstraintViolationKind.DuplicateEntry, entry.Key, "The constraint has two entries.")
                );
            }

            _ = mappedCodes.Add(entry.Code);
            CheckCode(entry, codeByAlias, violations);
            CheckEvidence(entry, testExists, xmlConfCaseExists, violations);
        }

        foreach (var code in codes)
        {
            if (code.Kind is { } kind && !mappedCodes.Contains(code.Alias))
            {
                violations.Add(
                    new(
                        ConstraintViolationKind.MissingEntry,
                        code.Alias,
                        "No entry maps the " + kind + " constraint '" + code.Name + "' of the error table."
                    )
                );
            }
        }

        return violations;
    }

    private static void CheckCode(
        ConstraintEntry entry,
        Dictionary<string, ConstraintCode> codeByAlias,
        List<ConstraintViolation> violations
    )
    {
        if (!codeByAlias.TryGetValue(entry.Code, out var code))
        {
            violations.Add(
                new(
                    ConstraintViolationKind.UnknownCode,
                    entry.Key,
                    "The code '" + entry.Code + "' is in no error table."
                )
            );
            return;
        }

        // A code of a production (XML0011) or of an encoding names no constraint, so the table has nothing to compare.
        if (
            code.Kind is { } kind
            && (
                kind != entry.Kind
                || !string.Equals(code.Name, entry.Name, StringComparison.Ordinal)
                || (code.Section.Length > 0 && !string.Equals(code.Section, entry.Section, StringComparison.Ordinal))
            )
        )
        {
            violations.Add(
                new(
                    ConstraintViolationKind.Mismatch,
                    entry.Key,
                    "The entry disagrees with the error table about "
                        + code.Alias
                        + ": "
                        + kind
                        + " '"
                        + code.Name
                        + "' in section '"
                        + code.Section
                        + "'."
                )
            );
        }
    }

    private static void CheckEvidence(
        ConstraintEntry entry,
        Func<string, bool> testExists,
        Func<string, bool> xmlConfCaseExists,
        List<ConstraintViolation> violations
    )
    {
        var positive = CheckReferences(entry, entry.Positive, testExists, xmlConfCaseExists, violations);
        var negative = CheckReferences(entry, entry.Negative, testExists, xmlConfCaseExists, violations);
        if (entry.Gap is { } gap)
        {
            CheckGap(entry, gap, testExists, violations);
            return;
        }

        if (positive == 0)
        {
            violations.Add(
                new(ConstraintViolationKind.MissingPositive, entry.Key, "No positive test or case is mapped.")
            );
        }

        if (negative == 0)
        {
            violations.Add(
                new(ConstraintViolationKind.MissingNegative, entry.Key, "No negative test or case is mapped.")
            );
        }
    }

    // Returns the number of references that are not blank; a reference that does not exist is reported on its own.
    private static int CheckReferences(
        ConstraintEntry entry,
        IReadOnlyList<string> references,
        Func<string, bool> testExists,
        Func<string, bool> xmlConfCaseExists,
        List<ConstraintViolation> violations
    )
    {
        var count = 0;
        foreach (var reference in references)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                violations.Add(new(ConstraintViolationKind.InvalidReference, entry.Key, "A reference is empty."));
                continue;
            }

            count++;
            if (reference.StartsWith(XmlConfPrefix, StringComparison.Ordinal))
            {
                var id = reference[XmlConfPrefix.Length..];
                if (string.IsNullOrWhiteSpace(id))
                {
                    violations.Add(new(ConstraintViolationKind.InvalidReference, entry.Key, "The case id is empty."));
                }
                else if (!xmlConfCaseExists(id))
                {
                    violations.Add(
                        new(
                            ConstraintViolationKind.UnknownXmlConfCase,
                            entry.Key,
                            "The xmlconf case '" + id + "' does not exist."
                        )
                    );
                }
            }
            else if (!testExists(reference))
            {
                violations.Add(
                    new(ConstraintViolationKind.UnknownTest, entry.Key, "The test '" + reference + "' does not exist.")
                );
            }
        }

        return count;
    }

    private static void CheckGap(
        ConstraintEntry entry,
        ConstraintGap gap,
        Func<string, bool> testExists,
        List<ConstraintViolation> violations
    )
    {
        if (gap.Issues.Count == 0)
        {
            violations.Add(new(ConstraintViolationKind.InvalidGap, entry.Key, "The gap names no issue."));
        }

        foreach (var issue in gap.Issues)
        {
            if (!IsIssueReference(issue))
            {
                violations.Add(
                    new(
                        ConstraintViolationKind.InvalidGap,
                        entry.Key,
                        "The gap names no issue ('" + issue + "'); an issue reference is '#' and its number."
                    )
                );
            }
        }

        if (gap.Probes.Count == 0)
        {
            violations.Add(new(ConstraintViolationKind.InvalidGap, entry.Key, "The gap names no probe."));
        }

        foreach (var probe in gap.Probes)
        {
            if (string.IsNullOrWhiteSpace(probe) || !testExists(probe))
            {
                violations.Add(
                    new(
                        ConstraintViolationKind.InvalidGap,
                        entry.Key,
                        "The probe '" + probe + "' of the gap does not exist."
                    )
                );
            }
        }
    }

    private static bool IsIssueReference(string issue)
    {
        if (issue.Length < 2 || issue[0] != '#')
        {
            return false;
        }

        for (var i = 1; i < issue.Length; i++)
        {
            if (issue[i] is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }
}
