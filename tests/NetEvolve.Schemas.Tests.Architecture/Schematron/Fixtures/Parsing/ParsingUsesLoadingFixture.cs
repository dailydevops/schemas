namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Parsing;

/// <summary>Violating fixture: the parsing stage that depends on the loading stage after it.</summary>
internal static class ParsingUsesLoadingFixture
{
    public static object Create() => new Loading.LoaderFixture();
}
