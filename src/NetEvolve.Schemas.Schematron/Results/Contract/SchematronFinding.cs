#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas.Schematron for the public result types (design #137 section 2)

namespace NetEvolve.Schemas.Schematron;

using System;

/// <summary>A failed assert or a successful report. Text members are valid only during the callback.</summary>
public readonly ref struct SchematronFinding
{
    internal SchematronFinding(
        bool isReport,
        ReadOnlySpan<char> id,
        ReadOnlySpan<char> ruleId,
        ReadOnlySpan<char> constraint,
        ReadOnlySpan<char> test,
        ReadOnlySpan<char> location,
        ReadOnlySpan<char> role,
        ReadOnlySpan<char> flags,
        ReadOnlySpan<char> severity,
        ReadOnlySpan<char> message,
        SchematronReferenceList diagnostics,
        SchematronReferenceList properties,
        ValidationIssue? issue
    )
    {
        IsReport = isReport;
        Id = id;
        RuleId = ruleId;
        Constraint = constraint;
        Test = test;
        Location = location;
        Role = role;
        Flags = flags;
        Severity = severity;
        Message = message;
        Diagnostics = diagnostics;
        Properties = properties;
        Issue = issue;
    }

    /// <summary>Gets a value indicating whether the finding is a successful report; <see langword="false"/> is a failed assert.</summary>
    public bool IsReport { get; }

    /// <summary>Gets the author id, with the <c>#i{N}</c> suffix on expanded copies; empty if none.</summary>
    public ReadOnlySpan<char> Id { get; }

    /// <summary>Gets the rule id from the <c>id</c> attribute; empty if none.</summary>
    public ReadOnlySpan<char> RuleId { get; }

    /// <summary>Gets the id that went into <see cref="ValidationIssue.Constraint"/>.</summary>
    public ReadOnlySpan<char> Constraint { get; }

    /// <summary>Gets the raw test expression.</summary>
    public ReadOnlySpan<char> Test { get; }

    /// <summary>Gets the SVRL location path; empty when no sink asked for it.</summary>
    public ReadOnlySpan<char> Location { get; }

    /// <summary>Gets the role from the assertion, else the rule, else the pattern.</summary>
    public ReadOnlySpan<char> Role { get; }

    /// <summary>Gets the flag tokens joined with one space.</summary>
    public ReadOnlySpan<char> Flags { get; }

    /// <summary>Gets the raw <c>severity</c> token, unchanged.</summary>
    public ReadOnlySpan<char> Severity { get; }

    /// <summary>Gets the rendered message.</summary>
    public ReadOnlySpan<char> Message { get; }

    /// <summary>Gets the referenced diagnostics.</summary>
    public SchematronReferenceList Diagnostics { get; }

    /// <summary>Gets the referenced properties.</summary>
    public SchematronReferenceList Properties { get; }

    /// <summary>Gets the mapped issue; <see langword="null"/> when <c>MaxIssues</c> dropped it.</summary>
    public ValidationIssue? Issue { get; }
}
