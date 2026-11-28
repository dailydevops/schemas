namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:element</c> (XSLT 2.0 section 11.2): an element with a computed name and namespace, and the content of the sequence
/// constructor.
/// </summary>
/// <remarks>
/// Limitations: <c>use-attribute-sets</c> (#34), <c>type</c> and <c>validation</c> (#23), and <c>inherit-namespaces="no"</c> (#813)
/// are the unsupported construct error.
/// </remarks>
internal sealed class XsltElementInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "element");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var ok = ConstructionCompile.CheckUnsupported(
            context,
            element,
            "xsl:element",
            "use-attribute-sets",
            "validation",
            "type"
        );
        ok &= ConstructionCompile.TryReadSwitch(context, element, "inherit-namespaces", "xsl:element");
        ok &= NameTemplate.TryCompile(context, true, out var name);
        ok &= FlowCompile.TryCompileBody(context, element.Node.FirstChild, out var body, out var slotEnd);
        if (!ok)
        {
            return false;
        }

        executable = new XsltElementExecutable(name, body, XsltElementResolver.LocationOf(element, null), slotEnd);
        return true;
    }
}
