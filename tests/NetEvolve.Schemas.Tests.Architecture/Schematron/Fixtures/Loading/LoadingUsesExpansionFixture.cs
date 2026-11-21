namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Loading;

/// <summary>Violating fixture: the loading stage that depends on the expansion stage after it.</summary>
internal static class LoadingUsesExpansionFixture
{
    public static object Create() => new Expansion.ExpanderFixture();
}
