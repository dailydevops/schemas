namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

/// <summary>The values of the <c>explicitTimezone</c> facet (XSD 1.1 Part 2, section 4.3.14).</summary>
public enum ExplicitTimezoneMode
{
    /// <summary>A timezone may be present or absent.</summary>
    Optional = 0,

    /// <summary>A timezone must be present.</summary>
    Required = 1,

    /// <summary>A timezone must not be present.</summary>
    Prohibited = 2,
}
