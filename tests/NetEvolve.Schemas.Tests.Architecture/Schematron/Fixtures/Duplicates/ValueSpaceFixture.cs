namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Duplicates;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>Violating fixture: a second datatype, found because it implements the datatype seam.</summary>
internal abstract class ValueSpaceFixture : IDatatypeDescriptor
{
    public abstract string Name { get; }

    public abstract DatatypeVariety Variety { get; }

    public abstract IDatatypeDescriptor? BaseType { get; }

    public abstract XsdVersion LexicalMode { get; }

    public abstract bool IsOrdered { get; }

    public abstract IReadOnlyList<IFacet> Facets { get; }

    public abstract ValidationResult Validate(ReadOnlySpan<char> lexical);
}
