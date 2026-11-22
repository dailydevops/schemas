#pragma warning disable CA1032, RCS1194 // The exception keeps the approved constructor set of #103 (issues, optional inner exception); no parameterless or message-only constructor.

namespace NetEvolve.Schemas;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using NetEvolve.Arguments;

/// <summary>Thrown when a schema cannot be compiled.</summary>
public sealed class SchemaCompilationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="SchemaCompilationException"/> class.</summary>
    /// <param name="issues">The compilation issues.</param>
    public SchemaCompilationException(IReadOnlyList<ValidationIssue> issues)
        : this(issues, null) { }

    /// <summary>Initializes a new instance of the <see cref="SchemaCompilationException"/> class.</summary>
    /// <param name="issues">The compilation issues.</param>
    /// <param name="innerException">The cause, or <see langword="null"/>.</param>
    public SchemaCompilationException(IReadOnlyList<ValidationIssue> issues, Exception? innerException)
        : this(NonEmptyCopyOf(issues), innerException) { }

    private SchemaCompilationException(ReadOnlyCollection<ValidationIssue> issues, Exception? innerException)
        : base(
            string.Format(
                CultureInfo.InvariantCulture,
                "Schema compilation failed with {0} issue(s). First: {1}",
                issues.Count,
                issues[0]
            ),
            innerException
        ) => Issues = issues;

    /// <summary>Gets the compilation issues.</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; }

    private static ReadOnlyCollection<ValidationIssue> NonEmptyCopyOf(IReadOnlyList<ValidationIssue> issues)
    {
        var copy = ValidationResult.CopyOf(issues);
        ArgumentOutOfRangeException.ThrowIfLessThan(copy.Count, 1, nameof(issues));
        return copy;
    }
}
