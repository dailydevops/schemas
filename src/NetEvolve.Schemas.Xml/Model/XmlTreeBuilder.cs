namespace NetEvolve.Schemas.Xml.Model;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>
/// An <see cref="IXmlSink"/> that builds an immutable <see cref="XmlDocumentTree"/>; it serves loaded documents, temporary trees
/// and fragments alike.
/// </summary>
/// <remarks>
/// <para>
/// Events arrive in the order of the sink contract. <see cref="StartDocument"/> is optional: without it the tree is a
/// fragment whose first top-level node is the root. Consecutive <see cref="Text"/> events form one text node; empty text
/// creates no node. A <see cref="Doctype"/> event adds no node.
/// </para>
/// <para>
/// Working columns are pooled and kept across <see cref="Complete"/> calls, so one builder can build many trees; only the
/// final arrays of each tree are allocated. A builder is not thread-safe. A name table created by the builder is frozen by
/// <see cref="Complete"/>; a table passed to the constructor is never frozen by the library.
/// </para>
/// <para>
/// Limitation: <see cref="XmlDocumentTree"/> does not materialize the implicit <c>xml</c> namespace node. Limit accounting
/// (node count, text length) is out of scope here and belongs to XML-08.
/// </para>
/// </remarks>
public sealed class XmlTreeBuilder : IXmlAttributeInfoSink, IDisposable
{
    private readonly bool _ownsNames;
    private readonly bool _trackPositions;
    private readonly Uri? _documentUri;
    private readonly PooledBuffer<byte> _kinds = new();
    private readonly PooledBuffer<int> _parents = new();
    private readonly PooledBuffer<XmlName> _names = new();
    private readonly PooledBuffer<int> _textStarts = new();
    private readonly PooledBuffer<int> _otherStarts = new();
    private readonly PooledBuffer<XmlPosition> _positions = new();
    private readonly PooledBuffer<char> _text = new();
    private readonly PooledBuffer<char> _other = new();
    private readonly PooledBuffer<int> _factRows = new();
    private readonly PooledBuffer<byte> _factValues = new();
    private readonly PooledBuffer<NamespaceBinding> _declared = new();
    private readonly PooledBuffer<NamespaceBinding> _scope = new();
    private readonly List<XmlUnparsedEntity> _unparsedEntities = [];
    private readonly List<XmlNotation> _notations = [];
    private int[] _open = new int[16];
    private int _depth;
    private XmlNameTable _nameTable;
    private Dictionary<string, int>? _ids;
    private int _openText = XmlTree.NoNode;
    private int _lastAttribute = XmlTree.NoNode;
    private bool _inTag;

    /// <summary>Initializes a new instance of the <see cref="XmlTreeBuilder"/> class.</summary>
    /// <param name="names">A shared name table, or <see langword="null"/> for one table per tree that the builder owns and freezes.</param>
    /// <param name="documentUri">The document URI of the trees, or <see langword="null"/> for none.</param>
    /// <param name="trackPositions">Whether element start positions are stored.</param>
    public XmlTreeBuilder(XmlNameTable? names = null, Uri? documentUri = null, bool trackPositions = true)
    {
        _ownsNames = names is null;
        _nameTable = names ?? CreateOwnedTable();
        _documentUri = documentUri;
        _trackPositions = trackPositions;
    }

    /// <inheritdoc />
    public XmlNameTable Names => _nameTable;

    /// <summary>Adds an unparsed entity declaration to the tree that <see cref="Complete"/> returns next.</summary>
    /// <param name="entity">The declaration.</param>
    public void AddUnparsedEntity(XmlUnparsedEntity entity)
    {
        Argument.ThrowIfNull(entity);
        _unparsedEntities.Add(entity);
    }

    /// <summary>Adds a notation declaration to the tree that <see cref="Complete"/> returns next.</summary>
    /// <param name="notation">The declaration.</param>
    public void AddNotation(XmlNotation notation)
    {
        Argument.ThrowIfNull(notation);
        _notations.Add(notation);
    }

    /// <summary>
    /// Registers an ID value for the innermost open element; the hook for the packages that evaluate ID declarations.
    /// The first registration of a value wins.
    /// </summary>
    /// <param name="id">The ID value.</param>
    public void RegisterId(string id)
    {
        Argument.ThrowIfNullOrEmpty(id);

        var element = _depth == 0 ? XmlTree.NoNode : _open[_depth - 1];
        if (element == XmlTree.NoNode || (XmlNodeKind)_kinds.WrittenSpan[element] != XmlNodeKind.Element)
        {
            throw new InvalidOperationException("An ID can only be registered while an element is open.");
        }

        _ids ??= new Dictionary<string, int>(StringComparer.Ordinal);
        if (!_ids.TryGetValue(id, out _))
        {
            _ids[id] = element;
        }
    }

    /// <inheritdoc />
    public void StartDocument(in XmlDeclaration declaration)
    {
        if (_kinds.Length != 0)
        {
            throw new InvalidOperationException("The document node must be the first node of a tree.");
        }

        Push(AddRow(XmlNodeKind.Document, XmlTree.NoNode, default, default));
    }

    /// <inheritdoc />
    public void EndDocument()
    {
        FlushTag();
        if (_depth != 1 || (XmlNodeKind)_kinds.WrittenSpan[_open[0]] != XmlNodeKind.Document)
        {
            throw new InvalidOperationException("EndDocument requires an open document node and no open element.");
        }

        _openText = XmlTree.NoNode;
        _depth = 0;
    }

    /// <inheritdoc />
    public void StartElement(XmlName name, XmlPosition position)
    {
        FlushTag();
        var row = AddRow(XmlNodeKind.Element, Parent(), name, position);
        _lastAttribute = XmlTree.NoNode;
        Push(row);
        _declared.Clear();
        _inTag = true;
    }

    /// <inheritdoc />
    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri)
    {
        if (!_inTag)
        {
            throw new InvalidOperationException("Namespace declarations belong to a start tag, before its attributes.");
        }

        _declared.Append(new NamespaceBinding(prefix, uri));
        _lastAttribute = XmlTree.NoNode;
    }

    /// <inheritdoc />
    public void Attribute(XmlName name, ReadOnlySpan<char> value)
    {
        FlushTag();
        if (_depth == 0 || (XmlNodeKind)_kinds.WrittenSpan[_open[_depth - 1]] != XmlNodeKind.Element)
        {
            throw new InvalidOperationException("Attributes belong to an open element.");
        }

        _lastAttribute = AddRow(XmlNodeKind.Attribute, Parent(), name, default);
        _other.Append(value);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The declared type and the specified flag of the attribute that was reported last are kept for the tree; the facts of a
    /// namespace declaration are ignored, because a namespace declaration is no attribute node. An attribute that no
    /// definition covers and that the document specified has no facts to keep.
    /// </remarks>
    public void AttributeInfo(in ValidationAttributeInfo attribute)
    {
        if (_lastAttribute == XmlTree.NoNode || (attribute.DeclaredType is null && attribute.IsSpecified))
        {
            return;
        }

        _factRows.Append(_lastAttribute);
        _factValues.Append(XmlDocumentTree.EncodeFacts(attribute.DeclaredType, attribute.IsSpecified));
    }

    /// <inheritdoc />
    public void EndAttributes() => FlushTag();

    /// <inheritdoc />
    public void EndElement(XmlName name)
    {
        FlushTag();
        _openText = XmlTree.NoNode;
        if (_depth == 0 || (XmlNodeKind)_kinds.WrittenSpan[_open[_depth - 1]] != XmlNodeKind.Element)
        {
            throw new InvalidOperationException("EndElement requires an open element.");
        }

        _depth--;
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind)
    {
        FlushTag();
        if (text.IsEmpty)
        {
            return;
        }

        if (_openText == XmlTree.NoNode)
        {
            _openText = AddRow(XmlNodeKind.Text, Parent(), default, default);
        }

        _text.Append(text);
    }

    /// <inheritdoc />
    public void Comment(ReadOnlySpan<char> text)
    {
        FlushTag();
        _openText = XmlTree.NoNode;
        _ = AddRow(XmlNodeKind.Comment, Parent(), default, default);
        _other.Append(text);
    }

    /// <inheritdoc />
    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data)
    {
        FlushTag();
        _openText = XmlTree.NoNode;
        _ = AddRow(XmlNodeKind.ProcessingInstruction, Parent(), new XmlName(default, target, default), default);
        _other.Append(data);
    }

    /// <inheritdoc />
    /// <remarks>The XDM has no document type node, so the event adds nothing.</remarks>
    public void Doctype(in XmlDoctypeInfo doctype) { }

    /// <summary>Finishes the tree, freezes an owned name table, and resets the builder for the next tree.</summary>
    /// <returns>The immutable tree.</returns>
    /// <exception cref="InvalidOperationException">No node was built, or an element is still open.</exception>
    /// <remarks>A failed call does not reset the builder; discard it or finish the open nodes and call again.</remarks>
    public XmlDocumentTree Complete()
    {
        FlushTag();
        var count = _kinds.Length;
        if (count == 0)
        {
            throw new InvalidOperationException("The tree has no nodes.");
        }

        if (_depth > 1 || (_depth == 1 && (XmlNodeKind)_kinds.WrittenSpan[_open[0]] != XmlNodeKind.Document))
        {
            throw new InvalidOperationException("The tree has an open element.");
        }

        var kinds = _kinds.WrittenSpan.ToArray();
        var parents = _parents.WrittenSpan.ToArray();
        var (ends, firstChildren, firstAttributes) = Link(kinds, parents);

        var tree = new XmlDocumentTree(
            _nameTable,
            kinds,
            parents,
            ends,
            firstChildren,
            firstAttributes,
            _names.WrittenSpan.ToArray(),
            WithSentinel(_textStarts, _text.Length),
            WithSentinel(_otherStarts, _other.Length),
            _text.WrittenSpan.ToArray(),
            _other.WrittenSpan.ToArray(),
            _trackPositions ? _positions.WrittenSpan.ToArray() : null,
            _documentUri,
            _ids,
            Facts(count),
            [.. _unparsedEntities],
            [.. _notations]
        );

        if (_ownsNames)
        {
            _nameTable.Freeze();
            _nameTable = CreateOwnedTable();
        }

        Reset();
        return tree;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _kinds.Dispose();
        _parents.Dispose();
        _names.Dispose();
        _textStarts.Dispose();
        _otherStarts.Dispose();
        _positions.Dispose();
        _text.Dispose();
        _other.Dispose();
        _factRows.Dispose();
        _factValues.Dispose();
        _declared.Dispose();
        _scope.Dispose();
    }

    // Derives the subtree ends and the first child and attribute rows; rows follow document order, so every descendant
    // has a larger index than its ancestors.
    private static (int[] Ends, int[] FirstChildren, int[] FirstAttributes) Link(byte[] kinds, int[] parents)
    {
        var count = kinds.Length;
        var ends = new int[count];
        var firstChildren = new int[count];
        var firstAttributes = new int[count];
        for (var i = 0; i < count; i++)
        {
            ends[i] = i + 1;
            firstChildren[i] = XmlTree.NoNode;
            firstAttributes[i] = XmlTree.NoNode;
        }

        // Rows follow document order, so every descendant has a larger index than its ancestors.
        for (var i = count - 1; i >= 0; i--)
        {
            var parent = parents[i];
            if (parent >= 0 && ends[i] > ends[parent])
            {
                ends[parent] = ends[i];
            }
        }

        for (var i = 0; i < count; i++)
        {
            var parent = parents[i];
            if (parent < 0)
            {
                continue;
            }

            switch ((XmlNodeKind)kinds[i])
            {
                case XmlNodeKind.Namespace:
                    break;
                case XmlNodeKind.Attribute:
                    if (firstAttributes[parent] == XmlTree.NoNode)
                    {
                        firstAttributes[parent] = i;
                    }

                    break;
                default:
                    if (firstChildren[parent] == XmlTree.NoNode)
                    {
                        firstChildren[parent] = i;
                    }

                    break;
            }
        }

        return (ends, firstChildren, firstAttributes);
    }

    // One byte per row, written for the attribute rows that carry facts; null when no row does.
    private byte[]? Facts(int count)
    {
        if (_factRows.Length == 0)
        {
            return null;
        }

        var facts = new byte[count];
        var rows = _factRows.WrittenSpan;
        var values = _factValues.WrittenSpan;
        for (var i = 0; i < rows.Length; i++)
        {
            facts[rows[i]] = values[i];
        }

        return facts;
    }

    private static SpanInterningNameTable CreateOwnedTable() =>
        new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames);

    private static int[] WithSentinel(PooledBuffer<int> starts, int total)
    {
        var span = starts.WrittenSpan;
        var result = new int[span.Length + 1];
        span.CopyTo(result);
        result[span.Length] = total;
        return result;
    }

    private int Parent() => _depth == 0 ? XmlTree.NoNode : _open[_depth - 1];

    private void Push(int row)
    {
        if (_depth == _open.Length)
        {
            Array.Resize(ref _open, _depth * 2);
        }

        _open[_depth++] = row;
    }

    private int AddRow(XmlNodeKind kind, int parent, XmlName name, XmlPosition position)
    {
        if (kind != XmlNodeKind.Text)
        {
            _openText = XmlTree.NoNode;
        }

        var index = _kinds.Length;
        _kinds.Append((byte)kind);
        _parents.Append(parent);
        _names.Append(name);
        _textStarts.Append(_text.Length);
        _otherStarts.Append(_other.Length);
        if (_trackPositions)
        {
            _positions.Append(position);
        }

        return index;
    }

    // Materializes the in-scope namespace nodes of the element whose start tag is open, once, before its first attribute.
    private void FlushTag()
    {
        if (!_inTag)
        {
            return;
        }

        _inTag = false;
        var element = _open[_depth - 1];
        var parent = _parents.WrittenSpan[element];
        var declared = _declared.WrittenSpan;

        _scope.Clear();
        if (parent >= 0 && (XmlNodeKind)_kinds.WrittenSpan[parent] == XmlNodeKind.Element)
        {
            var kinds = _kinds.WrittenSpan;
            var parents = _parents.WrittenSpan;
            var names = _names.WrittenSpan;
            for (
                var row = parent + 1;
                row < kinds.Length && (XmlNodeKind)kinds[row] == XmlNodeKind.Namespace && parents[row] == parent;
                row++
            )
            {
                var inherited = new NamespaceBinding(names[row].Local, names[row].NamespaceUri);
                if (!IsRedeclared(declared, inherited.Prefix))
                {
                    _scope.Append(inherited);
                }
            }
        }

        for (var i = 0; i < declared.Length; i++)
        {
            // An empty URI undeclares a prefix (Namespaces in XML 1.0 section 6.2); it creates no namespace node.
            if (!declared[i].Uri.IsEmpty)
            {
                _scope.Append(declared[i]);
            }
        }

        var scope = _scope.WrittenSpan;
        for (var i = 0; i < scope.Length; i++)
        {
            _ = AddRow(XmlNodeKind.Namespace, element, new XmlName(default, scope[i].Prefix, scope[i].Uri), default);
        }
    }

    private static bool IsRedeclared(ReadOnlySpan<NamespaceBinding> declared, XmlSymbol prefix)
    {
        for (var i = 0; i < declared.Length; i++)
        {
            if (declared[i].Prefix == prefix)
            {
                return true;
            }
        }

        return false;
    }

    private void Reset()
    {
        _kinds.Clear();
        _parents.Clear();
        _names.Clear();
        _textStarts.Clear();
        _otherStarts.Clear();
        _positions.Clear();
        _text.Clear();
        _other.Clear();
        _factRows.Clear();
        _factValues.Clear();
        _lastAttribute = XmlTree.NoNode;
        _depth = 0;
        _declared.Clear();
        _scope.Clear();
        _unparsedEntities.Clear();
        _notations.Clear();
        _ids = null;
        _openText = XmlTree.NoNode;
        _inTag = false;
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct NamespaceBinding(XmlSymbol prefix, XmlSymbol uri)
    {
        public XmlSymbol Prefix { get; } = prefix;

        public XmlSymbol Uri { get; } = uri;
    }
}
