namespace NetEvolve.Schemas.Xml.Datatypes.Floating;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>Framework-independent parsing and canonical output of <c>xs:float</c> and <c>xs:double</c>.</summary>
public static class XsdFloatingPoint
{
    /// <summary>Parses an <c>xs:double</c> lexical form.</summary>
    /// <param name="lexical">The whitespace-normalized lexical form.</param>
    /// <param name="version">The XSD version whose lexical rules apply.</param>
    /// <returns>The value or the error with its offset.</returns>
    public static ParseResult<double> ParseDouble(ReadOnlySpan<char> lexical, XsdVersion version) =>
        throw new NotSupportedException();

    /// <summary>Parses an <c>xs:float</c> lexical form.</summary>
    /// <param name="lexical">The whitespace-normalized lexical form.</param>
    /// <param name="version">The XSD version whose lexical rules apply.</param>
    /// <returns>The value or the error with its offset.</returns>
    public static ParseResult<float> ParseSingle(ReadOnlySpan<char> lexical, XsdVersion version) =>
        throw new NotSupportedException();

    /// <summary>Gets the canonical form of a double.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The canonical form.</returns>
    public static string ToCanonicalString(double value) => throw new NotSupportedException();

    /// <summary>Gets the canonical form of a float.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The canonical form.</returns>
    public static string ToCanonicalString(float value) => throw new NotSupportedException();
}
