namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// <c>xsl:otherwise</c> (XSLT 2.0 section 8.2): the last branch of <c>xsl:choose</c>, a sequence constructor that runs when no
/// <c>xsl:when</c> test is true. It is a structural child, registered for <see cref="XsltPosition.Child"/>; <c>xsl:choose</c>
/// reads it directly.
/// </summary>
internal sealed class XsltOtherwiseInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "otherwise");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Child;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        problem = default;
        var ok = TryRead(context, context.Element, out var body, out _);
        executable = ok ? body : null!;
        return ok;
    }

    /// <summary>Reads an <c>xsl:otherwise</c> element.</summary>
    /// <param name="context">The compile context; its scope is the scope the element sits in.</param>
    /// <param name="element">The element.</param>
    /// <param name="body">The compiled body when the result is <see langword="true"/>.</param>
    /// <param name="slotEnd">The first slot above every slot of the body.</param>
    /// <returns><see langword="true"/> when the body compiled.</returns>
    internal static bool TryRead(
        XsltCompileContext context,
        XPathNode element,
        out XsltBodyExecutable body,
        out int slotEnd
    )
    {
        Argument.ThrowIfNull(context);

        return FlowCompile.TryCompileBody(context, element.Node.FirstChild, out body, out slotEnd);
    }
}
