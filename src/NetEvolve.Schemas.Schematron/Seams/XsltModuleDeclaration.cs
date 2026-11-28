namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// One placement of a top-level declaration of a stylesheet module: the element, the system identifier of the module that holds it,
/// and the import precedence of the level it belongs to. A module that is reached twice (a diamond of imports) has two placements
/// over one parsed tree, each with the rank of its own level.
/// </summary>
internal sealed class XsltModuleDeclaration
{
    internal XsltModuleDeclaration(XPathNode element, string? systemId, int precedence)
    {
        ArgumentNullException.ThrowIfNull(element.Tree);
        ArgumentOutOfRangeException.ThrowIfLessThan(precedence, 1);

        Element = element;
        SystemId = systemId;
        Precedence = precedence;
    }

    /// <summary>Gets the declaration element.</summary>
    internal XPathNode Element { get; }

    /// <summary>Gets the system identifier of the module that holds the element, or <see langword="null"/> when it has none.</summary>
    internal string? SystemId { get; }

    /// <summary>Gets the import precedence; a higher number wins, and the principal module has the highest.</summary>
    internal int Precedence { get; }
}
