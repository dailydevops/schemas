namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The character data of a sequence constructor (XSLT 2.0 section 5.7.1), the entry of <see cref="XsltSyntheticElement.TextNode"/>: a
/// text node of the stylesheet creates a text node with the same value. The context element is the text node. White space only
/// text is stripped by the sequence constructor before it gets here.
/// </summary>
internal sealed class XsltLiteralTextInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name => default;

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        problem = default;
        executable = new XsltTextExecutable(context.Element.Node.StringValue);
        return true;
    }
}
