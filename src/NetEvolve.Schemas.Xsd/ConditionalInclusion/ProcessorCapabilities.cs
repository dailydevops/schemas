namespace NetEvolve.Schemas.Xsd.ConditionalInclusion;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>What this processor knows, as far as conditional inclusion asks (Structures 1.1, 4.2.2).</summary>
/// <remarks>
/// The types are the built-in types of the version 1.1 registry, so a datatype that the registry does not hold yet is not
/// available. Limitation: the facet <c>assertion</c> is known but not supported before #321, so it is not available; add it
/// to the facet list in the change that supports it.
/// </remarks>
internal static class ProcessorCapabilities
{
    /// <summary>The version of XSD that this processor supports, the variable V of the specification.</summary>
    internal static readonly XsdDecimal Version = XsdDecimal.Parse("1.1".AsSpan()).Value;

    // The names of the elements that apply the supported facets.
    private static readonly string[] Facets =
    [
        "length",
        "minLength",
        "maxLength",
        "pattern",
        "enumeration",
        "whiteSpace",
        "maxInclusive",
        "maxExclusive",
        "minExclusive",
        "minInclusive",
        "totalDigits",
        "fractionDigits",
        "explicitTimezone",
    ];

    private static readonly Lazy<HashSet<string>> Types = new(static () => CollectTypes());

    /// <summary>Tells whether a type is automatically known to the processor.</summary>
    /// <param name="namespaceName">The namespace name, empty for no namespace.</param>
    /// <param name="localName">The local name.</param>
    /// <returns><see langword="true"/> when the processor knows the type.</returns>
    internal static bool IsTypeAvailable(string namespaceName, string localName)
    {
        Argument.ThrowIfNull(namespaceName);
        Argument.ThrowIfNull(localName);

        return string.Equals(namespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
            && Types.Value.Contains(localName);
    }

    /// <summary>Tells whether a facet is known to and supported by the processor.</summary>
    /// <param name="namespaceName">The namespace name, empty for no namespace.</param>
    /// <param name="localName">The local name of the element that applies the facet.</param>
    /// <returns><see langword="true"/> when the processor supports the facet.</returns>
    internal static bool IsFacetAvailable(string namespaceName, string localName)
    {
        Argument.ThrowIfNull(namespaceName);
        Argument.ThrowIfNull(localName);

        return string.Equals(namespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
            && Array.IndexOf(Facets, localName) >= 0;
    }

    private static HashSet<string> CollectTypes()
    {
        var types = XsdBuiltInTypes.For(XsdVersion.V11);
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < types.Count; i++)
        {
            _ = names.Add(types[i].Name.LocalName);
        }

        return names;
    }
}
