namespace NetEvolve.Schemas.Xsd.Tests.Integration.EInvoicing;

/// <summary>One schema set of a pinned archive with the documents that are validated against it.</summary>
/// <param name="Name">The set name, unique across the record; it is the test set of the conformance rate.</param>
/// <param name="SchemaSuite">The key of the suite manifest that pins the schema documents.</param>
/// <param name="Schemas">The root schema documents relative to the suite root; the others are reached through includes and imports.</param>
/// <param name="Documents">The directories of the documents; empty for a set that only builds.</param>
public sealed record EInvoicingSet(
    string Name,
    string SchemaSuite,
    IReadOnlyList<string> Schemas,
    IReadOnlyList<EInvoicingDocumentGroup> Documents
);
