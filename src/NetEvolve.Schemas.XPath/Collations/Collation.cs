namespace NetEvolve.Schemas.XPath.Collations;

using System;
using NetEvolve.Arguments;

/// <summary>
/// A collation (Functions and Operators 1.0, 7.3): the rules to compare strings and to find a string in another one. A host adds its own
/// through <see cref="CollationRegistry.With"/>.
/// </summary>
/// <remarks>
/// Implementations are stateless or thread-safe, and compare without allocating where they can. <see cref="Compare"/> and
/// <see cref="AreEqual"/> take strings, which every caller has at hand, so that the codepoint collation reuses
/// <c>CodepointComparison</c>; the matching operations take spans, because the callers cut the result from the text.
/// </remarks>
internal abstract class Collation
{
    /// <summary>Initializes a new instance of the <see cref="Collation"/> class.</summary>
    /// <param name="uri">The absolute URI that names the collation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="uri"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="uri"/> has no scheme, so it is not absolute.</exception>
    protected Collation(string uri)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(uri);
        ArgumentOutOfRangeException.ThrowIfEqual(CollationUri.IsAbsolute(uri), false);

        Uri = uri;
    }

    /// <summary>Gets the absolute URI that names the collation.</summary>
    public string Uri { get; }

    /// <summary>Compares two strings.</summary>
    /// <param name="first">The first string.</param>
    /// <param name="second">The second string.</param>
    /// <returns>Less than zero when the first string precedes the second, zero when they are equal, greater than zero otherwise; the magnitude carries no meaning.</returns>
    public abstract int Compare(string first, string second);

    /// <summary>Tests two strings for equality in this collation.</summary>
    /// <param name="first">The first string.</param>
    /// <param name="second">The second string.</param>
    /// <returns><see langword="true"/> when the strings are equal.</returns>
    public virtual bool AreEqual(string first, string second) => Compare(first, second) == 0;

    /// <summary>Tests whether a text starts with a value.</summary>
    /// <param name="text">The text.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the text starts with the value; always for a zero-length value.</returns>
    public abstract bool StartsWith(ReadOnlySpan<char> text, ReadOnlySpan<char> value);

    /// <summary>Tests whether a text ends with a value.</summary>
    /// <param name="text">The text.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the text ends with the value; always for a zero-length value.</returns>
    public abstract bool EndsWith(ReadOnlySpan<char> text, ReadOnlySpan<char> value);

    /// <summary>Finds the first match of a value in a text.</summary>
    /// <param name="text">The text.</param>
    /// <param name="value">The value.</param>
    /// <param name="index">The index of the match in code units, <c>-1</c> when there is none.</param>
    /// <param name="length">The length of the match in code units; it may differ from the length of the value in a collation that matches strings of different lengths.</param>
    /// <returns><see langword="true"/> when the text holds the value.</returns>
    public abstract bool TryFind(ReadOnlySpan<char> text, ReadOnlySpan<char> value, out int index, out int length);
}
