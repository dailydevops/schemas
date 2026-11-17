namespace NetEvolve.Schemas.Tests.Architecture.XPath.Fixtures.Evaluation;

/// <summary>Conforming fixture: evaluation code that reaches the function library through the registry.</summary>
internal static class EvaluationUsesRegistryFixture
{
    public static object Create() => new Functions.Binding.RegistryFixture();
}
