namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Regex;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// <c>xsl:matching-substring</c> and <c>xsl:non-matching-substring</c> (XSLT 2.0 section 15.1): the two sequence constructors of
/// <c>xsl:analyze-string</c>. Each is a structural child, registered for <see cref="XsltPosition.Child"/>; the parent reads them
/// directly with <see cref="TryRead"/>.
/// </summary>
internal sealed class XsltSubstringInstruction : IXsltInstruction
{
    internal XsltSubstringInstruction(string localName) => Name = new(XsltInstructionRegistry.Namespace, localName);

    /// <inheritdoc />
    public XPathQName Name { get; }

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Child;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        problem = default;
        var ok = TryRead(context, context.Element, out var body, out _);
        executable = ok ? body : null!;
        return ok;
    }

    /// <summary>Reads the body of one of the two elements.</summary>
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
        ArgumentNullException.ThrowIfNull(context);

        return FlowCompile.TryCompileBody(context, element.Node.FirstChild, out body, out slotEnd);
    }
}
