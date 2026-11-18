namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

/// <summary>A golden document that was fetched through its pinned suite.</summary>
/// <param name="Id">The case id of the source.</param>
/// <param name="Path">The full path of the extracted document.</param>
internal sealed record GoldenDocument(string Id, string Path);
