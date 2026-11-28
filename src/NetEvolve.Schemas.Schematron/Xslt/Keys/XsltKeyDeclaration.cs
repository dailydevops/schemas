namespace NetEvolve.Schemas.Schematron.Xslt.Keys;

using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The compiled form of an <c>xsl:key</c> declaration: it carries the component for the table that the link step builds, and it does
/// nothing when executed, because a declaration produces no output.
/// </summary>
internal sealed class XsltKeyDeclaration : XsltExecutable
{
    internal XsltKeyDeclaration(XsltKeyComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        Component = component;
    }

    /// <summary>Gets the key declaration.</summary>
    internal XsltKeyComponent Component { get; }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(sink);

        return QueryStatus.Ok;
    }
}
