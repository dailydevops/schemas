namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:when</c> (XSLT 2.0 section 8.2): a branch of <c>xsl:choose</c>. It is a structural child, registered for
/// <see cref="XsltPosition.Child"/>, so it is misplaced everywhere else; <c>xsl:choose</c> reads it directly.
/// </summary>
internal sealed class XsltWhenInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "when");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Child;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        problem = default;
        var ok = FlowCompile.TryReadBranch(context, context.Element, "xsl:when", out var branch);
        executable = ok ? branch : null!;
        return ok;
    }
}
