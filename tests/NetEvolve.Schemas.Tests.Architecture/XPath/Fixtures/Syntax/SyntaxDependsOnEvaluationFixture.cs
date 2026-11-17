namespace NetEvolve.Schemas.Tests.Architecture.XPath.Fixtures.Syntax;

/// <summary>Violating fixture: a syntax type that depends on the evaluation layer.</summary>
internal static class SyntaxDependsOnEvaluationFixture
{
    public static object Create() => new Evaluation.EvaluatorFixture();
}
