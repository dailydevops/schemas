namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core.Fakes;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>A fake primitive: a non-empty run of ASCII digits, collapsed, mapped to <see cref="int"/>.</summary>
internal sealed class FakeDigitsMapping : ILexicalMapping<int>
{
    public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

    public bool IsOrdered => true;

    public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
    {
        errorOffset = 0;
        if (normalized.IsEmpty)
        {
            return DatatypeError.InvalidCharacter;
        }

        for (var i = 0; i < normalized.Length; i++)
        {
            if (normalized[i] is < '0' or > '9')
            {
                errorOffset = i;
                return DatatypeError.InvalidCharacter;
            }
        }

        return DatatypeError.None;
    }

    public int Map(ReadOnlySpan<char> normalized, XsdVersion version)
    {
        var value = 0;
        for (var i = 0; i < normalized.Length; i++)
        {
            value = (value * 10) + (normalized[i] - '0');
        }

        return value;
    }

    public XsdOrder Compare(in int a, in int b, XsdVersion version) =>
        a < b ? XsdOrder.Less
        : a > b ? XsdOrder.Greater
        : XsdOrder.Equal;

    public bool IsIdentical(in int a, in int b, XsdVersion version) => a == b;
}
