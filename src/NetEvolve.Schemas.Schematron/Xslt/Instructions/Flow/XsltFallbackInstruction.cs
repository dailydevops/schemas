namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The entry of <see cref="XsltSyntheticElement.UnknownInstruction"/> (XSLT 2.0 section 18.2): an instruction element that the
/// supported XSLT versions do not define is replaced by the sequence constructors of its <c>xsl:fallback</c> children, in order.
/// </summary>
/// <remarks>
/// Limitation: the element-name set of XSLT 2.0 and 3.0 decides what is unknown; forwards-compatible processing by the
/// <c>version</c> attribute is not tracked (SCH-58). A known but deferred instruction never falls back.
/// </remarks>
internal sealed class XsltFallbackInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "fallback");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        problem = default;
        var bodies = new List<XsltExecutable>();
        var slotEnd = context.Scope.VariableNames.Count;
        var ok = true;
        for (var child = context.Element.Node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (!XsltElementReader.IsXslt(child, "fallback"))
            {
                continue;
            }

            ok &= FlowCompile.TryCompileBody(context, child.FirstChild, out var body, out var end);
            slotEnd = System.Math.Max(slotEnd, end);
            bodies.Add(body);
        }

        executable = ok ? new XsltFallbackExecutable(new XsltBodyExecutable([.. bodies]), slotEnd) : null!;
        return ok;
    }
}
