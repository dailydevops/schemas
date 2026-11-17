namespace NetEvolve.Schemas.Xsd.Contracts;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The input of <see cref="ValidationListener.Attribute"/>, valid only during the call.</summary>
internal readonly ref struct AttributeInfo
{
    internal AttributeInfo(
        int ownerOrdinal,
        XmlName name,
        DocumentLocation location,
        IXsdComponent? declaration,
        ReadOnlySpan<char> value,
        IXsdComponent? type,
        bool isValid,
        bool isDefaulted
    )
    {
        OwnerOrdinal = ownerOrdinal;
        Name = name;
        Location = location;
        Declaration = declaration;
        Value = value;
        Type = type;
        IsValid = isValid;
        IsDefaulted = isDefaulted;
    }

    /// <summary>Gets the ordinal of the owner element.</summary>
    public int OwnerOrdinal { get; }

    /// <summary>Gets the name handle.</summary>
    public XmlName Name { get; }

    /// <summary>Gets the location of the attribute, the owner's start tag for a defaulted one.</summary>
    public DocumentLocation Location { get; }

    /// <summary>Gets the attribute declaration, <see langword="null"/> when none was found.</summary>
    public IXsdComponent? Declaration { get; }

    /// <summary>Gets the normalized value.</summary>
    public ReadOnlySpan<char> Value { get; }

    /// <summary>Gets the validated simple type, <see langword="null"/> when none was found.</summary>
    public IXsdComponent? Type { get; }

    /// <summary>Gets a value indicating whether the attribute is valid.</summary>
    public bool IsValid { get; }

    /// <summary>Gets a value indicating whether the attribute comes from a default or fixed value.</summary>
    public bool IsDefaulted { get; }
}
