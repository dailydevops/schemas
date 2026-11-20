namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The annotation of one node: validity, type name, nilled state, and typed value (design #146 DP-18). It holds the source and the node,
/// not a copy of the record's data, because the typed value is not stored: it is derived from the text of the node when it is read.
/// <see langword="default"/> is safe and reads as not known, with empty names and no typed value.
/// </summary>
internal readonly struct XsdAnnotation : IEquatable<XsdAnnotation>
{
    private static readonly XsdTypeName AnyType = Schema("anyType");
    private static readonly XsdTypeName AnySimpleType = Schema("anySimpleType");
    private static readonly XsdTypeName Untyped = Schema("untyped");
    private static readonly XsdTypeName UntypedAtomic = Schema("untypedAtomic");

    private readonly AnnotationSource? _source;
    private readonly XmlNode _node;
    private readonly AnnotationRecord _record;

    internal XsdAnnotation(AnnotationSource source, XmlNode node, in AnnotationRecord record)
    {
        _source = source;
        _node = node;
        _record = record;
    }

    /// <summary>Gets the validity.</summary>
    public XsdValidity Validity => _record.Validity;

    /// <summary>
    /// Gets the type name as the data model requires it (XDM, construction from a PSVI): the name of the type for a valid node,
    /// <c>xs:anyType</c> for an invalid element and <c>xs:anySimpleType</c> for an invalid attribute, and <c>xs:untyped</c> and
    /// <c>xs:untypedAtomic</c> for a node that is not known.
    /// </summary>
    public XsdTypeName TypeName
    {
        get
        {
            if (_source is null)
            {
                return default;
            }

            return _record.Validity switch
            {
                XsdValidity.Valid => NameOf(_record.Type),
                XsdValidity.Invalid => _record.IsAttribute ? AnySimpleType : AnyType,
                _ => _record.IsAttribute ? UntypedAtomic : Untyped,
            };
        }
    }

    /// <summary>Gets a value indicating whether the element is nilled; only a valid element can be.</summary>
    public bool IsNilled => _record.IsNilled;

    /// <summary>
    /// Gets the kind of the typed value: the string value as <c>xs:untypedAtomic</c> for a node that is invalid or not known and for mixed
    /// content, empty for a nilled element and for empty content, the lexical mapping of the simple type for an attribute, a simple type,
    /// and simple content, and none for element-only content.
    /// </summary>
    public XsdTypedValueKind TypedValueKind
    {
        get
        {
            if (_source is null)
            {
                return XsdTypedValueKind.None;
            }

            if (_record.Validity != XsdValidity.Valid)
            {
                return XsdTypedValueKind.Untyped;
            }

            if (_record.IsNilled)
            {
                return XsdTypedValueKind.Empty;
            }

            return _record.IsAttribute ? XsdTypedValueKind.Atomic : KindOfElement(_record.Type);
        }
    }

    /// <summary>Gets the number of items of the typed value, zero for an empty typed value and for none; it reads the text of the node and allocates.</summary>
    public int TypedValueCount =>
        TypedValueKind switch
        {
            XsdTypedValueKind.Untyped => 1,
            XsdTypedValueKind.Atomic => AtomicValues().Count,
            _ => 0,
        };

    /// <summary>Gets the data model type of the node, which is what the annotation slot of the XPath node adapter reads.</summary>
    internal XdmTypeId DataModelType
    {
        get
        {
            if (_record.Validity == XsdValidity.Valid)
            {
                return TypedValues.DataModelType(_record.Type!);
            }

            if (_record.Validity == XsdValidity.Invalid)
            {
                return _record.IsAttribute ? TypedValues.AnySimpleType : TypedValues.AnyType;
            }

            return _record.IsAttribute ? XdmTypeId.UntypedAtomic : XdmTypeId.Untyped;
        }
    }

    /// <summary>Copies the typed value.</summary>
    /// <param name="destination">The destination; items beyond its length are not written.</param>
    /// <returns>The number of items written.</returns>
    public int CopyTypedValue(Span<XdmItem> destination)
    {
        var kind = TypedValueKind;
        if (kind == XsdTypedValueKind.Untyped)
        {
            if (destination.Length == 0)
            {
                return 0;
            }

            destination[0] = XdmItem.FromUntypedAtomic(_node.StringValue);
            return 1;
        }

        if (kind != XsdTypedValueKind.Atomic)
        {
            return 0;
        }

        var items = AtomicValues();
        var written = Math.Min(items.Count, destination.Length);
        for (var i = 0; i < written; i++)
        {
            destination[i] = items[i];
        }

        return written;
    }

    /// <summary>Gets the typed value as one item, when it is exactly one atomic item.</summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true"/> when the typed value is one atomic item.</returns>
    internal bool TryGetSingleItem(out XdmItem item)
    {
        if (TypedValueKind == XsdTypedValueKind.Atomic)
        {
            var items = AtomicValues();
            if (items.Count == 1)
            {
                item = items[0];
                return true;
            }
        }

        item = default;
        return false;
    }

    /// <inheritdoc />
    public bool Equals(XsdAnnotation other) => ReferenceEquals(_source, other._source) && _node == other._node;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XsdAnnotation other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _node.IsEmpty ? 0 : _node.GetHashCode();

    /// <summary>Determines whether two annotations are equal.</summary>
    /// <param name="left">The first annotation.</param>
    /// <param name="right">The second annotation.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XsdAnnotation left, XsdAnnotation right) => left.Equals(right);

    /// <summary>Determines whether two annotations differ.</summary>
    /// <param name="left">The first annotation.</param>
    /// <param name="right">The second annotation.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XsdAnnotation left, XsdAnnotation right) => !left.Equals(right);

    private static XsdTypeName Schema(string local) => new(SchemaGrammar.Namespace, local, false);

    private static XsdTypeName NameOf(IXsdComponent? type) =>
        type is { } named && named.Name != default
            ? new XsdTypeName(named.Name.NamespaceName, named.Name.LocalName, false)
            : XsdTypeName.Anonymous;

    // A complex type decides by its content type; xs:anyType is mixed; every other type is a simple type.
    private static XsdTypedValueKind KindOfElement(IXsdComponent? type) =>
        type switch
        {
            ComplexTypeDefinition { ContentKind: ComplexContentKind.Empty } => XsdTypedValueKind.Empty,
            ComplexTypeDefinition { ContentKind: ComplexContentKind.ElementOnly } => XsdTypedValueKind.None,
            ComplexTypeDefinition { ContentKind: ComplexContentKind.Simple } => XsdTypedValueKind.Atomic,
            ComplexTypeDefinition => XsdTypedValueKind.Untyped,
            XsdBuiltInType { Kind: XsdComponentKind.ComplexType } => XsdTypedValueKind.Untyped,
            _ => XsdTypedValueKind.Atomic,
        };

    private List<XdmItem> AtomicValues()
    {
        var items = new List<XdmItem>(1);
        TypedValues.Append(TypedValues.SimpleTypeOf(_record.Type!), _node.StringValue, _source!.Assessor, items);
        return items;
    }
}
