namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>A test selected for one XSD version, with the expectation that applies to that version.</summary>
/// <param name="Id">The stable hierarchical case id: <c>version/group/name</c>; unique within its test set.</param>
/// <param name="Version">The XSD version token.</param>
/// <param name="ExpectValid"><see langword="true"/> when the schema or instance must be valid.</param>
/// <param name="Case">The test.</param>
internal sealed record XstsSelectedCase(string Id, string Version, bool ExpectValid, XstsCase Case)
{
    public string TestSet => Case.TestSet;
}
