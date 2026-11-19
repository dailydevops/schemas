namespace NetEvolve.Schemas.XPath.Casting.Core;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>
/// The whitespace of the <c>whiteSpace</c> facet applied to a string. The casts to the derived string types and to <c>xs:anyURI</c> and
/// <c>xs:QName</c> share it.
/// </summary>
internal static class WhitespaceText
{
    // A text up to this length is normalized in stack memory; a longer one rents a pooled buffer.
    private const int StackLength = 256;

    /// <summary>Applies the whitespace mode to a lexical form.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <param name="mode">The whitespace mode.</param>
    /// <returns>The lexical form with the whitespace of the mode; <paramref name="lexical"/> itself when nothing changes.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="lexical"/> is <see langword="null"/>.</exception>
    public static string Apply(string lexical, WhitespaceMode mode)
    {
        Argument.ThrowIfNull(lexical);

        if (WhitespaceNormalizer.IsNormalized(lexical.AsSpan(), mode))
        {
            return lexical;
        }

        var normalized = WhitespaceNormalizer.Apply(
            lexical.AsSpan(),
            mode,
            stackalloc char[StackLength],
            out var rented
        );
        try
        {
            return normalized.ToString();
        }
        finally
        {
            rented?.Dispose();
        }
    }
}
