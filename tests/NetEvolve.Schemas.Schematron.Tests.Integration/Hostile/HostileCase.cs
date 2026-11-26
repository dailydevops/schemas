namespace NetEvolve.Schemas.Schematron.Tests.Integration.Hostile;

/// <summary>One hostile schema and instance combination with the diagnostic code its run must report.</summary>
/// <param name="Id">The unique case name; the text before the first dash is the attack family.</param>
/// <param name="Schema">The principal schema document.</param>
/// <param name="Documents">The documents the resolver can serve, by absolute URI.</param>
/// <param name="Instance">The instance document.</param>
/// <param name="ExpectedCode">The diagnostic number the run must report.</param>
/// <param name="Limits">The limits of the validator.</param>
/// <param name="ExternalAccess">Whether the case tries to reach a document outside the corpus.</param>
internal sealed record HostileCase(
    string Id,
    string Schema,
    IReadOnlyDictionary<string, string> Documents,
    string Instance,
    int ExpectedCode,
    ValidationLimits Limits,
    bool ExternalAccess
)
{
    /// <summary>The base address of the documents of a case.</summary>
    public string BaseUri => "file:///hostile/" + Id + "/";

    public string SchemaUri => BaseUri + "schema.sch";

    public string InstanceUri => BaseUri + "instance.xml";

    public override string ToString() => Id;
}
