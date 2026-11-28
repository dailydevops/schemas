namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// <c>xsl:choose</c> (XSLT 2.0 section 8.2): one or more <c>xsl:when</c> branches and an optional last <c>xsl:otherwise</c>. The
/// instruction reads its children directly, so their order and number are checked here.
/// </summary>
internal sealed class XsltChooseInstruction : IXsltInstruction
{
    private const string WhenLocalName = "when";
    private const string OtherwiseLocalName = "otherwise";

    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "choose");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var branches = new List<XsltIfExecutable>();
        XsltBodyExecutable? otherwise = null;
        var slotEnd = context.Scope.VariableNames.Count;
        var ok = true;
        for (var child = element.Node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (child.Kind == XmlNodeKind.Text && !XsltElementReader.IsWhitespace(child.StringValue))
            {
                Misplaced(context, element, "The xsl:choose element has no text content.");
                ok = false;
            }
            else if (child.Kind == XmlNodeKind.Element)
            {
                ok &= TryReadChild(context, new XPathNode(child), branches, ref otherwise, ref slotEnd);
            }
        }

        if (branches.Count == 0 && ok)
        {
            Misplaced(context, element, "The xsl:choose element has no xsl:when child.");
            ok = false;
        }

        if (!ok)
        {
            return false;
        }

        executable = new XsltChooseExecutable(branches.ToArray(), otherwise, slotEnd);
        return true;
    }

    private static bool TryReadChild(
        XsltCompileContext context,
        XPathNode child,
        List<XsltIfExecutable> branches,
        ref XsltBodyExecutable? otherwise,
        ref int slotEnd
    )
    {
        if (XsltElementReader.IsXslt(child.Node, WhenLocalName))
        {
            if (otherwise is not null)
            {
                Misplaced(context, child, "The xsl:when element is not allowed after xsl:otherwise.");
                return false;
            }

            if (!FlowCompile.TryReadBranch(context, child, "xsl:when", out var branch))
            {
                return false;
            }

            branches.Add(branch);
            slotEnd = System.Math.Max(slotEnd, branch.SlotEnd);
            return true;
        }

        if (XsltElementReader.IsXslt(child.Node, OtherwiseLocalName))
        {
            if (otherwise is not null)
            {
                Misplaced(context, child, "The xsl:choose element has more than one xsl:otherwise.");
                return false;
            }

            var ok = XsltOtherwiseInstruction.TryRead(context, child, out var body, out var end);
            otherwise = ok ? body : null;
            slotEnd = System.Math.Max(slotEnd, end);
            return ok;
        }

        Misplaced(context, child, "The xsl:choose element has no content other than xsl:when and xsl:otherwise.");
        return false;
    }

    private static void Misplaced(XsltCompileContext context, XPathNode element, string message) =>
        FlowCompile.Report(
            context,
            SchematronDiagnosticCodes.ElementMisplaced,
            message,
            element,
            FlowCompile.MisplacedConstraint
        );
}
