namespace NetEvolve.Schemas.Xml.Model;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>
/// The immutable struct-of-arrays arena that backs every tree built by <see cref="XmlTreeBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// Rows are stored in document order: an element row is followed by its namespace rows, its attribute rows and then its
/// descendants, so the row index is the document-order key and a subtree is the half-open range
/// <c>[index, end)</c>. A tree built without a document node (a fragment) starts with its first top-level node at row 0;
/// <see cref="XmlTree.Document"/> then returns that node.
/// </para>
/// <para>
/// Every array is written once by the builder and never changed afterwards, so any number of threads can read one
/// tree at the same time without synchronization.
/// The two name indexes (elements and attributes by local name) are the only state built later: the first walks that ask for
/// one read the rows, a later walk builds it from the arrays and publishes it with <see cref="Interlocked"/>. Racing threads may
/// each build one, one wins, and the loser's copy is dropped; a published index never changes.
/// </para>
/// <para>
/// Limitation: the implicit <c>xml</c> namespace node of XDM is not materialized; only declared bindings are in scope.
/// </para>
/// </remarks>
public sealed class XmlDocumentTree : XmlTree
{
    private const string XmlNamespaceUri = "http://www.w3.org/XML/1998/namespace";
    private const int NotSpecified = 0x80;

    // The walks that ask for the elements of one name before the index is built; the build costs about one walk of the whole tree.
    private const int IndexAfterRequests = 2;

    private readonly byte[] _kinds;
    private readonly int[] _parents;
    private readonly int[] _ends;
    private readonly int[] _firstChildren;
    private readonly int[] _firstAttributes;
    private readonly XmlName[] _names;
    private readonly int[] _textStarts; // length + 1; text-buffer offset when the row was appended
    private readonly int[] _otherStarts; // length + 1; value-buffer offset when the row was appended
    private readonly char[] _text;
    private readonly char[] _other;
    private readonly XmlPosition[]? _positions; // one entry per element row, in row order
    private readonly int[]? _positionRows; // the element rows of _positions, ascending
    private readonly Uri? _documentUri;
    private readonly Dictionary<string, int>? _ids;
    private readonly byte[]? _facts;
    private readonly XmlNameTable _nameTable;
    private readonly IReadOnlyList<XmlUnparsedEntity> _unparsedEntities;
    private readonly IReadOnlyList<XmlNotation> _notations;
    private readonly bool _hasXmlBase;
    private readonly XmlSymbol _xmlBaseLocal;
    private readonly XmlSymbol _xmlNamespace;

    // Published once with Interlocked; the arrays they read never change.
    private XmlNameIndex? _elements;
    private XmlNameIndex? _attributes;
    private int _elementRequests;
    private int _attributeRequests;

    internal XmlDocumentTree(
        XmlNameTable names,
        byte[] kinds,
        int[] parents,
        int[] ends,
        int[] firstChildren,
        int[] firstAttributes,
        XmlName[] nodeNames,
        int[] textStarts,
        int[] otherStarts,
        char[] text,
        char[] other,
        XmlPosition[]? positions,
        int[]? positionRows,
        Uri? documentUri,
        Dictionary<string, int>? ids,
        byte[]? facts,
        XmlUnparsedEntity[] unparsedEntities,
        XmlNotation[] notations
    )
    {
        _nameTable = names;
        _kinds = kinds;
        _parents = parents;
        _ends = ends;
        _firstChildren = firstChildren;
        _firstAttributes = firstAttributes;
        _names = nodeNames;
        _textStarts = textStarts;
        _otherStarts = otherStarts;
        _text = text;
        _other = other;
        _positions = positions;
        _positionRows = positionRows;
        _documentUri = documentUri;
        _ids = ids;
        _facts = facts;
        _unparsedEntities = Array.AsReadOnly(unparsedEntities);
        _notations = Array.AsReadOnly(notations);
        _hasXmlBase =
            names.TryGet("base".AsSpan(), out _xmlBaseLocal)
            && names.TryGet(XmlNamespaceUri.AsSpan(), out _xmlNamespace);
    }

    /// <summary>Gets the number of nodes (rows) of this tree, namespace and attribute nodes included.</summary>
    public int Count => _kinds.Length;

    /// <inheritdoc />
    public override XmlNameTable Names => _nameTable;

    /// <inheritdoc />
    public override IReadOnlyList<XmlUnparsedEntity> UnparsedEntities => _unparsedEntities;

    /// <inheritdoc />
    public override IReadOnlyList<XmlNotation> Notations => _notations;

    /// <summary>Gets the source position recorded for a node.</summary>
    /// <param name="index">The node index, see <see cref="XmlNode.Index"/>.</param>
    /// <returns>The start position of an element; <see langword="default"/> for other kinds and for trees built without positions.</returns>
    public XmlPosition GetPosition(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _kinds.Length);

        if (_positions is null || _positionRows is null)
        {
            return default;
        }

        var slot = Array.BinarySearch(_positionRows, index);
        return slot < 0 ? default : _positions[slot];
    }

    /// <summary>Looks up the element registered for an ID value; the first registration in document order wins.</summary>
    /// <param name="id">The ID value.</param>
    /// <param name="element">The element, or an empty node when no element carries the ID.</param>
    /// <returns><see langword="true"/> when an element was found.</returns>
    public bool TryGetElementById(string id, out XmlNode element)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (_ids is not null && _ids.TryGetValue(id, out var index))
        {
            element = new XmlNode(this, index);
            return true;
        }

        element = default;
        return false;
    }

    /// <summary>Gets the characters of a text node, or the value of an attribute, comment or processing instruction node, without allocating.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The characters.</returns>
    internal ReadOnlySpan<char> GetValueSpan(int index) =>
        (XmlNodeKind)_kinds[index] == XmlNodeKind.Text
            ? _text.AsSpan(_textStarts[index], _textStarts[index + 1] - _textStarts[index])
            : _other.AsSpan(_otherStarts[index], _otherStarts[index + 1] - _otherStarts[index]);

    /// <summary>Gets the namespace URI symbol bound by a namespace node.</summary>
    /// <param name="index">The index of a namespace node.</param>
    /// <returns>The symbol.</returns>
    internal XmlSymbol GetNamespaceUri(int index) => _names[index].NamespaceUri;

    // The facts of an attribute row in one byte: the declared type plus one in the low seven bits (zero for none), and the
    // high bit for an attribute that the document did not specify.
    internal static byte EncodeFacts(DtdAttributeType? type, bool isSpecified) =>
        (byte)((type is { } declared ? (int)declared + 1 : 0) | (isSpecified ? 0 : NotSpecified));

    /// <inheritdoc />
    protected internal override DtdAttributeType? GetAttributeType(int index)
    {
        var declared = _facts is null ? 0 : _facts[index] & ~NotSpecified;
        return declared == 0 ? null : (DtdAttributeType)(declared - 1);
    }

    /// <inheritdoc />
    protected internal override bool GetIsSpecified(int index) => _facts is null || (_facts[index] & NotSpecified) == 0;

    /// <inheritdoc />
    protected internal override XmlNodeKind GetKind(int index) => (XmlNodeKind)_kinds[index];

    /// <inheritdoc />
    protected internal override XmlName GetName(int index) =>
        (XmlNodeKind)_kinds[index] == XmlNodeKind.Namespace
            ? new XmlName(default, _names[index].Local, default)
            : _names[index];

    /// <inheritdoc />
    protected internal override int GetParent(int index) => _parents[index];

    /// <inheritdoc />
    protected internal override int GetFirstChild(int index) => _firstChildren[index];

    /// <inheritdoc />
    protected internal override int GetNextSibling(int index)
    {
        if ((XmlNodeKind)_kinds[index] is XmlNodeKind.Attribute or XmlNodeKind.Namespace)
        {
            return NoNode;
        }

        var next = _ends[index];
        return next < _kinds.Length && _parents[next] == _parents[index] ? next : NoNode;
    }

    /// <summary>Gets the first row after the subtree of a node.</summary>
    /// <param name="index">The node index.</param>
    /// <returns>The row after the last descendant, attribute, and namespace row of the node.</returns>
    internal int GetEnd(int index) => _ends[index];

    /// <summary>Gets the index of the elements by local name, once enough walks asked for it to pay for building it.</summary>
    /// <returns>The index, or <see langword="null"/> while the walks still read the rows themselves.</returns>
    internal XmlNameIndex? GetElementIndex() => GetIndex(ref _elements, ref _elementRequests, XmlNodeKind.Element);

    /// <summary>Gets the index of the attributes by local name, once enough walks asked for it to pay for building it.</summary>
    /// <returns>The index, or <see langword="null"/> while the walks still read the rows themselves.</returns>
    internal XmlNameIndex? GetAttributeIndex() =>
        GetIndex(ref _attributes, ref _attributeRequests, XmlNodeKind.Attribute);

    private XmlNameIndex? GetIndex(ref XmlNameIndex? field, ref int requests, XmlNodeKind kind)
    {
        var index = Volatile.Read(ref field);
        if (index is not null)
        {
            return index;
        }

        if (Interlocked.Increment(ref requests) <= IndexAfterRequests)
        {
            return null;
        }

        index = XmlNameIndex.Build(_kinds, _names, kind);
        return Interlocked.CompareExchange(ref field, index, null) ?? index;
    }

    /// <inheritdoc />
    protected internal override int GetFirstAttribute(int index) => _firstAttributes[index];

    /// <inheritdoc />
    protected internal override int GetNextAttribute(int index) => Adjacent(index, XmlNodeKind.Attribute);

    /// <inheritdoc />
    protected internal override int GetFirstNamespace(int index) =>
        (XmlNodeKind)_kinds[index] == XmlNodeKind.Element
            ? AdjacentOf(index, index + 1, XmlNodeKind.Namespace)
            : NoNode;

    /// <inheritdoc />
    protected internal override int GetNextNamespace(int index) => Adjacent(index, XmlNodeKind.Namespace);

    /// <inheritdoc />
    protected internal override string GetStringValue(int index)
    {
        switch ((XmlNodeKind)_kinds[index])
        {
            case XmlNodeKind.Document:
            case XmlNodeKind.Element:
                return new string(_text, _textStarts[index], _textStarts[_ends[index]] - _textStarts[index]);
            case XmlNodeKind.Text:
                return new string(_text, _textStarts[index], _textStarts[index + 1] - _textStarts[index]);
            case XmlNodeKind.Namespace:
                return _nameTable.GetString(_names[index].NamespaceUri);
            case XmlNodeKind.Attribute:
            case XmlNodeKind.Comment:
            case XmlNodeKind.ProcessingInstruction:
            default:
                return new string(_other, _otherStarts[index], _otherStarts[index + 1] - _otherStarts[index]);
        }
    }

    /// <inheritdoc />
    protected internal override Uri? GetBaseUri(int index)
    {
        // Namespace, attribute, text, comment and processing instruction nodes take the base URI of their parent.
        List<string>? chain = null;
        for (var current = index; current >= 0; current = _parents[current])
        {
            if ((XmlNodeKind)_kinds[current] != XmlNodeKind.Element)
            {
                continue;
            }

            var value = FindXmlBase(current);
            if (value is null)
            {
                continue;
            }

            (chain ??= []).Add(value);
            if (IsAbsolute(value, out _))
            {
                break;
            }
        }

        var result = _documentUri;
        if (chain is not null)
        {
            for (var i = chain.Count - 1; i >= 0; i--)
            {
                result = Resolve(result, chain[i]);
            }
        }

        return result;
    }

    /// <inheritdoc />
    protected internal override Uri? GetDocumentUri(int index) => _documentUri;

    /// <inheritdoc />
    protected internal override int CompareDocumentOrder(int first, int second) => first.CompareTo(second);

    // A rooted path such as "/dir/" parses as an absolute file URI on Unix but as relative on Windows; treat it as relative everywhere.
    private static bool IsAbsolute(string value, out Uri? uri) =>
        Uri.TryCreate(value, UriKind.Absolute, out uri) && value.Length > 0 && value[0] != '/';

    private static Uri? Resolve(Uri? baseUri, string value)
    {
        if (IsAbsolute(value, out var absolute))
        {
            return absolute;
        }

        return baseUri is not null && baseUri.IsAbsoluteUri && Uri.TryCreate(baseUri, value, out var resolved)
            ? resolved
            : baseUri;
    }

    private int Adjacent(int index, XmlNodeKind kind) =>
        (XmlNodeKind)_kinds[index] == kind ? AdjacentOf(_parents[index], index + 1, kind) : NoNode;

    private int AdjacentOf(int parent, int candidate, XmlNodeKind kind) =>
        candidate < _kinds.Length && (XmlNodeKind)_kinds[candidate] == kind && _parents[candidate] == parent
            ? candidate
            : NoNode;

    private string? FindXmlBase(int element)
    {
        if (!_hasXmlBase)
        {
            return null;
        }

        for (var attribute = _firstAttributes[element]; attribute != NoNode; attribute = GetNextAttribute(attribute))
        {
            var name = _names[attribute];
            if (name.Local == _xmlBaseLocal && name.NamespaceUri == _xmlNamespace)
            {
                return GetStringValue(attribute);
            }
        }

        return null;
    }
}
