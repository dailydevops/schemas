namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Svrl;

/// <summary>Violating fixture: the report writer that depends on the issue mapper.</summary>
internal static class WriterUsesResultsFixture
{
    public static object Create() => new Results.MapperFixture();
}
