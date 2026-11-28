namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:copy-of</c> (XSLT 2.0 section 11.9.2): a deep copy of the items of the <see langword="select"/> expression. The nodes are
/// copied through the Xml pump, so they have a new identity and keep their document order; atomic values are copied as they are.
/// </summary>
/// <remarks>
/// Limitations: <c>type</c> and <c>validation</c> (#23) and <c>copy-namespaces="no"</c> (#813) are the unsupported construct error;
/// a copied element always carries the namespace nodes of its source.
/// </remarks>
internal sealed class XsltCopyOfInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "copy-of");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var ok = ConstructionCompile.CheckUnsupported(context, element, "xsl:copy-of", "validation", "type");
        ok &= ConstructionCompile.TryReadSwitch(context, element, "copy-namespaces", "xsl:copy-of");
        QueryExpression? select = null;
        if (XsltElementReader.TryGetAttribute(element, "select", out var text))
        {
            ok &= FlowCompile.TryCompileExpression(context, text, QueryExpressionKind.Value, out select);
        }
        else
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeMissing,
                "The xsl:copy-of element has no select attribute.",
                element
            );
            ok = false;
        }

        if (!ok)
        {
            return false;
        }

        executable = new XsltCopyOfExecutable(
            select!,
            XsltElementResolver.LocationOf(element, null),
            context.Scope.VariableNames.Count
        );
        return true;
    }
}
