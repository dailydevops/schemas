namespace NetEvolve.Schemas.XPath.Evaluation.Axes;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Per-axis facts of the twelve supported axes.</summary>
/// <remarks>Limitation: the namespace axis is not supported and raises XPST0010 (#54), as XPath 2.0 allows.</remarks>
internal static class XPathAxes
{
    private const XPathAxisProperties Forward = XPathAxisProperties.InDocumentOrder | XPathAxisProperties.DuplicateFree;
    private const XPathAxisProperties Backward = XPathAxisProperties.Reverse | XPathAxisProperties.DuplicateFree;

    /// <summary>Gets the delivery properties of an axis for one context node: the input for deciding whether a step needs sorting and duplicate removal.</summary>
    /// <param name="axis">The axis.</param>
    /// <returns>The properties.</returns>
    /// <exception cref="XPathException">The axis is the namespace axis (XPST0010).</exception>
    public static XPathAxisProperties GetProperties(Axis axis) =>
        axis switch
        {
            Axis.Ancestor or Axis.AncestorOrSelf or Axis.Preceding or Axis.PrecedingSibling => Backward,
            Axis.Namespace => throw XPathErrors.Exception(XPathErrorCodes.XPST0010),
            _ => Forward,
        };
}
