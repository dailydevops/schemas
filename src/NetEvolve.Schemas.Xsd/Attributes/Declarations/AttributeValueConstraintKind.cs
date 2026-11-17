namespace NetEvolve.Schemas.Xsd.Attributes.Declarations;

/// <summary>Whether a value constraint is a default or a fixed value.</summary>
internal enum AttributeValueConstraintKind
{
    /// <summary>No value constraint.</summary>
    None = 0,
    Default = 1,
    Fixed = 2,
}
