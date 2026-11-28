namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary><c>xsl:if</c> (XSLT 2.0 section 8.1): a required <c>test</c> and a sequence constructor that runs when the test is true.</summary>
internal sealed class XsltIfInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "if");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        problem = default;
        var ok = FlowCompile.TryReadBranch(context, context.Element, "xsl:if", out var branch);
        executable = ok ? branch : null!;
        return ok;
    }
}
