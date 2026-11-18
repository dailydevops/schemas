namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The focus of a function body: none. The context node is the default node, position and size are zero; a binding that is handed
/// this focus treats the context item as absent, so a reference to it is XPDY0002 (XSLT 2.0 section 10.3, design #150 EP-13).
/// </summary>
internal static class XsltFunctionFocus
{
    /// <summary>Gets the absent focus.</summary>
    internal static QueryFocus None => new(default, 0, 0);
}
