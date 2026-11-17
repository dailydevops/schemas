namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The type-specific part of a primitive datatype: the lexical check and the mapping to the value space. A primitive package implements this once.</summary>
/// <typeparam name="TValue">The value type, always a non-ref struct.</typeparam>
public interface ILexicalMapping<TValue>
{
    /// <summary>Gets the whitespace mode of the primitive type.</summary>
    WhitespaceMode Whitespace { get; }

    /// <summary>Gets a value indicating whether the values are ordered.</summary>
    bool IsOrdered { get; }

    /// <summary>Checks the whitespace-normalized lexical form.</summary>
    /// <param name="normalized">The normalized lexical form.</param>
    /// <param name="version">The XSD version whose lexical rules apply.</param>
    /// <param name="errorOffset">The zero-based offset of the error in <paramref name="normalized"/>.</param>
    /// <returns><see cref="DatatypeError.None"/> when the form is in the lexical space.</returns>
    DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset);

    /// <summary>Maps a form that passed <see cref="Check"/> to its value.</summary>
    /// <param name="normalized">The normalized lexical form.</param>
    /// <param name="mode">The whitespace mode that produced <paramref name="normalized"/>, for values that are views of the caller span.</param>
    /// <param name="version">The XSD version whose value-space rules apply.</param>
    /// <returns>The value.</returns>
    TValue Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version);

    /// <summary>Compares two values.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <param name="version">The XSD version.</param>
    /// <returns>The order.</returns>
    XsdOrder Compare(in TValue a, in TValue b, XsdVersion version);

    /// <summary>Determines whether two values are identical.</summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <param name="version">The XSD version.</param>
    /// <returns><see langword="true"/> when identical.</returns>
    bool IsIdentical(in TValue a, in TValue b, XsdVersion version);
}
