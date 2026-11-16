namespace NetEvolve.Schemas;

using System;
using System.Globalization;

/// <summary>A stable diagnostic code, <c>NES</c> plus four digits. <see langword="default"/> means unassigned (<c>NES0000</c>).</summary>
public readonly struct DiagnosticCode : IEquatable<DiagnosticCode>
{
    internal const int MaxNumber = 9999;

    /// <summary>Initializes a new instance of the <see cref="DiagnosticCode"/> struct.</summary>
    /// <param name="number">The number, 1 to 9999.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="number"/> is outside 1 to 9999.</exception>
    public DiagnosticCode(int number)
    {
        if (number < 1 || number > MaxNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, "The number must be between 1 and 9999.");
        }

        Number = number;
    }

    /// <summary>Gets the number, 0 for <see langword="default"/>.</summary>
    public int Number { get; }

    /// <inheritdoc />
    public bool Equals(DiagnosticCode other) => Number == other.Number;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DiagnosticCode other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Number;

    /// <inheritdoc />
    public override string ToString() => "NES" + Number.ToString("D4", CultureInfo.InvariantCulture);

    /// <summary>Determines whether two codes are equal.</summary>
    /// <param name="left">The first code.</param>
    /// <param name="right">The second code.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(DiagnosticCode left, DiagnosticCode right) => left.Equals(right);

    /// <summary>Determines whether two codes differ.</summary>
    /// <param name="left">The first code.</param>
    /// <param name="right">The second code.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(DiagnosticCode left, DiagnosticCode right) => !left.Equals(right);
}
