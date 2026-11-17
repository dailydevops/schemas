namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

/// <summary>The unit in which the length facets (XSD 1.1 Part 2, sections 4.3.1 to 4.3.3) count.</summary>
public enum LengthUnit
{
    /// <summary>Unicode code points of a string type; a supplementary character counts once.</summary>
    Characters = 0,

    /// <summary>The items of a list type.</summary>
    Items = 1,
}
