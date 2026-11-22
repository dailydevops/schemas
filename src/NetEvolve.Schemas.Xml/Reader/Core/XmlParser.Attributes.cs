namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Dtd.Attributes;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

// The attribute definitions of the document type declaration (XML-16): once the start tag is complete, every attribute that
// a definition covers is normalized by its declared type, and every definition with a default or a fixed value that the tag
// does not mention is added as an attribute that the document did not specify. The added attributes join the buffered tag
// before the namespace layer runs, so a defaulted namespace declaration binds its prefix like a written one.
internal sealed partial class XmlParser
{
    private bool[] _seen = [];

    // Applies the definitions of the element of the buffered start tag; false when a diagnostic was set.
    private bool ApplyAttributeDeclarations()
    {
        if (_attributeDeclarations?.Find(_tag.WrittenSpan.Slice(0, _elementNameLength)) is not { } declared)
        {
            return true;
        }

        if (_seen.Length < declared.Count)
        {
            _seen = new bool[declared.Count];
        }

        Array.Clear(_seen, 0, declared.Count);
        var specified = _attributeCount;
        for (var i = 0; i < specified; i++)
        {
            ref var attribute = ref _attributes[i];
            var name = _tag.WrittenSpan.Slice(attribute.NameStart, attribute.NameLength);
            if (declared.TryFind(name, out var index))
            {
                _seen[index] = true;
                attribute.Type = declared[index].Type;
                Collapse(ref attribute);
            }
        }

        foreach (var index in declared.Defaulted)
        {
            if (!_seen[index] && !AddDefault(declared, index))
            {
                return false;
            }
        }

        return true;
    }

    // Adds the attribute of a definition with a default through the same funnel as a written one, so the limits of the
    // attributes, the namespace declarations, the nodes, and the value length apply, and expands the literal of the default
    // as an attribute value (section 3.3.3). A diagnostic is at the start tag, like one of a reference.
    private bool AddDefault(DtdElementAttributes declared, int index)
    {
        var definition = declared[index];
        if (!AttributeName(definition.Name.AsSpan(), _startPosition))
        {
            return false;
        }

        ref var added = ref _attributes[_attributeCount - 1];
        added.IsDefault = true;
        added.Type = definition.Type;
        if (_frameCount == 0)
        {
            _referencePosition = _startPosition;
        }

        if (!ExpandInAttributeValue(declared.DefaultText(index), _startPosition, isLiteral: true))
        {
            return false;
        }

        Collapse(ref _attributes[_attributeCount - 1]);
        return true;
    }

    // Section 3.3.3: a declared type other than CDATA trims and collapses the space characters; the tag buffer is rewritten in
    // place, because the result is never longer, so the chunking of the input cannot show.
    private void Collapse(ref AttributeEntry attribute)
    {
        if (attribute.Type is null or DtdAttributeType.Cdata)
        {
            return;
        }

        var value = MemoryMarshal.AsMemory(_tag.WrittenMemory).Span.Slice(attribute.ValueStart, attribute.ValueLength);
        var length = DtdAttributeNormalizer.Collapse(value);
        attribute.IsChanged = length != attribute.ValueLength;
        attribute.ValueLength = length;
    }

    // The declaration facts of an attribute of the buffered start tag; the memory is the tag buffer.
    private ValidationAttributeInfo AttributeFacts(int index)
    {
        var attribute = _attributes[index];
        var tag = _tag.WrittenMemory;

        return new ValidationAttributeInfo(
            tag.Slice(attribute.NameStart, attribute.NameLength),
            tag.Slice(attribute.ValueStart, attribute.ValueLength),
            attribute.Type,
            !attribute.IsDefault,
            attribute.Position,
            attribute.IsChanged
        );
    }
}
