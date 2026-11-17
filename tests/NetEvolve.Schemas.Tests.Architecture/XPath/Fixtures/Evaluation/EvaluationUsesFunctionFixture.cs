namespace NetEvolve.Schemas.Tests.Architecture.XPath.Fixtures.Evaluation;

/// <summary>Violating fixture: evaluation code that references a function-library type directly.</summary>
internal static class EvaluationUsesFunctionFixture
{
    public static object Create() => new Functions.Strings.StringFunctionFixture();
}
