namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Core.Fakes;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>A fake primitive whose values have two lexical forms: <c>one</c> and <c>uno</c> denote 1, <c>two</c> and <c>dos</c> denote 2.</summary>
internal sealed class FakeTwoFormsMapping : ILexicalMapping<int>
{
    public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

    public bool IsOrdered => false;

    public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
    {
        errorOffset = 0;
        return Value(normalized) == 0 ? DatatypeError.InvalidLexicalForm : DatatypeError.None;
    }

    public int Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version) => Value(normalized);

    // Unordered type: Compare never orders two values, only identity relates equal ones.
    public XsdOrder Compare(in int a, in int b, XsdVersion version) => XsdOrder.Incomparable;

    public bool IsIdentical(in int a, in int b, XsdVersion version) => a == b;

    private static int Value(ReadOnlySpan<char> text) =>
        text.SequenceEqual("one".AsSpan()) || text.SequenceEqual("uno".AsSpan()) ? 1
        : text.SequenceEqual("two".AsSpan()) || text.SequenceEqual("dos".AsSpan()) ? 2
        : 0;
}
