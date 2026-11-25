namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:path</c> (Functions and Operators 3.1, 14.1.5): a path expression that selects the node from the root of its tree, with
/// every element and attribute name in the form <c>Q{uri}local</c>, a position among the siblings of the same kind and name on each
/// step but the attributes, and <c>Q{http://www.w3.org/2005/xpath-functions}root()</c> at the start when the root is not a document
/// node. The empty sequence gives the empty sequence; without an argument the function describes the context item.
/// </summary>
internal sealed class PathFunction : Delta3Function
{
    private const string RootFunction = "Q{http://www.w3.org/2005/xpath-functions}root()";

    /// <summary>Initializes a new instance of the <see cref="PathFunction"/> class.</summary>
    public PathFunction()
        : base("path", 0, 1, [OptionalNode]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (TryGetNode(ref invocation, 0, out var node))
        {
            invocation.Result = XdmSequence.Create(XdmItem.FromString(PathOf(node)));
        }

        return XPathEvaluationStatus.Ok;
    }

    private static string PathOf(XPathNode node)
    {
        var below = new List<XPathNode>();
        var root = node;
        for (var parent = root.Parent(); !parent.IsEmpty(); parent = root.Parent())
        {
            below.Add(root);
            root = parent;
        }

        var isDocument = root.Kind == XdmNodeKind.Document;
        if (below.Count == 0 && isDocument)
        {
            return "/";
        }

        var builder = new StringBuilder(isDocument ? string.Empty : RootFunction);
        for (var i = below.Count - 1; i >= 0; i--)
        {
            _ = builder.Append('/');
            Step(builder, below[i]);
        }

        return builder.ToString();
    }

    private static void Step(StringBuilder builder, XPathNode node)
    {
        var names = node.Tree!.Names;
        var name = node.Name();
        switch (node.Kind)
        {
            case XdmNodeKind.Element:
                AppendName(builder, names.GetString(name.NamespaceUri), names.GetString(name.Local), always: true);
                Position(builder, node);
                break;
            case XdmNodeKind.Attribute:
                _ = builder.Append('@');
                AppendName(builder, names.GetString(name.NamespaceUri), names.GetString(name.Local), always: false);
                break;
            case XdmNodeKind.Text:
                _ = builder.Append("text()");
                Position(builder, node);
                break;
            case XdmNodeKind.Comment:
                _ = builder.Append("comment()");
                Position(builder, node);
                break;
            case XdmNodeKind.ProcessingInstruction:
                _ = builder.Append("processing-instruction(").Append(names.GetString(name.Local)).Append(')');
                Position(builder, node);
                break;
            default:
                var prefix = names.GetString(name.Local);
                _ =
                    prefix.Length == 0
                        ? builder.Append("namespace::*[Q{http://www.w3.org/2005/xpath-functions}local-name()=\"\"]")
                        : builder.Append("namespace::").Append(prefix);
                break;
        }
    }

    // An element always carries the braces; an attribute only when it is in a namespace.
    private static void AppendName(StringBuilder builder, string uri, string local, bool always)
    {
        if (always || uri.Length > 0)
        {
            _ = builder.Append("Q{").Append(uri).Append('}');
        }

        _ = builder.Append(local);
    }

    // The position among the preceding siblings of the same kind, and of the same name for an element or a processing instruction.
    private static void Position(StringBuilder builder, XPathNode node)
    {
        var position = 1;
        var name = node.Name();
        for (var sibling = node.Parent().FirstChild(); !sibling.IsSameNode(in node); sibling = sibling.NextSibling())
        {
            if (
                sibling.Kind == node.Kind
                && (node.Kind is XdmNodeKind.Text or XdmNodeKind.Comment || SameName(sibling.Name(), name, node.Kind))
            )
            {
                position++;
            }
        }

        _ = builder.Append('[').Append(position.ToString(CultureInfo.InvariantCulture)).Append(']');
    }

    private static bool SameName(XmlName left, XmlName right, XdmNodeKind kind) =>
        left.Local == right.Local
        && (kind == XdmNodeKind.ProcessingInstruction || left.NamespaceUri == right.NamespaceUri);
}
