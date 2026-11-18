namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// <c>xsl:sequence</c> (XSLT 2.0 section 11.9): the value of its <see langword="select"/> expression is the result, nodes by reference and
/// without a copy. The only content XSLT 2.0 allows is <c>xsl:fallback</c>, which a 2.0 processor ignores and so does this one.
/// </summary>
internal sealed class XsltSequenceInstruction : IXsltInstruction
{
    private const string FallbackLocalName = "fallback";

    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "sequence");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var ok = true;
        if (!XsltElementReader.TryGetAttribute(element, "select", out var select))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeMissing,
                "The xsl:sequence element has no select attribute.",
                element
            );
            return false;
        }

        for (var child = element.Node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            var misplaced =
                child.Kind == XmlNodeKind.Element
                    ? !XsltElementReader.IsXslt(child, FallbackLocalName)
                    : child.Kind == XmlNodeKind.Text && !XsltElementReader.IsWhitespace(child.StringValue);
            if (misplaced)
            {
                XsltProblems.Report(
                    context.Problems,
                    SchematronDiagnosticCodes.ElementMisplaced,
                    "The xsl:sequence element has no content other than xsl:fallback.",
                    child.Kind == XmlNodeKind.Element ? new XPathNode(child) : element,
                    "XTSE0010"
                );
                ok = false;
            }
        }

        var binding = context.Binding
            ?? throw new InvalidOperationException(
                "The compiler of the context gave no query binding for an expression."
            );
        if (
            !binding.TryCompile(select.AsSpan(), QueryExpressionKind.Value, context.Scope, out var value, out var found)
        )
        {
            context.Problems.Add(in found);
            return false;
        }

        if (!ok)
        {
            return false;
        }

        executable = new XsltSequenceExecutable(value);
        return true;
    }
}
