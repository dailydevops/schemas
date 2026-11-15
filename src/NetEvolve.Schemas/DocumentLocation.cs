#pragma warning disable CA1065, MA0025 // Contract stub: the owning work package replaces the throwing bodies.

namespace NetEvolve.Schemas;

using System;

/// <summary>A one-based line and column in a document, zero meaning unknown.</summary>
/// <remarks>Limitation: members throw until FND-09 lands.</remarks>
public readonly struct DocumentLocation : IEquatable<DocumentLocation>
{
    /// <summary>Initializes a new instance of the <see cref="DocumentLocation"/> struct.</summary>
    /// <param name="source">The system identifier of the document, or <see langword="null"/>.</param>
    /// <param name="line">The one-based line, zero when unknown.</param>
    /// <param name="column">The one-based column, zero when unknown.</param>
    public DocumentLocation(string? source, int line, int column) => throw Stub.For("FND-09");

    /// <summary>Gets the system identifier of the document.</summary>
    public string? Source => throw Stub.For("FND-09");

    /// <summary>Gets the one-based line, zero when unknown.</summary>
    public int Line => throw Stub.For("FND-09");

    /// <summary>Gets the one-based column, zero when unknown.</summary>
    public int Column => throw Stub.For("FND-09");

    /// <inheritdoc />
    public bool Equals(DocumentLocation other) => throw Stub.For("FND-09");

    /// <inheritdoc />
    public override bool Equals(object? obj) => throw Stub.For("FND-09");

    /// <inheritdoc />
    public override int GetHashCode() => throw Stub.For("FND-09");

    /// <inheritdoc />
    public override string ToString() => throw Stub.For("FND-09");

    /// <summary>Determines whether two locations are equal.</summary>
    /// <param name="left">The first location.</param>
    /// <param name="right">The second location.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(DocumentLocation left, DocumentLocation right) => throw Stub.For("FND-09");

    /// <summary>Determines whether two locations differ.</summary>
    /// <param name="left">The first location.</param>
    /// <param name="right">The second location.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(DocumentLocation left, DocumentLocation right) => throw Stub.For("FND-09");
}
