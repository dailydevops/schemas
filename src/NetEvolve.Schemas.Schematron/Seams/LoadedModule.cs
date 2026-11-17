namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>A loaded module: the XPath node of the document node and the resolved system identifier.</summary>
internal readonly struct LoadedModule
{
    internal LoadedModule(XPathNode root, string systemId)
    {
        Root = root;
        SystemId = systemId;
    }

    internal XPathNode Root { get; }

    internal string SystemId { get; }
}
