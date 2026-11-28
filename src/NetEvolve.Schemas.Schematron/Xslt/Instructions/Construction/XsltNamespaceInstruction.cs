namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:namespace</c> (XSLT 2.0 section 11.7): a namespace node with a computed prefix (an attribute value template; empty for the
/// default namespace) whose value is the <see langword="select"/> expression or the content, joined by one space.
/// </summary>
internal sealed class XsltNamespaceInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "namespace");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var ok = ConstructionCompile.TryReadRequired(context, element, "name", "xsl:namespace", out var name);
        ok &= ConstructionCompile.TryReadValue(
            context,
            element,
            "xsl:namespace",
            "XTSE0910",
            out var select,
            out var body,
            out var slotEnd
        );
        if (ok && select is null && !XsltVariableInstruction.HasContent(element))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeMissing,
                "The xsl:namespace element has neither a select attribute nor content.",
                element,
                "XTSE0910"
            );
            ok = false;
        }

        if (!ok)
        {
            return false;
        }

        executable = new XsltNamespaceExecutable(
            name,
            select,
            body,
            XsltElementResolver.LocationOf(element, null),
            slotEnd
        );
        return true;
    }
}
