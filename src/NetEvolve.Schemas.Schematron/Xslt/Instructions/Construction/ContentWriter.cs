namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The sink that receives the content of one element or document under construction (XSLT 2.0 section 5.7.1). It is a
/// <see cref="SequenceSinkKind.Tree"/> sink over the events of the tree that is being built: text, comments, processing instructions,
/// and nodes pass through at once, nodes are copied by the Xml pump, and adjacent atomic values are separated by one space.
/// </summary>
/// <remarks>
/// <para>
/// The Xml tree builder takes the namespace declarations of an element before its attributes and cannot add an attribute after a child,
/// while XSLT delivers namespace and attribute nodes in any order before the first child. The writer therefore holds the start tag of
/// an element back until the first child or the end: it collects the namespace nodes and the attributes (a later attribute of the same
/// name replaces an earlier one), runs the namespace fixup of section 5.7.3, and only then writes the start tag. A namespace or
/// attribute node after the first child is the error XTDE0410, in the content of a document XTDE0420. The first problem is kept in
/// <see cref="Problem"/> and the instruction that wrote last reports it with its own location.
/// </para>
/// <para>
/// Namespace fixup: an element or attribute name whose namespace has no binding of its prefix gets a declaration; a prefix that is
/// bound to another namespace by a namespace node of the same element, or by any ancestor for an attribute, is replaced by the prefix
/// that already stands for the namespace, or else by a generated prefix <c>ns1</c>, <c>ns2</c>, and so on, the first that is not in
/// scope. An element that has no namespace under a default namespace undeclares it.
/// </para>
/// </remarks>
internal sealed class ContentWriter : ISequenceSink
{
    internal const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    internal const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    private readonly ISequenceSink _owner;
    private readonly IXmlSink _target;
    private readonly NamespaceScope _inherited;
    private readonly bool _isElement;
    private readonly string _prefix;
    private readonly string _local;
    private readonly string _uri;
    private readonly List<NamespaceBinding> _declared = [];
    private readonly List<PendingAttribute> _attributes = [];
    private NamespaceScope _scope;
    private XmlName _name;
    private bool _started;
    private bool _startFailed;
    private bool _open;
    private bool _lastAtomic;

    private ContentWriter(
        ISequenceSink owner,
        IXmlSink target,
        NamespaceScope inherited,
        bool isElement,
        string prefix,
        string local,
        string uri
    )
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(inherited);
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(uri);

        _owner = owner;
        _target = target;
        _inherited = inherited;
        _scope = inherited;
        _isElement = isElement;
        _prefix = prefix;
        _local = local;
        _uri = uri;
    }

    /// <inheritdoc />
    public SequenceSinkKind Kind => SequenceSinkKind.Tree;

    /// <summary>Gets the first problem of a push, or <see langword="null"/>; it has no location, the instruction that reports it adds its own.</summary>
    internal SchematronProblem? Problem { get; private set; }

    /// <summary>Gets the namespace bindings that the children of the element inherit; valid once the start tag is written, which <see cref="Content"/> does.</summary>
    internal NamespaceScope ChildScope => _scope;

    // The writer of the content may emit a node, so the start tag is written and the adjacency of atomic values restarts.
    /// <inheritdoc />
    public IXmlSink Content
    {
        get
        {
            _ = Start();
            _lastAtomic = false;
            return _target;
        }
    }

    /// <summary>Creates the writer of the content of a document node.</summary>
    /// <param name="owner">The sink that the content is built for; it builds the nested trees.</param>
    /// <param name="target">The events of the document under construction.</param>
    /// <returns>The writer.</returns>
    internal static ContentWriter ForDocument(ISequenceSink owner, IXmlSink target) =>
        new(owner, target, NamespaceScope.Empty, false, string.Empty, string.Empty, string.Empty);

    /// <summary>Creates the writer of the content of an element; the start tag is written with the first child or <see cref="Finish"/>.</summary>
    /// <param name="owner">The sink that the element is built for; it builds the nested trees.</param>
    /// <param name="target">The events of the tree under construction.</param>
    /// <param name="inherited">The namespace bindings of the parent element.</param>
    /// <param name="prefix">The prefix of the element name, as requested.</param>
    /// <param name="local">The local name.</param>
    /// <param name="uri">The namespace URI of the element; empty for none.</param>
    /// <returns>The writer.</returns>
    internal static ContentWriter ForElement(
        ISequenceSink owner,
        IXmlSink target,
        NamespaceScope inherited,
        string prefix,
        string local,
        string uri
    ) => new(owner, target, inherited, true, prefix, local, uri);

    /// <summary>Adds a namespace node to the element.</summary>
    /// <param name="prefix">The prefix; empty for the default namespace.</param>
    /// <param name="uri">The namespace URI.</param>
    /// <returns><see langword="false"/> when the node is not allowed; <see cref="Problem"/> has the error.</returns>
    internal bool TryAddNamespace(string prefix, string uri)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(uri);

        if (!_isElement)
        {
            return Fail("A namespace node cannot be content of a document node.", "XTDE0420");
        }

        if (_started)
        {
            return Fail("A namespace node cannot be added after a child node of the element.", "XTDE0410");
        }

        if (prefix.Length == 0 && uri.Length > 0 && _uri.Length == 0)
        {
            return Fail("A default namespace cannot be declared on an element in no namespace.", "XTDE0440");
        }

        if (string.Equals(prefix, "xml", StringComparison.Ordinal))
        {
            return true;
        }

        for (var i = 0; i < _declared.Count; i++)
        {
            if (string.Equals(_declared[i].Prefix, prefix, StringComparison.Ordinal))
            {
                return string.Equals(_declared[i].Uri, uri, StringComparison.Ordinal)
                    || Fail($"The prefix '{prefix}' is bound to two different namespaces.", "XTDE0430");
            }
        }

        _declared.Add(new NamespaceBinding(prefix, uri));
        return true;
    }

    /// <summary>Adds an attribute to the element; an attribute of the same expanded name that came earlier is discarded.</summary>
    /// <param name="prefix">The prefix as requested.</param>
    /// <param name="local">The local name.</param>
    /// <param name="uri">The namespace URI; empty for none.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="false"/> when the node is not allowed; <see cref="Problem"/> has the error.</returns>
    internal bool TryAddAttribute(string prefix, string local, string uri, string value)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(value);

        if (!_isElement)
        {
            return Fail("An attribute node cannot be content of a document node.", "XTDE0420");
        }

        if (_started)
        {
            return Fail("An attribute node cannot be added after a child node of the element.", "XTDE0410");
        }

        for (var i = 0; i < _attributes.Count; i++)
        {
            if (
                string.Equals(_attributes[i].Local, local, StringComparison.Ordinal)
                && string.Equals(_attributes[i].Uri, uri, StringComparison.Ordinal)
            )
            {
                _attributes.RemoveAt(i);
                break;
            }
        }

        _attributes.Add(new PendingAttribute(prefix, local, uri, value));
        return true;
    }

    /// <summary>Writes the start tag if it is not written yet, then the end of the element.</summary>
    internal void Finish()
    {
        _ = Start();
        if (_open)
        {
            _open = false;
            _target.EndElement(_name);
        }
    }

    /// <inheritdoc />
    public void Item(in XdmItem item)
    {
        Debug.Assert(item.Kind != XdmItemKind.None, "An item must have a value.");

        if (item.TryGetNode(out var node))
        {
            _lastAtomic = false;
            AddNode(node);
            return;
        }

        if (!AtomicText.TryGet(in item, out var text))
        {
            _ = Fail("The atomic value cannot be converted to text yet (limitation).", null);
            return;
        }

        if (_lastAtomic)
        {
            Write(" ".AsSpan());
        }

        Write(text.AsSpan());
        _lastAtomic = true;
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text)
    {
        Write(text);
        _lastAtomic = false;
    }

    /// <inheritdoc />
    public IXmlSink BeginTree() => _owner.BeginTree();

    /// <inheritdoc />
    public XdmItem EndTree(IXmlSink tree) => _owner.EndTree(tree);

    // A zero-length text node is no child, so it neither writes the start tag nor ends the attributes.
    private void Write(ReadOnlySpan<char> text)
    {
        if (!text.IsEmpty && Start())
        {
            _target.Text(text, XmlTextKind.Text);
        }
    }

    private void AddNode(XPathNode node)
    {
        var xml = node.Node;
        var names = node.Tree!.Names;
        switch (xml.Kind)
        {
            case XmlNodeKind.Attribute:
                var name = xml.Name;
                _ = TryAddAttribute(
                    names.GetString(name.Prefix),
                    names.GetString(name.Local),
                    names.GetString(name.NamespaceUri),
                    xml.StringValue
                );
                break;
            case XmlNodeKind.Namespace:
                _ = TryAddNamespace(names.GetString(xml.Name.Local), xml.StringValue);
                break;
            default:
                if (Start())
                {
                    ConstructionOutput.Pump(xml, _target);
                }

                break;
        }
    }

    private bool Fail(string message, string? constraint)
    {
        Problem ??= new SchematronProblem(SchematronDiagnosticCodes.EvaluationFailed, message, default, constraint);
        return false;
    }

    private bool Start()
    {
        if (_started)
        {
            return !_startFailed;
        }

        _started = true;
        if (!_isElement)
        {
            return true;
        }

        var elementPrefix = ResolveElementPrefix();
        var attributePrefixes = new string[_attributes.Count];
        for (var i = 0; i < attributePrefixes.Length; i++)
        {
            var attribute = _attributes[i];
            attributePrefixes[i] =
                attribute.Uri.Length == 0 ? string.Empty : UsePrefix(attribute.Prefix, attribute.Uri, false);
        }

        _scope = _inherited.With(_declared);
        _startFailed = !TryName(elementPrefix, _local, _uri, out _name);
        if (_startFailed)
        {
            return false;
        }

        _target.StartElement(_name, default);
        _open = true;
        _startFailed = !WriteTag(attributePrefixes);
        return !_startFailed;
    }

    private bool WriteTag(string[] attributePrefixes)
    {
        foreach (var binding in _declared)
        {
            // A declaration that repeats a binding of the parent adds no namespace node.
            if (string.Equals(_inherited.Lookup(binding.Prefix) ?? string.Empty, binding.Uri, StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryIntern(binding.Prefix, out var prefix) || !TryIntern(binding.Uri, out var uri))
            {
                return false;
            }

            _target.NamespaceDeclaration(prefix, uri);
        }

        for (var i = 0; i < _attributes.Count; i++)
        {
            var attribute = _attributes[i];
            if (!TryName(attributePrefixes[i], attribute.Local, attribute.Uri, out var name))
            {
                return false;
            }

            _target.Attribute(name, attribute.Value.AsSpan());
        }

        _target.EndAttributes();
        return true;
    }

    private string ResolveElementPrefix()
    {
        if (_uri.Length > 0)
        {
            return UsePrefix(_prefix, _uri, true);
        }

        // An element in no namespace under a default namespace undeclares it; a default namespace of its own was refused earlier.
        if (!string.IsNullOrEmpty(Lookup(string.Empty, out _)))
        {
            _declared.Add(new NamespaceBinding(string.Empty, string.Empty));
        }

        return string.Empty;
    }

    // The prefix of a name in a namespace, with the declaration that it needs. The element may take over a prefix that only an
    // ancestor binds; an attribute cannot, because the names resolved before it could depend on that binding.
    private string UsePrefix(string wanted, string uri, bool isElement)
    {
        if (string.Equals(uri, XmlNamespace, StringComparison.Ordinal))
        {
            return "xml";
        }

        var reserved =
            string.Equals(wanted, "xml", StringComparison.Ordinal)
            || string.Equals(wanted, "xmlns", StringComparison.Ordinal);
        if (!reserved && (wanted.Length > 0 || isElement))
        {
            var bound = Lookup(wanted, out var declaredHere);
            if (string.Equals(bound, uri, StringComparison.Ordinal))
            {
                return wanted;
            }

            if (string.IsNullOrEmpty(bound) || (isElement && !declaredHere))
            {
                _declared.Add(new NamespaceBinding(wanted, uri));
                return wanted;
            }
        }

        var found = FindPrefix(uri);
        if (found is not null)
        {
            return found;
        }

        for (var n = 1; ; n++)
        {
            var generated = "ns" + n.ToString(CultureInfo.InvariantCulture);
            if (Lookup(generated, out _) is null)
            {
                _declared.Add(new NamespaceBinding(generated, uri));
                return generated;
            }
        }
    }

    // A prefix that already stands for the namespace on this element, and is not hidden by a later declaration.
    private string? FindPrefix(string uri)
    {
        for (var i = _declared.Count - 1; i >= 0; i--)
        {
            var binding = _declared[i];
            if (
                binding.Prefix.Length > 0
                && string.Equals(binding.Uri, uri, StringComparison.Ordinal)
                && string.Equals(Lookup(binding.Prefix, out _), uri, StringComparison.Ordinal)
            )
            {
                return binding.Prefix;
            }
        }

        for (var i = 0; i < _inherited.Count; i++)
        {
            var binding = _inherited[i];
            if (
                binding.Prefix.Length > 0
                && string.Equals(binding.Uri, uri, StringComparison.Ordinal)
                && string.Equals(Lookup(binding.Prefix, out _), uri, StringComparison.Ordinal)
            )
            {
                return binding.Prefix;
            }
        }

        return null;
    }

    private string? Lookup(string prefix, out bool declaredHere)
    {
        declaredHere = false;
        for (var i = _declared.Count - 1; i >= 0; i--)
        {
            if (string.Equals(_declared[i].Prefix, prefix, StringComparison.Ordinal))
            {
                declaredHere = true;
                return _declared[i].Uri;
            }
        }

        return _inherited.Lookup(prefix);
    }

    private bool TryName(string prefix, string local, string uri, out XmlName name)
    {
        name = default;
        if (
            !TryIntern(prefix, out var prefixSymbol)
            || !TryIntern(local, out var localSymbol)
            || !TryIntern(uri, out var uriSymbol)
        )
        {
            return false;
        }

        name = new XmlName(prefixSymbol, localSymbol, uriSymbol);
        return true;
    }

    private bool TryIntern(string text, out XmlSymbol symbol)
    {
        if (ConstructionOutput.TryIntern(_target.Names, text, out symbol, out var problem))
        {
            return true;
        }

        Problem ??= problem;
        return false;
    }
}
