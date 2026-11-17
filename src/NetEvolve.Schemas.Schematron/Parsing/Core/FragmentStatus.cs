namespace NetEvolve.Schemas.Schematron.Parsing.Core;

/// <summary>The outcome of parsing an included document.</summary>
internal enum FragmentStatus
{
    /// <summary>The document was parsed.</summary>
    Parsed = 0,

    /// <summary>The root element is allowed at the place by the grammar, but not supported yet; nothing was reported.</summary>
    Unsupported,

    /// <summary>The root element is not allowed at the place by the grammar; nothing was reported.</summary>
    WrongRoot,

    /// <summary>The document has problems; they were reported.</summary>
    Failed,
}
