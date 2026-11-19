namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System.Text;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// <c>xsl:text</c> (XSLT 2.0 section 11.4.2): its character content, white space included, becomes a text node. Text that is an
/// immediate child of <c>xsl:text</c> is never stripped, so the content is taken as written.
/// </summary>
/// <remarks>
/// Limitation: <c>disable-output-escaping</c> is read and checked but has no effect, because the package produces no serialized
/// output; the attribute is a hint for a serializer (XSLT 2.0 section 20.2).
/// </remarks>
internal sealed class XsltTextInstruction : IXsltInstruction
{
    private static readonly string[] YesNo = ["yes", "no"];

    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "text");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var ok = FlowCompile.TryReadChoice(context, element, "disable-output-escaping", "xsl:text", YesNo, out _);

        var text = new StringBuilder();
        for (var child = element.Node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (child.Kind == XmlNodeKind.Text)
            {
                _ = text.Append(child.StringValue);
            }
            else if (child.Kind == XmlNodeKind.Element)
            {
                FlowCompile.Report(
                    context,
                    SchematronDiagnosticCodes.ElementMisplaced,
                    "The xsl:text element has no content other than text.",
                    new XPathNode(child),
                    FlowCompile.MisplacedConstraint
                );
                ok = false;
            }
        }

        if (!ok)
        {
            return false;
        }

        executable = new XsltTextExecutable(text.ToString());
        return true;
    }
}
