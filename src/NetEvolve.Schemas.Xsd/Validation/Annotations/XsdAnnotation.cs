namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The annotation of one node: validity, type name, nilled state, and typed value.</summary>
internal readonly struct XsdAnnotation : IEquatable<XsdAnnotation>
{
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
    public XsdValidity Validity => throw new NotSupportedException("Not implemented yet.");

    /// <summary>Gets the type name as the data model requires it.</summary>
    public XsdTypeName TypeName => throw new NotSupportedException("Not implemented yet.");

    /// <summary>Gets a value indicating whether the element is nilled.</summary>
    public bool IsNilled => throw new NotSupportedException("Not implemented yet.");

    /// <summary>Gets the kind of the typed value.</summary>
    public XsdTypedValueKind TypedValueKind => throw new NotSupportedException("Not implemented yet.");

    /// <summary>Gets the number of items of the typed value.</summary>
    public int TypedValueCount => throw new NotSupportedException("Not implemented yet.");

    /// <summary>Copies the typed value.</summary>
    /// <param name="destination">The destination.</param>
    /// <returns>The number of items written.</returns>
    public int CopyTypedValue(Span<XdmItem> destination) => throw new NotSupportedException("Not implemented yet.");

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
}
