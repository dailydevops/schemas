namespace NetEvolve.Schemas.Xsd.Validation.Tree;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;

/// <summary>
/// The sink of a tree run (XSD-79, #361): it forwards every event of the model-to-sink pump to the sink of the validator run and to a builder
/// that copies the pumped content into the annotated tree, and it logs the element at which each new issue was reported, so that an issue can
/// be mapped to its node afterwards. The copy also holds what the validator adds from the schema: the defaulted attributes after the specified
/// ones of their element, and the default or fixed value of an empty element as its text (design #146 DP-19, option A).
/// </summary>
/// <remarks>
/// <para>
/// The ordinal of an element is the number of its start event, counted from one, which is how the validator numbers the elements for its
/// annotations; ordinal zero is the document. An issue belongs to the innermost element that is open when the event that raised it is done,
/// which for an end tag is the element that ends, so incomplete content belongs to the element that is incomplete.
/// </para>
/// <para>
/// Limitations: the pump emits no document type declaration, so the copy has none. An issue of an attribute belongs to the element that owns
/// the attribute. Not thread-safe; one instance serves one run.
/// </para>
/// </remarks>
internal sealed class TreeRunSink : IXmlSink
{
    private readonly IXmlSink _inner;
    private readonly XmlTreeBuilder _copy;
    private readonly ValidationIssueCollector _collector;
    private readonly TreeIssueLog _log;
    private readonly TreeDefaults _defaults;
    private int[] _open = new int[16];
    private int _depth;
    private int _ordinal;
    private int _logged;

    /// <summary>Initializes a new instance of the <see cref="TreeRunSink"/> class.</summary>
    /// <param name="inner">The sink of the validator run.</param>
    /// <param name="copy">The builder of the annotated tree; it must share the name table of <paramref name="inner"/>.</param>
    /// <param name="collector">The collector that the validator run reports to.</param>
    /// <param name="log">The log that receives the element ordinal of every issue.</param>
    /// <param name="defaults">The sink that the annotation listener of the validator run reports to, which keeps the defaults for the copy.</param>
    internal TreeRunSink(
        IXmlSink inner,
        XmlTreeBuilder copy,
        ValidationIssueCollector collector,
        TreeIssueLog log,
        TreeDefaults defaults
    )
    {
        Argument.ThrowIfNull(inner);
        Argument.ThrowIfNull(copy);
        Argument.ThrowIfNull(collector);
        Argument.ThrowIfNull(log);
        Argument.ThrowIfNull(defaults);

        _inner = inner;
        _copy = copy;
        _collector = collector;
        _log = log;
        _defaults = defaults;
    }

    /// <inheritdoc />
    public XmlNameTable Names => _inner.Names;

    /// <inheritdoc />
    public void StartDocument(in XmlDeclaration declaration)
    {
        _inner.StartDocument(in declaration);
        _copy.StartDocument(in declaration);
        Observe();
    }

    /// <inheritdoc />
    public void EndDocument()
    {
        _inner.EndDocument();
        _copy.EndDocument();
        Observe();
    }

    /// <inheritdoc />
    public void StartElement(XmlName name, XmlPosition position)
    {
        if (_depth == _open.Length)
        {
            Array.Resize(ref _open, _depth * 2);
        }

        _open[_depth++] = ++_ordinal;
        _inner.StartElement(name, position);
        _copy.StartElement(name, position);
        Observe();
    }

    /// <inheritdoc />
    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri)
    {
        _inner.NamespaceDeclaration(prefix, uri);
        _copy.NamespaceDeclaration(prefix, uri);
        Observe();
    }

    /// <inheritdoc />
    public void Attribute(XmlName name, ReadOnlySpan<char> value)
    {
        _inner.Attribute(name, value);
        _copy.Attribute(name, value);
        Observe();
    }

    /// <inheritdoc />
    public void EndAttributes()
    {
        _inner.EndAttributes();

        // The validator adds the defaulted attributes while it ends the start tag; they follow the specified ones.
        _defaults.CopyAttributes(_copy);
        _copy.EndAttributes();
        Observe();
    }

    /// <inheritdoc />
    public void EndElement(XmlName name)
    {
        _inner.EndElement(name);

        // An empty element takes the default or fixed value of its declaration while it ends; it becomes the content of the copy.
        var value = _defaults.TakeValue();
        if (value is not null)
        {
            _copy.Text(value.AsSpan(), XmlTextKind.Text);
        }

        _copy.EndElement(name);

        // The issues of an end tag belong to the element that ends; it is popped afterwards.
        Observe();
        _depth--;
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind)
    {
        _inner.Text(text, kind);
        _copy.Text(text, kind);
        Observe();
    }

    /// <inheritdoc />
    public void Comment(ReadOnlySpan<char> text)
    {
        _inner.Comment(text);
        _copy.Comment(text);
        Observe();
    }

    /// <inheritdoc />
    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data)
    {
        _inner.ProcessingInstruction(target, data);
        _copy.ProcessingInstruction(target, data);
        Observe();
    }

    /// <inheritdoc />
    public void Doctype(in XmlDoctypeInfo doctype)
    {
        _inner.Doctype(in doctype);
        _copy.Doctype(in doctype);
        Observe();
    }

    // The issues that the last event added belong to the innermost open element, or to the document when none is open.
    private void Observe()
    {
        var count = _collector.Count;
        if (count == _logged)
        {
            return;
        }

        var ordinal = _depth == 0 ? 0 : _open[_depth - 1];
        while (_logged < count)
        {
            _log.Add(ordinal);
            _logged++;
        }
    }
}
