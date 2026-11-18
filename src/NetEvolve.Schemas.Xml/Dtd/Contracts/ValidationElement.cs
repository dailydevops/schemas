namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>An element as a validation step sees it: the name, the position, and for a start tag the attributes after defaults are applied.</summary>
/// <remarks>The memory is valid only during the call that delivers the element; a step copies what it keeps.</remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ValidationElement
{
    /// <summary>Initializes a new instance of the <see cref="ValidationElement"/> struct.</summary>
    /// <param name="name">The element name as written.</param>
    /// <param name="position">The position of the tag.</param>
    /// <param name="attributes">The attributes of a start tag, empty for an end tag.</param>
    internal ValidationElement(
        ReadOnlyMemory<char> name,
        XmlPosition position,
        ReadOnlyMemory<ValidationAttribute> attributes = default
    )
    {
        Name = name;
        Position = position;
        Attributes = attributes;
    }

    /// <summary>Gets the element name as written.</summary>
    internal ReadOnlyMemory<char> Name { get; }

    /// <summary>Gets the position of the tag.</summary>
    internal XmlPosition Position { get; }

    /// <summary>Gets the attributes of a start tag, empty for an end tag.</summary>
    internal ReadOnlyMemory<ValidationAttribute> Attributes { get; }
}
