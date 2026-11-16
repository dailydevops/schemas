#pragma warning disable CA1065, MA0025, CA1032, RCS1194 // Stub per #103: throwing bodies stay until FND-09, FND-10, FND-55 and FND-56 replace them; the exception keeps the approved constructor set (CA1032, RCS1194).

namespace NetEvolve.Schemas;

using System;
using System.Collections.Generic;

/// <summary>Thrown when a schema cannot be compiled.</summary>
/// <remarks>Limitation: members throw until FND-09 lands.</remarks>
public sealed class SchemaCompilationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="SchemaCompilationException"/> class.</summary>
    /// <param name="issues">The compilation issues.</param>
    public SchemaCompilationException(IReadOnlyList<ValidationIssue> issues) => throw Stub.For("FND-09");

    /// <summary>Initializes a new instance of the <see cref="SchemaCompilationException"/> class.</summary>
    /// <param name="issues">The compilation issues.</param>
    /// <param name="innerException">The cause, or <see langword="null"/>.</param>
    public SchemaCompilationException(IReadOnlyList<ValidationIssue> issues, Exception? innerException) =>
        throw Stub.For("FND-09");

    /// <summary>Gets the compilation issues.</summary>
    public IReadOnlyList<ValidationIssue> Issues => throw Stub.For("FND-09");
}
