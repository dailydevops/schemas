namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Definitions;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>A datatype with a scripted variety and facets.</summary>
internal sealed class FakeDatatype : IDatatypeDescriptor
{
    public FakeDatatype(string name, DatatypeVariety variety, params IFacet[] facets)
    {
        Name = name;
        Variety = variety;
        Facets = facets;
    }

    public string Name { get; }

    public DatatypeVariety Variety { get; }

    public IDatatypeDescriptor? BaseType => null;

    public XsdVersion LexicalMode => XsdVersion.V10;

    public bool IsOrdered => false;

    public IReadOnlyList<IFacet> Facets { get; }

    public ValidationResult Validate(ReadOnlySpan<char> lexical) => ValidationResult.Valid;
}

/// <summary>A facet that is only identified by its kind.</summary>
internal sealed class FakeFacet : IFacet
{
    public FakeFacet(FacetKind kind) => Kind = kind;

    public FacetKind Kind { get; }
}
