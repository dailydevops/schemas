namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>A schema or instance test as written in a test set, before any filter is applied.</summary>
/// <param name="TestSet">The test set key: the directory and file name of the test set without extension.</param>
/// <param name="Group">The name of the test group.</param>
/// <param name="Name">The name of the test; unique within its group.</param>
/// <param name="Kind">The kind of the test.</param>
/// <param name="SchemaDocuments">The schema documents, relative to the suite root; for an instance test those of its group.</param>
/// <param name="InstanceDocument">The instance document relative to the suite root; <see langword="null"/> for a schema test.</param>
/// <param name="Status">The current status, or <see langword="null"/> when the test records none.</param>
/// <param name="VersionScopes">The version tokens of every enclosing element that has a <c>version</c> attribute; each applies on its own.</param>
/// <param name="Expectations">The <c>expected</c> elements of the test.</param>
/// <param name="ExclusionReason">A reason that excludes the test whatever the filter, or <see langword="null"/>.</param>
internal sealed record XstsCase(
    string TestSet,
    string Group,
    string Name,
    XstsKind Kind,
    IReadOnlyList<string> SchemaDocuments,
    string? InstanceDocument,
    string? Status,
    IReadOnlyList<IReadOnlyList<string>> VersionScopes,
    IReadOnlyList<XstsExpectation> Expectations,
    string? ExclusionReason = null
);
