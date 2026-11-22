namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>
/// The ID and IDREF values of one tree, found in one pass in document order: for every ID value the first element that has it, and for
/// every IDREF value the attributes that name it, in document order (XPath 2.0 Functions and Operators 1.0, 15.5.2 and 15.5.3; XDM,
/// <c>is-id</c> and <c>is-idrefs</c>). The tree is immutable, so the index is built once and kept with the tree.
/// </summary>
/// <remarks>
/// <para>
/// An attribute is an ID, an IDREF, or an IDREFS when its attribute definition in the document type declaration says so
/// (<see cref="XmlNode.AttributeType"/>); a tree without such declarations has none, and so the functions give the empty sequence.
/// </para>
/// <para>
/// Limitations: the ID typing that <c>xml:id</c> gives is DEF-67 (#79), and the one that a schema gives needs schema-aware processing, so
/// no element is an ID or IDREF by its own typed value. A value is matched as written: the data model requires an <c>xs:NCName</c>, but
/// a DTD ID is a <c>Name</c>, so a value with a colon matches too. Every tree has a document node as its root, so FODC0001 is never raised.
/// The index of a tree is kept as long as the tree lives and is built on the first call, which reads every node once.
/// </para>
/// </remarks>
internal sealed class IdIndex
{
    private static readonly ConditionalWeakTable<XmlTree, IdIndex> Indexes = new();

    private readonly Dictionary<string, XmlNode> _elements = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<XmlNode>> _references = new(StringComparer.Ordinal);

    private IdIndex() { }

    /// <summary>Gets the index of a tree, building it on first use.</summary>
    /// <param name="tree">The tree.</param>
    /// <returns>The index.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tree"/> is <see langword="null"/>.</exception>
    internal static IdIndex For(XmlTree tree)
    {
        Argument.ThrowIfNull(tree);

        return Indexes.GetValue(tree, static source => Build(source));
    }

    /// <summary>Reads the next token of a string that is separated by white space.</summary>
    /// <param name="text">The string.</param>
    /// <param name="position">The position to read from; moved behind the token.</param>
    /// <param name="token">The token when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when a token was read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    internal static bool TryReadToken(string text, ref int position, out string token)
    {
        Argument.ThrowIfNull(text);

        var index = position;
        while (index < text.Length && IsSpace(text[index]))
        {
            index++;
        }

        var start = index;
        while (index < text.Length && !IsSpace(text[index]))
        {
            index++;
        }

        position = index;
        token = start == 0 && index == text.Length ? text : text.Substring(start, index - start);
        return index > start;
    }

    /// <summary>Gets the first element in document order whose ID is a value.</summary>
    /// <param name="id">The ID value.</param>
    /// <param name="element">The element when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when an element has the ID.</returns>
    internal bool TryGetElement(string id, out XmlNode element) => _elements.TryGetValue(id, out element);

    /// <summary>Gets the attributes in document order whose IDREF or IDREFS value names an ID.</summary>
    /// <param name="id">The ID value.</param>
    /// <param name="attributes">The attributes when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when an attribute names the ID.</returns>
    internal bool TryGetReferences(string id, out List<XmlNode>? attributes) =>
        _references.TryGetValue(id, out attributes);

    private static IdIndex Build(XmlTree tree)
    {
        var index = new IdIndex();
        var root = tree.Document;
        var node = root;
        while (!node.IsEmpty)
        {
            if (node.Kind == XmlNodeKind.Element)
            {
                index.AddAttributes(node);
            }

            node = Next(node, root);
        }

        return index;
    }

    // The next node in document order below the root: the first child, else the next sibling of the node or of its nearest ancestor.
    private static XmlNode Next(XmlNode node, XmlNode root)
    {
        var child = node.FirstChild;
        if (!child.IsEmpty)
        {
            return child;
        }

        while (node != root)
        {
            var sibling = node.NextSibling;
            if (!sibling.IsEmpty)
            {
                return sibling;
            }

            node = node.Parent;
        }

        return default;
    }

    private static bool IsSpace(char c) => c is ' ' or '\t' or '\n' or '\r';

    private void AddAttributes(XmlNode element)
    {
        for (var attribute = element.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
        {
            switch (attribute.AttributeType)
            {
                case DtdAttributeType.Id:
                    AddId(attribute.StringValue, element);
                    break;
                case DtdAttributeType.IdRef or DtdAttributeType.IdRefs:
                    AddReferences(attribute);
                    break;
                default:
                    break;
            }
        }
    }

    // The first element in document order keeps the ID; a later one with the same value is not found by it.
    private void AddId(string id, XmlNode element)
    {
        var position = 0;
        if (TryReadToken(id, ref position, out var token) && !_elements.ContainsKey(token))
        {
            _elements.Add(token, element);
        }
    }

    private void AddReferences(XmlNode attribute)
    {
        var value = attribute.StringValue;
        var position = 0;
        while (TryReadToken(value, ref position, out var token))
        {
            if (!_references.TryGetValue(token, out var list))
            {
                list = [];
                _references.Add(token, list);
            }

            // An IDREFS value that repeats a token adds the attribute once.
            if (list.Count == 0 || list[list.Count - 1] != attribute)
            {
                list.Add(attribute);
            }
        }
    }
}
