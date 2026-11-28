namespace NetEvolve.Schemas;

using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>The outcome of a validation. Immutable and thread-safe.</summary>
public sealed class ValidationResult
{
    /// <summary>Initializes a new instance of the <see cref="ValidationResult"/> class.</summary>
    /// <param name="issues">The issues.</param>
    /// <param name="isTruncated">Whether the issue list is not exhaustive.</param>
    /// <param name="hasDroppedErrors">Whether a dropped issue had severity error.</param>
    public ValidationResult(
        IReadOnlyList<ValidationIssue> issues,
        bool isTruncated = false,
        bool hasDroppedErrors = false
    )
    {
        // Dropped errors require a truncated result; the guard throws an ArgumentOutOfRangeException, an ArgumentException.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            hasDroppedErrors ? 1 : 0,
            isTruncated ? 1 : 0,
            nameof(hasDroppedErrors)
        );

        var copy = CopyOf(issues);
        Issues = copy;
        IsTruncated = isTruncated;
        HasDroppedErrors = hasDroppedErrors;
        IsValid = !hasDroppedErrors && !ContainsError(copy);
    }

    /// <summary>Gets the shared result without issues.</summary>
    public static ValidationResult Valid { get; } = new([]);

    /// <summary>Gets a value indicating whether there is no error and no dropped error.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the issues.</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; }

    /// <summary>Gets a value indicating whether the issue list is not exhaustive.</summary>
    public bool IsTruncated { get; }

    /// <summary>Gets a value indicating whether at least one dropped issue had severity error.</summary>
    public bool HasDroppedErrors { get; }

    /// <summary>Copies the list into an immutable snapshot; shared with <see cref="SchemaCompilationException"/>.</summary>
    internal static ReadOnlyCollection<ValidationIssue> CopyOf(IReadOnlyList<ValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        var copy = new ValidationIssue[issues.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            var issue = issues[i];
            ArgumentNullException.ThrowIfNull(issue);
            copy[i] = issue;
        }

        return new ReadOnlyCollection<ValidationIssue>(copy);
    }

    private static bool ContainsError(ReadOnlyCollection<ValidationIssue> issues)
    {
        foreach (var issue in issues)
        {
            if (issue.Severity == IssueSeverity.Error)
            {
                return true;
            }
        }

        return false;
    }
}
