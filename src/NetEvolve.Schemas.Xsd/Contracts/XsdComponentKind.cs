namespace NetEvolve.Schemas.Xsd.Contracts;

/// <summary>The kinds of schema components of Structures section 2.2 and section 3.</summary>
internal enum XsdComponentKind
{
    SimpleType = 0,
    ComplexType = 1,
    AttributeDeclaration = 2,
    ElementDeclaration = 3,
    AttributeUse = 4,
    AttributeGroup = 5,
    ModelGroupDefinition = 6,
    ModelGroup = 7,
    Particle = 8,
    Wildcard = 9,
    IdentityConstraint = 10,
    Notation = 11,

    /// <summary>An XSD 1.1 assertion.</summary>
    Assertion = 12,

    /// <summary>An XSD 1.1 type alternative.</summary>
    TypeAlternative = 13,

    /// <summary>An XSD 1.1 type table.</summary>
    TypeTable = 14,
}
