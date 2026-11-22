namespace NetEvolve.Schemas.Schematron.Xslt.Construction;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The shared part of the item and tree sinks: nested temporary trees, built only by the Xml package tree builder on the
/// run-owned, caller-supplied name table (never frozen by the library).
/// </summary>
internal abstract class TemporaryTreeSink : ISequenceSink
{
    private protected TemporaryTreeSink(XmlNameTable names)
    {
        ArgumentNullException.ThrowIfNull(names);

        Names = names;
    }

    private protected XmlNameTable Names { get; }

    public abstract SequenceSinkKind Kind { get; }

    public abstract IXmlSink Content { get; }

    public abstract void Item(in XdmItem item);

    public abstract void Text(ReadOnlySpan<char> text);

    public IXmlSink BeginTree()
    {
        var builder = new XmlTreeBuilder(Names, null, false);
        builder.StartDocument(default);
        return builder;
    }

    public XdmItem EndTree(IXmlSink tree)
    {
        var builder = tree as XmlTreeBuilder;
        ArgumentNullException.ThrowIfNull(builder);

        using (builder)
        {
            builder.EndDocument();
            return DocumentItem(builder.Complete());
        }
    }

    private protected static XdmItem DocumentItem(XmlDocumentTree tree) =>
        XdmItem.FromNode(new XPathNode(new XmlNode(tree, 0)));
}
