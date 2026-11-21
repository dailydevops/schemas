namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>One corpus file: a hostile document, the resources it can reach, how it loads, and the diagnostic it must end in.</summary>
/// <param name="Name">The unique name of the file.</param>
/// <param name="Category">The family of hostile input.</param>
/// <param name="Expected">The code of the diagnostic every input kind must report.</param>
/// <param name="Document">Builds the document text on demand, so the corpus never holds all large files at once.</param>
/// <param name="Access">How the file loads with respect to external access.</param>
/// <param name="Limits">The limits of the load, <see langword="null"/> for the library defaults.</param>
/// <param name="Resources">Builds the external resources by absolute URI, <see langword="null"/> for none.</param>
internal sealed record HostileCase(
    string Name,
    HostileCategory Category,
    XmlErrorCode Expected,
    Func<string> Document,
    HostileAccess Access = HostileAccess.Internal,
    XmlReaderLimits? Limits = null,
    IReadOnlyDictionary<string, Func<string>>? Resources = null
)
{
    /// <inheritdoc />
    public override string ToString() => Name;
}
