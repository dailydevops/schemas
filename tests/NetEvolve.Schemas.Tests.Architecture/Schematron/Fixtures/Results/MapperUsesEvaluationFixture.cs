namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Results;

/// <summary>Violating fixture: the issue mapper that depends on the evaluation stage.</summary>
internal static class MapperUsesEvaluationFixture
{
    public static object Create() => new Evaluation.EvaluatorFixture();
}
