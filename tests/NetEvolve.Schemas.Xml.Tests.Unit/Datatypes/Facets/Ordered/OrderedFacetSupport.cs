namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Ordered;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

/// <summary>Builds restricted built-in types for the ordered facet tests; bounds and instances are parsed by the same descriptor and version.</summary>
internal static class OrderedFacetSupport
{
    internal static AtomicDescriptor<T> Base<T>(string typeName, XsdVersion version) =>
        (AtomicDescriptor<T>)BuiltInDatatypes.For(version).Get(typeName);

    internal static T Value<T>(string typeName, string lexical, XsdVersion version)
    {
        var result = Base<T>(typeName, version).Parse(lexical.AsSpan());
        return result.Success
            ? result.Value
            : throw new InvalidOperationException($"'{lexical}' is not a valid {typeName}: {result.Error}.");
    }

    internal static AtomicDescriptor<T> Restrict<T>(string typeName, XsdVersion version, params IFacet[] facets) =>
        new(typeName + "Restricted", Base<T>(typeName, version), null, facets);

    internal static IFacet Bound<T>(FacetKind kind, string typeName, string boundLexical, XsdVersion version)
        where T : struct
    {
        var type = Base<T>(typeName, version);
        var bound = Value<T>(typeName, boundLexical, version);
        return new BoundFacet<T>(kind, type, bound);
    }

    internal static ValidationResult Validate<T>(
        FacetKind kind,
        string typeName,
        string boundLexical,
        string lexical,
        XsdVersion version
    )
        where T : struct =>
        Restrict<T>(typeName, version, Bound<T>(kind, typeName, boundLexical, version)).Validate(lexical.AsSpan());
}
