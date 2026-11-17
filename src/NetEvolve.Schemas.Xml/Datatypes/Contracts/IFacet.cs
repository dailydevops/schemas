namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>A constraining facet of a datatype. Implement <see cref="ILexicalFacet"/> or <see cref="IValueFacet{TValue}"/>.</summary>
public interface IFacet
{
    /// <summary>Gets the kind of the facet.</summary>
    FacetKind Kind { get; }
}
