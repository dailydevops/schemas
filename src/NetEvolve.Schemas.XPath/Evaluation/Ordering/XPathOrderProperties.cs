namespace NetEvolve.Schemas.XPath.Evaluation.Ordering;

using System;

/// <summary>What is statically known about a node sequence: its order, its duplicates, and how its nodes relate.</summary>
[Flags]
internal enum XPathOrderProperties
{
    /// <summary>Nothing is known.</summary>
    None = 0,

    /// <summary>The nodes are in document order.</summary>
    Ordered = 1,

    /// <summary>The nodes are in reverse document order.</summary>
    Reverse = 2,

    /// <summary>No node occurs twice.</summary>
    DuplicateFree = 4,

    /// <summary>The sequence holds at most one node.</summary>
    Single = 8,

    /// <summary>No node is an ancestor of another node of the sequence.</summary>
    NonNested = 16,
}
