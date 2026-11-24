namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Regex;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// <c>xsl:analyze-string</c> (XSLT 2.0 section 15.1): a required <see langword="select"/>, a required <c>regex</c> and an optional
/// <c>flags</c>, both attribute value templates, then an optional <c>xsl:matching-substring</c> and an optional
/// <c>xsl:non-matching-substring</c>, at least one of the two (XTSE1130), in this order. Every problem is reported through the
/// context and the compile continues after one.
/// </summary>
/// <remarks>
/// Limitations: <c>xsl:fallback</c> inside the instruction is the unsupported construct error (DEF-27, #39); an invalid regex, invalid
/// flags, and a regex that matches the zero-length string are dynamic errors (XTDE1140, XTDE1145, XTDE1150) reported when the
/// instruction runs, also when the attributes are constants.
/// </remarks>
internal sealed class XsltAnalyzeStringInstruction : IXsltInstruction
{
    private const string MatchingLocalName = "matching-substring";
    private const string NonMatchingLocalName = "non-matching-substring";
    private const string FallbackLocalName = "fallback";

    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "analyze-string");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var ok = TryCompileSelect(context, out var select);
        ok &= TryCompileTemplate(context, "regex", required: true, out var regex);
        ok &= TryCompileTemplate(context, "flags", required: false, out var flags);
        ok &= TryReadChildren(context, out var matching, out var nonMatching, out var slotEnd);
        if (!ok)
        {
            return false;
        }

        executable = new XsltAnalyzeStringExecutable(
            select!,
            regex!,
            flags,
            matching,
            nonMatching,
            slotEnd,
            XsltElementResolver.LocationOf(context.Element, null)
        );
        return true;
    }

    private static bool TryCompileSelect(XsltCompileContext context, out QueryExpression? select)
    {
        select = null;
        if (XsltElementReader.TryGetAttribute(context.Element, "select", out var text))
        {
            return FlowCompile.TryCompileExpression(context, text, QueryExpressionKind.Value, out select);
        }

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.AttributeMissing,
            "The xsl:analyze-string element has no select attribute.",
            context.Element
        );
        return false;
    }

    private static bool TryCompileTemplate(
        XsltCompileContext context,
        string name,
        bool required,
        out AttributeValueTemplate? template
    )
    {
        template = null;
        if (!XsltElementReader.TryGetAttribute(context.Element, name, out var text))
        {
            if (required)
            {
                XsltProblems.Report(
                    context.Problems,
                    SchematronDiagnosticCodes.AttributeMissing,
                    $"The xsl:analyze-string element has no {name} attribute.",
                    context.Element
                );
            }

            return !required;
        }

        var ok = AttributeValueTemplate.TryCompile(context, context.Element, name, text, out var compiled);
        template = ok ? compiled : null;
        return ok;
    }

    private static bool TryReadChildren(
        XsltCompileContext context,
        out XsltBodyExecutable? matching,
        out XsltBodyExecutable? nonMatching,
        out int slotEnd
    )
    {
        var element = context.Element;
        matching = null;
        nonMatching = null;
        slotEnd = context.Scope.VariableNames.Count;
        var ok = true;
        for (var child = element.Node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (child.Kind == XmlNodeKind.Text && !XsltElementReader.IsWhitespace(child.StringValue))
            {
                ok &= Misplaced(context, element, "The xsl:analyze-string element has no text content.");
            }
            else if (child.Kind == XmlNodeKind.Element)
            {
                ok &= TryReadChild(context, new XPathNode(child), ref matching, ref nonMatching, ref slotEnd);
            }
        }

        if (ok && matching is null && nonMatching is null)
        {
            return Misplaced(
                context,
                element,
                "The xsl:analyze-string element has neither xsl:matching-substring nor xsl:non-matching-substring.",
                "XTSE1130"
            );
        }

        return ok;
    }

    private static bool TryReadChild(
        XsltCompileContext context,
        XPathNode child,
        ref XsltBodyExecutable? matching,
        ref XsltBodyExecutable? nonMatching,
        ref int slotEnd
    )
    {
        var isMatching = XsltElementReader.IsXslt(child.Node, MatchingLocalName);
        if (!isMatching && !XsltElementReader.IsXslt(child.Node, NonMatchingLocalName))
        {
            return XsltElementReader.IsXslt(child.Node, FallbackLocalName)
                ? Unsupported(context, child)
                : Misplaced(
                    context,
                    child,
                    "The xsl:analyze-string element has no content other than xsl:matching-substring and xsl:non-matching-substring."
                );
        }

        if ((isMatching ? matching : nonMatching) is not null || (isMatching && nonMatching is not null))
        {
            return Misplaced(
                context,
                child,
                isMatching
                    ? "The xsl:matching-substring element is repeated or follows xsl:non-matching-substring."
                    : "The xsl:non-matching-substring element is repeated."
            );
        }

        var ok = XsltSubstringInstruction.TryRead(context, child, out var body, out var end);
        slotEnd = System.Math.Max(slotEnd, end);
        if (!ok)
        {
            return false;
        }

        if (isMatching)
        {
            matching = body;
        }
        else
        {
            nonMatching = body;
        }

        return true;
    }

    private static bool Unsupported(XsltCompileContext context, XPathNode child)
    {
        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.XsltUnsupported,
            "The xsl:fallback element inside xsl:analyze-string is not supported (DEF-27, #39).",
            child
        );
        return false;
    }

    private static bool Misplaced(
        XsltCompileContext context,
        XPathNode element,
        string message,
        string constraint = FlowCompile.MisplacedConstraint
    )
    {
        FlowCompile.Report(context, SchematronDiagnosticCodes.ElementMisplaced, message, element, constraint);
        return false;
    }
}
