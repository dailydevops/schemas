namespace NetEvolve.Schemas.Xml.Dtd.Model;

using System;
using System.Collections.Generic;

/// <summary>An attribute definition of an attribute-list declaration (production 53).</summary>
/// <remarks>Limitation: the default value is the literal as written, with references unexpanded and unnormalized (XML-16).</remarks>
internal sealed class DtdAttributeDefinition
{
    internal DtdAttributeDefinition(
        string name,
        DtdAttributeType type,
        IReadOnlyList<string>? values,
        DtdDefaultKind defaultKind,
        string? defaultValue
    )
    {
        Name = name;
        Type = type;
        Values = values ?? Array.Empty<string>();
        DefaultKind = defaultKind;
        DefaultValue = defaultValue;
    }

    /// <summary>Gets the attribute name.</summary>
    internal string Name { get; }

    /// <summary>Gets the attribute type.</summary>
    internal DtdAttributeType Type { get; }

    /// <summary>Gets the notation names or enumerated tokens, empty for other types.</summary>
    internal IReadOnlyList<string> Values { get; }

    /// <summary>Gets the kind of default declaration.</summary>
    internal DtdDefaultKind DefaultKind { get; }

    /// <summary>Gets the default value for <see cref="DtdDefaultKind.Fixed"/> and <see cref="DtdDefaultKind.Default"/>, otherwise <see langword="null"/>.</summary>
    internal string? DefaultValue { get; }
}
