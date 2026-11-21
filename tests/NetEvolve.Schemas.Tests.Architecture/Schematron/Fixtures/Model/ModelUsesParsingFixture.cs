namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Model;

/// <summary>Violating fixture: a model type that depends on the parsing stage.</summary>
internal static class ModelUsesParsingFixture
{
    public static object Create() => new Parsing.ParserFixture();
}
