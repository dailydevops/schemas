namespace NetEvolve.Schemas.Schematron.Xslt.Embedding;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Traversal;

/// <summary>
/// The owned snapshot of the content of one referenced property: a tree whose first element holds the copied attributes, the namespace
/// declarations, and the children. The tree is never changed after it is built, so the report writer may keep it as long as it needs it.
/// </summary>
internal sealed class FindingContent : IFindingContent
{
    private readonly XmlNode _holder;

    internal FindingContent(XmlDocumentTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);

        _holder = tree.Document.FirstChild;
    }

    /// <inheritdoc />
    public void WriteAttributes(IXmlSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        var names = _holder.Tree!.Names;
        for (var ns = _holder.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
        {
            sink.NamespaceDeclaration(
                sink.Names.Intern(names.GetString(ns.Name.Local)),
                sink.Names.Intern(ns.StringValue)
            );
        }

        for (var attribute = _holder.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
        {
            var name = attribute.Name;
            sink.Attribute(
                sink.Names.Intern(
                    names.GetString(name.Prefix),
                    names.GetString(name.Local),
                    names.GetString(name.NamespaceUri)
                ),
                attribute.StringValue
            );
        }
    }

    /// <inheritdoc />
    public void WriteChildren(IXmlSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        for (var child = _holder.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            XmlTreePump.Pump(child, sink);
        }
    }
}
