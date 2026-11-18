namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

/// <summary>Where a golden document lives: a file inside a pinned suite, never a free URL.</summary>
/// <param name="SuiteKey">The key of the suite manifest (<c>tests/conformance/suites/&lt;suite&gt;.json</c>) that pins the archive.</param>
/// <param name="Path">The forward-slash path of the document relative to the suite root.</param>
internal sealed record GoldenDocumentSource(string SuiteKey, string Path)
{
    /// <summary>Gets the stable case id, <c>&lt;suite key&gt;/&lt;path&gt;</c>.</summary>
    public string Id => $"{SuiteKey}/{Path}";
}
