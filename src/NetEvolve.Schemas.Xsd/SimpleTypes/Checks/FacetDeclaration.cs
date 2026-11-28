namespace NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>One constraining facet of a restriction step as the schema document states it: the facet, its lexical value, and the fixed flag.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct FacetDeclaration
{
    internal FacetDeclaration(FacetKind kind, string value, DocumentLocation location, bool isFixed = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)kind, (int)FacetKind.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)kind, (int)FacetKind.ExplicitTimezone);
        ArgumentNullException.ThrowIfNull(value);

        Kind = kind;
        Value = value;
        Location = location;
        IsFixed = isFixed;
    }

    /// <summary>Gets the facet.</summary>
    public FacetKind Kind { get; }

    /// <summary>Gets the lexical value of the <see langword="value"/> attribute.</summary>
    public string Value { get; }

    /// <summary>Gets the location of the facet element.</summary>
    public DocumentLocation Location { get; }

    /// <summary>Gets a value indicating whether the <see langword="fixed"/> attribute is true, so further restrictions may not change the facet.</summary>
    public bool IsFixed { get; }
}
