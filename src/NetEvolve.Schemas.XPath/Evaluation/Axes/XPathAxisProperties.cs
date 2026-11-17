namespace NetEvolve.Schemas.XPath.Evaluation.Axes;

using System;

/// <summary>What the nodes of one axis step from one context node look like as delivered by <see cref="XPathAxisIterator"/>.</summary>
[Flags]
internal enum XPathAxisProperties
{
    /// <summary>No property.</summary>
    None = 0,

    /// <summary>A reverse axis: the nodes arrive nearest first, which is reverse document order and the order that positions count in.</summary>
    Reverse = 1,

    /// <summary>The nodes arrive in document order.</summary>
    InDocumentOrder = 2,

    /// <summary>No node arrives twice.</summary>
    DuplicateFree = 4,
}
