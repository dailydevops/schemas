namespace NetEvolve.Schemas.XPath.Functions.Documents;

/// <summary>The outcome of resolving the URI reference of a document or text function.</summary>
internal enum ResourceUriStatus
{
    /// <summary>The reference resolved to an absolute URI.</summary>
    Resolved,

    /// <summary>The reference is no URI reference, or carries a fragment identifier.</summary>
    Invalid,

    /// <summary>The reference is relative and there is no absolute base URI to resolve it against.</summary>
    NoBase,
}
