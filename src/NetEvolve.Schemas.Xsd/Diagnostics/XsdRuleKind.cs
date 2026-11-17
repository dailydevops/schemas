#pragma warning disable NE0002 // Approved public namespace NetEvolve.Schemas.Xsd for the rule catalog (design #146 DP-1)

namespace NetEvolve.Schemas.Xsd;

/// <summary>The family of a rule, which fixes its block of the Xsd diagnostic range.</summary>
public enum XsdRuleKind
{
    /// <summary>An engine limit that no specification constraint names, NES4000 to NES4019.</summary>
    Engine = 0,

    /// <summary>An XSD 1.1 construct rejected in strict 1.0 mode, NES4020 to NES4059.</summary>
    Strict10Rejection = 1,

    /// <summary>An XSD 1.1 construct that the default 1.1 mode does not implement yet, NES4060 to NES4099.</summary>
    NotYetSupported = 2,

    /// <summary>A schema representation constraint (<c>src-*</c>), NES4100 to NES4299.</summary>
    SchemaRepresentation = 3,

    /// <summary>A schema component constraint (<c>*-props-correct</c>, <c>cos-*</c>, ...), NES4300 to NES4599.</summary>
    SchemaComponent = 4,

    /// <summary>A validation rule (<c>cvc-*</c>), NES4600 to NES4899.</summary>
    Validation = 5,
}
