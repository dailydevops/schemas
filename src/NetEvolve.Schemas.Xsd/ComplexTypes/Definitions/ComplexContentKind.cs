namespace NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;

/// <summary>The variety of the content type of a complex type definition (Structures section 3.4.1).</summary>
internal enum ComplexContentKind
{
    /// <summary>No content: neither character data nor child elements.</summary>
    Empty = 0,

    /// <summary>Character data that is a value of a simple type.</summary>
    Simple = 1,

    /// <summary>Child elements only.</summary>
    ElementOnly = 2,

    /// <summary>Child elements interleaved with character data.</summary>
    Mixed = 3,
}
