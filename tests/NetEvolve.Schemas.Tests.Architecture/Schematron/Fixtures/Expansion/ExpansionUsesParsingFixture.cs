namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Expansion;

/// <summary>Violating fixture: the expansion stage that depends on the parsing stage before it.</summary>
internal static class ExpansionUsesParsingFixture
{
    public static object Create() => new Parsing.ParserFixture();
}
