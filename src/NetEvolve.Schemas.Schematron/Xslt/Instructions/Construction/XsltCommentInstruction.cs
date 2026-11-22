namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:comment</c> (XSLT 2.0 section 11.8): a comment node whose value is the <see langword="select"/> expression or the content,
/// joined by one space.
/// </summary>
internal sealed class XsltCommentInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "comment");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        if (
            !ConstructionCompile.TryReadValue(
                context,
                element,
                "xsl:comment",
                "XTSE0940",
                out var select,
                out var body,
                out var slotEnd
            )
        )
        {
            return false;
        }

        executable = new XsltCommentExecutable(select, body, XsltElementResolver.LocationOf(element, null), slotEnd);
        return true;
    }
}
