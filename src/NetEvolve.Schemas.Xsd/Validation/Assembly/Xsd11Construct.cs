namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>One XSD 1.1 construct that the assembled validator does not support yet, with the rule it reports under the default version.</summary>
internal sealed class Xsd11Construct
{
    internal Xsd11Construct(
        bool isAttribute,
        string[] names,
        XsdRule notYetSupported,
        string[]? owners = null,
        bool localOnly = false,
        Func<SchemaSyntaxNode, string, bool>? valueMatches = null
    )
    {
        Argument.ThrowIfNull(names);
        Argument.ThrowIfNull(notYetSupported);

        IsAttribute = isAttribute;
        Names = names;
        NotYetSupported = notYetSupported;
        Owners = owners ?? [];
        LocalOnly = localOnly;
        ValueMatches = valueMatches;
    }

    /// <summary>Gets a value indicating whether the construct is an attribute (otherwise an element).</summary>
    internal bool IsAttribute { get; }

    /// <summary>Gets the local names that carry the construct; an attribute construct may have several, such as every attribute that names a type.</summary>
    internal string[] Names { get; }

    /// <summary>Gets the not-yet-supported rule.</summary>
    internal XsdRule NotYetSupported { get; }

    /// <summary>Gets the local names of the elements that carry the attribute; empty for any.</summary>
    internal string[] Owners { get; }

    /// <summary>Gets a value indicating whether the construct is reported only on a local declaration.</summary>
    internal bool LocalOnly { get; }

    /// <summary>Gets the test of the attribute value, <see langword="null"/> when the presence of the attribute is the construct.</summary>
    internal Func<SchemaSyntaxNode, string, bool>? ValueMatches { get; }

    /// <summary>Gets the name that messages use.</summary>
    internal string Name => Names[0];
}
