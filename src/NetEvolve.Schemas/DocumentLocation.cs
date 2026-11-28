namespace NetEvolve.Schemas;

using System;
using System.Globalization;

/// <summary>A one-based line and column in a document, zero meaning unknown.</summary>
public readonly struct DocumentLocation : IEquatable<DocumentLocation>
{
    /// <summary>Initializes a new instance of the <see cref="DocumentLocation"/> struct.</summary>
    /// <param name="source">The system identifier of the document, or <see langword="null"/>.</param>
    /// <param name="line">The one-based line, zero when unknown.</param>
    /// <param name="column">The one-based column, zero when unknown.</param>
    public DocumentLocation(string? source, int line, int column)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(line, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(column, 0);

        Source = source;
        Line = line;
        Column = column;
    }

    /// <summary>Gets the system identifier of the document.</summary>
    public string? Source { get; }

    /// <summary>Gets the one-based line, zero when unknown.</summary>
    public int Line { get; }

    /// <summary>Gets the one-based column, zero when unknown.</summary>
    public int Column { get; }

    /// <inheritdoc />
    public bool Equals(DocumentLocation other) =>
        Line == other.Line && Column == other.Column && string.Equals(Source, other.Source, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DocumentLocation other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Source is null ? 0 : StringComparer.Ordinal.GetHashCode(Source);
            hash = (hash * 397) ^ Line;
            return (hash * 397) ^ Column;
        }
    }

    /// <summary>Returns <c>source(line,column)</c>, or <c>(line,column)</c> without a source.</summary>
    /// <returns>The text form.</returns>
    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture, "{0}({1},{2})", Source, Line, Column);

    /// <summary>Determines whether two locations are equal.</summary>
    /// <param name="left">The first location.</param>
    /// <param name="right">The second location.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(DocumentLocation left, DocumentLocation right) => left.Equals(right);

    /// <summary>Determines whether two locations differ.</summary>
    /// <param name="left">The first location.</param>
    /// <param name="right">The second location.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(DocumentLocation left, DocumentLocation right) => !left.Equals(right);
}
