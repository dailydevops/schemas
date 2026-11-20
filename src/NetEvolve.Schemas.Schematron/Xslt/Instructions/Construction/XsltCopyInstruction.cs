namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:copy</c> (XSLT 2.0 section 11.9.1): a shallow copy of the context item. An element or document node gets the content of the
/// sequence constructor (an element also the namespace nodes of the source); an attribute, namespace, text, comment, or processing
/// instruction node is copied as it is, and an atomic value is returned, without running the content.
/// </summary>
/// <remarks>
/// Limitations: <c>use-attribute-sets</c> (#34), <c>type</c> and <c>validation</c> (#23), and <c>copy-namespaces="no"</c> and
/// <c>inherit-namespaces="no"</c> (#813) are the unsupported construct error.
/// </remarks>
internal sealed class XsltCopyInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "copy");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var ok = ConstructionCompile.CheckUnsupported(
            context,
            element,
            "xsl:copy",
            "use-attribute-sets",
            "validation",
            "type"
        );
        ok &= ConstructionCompile.TryReadSwitch(context, element, "copy-namespaces", "xsl:copy");
        ok &= ConstructionCompile.TryReadSwitch(context, element, "inherit-namespaces", "xsl:copy");
        ok &= FlowCompile.TryCompileBody(context, element.Node.FirstChild, out var body, out var slotEnd);
        if (!ok)
        {
            return false;
        }

        executable = new XsltCopyExecutable(body, XsltElementResolver.LocationOf(element, null), slotEnd);
        return true;
    }
}
