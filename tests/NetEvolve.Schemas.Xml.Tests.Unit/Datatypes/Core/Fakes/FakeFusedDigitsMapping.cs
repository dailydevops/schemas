namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core.Fakes;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>The digits fake with a fused path that counts which calls the descriptor makes.</summary>
internal sealed class FakeFusedDigitsMapping : ILexicalMapping<int>, IFusedLexicalMapping<int>
{
    private readonly FakeDigitsMapping _inner = new();

    public int CheckCalls { get; private set; }

    public int MapCalls { get; private set; }

    public int TryMapCalls { get; private set; }

    public WhitespaceMode Whitespace => _inner.Whitespace;

    public bool IsOrdered => _inner.IsOrdered;

    public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset)
    {
        CheckCalls++;
        return _inner.Check(normalized, version, out errorOffset);
    }

    public int Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version)
    {
        MapCalls++;
        return _inner.Map(normalized, mode, version);
    }

    public DatatypeError TryMap(
        ReadOnlySpan<char> normalized,
        WhitespaceMode mode,
        XsdVersion version,
        out int value,
        out int errorOffset
    )
    {
        TryMapCalls++;
        var error = _inner.Check(normalized, version, out errorOffset);
        value = error == DatatypeError.None ? _inner.Map(normalized, mode, version) : 0;
        return error;
    }

    public XsdOrder Compare(in int a, in int b, XsdVersion version) => _inner.Compare(in a, in b, version);

    public bool IsIdentical(in int a, in int b, XsdVersion version) => _inner.IsIdentical(in a, in b, version);
}
