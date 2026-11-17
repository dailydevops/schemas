namespace NetEvolve.Schemas.Xsd.Attributes.Declarations;

/// <summary>The <c>use</c> of an attribute use (Structures section 3.5.2).</summary>
internal enum AttributeUseKind
{
    /// <summary>The attribute may appear.</summary>
    Optional = 0,

    /// <summary>The attribute must appear.</summary>
    Required = 1,

    /// <summary>The attribute must not appear.</summary>
    Prohibited = 2,
}
