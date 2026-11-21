namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Evaluation;

/// <summary>Violating fixture: evaluation code that depends on the report writer.</summary>
internal static class EvaluationUsesSvrlFixture
{
    public static object Create() => new Svrl.WriterFixture();
}
