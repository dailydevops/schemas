namespace NetEvolve.Schemas.Xsd.Components;

/// <summary>The symbol spaces of Structures section 2.5: one per kind of named component, so equal names in two spaces never clash.</summary>
internal enum XsdSymbolSpace
{
    /// <summary>Simple and complex type definitions share one space.</summary>
    Types = 0,
    Elements = 1,
    Attributes = 2,

    /// <summary>Model group definitions.</summary>
    Groups = 3,
    AttributeGroups = 4,
    IdentityConstraints = 5,
    Notations = 6,
}
