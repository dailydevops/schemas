namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

/// <summary>One case of the suite.</summary>
/// <param name="Id">The id: the test set and the catalog <c>ID</c>, for example <c>xmltest/001</c>.</param>
/// <param name="TestSet">The test set: the effective <c>xml:base</c> of the case with <c>/</c> replaced by <c>-</c>.</param>
/// <param name="Uri">The document path relative to the suite root, with <c>/</c> separators.</param>
/// <param name="Type">The type of the case.</param>
/// <param name="Entities">The <c>ENTITIES</c> tag; <see cref="XmlConfEntities.None"/> when the catalog has none.</param>
/// <param name="Versions">The <c>VERSION</c> tokens; empty when the case applies to every version.</param>
/// <param name="Editions">The <c>EDITION</c> tokens; empty when the case applies to every edition.</param>
/// <param name="Recommendation">The <c>RECOMMENDATION</c>; <c>XML1.0</c> when the catalog has none.</param>
/// <param name="Namespaces">Whether namespace processing is enabled; <see langword="false"/> for <c>NAMESPACE="no"</c>.</param>
/// <param name="Description">The description text of the case.</param>
internal sealed record XmlConfCase(
    string Id,
    string TestSet,
    string Uri,
    XmlConfType Type,
    XmlConfEntities Entities,
    IReadOnlyList<string> Versions,
    IReadOnlyList<string> Editions,
    string Recommendation,
    bool Namespaces,
    string Description
);
