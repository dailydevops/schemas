namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The compiled form of an <c>xsl:function</c> declaration: it carries the component for the table that the link step builds, and it
/// does nothing when executed, because a declaration produces no output.
/// </summary>
internal sealed class XsltFunctionDeclaration : XsltExecutable
{
    internal XsltFunctionDeclaration(XsltFunctionComponent component)
    {
        Argument.ThrowIfNull(component);

        Component = component;
    }

    /// <summary>Gets the function.</summary>
    internal XsltFunctionComponent Component { get; }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink) => QueryStatus.Ok;
}
