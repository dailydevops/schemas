namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System;
using System.Runtime.InteropServices;

/// <summary>A half-open range of characters in the expression text.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct SourceSpan : IEquatable<SourceSpan>
{
    /// <summary>Initializes a new instance of the <see cref="SourceSpan"/> struct.</summary>
    /// <param name="offset">The zero-based offset of the first character.</param>
    /// <param name="length">The number of characters.</param>
    public SourceSpan(int offset, int length)
    {
        Offset = offset;
        Length = length;
    }

    /// <summary>Gets the zero-based offset of the first character.</summary>
    public int Offset { get; }

    /// <summary>Gets the number of characters.</summary>
    public int Length { get; }

    /// <summary>Gets the offset just after the last character.</summary>
    public int End => Offset + Length;

    /// <inheritdoc />
    public bool Equals(SourceSpan other) => Offset == other.Offset && Length == other.Length;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SourceSpan other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => (Offset * 397) ^ Length;

    /// <summary>Determines whether two spans are equal.</summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(SourceSpan left, SourceSpan right) => left.Equals(right);

    /// <summary>Determines whether two spans differ.</summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(SourceSpan left, SourceSpan right) => !left.Equals(right);
}
