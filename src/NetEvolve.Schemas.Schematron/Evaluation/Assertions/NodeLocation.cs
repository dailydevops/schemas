namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>The position and the SVRL location path of a reported node.</summary>
internal static class NodeLocation
{
    /// <summary>Gets the line and column of an element, of the parent element for any other node, and 0, 0 for a document node.</summary>
    internal static DocumentLocation Of(in XPathNode node, string? source) =>
        throw NotImplementedFailure.For("assertion evaluation");

    /// <summary>Appends the location path that XPath 1.0 and 2.0 processors evaluate to the node.</summary>
    internal static void AppendPath(in XPathNode node, TextSink into) =>
        throw NotImplementedFailure.For("assertion evaluation");
}
