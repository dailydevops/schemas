namespace NetEvolve.Schemas;

using System;
using System.Runtime.InteropServices;

/// <summary>An inclusive range of diagnostic code numbers owned by one package.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct DiagnosticCodeRange : IEquatable<DiagnosticCodeRange>
{
    /// <summary>Initializes a new instance of the <see cref="DiagnosticCodeRange"/> struct.</summary>
    /// <param name="first">The first number, at least 1.</param>
    /// <param name="last">The last number, at most 9999 and not below <paramref name="first"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The bounds violate <c>1 &lt;= first &lt;= last &lt;= 9999</c>.</exception>
    public DiagnosticCodeRange(int first, int last)
    {
        if (first < 1 || first > DiagnosticCode.MaxNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(first), first, "The first number must be between 1 and 9999.");
        }

        if (last < first || last > DiagnosticCode.MaxNumber)
        {
            throw new ArgumentOutOfRangeException(
                nameof(last),
                last,
                "The last number must be between first and 9999."
            );
        }

        First = first;
        Last = last;
    }

    /// <summary>Gets the first number.</summary>
    public int First { get; }

    /// <summary>Gets the last number.</summary>
    public int Last { get; }

    /// <summary>Determines whether a code lies in the range, inclusive on both ends.</summary>
    /// <param name="code">The code.</param>
    /// <returns><see langword="true"/> when the code lies in the range; always <see langword="false"/> for <see langword="default"/>.</returns>
    public bool Contains(DiagnosticCode code) => code.Number >= First && code.Number <= Last;

    /// <inheritdoc />
    public bool Equals(DiagnosticCodeRange other) => First == other.First && Last == other.Last;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DiagnosticCodeRange other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => (First * 10_000) + Last;

    /// <inheritdoc />
    public override string ToString() => new DiagnosticCode(First) + "-" + new DiagnosticCode(Last);

    /// <summary>Determines whether two ranges are equal.</summary>
    /// <param name="left">The first range.</param>
    /// <param name="right">The second range.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(DiagnosticCodeRange left, DiagnosticCodeRange right) => left.Equals(right);

    /// <summary>Determines whether two ranges differ.</summary>
    /// <param name="left">The first range.</param>
    /// <param name="right">The second range.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(DiagnosticCodeRange left, DiagnosticCodeRange right) => !left.Equals(right);
}
