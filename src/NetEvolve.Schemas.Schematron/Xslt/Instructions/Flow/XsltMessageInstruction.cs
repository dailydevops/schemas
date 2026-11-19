namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:message</c> (XSLT 2.0 section 17): an optional <see langword="select"/> and a sequence constructor, which together make
/// the message, and an optional <c>terminate</c>. The message goes to the diagnostic channel of the frame; with
/// <c>terminate="yes"</c> the instruction then fails with the coded error XTMM9000.
/// </summary>
/// <remarks>
/// Limitations: the message is the string value of the constructed content, so the structure of a message is not kept, and the
/// content is built as text, which does not allow a literal result element in it until the construction instructions of SCH-67
/// (#253) provide a tree sink for it; the <c>terminate</c> value is a constant (#681); where the messages of a validation run go is
/// #683.
/// </remarks>
internal sealed class XsltMessageInstruction : IXsltInstruction
{
    private static readonly string[] YesNo = ["yes", "no"];

    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "message");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var ok = FlowCompile.TryReadChoice(context, element, "terminate", "xsl:message", YesNo, out var terminate);
        QueryExpression? select = null;
        if (XsltElementReader.TryGetAttribute(element, "select", out var text))
        {
            ok &= FlowCompile.TryCompileExpression(context, text, QueryExpressionKind.Value, out select);
        }

        ok &= FlowCompile.TryCompileBody(context, element.Node.FirstChild, out var body, out var slotEnd);
        if (!ok)
        {
            return false;
        }

        executable = new XsltMessageExecutable(
            select,
            body,
            string.Equals(terminate, "yes", System.StringComparison.Ordinal),
            slotEnd,
            XsltElementResolver.LocationOf(element, null)
        );
        return true;
    }
}
