namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:for-each</c> (XSLT 2.0 section 8.1): a required <see langword="select"/>, the <c>xsl:sort</c> elements, and a sequence
/// constructor that runs once per item with the item as the context item. The sort keys come first (XSLT 2.0 section 13).
/// </summary>
internal sealed class XsltForEachInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "for-each");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        QueryExpression? select = null;
        bool ok;
        if (XsltElementReader.TryGetAttribute(element, "select", out var text))
        {
            ok = FlowCompile.TryCompileExpression(context, text, QueryExpressionKind.Value, out select);
        }
        else
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeMissing,
                "The xsl:for-each element has no select attribute.",
                element
            );
            ok = false;
        }

        var keys = new List<XsltSortKey>();
        ok &= XsltSortInstruction.TryReadKeys(context, keys, out var first);
        ok &= FlowCompile.TryCompileBody(context, first, out var body, out var slotEnd);
        if (!ok)
        {
            return false;
        }

        executable = new XsltForEachExecutable(
            select!,
            keys.ToArray(),
            body,
            slotEnd,
            XsltElementResolver.LocationOf(element, null)
        );
        return true;
    }
}
