namespace NetEvolve.Schemas.XPath.Evaluation.Axes;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>A node test bound to the name table of a tree: names are compared as integer handles, never as strings.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XPathNodeMatcher
{
    private readonly XPathNodeTestKind _kind;
    private readonly XmlSymbol _local;
    private readonly XmlSymbol _namespace;
    private readonly bool _anyLocal;
    private readonly bool _anyNamespace;
    private readonly bool _requireDocumentElement;
    private readonly bool _neverMatches;

    internal XPathNodeMatcher(
        XPathNodeTestKind kind,
        XmlSymbol local,
        XmlSymbol namespaceUri,
        bool anyLocal,
        bool anyNamespace,
        bool requireDocumentElement,
        bool neverMatches
    )
    {
        _kind = kind;
        _local = local;
        _namespace = namespaceUri;
        _anyLocal = anyLocal;
        _anyNamespace = anyNamespace;
        _requireDocumentElement = requireDocumentElement;
        _neverMatches = neverMatches;
    }

    /// <summary>Determines whether a node passes the test.</summary>
    /// <param name="node">The node, from the tree the matcher is bound to.</param>
    /// <returns><see langword="true"/> when the node matches.</returns>
    public bool Matches(in XPathNode node)
    {
        if (_neverMatches)
        {
            return false;
        }

        var kind = node.Kind;
        return _kind switch
        {
            XPathNodeTestKind.AnyNode => true,
            XPathNodeTestKind.Text => kind == XdmNodeKind.Text,
            XPathNodeTestKind.Comment => kind == XdmNodeKind.Comment,
            XPathNodeTestKind.ProcessingInstruction => kind == XdmNodeKind.ProcessingInstruction
                && (_anyLocal || node.Name().Local == _local),
            XPathNodeTestKind.Document => kind == XdmNodeKind.Document
                && (!_requireDocumentElement || HasMatchingDocumentElement(node)),
            XPathNodeTestKind.Element => kind == XdmNodeKind.Element && MatchesName(node),
            XPathNodeTestKind.Attribute => kind == XdmNodeKind.Attribute && MatchesName(node),
            _ => false,
        };
    }

    private bool MatchesName(in XPathNode node)
    {
        var name = node.Name();
        return (_anyLocal || name.Local == _local) && (_anyNamespace || name.NamespaceUri == _namespace);
    }

    // document-node(element(E)): exactly one element child that matches E; comments and processing instructions may accompany it.
    private bool HasMatchingDocumentElement(in XPathNode document)
    {
        var elements = 0;
        var matches = false;
        for (var child = document.FirstChild(); !child.IsEmpty(); child = child.NextSibling())
        {
            switch (child.Kind)
            {
                case XdmNodeKind.Element:
                    elements++;
                    matches = MatchesName(child);
                    break;
                case XdmNodeKind.Text:
                    return false;
                default:
                    break;
            }
        }

        return elements == 1 && matches;
    }
}
