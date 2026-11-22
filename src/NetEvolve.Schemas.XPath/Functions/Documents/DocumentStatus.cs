namespace NetEvolve.Schemas.XPath.Functions.Documents;

/// <summary>The outcome of asking the pool for the document of a reference.</summary>
internal enum DocumentStatus
{
    /// <summary>The document is loaded or was pooled.</summary>
    Loaded,

    /// <summary>The reference is no URI reference (FODC0005).</summary>
    InvalidUri,

    /// <summary>The resource cannot be retrieved: no resolver, a refusal, a miss, a relative reference without a base, or a document that is not well formed (FODC0002).</summary>
    Unavailable,
}
