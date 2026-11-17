namespace NetEvolve.Schemas.Xsd.Elements;

/// <summary>The kind of the value constraint of an element declaration (Structures section 3.3.1).</summary>
internal enum ElementValueConstraintKind
{
    /// <summary>The declaration has no value constraint.</summary>
    None = 0,

    /// <summary>The declaration has a default value.</summary>
    Default = 1,

    /// <summary>The declaration has a fixed value.</summary>
    Fixed = 2,
}
