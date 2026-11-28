namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>
/// An optional extension of <see cref="ILexicalMapping{TValue}"/> that checks and maps a lexical form in one pass. The
/// atomic descriptor uses it when a value is needed, so the form is scanned once instead of in <c>Check</c> and again in <c>Map</c>.
/// </summary>
/// <typeparam name="TValue">The value type.</typeparam>
internal interface IFusedLexicalMapping<TValue>
{
    /// <summary>Checks the whitespace-normalized lexical form and maps it to its value.</summary>
    /// <param name="normalized">The normalized lexical form.</param>
    /// <param name="mode">The whitespace mode that produced <paramref name="normalized"/>.</param>
    /// <param name="version">The XSD version whose rules apply.</param>
    /// <param name="value">The value; meaningful only when the result is <see cref="DatatypeError.None"/>.</param>
    /// <param name="errorOffset">The zero-based offset of the error in <paramref name="normalized"/>.</param>
    /// <returns><see cref="DatatypeError.None"/> when the form is in the lexical space.</returns>
    DatatypeError TryMap(
        ReadOnlySpan<char> normalized,
        WhitespaceMode mode,
        XsdVersion version,
        out TValue value,
        out int errorOffset
    );
}
