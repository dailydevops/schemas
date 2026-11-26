namespace NetEvolve.Schemas.Xsd.Tests.Integration.EInvoicing;

/// <summary>The documents of one directory of a pinned archive that a schema set validates.</summary>
/// <param name="Suite">The key of the suite manifest that pins the archive.</param>
/// <param name="Directory">The directory relative to the suite root; its XML files are read, without subdirectories.</param>
/// <param name="Expectation">
/// <c>valid</c> for official examples that the schema set must accept; <c>differential</c> for documents without a stated
/// expectation, whose verdict must equal the verdict of System.Xml.Schema.
/// </param>
public sealed record EInvoicingDocumentGroup(string Suite, string Directory, string Expectation);
