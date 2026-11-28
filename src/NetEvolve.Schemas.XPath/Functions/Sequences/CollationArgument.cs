namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using System;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The collation argument of the functions that take one: only the Unicode codepoint collation is supported, and any other collation is FOCH0002.</summary>
/// <remarks>Limitation: collations other than the codepoint collation are left to XP-25 (#267) and DEF-43 (#55); the explicit error is the documented behavior until then.</remarks>
internal static class CollationArgument
{
    /// <summary>The URI of the Unicode codepoint collation.</summary>
    internal const string CodepointUri = "http://www.w3.org/2005/xpath-functions/collation/codepoint";

    /// <summary>Checks the collation argument of a call.</summary>
    /// <param name="arguments">The converted arguments of the call.</param>
    /// <param name="index">The position of the collation argument; a call with fewer arguments uses the default collation, which is the codepoint collation.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    /// <exception cref="XPathException">The collation is not the codepoint collation (FOCH0002).</exception>
    public static void Check(ReadOnlySpan<XdmSequence> arguments, int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);

        if (index >= arguments.Length)
        {
            return;
        }

        var argument = arguments[index];
        if (
            argument.Count != 1
            || !argument[0].TryGetString(out var uri)
            || !string.Equals(uri, CodepointUri, StringComparison.Ordinal)
        )
        {
            throw XPathErrors.Exception(XPathErrorCodes.FOCH0002);
        }
    }
}
