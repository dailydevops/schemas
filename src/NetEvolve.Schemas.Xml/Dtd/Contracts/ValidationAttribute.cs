namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>An attribute of a start tag as a validation step sees it: the normalized value, the declared type, and whether the document specified it.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ValidationAttribute
{
    /// <summary>Initializes a new instance of the <see cref="ValidationAttribute"/> struct.</summary>
    /// <param name="name">The attribute name as written.</param>
    /// <param name="value">The normalized value with references resolved.</param>
    /// <param name="declaredType">The declared type, <see langword="null"/> when the attribute is not declared.</param>
    /// <param name="isSpecified"><see langword="false"/> when the value comes from the default of the declaration.</param>
    /// <param name="position">The position of the attribute.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="declaredType"/> is not a defined value.</exception>
    internal ValidationAttribute(
        ReadOnlyMemory<char> name,
        ReadOnlyMemory<char> value,
        DtdAttributeType? declaredType,
        bool isSpecified,
        XmlPosition position
    )
    {
        if (declaredType is { } type)
        {
            Argument.ThrowIfGreaterThan((int)type, (int)DtdAttributeType.Enumeration);
        }

        Name = name;
        Value = value;
        DeclaredType = declaredType;
        IsSpecified = isSpecified;
        Position = position;
    }

    /// <summary>Gets the attribute name as written.</summary>
    internal ReadOnlyMemory<char> Name { get; }

    /// <summary>Gets the normalized value with references resolved.</summary>
    internal ReadOnlyMemory<char> Value { get; }

    /// <summary>Gets the declared type, <see langword="null"/> when the attribute is not declared.</summary>
    internal DtdAttributeType? DeclaredType { get; }

    /// <summary>Gets a value indicating whether the document specified the attribute, <see langword="false"/> for a defaulted one.</summary>
    internal bool IsSpecified { get; }

    /// <summary>Gets the position of the attribute.</summary>
    internal XmlPosition Position { get; }
}
