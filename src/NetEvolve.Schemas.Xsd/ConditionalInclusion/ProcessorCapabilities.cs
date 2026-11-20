namespace NetEvolve.Schemas.Xsd.ConditionalInclusion;

using NetEvolve.Arguments;

/// <summary>What this processor knows, as far as conditional inclusion asks (Structures 1.1, 4.2.2).</summary>
internal static class ProcessorCapabilities
{
    /// <summary>Tells whether a type is automatically known to the processor.</summary>
    /// <param name="namespaceName">The namespace name, empty for no namespace.</param>
    /// <param name="localName">The local name.</param>
    /// <returns><see langword="true"/> when the processor knows the type.</returns>
    internal static bool IsTypeAvailable(string namespaceName, string localName)
    {
        Argument.ThrowIfNull(namespaceName);
        Argument.ThrowIfNull(localName);

        return false;
    }

    /// <summary>Tells whether a facet is known to and supported by the processor.</summary>
    /// <param name="namespaceName">The namespace name, empty for no namespace.</param>
    /// <param name="localName">The local name of the element that applies the facet.</param>
    /// <returns><see langword="true"/> when the processor supports the facet.</returns>
    internal static bool IsFacetAvailable(string namespaceName, string localName)
    {
        Argument.ThrowIfNull(namespaceName);
        Argument.ThrowIfNull(localName);

        return false;
    }
}
