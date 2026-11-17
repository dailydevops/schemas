namespace NetEvolve.Schemas.Xsd.Attributes.Declarations;

/// <summary>Whether a value constraint is a default or a fixed value.</summary>
internal enum AttributeValueConstraintKind
{
    /// <summary>No value constraint.</summary>
    None = 0,

    /// <summary>The value is used when the attribute is absent.</summary>
    Default = 1,

    /// <summary>The attribute, when present, must have this value; it is also used when absent.</summary>
    Fixed = 2,
}
