namespace NetEvolve.Schemas.Schematron.Xslt.Embedding;

using System;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The <see cref="SequenceSinkKind.Tree"/> sink that collects the content of one referenced property (SCH-84, #289): the items that
/// <c>xsl:copy-of</c> pushes and the text of the other parts, in order. They are the content of one holder element, which the content
/// writer of the construction folder builds, so an attribute node becomes an attribute of the holder (and of the <c>svrl:text</c> that
/// takes its place), a namespace node a namespace declaration, and any other node a child. An attribute or a namespace node after a
/// child is the coded error XTDE0410, as in the content of any element (XSLT 2.0 section 5.7.1).
/// </summary>
/// <remarks>
/// Limitation: the content is a copy of the nodes, with the namespace nodes of their source and no type annotations. Not thread-safe.
/// </remarks>
internal sealed class FindingContentBuilder : TemporaryTreeSink, IFindingContentBuilder
{
    private XmlTreeBuilder? _tree;
    private ContentWriter? _writer;

    internal FindingContentBuilder(XmlNameTable names)
        : base(names)
    {
        var tree = new XmlTreeBuilder(names, null, false);
        tree.StartDocument(default);
        _tree = tree;
        _writer = ContentWriter.ForElement(this, tree, NamespaceScope.Empty, string.Empty, "holder", string.Empty);
    }

    /// <inheritdoc />
    public override SequenceSinkKind Kind => SequenceSinkKind.Tree;

    /// <inheritdoc />
    public override IXmlSink Content => Writer.Content;

    private ContentWriter Writer => _writer ?? throw new InvalidOperationException("The content is complete.");

    /// <inheritdoc />
    public override void Item(in XdmItem item) => Writer.Item(in item);

    /// <inheritdoc />
    public override void Text(ReadOnlySpan<char> text) => Writer.Text(text);

    /// <inheritdoc />
    public bool TryComplete(out IFindingContent content, out SchematronProblem problem)
    {
        var writer = Writer;
        writer.Finish();
        var tree = _tree!;
        _writer = null;
        _tree = null;
        if (writer.Problem is { } found)
        {
            tree.Dispose();
            content = null!;
            problem = found;
            return false;
        }

        using (tree)
        {
            tree.EndDocument();
            content = new FindingContent(tree.Complete());
        }

        problem = default;
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _tree?.Dispose();
        _tree = null;
        _writer = null;
    }
}
