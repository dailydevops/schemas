namespace NetEvolve.Schemas.Schematron.Xslt.Construction;

using System;
using System.Diagnostics;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Traversal;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The <see cref="SequenceSinkKind.Tree"/> sink: everything pushed becomes the content of one document node. Nodes are
/// copied through the Xml pump (new identity), adjacent atomic values are separated by one space, and a text call resets
/// that adjacency.
/// </summary>
internal sealed class TreeContentSink : TemporaryTreeSink, IDisposable
{
    private XmlTreeBuilder? _builder;
    private XdmItem _document;
    private bool _lastAtomic;

    internal TreeContentSink(XmlNameTable names)
        : base(names)
    {
        _builder = new XmlTreeBuilder(names, null, false);
        _builder.StartDocument(default);
    }

    public override SequenceSinkKind Kind => SequenceSinkKind.Tree;

    /// <summary>Gets the first problem of a push, or <see langword="null"/>; the push that failed adds nothing.</summary>
    internal SchematronProblem? Problem { get; private set; }

    /// <summary>
    /// Gets the completed document node; reading it ends the construction. The node of a tree built by
    /// <see cref="TemporaryTreeSink.EndTree"/> is a different tree.
    /// </summary>
    internal XdmItem Document
    {
        get
        {
            if (_builder is { } builder)
            {
                builder.EndDocument();
                _document = DocumentItem(builder.Complete());
                builder.Dispose();
                _builder = null;
            }

            return _document;
        }
    }

    // The writer of the content may emit a node, so the adjacency of atomic values restarts.
    public override IXmlSink Content
    {
        get
        {
            _lastAtomic = false;
            return Open();
        }
    }

    public override void Item(in XdmItem item)
    {
        Debug.Assert(item.Kind != XdmItemKind.None, "An item must have a value.");

        var builder = Open();
        if (item.TryGetNode(out var node))
        {
            _lastAtomic = false;
            Copy(node.Node, builder);
            return;
        }

        if (!AtomicText.TryGet(in item, out var text))
        {
            Fail("The atomic value cannot be converted to text yet (limitation).", null);
            return;
        }

        if (_lastAtomic)
        {
            builder.Text(" ".AsSpan(), XmlTextKind.Text);
        }

        builder.Text(text.AsSpan(), XmlTextKind.Text);
        _lastAtomic = true;
    }

    public override void Text(ReadOnlySpan<char> text)
    {
        Open().Text(text, XmlTextKind.Text);
        _lastAtomic = false;
    }

    public void Dispose()
    {
        _builder?.Dispose();
        _builder = null;
    }

    private XmlTreeBuilder Open() => _builder ?? throw new InvalidOperationException("The tree is complete.");

    private void Copy(XmlNode node, XmlTreeBuilder builder)
    {
        switch (node.Kind)
        {
            case XmlNodeKind.Document:
                for (var child = node.FirstChild; !child.IsEmpty; child = child.NextSibling)
                {
                    XmlTreePump.Pump(child, builder);
                }

                break;
            case XmlNodeKind.Attribute:
            case XmlNodeKind.Namespace:
                // XSLT 2.0 section 5.7.1: an attribute or namespace node cannot be content of a document node.
                Fail("An attribute or namespace node cannot be content of a document node.", "XTDE0420");
                break;
            default:
                XmlTreePump.Pump(node, builder);
                break;
        }
    }

    private void Fail(string message, string? constraint) =>
        Problem ??= new SchematronProblem(SchematronDiagnosticCodes.EvaluationFailed, message, default, constraint);
}
