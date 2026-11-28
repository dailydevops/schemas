namespace NetEvolve.Schemas.XPath.Collations;

using System;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;

/// <summary>
/// The Unicode codepoint collation (<c>http://www.w3.org/2005/xpath-functions/collation/codepoint</c>, Functions and Operators 1.0, 7.3.1):
/// strings compare by code point, not by UTF-16 code unit, and a match never starts or ends inside a surrogate pair. It is the only
/// built-in collation (#123 AP19).
/// </summary>
/// <remarks>
/// Limitation: an unpaired surrogate counts as the code point of its own code unit (as in <see cref="CodepointComparison"/>); a valid
/// <c>xs:string</c> holds none. No culture data is used, so the result is the same on every target framework. Nothing here allocates.
/// </remarks>
internal sealed class CodepointCollation : Collation
{
    private CodepointCollation()
        : base("http://www.w3.org/2005/xpath-functions/collation/codepoint") { }

    /// <summary>Gets the one instance.</summary>
    public static CodepointCollation Instance { get; } = new();

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public override int Compare(string first, string second) => CodepointComparison.Compare(first, second);

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public override bool AreEqual(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        // The codepoint order and the ordinal order agree on equality, and the ordinal test is the faster one.
        return string.Equals(first, second, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool StartsWith(ReadOnlySpan<char> text, ReadOnlySpan<char> value) =>
        text.StartsWith(value, StringComparison.Ordinal) && IsBoundary(text, value.Length);

    /// <inheritdoc />
    public override bool EndsWith(ReadOnlySpan<char> text, ReadOnlySpan<char> value) =>
        text.EndsWith(value, StringComparison.Ordinal) && IsBoundary(text, text.Length - value.Length);

    /// <inheritdoc />
    public override bool TryFind(ReadOnlySpan<char> text, ReadOnlySpan<char> value, out int index, out int length)
    {
        length = value.Length;
        var from = 0;
        while (from <= text.Length)
        {
            var found = text.Slice(from).IndexOf(value, StringComparison.Ordinal);
            if (found < 0)
            {
                break;
            }

            index = from + found;
            if (IsBoundary(text, index) && IsBoundary(text, index + value.Length))
            {
                return true;
            }

            from = index + 1;
        }

        index = -1;
        return false;
    }

    // A position between two code units is a boundary of the code points unless it lies inside a surrogate pair.
    private static bool IsBoundary(ReadOnlySpan<char> text, int position) =>
        position <= 0
        || position >= text.Length
        || !char.IsLowSurrogate(text[position])
        || !char.IsHighSurrogate(text[position - 1]);
}
