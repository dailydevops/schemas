namespace NetEvolve.Schemas.Xsd.Tests.Integration.Hostile;

/// <summary>One hostile schema and instance combination with the rule code its run must end with.</summary>
/// <param name="Id">The unique case name.</param>
/// <param name="Category">The attack family, for the report.</param>
/// <param name="Schema">The schema document that is added to the builder.</param>
/// <param name="Documents">The documents the resolver can serve, by absolute URI.</param>
/// <param name="Instance">The instance document, or <see langword="null"/> when the case ends while the schema is built.</param>
/// <param name="ExpectedCode">The diagnostic number the run must report.</param>
/// <param name="SchemaLocationHints">Whether the xsi:schemaLocation hints of the instance are followed.</param>
/// <param name="Limits">The limits of the validator.</param>
/// <param name="ExternalAccess">Whether the case tries to reach a file or a network address outside the corpus.</param>
internal sealed record HostileCase(
    string Id,
    string Category,
    string Schema,
    IReadOnlyDictionary<string, string> Documents,
    string? Instance,
    int ExpectedCode,
    bool SchemaLocationHints,
    ValidationLimits Limits,
    bool ExternalAccess
)
{
    /// <summary>The base address of the documents of a case.</summary>
    public string BaseUri => "file:///hostile/" + Id + "/";

    public string SchemaUri => BaseUri + "schema.xsd";

    public string InstanceUri => BaseUri + "instance.xml";

    public override string ToString() => Id;
}
