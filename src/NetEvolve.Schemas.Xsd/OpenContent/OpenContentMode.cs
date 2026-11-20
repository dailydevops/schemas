namespace NetEvolve.Schemas.Xsd.OpenContent;

/// <summary>The mode of an open content declaration (Structures 1.1, 3.4.2.3.3, the <c>mode</c> attribute).</summary>
internal enum OpenContentMode
{
    /// <summary>No open content: the type, or the default, is declared to have none (<c>mode="none"</c>).</summary>
    None = 0,

    /// <summary>The wildcard may match between and around the elements of the content model (<c>mode="interleave"</c>).</summary>
    Interleave = 1,

    /// <summary>The wildcard may match only after the content model is complete (<c>mode="suffix"</c>).</summary>
    Suffix = 2,
}
