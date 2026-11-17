namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Contracts.Fakes;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>A lexical facet that accepts lexical forms up to a maximum length.</summary>
internal sealed class FakeLengthFacet(int maximum) : ILexicalFacet
{
    public FacetKind Kind => FacetKind.MaxLength;

    public bool IsSatisfiedBy(ReadOnlySpan<char> lexical) => lexical.Length <= maximum;
}
