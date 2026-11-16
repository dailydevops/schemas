#pragma warning disable CA1065, MA0025 // Stub per #103: throwing bodies stay until FND-09, FND-10, FND-55 and FND-56 replace them.

namespace NetEvolve.Schemas.Formats;

/// <summary>The single implementation of the issue drop policy, so no format handles <c>MaxIssues</c> itself.</summary>
/// <remarks>Limitation: members throw until FND-09 lands.</remarks>
public sealed class ValidationIssueCollector
{
    /// <summary>Initializes a new instance of the <see cref="ValidationIssueCollector"/> class.</summary>
    /// <param name="limits">The limits.</param>
    public ValidationIssueCollector(ValidationLimits limits) => throw Stub.For("FND-09");

    /// <summary>Gets the number of issues held, at most <c>MaxIssues + 1</c>.</summary>
    public int Count => throw Stub.For("FND-09");

    /// <summary>Gets a value indicating whether <see cref="Stop"/> was called.</summary>
    public bool IsStopped => throw Stub.For("FND-09");

    /// <summary>Adds an issue; dropped when full, ignored after <see cref="Stop"/>.</summary>
    /// <param name="issue">The issue.</param>
    public void Add(ValidationIssue issue) => throw Stub.For("FND-09");

    /// <summary>Appends a stop issue of severity error and ignores later issues.</summary>
    /// <param name="stopIssue">The stop issue.</param>
    public void Stop(ValidationIssue stopIssue) => throw Stub.For("FND-09");

    /// <summary>Creates a snapshot of the collected issues.</summary>
    /// <returns>The result.</returns>
    public ValidationResult ToResult() => throw Stub.For("FND-09");
}
