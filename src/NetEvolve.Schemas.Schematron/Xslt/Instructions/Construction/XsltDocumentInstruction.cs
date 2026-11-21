namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:document</c> (XSLT 2.0 section 11.5): a document node whose children are the content of the sequence constructor.
/// </summary>
/// <remarks>Limitation: <c>type</c> and <c>validation</c> are the unsupported construct error (#23).</remarks>
internal sealed class XsltDocumentInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "document");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var ok = ConstructionCompile.CheckUnsupported(context, element, "xsl:document", "validation", "type");
        ok &= FlowCompile.TryCompileBody(context, element.Node.FirstChild, out var body, out var slotEnd);
        if (!ok)
        {
            return false;
        }

        executable = new XsltDocumentExecutable(body, XsltElementResolver.LocationOf(element, null), slotEnd);
        return true;
    }
}
