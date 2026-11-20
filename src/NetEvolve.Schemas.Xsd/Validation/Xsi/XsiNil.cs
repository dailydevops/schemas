namespace NetEvolve.Schemas.Xsd.Validation.Xsi;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>Reads the <c>xsi:nil</c> attribute: its value must be a valid <c>xs:boolean</c>.</summary>
internal sealed class XsiNil
{
    private static readonly char[] Whitespace = [' ', '\t', '\r', '\n'];

    private readonly IDatatypeDescriptor _boolean;

    internal XsiNil(XsdVersion version)
    {
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        _boolean = BuiltInDatatypes.For(version).Get("boolean");
    }

    /// <summary>Reads a value.</summary>
    /// <param name="lexical">The attribute value as it appears in the instance.</param>
    /// <param name="nilled">Whether the value is the literal true or 1.</param>
    /// <returns><see langword="true"/> when the value is a valid boolean.</returns>
    internal bool TryParse(ReadOnlySpan<char> lexical, out bool nilled)
    {
        nilled = false;
        if (!_boolean.Validate(lexical).IsValid)
        {
            return false;
        }

        var value = lexical.Trim(Whitespace);
        nilled = value.SequenceEqual("true".AsSpan()) || value.SequenceEqual("1".AsSpan());
        return true;
    }
}
