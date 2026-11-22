namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// What the validator knows about one element or attribute after assessment: the key (the ordinal of the element start event and, for an
/// attribute, its index among the attributes of the element), the type, the validity, whether the element is nilled, and the default or fixed
/// value that an empty element or an absent attribute took from its declaration, which the input tree has no node or text for. The typed value is
/// not stored; it is derived from the type and the text of the node when it is read.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct AnnotationRecord
{
    internal AnnotationRecord(
        int ordinal,
        int attributeIndex,
        IXsdComponent? type,
        XsdValidity validity,
        bool isNilled,
        string? defaultedValue = null,
        XmlName defaultedName = default
    )
    {
        Ordinal = ordinal;
        AttributeIndex = attributeIndex;
        Type = type;
        Validity = validity;
        IsNilled = isNilled;
        DefaultedValue = defaultedValue;
        DefaultedName = defaultedName;
    }

    /// <summary>Gets the ordinal of the element start event in document order, starting at 1; for an attribute the ordinal of its owner.</summary>
    public int Ordinal { get; }

    /// <summary>Gets the index of the attribute among the attributes of its owner, <c>-1</c> for an element.</summary>
    public int AttributeIndex { get; }

    /// <summary>Gets the type that assessed the node, <see langword="null"/> when none did.</summary>
    public IXsdComponent? Type { get; }

    /// <summary>Gets the validity.</summary>
    public XsdValidity Validity { get; }

    /// <summary>Gets a value indicating whether the element is nilled; always <see langword="false"/> for an invalid element and an attribute.</summary>
    public bool IsNilled { get; }

    /// <summary>Gets the default or fixed value that an empty element or an absent attribute took from its declaration, <see langword="null"/> when the element has text of its own and for an attribute that the document specified.</summary>
    public string? DefaultedValue { get; }

    /// <summary>Gets the name of a defaulted attribute, as the declaration spells it; <see langword="default"/> for an element and for an attribute that the document specified.</summary>
    public XmlName DefaultedName { get; }

    /// <summary>Gets a value indicating whether the record describes an attribute.</summary>
    public bool IsAttribute => AttributeIndex >= 0;
}
