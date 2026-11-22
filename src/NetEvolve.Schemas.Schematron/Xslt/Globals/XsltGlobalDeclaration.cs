namespace NetEvolve.Schemas.Schematron.Xslt.Globals;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The compiled form of a top-level <c>xsl:variable</c> or <c>xsl:param</c>: it carries the declaration for the table that the link
/// step builds and for the planner that gives it a slot, and it does nothing when executed, because a declaration produces no output
/// and a global is bound lazily, when it is first used.
/// </summary>
internal sealed class XsltGlobalDeclaration : XsltExecutable
{
    internal XsltGlobalDeclaration(XPathQName name, GlobalDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        Name = name;
        Declaration = declaration;
    }

    /// <summary>Gets the expanded name; an unprefixed name has no namespace.</summary>
    internal XPathQName Name { get; }

    /// <summary>Gets the declaration the planner takes.</summary>
    internal GlobalDeclaration Declaration { get; }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(sink);

        return QueryStatus.Ok;
    }
}
