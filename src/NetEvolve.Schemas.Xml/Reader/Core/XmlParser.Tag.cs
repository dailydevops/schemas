namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Limits;
using NetEvolve.Schemas.Xml.Namespaces;

internal sealed partial class XmlParser
{
    // One attribute of the buffered start tag: the name and the normalized value are slices of the tag buffer.
    [StructLayout(LayoutKind.Auto)]
    private struct AttributeEntry
    {
        internal int NameStart;
        internal int NameLength;
        internal int ValueStart;
        internal int ValueLength;
        internal bool IsDeclaration;
        internal bool IsDefault;
        internal bool IsChanged;
        internal DtdAttributeType? Type;
        internal XmlPosition Position;
        internal XmlName Name;
        internal XmlNamespaceBinding Binding;
    }

    private readonly PooledBuffer<char> _tag = new(256);
    private AttributeEntry[] _attributes = new AttributeEntry[8];
    private XmlName[] _stack = new XmlName[16];
    private XmlBudget _elementDepth;
    private int _attributeCount;
    private int _plainAttributeCount;
    private int _declarationCount;
    private int _elementNameLength;
    private int _depth;
    private bool _inTag;
    private XmlPosition _startPosition;
    private XmlPosition _endTagPosition;

    private void InitializeTag(XmlReaderLimits limits) => _elementDepth = XmlBudget.ElementDepth(limits);

    private void DisposeTag() => _tag.Dispose();

    private bool StartTag(ReadOnlySpan<char> name, XmlPosition position)
    {
        if (_phase == Phase.Epilog)
        {
            return Fail(XmlErrorCodes.XML0012, position);
        }

        if (!_elementDepth.TryCharge(1))
        {
            return Fail(XmlErrorCodes.XML1102, position);
        }

        if (!_nodes.TryCharge(1))
        {
            return Fail(XmlErrorCodes.XML1108, position);
        }

        EndText();
        _phase = Phase.Content;
        _tag.Clear();
        _tag.Append(name);
        _elementNameLength = name.Length;
        _attributeCount = 0;
        _plainAttributeCount = 0;
        _declarationCount = 0;
        _startPosition = position;
        _inTag = true;
        return true;
    }

    private bool AttributeName(ReadOnlySpan<char> name, XmlPosition position)
    {
        var isDeclaration = XmlNamespaceScope.IsDeclaration(name);
        if (isDeclaration)
        {
            if (++_declarationCount > _limits.MaxNamespaceDeclarationsPerElement)
            {
                return Fail(XmlErrorCodes.XML1104, position);
            }
        }
        else
        {
            if (++_plainAttributeCount > _limits.MaxAttributesPerElement)
            {
                return Fail(XmlErrorCodes.XML1103, position);
            }

            if (!_nodes.TryCharge(1))
            {
                return Fail(XmlErrorCodes.XML1108, position);
            }
        }

        // ponytail: linear scan, quadratic in the attributes of one element, which MaxAttributesPerElement bounds.
        var tag = _tag.WrittenSpan;
        for (var i = 0; i < _attributeCount; i++)
        {
            if (tag.Slice(_attributes[i].NameStart, _attributes[i].NameLength).SequenceEqual(name))
            {
                return Fail(XmlErrorCodes.XML0006, position);
            }
        }

        if (_attributeCount == _attributes.Length)
        {
            Array.Resize(ref _attributes, _attributeCount * 2);
        }

        var start = _tag.Length;
        _tag.Append(name);
        _attributes[_attributeCount++] = new AttributeEntry
        {
            NameStart = start,
            NameLength = name.Length,
            ValueStart = _tag.Length,
            IsDeclaration = isDeclaration,
            Position = position,
        };
        return true;
    }

    private bool AppendValue(ReadOnlySpan<char> text, XmlPosition position)
    {
        ref var attribute = ref _attributes[_attributeCount - 1];
        if (text.Length > _limits.MaxAttributeValueLength - attribute.ValueLength)
        {
            return Fail(XmlErrorCodes.XML1112, position);
        }

        _tag.Append(text);
        attribute.ValueLength += text.Length;
        return true;
    }

    // Namespace layer: push the scope, declare every namespace attribute (they may follow the names that use them), then
    // resolve the element name and the other attribute names; the first error is reported at its attribute or the tag.
    private bool ResolveStartTag(ReadOnlySpan<char> tag, out XmlName name)
    {
        var scope = _scope!;
        scope.PushElement();
        name = default;
        for (var i = 0; i < _attributeCount; i++)
        {
            ref var attribute = ref _attributes[i];
            if (attribute.IsDeclaration)
            {
                var error = scope.Declare(
                    tag.Slice(attribute.NameStart, attribute.NameLength),
                    tag.Slice(attribute.ValueStart, attribute.ValueLength),
                    out attribute.Binding,
                    isName: true
                );
                if (error is not null)
                {
                    return Fail(error, attribute.Position);
                }
            }
        }

        var elementError = scope.ResolveElement(tag.Slice(0, _elementNameLength), out name, isName: true);
        if (elementError is not null)
        {
            return Fail(elementError, _startPosition);
        }

        for (var i = 0; i < _attributeCount; i++)
        {
            ref var attribute = ref _attributes[i];
            var error = attribute.IsDeclaration
                ? null
                : scope.ResolveAttribute(
                    tag.Slice(attribute.NameStart, attribute.NameLength),
                    out attribute.Name,
                    isName: true
                );
            if (error is not null)
            {
                return Fail(error, attribute.Position);
            }
        }

        return true;
    }

    private bool FinishStartTag<TSink>(ref TSink sink, bool isEmpty)
        where TSink : IXmlSink
    {
        _inTag = false;
        if (!ApplyAttributeDeclarations())
        {
            return false;
        }

        var tag = _tag.WrittenSpan;
        if (!ResolveStartTag(tag, out var name))
        {
            return false;
        }

        EndText();
        EventPosition = _startPosition;
        sink.StartElement(name, _startPosition);

        // Only a sink of a reference type is offered the declaration facts: a boxed copy of a struct sink would lose its state.
        var infoSink =
            _attributeDeclarations is not null && !typeof(TSink).IsValueType ? sink as IXmlAttributeInfoSink : null;
        for (var i = 0; i < _attributeCount; i++)
        {
            if (_attributes[i].IsDeclaration)
            {
                EventPosition = _attributes[i].Position;
                sink.NamespaceDeclaration(_attributes[i].Binding.Prefix, _attributes[i].Binding.Uri);
                infoSink?.AttributeInfo(AttributeFacts(i));
            }
        }

        for (var i = 0; i < _attributeCount; i++)
        {
            if (!_attributes[i].IsDeclaration)
            {
                EventPosition = _attributes[i].Position;
                sink.Attribute(_attributes[i].Name, tag.Slice(_attributes[i].ValueStart, _attributes[i].ValueLength));
                infoSink?.AttributeInfo(AttributeFacts(i));
            }
        }

        sink.EndAttributes();
        if (_depth == _stack.Length)
        {
            Array.Resize(ref _stack, _depth * 2);
        }

        _stack[_depth++] = name;
        if (isEmpty)
        {
            EventPosition = _startPosition;
            sink.EndElement(name);
            CloseElement();
        }

        return true;
    }

    // An end tag repeats the raw name of its start tag, so the prefix and local name are compared, not the namespace URI.
    // A name that is not in the table cannot match, so lookups never intern.
    private bool EndTagName(ReadOnlySpan<char> name, XmlPosition position)
    {
        // An end tag inside the replacement text of an entity cannot close an element that was opened outside of it.
        if (_depth <= FrameElementDepth)
        {
            return Fail(XmlErrorCodes.XML0005, position);
        }

        var colon = name.IndexOf(':');
        var prefix = colon < 0 ? ReadOnlySpan<char>.Empty : name.Slice(0, colon);
        var local = name.Slice(colon + 1);
        var open = _stack[_depth - 1];
        _endTagPosition = position;
        return (
                _names!.TryGet(prefix, out var prefixSymbol)
                && _names.TryGet(local, out var localSymbol)
                && prefixSymbol == open.Prefix
                && localSymbol == open.Local
            ) || Fail(XmlErrorCodes.XML0005, position);
    }

    private bool EndTagEnd<TSink>(ref TSink sink)
        where TSink : IXmlSink
    {
        EndText();
        EventPosition = _endTagPosition;
        sink.EndElement(_stack[_depth - 1]);
        CloseElement();
        return true;
    }

    private void CloseElement()
    {
        _depth--;
        _scope!.PopElement();
        _elementDepth.Release(1);
        if (_depth == 0)
        {
            _phase = Phase.Epilog;
        }
    }
}
