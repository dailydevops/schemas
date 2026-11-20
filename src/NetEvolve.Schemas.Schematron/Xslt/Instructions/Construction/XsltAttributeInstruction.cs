namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:attribute</c> (XSLT 2.0 section 11.3): an attribute with a computed name and namespace, whose value is the
/// <see langword="select"/> expression or the content, joined by the separator (default one space).
/// </summary>
/// <remarks>Limitation: <c>type</c> and <c>validation</c> are the unsupported construct error (#23).</remarks>
internal sealed class XsltAttributeInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "attribute");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var ok = ConstructionCompile.CheckUnsupported(context, element, "xsl:attribute", "validation", "type");
        ok &= NameTemplate.TryCompile(context, false, out var name);
        ok &= ConstructionCompile.TryReadOptional(context, element, "separator", out var separator);
        ok &= ConstructionCompile.TryReadValue(
            context,
            element,
            "xsl:attribute",
            "XTSE0840",
            out var select,
            out var body,
            out var slotEnd
        );
        if (!ok)
        {
            return false;
        }

        executable = new XsltAttributeExecutable(
            name,
            separator,
            select,
            body,
            XsltElementResolver.LocationOf(element, null),
            slotEnd
        );
        return true;
    }
}
