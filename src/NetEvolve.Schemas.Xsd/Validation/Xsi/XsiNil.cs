namespace NetEvolve.Schemas.Xsd.Validation.Xsi;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>Reads the <c>xsi:nil</c> attribute: its value must be a valid <c>xs:boolean</c>.</summary>
internal sealed class XsiNil
{
    internal XsiNil(XsdVersion version) => throw new NotSupportedException();

    /// <summary>Reads a value.</summary>
    /// <param name="lexical">The attribute value as it appears in the instance.</param>
    /// <param name="nilled">Whether the value is <see langword="true"/> or <c>1</c>.</param>
    /// <returns><see langword="true"/> when the value is a valid boolean.</returns>
    internal bool TryParse(ReadOnlySpan<char> lexical, out bool nilled) => throw new NotSupportedException();
}
