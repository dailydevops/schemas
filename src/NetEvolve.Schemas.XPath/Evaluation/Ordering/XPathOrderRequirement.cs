namespace NetEvolve.Schemas.XPath.Evaluation.Ordering;

/// <summary>What a step result needs to become a document-ordered, duplicate-free sequence; the flag the evaluator reads from an executable step (XP-10, #158).</summary>
internal enum XPathOrderRequirement
{
    /// <summary>Nothing: the result already is in document order without duplicates.</summary>
    None = 0,

    /// <summary>Reverse the result: it is duplicate-free and in reverse document order.</summary>
    Reverse = 1,

    /// <summary>Drop adjacent duplicates: the result is in document order.</summary>
    Deduplicate = 2,

    /// <summary>Sort by document order and remove duplicates.</summary>
    Sort = 3,
}
