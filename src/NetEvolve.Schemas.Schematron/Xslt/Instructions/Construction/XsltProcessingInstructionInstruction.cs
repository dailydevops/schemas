namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:processing-instruction</c> (XSLT 2.0 section 11.6): a processing instruction node with a computed target (an attribute
/// value template) and the <see langword="select"/> expression or the content as its data.
/// </summary>
internal sealed class XsltProcessingInstructionInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "processing-instruction");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var ok = ConstructionCompile.TryReadRequired(
            context,
            element,
            "name",
            "xsl:processing-instruction",
            out var name
        );
        ok &= ConstructionCompile.TryReadValue(
            context,
            element,
            "xsl:processing-instruction",
            "XTSE0880",
            out var select,
            out var body,
            out var slotEnd
        );
        if (!ok)
        {
            return false;
        }

        executable = new XsltProcessingInstructionExecutable(
            name,
            select,
            body,
            XsltElementResolver.LocationOf(element, null),
            slotEnd
        );
        return true;
    }
}
