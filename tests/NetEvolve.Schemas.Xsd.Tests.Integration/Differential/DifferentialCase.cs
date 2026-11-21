namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>One schema and optionally one instance that both engines receive, with the expectation recorded for it.</summary>
/// <param name="Id">The stable case id.</param>
/// <param name="SchemaPaths">The absolute paths of the schema documents; relative includes and imports resolve against them.</param>
/// <param name="InstancePath">The absolute path of the instance document, or <see langword="null"/> for a schema-only case.</param>
/// <param name="ExpectedSchema">The recorded verdict for the schema documents.</param>
/// <param name="ExpectedInstance">The recorded verdict for the instance; <see cref="Verdict.NotRun"/> for a schema-only case.</param>
internal sealed record DifferentialCase(
    string Id,
    IReadOnlyList<string> SchemaPaths,
    string? InstancePath,
    Verdict ExpectedSchema,
    Verdict ExpectedInstance
);
