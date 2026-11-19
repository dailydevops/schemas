namespace NetEvolve.Schemas.Xml.Dtd.Validation.Document;

/// <summary>The parse-time facts that only the declaration parser can see (design #122, D4).</summary>
internal enum DtdAnomalyKind
{
    /// <summary>A second declaration of an element type was dropped (Unique Element Type Declaration, section 3.2).</summary>
    ElementRedeclared = 0,

    /// <summary>A declaration starts and ends in different parameter-entity replacement texts (Proper Declaration/PE Nesting, section 2.8).</summary>
    DeclarationNesting = 1,

    /// <summary>A group starts and ends in different parameter-entity replacement texts (Proper Group/PE Nesting, section 3.2.1).</summary>
    GroupNesting = 2,

    /// <summary>A conditional section starts and ends in different parameter-entity replacement texts (Proper Conditional Section/PE Nesting, section 3.4).</summary>
    ConditionalSectionNesting = 3,
}
