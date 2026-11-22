namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>Reports the compile problems of the instructions of this folder, located at an element, through the problem sink of the context.</summary>
internal static class XsltProblems
{
    /// <summary>Adds a problem located at the start tag of an element.</summary>
    /// <param name="problems">The sink.</param>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <param name="element">The element the problem belongs to.</param>
    /// <param name="constraint">The code of the XSLT or XPath rule, or <see langword="null"/>.</param>
    internal static void Report(
        IProblemSink problems,
        DiagnosticCode code,
        string message,
        XPathNode element,
        string? constraint = null
    )
    {
        ArgumentNullException.ThrowIfNull(problems);
        ArgumentNullException.ThrowIfNull(message);

        var problem = new SchematronProblem(code, message, XsltElementResolver.LocationOf(element, null), constraint);
        problems.Add(in problem);
    }
}
