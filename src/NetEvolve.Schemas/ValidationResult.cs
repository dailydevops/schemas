#pragma warning disable CA1065, MA0025 // Contract stub: the owning work package replaces the throwing bodies.

namespace NetEvolve.Schemas;

using System.Collections.Generic;

/// <summary>The outcome of a validation. Immutable and thread-safe.</summary>
/// <remarks>Limitation: members throw until FND-09 lands.</remarks>
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
    ) => throw Stub.For("FND-09");

    /// <summary>Gets the shared result without issues.</summary>
    public static ValidationResult Valid => throw Stub.For("FND-09");

    /// <summary>Gets a value indicating whether there is no error and no dropped error.</summary>
    public bool IsValid => throw Stub.For("FND-09");

    /// <summary>Gets the issues.</summary>
    public IReadOnlyList<ValidationIssue> Issues => throw Stub.For("FND-09");

    /// <summary>Gets a value indicating whether the issue list is not exhaustive.</summary>
    public bool IsTruncated => throw Stub.For("FND-09");

    /// <summary>Gets a value indicating whether at least one dropped issue had severity error.</summary>
    public bool HasDroppedErrors => throw Stub.For("FND-09");
}
