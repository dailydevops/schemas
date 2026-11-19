namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:value-of</c> (XSLT 2.0 section 11.4.3): the string value of its <see langword="select"/> expression or of its content,
/// with the items joined by the separator, becomes text in the sink. The default separator is one space for <see langword="select"/>
/// and the empty string for content.
/// </summary>
/// <remarks>
/// Limitations: the first-item-only behavior of backwards compatible mode is not implemented, the instruction always follows XSLT
/// 2.0 (#298); the <c>separator</c> is a constant, not an attribute value template (#681); <c>disable-output-escaping</c> is not
/// supported and not read.
/// </remarks>
internal sealed class XsltValueOfInstruction : IXsltInstruction
{
    private const string ContentConstraint = "XTSE0870";

    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "value-of");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var hasSelect = XsltElementReader.TryGetAttribute(element, "select", out var selectText);
        var hasContent = XsltVariableInstruction.HasContent(element);
        var ok = FlowCompile.TryReadConstant(context, element, "separator", "xsl:value-of", out var separator);
        if (hasSelect == hasContent)
        {
            XsltProblems.Report(
                context.Problems,
                hasSelect ? SchematronDiagnosticCodes.ElementMisplaced : SchematronDiagnosticCodes.AttributeMissing,
                hasSelect
                    ? "The xsl:value-of element has either a select attribute or content, not both."
                    : "The xsl:value-of element has neither a select attribute nor content.",
                element,
                ContentConstraint
            );
            return false;
        }

        QueryExpression? select = null;
        XsltBodyExecutable? body = null;
        var slotEnd = context.Scope.VariableNames.Count;
        if (hasSelect)
        {
            ok &= FlowCompile.TryCompileExpression(context, selectText, QueryExpressionKind.Value, out select);
        }
        else
        {
            ok &= FlowCompile.TryCompileBody(context, element.Node.FirstChild, out body, out slotEnd);
        }

        if (!ok)
        {
            return false;
        }

        executable = new XsltValueOfExecutable(
            select,
            body,
            separator ?? (hasSelect ? " " : string.Empty),
            XsltElementResolver.LocationOf(element, null),
            slotEnd
        );
        return true;
    }
}
