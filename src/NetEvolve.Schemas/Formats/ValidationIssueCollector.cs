namespace NetEvolve.Schemas.Formats;

using System.Collections.Generic;
using NetEvolve.Arguments;

/// <summary>The single implementation of the issue drop policy, so no format handles <c>MaxIssues</c> itself.</summary>
public sealed class ValidationIssueCollector
{
    private readonly List<ValidationIssue> _issues = [];
    private readonly int _maxIssues;
    private bool _isTruncated;
    private bool _hasDroppedErrors;
    private bool _isStopped;

    /// <summary>Initializes a new instance of the <see cref="ValidationIssueCollector"/> class.</summary>
    /// <param name="limits">The limits.</param>
    public ValidationIssueCollector(ValidationLimits limits)
    {
        Argument.ThrowIfNull(limits);

        _maxIssues = limits.MaxIssues;
    }

    /// <summary>Gets the number of issues held, at most <c>MaxIssues + 1</c>.</summary>
    public int Count => _issues.Count;

    /// <summary>Gets a value indicating whether <see cref="Stop"/> was called.</summary>
    public bool IsStopped => _isStopped;

    /// <summary>Adds an issue; dropped when full, ignored after <see cref="Stop"/>.</summary>
    /// <param name="issue">The issue.</param>
    public void Add(ValidationIssue issue)
    {
        Argument.ThrowIfNull(issue);

        if (_isStopped)
        {
            return;
        }

        if (_issues.Count < _maxIssues)
        {
            _issues.Add(issue);
            return;
        }

        _isTruncated = true;
        _hasDroppedErrors |= issue.Severity == IssueSeverity.Error;
    }

    /// <summary>Appends a stop issue of severity error and ignores later issues.</summary>
    /// <param name="stopIssue">The stop issue.</param>
    public void Stop(ValidationIssue stopIssue)
    {
        Argument.ThrowIfNull(stopIssue);

        // Only severity error may stop; the guard throws an ArgumentOutOfRangeException, an ArgumentException.
        Argument.ThrowIfLessThan((int)stopIssue.Severity, (int)IssueSeverity.Error, nameof(stopIssue));

        if (_isStopped)
        {
            return;
        }

        _issues.Add(stopIssue);
        _isTruncated = true;
        _isStopped = true;
    }

    /// <summary>Creates a snapshot of the collected issues.</summary>
    /// <returns>The result.</returns>
    public ValidationResult ToResult() => new(_issues, _isTruncated, _hasDroppedErrors);
}
