namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.Suite;

/// <summary>One case of the W3C XInclude test suite catalog.</summary>
/// <param name="Id">The case id, unique in the catalog.</param>
/// <param name="Contributor">The contributor folder, for example <c>Imaq</c>; the baseline is kept per contributor.</param>
/// <param name="InputPath">The path of the input document relative to the suite root, with forward slashes.</param>
/// <param name="Type">The expected result.</param>
/// <param name="Features">The features the case needs, empty when none.</param>
/// <param name="OutputPath">The path of the reference result relative to the suite root, or <see langword="null"/> when the case has none.</param>
/// <param name="Description">The description of the case.</param>
internal sealed record XIncludeCase(
    string Id,
    string Contributor,
    string InputPath,
    XIncludeCaseType Type,
    IReadOnlyList<string> Features,
    string? OutputPath,
    string Description
)
{
    /// <summary>Gets the name of the test set of the case: the contributor folder.</summary>
    public string TestSet => Contributor;
}
